using System;
using System.Collections.Generic;
using System.Threading;

namespace Concurency {
   

    class ProducerConsumerMonitor {
        private static readonly Queue<int> _queue = new Queue<int>();
        private static readonly object _lock = new object();
        private static bool _isCompleted = false;

        public static void Main1() {
            Thread producer = new Thread(Produce);
            Thread consumer = new Thread(Consume);
            producer.Start();
            consumer.Start();
            producer.Join();
            consumer.Join();
        }

        private static void Produce() {
            for (int i = 1; i <= 5; i++) {
                lock (_lock) {
                    _queue.Enqueue(i);
                    Console.WriteLine($"Produced: {i}");
                    Monitor.Pulse(_lock); // Signal the consumer
                }
                Thread.Sleep(500); // Simulate work
            }
            lock (_lock) {
                _isCompleted = true;
                Monitor.Pulse(_lock); // Signal consumer to check completion
            }
        }

        private static void Consume() {
            while (true) {
                lock (_lock) {
                    // Wait if the queue is empty and not completed
                    while (_queue.Count == 0 && !_isCompleted) {
                        Monitor.Wait(_lock); // Release lock and wait for signal
                    }

                    if (_queue.Count == 0 && _isCompleted) {
                        break; // Exit if completed and no more items
                    }

                    int item = _queue.Dequeue();
                    Console.WriteLine($"Consumed: {item}");
                }
                Thread.Sleep(1000); // Simulate work
            }
        }
    }
}
