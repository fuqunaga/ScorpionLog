using System;
using NUnit.Framework;
using ScotchLog.Scope;

namespace ScotchLog.Test.Editor
{
    public class TestLogScope
    {
        // ─── Log.BeginScope ───────────────────────────────────────────────────

        [Test]
        public void BeginScope_SetsCurrentScopeName()
        {
            var scope = Log.BeginScope("myScope");

            Assert.That(LogScopeRecord.Current.Name, Is.EqualTo("myScope"));

            scope.Dispose();
        }

        [Test]
        public void BeginScope_EmptyName_SetsCurrentScope()
        {
            var scope = Log.BeginScope();

            Assert.That(LogScopeRecord.Current.Name, Is.EqualTo(""));

            scope.Dispose();
        }

        [Test]
        public void Dispose_RestoresPreviousScope()
        {
            var outer = Log.BeginScope("outer");
            var inner = Log.BeginScope("inner");

            Assert.That(LogScopeRecord.Current.Name, Is.EqualTo("inner"));

            inner.Dispose();

            Assert.That(LogScopeRecord.Current.Name, Is.EqualTo("outer"));

            outer.Dispose();
        }

        [Test]
        public void Dispose_RootScope_RestoredAfterDispose()
        {
            var rootBeforeTest = LogScopeRecord.Current;

            var scope = Log.BeginScope("temp");
            scope.Dispose();

            Assert.That(LogScopeRecord.Current, Is.SameAs(rootBeforeTest));
        }

        // ─── SetProperty ─────────────────────────────────────────────────────

        [Test]
        public void SetProperty_StoresPropertyInScope()
        {
            var scope = Log.BeginScope("propScope").SetProperty("key1", "val1");

            var props = LogScopeRecord.Current.Properties;
            Assert.That(props, Contains.Key("key1"));
            Assert.That(props["key1"], Is.EqualTo("val1"));

            scope.Dispose();
        }

        [Test]
        public void SetProperty_MultipleProperties_AllStored()
        {
            var scope = Log.BeginScope("multiProp")
                .SetProperty("env", "prod")
                .SetProperty("region", "ap-northeast-1");

            var props = LogScopeRecord.Current.Properties;
            Assert.That(props["env"], Is.EqualTo("prod"));
            Assert.That(props["region"], Is.EqualTo("ap-northeast-1"));

            scope.Dispose();
        }

        [Test]
        public void SetProperty_OverwritesExistingKey()
        {
            var scope = Log.BeginScope("overwrite")
                .SetProperty("key", "first")
                .SetProperty("key", "second");

            Assert.That(LogScopeRecord.Current.Properties["key"], Is.EqualTo("second"));

            scope.Dispose();
        }

        [Test]
        public void SetProperty_NonStringValue_StoredAsToString()
        {
            var scope = Log.BeginScope("intVal").SetProperty("count", 42);

            Assert.That(LogScopeRecord.Current.Properties["count"], Is.EqualTo("42"));

            scope.Dispose();
        }

        // ─── Log エントリにスコープが付与されること ────────────────────────────
        // LogEntry はディスパッチ後にプールに返却（Dispose）されるため、
        // Scope などのプロパティはコールバック内で取り出す必要があります。

        [Test]
        public void BeginScope_LogEntry_ReceivesScopeName()
        {
            string capturedScopeName = null;

            using (Log.Listen(LogLevel.Trace, e => capturedScopeName = e.Scope?.Name))
            using (Log.BeginScope("captureScope"))
            {
                Log.Debug("in scope");
            }

            Assert.That(capturedScopeName, Is.EqualTo("captureScope"));
        }

        [Test]
        public void BeginScope_NestedScope_LogEntry_ReceivesInnerScopeName()
        {
            string capturedScopeName = null;

            using (Log.Listen(LogLevel.Trace, e => capturedScopeName = e.Scope?.Name))
            using (Log.BeginScope("outer"))
            using (Log.BeginScope("inner"))
            {
                Log.Debug("nested");
            }

            Assert.That(capturedScopeName, Is.EqualTo("inner"));
        }

        [Test]
        public void BeginScope_Properties_AppearInLogEntryScope()
        {
            string capturedValue = null;

            using (Log.Listen(LogLevel.Trace, e => capturedValue = e.Scope?.Properties?["svcName"]))
            using (Log.BeginScope("propsScope").SetProperty("svcName", "auth"))
            {
                Log.Debug("with props");
            }

            Assert.That(capturedValue, Is.EqualTo("auth"));
        }

        // ─── Log.BeginPropertyScope ───────────────────────────────────────────

