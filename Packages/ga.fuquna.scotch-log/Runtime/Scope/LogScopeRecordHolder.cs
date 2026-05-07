using System;

namespace ScotchLog.Scope;

/// <summary>
/// LogScopeRecord保持インターフェース
/// LogScopeRecordは不要になったら回収したい
/// 本クラスで参照中であることを登録し、
/// どのHolderからも参照されなくなったらLogScopeRecordは回収される
/// </summary>
public readonly record struct LogScopeRecordHolder : IDisposable
{
    public　LogScopeRecord Record { get; }


    public LogScopeRecordHolder(LogScopeRecord record)
    {
        Record = record;
        Record.AddReference();
    }
    
    public void Dispose()
    {
        Record?.RemoveReference();
    }
}