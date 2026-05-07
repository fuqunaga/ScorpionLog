using System;
using System.Runtime.CompilerServices;

namespace ScotchLog.Scope;

/// <summary>
/// ログスコープ
/// アプリケーション向けのLogSpanのインターフェース
/// </summary>
public readonly struct LogScope : IDisposable
{
    private readonly LogScopeRecord _record;

        
    public LogScope() : this("")
    {
    }

    public LogScope(string name, LogScopeRecord parent = null)
    {
        _record = LogScopeRecord.Start(name, parent);
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
        _record.End();
    }
}