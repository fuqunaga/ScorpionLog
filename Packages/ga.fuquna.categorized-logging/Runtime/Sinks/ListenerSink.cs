using System;

namespace CategorizedLogging
{
    /// <summary>
    /// A log sink that allows external code to listen to log messages via a callback.
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
}