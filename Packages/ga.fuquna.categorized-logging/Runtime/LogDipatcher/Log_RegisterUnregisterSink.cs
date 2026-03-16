namespace CategorizedLogging
{
    public static partial class Log
    {
        public static void RegisterSink(ISink sink, LogFilter filter)
        {
            LogDispatcher?.Register(sink, filter);
        }
        
        public static void UnregisterSink(ISink sink)
        {
            LogDispatcher?.Unregister(sink);
        }


        public static void RegisterAsyncLocalSink(ISink sink, LogFilter filter)
        {
            AsyncLocalLogDispatcher ??= new LogDispatcher();
            AsyncLocalLogDispatcher.Register(sink, filter);
        }
        
        public static void UnregisterAsyncLocalSink(ISink sink)
        {
            AsyncLocalLogDispatcher?.Unregister(sink);
        }
    }
}