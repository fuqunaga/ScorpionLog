using System;
using JetBrains.Annotations;

namespace CategorizedLogging.Scope
{
    /// <summary>
    /// 同一スレッドにおけるスコープ
    /// スコープとはLogPropertyを保持する単位
    /// </summary>
    [MustDisposeResource]
    public readonly struct LogScope : IDisposable
    {
        private readonly LogScopeRecord _record;
        
        public LogScope(string name = "", LogScopeRecord parent = null)
        {
            _record = new LogScopeRecord(name, parent);
        }
        
        public LogScope SetProperty(string propertyName, string propertyValue)
        {
            _record.SetProperty(propertyName, propertyValue);
            return this;
        }

        public void Dispose()
        {
            _record.Complete();
        }
    }
}