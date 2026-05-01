using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ScotchLog;

public sealed class ConcurrentHashSet<T> : IReadOnlyCollection<T>
{
    private const byte Present = 0;
    private readonly ConcurrentDictionary<T, byte> _dictionary;

    public ConcurrentHashSet()
        : this(null)
    {
    }

    public ConcurrentHashSet(IEqualityComparer<T> comparer)
    {
        _dictionary = new ConcurrentDictionary<T, byte>(comparer ?? EqualityComparer<T>.Default);
    }

    public int Count => _dictionary.Count;

    public bool IsEmpty => _dictionary.IsEmpty;

    public bool Add(T item)
    {
        return _dictionary.TryAdd(item, Present);
    }

    public bool Remove(T item)
    {
        return _dictionary.TryRemove(item, out _);
    }

    public bool Contains(T item)
    {
        return _dictionary.ContainsKey(item);
    }

    // Tries to remove and return one arbitrary element.
    public bool TryTake(out T item)
    {
        foreach (var entry in _dictionary)
        {
            var key = entry.Key;
            if (_dictionary.TryRemove(key, out _))
            {
                item = key;
                return true;
            }
        }

        item = default;
        return false;
    }

    public void Clear()
    {
        _dictionary.Clear();
    }

    // Returns a point-in-time copy for deterministic iteration.
    public T[] ToArray()
    {
        return _dictionary.Keys.ToArray();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _dictionary.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
