using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefactoringTask5 {
    // ЗАДАЧА 5: Асинхронная система с deadlock и проблемами производительности
    // Проблемы: deadlock, неправильное использование async/await, 
    // проблемы с контекстом синхронизации, утечки ресурсов

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class AsyncDataService {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _semaphore;
        private readonly object _lockObject = new object();
        private List<ProcessedData> _cache = new List<ProcessedData>();

        public AsyncDataService() {
            // Проблема: HttpClient создается без using и не disposed
            _httpClient = new HttpClient();
            _semaphore = new SemaphoreSlim(5, 5);
        }

        // Проблема: async void вместо async Task
        public async void ProcessDataAsync(List<string> urls) {
            foreach (var url in urls) {
                // Проблема: последовательная обработка вместо параллельной
                await ProcessSingleUrlAsync(url);
            }
        }

        // Проблема: смешивание sync и async кода, potential deadlock
        public string ProcessDataSync(string url) {
            // Проблема: .Result на async операции может вызвать deadlock
            var task = ProcessSingleUrlAsync(url);
            var result = task.Result;

            return result?.Content ?? "No data";
        }

        // Проблема: неправильное использование ConfigureAwait
        public async Task<ProcessedData> ProcessSingleUrlAsync(string url) {
            await _semaphore.WaitAsync();

            try {
                // Проблема: отсутствует ConfigureAwait(false) в библиотечном коде
                var response = await _httpClient.GetStringAsync(url);

                // Проблема: sync операция в async методе
                var processedData = ProcessResponse(response);

                // Проблема: lock в async методе
                lock (_lockObject) {
                    _cache.Add(processedData);
                }

                // Проблема: async операция внутри lock
                await SaveToFileAsync(processedData);

                return processedData;
            } finally {
                _semaphore.Release();
            }
        }

        // Проблема: CPU-bound операция не помечена как async, но содержит Task.Delay
        private ProcessedData ProcessResponse(string response) {
            // Проблема: Task.Delay в sync методе
            Task.Delay(100).Wait(); // Имитация обработки

            return new ProcessedData {
                Content = response?.Substring(0, Math.Min(response.Length, 100)),
                ProcessedAt = DateTime.Now
            };
        }

        // Проблема: async операция с файлом, но используется sync API
        private async Task SaveToFileAsync(ProcessedData data) {
            var fileName = $"data_{DateTime.Now.Ticks}.txt";

            // Проблема: sync file operations в async методе
            using (var writer = new StreamWriter(fileName)) {
                writer.WriteLine(data.Content);
                writer.Flush();
            }
        }

        // Проблема: неправильная обработка параллельных задач
        public async Task<List<ProcessedData>> ProcessMultipleUrlsAsync(List<string> urls) {
            var tasks = new List<Task<ProcessedData>>();

            foreach (var url in urls) {
                // Проблема: создание задач без ограничения параллелизма
                tasks.Add(ProcessSingleUrlAsync(url));
            }

            // Проблема: ждем все задачи, даже если некоторые упали
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        // Проблема: cancellation token не передается дальше
        public async Task<ProcessedData> ProcessWithTimeoutAsync(string url,
                                                                CancellationToken cancellationToken) {
            // Проблема: cancellationToken не используется
            using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30))) {
                // Проблема: не комбинируются токены отмены
                return await ProcessSingleUrlAsync(url);
            }
        }

        // Проблема: async методы в sync контексте
        public List<ProcessedData> GetCachedDataSync() {
            // Проблема: lock с async операциями в других местах
            lock (_lockObject) {
                // Проблема: возвращает mutable коллекцию
                return _cache.ToList();
            }
        }

        // Проблема: неправильная обработка исключений в async коде
        public async Task<bool> TryProcessDataAsync(string url) {
            try {
                var result = await ProcessSingleUrlAsync(url);
                return result != null;
            } catch {
                // Проблема: проглатывание всех исключений
                return false;
            }
        }
    }

    // Проблема: класс не immutable
    public class ProcessedData {
        public string Content { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    // Проблема: неправильная реализация async disposable
    public class ResourceManager : IDisposable {
        private readonly List<IDisposable> _resources = new List<IDisposable>();
        private readonly SemaphoreSlim _resourceSemaphore = new SemaphoreSlim(1, 1);

        // Проблема: async операции в Dispose
        public void Dispose() {
            // Проблема: async операция в sync Dispose
            DisposeAsync().Wait();
        }

        private async Task DisposeAsync() {
            await _resourceSemaphore.WaitAsync();

            try {
                foreach (var resource in _resources) {
                    resource?.Dispose();
                }
                _resources.Clear();
            } finally {
                _resourceSemaphore.Release();
                _resourceSemaphore.Dispose();
            }
        }

        // Проблема: async операция не awaitable
        public void AddResourceAsync(IDisposable resource) {
            // Проблема: fire-and-forget async операция
            Task.Run(async () =>
            {
                await _resourceSemaphore.WaitAsync();
                try {
                    _resources.Add(resource);
                } finally {
                    _resourceSemaphore.Release();
                }
            });
        }
    }

    // Проблема: неправильное использование TaskCompletionSource
    public class AsyncQueue<T> {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly Queue<TaskCompletionSource<T>> _waitingTasks =
            new Queue<TaskCompletionSource<T>>();
        private readonly object _lock = new object();

        public void Enqueue(T item) {
            TaskCompletionSource<T> tcs = null;

            lock (_lock) {
                if (_waitingTasks.Count > 0) {
                    tcs = _waitingTasks.Dequeue();
                } else {
                    _queue.Enqueue(item);
                }
            }

            // Проблема: SetResult может выполняться синхронно
            tcs?.SetResult(item);
        }

        public async Task<T> DequeueAsync() {
            lock (_lock) {
                if (_queue.Count > 0) {
                    return _queue.Dequeue();
                }

                var tcs = new TaskCompletionSource<T>();
                _waitingTasks.Enqueue(tcs);

                // Проблема: возвращаем Task из lock блока
                //return await tcs.Task;
                tcs.Task.Wait();
                return tcs.Task.Result;
            }
        }
    }

    // Проблема: неправильная координация между потоками
    public class DataCoordinator {
        private readonly AsyncQueue<WorkItem> _workQueue = new AsyncQueue<WorkItem>();
        private readonly List<Task> _workerTasks = new List<Task>();
        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();

        public void StartProcessing(int workerCount) {
            for (int i = 0; i < workerCount; i++) {
                // Проблема: не сохраняем ссылки на задачи для proper cleanup
                var workerTask = Task.Run(async () => await WorkerLoopAsync());
                _workerTasks.Add(workerTask);
            }
        }

        private async Task WorkerLoopAsync() {
            while (!_cancellationTokenSource.Token.IsCancellationRequested) {
                try {
                    // Проблема: нет timeout для DequeueAsync
                    var workItem = await _workQueue.DequeueAsync();

                    // Проблема: обработка может быть долгой, но cancellation token не передается
                    await ProcessWorkItemAsync(workItem);
                } catch (Exception ex) {
                    // Проблема: проглатывание исключений в worker loop
                    Console.WriteLine($"Error in worker: {ex.Message}");
                }
            }
        }

        private async Task ProcessWorkItemAsync(WorkItem item) {
            // Проблема: sync delay в async методе
            Thread.Sleep(item.ProcessingTimeMs);

            item.IsCompleted = true;
            item.CompletedAt = DateTime.Now;
        }

        // Проблема: неправильное завершение async операций
        public void Stop() {
            _cancellationTokenSource.Cancel();

            // Проблема: блокирующее ожидание async операций
            Task.WaitAll(_workerTasks.ToArray(), TimeSpan.FromSeconds(5));
        }
    }

    public class WorkItem {
        public string Id { get; set; }
        public string Data { get; set; }
        public int ProcessingTimeMs { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    class Program {
        static async Task Main(string[] args) {
            var dataService = new AsyncDataService();
            var urls = new List<string>
            {
            "https://api.example1.com/data",
            "https://api.example2.com/data",
            "https://api.example3.com/data"
        };

            // Проблема: смешивание sync и async вызовов
            dataService.ProcessDataAsync(urls); // fire-and-forget

            // Проблема: potential deadlock в sync контексте
            var syncResult = dataService.ProcessDataSync(urls.First());

            // Проблема: нет обработки исключений
            var asyncResults = await dataService.ProcessMultipleUrlsAsync(urls);

            Console.WriteLine($"Processed {asyncResults.Count} URLs");

            // Проблема: ресурсы не освобождаются properly
            var coordinator = new DataCoordinator();
            coordinator.StartProcessing(3);

            // Добавляем работу
            var queue = new AsyncQueue<WorkItem>();
            for (int i = 0; i < 10; i++) {
                queue.Enqueue(new WorkItem {
                    Id = $"work_{i}",
                    Data = $"data_{i}",
                    ProcessingTimeMs = 1000
                });
            }

            await Task.Delay(5000);
            coordinator.Stop();
        }
    }

    /*
    ЗАДАНИЯ ДЛЯ РЕФАКТОРИНГА:

    1. Устраните все potential deadlocks
    2. Исправьте неправильное использование async/await
    3. Добавьте правильную обработку CancellationToken
    4. Реализуйте правильное управление ресурсами для async операций
    5. Исправьте проблемы с ConfigureAwait
    6. Замените async void на async Task
    7. Оптимизируйте параллельную обработку
    8. Исправьте проблемы с TaskCompletionSource
    9. Добавьте proper exception handling в async коде
    10. Реализуйте правильный AsyncDisposable паттерн
    11. Исправьте координацию между потоками
    12. Добавьте timeout и retry механизмы

    БОНУС: Реализуйте backpressure для очереди и circuit breaker для HTTP вызовов
    */
}
