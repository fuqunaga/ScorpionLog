using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Pool;

namespace ScotchLog;

public class ConcurrentObjectPool<T> : IObjectPool<T>
    where T : class
{
    // 参照同一性で比較し、同じインスタンスの二重Releaseを検出する。
    private sealed class ReferenceComparer : IEqualityComparer<T>
    {
        public static readonly ReferenceComparer Instance = new();
        public bool Equals(T x, T y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private readonly object _gate = new();
    // 非アクティブ要素の本体ストア。
    private readonly Stack<T> _pool = new();
    // プール内重複を防ぐための在庫インデックス。
    private readonly HashSet<T> _inPool = new(ReferenceComparer.Instance);

    private readonly Func<T> _createFunc;
    private readonly Action<T> _actionOnGet;
    private readonly Action<T> _actionOnRelease;

    public int CountInactive
    {
        get
        {
            lock (_gate)
            {
                return _pool.Count;
            }
        }
    }

    public ConcurrentObjectPool(
        Func<T> createFunc,
        Action<T> actionOnGet = null,
        Action<T> actionOnRelease = null
    )
    {
        _createFunc = createFunc;
        _actionOnGet = actionOnGet;
        _actionOnRelease = actionOnRelease;
    }

    public T Get()
    {
        T value;
        lock (_gate)
        {
            if (_pool.Count > 0)
            {
                value = _pool.Pop();
                _inPool.Remove(value);
            }
            else
            {
                value = _createFunc();
            }
        }

        _actionOnGet?.Invoke(value);
        return value;
    }

    public PooledObject<T> Get(out T v)
    {
        v = Get();
        return new PooledObject<T>(v, this);
    }

    // 重複Releaseは例外ではなくfalseを返す。
    public bool Release(T element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));

        lock (_gate)
        {
            if (!_inPool.Add(element))
            {
                return false;
            }

            _actionOnRelease?.Invoke(element);
            _pool.Push(element);
            return true;
        }
    }

    // IObjectPool<T> 互換（戻り値は破棄）。
    void IObjectPool<T>.Release(T element)
    {
        _ = Release(element);
    }

    public void Clear()
    {
        lock (_gate)
        {
            _pool.Clear();
            _inPool.Clear();
        }
    }
}