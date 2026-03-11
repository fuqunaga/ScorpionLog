using System.Collections.Generic;

namespace CategorizedLogging
{
    public interface ILogDispatcher
    {
        void Log(in LogEntry logEntry);
        public void Register(ISink sink, IEnumerable<CategoryMinimumLogLevel> categoryLogLevels);
        public void Register(ISink sink, string category, LogLevel logLevel);
        public void Unregister(ISink sink);
    }
}