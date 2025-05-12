namespace Concurency {

    internal class IncClass {
        
        bool isFinished = false;
        int count = 0;
        int maxCount = 200*1000*1000;
        int sum = 0;
        object _lock = new object();
        
        void Increment() {
            while (true) {
                //Thread.Sleep(new Random().Next(100, 300));
                lock (_lock) {
                    if (isFinished) return;
                    count++;
                    sum += 1;
                    //Console.WriteLine($"Count = {count} in the thread {Environment.CurrentManagedThreadId}");
                    if (count == maxCount) isFinished = true;
                }
            }
        }

        internal void Test() {
            Console.WriteLine($"At start count = {count}");
            Task.WaitAll([
                Task.Run(Increment), Task.Run(Increment), Task.Run(Increment), Task.Run(Increment),
            ]);
            Console.WriteLine($"At the end count = {count}, sum = {sum}");
        }
    }

    internal class ThreadSafeOperations {

        static void Main() {
            new IncClass().Test();
        }
    }
}
