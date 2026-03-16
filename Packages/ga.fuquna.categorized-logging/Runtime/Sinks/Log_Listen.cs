using System;
using JetBrains.Annotations;

namespace CategorizedLogging
{
    public static partial class Log
    {
        /// <summary>
        /// Starts listening to log messages on the current thread with the specified log level filter.
        /// </summary>
        [MustDisposeResource]
        public static AsyncLocalSinkScope Listen(LogLevel logLevel, Action<string> logMessageCallback)
        {
            return new AsyncLocalSinkScope(new ListenerSink(logMessageCallback), logLevel);
        }

        /// <summary>
        /// Starts listening to log messages on the current thread with the specified log level filter.
        /// </summary>
        [MustDisposeResource]
        public static AsyncLocalSinkScope Listen(LogLevel logLevel, Action<LogRecord> logCallback)
        {
            return new AsyncLocalSinkScope(new ListenerSink(logCallback), logLevel);
        }
    }
}