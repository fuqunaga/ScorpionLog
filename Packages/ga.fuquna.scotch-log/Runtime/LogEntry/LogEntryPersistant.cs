using System;
using ScotchLog.Scope;
using Unity.Collections;

namespace ScotchLog;


/// <summary>
/// LogEntryの内容を長期保存するための構造体
/// </summary>
public readonly struct LogEntryPersistant : IDisposable
{
    public DateTime Timestamp { get; }
    public LogLevel LogLevel { get; }
    public StringWrapper StringWrapper { get; }
    public CallerInformation CallerInfo { get; }
    public LogScopeRecordHolder Scope { get; }

    
    public LogEntryPersistant(LogEntry logEntry)
    {
        Timestamp = logEntry.Timestamp;
        LogLevel = logEntry.LogLevel;
        StringWrapper = logEntry.StringWrapper.Clone(Allocator.Persistent);
        CallerInfo = logEntry.CallerInfo;
        Scope = logEntry.Scope.CreateHolder();
    }

    public void Dispose()
    {
        StringWrapper.Dispose();
        Scope.Dispose();
    }
}