        [Test]
        public void BeginPropertyScope_StoredAsProperty()
        {
            const string requestId = "req-123";
            string capturedValue = null;

            using (Log.Listen(LogLevel.Trace, e => capturedValue = e.Scope?.Properties?["requestId"]))
            using (Log.BeginPropertyScope(requestId))
            {
                Log.Debug("prop scope test");
            }

            Assert.That(capturedValue, Is.EqualTo("req-123"));
        }

        // ─── LogScopeRecord 直接テスト ─────────────────────────────────────────

        [Test]
        public void LogScopeRecord_Start_HasPositiveId()
        {
            var record = LogScopeRecord.Start("direct");

            Assert.That(record.Id, Is.GreaterThan(0));

            record.End();
        }

        [Test]
        public void LogScopeRecord_Start_SetsName()
        {
            var record = LogScopeRecord.Start("directName");

            Assert.That(record.Name, Is.EqualTo("directName"));

            record.End();
        }

        [Test]
        public void LogScopeRecord_Start_SetsStartTimeUtc()
        {
            var before = DateTime.UtcNow;
            var record = LogScopeRecord.Start("timeScope");
            var after = DateTime.UtcNow;

            Assert.That(record.StartTimeUtc, Is.GreaterThanOrEqualTo(before));
            Assert.That(record.StartTimeUtc, Is.LessThanOrEqualTo(after));

            record.End();
        }

        [Test]
        public void LogScopeRecord_End_SetsEndTimeUtc()
        {
            var record = LogScopeRecord.Start("endScope");

            // CreateHolder() で参照カウントを増やし、End() 後もプールに戻らないようにする。
            // （参照がなければ End() → Pool.Release() → Deactivate() → Id=-1 となり
            //   プロパティアクセスで "Scope is already closed." が投げられる）
            var holder = record.CreateHolder();

            var before = DateTime.UtcNow;
            record.End();
            var after = DateTime.UtcNow;

            // EndTimeUtc は非ゼロ、かつ End() 呼び出し前後に収まる
            Assert.That(record.EndTimeUtc, Is.GreaterThanOrEqualTo(before));
            Assert.That(record.EndTimeUtc, Is.LessThanOrEqualTo(after));

            holder.Dispose(); // 参照を解放 → ここでプールに戻る
        }

        // ─── HasEnded ─────────────────────────────────────────────────────────

        [Test]
        public void LogScopeRecord_HasEnded_IsFalseBeforeEnd()
        {
            var record = LogScopeRecord.Start("hasEndedFalse");

            Assert.That(record.HasEnded, Is.False);

            record.End();
        }

        [Test]
        public void LogScopeRecord_HasEnded_IsTrueAfterEnd()
        {
            var record = LogScopeRecord.Start("hasEndedTrue");
            var holder = record.CreateHolder();

            record.End();

            Assert.That(record.HasEnded, Is.True);

            holder.Dispose();
        }

        [Test]
        public void LogScopeRecord_End_CalledTwice_ThrowsInvalidOperationException()
        {
            var record = LogScopeRecord.Start("doubleEnd");
            record.End();

            Assert.Throws<InvalidOperationException>(() => record.End());
        }

        [Test]
        public void LogScopeRecord_SetProperty_AfterEnd_ThrowsInvalidOperationException()
        {
            var record = LogScopeRecord.Start("closedProp");
            record.End();

            Assert.Throws<InvalidOperationException>(() => record.SetProperty("k", "v"));
        }

        [Test]
        public void LogScopeRecord_IsRoot_ForCurrentWithNoActiveScope_IsTrue()
        {
            // スコープが一切ない状態で Current は RootScope
            Assert.That(LogScopeRecord.Current.IsRoot, Is.True);
        }

        [Test]
        public void LogScopeRecord_IsRoot_ForNamedScope_IsFalse()
        {
            var scope = Log.BeginScope("nonRoot");

            Assert.That(LogScopeRecord.Current.IsRoot, Is.False);

            scope.Dispose();
        }

        // ─── LogScopeRecordHolder ─────────────────────────────────────────────

        [Test]
        public void LogScopeRecordHolder_Record_ReturnsCreatingScope()
        {
            var scope = Log.BeginScope("holderScope");
            var record = LogScopeRecord.Current;
            var holder = record.CreateHolder();

            Assert.That(holder.Record, Is.SameAs(record));

            holder.Dispose();
            scope.Dispose();
        }

        [Test]
        public void LogScopeRecordHolder_Dispose_DoesNotThrow()
        {
            var scope = Log.BeginScope("holderDispose");
            var holder = LogScopeRecord.Current.CreateHolder();

            Assert.DoesNotThrow(() => holder.Dispose());

            scope.Dispose();
        }
    }
}




