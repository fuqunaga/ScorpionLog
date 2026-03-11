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
        public static ThreadLocalSinkScope Listen(LogLevel logLevel, Action<string> logMessageCallback)
        {
            return new ThreadLocalSinkScope(new ListenerSink(logMessageCallback), logLevel);
        }

        /// <summary>
        /// Starts listening to log messages on the current thread with the specified log level filter.
        /// </summary>
        [MustDisposeResource]
        public static ThreadLocalSinkScope Listen(LogLevel logLevel, ListenerSink.LogCallback logCallback)
        {
            return new ThreadLocalSinkScope(new ListenerSink(logCallback), logLevel);
        }
    }
}