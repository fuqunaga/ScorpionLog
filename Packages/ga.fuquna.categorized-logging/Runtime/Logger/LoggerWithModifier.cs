using UnityEngine;

namespace CategorizedLogging
{
    public class LoggerWithModifier : ILogger
    {
        private readonly ILogger _logger;
        private readonly ILogModifier _modifier;
        
        
        public LoggerWithModifier(ILogger logger, ILogModifier modifier)
        {
            _logger = logger;
            _modifier = modifier;
        }
        
        [HideInCallstack]
        public LogEntry CreateLogEntry(LogLevel logLevel, string message)
        {
            var originalLogEntry = _logger.CreateLogEntry(logLevel, message);
            return _modifier.Modify(originalLogEntry);
        }
    }
    
    public static class LoggerExtensionsForLogModifier
    {
        public static ILogger AddModifier(this ILogger logger, ILogModifier modifier)
        {
            return new LoggerWithModifier(logger, modifier);
        }
    }
}