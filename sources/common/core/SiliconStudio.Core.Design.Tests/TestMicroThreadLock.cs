// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SiliconStudio.Core.MicroThreading;

namespace SiliconStudio.Core.Design.Tests
{
    [TestFixture]
    public class TestMicroThreadLock
    {
        const int ThreadCount = 50;
        const int IncrementCount = 20;

        [Test, Timeout(5000)]
        public void TestConcurrencyInMicrothreads()
        {
            var scheduler = new Scheduler();
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            for (var j = 0; j < ThreadCount; ++j)
            {
                var microThread = scheduler.Create();
                microThread.Start(async () =>
                {
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.AreEqual(initialValue + i, counter);
                            await Task.Yield();
                            ++counter;
                        }
                    }
                });
            }
            while (scheduler.MicroThreads.Count > 0)
            {
                scheduler.Run();
            }
            Assert.AreEqual(ThreadCount * IncrementCount, counter);
        }

        [Test, Timeout(5000)]
        public void TestSequentialLocksInMicrothreads()
        {
            var scheduler = new Scheduler();
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            for (var j = 0; j < ThreadCount; ++j)
            {
                var microThread = scheduler.Create();
                microThread.Start(async () =>
                {
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.AreEqual(initialValue + i, counter);
                            await Task.Yield();
                            ++counter;
                        }
                    }
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.AreEqual(initialValue + i, counter);
                            await Task.Yield();
                            ++counter;
                        }
                    }
                });
            }
            while (scheduler.MicroThreads.Count > 0)
            {
                scheduler.Run();
            }
            Assert.AreEqual(2 * ThreadCount * IncrementCount, counter);
        }

        [Test, Timeout(5000)]
        public void TestReentrancyInMicrothreads()
        {
            var scheduler = new Scheduler();
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            for (var j = 0; j < ThreadCount; ++j)
            {
                var microThread = scheduler.Create();
                microThread.Start(async () =>
                {
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        using (await microThreadLock.LockAsync())
                        {
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                using (await microThreadLock.LockAsync())
                                {
                                    using (await microThreadLock.LockAsync())
                                    {
                                        Assert.AreEqual(initialValue + i, counter);
                                    }
                                    using (await microThreadLock.LockAsync())
                                    {
                                        await Task.Yield();
                                    }
                                    using (await microThreadLock.LockAsync())
                                    {
                                        ++counter;
                                    }
                                }
                            }
                        }
                    }
                });
            }
            while (scheduler.MicroThreads.Count > 0)
            {
                scheduler.Run();
            }
            Assert.AreEqual(ThreadCount * IncrementCount, counter);
        }

        [Test, Timeout(5000)]
        public void TestConcurrencyInThreads()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var threads = new List<Thread>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var thread = new Thread(() =>
                {
                    var sc = new TestSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(sc);
                    sc.Post(async x =>
                    {
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            var initialValue = counter;
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                Assert.AreEqual(initialValue + i, counter);
                                Thread.Sleep(1);
                                ++counter;
                            }
                        }
                        sc.SignalEnd();
                    }, null);
                    sc.RunUntilEnd();
                }) { Name = $"Thread {j}" };
                thread.Start();
                threads.Add(thread);
            }
            threads.ForEach(x => x.Join());
            Assert.AreEqual(ThreadCount * IncrementCount, counter);
        }

        [Test, Timeout(5000)]
        public void TestSequentialLocksInThreads()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var threads = new List<Thread>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var thread = new Thread(() =>
                {
                    var sc = new TestSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(sc);
                    sc.Post(async x =>
                    {
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            var initialValue = counter;
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                Assert.AreEqual(initialValue + i, counter);
                                Thread.Sleep(1);
                                ++counter;
                            }
                        }
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            var initialValue = counter;
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                Assert.AreEqual(initialValue + i, counter);
                                Thread.Sleep(1);
                                ++counter;
                            }
                        }
                        sc.SignalEnd();
                    }, null);
                    sc.RunUntilEnd();
                }) { Name = $"Thread {j}" };
                thread.Start();
                threads.Add(thread);
            }
            threads.ForEach(x => x.Join());
            Assert.AreEqual(2 * ThreadCount * IncrementCount, counter);
        }

        [Test, Timeout(5000)]
        public void TestReentrancyInThreads()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var threads = new List<Thread>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var thread = new Thread(() =>
                {
                    var sc = new TestSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(sc);
                    sc.Post(async x =>
                    {
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            var initialValue = counter;
                            using ((await microThreadLock.ReserveSyncLock()).Lock())
                            {
                                for (var i = 0; i < IncrementCount; ++i)
                                {
                                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                                    {
                                        Assert.AreEqual(initialValue + i, counter);
                                    }
                                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                                    {
                                        Thread.Sleep(1);
                                    }
                                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                                    {
                                        ++counter;
                                    }
                                }
                            }
                        }
                        sc.SignalEnd();
                    }, null);
                    sc.RunUntilEnd();
                }) { Name = $"Thread {j}" };
                thread.Start();
                threads.Add(thread);
            }
            threads.ForEach(x => x.Join());
            Assert.AreEqual(ThreadCount * IncrementCount, counter);
        }

        [Test, Timeout(5000)]
        public void TestConcurrencyInTasks()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var tasks = new List<Task>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var task = Task.Run(async () =>
                {
                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.AreEqual(initialValue + i, counter);
                            //Thread.Sleep(1);
                            ++counter;
                        }
                    }
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(ThreadCount * IncrementCount, counter);
        }

        [Test, Timeout(5000)]
        public void TestSequentialLocksInTasks()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var tasks = new List<Task>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var task = Task.Run(async () =>
                {
                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.AreEqual(initialValue + i, counter);
                            Thread.Sleep(1);
                            ++counter;
                        }
                    }
                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.AreEqual(initialValue + i, counter);
                            Thread.Sleep(1);
                            ++counter;
                        }
                    }
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(2 * ThreadCount * IncrementCount, counter);
        }

        [Test, Timeout(5000)]
        public void TestReentrancyInTasks()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var tasks = new List<Task>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var task = Task.Run(async () =>
                {
                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                    {
                        var initialValue = counter;
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                using ((await microThreadLock.ReserveSyncLock()).Lock())
                                {
                                    Assert.AreEqual(initialValue + i, counter);
                                }
                                using ((await microThreadLock.ReserveSyncLock()).Lock())
                                {
                                    Thread.Sleep(1);
                                }
                                using ((await microThreadLock.ReserveSyncLock()).Lock())
                                {
                                    ++counter;
                                }
                            }
                        }
                    }
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(ThreadCount * IncrementCount, counter);
        }

        [Test, Timeout(5000)]
        public void TestConcurrencyInThreadsAndMicrothreads()
        {
            var scheduler = new Scheduler();
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            for (var j = 0; j < ThreadCount; ++j)
            {
                var microThread = scheduler.Create();
                microThread.Start(async () =>
                {
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.AreEqual(initialValue + i, counter);
                            await Task.Yield();
                            ++counter;
                        }
                    }
                });
            }
            var threads = new List<Thread>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var thread = new Thread(() =>
                {
                    var sc = new TestSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(sc);
                    sc.Post(async x =>
                    {
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            var initialValue = counter;
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                Assert.AreEqual(initialValue + i, counter);
                                Thread.Sleep(1);
                                ++counter;
                            }
                        }
                        sc.SignalEnd();
                    }, null);
                    sc.RunUntilEnd();
                })
                { Name = $"Thread {j}" };
                thread.Start();
                threads.Add(thread);
            }
            while (scheduler.MicroThreads.Count > 0)
            {
                scheduler.Run();
            }
            threads.ForEach(x => x.Join());
            Assert.AreEqual(2 * ThreadCount * IncrementCount, counter);
        }

        [Test, Timeout(5000)]
        public void TestConcurrencyInTasksAndMicrothreads()
        {
            var scheduler = new Scheduler();
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            for (var j = 0; j < ThreadCount; ++j)
            {
                var microThread = scheduler.Create();
                microThread.Start(async () =>
                {
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.AreEqual(initialValue + i, counter);
                            await Task.Yield();
                            ++counter;
                        }
                    }
                });
            }
            var tasks = new List<Task>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var task = Task.Run(async () =>
                {
                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.AreEqual(initialValue + i, counter);
                            Thread.Sleep(1);
                            ++counter;
                        }
                    }
                });
                tasks.Add(task);
            }
            while (scheduler.MicroThreads.Count > 0)
            {
                scheduler.Run();
            }
            Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(2 * ThreadCount * IncrementCount, counter);
        }


        /// <summary>
        /// A very basic dispatcher implementation for our unit tests.
        /// </summary>
        private class TestSynchronizationContext : SynchronizationContext
        {
            private readonly List<Tuple<SendOrPostCallback, object>> continuations = new List<Tuple<SendOrPostCallback, object>>();
            private bool ended;

            public override void Post(SendOrPostCallback d, object state)
            {
                lock (continuations)
                {
                    continuations.Add(Tuple.Create(d, state));
                }
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException();
            }

            public void RunUntilEnd()
            {
                while (!ended)
                {
                    List<Tuple<SendOrPostCallback, object>> localCopy;
                    lock (continuations)
                    {
                        localCopy = continuations.ToList();
                        continuations.Clear();
                    }
                    foreach (var continuation in localCopy)
                    {
                        continuation.Item1.Invoke(continuation.Item2);
                    }
                    Thread.Sleep(1);
                }
            }

            public void SignalEnd() => ended = true;
        }
    }
}
