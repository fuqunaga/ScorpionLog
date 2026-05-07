using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools.Constraints;

namespace ScotchLog.Test.Editor
{
    /// <summary>
    /// GCAlloc 発生有無を確認する計測テスト。
    ///
    /// AllocDiagnostic の結論：
    ///   - Log.Debug() 単体                    → GCAlloc なし（保証）
    ///   - BeginScope / Dispose                → GCAlloc あり（AsyncLocal 書き込み・仕様）
    ///   - スコープ確立後の Log.Debug() 単体    → GCAlloc なし（保証）
    /// </summary>
    public class AllocationLog
    {
        private const int DefaultCount = 1000;

        // -------------------------------------------------------
        // ヘルパー
        // -------------------------------------------------------

        /// <summary>ウォームアップ後にゼロアロケを保証する</summary>
        private static void AssertNoGCAlloc(Action action, string label)
        {
            action(); action();
            Assert.That(() => action(), new NUnit.Framework.Constraints.NotConstraint(new AllocatingGCMemoryConstraint()),
                $"[NoAlloc期待] {label} で GCAlloc が発生しています");
        }

        /// <summary>ウォームアップ後にアロケーションが発生することを保証する（仕様確認）</summary>
        private static void AssertAllocatesGCMemory(Action action, string label)
        {
            action(); action();
            Assert.That(() => action(), new AllocatingGCMemoryConstraint(),
                $"[Alloc期待] {label} で GCAlloc が発生していません（async/await 伝播が壊れた可能性）");
        }

        /// <summary>
        /// GC.GetTotalMemory の差分で、action 1 回あたりのヒープ増分を推定する。
        ///
        /// 計測前は GC.GetTotalMemory(true) で可能な範囲で収集して基準を揃え、
        /// 計測後は GC.GetTotalMemory(false) で回収を起こさず差分を見る。
        /// サンプルを複数回取り平均することで揺れをならす。
        /// 戻り値の totalBytes は bytesPerCall を count 回分に外挿した推定値。
        /// </summary>
        private static (double totalBytes, double bytesPerCall) MeasureAllocBytes(Action action, int count = DefaultCount)
        {
            // JIT・プール安定化
            action(); action();

            // 計測前に基準メモリを揃え、1回実行の差分を複数サンプル平均する
            const int sampleCount = 20;
            long totalSampledBytes = 0;

            for (var i = 0; i < sampleCount; i++)
            {
                var before = GC.GetTotalMemory(true);
                action();
                var after = GC.GetTotalMemory(false);

                totalSampledBytes += Math.Max(0L, after - before);
            }

            var perCall = (double)totalSampledBytes / sampleCount;
            return (perCall * count, perCall);
        }

        /// <summary>アロケーション有無を Debug.Log に出力するだけ（情報収集用）</summary>
        private static void LogAllocStatus(Action action, string label)
        {
            action(); action();
            var constraint = new AllocatingGCMemoryConstraint();
            bool allocates = constraint.ApplyTo((TestDelegate)(() => action())).IsSuccess;
            Debug.Log($"{label}: GCAlloc={(allocates ? "YES" : "NO")}");
        }

        private static string FormatBytes(double bytes)
        {
            if (bytes < 1024) return $"{bytes:F1} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F2} KB";
            return $"{bytes / (1024 * 1024):F2} MB";
        }

        private static void LogAlloc(string label, double totalBytes, double bytesPerCall, int count = DefaultCount)
            => Debug.Log($"{label}: {FormatBytes(bytesPerCall)}/call  (total {FormatBytes(totalBytes)} / {count} calls)");

        // -------------------------------------------------------
        // スコープなし：Log.Debug → GCAlloc なし（保証）
        // -------------------------------------------------------

        [Test]
        public void Alloc_NoScope_NoProperty()
        {
            var (total, perCall) = MeasureAllocBytes(() => Log.Debug("message"));
            LogAlloc("Log.Debug / スコープなし", total, perCall);
            AssertNoGCAlloc(() => Log.Debug("message"), "Log.Debug / スコープなし");
        }

        // -------------------------------------------------------
        // BeginScope / Dispose → GCAlloc あり（仕様・保証）
        //
        // AsyncLocal<T>.Value への書き込みが ExecutionContext コピーを発生させる。
        // async/await 伝播を維持する限り回避不可能。
        // Microsoft.Extensions.Logging / Serilog 等も同様に allocate する。
        // -------------------------------------------------------

