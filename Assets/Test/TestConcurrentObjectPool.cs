using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace ScotchLog.Test.Editor
{
    public class TestConcurrentObjectPool
    {
        private class SimpleObj
        {
            public int Value;
        }

        private static void AssertNoGCAlloc(Action action, string label)
        {
            // JIT/内部キャッシュの初期化ノイズを避ける。
            action();
            action();

            var constraint = new UnityEngine.TestTools.Constraints.AllocatingGCMemoryConstraint();
            var result = constraint.ApplyTo((TestDelegate)(() => action()));
            Assert.That(result.IsSuccess, Is.False, $"{label} でGCアロケーションが発生しました");
        }

        // ─── Get ─────────────────────────────────────────────────────────────

        [Test]
        public void Get_EmptyPool_CallsCreateFuncAndReturnsNewObject()
        {
            var createCount = 0;
            var pool = new ConcurrentObjectPool<SimpleObj>(
                createFunc: () =>
                {
                    createCount++;
                    return new SimpleObj();
                }
            );

            var obj = pool.Get();

            Assert.That(obj, Is.Not.Null);
            Assert.That(createCount, Is.EqualTo(1));
        }

        [Test]
        public void Get_AfterRelease_ReusesPooledObject()
        {
            var createCount = 0;
            var pool = new ConcurrentObjectPool<SimpleObj>(
                createFunc: () =>
                {
                    createCount++;
                    return new SimpleObj();
                }
            );

            var first = pool.Get();
            pool.Release(first);
            var second = pool.Get();

            Assert.That(second, Is.SameAs(first));
            Assert.That(createCount, Is.EqualTo(1));
        }

        [Test]
        public void Get_MultipleGets_EachCallsCreateFunc()
        {
            var createCount = 0;
            var pool = new ConcurrentObjectPool<SimpleObj>(
                createFunc: () =>
                {
                    createCount++;
                    return new SimpleObj();
                }
            );

            pool.Get();
            pool.Get();
            pool.Get();

            Assert.That(createCount, Is.EqualTo(3));
        }

        // ─── Release / CountInactive ──────────────────────────────────────────

        [Test]
        public void Release_IncreasesCountInactive()
        {
            var pool = new ConcurrentObjectPool<SimpleObj>(createFunc: () => new SimpleObj());

            pool.Release(new SimpleObj());
            pool.Release(new SimpleObj());

            Assert.That(pool.CountInactive, Is.EqualTo(2));
        }

        [Test]
        public void Get_DecreasesCountInactive()
        {
            var pool = new ConcurrentObjectPool<SimpleObj>(createFunc: () => new SimpleObj());

            var obj = new SimpleObj();
            pool.Release(obj);
            Assert.That(pool.CountInactive, Is.EqualTo(1));

            pool.Get();
            Assert.That(pool.CountInactive, Is.EqualTo(0));
        }

        // ─── ActionOnGet / ActionOnRelease ────────────────────────────────────

        [Test]
        public void Get_InvokesActionOnGet()
        {
            var getCallCount = 0;
            var pool = new ConcurrentObjectPool<SimpleObj>(
                createFunc: () => new SimpleObj(),
                actionOnGet: _ => getCallCount++
            );

            pool.Get();
            pool.Get();

            Assert.That(getCallCount, Is.EqualTo(2));
        }

        [Test]
        public void Release_InvokesActionOnRelease()
        {
            var releaseCallCount = 0;
            var pool = new ConcurrentObjectPool<SimpleObj>(
                createFunc: () => new SimpleObj(),
                actionOnRelease: _ => releaseCallCount++
            );

            pool.Release(new SimpleObj());
            pool.Release(new SimpleObj());

            Assert.That(releaseCallCount, Is.EqualTo(2));
        }

        [Test]
        public void Get_ActionOnGet_ReceivesCorrectObject()
        {
            SimpleObj receivedInGet = null;
            var pool = new ConcurrentObjectPool<SimpleObj>(
                createFunc: () => new SimpleObj { Value = 99 },
                actionOnGet: obj => receivedInGet = obj
            );

            var gotten = pool.Get();

            Assert.That(receivedInGet, Is.SameAs(gotten));
        }

        [Test]
        public void Release_ActionOnRelease_ReceivesCorrectObject()
        {
            SimpleObj receivedInRelease = null;
            var pool = new ConcurrentObjectPool<SimpleObj>(
                createFunc: () => new SimpleObj(),
                actionOnRelease: obj => receivedInRelease = obj
            );

            var obj = new SimpleObj { Value = 42 };
            pool.Release(obj);

            Assert.That(receivedInRelease, Is.SameAs(obj));
        }

        // ─── Get(out T) / PooledObject ────────────────────────────────────────

        [Test]
        public void Get_OutParam_ReturnsSameObjectAsPooledObject()
        {
            var pool = new ConcurrentObjectPool<SimpleObj>(createFunc: () => new SimpleObj());

            using (pool.Get(out var obj))
            {
                Assert.That(obj, Is.Not.Null);
            }

            // PooledObject の Dispose 後は CountInactive が増えているはず
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        // ─── Clear ───────────────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesAllPooledObjects()
        {
            var pool = new ConcurrentObjectPool<SimpleObj>(createFunc: () => new SimpleObj());

            pool.Release(new SimpleObj());
            pool.Release(new SimpleObj());
            pool.Release(new SimpleObj());

            pool.Clear();

            Assert.That(pool.CountInactive, Is.EqualTo(0));
        }

        [Test]
        public void Clear_EmptyPool_DoesNotThrow()
        {
            var pool = new ConcurrentObjectPool<SimpleObj>(createFunc: () => new SimpleObj());
            Assert.DoesNotThrow(() => pool.Clear());
        }

        // ─── スレッドセーフ ───────────────────────────────────────────────────

        [Test]
        public void GetRelease_CalledFromMultipleThreads_DoesNotThrow()
        {
            const int threadCount = 8;
            const int iterations = 300;

            var pool = new ConcurrentObjectPool<SimpleObj>(createFunc: () => new SimpleObj());
            var exceptions = new List<Exception>();
            var exceptionLock = new object();
            var barrier = new Barrier(threadCount);
            var threads = new Thread[threadCount];

            for (var t = 0; t < threadCount; t++)
            {
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        for (var i = 0; i < iterations; i++)
                        {
                            var obj = pool.Get();
                            pool.Release(obj);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptionLock) { exceptions.Add(ex); }
                    }
                }) { IsBackground = true };
                threads[t].Start();
            }

            foreach (var thread in threads)
                thread.Join(TimeSpan.FromSeconds(10));

            if (exceptions.Count > 0)
                Assert.Fail($"マルチスレッドで例外が発生しました: {exceptions[0]}");
        }

        [Test]
        public void Release_DuplicateElement_ReturnsFalse()
        {
            var pool = new ConcurrentObjectPool<SimpleObj>(createFunc: () => new SimpleObj());
            var obj = new SimpleObj();

            var first = pool.Release(obj);
            var second = pool.Release(obj);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void Alloc_SteadyState_GetRelease_NoGCAlloc()
        {
            var pool = new ConcurrentObjectPool<SimpleObj>(createFunc: () => new SimpleObj());

            var seed = pool.Get();
            pool.Release(seed);

            AssertNoGCAlloc(() =>
            {
                var obj = pool.Get();
                pool.Release(obj);
            }, "steady-state Get/Release");
        }

        [Test]
        public void Alloc_SteadyState_GetOutDispose_NoGCAlloc()
        {
            var pool = new ConcurrentObjectPool<SimpleObj>(createFunc: () => new SimpleObj());

            var seed = pool.Get();
            pool.Release(seed);

            AssertNoGCAlloc(() =>
            {
                using (pool.Get(out _))
                {
                }
            }, "steady-state Get(out)/Dispose");
        }

        [Test]
        public void Alloc_DuplicateRelease_ReturnsFalse_NoGCAlloc()
        {
            var pool = new ConcurrentObjectPool<SimpleObj>(createFunc: () => new SimpleObj());
            var obj = new SimpleObj();
            pool.Release(obj);

            var result = true;
            AssertNoGCAlloc(() => { result = pool.Release(obj); }, "duplicate Release");

            Assert.That(result, Is.False);
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }
    }
}
