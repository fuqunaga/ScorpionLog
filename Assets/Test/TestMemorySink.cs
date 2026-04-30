using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Unity.Collections;

namespace ScotchLog.Test.Editor
{
    public class TestMemorySink
    {
        private static LogEntry MakeEntry(LogLevel logLevel, string message)
        {
            var entry = new LogEntry();
            entry.Set(logLevel, message, default);
            return entry;
        }

        // ─── Log() ───────────────────────────────────────────────────────────

        [Test]
        public void Log_AddsEntryToLogEntries()
        {
            var sink = new MemorySink();
            sink.Log(MakeEntry(LogLevel.Debug, "hello"));

            Assert.That(sink.LogEntries.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Log_PreservesLogLevel()
        {
            var sink = new MemorySink();
            sink.Log(MakeEntry(LogLevel.Warning, "warn"));

            Assert.That(sink.LogEntries.First().LogLevel, Is.EqualTo(LogLevel.Warning));
        }

        [Test]
        public void Log_PreservesMessage()
        {
            var sink = new MemorySink();
            sink.Log(MakeEntry(LogLevel.Information, "test message"));

            Assert.That(sink.LogEntries.First().Message, Is.EqualTo("test message"));
        }

        [Test]
        public void Log_FiresOnLogEntryAddedMultiThreadedEvent()
        {
            var sink = new MemorySink();
            var callCount = 0;
            sink.onLogEntryAddedMultiThreaded += () => callCount++;

            sink.Log(MakeEntry(LogLevel.Debug, "event test"));

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Log_MultipleEntries_FiresEventEachTime()
        {
            var sink = new MemorySink();
            var callCount = 0;
            sink.onLogEntryAddedMultiThreaded += () => callCount++;

            sink.Log(MakeEntry(LogLevel.Debug, "1"));
            sink.Log(MakeEntry(LogLevel.Debug, "2"));
            sink.Log(MakeEntry(LogLevel.Debug, "3"));

            Assert.That(callCount, Is.EqualTo(3));
        }

        [Test]
        public void Log_MultipleEntries_StoredInOrder()
        {
            var sink = new MemorySink();
            sink.Log(MakeEntry(LogLevel.Debug, "first"));
            sink.Log(MakeEntry(LogLevel.Information, "second"));
            sink.Log(MakeEntry(LogLevel.Warning, "third"));

            var entries = sink.LogEntries.ToList();
            Assert.That(entries.Count, Is.EqualTo(3));
            Assert.That(entries[0].Message, Is.EqualTo("first"));
            Assert.That(entries[1].Message, Is.EqualTo("second"));
            Assert.That(entries[2].Message, Is.EqualTo("third"));
        }

        // ─── Clear() ─────────────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesAllEntries()
        {
            var sink = new MemorySink();
            sink.Log(MakeEntry(LogLevel.Debug, "a"));
            sink.Log(MakeEntry(LogLevel.Debug, "b"));

            sink.Clear();

            Assert.That(sink.LogEntries.Count(), Is.EqualTo(0));
        }

        [Test]
        public void Clear_OnEmptySink_DoesNotThrow()
        {
            var sink = new MemorySink();
            Assert.DoesNotThrow(() => sink.Clear());
        }

        // ─── Capacity ────────────────────────────────────────────────────────

        [Test]
        public void DefaultCapacity_Is1000()
        {
            var sink = new MemorySink();
            Assert.That(sink.Capacity, Is.EqualTo(1000));
        }

        [Test]
        public void Capacity_CanBeChanged()
        {
            var sink = new MemorySink();
            sink.Capacity = 500;
            Assert.That(sink.Capacity, Is.EqualTo(500));
        }

        [Test]
        public void Log_ExceedingCapacity_OldestEntryEvicted()
        {
            var sink = new MemorySink();
            sink.Capacity = 3;

            sink.Log(MakeEntry(LogLevel.Debug, "oldest"));
            sink.Log(MakeEntry(LogLevel.Debug, "middle"));
            sink.Log(MakeEntry(LogLevel.Debug, "newest"));
            sink.Log(MakeEntry(LogLevel.Debug, "extra")); // "oldest" が溢れる

            var entries = sink.LogEntries.ToList();
            Assert.That(entries.Count, Is.EqualTo(3));
            Assert.That(entries[0].Message, Is.EqualTo("middle"));
            Assert.That(entries[1].Message, Is.EqualTo("newest"));
            Assert.That(entries[2].Message, Is.EqualTo("extra"));
        }

        [Test]
        public void Log_AfterCapacityReduced_RetainsNewestEntries()
        {
            var sink = new MemorySink();
            sink.Log(MakeEntry(LogLevel.Debug, "old1"));
            sink.Log(MakeEntry(LogLevel.Debug, "old2"));
            sink.Log(MakeEntry(LogLevel.Debug, "new1"));
            sink.Log(MakeEntry(LogLevel.Debug, "new2"));

            sink.Capacity = 2; // 古い2件が破棄される

            var entries = sink.LogEntries.ToList();
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].Message, Is.EqualTo("new1"));
            Assert.That(entries[1].Message, Is.EqualTo("new2"));
        }

        // ─── NativeText（TempJob）バックの StringWrapper は Persistent にクローンされる ──

        /// <summary>
        /// MemorySink.Log() は StringWrapper を Clone(Allocator.Persistent) してコピーする。
        /// そのため、FixedString 等から生成された NativeText(TempJob) バックの StringWrapper でも
        /// 保存済みエントリは Persistent な独立コピーを保持し、Message を読める。
        /// </summary>
        [Test]
        public void Log_NativeTextBackedStringWrapper_IsClonedToPersistent_MessageReadableWhileAlive()
        {
            var sink = new MemorySink();

            // FixedString → StringWrapper の暗黙変換で NativeText(TempJob) が生成される
            StringWrapper nativeTextBacked = new FixedString64Bytes("tempjob message");
            var entry = new LogEntry();
            entry.Set(LogLevel.Debug, nativeTextBacked, default);
            sink.Log(entry);

            // NativeText が生存中はメッセージを読める
            Assert.That(sink.LogEntries.First().Message, Is.EqualTo("tempjob message"));

            nativeTextBacked.Dispose();
        }

        /// <summary>
        /// MemorySink.Log() は StringWrapper を Clone(Allocator.Persistent) してコピーする。
        /// そのため、オリジナルの NativeText(TempJob) を Dispose しても、
        /// 保存済みエントリは独立した Persistent コピーを持ち、メッセージが引き続き読める。
        /// </summary>
        [Test]
        public void Log_NativeTextBackedStringWrapper_IsClonedToPersistent_MessageStillReadableAfterDispose()
        {
            var sink = new MemorySink();

            // FixedString → StringWrapper の暗黙変換で NativeText(TempJob) が生成される
            StringWrapper nativeTextBacked = new FixedString64Bytes("tempjob message");
            var entry = new LogEntry();
            entry.Set(LogLevel.Debug, nativeTextBacked, default);
            sink.Log(entry);

            var stored = sink.LogEntries.First();

            // オリジナルの NativeText を破棄（TempJob の寿命切れをシミュレート）
            nativeTextBacked.Dispose();

            // Clone(Allocator.Persistent) により独立したコピーが作られているため
            // オリジナル破棄後もメッセージが読める
            Assert.That(stored.Message, Is.EqualTo("tempjob message"));
        }

        // ─── スレッドセーフ ───────────────────────────────────────────────────

        [Test]
        public void Log_CalledFromMultipleThreads_DoesNotThrow()
        {
            var sink = new MemorySink();
            const int threadCount = 10;
            const int logsPerThread = 200;

            var threads = new Thread[threadCount];
            var exceptions = new List<Exception>();
            var exceptionLock = new object();
            var barrier = new Barrier(threadCount);

            for (var t = 0; t < threadCount; t++)
            {
                var threadIndex = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        for (var i = 0; i < logsPerThread; i++)
                        {
                            sink.Log(MakeEntry(LogLevel.Debug, $"thread{threadIndex}-{i}"));
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

            // 容量以下のエントリ数が保持されていること
            Assert.That(sink.LogEntries.Count(), Is.LessThanOrEqualTo(sink.Capacity));
        }
    }
}