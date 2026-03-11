using System;
using JetBrains.Annotations;

namespace CategorizedLogging
{
    /// <summary>
    /// Thread-local sink scope that registers a sink for the current thread and automatically unregisters it when disposed.
    /// </summary>
    [MustDisposeResource]
    public readonly struct ThreadLocalSinkScope : IDisposable
    {
        private readonly ISink _sink;
        
        
        public ThreadLocalSinkScope(ISink sink, LogLevel logLevel) : this(sink, SinkFilterConfig.Create(logLevel))
        {}
        
        public ThreadLocalSinkScope(ISink sink, SinkFilterConfig config)
        {
            _sink = sink;
            Log.RegisterThreadLocalSink(_sink, config);
        }
        
        public void Dispose()
        {
            Log.UnregisterThreadLocalSink(_sink);
        }
    }
}