        [Test]
        public void Alloc_BeginScope_AllocatesAsExpected()
        {
            var (total, perCall) = MeasureAllocBytes(() => { using (Log.BeginScope("Scope")) { } });
            LogAlloc("BeginScope + Dispose のみ", total, perCall);
            AssertAllocatesGCMemory(() => { using (Log.BeginScope("Scope")) { } }, "BeginScope + Dispose のみ");
        }

        [Test]
        public void Alloc_OneScope_NoProperty_AllocatesAsExpected()
        {
            var (total, perCall) = MeasureAllocBytes(
                () => { using (Log.BeginScope("Scope")) Log.Debug("message"); });
            LogAlloc("OneScope / NoProp（全体）", total, perCall);
            AssertAllocatesGCMemory(
                () => { using (Log.BeginScope("Scope")) Log.Debug("message"); },
                "OneScope / NoProp（全体）");
        }

        [Test]
        public void Alloc_OneScope_OneProperty_AllocatesAsExpected()
        {
            var (total, perCall) = MeasureAllocBytes(
                () => { using (Log.BeginScope("Scope").SetProperty("env", "prod")) Log.Debug("message"); });
            LogAlloc("OneScope / OneProp（全体）", total, perCall);
            AssertAllocatesGCMemory(
                () => { using (Log.BeginScope("Scope").SetProperty("env", "prod")) Log.Debug("message"); },
                "OneScope / OneProp（全体）");
        }

        [Test]
        public void Alloc_OneScope_ThreeProperties_AllocatesAsExpected()
        {
            Action action = () =>
            {
                using (Log.BeginScope("Scope")
                           .SetProperty("env", "prod")
                           .SetProperty("region", "jp")
                           .SetProperty("version", "1.0"))
                    Log.Debug("message");
            };
            var (total, perCall) = MeasureAllocBytes(action);
            LogAlloc("OneScope / ThreeProps（全体）", total, perCall);
            AssertAllocatesGCMemory(action, "OneScope / ThreeProps（全体）");
        }

        [Test]
        public void Alloc_NestedScope_AllocatesAsExpected()
        {
            Action action = () =>
            {
                using (Log.BeginScope("Outer").SetProperty("env", "prod"))
                using (Log.BeginScope("Inner").SetProperty("userId", "123"))
                    Log.Debug("message");
            };
            var (total, perCall) = MeasureAllocBytes(action);
            LogAlloc("NestedScope（全体）", total, perCall);
            AssertAllocatesGCMemory(action, "NestedScope（全体）");
        }

        // -------------------------------------------------------
        // スコープ操作単体のアロケーション量（Log.Debug なし）
        //
        // BeginScope ごとに何バイト消費するかを確認する。
        // プロパティ数・ネスト深さによる増加量の把握が目的。
        // -------------------------------------------------------

        [Test]
        public void Alloc_ScopeOnly_NoProperty()
        {
            var (total, perCall) = MeasureAllocBytes(() => { using (Log.BeginScope("Scope")) { } });
            LogAlloc("BeginScope+Dispose / プロパティなし", total, perCall);
            AssertAllocatesGCMemory(() => { using (Log.BeginScope("Scope")) { } }, "BeginScope+Dispose / プロパティなし");
        }

        [Test]
        public void Alloc_ScopeOnly_OneProperty()
        {
            var (total, perCall) = MeasureAllocBytes(
                () => { using (Log.BeginScope("Scope").SetProperty("env", "prod")) { } });
            LogAlloc("BeginScope+Dispose / プロパティ1つ", total, perCall);
            AssertAllocatesGCMemory(
                () => { using (Log.BeginScope("Scope").SetProperty("env", "prod")) { } },
                "BeginScope+Dispose / プロパティ1つ");
        }

        [Test]
        public void Alloc_ScopeOnly_ThreeProperties()
        {
            Action action = () =>
            {
                using (Log.BeginScope("Scope")
                           .SetProperty("env", "prod")
                           .SetProperty("region", "jp")
                           .SetProperty("version", "1.0"))
                { }
            };
            var (total, perCall) = MeasureAllocBytes(action);
            LogAlloc("BeginScope+Dispose / プロパティ3つ", total, perCall);
            AssertAllocatesGCMemory(action, "BeginScope+Dispose / プロパティ3つ");
        }

