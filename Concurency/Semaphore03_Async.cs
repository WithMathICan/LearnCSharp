using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurency {
    internal sealed class Semaphore03_Async(int initialCount, int maxCount) : IDisposable {
        private int _count = initialCount;
        private readonly int _maxCount = maxCount;
        private readonly object _lock = new();
        private readonly Queue<TaskCompletionSource<bool>> _waiters = new Queue<TaskCompletionSource<bool>>();
        private bool _disposed;

        public int CurrentCount { 
            get {
                lock (_lock) {
                    return _count;
                }
            } 
        }

        internal void Wait() {
            lock (_lock) {
                if (_disposed) throw new ObjectDisposedException(nameof(Semaphore03_Async));
                while (_count <= 0) {
                    Monitor.Wait(_lock); // Block if no slots available
                }
                _count--;
            }
        }

        internal async Task WaitAsync() {
            TaskCompletionSource<bool> tcs;
            lock (_lock) {
                if (_disposed) throw new ObjectDisposedException(nameof(Semaphore03_Async));
                if (_count > 0) {
                    _count--;
                    return;
                }
                tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Enqueue(tcs);
            }
            await tcs.Task.ConfigureAwait(false);
        }

        internal void Release() {
            lock (_lock) {
                if (_disposed) throw new ObjectDisposedException(nameof(Semaphore03_Async));
                if (_count < _maxCount) {
                    
                    if (_waiters.Count > 0) {
                        var tcs = _waiters.Dequeue();
                        tcs.TrySetResult(true);
                    } else {
                        _count++;
                        Monitor.Pulse(_lock);
                    }
                }
            }
        }
        
        public void Dispose() {
            lock (_lock) {
                if (_disposed) return;
                _disposed = true;
                while (_waiters.Count > 0) {
                    var tcs = _waiters.Dequeue();
                    tcs.TrySetException(new ObjectDisposedException(nameof(Semaphore03_Async)));
                }
                Monitor.PulseAll(_lock);
            }
        }

        static async Task Main1() {
            using var semaphore = new Semaphore03_Async(2, 5);
            //using var semaphore = new SemaphoreSlim(2, 5);
            semaphore.Wait();
            Console.WriteLine($"CurrentCount: {semaphore.CurrentCount}");
            await semaphore.WaitAsync();
            Console.WriteLine($"CurrentCount: {semaphore.CurrentCount}");
            Task t1 = Task.Run(() => semaphore.WaitAsync());
            Task t2 = Task.Run(() => semaphore.Release());
            await Task.WhenAll(t1, t2);
            Console.WriteLine($"CurrentCount: {semaphore.CurrentCount}");
            semaphore.Release();
            Console.WriteLine($"CurrentCount: {semaphore.CurrentCount}");
            await Task.Run(() => semaphore.WaitAsync());
            Console.WriteLine($"CurrentCount: {semaphore.CurrentCount}");

            //Task[] tasks = new Task[5];

            //for (int i = 0; i < 5; i++) {
            //    int id = i;
            //    tasks[i] = Task.Run(async () => {
            //        Console.WriteLine($"Task {id} waiting...");
            //        await semaphore.WaitAsync();
            //        Console.WriteLine($"Task {id} entered! CurrentCount: {semaphore.CurrentCount}");
            //        await Task.Delay(1000);
            //        Console.WriteLine($"Task {id} releasing...");
            //        semaphore.Release();
            //    });
            //}

            //await Task.WhenAll(tasks);
            Console.WriteLine($"All tasks completed. CurrentCount: {semaphore.CurrentCount}");
        }
    }


}
