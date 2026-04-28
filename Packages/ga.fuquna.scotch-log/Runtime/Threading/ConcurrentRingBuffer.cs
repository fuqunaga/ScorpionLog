using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ScotchLog;

/// <summary>
/// スレッドセーフなリングバッファ（固定容量・古いエントリを自動上書き）
/// </summary>
public class ConcurrentRingBuffer<T> : IEnumerable<T>
{
    private T[] _buffer;
    private readonly Action<T> _onItemEvicted;
    private readonly object _sync = new();
    // Capacityは実行中に変更可能なのでreadonlyにはしない
    private int _capacity;
    private int _head;
    private int _tail;
    private int _count;


    // リングバッファ上のインデックスを1つ進める
    private int AdvanceIndex(int index)
    {
        index++;
        return index == _capacity ? 0 : index;
    }

    // 任意capacity向け。リサイズ中の古いバッファ読み出しに使う
    private static int AdvanceIndex(int index, int capacity)
    {
        index++;
        return index == capacity ? 0 : index;
    }

    // 現在のバッファ内容を古い順にコピーする
    private void CopyTo(T[] destination, int destinationIndex, int count)
    {
        CopyTo(_buffer, _head, _capacity, destination, destinationIndex, count);
    }

    // リサイズ中も使えるよう、コピー元バッファを明示指定できる版
    private static void CopyTo(T[] source, int sourceHead, int sourceCapacity, T[] destination, int destinationIndex, int count)
    {
        if (count <= 0)
        {
            return;
        }

        var firstSegmentCount = Math.Min(count, sourceCapacity - sourceHead);
        Array.Copy(source, sourceHead, destination, destinationIndex, firstSegmentCount);

        var remainingCount = count - firstSegmentCount;
        if (remainingCount > 0)
        {
            Array.Copy(source, 0, destination, destinationIndex + firstSegmentCount, remainingCount);
        }
    }

        
    /// <summary>
    /// バッファ容量。
    /// 縮小時は古い要素から退役し、最新の要素を優先して残す。
    /// 退役した要素には lock の外で <see cref="_onItemEvicted"/> が呼ばれる。
    /// </summary>
    public int Capacity
    {
        get => _capacity;
        set
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));

            T[] evictedItems = null;
            int evictedCount;

            lock (_sync)
            {
                if (_capacity == value)
                {
                    return;
                }

                var oldBuffer = _buffer;
                var oldCapacity = _capacity;
                var oldHead = _head;
                var oldCount = _count;

                // 縮小時は新しい要素を優先して残す
                var retainedCount = Math.Min(oldCount, value);
                evictedCount = oldCount - retainedCount;
                if (evictedCount > 0 && _onItemEvicted != null)
                {
                    // 退役するのは保持対象からあふれた先頭側（古い要素）
                    evictedItems = ArrayPool<T>.Shared.Rent(evictedCount);
                    CopyTo(oldBuffer, oldHead, oldCapacity, evictedItems, 0, evictedCount);
                }

                var newBuffer = new T[value];
                if (retainedCount > 0)
                {
                    // 先頭の退役分を飛ばした位置から、残す要素を新バッファへ詰め直す
                    var retainedHead = oldHead;
                    for (var i = 0; i < evictedCount; i++)
                    {
                        retainedHead = AdvanceIndex(retainedHead, oldCapacity);
                    }

                    CopyTo(oldBuffer, retainedHead, oldCapacity, newBuffer, 0, retainedCount);
                }

                _buffer = newBuffer;
                _capacity = value;
                _head = 0;
                // 満杯なら次の書き込み位置は先頭に戻る
                _tail = retainedCount == value ? 0 : retainedCount;
                _count = retainedCount;
            }

            if (evictedItems == null)
            {
                return;
            }

            // Disposeなど重い処理を呼ぶ可能性があるので lock の外で通知する
            for (var i = 0; i < evictedCount; i++)
            {
                _onItemEvicted(evictedItems[i]);
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(evictedItems, 0, evictedCount);
            }

            ArrayPool<T>.Shared.Return(evictedItems);
        }
    }
    
        
    public ConcurrentRingBuffer(int capacity, Action<T> onItemEvicted = null)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _buffer = new T[capacity];
        _onItemEvicted = onItemEvicted;
    }

        
    /// <summary>
    /// 要素を追加する。バッファが満杯の場合は最も古い要素を上書きする。
    /// </summary>
    public void Add(T item)
    {
        bool hasOverwritten;
        T overwritten;

        lock (_sync)
        {
            hasOverwritten = _count == Capacity;
            if (hasOverwritten)
            {
                overwritten = _buffer[_head];
                _buffer[_head] = default;
                _head = AdvanceIndex(_head);
                _count--;
            }
            else
            {
                overwritten = default;
            }

            _buffer[_tail] = item;
            _tail = AdvanceIndex(_tail);
            _count++;
        }

        if (hasOverwritten)
        {
            // lock中に外部処理を呼ばないため、退役通知は後で行う
            _onItemEvicted?.Invoke(overwritten);
        }
    }

    /// <summary>
    /// 現在のスナップショットを古い順に返す（スレッドセーフ）
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        int count;
        T[] snapshot;

        lock (_sync)
        {
            count = _count;
            if (count == 0)
            {
                yield break;
            }

            snapshot = ArrayPool<T>.Shared.Rent(count);
            CopyTo(snapshot, 0, count);
        }

        try
        {
            for (var i = 0; i < count; i++)
            {
                yield return snapshot[i];
            }
        }
        finally
        {
            if (snapshot != null)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    Array.Clear(snapshot, 0, count);
                }

                ArrayPool<T>.Shared.Return(snapshot);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// バッファをクリアする。
    /// 保持中の要素には lock の外で <see cref="_onItemEvicted"/> を呼ぶ。
    /// </summary>
    public void Clear()
    {
        T[] removedItems = null;
        var removedCount = 0;

        lock (_sync)
        {
            if (_count > 0 && _onItemEvicted != null)
            {
                removedCount = _count;
                removedItems = ArrayPool<T>.Shared.Rent(removedCount);
                CopyTo(removedItems, 0, removedCount);
            }

            _head = 0;
            _tail = 0;
            _count = 0;
            Array.Clear(_buffer, 0, Capacity);
        }

        if (removedItems == null)
        {
            return;
        }

        for (var i = 0; i < removedCount; i++)
        {
            _onItemEvicted(removedItems[i]);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(removedItems, 0, removedCount);
        }

        ArrayPool<T>.Shared.Return(removedItems);
    }
}