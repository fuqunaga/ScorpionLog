using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace ScotchLog.Test.Editor
{
    public class TestConcurrentRingBuffer
    {
        [Test]
        public void Constructor_CapacityZero_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentRingBuffer<int>(0));
        }

        [Test]
        public void Capacity_SetZero_ThrowsArgumentOutOfRangeException()
        {
            var buffer = new ConcurrentRingBuffer<int>(3);

            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Capacity = 0);
        }

        [Test]
        public void Add_WithinCapacity_KeepsInsertionOrder()
        {
            var buffer = new ConcurrentRingBuffer<int>(5);

            buffer.Add(10);
            buffer.Add(20);
            buffer.Add(30);

            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, buffer.ToArray());
        }

        [Test]
        public void Add_ExceedCapacity_EvictsOldestAndKeepsNewest()
        {
            var evicted = new List<int>();
            var buffer = new ConcurrentRingBuffer<int>(3, item => evicted.Add(item));

            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, buffer.ToArray());
            CollectionAssert.AreEqual(new[] { 1 }, evicted);
        }

        [Test]
        public void Capacity_Reduced_RetainsNewestEntriesAndEvictsOldest()
        {
            var evicted = new List<int>();
            var buffer = new ConcurrentRingBuffer<int>(5, item => evicted.Add(item));

            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            buffer.Capacity = 2;

            Assert.That(buffer.Capacity, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { 4, 5 }, buffer.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, evicted);
        }

        [Test]
        public void Capacity_Increased_PreservesAllExistingEntries()
        {
            var evicted = new List<int>();
            var buffer = new ConcurrentRingBuffer<int>(2, item => evicted.Add(item));

            buffer.Add(7);
            buffer.Add(8);

            buffer.Capacity = 4;
            buffer.Add(9);

            Assert.That(buffer.Capacity, Is.EqualTo(4));
            CollectionAssert.AreEqual(new[] { 7, 8, 9 }, buffer.ToArray());
            Assert.That(evicted.Count, Is.EqualTo(0));
        }

        [Test]
        public void Clear_RemovesAllEntries_AndEvictsStoredItems()
        {
            var evicted = new List<int>();
            var buffer = new ConcurrentRingBuffer<int>(4, item => evicted.Add(item));

            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.Clear();

            Assert.That(buffer.ToArray(), Is.Empty);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, evicted);
        }

        [Test]
        public void Clear_EmptyBuffer_DoesNotThrow()
        {
            var buffer = new ConcurrentRingBuffer<int>(3);

            Assert.DoesNotThrow(() => buffer.Clear());
            Assert.That(buffer.ToArray(), Is.Empty);
        }

        [Test]
        public void Add_CalledFromMultipleThreads_DoesNotThrow_AndStaysWithinCapacity()
        {
            const int capacity = 1000;
            const int threadCount = 8;
            const int addPerThread = 250;

            var buffer = new ConcurrentRingBuffer<int>(capacity);
            var exceptions = new List<Exception>();
            var exceptionsLock = new object();
            var barrier = new Barrier(threadCount);
            var threads = new Thread[threadCount];

            for (var t = 0; t < threadCount; t++)
            {
                var threadId = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        for (var i = 0; i < addPerThread; i++)
                        {
                            buffer.Add((threadId * 100000) + i);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptionsLock)
                        {
                            exceptions.Add(ex);
                        }
                    }
                })
                {
                    IsBackground = true
                };
                threads[t].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join(TimeSpan.FromSeconds(10));
            }

            if (exceptions.Count > 0)
            {
                Assert.Fail($"Concurrent Add raised exception: {exceptions[0]}");
            }

            Assert.That(buffer.Count(), Is.LessThanOrEqualTo(buffer.Capacity));
        }
    }
}

