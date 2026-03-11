using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CategorizedLogging
{
    /// <summary>
    /// カテゴリを紐づけたロガーのインターフェース
    /// </summary>
    public class Logger : ILogger
    {
        private static readonly Dictionary<string, Logger> Loggers = new();
        private static readonly Dictionary<(string callcerFilePath, int lineNumber), string> CallerToCategoryCache = new();
        
        
        public static Logger Get<T>(T _) => Get<T>();
        public static Logger Get<T>() => Get(typeof(T).Name);
        public static Logger Get(string category)
        {
            if (!Loggers.TryGetValue(category, out var logger))
            {
                logger = new Logger(category);
                Loggers[category] = logger;
            }
            return logger;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Logger GetForCaller([CallerFilePath]string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var key = (callerFilePath, callerLineNumber);
            if (CallerToCategoryCache.TryGetValue(key, out var cachedCategory))
            {
                return Get(cachedCategory);
            }
            
            var stackTrace = new System.Diagnostics.StackTrace();
            var callingFrame = stackTrace.GetFrame(1);
            var method = callingFrame.GetMethod();
            var declaringType = method.DeclaringType;
            var category = declaringType != null ? declaringType.Name : "UnknownCategory";
            var logger = Get(category);
            
            CallerToCategoryCache[key] = category;
            return logger;
        }
        
        
        private readonly string _category;
        
        public Logger(string category)
        {
            _category = category;
        }

        [HideInCallstack]
        public void EmitLog(LogLevel logLevel, string message)
        {
            Log.EmitLog(_category, logLevel, message);
        }
    }
}