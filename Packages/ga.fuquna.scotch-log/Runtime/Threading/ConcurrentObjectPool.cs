using System;
using System.Collections.Concurrent;
using UnityEngine.Pool;

namespace ScotchLog;

public class ConcurrentObjectPool<T> : IObjectPool<T>
    where T : class, new()
{
    private readonly ConcurrentQueue<T> _pool = new();

    private readonly Func<T> _createFunc;
    private readonly Action<T> _actionOnGet;
    private readonly Action<T> _actionOnRelease;
    
    public int CountInactive => _pool.Count;
    
    
    public ConcurrentObjectPool(
        Func<T> createFunc,
        Action<T> actionOnGet = null,
        Action<T> actionOnRelease = null,
        Action<T> actionOnDestroy = null
    )
    {
        _createFunc = createFunc;
        _actionOnGet = actionOnGet;
        _actionOnRelease = actionOnRelease;
    }

    public T Get()
    {
        if (!_pool.TryDequeue(out var value))
        {
            value = _createFunc();        
        }

        _actionOnGet?.Invoke(value);
        return value;
        
    }

    public PooledObject<T> Get(out T v)
    {
        v = Get();
        return new PooledObject<T>(v, this);
    }

    public void Release(T element)
    {
        _actionOnRelease?.Invoke(element);
        _pool.Enqueue(element);
    }

    public void Clear()
    {
        _pool.Clear();
    }
}