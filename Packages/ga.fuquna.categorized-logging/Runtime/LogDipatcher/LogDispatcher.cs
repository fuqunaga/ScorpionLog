using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CategorizedLogging
{
    public class LogDispatcher : ILogDispatcher
    {
        private readonly ThreadLocal<int> _threadRecursionDepth = new(() => 0);
        
        
        private readonly Dictionary<LogFilter, HashSet<ISink>> _filterToSinks = new();
        private readonly object _lockFilterToSinks = new();

#if UNITY_EDITOR
        public LogDispatcher()
        {
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    lock (_lockFilterToSinks)
                    {
                        _filterToSinks.Clear();
                    }
                }
            };
        }
#endif
        
        
        [HideInCallstack]
        public void Log(LogRecord logRecord)
        {
            _threadRecursionDepth.Value++;
            try
            {
                // 再帰呼び出し防止
                if (_threadRecursionDepth.Value > 1)
                {
                    return;
                }

                if (logRecord.LogLevel == LogLevel.None)
                {
                    return;
                }

                
                using var _ = HashSetPool<ISink>.Get(out var totalSinks);

                lock (_lockFilterToSinks)
                {
                    foreach(var (filter, sinks) in _filterToSinks)
                    {
                        if (filter.IsMatch(logRecord))
                        {
                            totalSinks.UnionWith(sinks);
                        }
                    }
                }

                foreach (var sink in totalSinks)
                {
                    sink.Log(logRecord);
                }
            }
            finally
            {
                _threadRecursionDepth.Value--;
            }
        }
        


        /// <summary>
        /// Sinkを登録する
        /// </summary>
        public void Register(ISink sink, LogFilter filter)
        {
            Unregister(sink);

            lock (_lockFilterToSinks)
            {
                if (_filterToSinks.TryGetValue(filter, out var sinks))
                {
                    sinks.Add(sink);
                    return;
                }
                
                _filterToSinks[filter] = new HashSet<ISink> { sink };
            }
        }
        
        public void Unregister(ISink sink)
        {
            lock (_lockFilterToSinks)
            {
                foreach (var sinks in _filterToSinks.Values)
                {
                    sinks.Remove(sink);
                }
            }
        }
    }
}