using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using ScotchLog.Scope;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace ScotchLog.Test.Editor
{
    /// <summary>
    /// Log.Debug() 内のどのステップでGCAllocが発生しているかを特定する診断テスト。
    /// テストが「失敗」した箇所 = GCAllocが発生している箇所。
    ///
    /// フローは以下の通り：
    ///   Log.Debug(msg)
    ///     → LogEntry.Rent()
    ///         → .Get()        [1]
    ///         → LogEntry.Set()
    ///             → DateTime.Now                  [2] ← Mono で TimeZoneInfo 変換アロケ疑い
    ///             → LogScopeRecord.Current        [3] ← AsyncLocal.Value 読み取り疑い
    ///     → LogDispatcher.Log()
    ///         → ThreadLocal<int>.Value++          [4]
    ///         → lock + foreach(Dictionary)        [5] ← Mono では Enumerator アロケ疑い
    ///     → AsyncLocalLogDispatcher?.Log()        [6] ← AsyncLocal.Value 読み取り疑い
    ///     → logEntry.Dispose()
    ///         → .Release()    [7]
    /// </summary>
    public class AllocDiagnostic
    {
        // -------------------------------------------------------
        // ヘルパー：JIT ウォームアップ後に制約を適用
        // -------------------------------------------------------

        private static void AssertNoGCAlloc(Action action, string label)
        {
            // ウォームアップ（JIT / キャッシュ安定化）
            action();
            action();

            Assert.That(
                () => action(),
                Is.Not.AllocatingGCMemory(),
                $"{label} で GCAlloc が発生しています");
        }

        // -------------------------------------------------------
        // [全体] Log.Debug() 全体（まずここで YES/NO を確認）
        // -------------------------------------------------------
        [Test]
        public void Diag_LogDebug_Full()
            => AssertNoGCAlloc(
                () => Log.Debug("message"),
                "Log.Debug()");

        // -------------------------------------------------------
        // [全体+スコープ] BeginScope + Log.Debug
        //
        // NOTE: BeginScope/Dispose は AsyncLocal<T>.Value の書き込みを伴うため
        //       ExecutionContext のコピーが発生し、GCAlloc は回避不可能。
        //       async/await 伝播を維持する限りこれはランタイムの仕様。
        //       Microsoft.Extensions.Logging / Serilog 等も同様に allocate する。
        //
        //       ここでは「スコープが確立された状態での Log.Debug 単体」を
        //       ゼロアロケ保証の対象とする。
        // -------------------------------------------------------
        [Test]
        public void Diag_LogDebug_WithScope_BeginScope_AllocatesAsExpected()
        {
            // BeginScope/Dispose は AsyncLocal 書き込みにより ExecutionContext をアロケートする。
            // async/await 伝播を維持する限り回避不可能な仕様。
            // このテストは「アロケーションが発生すること」を意図的に確認する。
            Assert.That(
                () =>
                {
                    using (Log.BeginScope("S").SetProperty("k", "v"))
                        Log.Debug("message");
                },
                Is.AllocatingGCMemory(),
                "BeginScope/Dispose は AsyncLocal 書き込みにより GCAlloc が発生するはず");
        }

        [Test]
        public void Diag_LogDebug_WithScope_LogDebug_NoAlloc()
        {
            // スコープ確立済みの状態で Log.Debug 単体はゼロアロケであることを保証する
            using var scope = Log.BeginScope("S").SetProperty("k", "v");
            AssertNoGCAlloc(
                () => Log.Debug("message"),
                "Log.Debug（スコープ確立済み）");
        }

        // -------------------------------------------------------
        // [1] ConcurrentObjectPool.Get() / Release()
        // -------------------------------------------------------
        [Test]
        public void Diag_PoolGet()
        {
            AssertNoGCAlloc(
                () =>
                {
                    var entry = LogEntry.Rent(LogLevel.Debug, "x",
                        new CallerInformation("", 0, ""));
                    LogEntry.Return(entry);
                },
                "LogEntry.Rent + Return（Pool の Get/Release）");
        }

        // -------------------------------------------------------
        // [2] DateTime.Now
        // -------------------------------------------------------
        [Test]
        public void Diag_DateTimeNow()
        {
            DateTime dummy = default;
            AssertNoGCAlloc(
                () => dummy = DateTime.Now,
                "DateTime.Now");
            _ = dummy;
        }

        // -------------------------------------------------------
        // [2'] DateTime.UtcNow（比較用）
        // -------------------------------------------------------
        [Test]
        public void Diag_DateTimeUtcNow()
        {
            DateTime dummy = default;
            AssertNoGCAlloc(
                () => dummy = DateTime.UtcNow,
                "DateTime.UtcNow");
            _ = dummy;
        }

        // -------------------------------------------------------
        // [3] LogScopeRecord.Current (AsyncLocal<T>.Value 読み取り)
        // -------------------------------------------------------
        [Test]
        public void Diag_LogScopeRecord_Current()
        {
            LogScopeRecord dummy = null;
            AssertNoGCAlloc(
                () => dummy = LogScopeRecord.Current,
                "LogScopeRecord.Current（AsyncLocal<T>.Value 読み取り）");
            _ = dummy;
        }

        // -------------------------------------------------------
        // [6] Log.AsyncLocalLogDispatcher (AsyncLocal<T>.Value 読み取り)
        // -------------------------------------------------------
        [Test]
        public void Diag_AsyncLocalLogDispatcher_Read()
        {
            ILogDispatcher dummy = null;
            AssertNoGCAlloc(
                () => dummy = Log.AsyncLocalLogDispatcher,
                "Log.AsyncLocalLogDispatcher（AsyncLocal<T>.Value 読み取り）");
            _ = dummy;
        }

        // -------------------------------------------------------
        // [3+6] AsyncLocal<T>.Value 書き込み
        // -------------------------------------------------------
        [Test]
        public void Diag_AsyncLocal_Write()
        {
            var original = Log.AsyncLocalLogDispatcher;
            AssertNoGCAlloc(
                () => Log.AsyncLocalLogDispatcher = null,
                "AsyncLocal<T>.Value 書き込み");
            Log.AsyncLocalLogDispatcher = original;
        }

        // -------------------------------------------------------
        // [4] ThreadLocal<int>.Value++ (LogDispatcher 内)
        // -------------------------------------------------------
        [Test]
        public void Diag_ThreadLocal_ReadWrite()
        {
            var tl = new ThreadLocal<int>(() => 0);
            AssertNoGCAlloc(
                () => tl.Value++,
                "ThreadLocal<int>.Value++");
        }

        // -------------------------------------------------------
        // [5] lock + foreach(Dictionary) (LogDispatcher.Log 内)
        // -------------------------------------------------------
        [Test]
        public void Diag_LockAndForeachDictionary()
        {
            var dict = new Dictionary<int, int> { { 1, 1 }, { 2, 2 } };
            var lockObj = new object();
            int sum = 0;
            AssertNoGCAlloc(
                () =>
                {
                    lock (lockObj)
                        foreach (var kv in dict) sum += kv.Value;
                },
                "lock + foreach(Dictionary<K,V>)");
            _ = sum;
        }
    }
}

