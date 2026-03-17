using System;
using NUnit.Framework;

namespace CategorizedLogging.Test.Editor
{
    public class TestLog_Output
    {
        // -------------------------------------------------------
        // Log.Trace/Debug/Warning/Error() のカテゴリ名テスト（直接呼び出し）
        // -------------------------------------------------------
        // [Test]
        // public void LogTrace_Direct_EmitsCorrectCategory()
        // {
        //     LogRecord captured = null;
        //     using (Log.Listen(LogLevel.Trace, e => captured = e))
        //     {
        //         Log.Trace("trace message");
        //     }
        //
        //     Assert.IsNotNull(captured);
        //     Assert.AreEqual(nameof(TestLog_Output), captured.Category);
        //     Assert.AreEqual(LogLevel.Trace, captured.LogLevel);
        // }
        //
        // [Test]
        // public void LogDebug_Direct_EmitsCorrectCategory()
        // {
        //     LogRecord captured = null;
        //     using (Log.Listen(LogLevel.Debug, e => captured = e))
        //     {
        //         Log.Debug("debug message");
        //     }
        //
        //     Assert.IsNotNull(captured);
        //     Assert.AreEqual(nameof(TestLog_Output), captured.Category);
        //     Assert.AreEqual(LogLevel.Debug, captured.LogLevel);
        // }
        //
        // [Test]
        // public void LogWarning_Direct_EmitsCorrectCategory()
        // {
        //     LogRecord captured = null;
        //     using (Log.Listen(LogLevel.Warning, e => captured = e))
        //     {
        //         Log.Warning("warning message");
        //     }
        //
        //     Assert.IsNotNull(captured);
        //     Assert.AreEqual(nameof(TestLog_Output), captured.Category);
        //     Assert.AreEqual(LogLevel.Warning, captured.LogLevel);
        // }
        //
        // [Test]
        // public void LogError_Direct_EmitsCorrectCategory()
        // {
        //     LogRecord captured = null;
        //     using (Log.Listen(LogLevel.Error, e => captured = e))
        //     {
        //         Log.Error("error message");
        //     }
        //
        //     Assert.IsNotNull(captured);
        //     Assert.AreEqual(nameof(TestLog_Output), captured.Category);
        //     Assert.AreEqual(LogLevel.Error, captured.LogLevel);
        // }

        [Test]
        public void LogTrace_EmitsCorrectLevel()
        {
            LogRecord captured = null;
            using (Log.Listen(LogLevel.Trace, e => captured = e))
            {
                Log.Trace("trace message");
            }

            Assert.IsNotNull(captured);
            Assert.AreEqual(LogLevel.Trace, captured.LogLevel);
            Assert.AreEqual("trace message", captured.Message);
        }

        [Test]
        public void LogDebug_EmitsCorrectLevel()
        {
            LogRecord captured = null;
            using (Log.Listen(LogLevel.Trace, e => captured = e))
            {
                Log.Debug("debug message");
            }

            Assert.IsNotNull(captured);
            Assert.AreEqual(LogLevel.Debug, captured.LogLevel);
            Assert.AreEqual("debug message", captured.Message);
        }

        [Test]
        public void LogInformation_EmitsCorrectLevel()
        {
            LogRecord captured = null;
            using (Log.Listen(LogLevel.Trace, e => captured = e))
            {
                Log.Information("information message");
            }

            Assert.IsNotNull(captured);
            Assert.AreEqual(LogLevel.Information, captured.LogLevel);
            Assert.AreEqual("information message", captured.Message);
        }

        [Test]
        public void LogWarning_EmitsCorrectLevel()
        {
            LogRecord captured = null;
            using (Log.Listen(LogLevel.Trace, e => captured = e))
            {
                Log.Warning("warning message");
            }

            Assert.IsNotNull(captured);
            Assert.AreEqual(LogLevel.Warning, captured.LogLevel);
            Assert.AreEqual("warning message", captured.Message);
        }

        [Test]
        public void LogError_EmitsCorrectLevel()
        {
            LogRecord captured = null;
            using (Log.Listen(LogLevel.Trace, e => captured = e))
            {
                Log.Error("error message");
            }

            Assert.IsNotNull(captured);
            Assert.AreEqual(LogLevel.Error, captured.LogLevel);
            Assert.AreEqual("error message", captured.Message);
        }

        [Test]
        public void LogFatal_EmitsCorrectLevel()
        {
            LogRecord captured = null;
            using (Log.Listen(LogLevel.Trace, e => captured = e))
            {
                Log.Fatal("fatal message");
            }

            Assert.IsNotNull(captured);
            Assert.AreEqual(LogLevel.Fatal, captured.LogLevel);
            Assert.AreEqual("fatal message", captured.Message);
        }

        [Test]
        public void Listen_BelowMinLevel_DoesNotCapture()
        {
            LogRecord captured = null;
            using (Log.Listen(LogLevel.Warning, e => captured = e))
            {
                Log.Debug("should not be captured");
            }

            Assert.IsNull(captured);
        }

        [Test]
        public void LogRecord_HasTimestamp()
        {
            LogRecord captured = null;
            using (Log.Listen(LogLevel.Trace, e => captured = e))
            {
                Log.Debug("timestamp test");
            }

            Assert.IsNotNull(captured);
            Assert.AreNotEqual(default(DateTime), captured.Timestamp);
        }
    }
}
