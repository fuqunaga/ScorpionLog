using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace CategorizedLogging
{
    public class UnityLoggerSink : ISink
    {
        public Func<LogRecord, string> LogEntryToMessageFormatter { get; set; } = logRecord => logRecord.ToString();
        
        public ConcurrentDictionary<LogLevel, LogType?> LogLevelToUnityLogTypeTable { get; } = new()
        {
            [LogLevel.Trace] = LogType.Log,
            [LogLevel.Debug] = LogType.Log,
            [LogLevel.Information] = LogType.Log,
            [LogLevel.Warning] = LogType.Warning,
            [LogLevel.Error] = LogType.Error,
            [LogLevel.Fatal] = LogType.Error,
            [LogLevel.None] = null,
        };

        
        [HideInCallstack]
        public void Log(LogRecord logRecord)
        {
            if (LogLevelToUnityLogTypeTable.TryGetValue(logRecord.LogLevel, out var unityLogType)
                && unityLogType is { } logType
               )
            {
                Debug.unityLogger.Log(logType, LogEntryToMessageFormatter(logRecord));
            }
        }
    }
}