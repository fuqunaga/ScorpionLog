using System.Collections.Generic;

namespace CategorizedLogging
{
    public static partial class Log
    {
        public static void RegisterSink(ISink sink, SinkFilterConfig filterConfig)
        {
            RegisterSink(sink, filterConfig.categoryLogLevels);
        }

        public static void RegisterSink(ISink sink, IEnumerable<CategoryMinimumLogLevel> categoryLogLevels)
        {
            LogDispatcher?.Register(sink, categoryLogLevels);
        }

        public static void RegisterSink(ISink sink, string category, LogLevel logLevel)
        {
            LogDispatcher?.Register(sink, category, logLevel);
        }

        public static void UnregisterSink(ISink sink)
        {
            LogDispatcher?.Unregister(sink);
        }


        public static void RegisterThreadLocalSink(ISink sink, SinkFilterConfig filterConfig)
        {
            ThreadLocalDispatcher ??= new LogDispatcher();
            LogDispatcher?.Register(sink, filterConfig.categoryLogLevels);
        }

        public static void RegisterThreadLocalSink(ISink sink, IEnumerable<CategoryMinimumLogLevel> categoryLogLevels)
        {
            ThreadLocalDispatcher ??= new LogDispatcher();
            LogDispatcher?.Register(sink, categoryLogLevels);
        }

        public static void RegisterThreadLocalSink(ISink sink, string category, LogLevel logLevel)
        {
            ThreadLocalDispatcher ??= new LogDispatcher();
            LogDispatcher?.Register(sink, category, logLevel);
        }

        public static void UnregisterThreadLocalSink(ISink sink)
        {
            ThreadLocalDispatcher?.Unregister(sink);
        }
    }
}