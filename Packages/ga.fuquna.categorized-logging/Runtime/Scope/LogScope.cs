using System;
using JetBrains.Annotations;

namespace CategorizedLogging
{
    /// <summary>
    /// 同一スレッドにおけるスコープ
    /// スコープとはLogPropertyを保持する単位
    /// </summary>
    [MustDisposeResource]
    public readonly struct LogScope : IDisposable
    {
        private readonly int _propertyId;

        public LogScope(in LogProperty logProperty)
        {
            _propertyId = Log.PropertyHolder.Add(in logProperty);
        }

        public void Dispose()
        {
            Log.PropertyHolder.Remove(_propertyId);
        }
    }
}