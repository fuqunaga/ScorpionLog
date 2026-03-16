using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CategorizedLogging
{
    /// <summary>
    /// アプリケーション内で管理するログ
    /// </summary>
    [Serializable]
    public class MemorySink : ISink
    {
        private readonly ConcurrentQueue<LogRecord> _logEntries = new();
        
        // 別スレッドから呼ばれるので注意
        public event Action onLogEntryAddedMultiThreaded;

        
        public int Capacity { get; set; } = 1000;
        
        public IEnumerable<LogRecord> LogEntries => _logEntries;


        public void Log(LogRecord logRecord)
        {
            _logEntries.Enqueue(logRecord);
            
            // 古いログを削除
            // たぶんO(n)なのでパフォーマンスが気になったら別の方法を検討する
            while (_logEntries.Count > Capacity)
            {
                _logEntries.TryDequeue(out _);
            }
            
            onLogEntryAddedMultiThreaded?.Invoke();
        }

        public void Clear()
        {
            _logEntries.Clear();
        }
    }
}