        [Test]
        public void Alloc_ScopeOnly_Nested2_NoProperty()
        {
            Action action = () =>
            {
                using (Log.BeginScope("Outer"))
                using (Log.BeginScope("Inner"))
                { }
            };
            var (total, perCall) = MeasureAllocBytes(action);
            LogAlloc("BeginScope×2ネスト+Dispose / プロパティなし", total, perCall);
            AssertAllocatesGCMemory(action, "BeginScope×2ネスト+Dispose / プロパティなし");
        }

        [Test]
        public void Alloc_ScopeOnly_Nested2_WithProperties()
        {
            void Act()
            {
                using var outer = Log.BeginScope("Outer").SetProperty("env", "prod").SetProperty("region", "jp");
                using var inner = Log.BeginScope("Inner").SetProperty("userId", "123").SetProperty("action", "login");
            }

            Action action = Act;
            var (total, perCall) = MeasureAllocBytes(action);
            LogAlloc("BeginScope×2ネスト+Dispose / 各プロパティ2つ", total, perCall);
            AssertAllocatesGCMemory(action, "BeginScope×2ネスト+Dispose / 各プロパティ2つ");
        }

        /// <summary>
        /// 全スコープ構成のアロケーション量をまとめて出力する比較用テスト
        /// </summary>
        [Test]
        public void Info_ScopeAllocComparison()
        {
            void LogScopeAllocComparison(string label, Action action)
            {
                var (total, perCall) = MeasureAllocBytes(action);
                Debug.Log($"[Scope alloc] {label}: {FormatBytes(perCall)}/scope  (total {FormatBytes(total)} / {DefaultCount} calls)");
            }

            LogScopeAllocComparison("NoProperty", () =>
            {
                using var scope = Log.BeginScope("S");
            });

            LogScopeAllocComparison("1 Property", () =>
            {
                using var scope = Log.BeginScope("S").SetProperty("k", "v");
            });

            LogScopeAllocComparison("3 Properties", () =>
            {
                using var scope = Log.BeginScope("S").SetProperty("k1", "v").SetProperty("k2", "v").SetProperty("k3", "v");
            });

            LogScopeAllocComparison("Nested×2 NoProperty", () =>
            {
                using var outer = Log.BeginScope("O");
                using var inner = Log.BeginScope("I");
            });

            LogScopeAllocComparison("Nested×2 + 2Props each", () =>
            {
                using var outer = Log.BeginScope("O").SetProperty("k1", "v").SetProperty("k2", "v");
                using var inner = Log.BeginScope("I").SetProperty("k3", "v").SetProperty("k4", "v");
            });
        }

        // -------------------------------------------------------
        // スコープ確立後の Log.Debug 単体 → GCAlloc なし（保証）
        //
        // BeginScope をループ外・測定外で行い、Log.Debug のみを計測する。
        // ゲームループ内で問題になるのはここのアロケーション。
        // -------------------------------------------------------

        [Test]
        public void Alloc_LogDebug_InScope_NoProperty_NoAlloc()
        {
            using var scope = Log.BeginScope("Scope");
            var (total, perCall) = MeasureAllocBytes(() => Log.Debug("message"));
            LogAlloc("Log.Debug / スコープ確立済み / プロパティなし", total, perCall);
            AssertNoGCAlloc(() => Log.Debug("message"), "Log.Debug / スコープ確立済み / プロパティなし");
        }

        [Test]
        public void Alloc_LogDebug_InScope_WithProperty_NoAlloc()
        {
            using var scope = Log.BeginScope("Scope").SetProperty("env", "prod");
            var (total, perCall) = MeasureAllocBytes(() => Log.Debug("message"));
            LogAlloc("Log.Debug / スコープ確立済み / プロパティあり", total, perCall);
            AssertNoGCAlloc(() => Log.Debug("message"), "Log.Debug / スコープ確立済み / プロパティあり");
        }

