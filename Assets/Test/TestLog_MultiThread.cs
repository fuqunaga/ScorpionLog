using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace ScotchLog.Test.Editor
{
    /// <summary>
    /// 公開 Log API を並行実行しても安定して動作することを検証するテスト。
    /// </summary>
    public class TestLog_MultiThread
    {
        /// <summary>
        /// LogEntry を保持せず、受信件数のみを計測する最小実装の Sink。
        /// </summary>
        private sealed class CountingSink : ISink
        {
            private int _count;
            public int Count => _count;

            /// <summary>
            /// 受信したログ件数をスレッドセーフにインクリメントする。
            /// </summary>
            public void Log(LogEntry logEntry)
            {
                Interlocked.Increment(ref _count);
            }
        }

        /// <summary>
        /// 指定したログ出力処理を並行実行し、例外が発生しないことと配信件数を検証する。
        /// </summary>
        private static void RunConcurrentLoggingAndAssert(
            int threadCount,
            int logsPerThread,
            Action<int, int> emitLog)
        {
            var oldDispatcher = Log.LogDispatcher;
            var oldAsyncLocalDispatcher = Log.AsyncLocalLogDispatcher;

            var dispatcher = new LogDispatcher();
            var sink = new CountingSink();

            Log.LogDispatcher = dispatcher;
            Log.AsyncLocalLogDispatcher = null;
            Log.RegisterSink(sink, LogFilter.All);

            var barrier = new Barrier(threadCount);
            var exceptions = new List<Exception>();
            var exceptionLock = new object();
            var threads = new Thread[threadCount];

            try
            {
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
                                emitLog(threadIndex, i);
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (exceptionLock)
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

                for (var i = 0; i < threads.Length; i++)
                {
                    var joined = threads[i].Join(TimeSpan.FromSeconds(20));
                    if (!joined)
                    {
                        Assert.Fail($"Thread join timed out at index {i}.");
                    }
                }

                if (exceptions.Count > 0)
                {
                    Assert.Fail($"Multithreaded logging raised exception: {exceptions[0]}");
                }

                var expected = threadCount * logsPerThread;
                Assert.That(sink.Count, Is.EqualTo(expected));
            }
            finally
            {
                Log.UnregisterSink(sink);
                Log.LogDispatcher = oldDispatcher;
                Log.AsyncLocalLogDispatcher = oldAsyncLocalDispatcher;
            }
        }

        /// <summary>
        /// Log.Information を複数スレッドから呼んでも例外が出ず、件数が欠落しないことを確認する。
        /// </summary>
        [Test]
        public void Log_Information_CalledFromMultipleThreads_DoesNotThrow_AndCountMatchesExpected()
        {
            const int threadCount = 8;
            const int logsPerThread = 200;

            RunConcurrentLoggingAndAssert(
                threadCount,
                logsPerThread,
                (threadIndex, i) => Log.Information($"thread{threadIndex}-{i}"));
        }

        /// <summary>
        /// Log.Error を複数スレッドから呼んでも例外が出ず、件数が欠落しないことを確認する。
        /// </summary>
        [Test]
        public void Log_Error_CalledFromMultipleThreads_DoesNotThrow_AndCountMatchesExpected()
        {
            const int threadCount = 8;
            const int logsPerThread = 200;

            RunConcurrentLoggingAndAssert(
                threadCount,
                logsPerThread,
                (threadIndex, i) => Log.Error($"thread{threadIndex}-{i}"));
        }

        /// <summary>
        /// Scope 付きログ出力でも並行実行時に安定動作することを確認する。
        /// </summary>
        [Test]
        public void Log_InformationWithScope_CalledFromMultipleThreads_DoesNotThrow_AndCountMatchesExpected()
        {
            const int threadCount = 8;
            const int logsPerThread = 100;

            RunConcurrentLoggingAndAssert(
                threadCount,
                logsPerThread,
                (threadIndex, i) =>
                {
                    using (Log.BeginScope($"thread-scope-{threadIndex}").SetProperty("iteration", i.ToString()))
                    {
                        Log.Information($"thread{threadIndex}-{i}");
                    }
                });
        }
    }
}




