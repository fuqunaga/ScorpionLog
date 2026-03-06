using System;

namespace CategorizedLogging
{
    /// <summary>
    /// ログを受け取り先をデリゲートで指定するシンク
    /// </summary>
    public class ListenerSink : ISink
    {
        public delegate void LogCallback(in LogEntry logEntry);
        
        private readonly LogCallback _callback;
        
        public ListenerSink(LogCallback　callback)
        {
            _callback = callback;
        }

        public ListenerSink(Action<string>　callback)
        {
            _callback = CallbackWrapper;
            return;
            
            void CallbackWrapper(in LogEntry logEntry)
            {
                callback(logEntry.Message);
            }
        }
        
        public void Log(in LogEntry logEntry)
        {
            _callback(in logEntry);
        }
    }


    public static class LoggerExtensionsForListenerSink
    {
        public static ILogger AddListener(this ILogger logger, ListenerSink.LogCallback callback)
        {
            return logger.AddSink(new ListenerSink(callback));
        }

        public static ILogger AddListener(this ILogger logger, Action<string> callback)
        {
            return logger.AddSink(new ListenerSink(callback));
        }
    }
}