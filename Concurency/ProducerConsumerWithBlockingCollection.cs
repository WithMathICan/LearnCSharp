using System.Collections.Concurrent;

namespace Concurency {
    class BlockingProducer(ConcurrentQueue<int> events, BlockingCollection<int> eventsQueue) {
        private ConcurrentQueue<int> Events = events;
        private BlockingCollection<int> EventsQueue = eventsQueue;
        private readonly Random Rand = new Random();

        public async Task Produce(CancellationToken token) {
            while (true) {
                await Task.Delay(Rand.Next(200, 1000), token);
                if (Events.TryDequeue(out int value)) {
                    EventsQueue.Add(value);
                    Console.WriteLine($"~~ Producer Value = {value}, Thread = {Thread.CurrentThread.ManagedThreadId}");
                } else {
                    break;
                }
            }
        }
    }

    class BlockingConsumer(BlockingCollection<int> eventsQueue) {
        private BlockingCollection<int> EventsQueue = eventsQueue;
        private readonly Random Rand = new Random();

        public async Task Consume(CancellationToken token) {
            foreach (var elem in EventsQueue.GetConsumingEnumerable(token)) {
                Console.WriteLine($"\t\t\t\t -- Consumer Value = {elem}, Thread = {Thread.CurrentThread.ManagedThreadId}");
                await Task.Delay(Rand.Next(1000, 2000));
            }
        }
    }

    internal class ProducerConsumerWithBlockingCollection {
        public static async Task Main() {
            using BlockingCollection<int> eventsQueue = new(3);
            ConcurrentQueue<int> allEvents = new();
            using CancellationTokenSource cts = new();
            for (int i = 0; i < 10; i++) {
                allEvents.Enqueue(i);
            }

            BlockingProducer[] producers = [new(allEvents, eventsQueue), new(allEvents, eventsQueue), new(allEvents, eventsQueue)];
            BlockingConsumer[] consumers = [new(eventsQueue), new(eventsQueue), new(eventsQueue), new(eventsQueue)];

            //try {
            //    Task producerTask = Task.Run(async () => {
            //        await Task.WhenAll(producers.Select(p => p.Produce(cts.Token)));
            //        eventsQueue.CompleteAdding();
            //    }, cts.Token);
            //    Task consumerTask = Task.WhenAll(consumers.Select(c => Task.Run(async () => await c.Consume(cts.Token), cts.Token)));
            //    await Task.WhenAll(producerTask, consumerTask);
            //} catch (Exception ex) {
            //    Console.WriteLine($"Error occurred: {ex.Message}");
            //    cts.Cancel();
            //}

            try {
                // Start all producer and consumer tasks concurrently
                Task producerTask = Task.WhenAll(producers.Select(p => p.Produce(cts.Token)))
                    .ContinueWith(_ => eventsQueue.CompleteAdding(), TaskContinuationOptions.ExecuteSynchronously);
                Task consumerTask = Task.WhenAll(consumers.Select(c => c.Consume(cts.Token)));
                await Task.WhenAll(producerTask, consumerTask);
            } catch (Exception ex) {
                Console.WriteLine($"Error occurred: {ex.Message}");
                cts.Cancel();
                throw; // Re-throw to ensure the program fails appropriately
            }

        }
    }
}
