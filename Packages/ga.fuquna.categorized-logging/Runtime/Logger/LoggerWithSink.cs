using UnityEngine;

namespace CategorizedLogging
{
    public class LoggerWithSink : ILogger
    {
        private readonly ILogger _logger;
        private readonly ISink _sink;
        
        public LoggerWithSink(ILogger logger, ISink sink)
        {
            _logger = logger;
            _sink = sink;
        }
        
        [HideInCallstack]
        public LogEntry CreateLogEntry(LogLevel logLevel, string message) => _logger.CreateLogEntry(logLevel, message);

        [HideInCallstack]
        public void EmitLog(in LogEntry logEntry)
        {
            _logger.EmitLog(logEntry);
            _sink.Log(logEntry);
        }
    }
    
    
    public static class LoggerExtensionsForSink
    {
        public static ILogger AddSink(this ILogger logger, ISink sink)
        {
            return new LoggerWithSink(logger, sink);
        }
    }
}