using UnityEngine;

namespace CategorizedLogging
{
    /// <summary>
    /// 呼び出し側でカテゴリを指定せずにログを出力するためのインターフェース
    /// </summary>
    public interface ILogger
    {
        void EmitLog(LogLevel logLevel, string message);
        void EmitLog(in LogEntry logEntry) => Log.EmitLog(logEntry);
    }
    
    public static class LoggerExtensions
    {
        [HideInCallstack]
        public static void Trace<TLogger>(this TLogger logger, string message) where TLogger : ILogger 
            => logger.EmitLog(LogLevel.Trace, message);

        [HideInCallstack]
        public static void Debug<TLogger>(this TLogger logger, string message) where TLogger : ILogger 
            => logger.EmitLog(LogLevel.Debug, message);

        [HideInCallstack]
        public static void Information<TLogger>(this TLogger logger, string message) where TLogger : ILogger 
            => logger.EmitLog(LogLevel.Information, message);

        [HideInCallstack]
        public static void Warning<TLogger>(this TLogger logger, string message) where TLogger : ILogger 
            => logger.EmitLog(LogLevel.Warning, message);

        [HideInCallstack]
        public static void Error<TLogger>(this TLogger logger, string message) where TLogger : ILogger 
            => logger.EmitLog(LogLevel.Error, message);

        [HideInCallstack]
        public static void Critical<TLogger>(this TLogger logger, string message) where TLogger : ILogger 
            => logger.EmitLog(LogLevel.Critical, message);
    }
}