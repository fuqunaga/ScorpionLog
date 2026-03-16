using System;
using CategorizedLogging.Scope;

namespace CategorizedLogging
{
    public record LogRecord
    {
        public DateTime Timestamp { get; }
        public LogLevel LogLevel { get; }
        public string Message { get; }
        public CallerInformation CallerInfo { get; }
        public LogScopeRecord Scope { get;  }
        

        public LogRecord(LogLevel logLevel, string message, in CallerInformation callerInfoInformation, LogScopeRecord scope = null)
        {
            Timestamp = DateTime.Now;
            LogLevel = logLevel;
            Message = message;
            CallerInfo = callerInfoInformation;
            Scope = scope ?? LogScopeRecord.Current;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}][{LogLevel}] {Message}";
        }
    }
}