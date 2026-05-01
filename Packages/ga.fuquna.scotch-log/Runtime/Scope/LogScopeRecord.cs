using System;
using System.Collections.Generic;
using System.Threading;

namespace ScotchLog.Scope;


/// <summary>
/// ログスコープの実態
/// 複数スレッドから参照されること想定してclass型
/// </summary>
public record LogScopeRecord
{
    #region Static members

    private static int _lastId;
    private static readonly LogScopeRecord RootScope;
    private static readonly AsyncLocal<LogScopeRecord> CurrentScope = new();
    private static readonly ConcurrentObjectPool<LogScopeRecord> Pool = new(
        () => new LogScopeRecord(),
        actionOnGet: record => record.Activate(),
        actionOnRelease: record => record.Deactivate() 
    );


    static LogScopeRecord()
    {
        var record = new LogScopeRecord
        {
            Id = GetNextId()
        };
        RootScope = record;
    }
    
    
    public static LogScopeRecord Current
    {
        get => CurrentScope.Value　?? RootScope;
        private set => CurrentScope.Value = value;
    }

    private static int GetNextId()
    {
        return Interlocked.Increment(ref _lastId);
    }

    public static LogScopeRecord Start(string name, LogScopeRecord parent = null)
    {
        var record = Pool.Get();
        record.Set(name, parent);
        return record;
    }

    #endregion


    private Dictionary<string, string> _properties;

    private int _referenceCount;

    public int Id { get; private set; } = -1;
    public string Name { get; private set; }
    public LogScopeRecord Parent { get; private set; }
    public DateTime StartTimeUtc { get; private set; }
    public DateTime EndTimeUtc { get; private set; }
    public DateTime StartTime => StartTimeUtc.ToLocalTime();
    public DateTime EndTime => EndTimeUtc.ToLocalTime();
    public IReadOnlyDictionary<string, string> Properties => _properties;
    public bool IsRoot => Parent == null || Parent == this;

    private void Activate()
    {
        Id = GetNextId();
        _referenceCount = 0;
    }

    private void Deactivate()
    {
        Id = -1;
    }
    
    private void Set(string name = "", LogScopeRecord parent = null)
    {
        StartTimeUtc = DateTime.UtcNow;
        EndTimeUtc = default;
        Name = name;
        Parent = parent ?? Current;
        Current = this;
        _properties?.Clear();
    }


    public void SetProperty(string propertyName, string propertyValue)
    {
        if (EndTimeUtc != default)
        {
            throw new InvalidOperationException("Cannot set property on a closed scope.");
        }

        _properties ??= new Dictionary<string, string>();
        _properties[propertyName] = propertyValue;
    }

    public void End()
    {
        if (EndTimeUtc != default)
        {
            throw new InvalidOperationException("Scope is already closed.");
        }

        EndTimeUtc = DateTime.UtcNow;

        if (Parent != null)
        {
            if (Current != this)
            {
                // Scopeは本来親子関係にないものが並列に存在してもよいが現状実装がめんどくさくて例外扱い
                // あまり無いと思うが需要が出たら対応したい
                throw new InvalidOperationException("Current scope does not match the scope being closed.");
            }

            Current = Parent;
        }

        if (_referenceCount <= 0)
        {
            Pool.Release(this);
        }
    }

    public LogScopeRecordHolder CreateHolder()
    {
        return new LogScopeRecordHolder(this);
    }

    public void AddReference()
    {
        Interlocked.Increment(ref _referenceCount);
    }

    public void RemoveReference()
    {
        if (Interlocked.Decrement(ref _referenceCount) <= 0)
        {
            Pool.Release(this);
        }
    }
}