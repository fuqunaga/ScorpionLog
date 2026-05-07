using System;
using NUnit.Framework;
using ScotchLog.Scope;

namespace ScotchLog.Test.Editor
{
    public class TestLogEntry
    {
        private static CallerInformation MakeCaller(string file = "TestFile.cs", int line = 1, string member = "TestMethod")
            => new CallerInformation(file, line, member);

        // ─── Rent / Return / Pool ─────────────────────────────────────────────

        [Test]
        public void Rent_ReturnsNonDisposedEntry()
        {
            var entry = LogEntry.Rent(LogLevel.Debug, "hello", MakeCaller());

            Assert.That(entry.IsDisposed, Is.False);

            entry.Dispose();
        }

        [Test]
        public void Rent_SetsLogLevel()
        {
            var entry = LogEntry.Rent(LogLevel.Warning, "msg", MakeCaller());

            Assert.That(entry.LogLevel, Is.EqualTo(LogLevel.Warning));

            entry.Dispose();
        }

        [Test]
        public void Rent_SetsMessage()
        {
            var entry = LogEntry.Rent(LogLevel.Information, "test message", MakeCaller());

            Assert.That(entry.Message, Is.EqualTo("test message"));

            entry.Dispose();
        }

        [Test]
        public void Rent_SetsCallerInformation()
        {
            var caller = MakeCaller("MyFile.cs", 42, "MyMethod");
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", caller);

            Assert.That(entry.CallerInfo.FileName, Is.EqualTo("MyFile.cs"));
            Assert.That(entry.CallerInfo.LineNumber, Is.EqualTo(42));
            Assert.That(entry.CallerInfo.MemberName, Is.EqualTo("MyMethod"));

            entry.Dispose();
        }

        [Test]
        public void Rent_SetsTimestamp()
        {
            var before = DateTime.Now;
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", MakeCaller());
            var after = DateTime.Now;

            Assert.That(entry.Timestamp, Is.GreaterThanOrEqualTo(before));
            Assert.That(entry.Timestamp, Is.LessThanOrEqualTo(after));

            entry.Dispose();
        }

        [Test]
        public void Rent_SetsCurrentScope()
        {
            using var scope = Log.BeginScope("rentScopeTest");
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", MakeCaller());

            Assert.That(entry.Scope, Is.Not.Null);
            Assert.That(entry.Scope.Name, Is.EqualTo("rentScopeTest"));

            entry.Dispose();
        }

        // ─── Dispose (Return) ─────────────────────────────────────────────────

        [Test]
        public void Dispose_MarksEntryAsDisposed()
        {
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", MakeCaller());
            entry.Dispose();

            Assert.That(entry.IsDisposed, Is.True);
        }

        [Test]
        public void LogLevel_AfterDispose_ThrowsObjectDisposedException()
        {
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", MakeCaller());
            entry.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { var _ = entry.LogLevel; });
        }

        [Test]
        public void Message_AfterDispose_ThrowsObjectDisposedException()
        {
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", MakeCaller());
            entry.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { var _ = entry.Message; });
        }

        [Test]
        public void Timestamp_AfterDispose_ThrowsObjectDisposedException()
        {
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", MakeCaller());
            entry.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { var _ = entry.Timestamp; });
        }

        [Test]
        public void CallerInfo_AfterDispose_ThrowsObjectDisposedException()
        {
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", MakeCaller());
            entry.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { var _ = entry.CallerInfo; });
        }

        [Test]
        public void Return_NullEntry_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => LogEntry.Return(null));
        }

        [Test]
        public void Return_AlreadyDisposedEntry_DoesNotThrow()
        {
            var entry = LogEntry.Rent(LogLevel.Debug, "msg", MakeCaller());
            entry.Dispose();

            Assert.DoesNotThrow(() => LogEntry.Return(entry));
        }

        // ─── Set ─────────────────────────────────────────────────────────────

        [Test]
        public void Set_UpdatesLogLevel()
        {
            var entry = new LogEntry();
            entry.Set(LogLevel.Error, "msg", MakeCaller());

            Assert.That(entry.LogLevel, Is.EqualTo(LogLevel.Error));
        }

        [Test]
        public void Set_UpdatesMessage()
        {
            var entry = new LogEntry();
            entry.Set(LogLevel.Debug, "updated message", MakeCaller());

            Assert.That(entry.Message, Is.EqualTo("updated message"));
        }

        [Test]
        public void Set_UpdatesCallerInformation()
        {
            var entry = new LogEntry();
            entry.Set(LogLevel.Debug, "msg", MakeCaller("Source.cs", 100, "Run"));

            Assert.That(entry.CallerInfo.LineNumber, Is.EqualTo(100));
            Assert.That(entry.CallerInfo.MemberName, Is.EqualTo("Run"));
        }

        [Test]
        public void Set_WithExplicitScope_UsesProvidedScope()
        {
            var scope = LogScopeRecord.Start("explicit");
            var entry = new LogEntry();
            entry.Set(LogLevel.Debug, "msg", MakeCaller(), scope);

            Assert.That(entry.Scope.Name, Is.EqualTo("explicit"));

            scope.End();
        }

        // ─── CopyFrom ─────────────────────────────────────────────────────────

        [Test]
        public void CopyFrom_CopiesLogLevel()
        {
            var source = LogEntry.Rent(LogLevel.Fatal, "src", MakeCaller());
            var dest = new LogEntry();
            dest.CopyFrom(source);

            Assert.That(dest.LogLevel, Is.EqualTo(LogLevel.Fatal));

            source.Dispose();
        }

        [Test]
        public void CopyFrom_CopiesMessage()
        {
            var source = LogEntry.Rent(LogLevel.Debug, "source message", MakeCaller());
            var dest = new LogEntry();
            dest.CopyFrom(source);

            Assert.That(dest.Message, Is.EqualTo("source message"));

            source.Dispose();
        }

        [Test]
        public void CopyFrom_ClonesStringWrapper_IndependentOfSource()
        {
            var source = LogEntry.Rent(LogLevel.Debug, "independent message", MakeCaller());
            var dest = new LogEntry();
            dest.CopyFrom(source);

            // ソースを破棄してもコピー先のメッセージは読める
            source.Dispose();

            Assert.That(dest.Message, Is.EqualTo("independent message"));

            dest.Dispose(); // クローンした NativeText(TempJob) を解放
        }

        [Test]
        public void CopyFrom_NullSource_ThrowsArgumentNullException()
        {
            var dest = new LogEntry();
            Assert.Throws<ArgumentNullException>(() => dest.CopyFrom(null));
        }

        [Test]
        public void CopyFrom_SetsIsDisposedFalse()
        {
            var source = LogEntry.Rent(LogLevel.Debug, "msg", MakeCaller());
            var dest = new LogEntry();
            dest.CopyFrom(source);

            Assert.That(dest.IsDisposed, Is.False);

            source.Dispose();
        }

        // ─── ToString ─────────────────────────────────────────────────────────

        [Test]
        public void ToString_ContainsLogLevelAndMessage()
        {
            var entry = LogEntry.Rent(LogLevel.Warning, "hello world", MakeCaller());

            var str = entry.ToString();

            Assert.That(str, Does.Contain("Warning"));
            Assert.That(str, Does.Contain("hello world"));

            entry.Dispose();
        }
    }
}


