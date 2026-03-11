using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CategorizedLogging
{
    public class LogDispatcher : ILogDispatcher
    {
        private readonly ThreadLocal<int> _threadRecursionDepth = new(() => 0);
        
        public static bool IsAnyCategory(string category) => category == "*";
        
        
        private readonly Dictionary<LogLevel, HashSet<ISink>> _anyCategoryLoggers = new();
        private readonly Dictionary<string, Dictionary<LogLevel, HashSet<ISink>>> _specificLoggers = new();
        private readonly Dictionary<string, Dictionary<LogLevel, HashSet<ISink>>> _cachedLoggerTable = new();
        private readonly object _lockLoggers = new();
        private readonly object _lockCache = new();
        private bool _needsCacheRefresh = false;
        


#if UNITY_EDITOR
        public LogDispatcher()
        {
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    lock (_lockLoggers)
                    {
                        _anyCategoryLoggers.Clear();
                        _specificLoggers.Clear();
                        _cachedLoggerTable.Clear();
                        _needsCacheRefresh = false;
                    }
                }
            };
        }
#endif
        
        [HideInCallstack]
        public void Log(in LogEntry logEntry)
        {
            _threadRecursionDepth.Value++;
            try
            {
                // 再帰呼び出し防止
                if (_threadRecursionDepth.Value > 1)
                {
                    return;
                }

                var category = logEntry.Category;
                var logLevel = logEntry.LogLevel;

                if (logLevel == LogLevel.None)
                {
                    return;
                }

                HashSet<ISink> loggers;

                lock (_lockCache)
                {
                    if (_needsCacheRefresh)
                    {
                        _needsCacheRefresh = false;
                        _cachedLoggerTable.Clear();
                    }

                    if (!_cachedLoggerTable.TryGetValue(category, out var logLevelTable))
                    {
                        logLevelTable = new Dictionary<LogLevel, HashSet<ISink>>();
                        _cachedLoggerTable[category] = logLevelTable;
                    }

                    if (!logLevelTable.TryGetValue(logLevel, out loggers))
                    {
                        loggers = CreateLoggerCache(category, logLevel);
                        logLevelTable[logLevel] = loggers;
                    }
                }

                foreach (var logger in loggers)
                {
                    logger.Log(logEntry);
                }
            }
            finally
            {
                _threadRecursionDepth.Value--;
            }
        }
        


        /// <summary>
        /// ILoggerを登録する
        /// categoryに"*"を指定すると全カテゴリに登録される
        /// ただし"*"以外のカテゴリに対して個別に登録されたログレベルのほうが優先される
        /// </summary>
        public void Register(ISink sink, IEnumerable<CategoryMinimumLogLevel> categoryLogLevels)
        {
            Unregister(sink);
            
            foreach (var categoryLogLevel in categoryLogLevels)
            {
                for(var level = categoryLogLevel.logLevel; level <= LogLevel.Critical; level++)
                {
                    Register(sink, categoryLogLevel.category, level);
                }
            }
        }
        
        
        [SuppressMessage("ReSharper", "ConvertIfStatementToSwitchStatement")]
        public void Register(ISink sink, string category, LogLevel logLevel)
        {
            var changed = false;

            Dictionary<LogLevel, HashSet<ISink>> logLevelTable = null;
            
            if (IsAnyCategory(category))
            {
                logLevelTable = _anyCategoryLoggers;
            }
            else
            {
                if (!_specificLoggers.TryGetValue(category, out logLevelTable))
                {
                    logLevelTable = new Dictionary<LogLevel, HashSet<ISink>>();
                    lock (_lockLoggers)
                    {
                        _specificLoggers[category] = logLevelTable;
                    }
                }
            }
            
            changed = SetLoggerToDictionary(logLevelTable, logLevel, sink);
            
            _needsCacheRefresh |= changed;
        }
        
        
        public void Unregister(ISink sink)
        {
            var changed = false;

            lock (_lockLoggers)
            {
                foreach (var logLevelTable in _anyCategoryLoggers.Values)
                {
                    changed |= logLevelTable.Remove(sink);
                }

                foreach (var logLevelTable in _specificLoggers.Values.SelectMany(categoryTable => categoryTable.Values))
                {
                    changed |= logLevelTable.Remove(sink);
                }
            }
            
            _needsCacheRefresh |= changed;
        }
        
        
        private bool SetLoggerToDictionary<TKey>(Dictionary<TKey, HashSet<ISink>> dictionary, TKey key, ISink sink)
        {
            lock (_lockLoggers)
            {
                if (!dictionary.TryGetValue(key, out var loggerSet))
                {
                    loggerSet = new HashSet<ISink>();
                    dictionary[key] = loggerSet;
                }

                return loggerSet.Add(sink);
            }
        }

        /// <summary>
        /// CategoryとLogLevelに基づいて対象のILoggerのキャッシュを作成する
        ///
        /// AnyCategoryで指定されていてもspecificLoggersで指定されているILoggerはspecificLoggersを優先する
        /// </summary>
        private HashSet<ISink> CreateLoggerCache(string category, LogLevel logLevel)
        {
            lock (_lockLoggers)
            {
                var categoryLoggers = _specificLoggers.GetValueOrDefault(category);
                var specificCategoryLoggers = categoryLoggers?
                                                  .SelectMany(table => table.Value)
                                                  .Distinct()
                                              ?? Enumerable.Empty<ISink>();
                
                var result = new HashSet<ISink>(
                    _anyCategoryLoggers.GetValueOrDefault(logLevel)
                    ?? Enumerable.Empty<ISink>()
                );
                result.ExceptWith(specificCategoryLoggers);
                
                if (categoryLoggers?.TryGetValue(logLevel, out var hashSet) ?? false)
                {
                    result.UnionWith(hashSet);
                }

                return result;
            }
        }
    }
}