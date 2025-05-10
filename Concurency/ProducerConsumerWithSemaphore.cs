using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurency {
    class QueueWithSemaphore(SemaphoreSlim items, SemaphoreSlim space) {
        private SemaphoreSlim ItemsAvailable = items; 
        private SemaphoreSlim SpaceAvailable = space;
        private ConcurrentQueue<int> queue = new();
        public bool IsFinished { get; set; } = false;

        public async Task<bool> AddAsync(int x) {
            await SpaceAvailable.WaitAsync();
            queue.Enqueue(x);
            ItemsAvailable.Release();
            return true;
        }

        public async Task<int?> DequeueAsync() {
            if (IsFinished && queue.Count == 0) return null;
            await ItemsAvailable.WaitAsync();
            bool res = queue.TryDequeue(out int value);
            if (res) {
                SpaceAvailable.Release();
                return value;
            }
            return null;
        }
    }
    class ProducerWithSemaphore(ConcurrentQueue<int> allEvents, QueueWithSemaphore queue) {
        private readonly ConcurrentQueue<int> AllEvents = allEvents;
        private readonly QueueWithSemaphore SemaphoreQueue = queue;
        private readonly Random Rand = new();

        public async Task Produce(CancellationToken token) {
            while(true) {
                await Task.Delay(Rand.Next(200, 500), token);
                if (AllEvents.TryDequeue(out int value)) {
                    await SemaphoreQueue.AddAsync(value);
                    Console.WriteLine($"~~ Producer Value = {value}, Thread = {Environment.CurrentManagedThreadId}");
                } else {
                    break;
                }
            }
        }
    }

    class ConsumerWithSemaphore(QueueWithSemaphore queue) {
        private readonly QueueWithSemaphore SenaphoreQueue = queue;
        private readonly Random Rand = new();

        public async Task Consume(CancellationToken token) {
            while (true) {
                await Task.Delay(Rand.Next(500, 1000), token);
                int? x = await SenaphoreQueue.DequeueAsync();
                if (x.HasValue) {
                    Console.WriteLine($"\t\t\t\t -- Consumer Value = {x}, Thread = {Environment.CurrentManagedThreadId}");
                } else {
                    break;
                }
            }
        }
    }

    internal class ProducerConsumerWithSemaphore {
        public static async Task Main() {
            int maxInQueue = 3;
            SemaphoreSlim spaceSemaphore = new(maxInQueue, maxInQueue);
            SemaphoreSlim itemsSemaphore = new(0);
            QueueWithSemaphore queue = new(itemsSemaphore, spaceSemaphore);
            ConcurrentQueue<int> allEvents = new ConcurrentQueue<int>([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

            ProducerWithSemaphore producer = new(allEvents, queue);
            ConsumerWithSemaphore consumer = new(queue);
            using CancellationTokenSource cts = new();
            try {
                await Task.WhenAll([
                    Task.Run(() => producer.Produce(cts.Token)).ContinueWith(_ => queue.IsFinished = true, TaskContinuationOptions.ExecuteSynchronously),
                    Task.Run(() => consumer.Consume(cts.Token)),
                ]);
            } catch(Exception ex) {
                Console.WriteLine(ex);
                cts.Cancel();
            }
        }
    }
}
