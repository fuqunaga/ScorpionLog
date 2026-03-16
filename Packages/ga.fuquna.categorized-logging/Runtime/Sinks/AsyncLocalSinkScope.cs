using System;
using JetBrains.Annotations;

namespace CategorizedLogging
{
    /// <summary>
    /// Thread-local sink scope that registers a sink for the current thread and automatically unregisters it when disposed.
    /// </summary>
    [MustDisposeResource]
    public readonly struct AsyncLocalSinkScope : IDisposable
    {
        private readonly ISink _sink;
        
        
        public AsyncLocalSinkScope(ISink sink, LogLevel logLevel) : this(sink, LogFilter.Create(logLevel))
        {}
        
        public AsyncLocalSinkScope(ISink sink, LogFilter config)
        {
            _sink = sink;
            Log.RegisterAsyncLocalSink(_sink, config);
        }
        
        public void Dispose()
        {
            Log.UnregisterAsyncLocalSink(_sink);
        }
    }
}