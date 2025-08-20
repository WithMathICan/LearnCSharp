using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefactoringTask8 {
    // ЗАДАЧА 8: Распределенная система кэширования с межпроцессными deadlock'ами
    // Проблемы: distributed deadlock, lock escalation, phantom reads,
    // consistency vs availability tradeoffs, cascade failures

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    // Проблема: не учитывает distributed locks и может создавать inconsistency
    public class DistributedCacheNode {
        private readonly string _nodeId;
        private readonly ConcurrentDictionary<string, CacheEntry> _localCache;
        private readonly Dictionary<string, DistributedCacheNode> _otherNodes;
        private readonly Dictionary<string, NodeLock> _distributedLocks;
        private readonly ReaderWriterLockSlim _nodeLock;
        private readonly object _lockManagerLock = new object();

        public string NodeId => _nodeId;
        public bool IsOnline { get; private set; } = true;

        public DistributedCacheNode(string nodeId) {
            _nodeId = nodeId;
            _localCache = new ConcurrentDictionary<string, CacheEntry>();
            _otherNodes = new Dictionary<string, DistributedCacheNode>();
            _distributedLocks = new Dictionary<string, NodeLock>();
            _nodeLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        public void RegisterNode(DistributedCacheNode node) {
            _otherNodes[node.NodeId] = node;
        }

        // Проблема: может создать distributed deadlock при одновременном обновлении
        public async Task<bool> SetAsync(string key, object value, TimeSpan expiration) {
            // Проблема: пытаемся взять distributed lock без proper ordering
            var distributedLock = await AcquireDistributedLockAsync(key, LockType.Write);
            if (distributedLock == null) {
                throw new TimeoutException($"Failed to acquire distributed lock for key: {key}");
            }

            try {
                // Проблема: берем local lock после distributed lock
                _nodeLock.EnterWriteLock();
                try {
                    var entry = new CacheEntry {
                        Key = key,
                        Value = value,
                        ExpiresAt = DateTime.UtcNow.Add(expiration),
                        Version = GenerateVersion(),
                        LastModifiedBy = _nodeId
                    };

                    _localCache[key] = entry;

                    // Проблема: реплицируем на другие узлы под lock'ом
                    await ReplicateToOtherNodesAsync(entry);

                    return true;
                } finally {
                    _nodeLock.ExitWriteLock();
                }
            } finally {
                await ReleaseDistributedLockAsync(distributedLock);
            }
        }

        // Проблема: может создать phantom reads и inconsistent state
        public async Task<T> GetAsync<T>(string key) {
            // Проблема: читаем без proper isolation level
            _nodeLock.EnterReadLock();
            try {
                if (_localCache.TryGetValue(key, out var localEntry) && !IsExpired(localEntry)) {
                    // Проблема: возвращаем potentially stale data
                    return (T)localEntry.Value;
                }

                // Проблема: запрашиваем данные с других узлов под read lock'ом
                var remoteEntry = await FetchFromOtherNodesAsync(key);
                if (remoteEntry != null) {
                    // Проблема: пытаемся upgrade read lock to write lock
                    _nodeLock.ExitReadLock();
                    _nodeLock.EnterWriteLock();
                    try {
                        // Проблема: между exit read и enter write другой поток мог изменить cache
                        _localCache[key] = remoteEntry;
                        return (T)remoteEntry.Value;
                    } finally {
                        _nodeLock.ExitWriteLock();
                    }
                }

                return default(T);
            } finally {
                if (_nodeLock.IsReadLockHeld)
                    _nodeLock.ExitReadLock();
            }
        }

        // Проблема: может создать cascade failure и distributed deadlock
        private async Task ReplicateToOtherNodesAsync(CacheEntry entry) {
            var replicationTasks = new List<Task<bool>>();

            foreach (var otherNode in _otherNodes.Values) {
                if (otherNode.IsOnline) {
                    // Проблема: одновременная репликация на все узлы может создать deadlock
                    replicationTasks.Add(ReplicateToNodeAsync(otherNode, entry));
                }
            }

            try {
                // Проблема: ждем все узлы, даже если некоторые недоступны
                var results = await Task.WhenAll(replicationTasks);

                // Проблема: не обрабатываем partial failures
                if (!results.All(r => r)) {
                    throw new InvalidOperationException("Failed to replicate to all nodes");
                }
            } catch (Exception) {
                // Проблема: при неудачной репликации не откатываем local changes
                throw;
            }
        }

        private async Task<bool> ReplicateToNodeAsync(DistributedCacheNode targetNode, CacheEntry entry) {
            try {
                // Проблема: может создать circular replication calls
                return await targetNode.ReceiveReplicationAsync(entry, _nodeId);
            } catch (Exception) {
                // Проблема: помечаем узел как offline, но не восстанавливаем connection
                targetNode.IsOnline = false;
                return false;
            }
        }

        // Проблема: может создать replication loops и inconsistency
        public async Task<bool> ReceiveReplicationAsync(CacheEntry entry, string sourceNodeId) {
            // Проблема: не проверяем version conflicts
            _nodeLock.EnterWriteLock();
            try {
                if (_localCache.TryGetValue(entry.Key, out var existingEntry)) {
                    // Проблема: простое сравнение версий может не работать в distributed environment
                    if (existingEntry.Version >= entry.Version) {
                        return true; // Ignore older version
                    }
                }

                _localCache[entry.Key] = entry;

                // Проблема: может создать infinite replication loop
                if (sourceNodeId != _nodeId) {
                    // Реплицируем дальше на другие узлы (кроме источника)
                    var otherNodes = _otherNodes.Values.Where(n => n.NodeId != sourceNodeId && n.IsOnline);
                    foreach (var node in otherNodes) {
                        // Проблема: recursive replication calls могут создать deadlock
                        _ = Task.Run(() => node.ReceiveReplicationAsync(entry, _nodeId));
                    }
                }

                return true;
            } finally {
                _nodeLock.ExitWriteLock();
            }
        }

        // Проблема: distributed lock может создать deadlock между узлами
        private async Task<NodeLock> AcquireDistributedLockAsync(string key, LockType lockType, int timeoutMs = 5000) {
            var lockId = Guid.NewGuid().ToString();
            var nodeLock = new NodeLock {
                LockId = lockId,
                Key = key,
                LockType = lockType,
                OwnerNodeId = _nodeId,
                AcquiredAt = DateTime.UtcNow
            };

            // Проблема: пытаемся получить консенсус от всех узлов без proper leader election
            var lockVotes = new List<Task<bool>>();

            lock (_lockManagerLock) {
                _distributedLocks[lockId] = nodeLock;
            }

            foreach (var otherNode in _otherNodes.Values.Where(n => n.IsOnline)) {
                lockVotes.Add(RequestLockVoteAsync(otherNode, nodeLock));
            }

            try {
                var votes = await Task.WhenAll(lockVotes);

                // Проблема: требуем единогласия, что может привести к deadlock
                if (votes.All(v => v)) {
                    nodeLock.IsAcquired = true;
                    return nodeLock;
                } else {
                    // Проблема: не отменяем partial locks при неудаче
                    lock (_lockManagerLock) {
                        _distributedLocks.Remove(lockId);
                    }
                    return null;
                }
            } catch (Exception) {
                lock (_lockManagerLock) {
                    _distributedLocks.Remove(lockId);
                }
                throw;
            }
        }

        // Проблема: может создать voting deadlock
        private async Task<bool> RequestLockVoteAsync(DistributedCacheNode targetNode, NodeLock lockRequest) {
            try {
                return await targetNode.VoteForLockAsync(lockRequest);
            } catch (Exception) {
                return false; // Недоступный узел голосует "против"
            }
        }

        // Проблема: может создать circular voting deadlock
        public async Task<bool> VoteForLockAsync(NodeLock lockRequest) {
            lock (_lockManagerLock) {
                // Проблема: проверяем конфликты locks без учета deadlock detection
                var conflictingLocks = _distributedLocks.Values.Where(l =>
                    l.Key == lockRequest.Key &&
                    l.IsAcquired &&
                    IsConflicting(l.LockType, lockRequest.LockType)).ToList();

                if (conflictingLocks.Any()) {
                    // Проблема: может создать circular wait situation
                    foreach (var conflictingLock in conflictingLocks) {
                        if (string.Compare(conflictingLock.OwnerNodeId, lockRequest.OwnerNodeId) > 0) {
                            // Простой tie-breaker, но может создать livelock
                            return false;
                        }
                    }
                }

                // Записываем pending lock
                _distributedLocks[lockRequest.LockId] = lockRequest;
                return true;
            }
        }

        private bool IsConflicting(LockType existingLock, LockType requestedLock) {
            return existingLock == LockType.Write || requestedLock == LockType.Write;
        }

        private async Task ReleaseDistributedLockAsync(NodeLock distributedLock) {
            lock (_lockManagerLock) {
                _distributedLocks.Remove(distributedLock.LockId);
            }

            // Проблема: уведомляем все узлы об освобождении lock'а
            var releaseTasks = _otherNodes.Values.Where(n => n.IsOnline)
                                                .Select(n => n.ReleaseLockVoteAsync(distributedLock.LockId));

            // Проблема: fire-and-forget может оставить stale locks
            _ = Task.WhenAll(releaseTasks);
        }

        public async Task ReleaseLockVoteAsync(string lockId) {
            lock (_lockManagerLock) {
                _distributedLocks.Remove(lockId);
            }
        }

        // Проблема: может создать inconsistent reads
        private async Task<CacheEntry> FetchFromOtherNodesAsync(string key) {
            var fetchTasks = _otherNodes.Values.Where(n => n.IsOnline)
                                              .Select(n => FetchFromNodeAsync(n, key));

            try {
                var results = await Task.WhenAll(fetchTasks);

                // Проблема: выбираем первый найденный результат без проверки consistency
                return results.FirstOrDefault(r => r != null);
            } catch (Exception) {
                return null;
            }
        }

        private async Task<CacheEntry> FetchFromNodeAsync(DistributedCacheNode node, string key) {
            try {
                return await node.GetLocalEntryAsync(key);
            } catch (Exception) {
                return null;
            }
        }

        public async Task<CacheEntry> GetLocalEntryAsync(string key) {
            _nodeLock.EnterReadLock();
            try {
                if (_localCache.TryGetValue(key, out var entry) && !IsExpired(entry)) {
                    return entry;
                }
                return null;
            } finally {
                _nodeLock.ExitReadLock();
            }
        }

        private bool IsExpired(CacheEntry entry) {
            return DateTime.UtcNow > entry.ExpiresAt;
        }

        private long GenerateVersion() {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    public class CacheEntry {
        public string Key { get; set; }
        public object Value { get; set; }
        public DateTime ExpiresAt { get; set; }
        public long Version { get; set; }
        public string LastModifiedBy { get; set; }
    }

    public class NodeLock {
        public string LockId { get; set; }
        public string Key { get; set; }
        public LockType LockType { get; set; }
        public string OwnerNodeId { get; set; }
        public DateTime AcquiredAt { get; set; }
        public bool IsAcquired { get; set; }
    }

    public enum LockType {
        Read,
        Write
    }

    // Проблема: может создать cascade failures и split-brain scenarios
    public class DistributedCacheCluster {
        private readonly Dictionary<string, DistributedCacheNode> _nodes;
        private readonly object _clusterLock = new object();
        private readonly Timer _healthCheckTimer;
        private volatile bool _isPartitioned = false;

        public DistributedCacheCluster() {
            _nodes = new Dictionary<string, DistributedCacheNode>();
            _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public void AddNode(DistributedCacheNode node) {
            lock (_clusterLock) {
                _nodes[node.NodeId] = node;

                // Проблема: регистрируем узел на всех существующих узлах под lock'ом
                foreach (var existingNode in _nodes.Values.Where(n => n.NodeId != node.NodeId)) {
                    existingNode.RegisterNode(node);
                    node.RegisterNode(existingNode);
                }
            }
        }

        // Проблема: может создать split-brain при network partition
        private void PerformHealthCheck(object state) {
            var onlineNodes = new List<DistributedCacheNode>();
            var offlineNodes = new List<DistributedCacheNode>();

            lock (_clusterLock) {
                foreach (var node in _nodes.Values) {
                    // Проблема: простая проверка IsOnline недостаточна для distributed system
                    if (node.IsOnline) {
                        onlineNodes.Add(node);
                    } else {
                        offlineNodes.Add(node);
                    }
                }
            }

            // Проблема: не проверяем quorum для предотвращения split-brain
            if (offlineNodes.Count > 0) {
                Console.WriteLine($"Health check: {offlineNodes.Count} nodes are offline");

                // Проблема: не реализован proper failover mechanism
                if (onlineNodes.Count < _nodes.Count / 2 + 1) {
                    _isPartitioned = true;
                    Console.WriteLine("WARNING: Cluster partition detected!");
                }
            }
        }

        // Проблема: может создать inconsistency при concurrent operations
        public async Task<bool> DistributedSetAsync(string key, object value, TimeSpan expiration) {
            if (_isPartitioned) {
                throw new InvalidOperationException("Cluster is partitioned, cannot perform write operations");
            }

            var onlineNodes = GetOnlineNodes();
            if (onlineNodes.Count == 0) {
                throw new InvalidOperationException("No online nodes available");
            }

            // Проблема: выбираем primary node без proper leader election
            var primaryNode = onlineNodes.First();

            try {
                return await primaryNode.SetAsync(key, value, expiration);
            } catch (Exception ex) {
                Console.WriteLine($"Failed to set key {key} on primary node: {ex.Message}");

                // Проблема: fallback на другой узел может создать inconsistency
                foreach (var fallbackNode in onlineNodes.Skip(1)) {
                    try {
                        return await fallbackNode.SetAsync(key, value, expiration);
                    } catch (Exception fallbackEx) {
                        Console.WriteLine($"Fallback node {fallbackNode.NodeId} also failed: {fallbackEx.Message}");
                    }
                }

                throw;
            }
        }

        // Проблема: может вернуть stale data
        public async Task<T> DistributedGetAsync<T>(string key) {
            var onlineNodes = GetOnlineNodes();
            if (onlineNodes.Count == 0) {
                throw new InvalidOperationException("No online nodes available");
            }

            // Проблема: читаем с первого доступного узла без проверки consistency
            foreach (var node in onlineNodes) {
                try {
                    var result = await node.GetAsync<T>(key);
                    if (result != null) {
                        return result;
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Failed to get key {key} from node {node.NodeId}: {ex.Message}");
                }
            }

            return default(T);
        }

        private List<DistributedCacheNode> GetOnlineNodes() {
            lock (_clusterLock) {
                return _nodes.Values.Where(n => n.IsOnline).ToList();
            }
        }
    }

    // Проблема: может создать lock escalation и deadlock
    public class DistributedLockManager {
        private readonly DistributedCacheCluster _cluster;
        private readonly Dictionary<string, GlobalLock> _globalLocks;
        private readonly ReaderWriterLockSlim _lockManagerLock;
        private readonly Dictionary<string, Queue<LockRequest>> _lockQueues;

        public DistributedLockManager(DistributedCacheCluster cluster) {
            _cluster = cluster;
            _globalLocks = new Dictionary<string, GlobalLock>();
            _lockManagerLock = new ReaderWriterLockSlim();
            _lockQueues = new Dictionary<string, Queue<LockRequest>>();
        }

        // Проблема: может создать deadlock при множественных lock requests
        public async Task<IDisposable> AcquireGlobalLockAsync(string lockKey, LockType lockType,
                                                              string requesterId, int timeoutMs = 30000) {
            var request = new LockRequest {
                LockKey = lockKey,
                LockType = lockType,
                RequesterId = requesterId,
                RequestTime = DateTime.UtcNow,
                CompletionSource = new TaskCompletionSource<GlobalLock>()
            };

            // Проблема: добавляем в очередь под write lock'ом
            _lockManagerLock.EnterWriteLock();
            try {
                if (!_lockQueues.ContainsKey(lockKey)) {
                    _lockQueues[lockKey] = new Queue<LockRequest>();
                }

                _lockQueues[lockKey].Enqueue(request);

                // Проблема: запускаем processing под lock'ом
                _ = Task.Run(() => ProcessLockQueue(lockKey));
            } finally {
                _lockManagerLock.ExitWriteLock();
            }

            // Проблема: может зависнуть навсегда при deadlock
            var globalLock = await request.CompletionSource.Task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));

            return new GlobalLockHandle(this, globalLock);
        }

        // Проблема: может создать lock starvation
        private async Task ProcessLockQueue(string lockKey) {
            while (true) {
                LockRequest nextRequest = null;

                _lockManagerLock.EnterWriteLock();
                try {
                    if (!_lockQueues.ContainsKey(lockKey) || _lockQueues[lockKey].Count == 0) {
                        return; // Очередь пуста
                    }

                    // Проблема: FIFO может привести к starvation для write locks
                    nextRequest = _lockQueues[lockKey].Peek();

                    // Проверяем, можем ли выдать lock
                    if (CanGrantLock(lockKey, nextRequest.LockType, nextRequest.RequesterId)) {
                        _lockQueues[lockKey].Dequeue();

                        var globalLock = new GlobalLock {
                            LockKey = lockKey,
                            LockType = nextRequest.LockType,
                            OwnerId = nextRequest.RequesterId,
                            AcquiredAt = DateTime.UtcNow
                        };

                        _globalLocks[GenerateLockId(lockKey, nextRequest.RequesterId)] = globalLock;
                        nextRequest.CompletionSource.SetResult(globalLock);
                    } else {
                        // Не можем выдать lock сейчас, ждем
                        return;
                    }
                } finally {
                    _lockManagerLock.ExitWriteLock();
                }

                // Проблема: короткая задержка может создать busy wait
                await Task.Delay(10);
            }
        }

        // Проблема: простая логика не учитывает lock upgrade/downgrade scenarios
        private bool CanGrantLock(string lockKey, LockType requestedType, string requesterId) {
            var existingLocks = _globalLocks.Values
                .Where(l => l.LockKey == lockKey && l.OwnerId != requesterId)
                .ToList();

            if (existingLocks.Count == 0) {
                return true; // Нет конфликтующих lock'ов
            }

            if (requestedType == LockType.Read) {
                // Read lock можно выдать, если нет write lock'ов
                return !existingLocks.Any(l => l.LockType == LockType.Write);
            }

            // Write lock требует отсутствия любых других lock'ов
            return false;
        }

        private string GenerateLockId(string lockKey, string ownerId) {
            return $"{lockKey}:{ownerId}";
        }

        public void ReleaseGlobalLock(GlobalLock globalLock) {
            _lockManagerLock.EnterWriteLock();
            try {
                var lockId = GenerateLockId(globalLock.LockKey, globalLock.OwnerId);
                _globalLocks.Remove(lockId);

                // Проблема: запускаем processing queue для всех lock keys
                _ = Task.Run(() => ProcessLockQueue(globalLock.LockKey));
            } finally {
                _lockManagerLock.ExitWriteLock();
            }
        }
    }

    public class LockRequest {
        public string LockKey { get; set; }
        public LockType LockType { get; set; }
        public string RequesterId { get; set; }
        public DateTime RequestTime { get; set; }
        public TaskCompletionSource<GlobalLock> CompletionSource { get; set; }
    }

    public class GlobalLock {
        public string LockKey { get; set; }
        public LockType LockType { get; set; }
        public string OwnerId { get; set; }
        public DateTime AcquiredAt { get; set; }
    }

    public class GlobalLockHandle : IDisposable {
        private readonly DistributedLockManager _lockManager;
        private readonly GlobalLock _globalLock;
        private bool _disposed = false;

        public GlobalLockHandle(DistributedLockManager lockManager, GlobalLock globalLock) {
            _lockManager = lockManager;
            _globalLock = globalLock;
        }

        public void Dispose() {
            if (!_disposed) {
                _lockManager.ReleaseGlobalLock(_globalLock);
                _disposed = true;
            }
        }
    }

    // Проблема: может создать transaction deadlock в distributed environment
    public class DistributedTransactionManager {
        private readonly DistributedCacheCluster _cluster;
        private readonly DistributedLockManager _lockManager;
        private readonly Dictionary<string, DistributedTransaction> _activeTransactions;
        private readonly object _transactionLock = new object();

        public DistributedTransactionManager(DistributedCacheCluster cluster, DistributedLockManager lockManager) {
            _cluster = cluster;
            _lockManager = lockManager;
            _activeTransactions = new Dictionary<string, DistributedTransaction>();
        }

        // Проблема: может создать distributed deadlock при concurrent transactions
        public async Task<DistributedTransaction> BeginTransactionAsync(string transactionId, List<string> keys) {
            var transaction = new DistributedTransaction {
                Id = transactionId,
                Keys = keys,
                Status = TransactionStatus.Active,
                StartTime = DateTime.UtcNow,
                Locks = new List<IDisposable>()
            };

            lock (_transactionLock) {
                _activeTransactions[transactionId] = transaction;
            }

            try {
                // Проблема: берем locks для всех keys без proper ordering
                foreach (var key in keys) {
                    var lockHandle = await _lockManager.AcquireGlobalLockAsync(
                        key, LockType.Write, transactionId, 10000);

                    transaction.Locks.Add(lockHandle);
                }

                return transaction;
            } catch (Exception) {
                // Проблема: rollback может сам создать deadlock
                await RollbackTransactionAsync(transaction);
                throw;
            }
        }

        // Проблема: может создать inconsistent state при partial commit failure
        public async Task<bool> CommitTransactionAsync(DistributedTransaction transaction) {
            if (transaction.Status != TransactionStatus.Active) {
                throw new InvalidOperationException($"Transaction {transaction.Id} is not active");
            }

            transaction.Status = TransactionStatus.Committing;

            try {
                // Проблема: commit на всех узлах без proper 2PC protocol
                var commitTasks = transaction.Operations.Select(async op =>
                {
                    switch (op.Type) {
                        case OperationType.Set:
                            return await _cluster.DistributedSetAsync(op.Key, op.Value, op.Expiration);
                        default:
                            return true;
                    }
                });

                var results = await Task.WhenAll(commitTasks);

                if (results.All(r => r)) {
                    transaction.Status = TransactionStatus.Committed;
                    return true;
                } else {
                    // Проблема: partial failure handling может оставить inconsistent state
                    await RollbackTransactionAsync(transaction);
                    return false;
                }
            } catch (Exception) {
                await RollbackTransactionAsync(transaction);
                throw;
            } finally {
                ReleaseLocks(transaction);

                lock (_transactionLock) {
                    _activeTransactions.Remove(transaction.Id);
                }
            }
        }

        private async Task RollbackTransactionAsync(DistributedTransaction transaction) {
            transaction.Status = TransactionStatus.RolledBack;

            // Проблема: rollback logic может быть сложной в distributed environment
            // Для упрощения не реализуем полный rollback

            ReleaseLocks(transaction);
        }

        private void ReleaseLocks(DistributedTransaction transaction) {
            foreach (var lockHandle in transaction.Locks) {
                try {
                    lockHandle?.Dispose();
                } catch (Exception ex) {
                    Console.WriteLine($"Error releasing lock: {ex.Message}");
                }
            }

            transaction.Locks.Clear();
        }
    }

    public class DistributedTransaction {
        public string Id { get; set; }
        public List<string> Keys { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public List<IDisposable> Locks { get; set; }
        public List<TransactionOperation> Operations { get; set; } = new List<TransactionOperation>();

        public void AddOperation(string key, object value, TimeSpan expiration) {
            Operations.Add(new TransactionOperation {
                Type = OperationType.Set,
                Key = key,
                Value = value,
                Expiration = expiration
            });
        }
    }

    public class TransactionOperation {
        public OperationType Type { get; set; }
        public string Key { get; set; }
        public object Value { get; set; }
        public TimeSpan Expiration { get; set; }
    }

    public enum TransactionStatus {
        Active,
        Committing,
        Committed,
        RolledBack
    }

    public enum OperationType {
        Set,
        Delete
    }

    class Program {
        static async Task Main() {
            Console.WriteLine("Starting distributed cache with deadlock scenarios...");

            // Создаем кластер
            var cluster = new DistributedCacheCluster();
            var lockManager = new DistributedLockManager(cluster);
            var transactionManager = new DistributedTransactionManager(cluster, lockManager);

            // Создаем узлы
            var node1 = new DistributedCacheNode("node1");
            var node2 = new DistributedCacheNode("node2");
            var node3 = new DistributedCacheNode("node3");

            cluster.AddNode(node1);
            cluster.AddNode(node2);
            cluster.AddNode(node3);

            // Сценарий 1: Distributed deadlock при одновременных операциях
            var deadlockTasks = new List<Task>();

            // Transaction 1: keys A, B, C
            deadlockTasks.Add(Task.Run(async () =>
            {
                try {
                    /*using*/ var transaction = await transactionManager.BeginTransactionAsync("tx1", new List<string> { "keyA", "keyB", "keyC" });
                    transaction.AddOperation("keyA", "value1", TimeSpan.FromMinutes(5));
                    transaction.AddOperation("keyB", "value2", TimeSpan.FromMinutes(5));
                    transaction.AddOperation("keyC", "value3", TimeSpan.FromMinutes(5));

                    await Task.Delay(2000); // Держим locks

                    var result = await transactionManager.CommitTransactionAsync(transaction);
                    Console.WriteLine($"Transaction 1 result: {result}");
                } catch (Exception ex) {
                    Console.WriteLine($"Transaction 1 failed: {ex.Message}");
                }
            }));

            // Transaction 2: keys C, B, A (обратный порядок!)
            deadlockTasks.Add(Task.Run(async () =>
            {
                await Task.Delay(100); // Небольшая задержка

                try {
                    /*using*/ var transaction = await transactionManager.BeginTransactionAsync("tx2", new List<string> { "keyC", "keyB", "keyA" });
                    transaction.AddOperation("keyC", "value4", TimeSpan.FromMinutes(5));
                    transaction.AddOperation("keyB", "value5", TimeSpan.FromMinutes(5));
                    transaction.AddOperation("keyA", "value6", TimeSpan.FromMinutes(5));

                    await Task.Delay(2000); // Держим locks

                    var result = await transactionManager.CommitTransactionAsync(transaction);
                    Console.WriteLine($"Transaction 2 result: {result}");
                } catch (Exception ex) {
                    Console.WriteLine($"Transaction 2 failed: {ex.Message}");
                }
            }));

            // Сценарий 2: Concurrent replication deadlock
            deadlockTasks.Add(Task.Run(async () =>
            {
                try {
                    await cluster.DistributedSetAsync("shared_key1", "data1", TimeSpan.FromMinutes(10));
                    Console.WriteLine("Set shared_key1 completed");
                } catch (Exception ex) {
                    Console.WriteLine($"Set shared_key1 failed: {ex.Message}");
                }
            }));

            deadlockTasks.Add(Task.Run(async () =>
            {
                await Task.Delay(50);
                try {
                    await cluster.DistributedSetAsync("shared_key2", "data2", TimeSpan.FromMinutes(10));
                    Console.WriteLine("Set shared_key2 completed");
                } catch (Exception ex) {
                    Console.WriteLine($"Set shared_key2 failed: {ex.Message}");
                }
            }));

            // Сценарий 3: Read-write conflicts
            for (int i = 0; i < 5; i++) {
                var index = i;
                deadlockTasks.Add(Task.Run(async () =>
                {
                    try {
                        var readValue = await cluster.DistributedGetAsync<string>($"concurrent_key_{index % 2}");
                        Console.WriteLine($"Read operation {index}: {readValue ?? "null"}");
                    } catch (Exception ex) {
                        Console.WriteLine($"Read operation {index} failed: {ex.Message}");
                    }
                }));

                deadlockTasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(25);
                    try {
                        await cluster.DistributedSetAsync($"concurrent_key_{index % 2}", $"updated_value_{index}", TimeSpan.FromMinutes(5));
                        Console.WriteLine($"Write operation {index} completed");
                    } catch (Exception ex) {
                        Console.WriteLine($"Write operation {index} failed: {ex.Message}");
                    }
                }));
            }

            try {
                // Проблема: может зависнуть из-за distributed deadlock
                await Task.WhenAll(deadlockTasks);
                Console.WriteLine("All distributed operations completed!");
            } catch (Exception ex) {
                Console.WriteLine($"Distributed operations failed: {ex.Message}");
            }

            Console.WriteLine("Distributed cache test completed.");
        }
    }

    /*
    ТИПЫ DISTRIBUTED DEADLOCK'ОВ В ЭТОЙ ЗАДАЧЕ:

    1. **Distributed Resource Ordering Deadlock**: Узлы берут distributed locks в разном порядке
    2. **Replication Cycle Deadlock**: Circular replication между узлами
    3. **Lock Escalation Deadlock**: Local locks -> Distributed locks -> Global locks
    4. **Transaction Deadlock**: Cross-node transactions с overlapping resource sets
    5. **Voting Deadlock**: Consensus protocols создают circular wait
    6. **Split-Brain Deadlock**: Network partition создает competing leaders
    7. **Priority Inversion Deadlock**: Высокоприоритетные операции блокируются низкоприоритетными

    ПРИЗНАКИ DISTRIBUTED DEADLOCK:

    - Операции зависают на неопределенное время
    - Timeout'ы на distributed lock acquisition
    - Inconsistent state между узлами
    - Split-brain symptoms (multiple leaders)
    - Cascade failures при node recovery

    ЗАДАНИЯ ДЛЯ ИСПРАВЛЕНИЯ:

    1. **Реализуйте distributed deadlock detection algorithm**
    2. **Добавьте proper leader election (Raft, PBFT)**
    3. **Реализуйте ordered distributed locking**
    4. **Добавьте quorum-based consistency**
    5. **Реализуйте proper 2-Phase Commit protocol**
    6. **Добавьте circuit breakers для cascade failure prevention**
    7. **Реализуйте eventual consistency с conflict resolution**
    8. **Добавьте network partition tolerance**
    9. **Реализуйте lock lease timeouts с automatic renewal**
    10. **Добавьте distributed transaction coordinator**

    ПРОДВИНУТЫЕ ТЕХНИКИ:

    - **Vector Clocks** для ordering distributed events
    - **Lamport Timestamps** для logical ordering  
    - **Consensus Algorithms** (Raft, PBFT) для consistency
    - **Anti-Entropy Protocols** для eventual consistency
    - **Gossip Protocols** для failure detection
    - **CRDT (Conflict-free Replicated Data Types)** для lock-free updates

    МОНИТОРИНГ И DEBUGGING:

    - Distributed trace correlation
    - Lock dependency graphs
    - Network partition simulation
    - Chaos engineering for resilience testing
    - Metrics для lock contention и wait times

    БОНУС: Реализуйте Byzantine Fault Tolerant consensus для hostile environments
    */
}
