using System;
using NUnit.Framework;
using ScotchLog.Scope;
using Unity.Collections;

namespace ScotchLog.Test.Editor
{
    public class TestLogEntryPersistant
    {
        private static CallerInformation MakeCaller()
            => new CallerInformation("TestFile.cs", 1, "TestMethod");

        /// <summary>
        /// LogEntry + LogScope を安全に作成・クリーンアップするヘルパー。
        /// スコープは LogEntryPersistant.Dispose() より先に Dispose することで
        /// LogScopeRecord のライフサイクルを正しく管理します。
        /// </summary>
        private static (LogEntry entry, LogScope scope) MakeEntryWithScope(LogLevel level, string message)
        {
            var scope = Log.BeginScope("persistantTestScope");
            var entry = LogEntry.Rent(level, message, MakeCaller());
            return (entry, scope);
        }

        // ─── コンストラクタ ──────────────────────────────────────────────────

        [Test]
        public void Constructor_CopiesLogLevel()
        {
            var (entry, scope) = MakeEntryWithScope(LogLevel.Error, "msg");
            var persistant = new LogEntryPersistant(entry);
            entry.Dispose();
            scope.Dispose();

            Assert.That(persistant.LogLevel, Is.EqualTo(LogLevel.Error));

            persistant.Dispose();
        }

        [Test]
        public void Constructor_CopiesTimestamp()
        {
            var before = DateTime.Now;
            var (entry, scope) = MakeEntryWithScope(LogLevel.Debug, "msg");
            var after = DateTime.Now;
            var persistant = new LogEntryPersistant(entry);
            entry.Dispose();
            scope.Dispose();

            Assert.That(persistant.Timestamp, Is.GreaterThanOrEqualTo(before));
            Assert.That(persistant.Timestamp, Is.LessThanOrEqualTo(after));

            persistant.Dispose();
        }

        [Test]
        public void Constructor_ClonesMessage_MessageReadable()
        {
            var (entry, scope) = MakeEntryWithScope(LogLevel.Information, "persisted message");
            var persistant = new LogEntryPersistant(entry);
            entry.Dispose();
            scope.Dispose();

            Assert.That(persistant.StringWrapper.ToString(), Is.EqualTo("persisted message"));

            persistant.Dispose();
        }

        [Test]
        public void Constructor_CopiesCallerInfo()
        {
            var scope = Log.BeginScope("callerScope");
            var caller = new CallerInformation("MySource.cs", 77, "MyMethod");
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", caller);
            var persistant = new LogEntryPersistant(entry);
            entry.Dispose();
            scope.Dispose();

            Assert.That(persistant.CallerInfo.LineNumber, Is.EqualTo(77));
            Assert.That(persistant.CallerInfo.MemberName, Is.EqualTo("MyMethod"));

            persistant.Dispose();
        }

        [Test]
        public void Constructor_ScopeHolder_IsNotNull()
        {
            var (entry, scope) = MakeEntryWithScope(LogLevel.Debug, "msg");
            var persistant = new LogEntryPersistant(entry);
            entry.Dispose();
            scope.Dispose();

            Assert.That(persistant.Scope.Record, Is.Not.Null);

            persistant.Dispose();
        }

        [Test]
        public void Constructor_ScopeHolder_PreservesScopeName()
        {
            var scope = Log.BeginScope("namedScope");
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", MakeCaller());
            var persistant = new LogEntryPersistant(entry);
            entry.Dispose();
            scope.Dispose();  // スコープを先に Dispose してもホルダーが保持

            Assert.That(persistant.Scope.Record.Name, Is.EqualTo("namedScope"));

            persistant.Dispose();
        }

        // ─── Persistent コピー ────────────────────────────────────────────────

        /// <summary>
        /// LogEntryPersistant のメッセージは Persistent アロケーターでコピーされるため、
        /// 元の NativeText(TempJob) バックの StringWrapper を破棄してもメッセージが読める。
        /// </summary>
        [Test]
        public void StringWrapper_IsPersistentClone_ReadableAfterOriginalDisposed()
        {
            var scope = Log.BeginScope("cloneScope");

            // FixedString → StringWrapper の暗黙変換で NativeText(TempJob) が生成される
            StringWrapper tempJobBacked = new FixedString64Bytes("native text message");
            var entry = LogEntry.Rent(LogLevel.Debug, tempJobBacked, MakeCaller());

            var persistant = new LogEntryPersistant(entry);
            // エントリを返却（内部 StringWrapper は dispose される）
            entry.Dispose();

            scope.Dispose();

            // Persistent コピーは元のエントリを破棄しても読める
            Assert.That(persistant.StringWrapper.ToString(), Is.EqualTo("native text message"));

            persistant.Dispose();
        }

        // ─── Dispose ─────────────────────────────────────────────────────────

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var (entry, scope) = MakeEntryWithScope(LogLevel.Debug, "msg");
            var persistant = new LogEntryPersistant(entry);
            entry.Dispose();
            scope.Dispose();

            Assert.DoesNotThrow(() => persistant.Dispose());
        }
    }
}

