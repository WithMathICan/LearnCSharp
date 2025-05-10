using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Producer(IEnumerable<int> arr, int maxInQueue) {
    private readonly Stack<int> Data = new(arr ?? []);
    private readonly Random Rand = new Random();
    private readonly int MaxInQueue = maxInQueue;
    public event EventHandler ProducingFinished;
    public bool IsProducingFinished { private set; get; } = false;

    public async Task Produce(ConcurrentQueue<int> queue) {
        while (Data.Count > 0) {
            int randomDelay = Rand.Next(200, 2000);
            await Task.Delay(randomDelay);
            if (queue.Count < MaxInQueue) {
                int elem = Data.Pop();
                queue.Enqueue(elem);
                Console.WriteLine($"~~ Producer ~~ element = {elem}");
            } else {
                Console.WriteLine("!! Queue is full ");
            }
        }
        Console.WriteLine("~~ Producing is finished");
        ProducingFinished?.Invoke(this, EventArgs.Empty);
    }
}

class  Consumer : IDisposable  {
    private bool IsProducingFinished = false;
    private readonly Random Rand = new Random();
    Producer Producer;

    public Consumer(Producer producer) {
        Producer = producer;
        Producer.ProducingFinished += SetProducingFinished;
    }

    private void SetProducingFinished(object? sender, EventArgs e) {
        IsProducingFinished = true;
    }

    public async Task Consume(ConcurrentQueue<int> queue) {
        while(!IsProducingFinished || queue.Count > 0) {
            int randomDelay = Rand.Next(1200, 2000);
            await Task.Delay(randomDelay);
            if (queue.TryDequeue(out int elem)) {
                Console.WriteLine($"-- Consumer -- element = {elem}");
            }
        }
        Console.WriteLine("-- Consuming is finished");
    }

    public void Dispose() {
        Producer.ProducingFinished -= SetProducingFinished;
    }
}

class ProducerConsumer() {
    static async Task Main() {
        ConcurrentQueue<int> queue = new();
        Producer producer = new([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], 3);
        Consumer consumer = new Consumer(producer);
        Task t1 = Task.Run(async () => await producer.Produce(queue));
        Task t2 = Task.Run(async () => {
            using (consumer) {
                await consumer.Consume(queue);
            } 
        });
        await Task.WhenAll(t1, t2);
    }
}
