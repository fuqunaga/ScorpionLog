using System;
using System.Runtime.CompilerServices;

namespace CategorizedLogging.Scope
{
    /// <summary>
    /// 同一スレッドにおけるスコープ
    /// スコープとはLogPropertyを保持する単位
    /// </summary>
    public readonly struct LogScope : IDisposable
    {
        private readonly LogScopeRecord _record;

        
        public LogScope() : this("")
        {
        }

        public LogScope(string name, LogScopeRecord parent = null)
        {
            _record = new LogScopeRecord(name, parent);
        }
        
        public LogScope SetProperty<T>(T propertyValue, [CallerArgumentExpression("propertyValue")] string propertyName = "")
            => SetProperty(propertyName, propertyValue);
        
        public LogScope SetProperty<T>(string propertyName, T propertyValue)
            => SetProperty(propertyName, propertyValue?.ToString());
        
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