using System;
using System.Collections.Generic;
using System.Linq;

namespace CategorizedLogging
{
    /// <summary>
    /// ログのフィルタリング設定
    /// </summary>
    [Serializable]
    public class LogFilter
    {
        public static LogFilter Create(LogLevel logLevel)
        {
            var config = new LogFilter();
            config.filterParameters[0].minimumLogLevel = logLevel;
            return config;
        }


        public List<LogFilterParameter> filterParameters = new()
        {
            new LogFilterParameter()
            {
                scopeName = "*",
                minimumLogLevel = LogLevel.Information
            }
        };

        
        public LogFilter()
        {
        }
        
        public LogFilter(LogFilter other)
        {
            filterParameters = other.filterParameters.Select(p => new LogFilterParameter(p)).ToList();
        }

        public bool IsMatch(LogRecord logRecord)
        {
            return filterParameters.Any(filterParameter => filterParameter.IsMatch(logRecord));
        }
    }
}