using System;

namespace ScotchLog.Scope;

/// <summary>
/// LogScopeRecord保持インターフェース
/// LogScopeRecordは不要になったら回収したい
/// 本クラスで参照中であることを登録し、
/// どのHolderからも参照されなくなったらLogScopeRecordは回収される
/// </summary>
public readonly struct LogScopeRecordHolder : IDisposable
{
    private readonly LogScopeRecord _record;


    public LogScopeRecordHolder(LogScopeRecord record)
    {
        _record = record;
        _record.AddReference();
    }

    
    public void Dispose()
    {
        _record.RemoveReference();
    }
}