namespace CategorizedLogging
{
    public interface ISink
    {
        /// <summary>
        /// Add a log entry
        /// must be thread-safe
        /// </summary>
        void Log(in LogEntry logEntry);
    }
}