using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurency {
    internal class ProducerConsumerMonitorAsync {

        static async Task Main() {
            int maxInQueue = 3;
            Queue<int> events = new();
            Queue<int> initialValues = new([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
            object _lock = new();
            bool isFinished = false;

            async Task Produce() {
                while (true) {
                    await Task.Delay(500);
                    lock (_lock) {
                        if (isFinished) {
                            Monitor.PulseAll(_lock);
                            return;
                        }
                        while (events.Count >= maxInQueue) {
                            Monitor.Wait(_lock);
                        }
                        if (initialValues.TryDequeue(out int value)) {
                            events.Enqueue(value);
                            Console.WriteLine($"Produce {value}");
                        } else {
                            isFinished = true;
                        }
                        Monitor.Pulse(_lock);
                    }
                }
            }

            async Task Consume() {
                while (true) {
                    await Task.Delay(800);
                    lock (_lock) {
                        if (isFinished && events.Count == 0) {
                            Monitor.PulseAll(_lock);
                            return;
                        }
                        while (events.Count == 0) {
                            Monitor.Wait(_lock);
                        }
                        if (events.TryDequeue(out int value)) {
                            Console.WriteLine($"\t\t Consume {value}");
                        }
                        Monitor.Pulse(_lock);
                    }
                }
            }

            Task producerTask = Task.WhenAll([
                Task.Run(Produce),
                Task.Run(Produce),
                Task.Run(Produce),
                Task.Run(Produce),
            ]);

            Task consumeTask = Task.WhenAll([
                Task.Run(Consume),
                Task.Run(Consume),
                Task.Run(Consume),
                Task.Run(Consume),
            ]);

            await Task.WhenAll(producerTask, consumeTask);
        }
    }
}