        [Test]
        public void Alloc_LogDebug_InNestedScope_NoAlloc()
        {
            using var outer = Log.BeginScope("Outer").SetProperty("env", "prod");
            using var inner = Log.BeginScope("Inner").SetProperty("userId", "123");
            var (total, perCall) = MeasureAllocBytes(() => Log.Debug("message"));
            LogAlloc("Log.Debug / ネストスコープ確立済み", total, perCall);
            AssertNoGCAlloc(() => Log.Debug("message"), "Log.Debug / ネストスコープ確立済み");
        }

        // -------------------------------------------------------
        // NativeText（Burst/Jobs 用）メッセージ → GCAlloc なし（保証）
        // NativeText は Allocator.Temp = アンマネージドメモリ、GC 対象外
        // -------------------------------------------------------

        private static float[] CreateSequentialFloats(int count)
        {
            var values = new float[count];
            for (var i = 0; i < count; i++) values[i] = i * 0.1f;
            return values;
        }

        private static NativeText BuildFloatCsv(float[] values)
        {
            if (values.Length == 0) return new NativeText(0, Allocator.Temp);
            const int estimatedCharsPerFloat = 16;
            var text = new NativeText(values.Length * estimatedCharsPerFloat, Allocator.Temp);
            for (var i = 0; i < values.Length; i++)
            {
                if (i > 0) text.Append(',');
                text.Append(values[i]);
            }
            return text;
        }

        [Test]
        public void Alloc_LogDebug_NativeText_100_NoScope()
        {
            var values = CreateSequentialFloats(100);
            Action action = () => { using var msg = BuildFloatCsv(values); Log.Debug(msg); };
            var (total, perCall) = MeasureAllocBytes(action, count: 500);
            LogAlloc("Log.Debug(NativeText x100) / スコープなし", total, perCall, count: 500);
            AssertNoGCAlloc(action, "Log.Debug(NativeText x100) / スコープなし");
        }

        [Test]
        public void Alloc_LogDebug_NativeText_1000_NoScope()
        {
            var values = CreateSequentialFloats(1000);
            Action action = () => { using var msg = BuildFloatCsv(values); Log.Debug(msg); };
            var (total, perCall) = MeasureAllocBytes(action, count: 200);
            LogAlloc("Log.Debug(NativeText x1000) / スコープなし", total, perCall, count: 200);
            AssertNoGCAlloc(action, "Log.Debug(NativeText x1000) / スコープなし");
        }

        [Test]
        public void Alloc_LogDebug_NativeText_100_InScope()
        {
            var values = CreateSequentialFloats(100);
            using var scope = Log.BeginScope("Scope").SetProperty("env", "prod");
            Action action = () => { using var msg = BuildFloatCsv(values); Log.Debug(msg); };
            var (total, perCall) = MeasureAllocBytes(action, count: 500);
            LogAlloc("Log.Debug(NativeText x100) / スコープ確立済み", total, perCall, count: 500);
            AssertNoGCAlloc(action, "Log.Debug(NativeText x100) / スコープ確立済み");
        }

        [Test]
        public void Alloc_LogDebug_NativeText_1000_InScope()
        {
            var values = CreateSequentialFloats(1000);
            using var scope = Log.BeginScope("Scope").SetProperty("env", "prod");
            Action action = () => { using var msg = BuildFloatCsv(values); Log.Debug(msg); };
            var (total, perCall) = MeasureAllocBytes(action, count: 200);
            LogAlloc("Log.Debug(NativeText x1000) / スコープ確立済み", total, perCall, count: 200);
            AssertNoGCAlloc(action, "Log.Debug(NativeText x1000) / スコープ確立済み");
        }

        // -------------------------------------------------------
        // 参考情報：各シナリオのアロケーション有無を一覧ログ出力
        // -------------------------------------------------------

        [Test]
        public void Info_AllocSummary()
        {
            LogAllocStatus(() => Log.Debug("message"),
                "Log.Debug / NoScope");
            LogAllocStatus(() => { using (Log.BeginScope("S")) { } },
                "BeginScope + Dispose のみ");
            LogAllocStatus(() => { using (Log.BeginScope("S").SetProperty("k", "v")) Log.Debug("message"); },
                "BeginScope + SetProperty + Log.Debug（全体）");

            using var scope = Log.BeginScope("S").SetProperty("k", "v");
            LogAllocStatus(() => Log.Debug("message"),
                "Log.Debug / スコープ確立済み");
        }
    }
}

