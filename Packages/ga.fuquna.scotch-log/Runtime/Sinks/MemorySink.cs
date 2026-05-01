using System;
using System.Collections.Generic;
using Unity.Collections;

namespace ScotchLog
{
    /// <summary>
    /// アプリケーション内で管理するログ
    /// </summary>
    [Serializable]
    public class MemorySink : ISink
    {
        private static readonly Action<LogEntry> DisposeLogEntry = entry => entry?.Dispose();

        private ConcurrentRingBuffer<LogEntry> _logEntries = new(1000, DisposeLogEntry);

        // 別スレッドから呼ばれるので注意
        public event Action onLogEntryAddedMultiThreaded;

        public int Capacity
        {
            get => _logEntries.Capacity;
            set => _logEntries.Capacity = value;
        }

        public IEnumerable<LogEntry> LogEntries => _logEntries;

        public void Log(LogEntry logEntry)
        {
            // StringWrapperをPersistantにしてコピー
            var copiedEntry = LogEntry.Rent(
                logEntry.LogLevel,
                logEntry.StringWrapper.Clone(Allocator.Persistent),
                logEntry.CallerInfo,
                logEntry.Scope
            );

            _logEntries.Add(copiedEntry);

            onLogEntryAddedMultiThreaded?.Invoke();
        }

        public void Clear()
        {
            _logEntries.Clear();
        }
    }
}