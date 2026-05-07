using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace ScotchLog.Test.Editor
{
    /// <summary>
    /// LogDispatcher のマルチスレッド実行時の安全性を検証するテスト
    /// </summary>
    public class TestLogDispatcher_MultiThread
    {
        private class CountingSink : ISink
        {
            private int _count;
            public int Count => _count;

            public void Log(LogEntry logEntry)
            {
                Interlocked.Increment(ref _count);
            }
        }

        private static LogEntry MakeRecord()
        {
            var entry = new LogEntry();
            entry.Set(LogLevel.Information, "test", default);
            return entry;
        }

        /// <summary>
        /// 複数スレッドから同時に Log() を呼んだときに、
        /// 例外が発生せず Sink 呼び出し回数が期待値と一致することを確認する
        /// </summary>
        [Test]
        public void Log_MultiThread_HashSetPool_IsNotThreadSafe()
        {
            const int threadCount = 16;
            const int logsPerThread = 500;
            const int sinkCount = 4;

            var dispatcher = new LogDispatcher();
            var sinks = new CountingSink[sinkCount];

            for (var i = 0; i < sinkCount; i++)
            {
                sinks[i] = new CountingSink();
                dispatcher.Register(sinks[i], LogFilter.All);
            }

            var barrier = new Barrier(threadCount);
            var exceptions = new List<Exception>();
            var exceptionLock = new object();
            var threads = new Thread[threadCount];

            for (var t = 0; t < threadCount; t++)
            {
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        // 全スレッドが同時にスタートするよう同期
                        barrier.SignalAndWait();

                        for (var i = 0; i < logsPerThread; i++)
                        {
                            dispatcher.Log(MakeRecord());
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

            foreach (var thread in threads)
            {
                thread.Join(TimeSpan.FromSeconds(30));
            }

            var expectedTotal = threadCount * logsPerThread;

            // 例外が発生していた場合はスレッドセーフでないことを示す
            if (exceptions.Count > 0)
            {
                Assert.Fail(
                    $"スレッドセーフでないため例外が発生しました ({exceptions.Count}件):\n" +
                    $"{exceptions[0]}");
            }

            // 各Sinkの呼び出し回数が期待値と一致しない場合もスレッドセーフ問題を示す
            foreach (var sink in sinks)
            {
                if (sink.Count != expectedTotal)
                {
                    Assert.Fail(
                        $"並行 Log 実行時に Sink の呼び出し回数が不正です。" +
                        $"期待値: {expectedTotal}, 実際: {sink.Count}\n" +
                        $"(LogDispatcher の並行実行における取りこぼし/重複の可能性があります)");
                }
            }

            // ここに到達した場合はたまたまセーフだった可能性がある
            Assert.Pass($"今回は競合が発生しませんでした (各Sink呼び出し数: {sinks[0].Count}/{expectedTotal})");
        }

        /// <summary>
        /// Log() と Register()/Unregister() を並行実行した場合の安全性を確認する
        /// </summary>
        [Test]
        public void Log_MultiThread_WithRegisterUnregister_IsNotThreadSafe()
        {
            const int logThreadCount = 8;
            const int registerThreadCount = 4;
            const int iterations = 200;

            var dispatcher = new LogDispatcher();
            var exceptions = new List<Exception>();
            var exceptionLock = new object();
            var cts = new CancellationTokenSource();
            var barrier = new Barrier(logThreadCount + registerThreadCount);
            var threads = new Thread[logThreadCount + registerThreadCount];

            // Logを送り続けるスレッド
            for (var t = 0; t < logThreadCount; t++)
            {
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait(cts.Token);
                        for (var i = 0; i < iterations; i++)
                        {
                            dispatcher.Log(MakeRecord());
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptionLock) { exceptions.Add(ex); }
                        cts.Cancel();
                    }
                })
                {
                    IsBackground = true
                };
            }

            // Register/Unregisterを繰り返すスレッド
            for (var t = 0; t < registerThreadCount; t++)
            {
                threads[logThreadCount + t] = new Thread(() =>
                {
                    var sink = new CountingSink();
                    try
                    {
                        barrier.SignalAndWait(cts.Token);
                        for (var i = 0; i < iterations; i++)
                        {
                            dispatcher.Register(sink, LogFilter.All);
                            dispatcher.Log(MakeRecord());
                            dispatcher.Unregister(sink);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptionLock) { exceptions.Add(ex); }
                        cts.Cancel();
                    }
                })
                {
                    IsBackground = true
                };
            }

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join(TimeSpan.FromSeconds(30));

            if (exceptions.Count > 0)
            {
                Assert.Fail(
                    $"並行 Register/Log/Unregister でスレッドセーフ問題が発生しました ({exceptions.Count}件):\n" +
                    $"{exceptions[0]}");
            }

            Assert.Pass("今回は競合が発生しませんでした");
        }
    }
}

