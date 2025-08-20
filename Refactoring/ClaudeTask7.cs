using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefactoringTask7 {
    // ЗАДАЧА 7: Система пула ресурсов с circular wait deadlock'ами
    // Проблемы: circular wait, resource starvation, convoy effect,
    // priority inversion, nested resource acquisition

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public enum ResourceType {
        DatabaseConnection,
        FileHandle,
        NetworkSocket,
        MemoryBuffer,
        CriticalSection
    }

    // Проблема: ресурс не thread-safe, требует внешней синхронизации
    public class Resource : IDisposable {
        public string Id { get; }
        public ResourceType Type { get; }
        public DateTime CreatedAt { get; }
        public DateTime LastUsed { get; set; }
        public bool IsInUse { get; set; }
        public string CurrentOwner { get; set; }

        private bool _disposed = false;

        public Resource(string id, ResourceType type) {
            Id = id;
            Type = type;
            CreatedAt = DateTime.Now;
            LastUsed = DateTime.Now;
        }

        public void Use() {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Resource));

            LastUsed = DateTime.Now;
            // Имитация использования ресурса
            Thread.Sleep(10);
        }

        public void Dispose() {
            _disposed = true;
        }
    }

    // Проблема: может вызвать circular wait при запросе множественных ресурсов
    public class ResourcePool {
        private readonly Dictionary<ResourceType, Queue<Resource>> _availableResources;
        private readonly Dictionary<ResourceType, HashSet<Resource>> _allResources;
        private readonly Dictionary<ResourceType, object> _typeLocks;
        private readonly Dictionary<string, List<Resource>> _ownerToResources;
        private readonly object _ownershipLock = new object();
        private readonly SemaphoreSlim _globalSemaphore;

        public ResourcePool(int maxTotalResources = 50) {
            _availableResources = new Dictionary<ResourceType, Queue<Resource>>();
            _allResources = new Dictionary<ResourceType, HashSet<Resource>>();
            _typeLocks = new Dictionary<ResourceType, object>();
            _ownerToResources = new Dictionary<string, List<Resource>>();
            _globalSemaphore = new SemaphoreSlim(maxTotalResources, maxTotalResources);

            foreach (ResourceType type in Enum.GetValues<ResourceType>()) {
                _availableResources[type] = new Queue<Resource>();
                _allResources[type] = new HashSet<Resource>();
                _typeLocks[type] = new object();
            }

            InitializeResources();
        }

        private void InitializeResources() {
            // Создаем по 10 ресурсов каждого типа
            foreach (ResourceType type in Enum.GetValues<ResourceType>()) {
                for (int i = 0; i < 10; i++) {
                    var resource = new Resource($"{type}_{i}", type);
                    _availableResources[type].Enqueue(resource);
                    _allResources[type].Add(resource);
                }
            }
        }

        // Проблема: single resource acquisition может участвовать в circular wait
        public Resource AcquireResource(ResourceType type, string ownerId, int timeoutMs = 5000) {
            // Проблема: глобальный семафор перед type-specific lock может вызвать deadlock
            if (!_globalSemaphore.Wait(timeoutMs)) {
                throw new TimeoutException($"Failed to acquire global semaphore for {type}");
            }

            try {
                // Проблема: порядок взятия locks не определен
                lock (_typeLocks[type]) {
                    if (_availableResources[type].Count > 0) {
                        var resource = _availableResources[type].Dequeue();
                        resource.IsInUse = true;
                        resource.CurrentOwner = ownerId;

                        // Проблема: вложенный lock может вызвать deadlock
                        lock (_ownershipLock) {
                            if (!_ownerToResources.ContainsKey(ownerId))
                                _ownerToResources[ownerId] = new List<Resource>();

                            _ownerToResources[ownerId].Add(resource);
                        }

                        return resource;
                    }

                    throw new InvalidOperationException($"No available resources of type {type}");
                }
            } catch {
                _globalSemaphore.Release();
                throw;
            }
        }

        // Проблема: КРИТИЧНАЯ - может вызвать circular wait deadlock!
        public List<Resource> AcquireMultipleResources(List<ResourceType> types, string ownerId, int timeoutMs = 10000) {
            var acquiredResources = new List<Resource>();
            var locksTaken = new List<ResourceType>();

            try {
                // Проблема: берем locks в произвольном порядке!
                foreach (var type in types) {
                    // Проблема: если другой поток берет эти же типы в другом порядке - deadlock!
                    lock (_typeLocks[type]) {
                        locksTaken.Add(type);

                        if (_availableResources[type].Count > 0) {
                            var resource = _availableResources[type].Dequeue();
                            resource.IsInUse = true;
                            resource.CurrentOwner = ownerId;
                            acquiredResources.Add(resource);
                        } else {
                            // Проблема: частичный откат может оставить систему в inconsistent состоянии
                            throw new InvalidOperationException($"No available resources of type {type}");
                        }
                    }
                }

                // Проблема: берем ownership lock после type locks
                lock (_ownershipLock) {
                    if (!_ownerToResources.ContainsKey(ownerId))
                        _ownerToResources[ownerId] = new List<Resource>();

                    _ownerToResources[ownerId].AddRange(acquiredResources);
                }

                return acquiredResources;
            } catch {
                // Проблема: rollback может сам вызвать deadlock из-за порядка unlock'ов
                foreach (var resource in acquiredResources) {
                    ReleaseResource(resource, ownerId);
                }
                throw;
            }
        }

        public void ReleaseResource(Resource resource, string ownerId) {
            // Проблема: порядок locks отличается от AcquireResource
            lock (_ownershipLock) {
                if (_ownerToResources.ContainsKey(ownerId)) {
                    _ownerToResources[ownerId].Remove(resource);
                    if (_ownerToResources[ownerId].Count == 0) {
                        _ownerToResources.Remove(ownerId);
                    }
                }
            }

            lock (_typeLocks[resource.Type]) {
                resource.IsInUse = false;
                resource.CurrentOwner = null;
                _availableResources[resource.Type].Enqueue(resource);
            }

            _globalSemaphore.Release();
        }

        // Проблема: может вызвать deadlock при cleanup
        public void ForceReleaseAllResources(string ownerId) {
            List<Resource> resourcesToRelease;

            lock (_ownershipLock) {
                if (!_ownerToResources.TryGetValue(ownerId, out resourcesToRelease))
                    return;

                _ownerToResources.Remove(ownerId);
            }

            // Проблема: берем type locks после того как отпустили ownership lock
            foreach (var resource in resourcesToRelease) {
                lock (_typeLocks[resource.Type]) {
                    resource.IsInUse = false;
                    resource.CurrentOwner = null;
                    _availableResources[resource.Type].Enqueue(resource);
                    _globalSemaphore.Release();
                }
            }
        }

        // Проблема: может вызвать deadlock при получении статистики
        public Dictionary<ResourceType, int> GetResourceStatistics() {
            var stats = new Dictionary<ResourceType, int>();

            // Проблема: берем все type locks одновременно в произвольном порядке
            foreach (var type in Enum.GetValues<ResourceType>()) {
                lock (_typeLocks[type]) {
                    lock (_ownershipLock) // Проблема: nested lock в цикле
                    {
                        var inUseCount = _allResources[type].Count(r => r.IsInUse);
                        stats[type] = inUseCount;
                    }
                }
            }

            return stats;
        }
    }

    // Проблема: WorkflowEngine может создать circular dependencies
    public class WorkflowEngine {
        private readonly ResourcePool _resourcePool;
        private readonly Dictionary<string, WorkflowStep> _steps;
        private readonly Dictionary<string, object> _stepLocks;
        private readonly object _engineLock = new object();

        public WorkflowEngine(ResourcePool resourcePool) {
            _resourcePool = resourcePool;
            _steps = new Dictionary<string, WorkflowStep>();
            _stepLocks = new Dictionary<string, object>();
        }

        public void RegisterStep(string stepId, List<ResourceType> requiredResources,
                               List<string> dependsOnSteps = null) {
            lock (_engineLock) {
                var step = new WorkflowStep {
                    Id = stepId,
                    RequiredResources = requiredResources,
                    DependsOnSteps = dependsOnSteps ?? new List<string>(),
                    Status = StepStatus.Pending
                };

                _steps[stepId] = step;
                _stepLocks[stepId] = new object();
            }
        }

        // Проблема: может создать circular wait в dependency graph
        public async Task ExecuteWorkflowAsync(List<string> stepIds, string workflowId) {
            var executionTasks = new List<Task>();

            foreach (var stepId in stepIds) {
                executionTasks.Add(Task.Run(() => ExecuteStep(stepId, workflowId)));
            }

            await Task.WhenAll(executionTasks);
        }

        private void ExecuteStep(string stepId, string workflowId) {
            WorkflowStep step;

            lock (_engineLock) {
                if (!_steps.TryGetValue(stepId, out step))
                    throw new ArgumentException($"Step {stepId} not found");
            }

            // Проблема: берем lock на step перед проверкой dependencies
            lock (_stepLocks[stepId]) {
                if (step.Status != StepStatus.Pending)
                    return;

                // Проблема: ждем dependencies под lock'ом
                WaitForDependencies(step.DependsOnSteps, workflowId);

                step.Status = StepStatus.Running;

                try {
                    // Проблема: запрашиваем ресурсы под step lock
                    var resources = _resourcePool.AcquireMultipleResources(
                        step.RequiredResources, $"{workflowId}_{stepId}");

                    // Имитация работы
                    Thread.Sleep(1000);

                    // Освобождаем ресурсы
                    foreach (var resource in resources) {
                        _resourcePool.ReleaseResource(resource, $"{workflowId}_{stepId}");
                    }

                    step.Status = StepStatus.Completed;
                } catch (Exception ex) {
                    step.Status = StepStatus.Failed;
                    step.ErrorMessage = ex.Message;
                    throw;
                }
            }
        }

        // Проблема: может создать circular wait при ожидании dependencies
        private void WaitForDependencies(List<string> dependsOnSteps, string workflowId) {
            foreach (var dependencyStepId in dependsOnSteps) {
                // Проблема: берем locks на dependency steps в произвольном порядке
                lock (_stepLocks[dependencyStepId]) {
                    var dependencyStep = _steps[dependencyStepId];

                    // Проблема: busy waiting под lock'ом
                    while (dependencyStep.Status == StepStatus.Pending ||
                           dependencyStep.Status == StepStatus.Running) {
                        Monitor.Wait(_stepLocks[dependencyStepId], 100);
                    }

                    if (dependencyStep.Status == StepStatus.Failed) {
                        throw new InvalidOperationException(
                            $"Dependency step {dependencyStepId} failed: {dependencyStep.ErrorMessage}");
                    }
                }
            }
        }
    }

    public class WorkflowStep {
        public string Id { get; set; }
        public List<ResourceType> RequiredResources { get; set; }
        public List<string> DependsOnSteps { get; set; }
        public StepStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }

    public enum StepStatus {
        Pending,
        Running,
        Completed,
        Failed
    }

    // Проблема: Priority Inversion scenario
    public class PriorityResourceManager {
        private readonly ResourcePool _resourcePool;
        private readonly Dictionary<int, List<ResourceRequest>> _priorityQueues;
        private readonly object _queueLock = new object();
        private readonly AutoResetEvent _newRequestEvent = new AutoResetEvent(false);
        private volatile bool _isProcessing = true;

        public PriorityResourceManager(ResourcePool resourcePool) {
            _resourcePool = resourcePool;
            _priorityQueues = new Dictionary<int, List<ResourceRequest>>();

            // Инициализируем очереди приоритетов
            for (int i = 1; i <= 10; i++) {
                _priorityQueues[i] = new List<ResourceRequest>();
            }

            // Запускаем processor thread
            Task.Run(ProcessRequests);
        }

        public TaskCompletionSource<Resource> RequestResource(ResourceType type, string ownerId, int priority = 5) {
            var request = new ResourceRequest {
                Type = type,
                OwnerId = ownerId,
                Priority = priority,
                TaskCompletionSource = new TaskCompletionSource<Resource>(),
                RequestTime = DateTime.Now
            };

            lock (_queueLock) {
                _priorityQueues[priority].Add(request);
            }

            _newRequestEvent.Set();
            return request.TaskCompletionSource;
        }

        // Проблема: Priority Inversion - низкоприоритетная задача может блокировать высокоприоритетную
        private void ProcessRequests() {
            while (_isProcessing) {
                _newRequestEvent.WaitOne();

                ResourceRequest nextRequest = null;

                // Проблема: поиск высокоприоритетного запроса под lock'ом
                lock (_queueLock) {
                    // Ищем запрос с наивысшим приоритетом
                    for (int priority = 10; priority >= 1; priority--) {
                        if (_priorityQueues[priority].Count > 0) {
                            nextRequest = _priorityQueues[priority][0];
                            _priorityQueues[priority].RemoveAt(0);
                            break;
                        }
                    }
                }

                if (nextRequest != null) {
                    try {
                        // Проблема: высокоприоритетный запрос может ждать пока низкоприоритетный держит ресурс
                        var resource = _resourcePool.AcquireResource(nextRequest.Type, nextRequest.OwnerId, 30000);
                        nextRequest.TaskCompletionSource.SetResult(resource);
                    } catch (Exception ex) {
                        nextRequest.TaskCompletionSource.SetException(ex);
                    }
                }
            }
        }

        public void Stop() {
            _isProcessing = false;
            _newRequestEvent.Set();
        }
    }

    public class ResourceRequest {
        public ResourceType Type { get; set; }
        public string OwnerId { get; set; }
        public int Priority { get; set; }
        public DateTime RequestTime { get; set; }
        public TaskCompletionSource<Resource> TaskCompletionSource { get; set; }
    }

    // Проблема: Convoy Effect - все потоки выстраиваются за одним медленным
    public class BatchProcessor {
        private readonly ResourcePool _resourcePool;
        private readonly object _batchLock = new object();
        private readonly List<BatchItem> _currentBatch = new List<BatchItem>();
        private readonly int _batchSize = 10;

        public BatchProcessor(ResourcePool resourcePool) {
            _resourcePool = resourcePool;
        }

        public async Task<string> ProcessItemAsync(BatchItem item) {
            // Проблема: все потоки блокируются на одном lock'е
            lock (_batchLock) {
                _currentBatch.Add(item);

                if (_currentBatch.Count >= _batchSize) {
                    // Проблема: обработка batch'а под lock'ом блокирует все другие потоки
                    var batchToProcess = _currentBatch.ToList();
                    _currentBatch.Clear();

                    // Проблема: долгая операция под lock'ом
                    return ProcessBatch(batchToProcess);
                }

                return "Item queued for batch processing";
            }
        }

        private string ProcessBatch(List<BatchItem> batch) {
            var results = new List<string>();

            foreach (var item in batch) {
                // Проблема: каждый item требует ресурсы, что может вызвать deadlock
                var resourceTypes = new List<ResourceType> { ResourceType.DatabaseConnection, ResourceType.FileHandle };

                try {
                    var resources = _resourcePool.AcquireMultipleResources(resourceTypes, $"batch_{item.Id}");

                    // Имитация медленной обработки
                    Thread.Sleep(500);

                    results.Add($"Processed {item.Id}");

                    foreach (var resource in resources) {
                        _resourcePool.ReleaseResource(resource, $"batch_{item.Id}");
                    }
                } catch (Exception ex) {
                    results.Add($"Failed to process {item.Id}: {ex.Message}");
                }
            }

            return string.Join(", ", results);
        }
    }

    public class BatchItem {
        public string Id { get; set; }
        public string Data { get; set; }
    }

    // Проблема: Nested Resource Dependencies создают complex deadlock scenarios
    public class ComplexWorkflowManager {
        private readonly ResourcePool _resourcePool;
        private readonly WorkflowEngine _workflowEngine;
        private readonly PriorityResourceManager _priorityManager;
        private readonly Dictionary<string, ComplexWorkflow> _activeWorkflows;
        private readonly object _workflowsLock = new object();

        public ComplexWorkflowManager(ResourcePool resourcePool) {
            _resourcePool = resourcePool;
            _workflowEngine = new WorkflowEngine(resourcePool);
            _priorityManager = new PriorityResourceManager(resourcePool);
            _activeWorkflows = new Dictionary<string, ComplexWorkflow>();
        }

        // Проблема: может создать circular dependencies между workflows
        public async Task<string> ExecuteComplexWorkflowAsync(string workflowId,
                                                              List<WorkflowDefinition> definitions) {
            var workflow = new ComplexWorkflow {
                Id = workflowId,
                Status = WorkflowStatus.Running,
                StartTime = DateTime.Now
            };

            lock (_workflowsLock) {
                _activeWorkflows[workflowId] = workflow;
            }

            try {
                var tasks = new List<Task>();

                foreach (var definition in definitions) {
                    // Регистрируем steps в workflow engine
                    foreach (var step in definition.Steps) {
                        _workflowEngine.RegisterStep($"{workflowId}_{step.Id}",
                                                   step.RequiredResources,
                                                   step.Dependencies?.Select(d => $"{workflowId}_{d}").ToList());
                    }

                    // Проблема: параллельные workflows могут создать cross-dependencies
                    var stepIds = definition.Steps.Select(s => $"{workflowId}_{s.Id}").ToList();
                    tasks.Add(_workflowEngine.ExecuteWorkflowAsync(stepIds, workflowId));
                }

                await Task.WhenAll(tasks);

                workflow.Status = WorkflowStatus.Completed;
                workflow.EndTime = DateTime.Now;

                return $"Workflow {workflowId} completed successfully";
            } catch (Exception ex) {
                workflow.Status = WorkflowStatus.Failed;
                workflow.ErrorMessage = ex.Message;
                workflow.EndTime = DateTime.Now;

                // Проблема: cleanup может сам вызвать deadlock
                await CleanupWorkflowAsync(workflowId);

                throw;
            } finally {
                lock (_workflowsLock) {
                    _activeWorkflows.Remove(workflowId);
                }
            }
        }

        private async Task CleanupWorkflowAsync(string workflowId) {
            // Проблема: force release может конфликтовать с текущими операциями
            _resourcePool.ForceReleaseAllResources(workflowId);

            // Дополнительная cleanup логика...
        }
    }

    public class ComplexWorkflow {
        public string Id { get; set; }
        public WorkflowStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class WorkflowDefinition {
        public string Id { get; set; }
        public List<StepDefinition> Steps { get; set; }
    }

    public class StepDefinition {
        public string Id { get; set; }
        public List<ResourceType> RequiredResources { get; set; }
        public List<string> Dependencies { get; set; }
    }

    public enum WorkflowStatus {
        Pending,
        Running,
        Completed,
        Failed
    }

    class Program {
        static async Task Main() {
            var resourcePool = new ResourcePool(100);
            var workflowManager = new ComplexWorkflowManager(resourcePool);
            var batchProcessor = new BatchProcessor(resourcePool);

            Console.WriteLine("Starting deadlock-prone scenarios...");

            // Сценарий 1: Circular wait при multiple resource acquisition
            var deadlockTasks = new List<Task>();

            // Thread 1: Database -> File -> Network
            deadlockTasks.Add(Task.Run(async () =>
            {
                try {
                    var resources = resourcePool.AcquireMultipleResources(
                        new List<ResourceType> { ResourceType.DatabaseConnection, ResourceType.FileHandle, ResourceType.NetworkSocket },
                        "thread1");

                    await Task.Delay(2000);

                    foreach (var resource in resources)
                        resourcePool.ReleaseResource(resource, "thread1");
                } catch (Exception ex) {
                    Console.WriteLine($"Thread 1 error: {ex.Message}");
                }
            }));

            // Thread 2: Network -> File -> Database (обратный порядок!)
            deadlockTasks.Add(Task.Run(async () =>
            {
                await Task.Delay(100); // Небольшая задержка для увеличения вероятности deadlock

                try {
                    var resources = resourcePool.AcquireMultipleResources(
                        new List<ResourceType> { ResourceType.NetworkSocket, ResourceType.FileHandle, ResourceType.DatabaseConnection },
                        "thread2");

                    await Task.Delay(2000);

                    foreach (var resource in resources)
                        resourcePool.ReleaseResource(resource, "thread2");
                } catch (Exception ex) {
                    Console.WriteLine($"Thread 2 error: {ex.Message}");
                }
            }));

            // Сценарий 2: Complex workflow dependencies
            var workflowDefinitions = new List<WorkflowDefinition>
            {
            new WorkflowDefinition
            {
                Id = "workflow1",
                Steps = new List<StepDefinition>
                {
                    new StepDefinition
                    {
                        Id = "step1",
                        RequiredResources = new List<ResourceType> { ResourceType.DatabaseConnection, ResourceType.MemoryBuffer }
                    },
                    new StepDefinition
                    {
                        Id = "step2",
                        RequiredResources = new List<ResourceType> { ResourceType.FileHandle, ResourceType.CriticalSection },
                        Dependencies = new List<string> { "step1" }
                    }
                }
            }
        };

            deadlockTasks.Add(Task.Run(() => workflowManager.ExecuteComplexWorkflowAsync("workflow_a", workflowDefinitions)));
            deadlockTasks.Add(Task.Run(() => workflowManager.ExecuteComplexWorkflowAsync("workflow_b", workflowDefinitions)));

            // Сценарий 3: Batch processing convoy effect
            var batchTasks = new List<Task>();
            for (int i = 0; i < 20; i++) {
                var itemId = $"item_{i}";
                batchTasks.Add(Task.Run(() => batchProcessor.ProcessItemAsync(new BatchItem { Id = itemId, Data = $"data_{itemId}" })));
            }

            deadlockTasks.AddRange(batchTasks);

            try {
                // Проблема: может зависнуть навсегда из-за deadlock'ов
                await Task.WhenAll(deadlockTasks);
                Console.WriteLine("All operations completed successfully!");
            } catch (Exception ex) {
                Console.WriteLine($"Operations failed with error: {ex.Message}");
            }

            // Показываем статистику ресурсов
            try {
                var stats = resourcePool.GetResourceStatistics();
                foreach (var stat in stats) {
                    Console.WriteLine($"{stat.Key}: {stat.Value} resources in use");
                }
            } catch (Exception ex) {
                Console.WriteLine($"Failed to get statistics: {ex.Message}");
            }
        }
    }

    /*
    ТИПЫ DEADLOCK'ОВ В ЭТОЙ ЗАДАЧЕ:

    1. **Circular Wait Deadlock**: Multiple resource acquisition в разном порядке
    2. **Resource Hierarchy Deadlock**: Global semaphore -> Type lock -> Ownership lock
    3. **Dependency Graph Deadlock**: Workflow steps с circular dependencies  
    4. **Priority Inversion Deadlock**: Высокоприоритетные задачи ждут низкоприоритетные
    5. **Convoy Effect**: Все потоки блокируются за одним медленным
    6. **Nested Lock Deadlock**: Type locks -> Ownership locks в разном порядке
    7. **Cross-Workflow Deadlock**: Workflows блокируют друг друга через shared resources

    DEADLOCK DETECTION ПРИЗНАКИ:

    - Потоки висят в lock'ах неопределенно долго
    - CPU usage падает до нуля при большом количестве активных потоков  
    - Timeout'ы в resource acquisition
    - Circular wait в thread dumps

    ЗАДАНИЯ ДЛЯ ИСПРАВЛЕНИЯ:

    1. **Реализуйте ordered resource acquisition** - всегда берите locks в одном порядке
    2. **Добавьте timeout'ы для всех lock операций**
    3. **Реализуйте deadlock detection algorithm**
    4. **Используйте try-lock patterns с backoff**
    5. **Замените nested locks на lock-free структуры где возможно**
    6. **Реализуйте resource reservation system**
    7. **Добавьте circuit breaker для resource pools**  
    8. **Используйте dependency injection для избежания lock hierarchies**
    9. **Реализуйте proper cleanup и resource leak detection**
    10. **Добавьте monitoring и alerting на potential deadlocks**

    ПРОДВИНУТЫЕ ТЕХНИКИ:

    - **Banker's Algorithm** для deadlock prevention
    - **Wait-for Graph** для deadlock detection  
    - **Resource Allocation Graph** для анализа dependencies
    - **Priority Inheritance** для борьбы с priority inversion
    - **Lock-free programming** для критичных участков

    БОНУС: Реализуйте distributed deadlock detection для multi-node scenarios
    */
}
