using System.Collections.Concurrent;

namespace Concurency {
    class CustomBlockingQueue {
        private readonly ConcurrentQueue<int> _queue = new();
        private readonly int _capacity;
        private readonly SemaphoreSlim SpaceAvailable;
        private readonly SemaphoreSlim ItemsAvailable;
        private bool _isAddingCompleted = false;
        private readonly object _lock = new object();

        public CustomBlockingQueue(int capacity) {
            _capacity = capacity;
            SpaceAvailable = new(_capacity, _capacity);
            ItemsAvailable = new(0, _capacity);
        }

        public void Add(int item, CancellationToken token) {
            if (_isAddingCompleted) throw new InvalidOperationException("Adding is complete.");
            SpaceAvailable.Wait(token);
            lock (_lock) {
                _queue.Enqueue(item);
                ItemsAvailable.Release();
            }
        }

        public bool TryTake(out int item, CancellationToken token) {
            item = default;
            if (_isAddingCompleted && _queue.IsEmpty) return false;
            ItemsAvailable.Wait(token);
            lock (_lock) {
                bool res = _queue.TryDequeue(out item);
                if (res) SpaceAvailable.Release();
                return res;
            }
        }

        public void CompleteAdding() {
            _isAddingCompleted = true;
        }
    }

    class Program {
        public static async Task Main() {
            var queue = new CustomBlockingQueue(3);
            using CancellationTokenSource cts = new();
            ConcurrentQueue<int> allEvents = new();
            for (int i = 0; i < 10; i++) {
                allEvents.Enqueue(i);
            }

            // Create producers and consumers
            var producers = new[]
            {
                Task.Run(() => Produce(queue, allEvents, cts.Token)),
                Task.Run(() => Produce(queue, allEvents, cts.Token)),
                Task.Run(() => Produce(queue, allEvents, cts.Token))
            };

            var consumers = new[]
            {
                Task.Run(() => Consume(queue, cts.Token)),
                Task.Run(() => Consume(queue, cts.Token)),
                Task.Run(() => Consume(queue, cts.Token)),
                Task.Run(() => Consume(queue, cts.Token))
            };

            try {
                await Task.WhenAll(producers)
                    .ContinueWith(_ => queue.CompleteAdding(), TaskContinuationOptions.ExecuteSynchronously);
                await Task.WhenAll(consumers);
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
                cts.Cancel();
                throw;
            }
        }

        private static void Produce(CustomBlockingQueue queue, ConcurrentQueue<int> allEvents, CancellationToken token) {
            while (allEvents.TryDequeue(out int value) && !token.IsCancellationRequested) {
                queue.Add(value, token);
                Console.WriteLine($"Produced {value}, Thread: {Environment.CurrentManagedThreadId}");
                Thread.Sleep(new Random().Next(200, 1000));
            }
        }

        private static void Consume(CustomBlockingQueue queue, CancellationToken token) {
            while (true && !token.IsCancellationRequested) {
                if (queue.TryTake(out int item, token)) {
                    Console.WriteLine($"\t\t\t Consumed {item}, Thread: {Environment.CurrentManagedThreadId}");
                    Thread.Sleep(new Random().Next(1000, 2000));
                } else {
                    break; // Exit if no more items and adding is complete
                }
            }
        }
    }
}