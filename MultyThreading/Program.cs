using System.Collections.Concurrent;

namespace MultyThreading {
    internal class Program {
        private static void Main(string[] args) {
            TestCacheManager();
            //VolatileExample ve = new();
            //ve.Start();

            int counter = 0;
            List<Task> tasks = [];
            for (int i = 0; i < 1000000; i++) {
                tasks.Add(Task.Run(() => Interlocked.Increment(ref counter)));
            }
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine(counter);
        }

        private static void TestCacheManager() {
            CacheManager cacheManager = new();
            //CacheManager_Refactored2 cacheManager = new();
            List<Task> allTasks = [];
            for (int i = 0; i < 100000; ++i) {
                Console.WriteLine(i);
                if (i % 100 == 50) {
                    allTasks.Add(Task.Run(() => cacheManager.ClearCache()));
                } else {
                    allTasks.Add(Task.Run(() => cacheManager.GetValue("A")));
                }
            }
            Task.WaitAll(allTasks.ToArray());
        }

        /*Представь, что у нас есть класс Counter с методом Increment(), 
             * который увеличивает целое поле count на единицу. 
             * Этот метод может вызываться из разных потоков. 
             * Как ты реализуешь этот класс, чтобы count всегда имел корректное значение?
             */
        internal class Counter {
            private int _count = 0;
            public int Count => _count;
            public void Increment() {
                Interlocked.Increment(ref _count);
            }
        }

        internal class Counter1 {
            private int _count = 0;
            private object _lock = new();
            public int Count => _count;
            public void Increment() {
                lock (_lock) {
                    ++_count;
                }
            }
        }

        /*
         У нас есть очередь заданий Queue<Action> _jobs. Несколько потоков добавляют задания в очередь, несколько — достают и выполняют.
         */
        internal class ThreadSafeQueue {
            private Queue<Action> _jobs = [];
            private object _lock = new();
            public bool TryGetAction(out Action? action) {
                lock (_lock) {
                    return _jobs.TryDequeue(out action);
                }
            }
            public void AddAction(Action action) {
                lock (_lock) {
                    _jobs.Enqueue(action);
                }
            }
        }
        internal class ThreadSafeConcurrentQueue {
            private ConcurrentQueue<Action> _jobs = [];
            public Action GetAction() {
                if (_jobs.TryDequeue(out Action? result)) {
                    return result;
                } else {
                    throw new Exception("Queue is empty");
                }
            }
            public bool TryGetAction(out Action? action) {
                return _jobs.TryDequeue(out action);
            }
            public void AddAction(Action action) {
                _jobs.Enqueue(action);
            }
        }
        internal class JobQueue {
            private readonly ConcurrentQueue<Action> _jobs = new();
            private readonly SemaphoreSlim _signal = new(0);

            public void AddAction(Action action) {
                _jobs.Enqueue(action);
                _signal.Release();
            }

            public async Task<Action> GetActionAsync(CancellationToken token) {
                await _signal.WaitAsync(token);
                _jobs.TryDequeue(out var job);
                return job!;
            }
        }
    }

    public class OrderManager {
        private List<string> _orders = new List<string>();

        public void AddOrder(string order) {
            _orders.Add(order);
        }

        public List<string> GetOrders() {
            return _orders;
        }
    }

    public class OrderManager_Refactored {
        private readonly List<string> _orders = new List<string>();
        private readonly object _lock = new object();

        public void AddOrder(string order) {
            lock (_lock) {
                _orders.Add(order);
            }
        }

        public IReadOnlyList<string> GetOrders() {
            lock (_lock) {
                return _orders.AsReadOnly();
            }
        }
    }


    /*Какие проблемы возникнут при использовании этого кода в многопоточной среде? 
     * Укажи все потенциальные race conditions.
     * Отрефактори этот код, чтобы он стал потокобезопасным. 
     * Используй подходящий примитив синхронизации и объясни, почему ты выбрал именно его.
     * 
     * Ловушка: Если ты используешь lock для синхронизации, может ли возникнуть deadlock в этой системе? 
     * Если да, как его избежать? Если нет, почему?*/
    public class OrderProcessor {
        private List<string> _orders = new List<string>();
        private int _processedCount = 0;

        public void AddOrder(string order) {
            _orders.Add(order);
        }

        public void ProcessOrders() {
            _processedCount += _orders.Count;
            _orders.Clear();
        }

        public int GetProcessedCount() {
            return _processedCount;
        }
    }

    public class OrderProcessor_Refactored {
        private readonly List<string> _orders = new List<string>();
        private int _processedCount = 0;
        private object _lock = new object();

        public void AddOrder(string order) {
            lock (_lock) {
                _orders.Add(order);
            }
        }

        public void ProcessOrders() {
            Monitor.Enter(_lock); 
            try{
                _processedCount += _orders.Count;
                _orders.Clear();
            } finally{ Monitor.Exit(_lock);}
        }

        public int GetProcessedCount() {
            lock (_lock) {
                return _processedCount;
            }
        }
    }

    public class OrderProcessor_Refactored2_Wrong {
        private readonly ConcurrentBag<string> _orders = new ConcurrentBag<string>();
        private int _processedCount = 0;

        public void AddOrder(string order) {
                _orders.Add(order);
        }

        public void ProcessOrders() {
                Interlocked.Add(ref _processedCount, _orders.Count);
                _orders.Clear();
        }

        public int GetProcessedCount() {
                return _processedCount;
        }
    }

    public class AsyncOrderProcessor {
        private List<string> _orders = [];
        
        public async Task AddOrderAsync(string order) {
            _orders.Add(order);
            await Task.Delay(100);
        }

        public async Task<int> ProcessOrdersAsync() {
            int count = _orders.Count;
            _orders.Clear();
            await Task.Delay(200);
            return count;
        }
    }

    public class AsyncOrderProcessor_Refactored {
        private readonly List<string> _orders = [];
        private SemaphoreSlim slim = new(1, 1);

        public async Task AddOrderAsync(string order) {
            await slim.WaitAsync();
            _orders.Add(order);
            await Task.Delay(100);
            slim.Release();
        }

        public async Task<int> ProcessOrdersAsync() {
            await slim.WaitAsync();
            int count = _orders.Count;
            _orders.Clear();
            await Task.Delay(200);
            slim.Release();
            return count;
        }
    }

    public class AsyncOrderProcessor_Refactored2 {
        private readonly List<string> _orders = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task AddOrderAsync(string order) {
            await _semaphore.WaitAsync();
            try {
                _orders.Add(order);
                await Task.Delay(100);
            } finally {
                _semaphore.Release();
            }
        }

        public async Task<int> ProcessOrdersAsync() {
            await _semaphore.WaitAsync();
            try {
                int count = _orders.Count;
                _orders.Clear();
                await Task.Delay(200);
                return count;
            } finally {
                _semaphore.Release();
            }
        }
    }

    

}