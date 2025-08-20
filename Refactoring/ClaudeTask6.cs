using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefactoringTask6 {
    // ЗАДАЧА 6: Банковская система с классическими deadlock'ами
    // Проблемы: классический deadlock при переводах, lock ordering, nested locks,
    // async deadlock, reader-writer deadlock, semaphore deadlock

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    // Проблема: классический deadlock при переводах между счетами
    public class BankAccount {
        private readonly object _lockObject = new object();
        private readonly string _accountNumber;
        private decimal _balance;
        private readonly List<Transaction> _transactions = new List<Transaction>();

        public string AccountNumber => _accountNumber;
        public decimal Balance {
            get {
                lock (_lockObject) {
                    return _balance;
                }
            }
        }

        public BankAccount(string accountNumber, decimal initialBalance) {
            _accountNumber = accountNumber;
            _balance = initialBalance;
        }

        // Проблема: nested locks без порядка могут вызвать deadlock
        public bool Transfer(BankAccount targetAccount, decimal amount) {
            lock (_lockObject) {
                if (_balance < amount)
                    return false;

                // Проблема: вложенный lock на другом объекте
                lock (targetAccount._lockObject) {
                    _balance -= amount;
                    targetAccount._balance += amount;

                    var transaction = new Transaction {
                        FromAccount = this.AccountNumber,
                        ToAccount = targetAccount.AccountNumber,
                        Amount = amount,
                        Timestamp = DateTime.Now
                    };

                    _transactions.Add(transaction);
                    targetAccount._transactions.Add(transaction);

                    return true;
                }
            }
        }

        // Проблема: может вызвать deadlock при одновременном вызове с Transfer
        public List<Transaction> GetTransactionHistory() {
            lock (_lockObject) {
                return _transactions.ToList();
            }
        }

        // Проблема: длительная операция под lock'ом
        public void GenerateMonthlyReport() {
            lock (_lockObject) {
                // Имитация долгой операции
                Thread.Sleep(2000);

                var report = string.Join("\n", _transactions.Select(t =>
                    $"{t.Timestamp}: {t.FromAccount} -> {t.ToAccount}: {t.Amount:C}"));

                Console.WriteLine($"Report for {AccountNumber}:\n{report}");
            }
        }
    }

    public class Transaction {
        public string FromAccount { get; set; }
        public string ToAccount { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Проблема: async/sync deadlock при использовании в UI потоке
    public class BankingService {
        private readonly Dictionary<string, BankAccount> _accounts =
            new Dictionary<string, BankAccount>();
        private readonly object _accountsLock = new object();
        private readonly SemaphoreSlim _transferSemaphore = new SemaphoreSlim(3, 3);

        public void CreateAccount(string accountNumber, decimal initialBalance) {
            lock (_accountsLock) {
                if (_accounts.ContainsKey(accountNumber))
                    throw new InvalidOperationException("Account already exists");

                _accounts[accountNumber] = new BankAccount(accountNumber, initialBalance);
            }
        }

        // Проблема: async method с sync locks может вызвать deadlock
        public async Task<bool> TransferAsync(string fromAccount, string toAccount, decimal amount) {
            await _transferSemaphore.WaitAsync();

            try {
                BankAccount from, to;

                // Проблема: lock в async методе
                lock (_accountsLock) {
                    if (!_accounts.TryGetValue(fromAccount, out from) ||
                        !_accounts.TryGetValue(toAccount, out to)) {
                        return false;
                    }
                }

                // Проблема: может вызвать deadlock если другой поток делает Transfer в обратном порядке
                var result = from.Transfer(to, amount);

                if (result) {
                    // Проблема: async операция после lock'а
                    await LogTransactionAsync(fromAccount, toAccount, amount);
                }

                return result;
            } finally {
                _transferSemaphore.Release();
            }
        }

        private async Task LogTransactionAsync(string from, string to, decimal amount) {
            // Имитация async логирования
            await Task.Delay(100);
            Console.WriteLine($"Logged: {from} -> {to}: {amount:C}");
        }

        // Проблема: potential deadlock при одновременном вызове TransferAsync
        public decimal GetTotalBalance() {
            decimal total = 0;

            lock (_accountsLock) {
                foreach (var account in _accounts.Values) {
                    // Проблема: вызов Balance может заблокировать account._lockObject
                    total += account.Balance;
                }
            }

            return total;
        }

        // Проблема: множественные locks без порядка
        public void GenerateAllReports() {
            lock (_accountsLock) {
                var accounts = _accounts.Values.ToList();

                // Проблема: вызываем методы, которые берут locks, находясь уже под lock'ом
                foreach (var account in accounts) {
                    account.GenerateMonthlyReport();
                }
            }
        }
    }

    // Проблема: Reader-Writer deadlock scenario
    public class AccountCache {
        private readonly Dictionary<string, BankAccount> _cache =
            new Dictionary<string, BankAccount>();
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        public BankAccount GetAccount(string accountNumber) {
            _cacheLock.EnterReadLock();
            try {
                if (_cache.TryGetValue(accountNumber, out var account)) {
                    // Проблема: попытка upgrade lock'а может вызвать deadlock
                    if (ShouldUpdateAccount(account)) {
                        _cacheLock.ExitReadLock();
                        _cacheLock.EnterWriteLock();
                        try {
                            // Проблема: между exit read и enter write другой поток мог изменить cache
                            UpdateAccountInCache(accountNumber, account);
                            return account;
                        } finally {
                            _cacheLock.ExitWriteLock();
                        }
                    }
                    return account;
                }

                return null;
            } finally {
                if (_cacheLock.IsReadLockHeld)
                    _cacheLock.ExitReadLock();
            }
        }

        public void UpdateAccount(string accountNumber, BankAccount account) {
            _cacheLock.EnterWriteLock();
            try {
                _cache[accountNumber] = account;

                // Проблема: вызов внешнего метода под write lock'ом
                NotifyAccountUpdated(accountNumber, account);
            } finally {
                _cacheLock.ExitWriteLock();
            }
        }

        private bool ShouldUpdateAccount(BankAccount account) {
            return true; // Упрощено для примера
        }

        private void UpdateAccountInCache(string accountNumber, BankAccount account) {
            _cache[accountNumber] = account;
        }

        // Проблема: может вызвать deadlock если внешний код попытается взять другие locks
        private void NotifyAccountUpdated(string accountNumber, BankAccount account) {
            Console.WriteLine($"Account {accountNumber} updated");
        }
    }

    // Проблема: Producer-Consumer deadlock с неправильным использованием Monitor
    public class TransactionProcessor {
        private readonly Queue<Transaction> _pendingTransactions = new Queue<Transaction>();
        private readonly object _queueLock = new object();
        private readonly int _maxQueueSize = 100;
        private volatile bool _isProcessing = true;

        public void AddTransaction(Transaction transaction) {
            lock (_queueLock) {
                // Проблема: Wait под lock'ом может вызвать deadlock
                while (_pendingTransactions.Count >= _maxQueueSize) {
                    Monitor.Wait(_queueLock); // Ждем пока очередь не освободится
                }

                _pendingTransactions.Enqueue(transaction);
                Monitor.Pulse(_queueLock); // Уведомляем consumer'а
            }
        }

        public Transaction GetNextTransaction() {
            lock (_queueLock) {
                // Проблема: потенциальный deadlock если producer не может добавить элементы
                while (_pendingTransactions.Count == 0 && _isProcessing) {
                    Monitor.Wait(_queueLock); // Ждем новые транзакции
                }

                if (_pendingTransactions.Count > 0) {
                    var transaction = _pendingTransactions.Dequeue();
                    Monitor.Pulse(_queueLock); // Уведомляем producer'а
                    return transaction;
                }

                return null;
            }
        }

        public void StopProcessing() {
            lock (_queueLock) {
                _isProcessing = false;
                Monitor.PulseAll(_queueLock);
            }
        }
    }

    // Проблема: Deadlock в иерархии locks
    public class BankManager {
        private readonly object _managerLock = new object();
        private readonly BankingService _bankingService;
        private readonly AccountCache _accountCache;
        private readonly TransactionProcessor _transactionProcessor;

        public BankManager() {
            _bankingService = new BankingService();
            _accountCache = new AccountCache();
            _transactionProcessor = new TransactionProcessor();
        }

        // Проблема: lock hierarchy может вызвать deadlock
        public async Task ProcessBulkTransfersAsync(List<TransferRequest> transfers) {
            lock (_managerLock) {
                foreach (var transfer in transfers) {
                    // Проблема: вызов async метода из sync context под lock'ом
                    var task = _bankingService.TransferAsync(transfer.From, transfer.To, transfer.Amount);

                    // Проблема: .Result может вызвать deadlock
                    var result = task.Result;

                    if (result) {
                        var transaction = new Transaction {
                            FromAccount = transfer.From,
                            ToAccount = transfer.To,
                            Amount = transfer.Amount,
                            Timestamp = DateTime.Now
                        };

                        // Проблема: вложенные вызовы с locks
                        _transactionProcessor.AddTransaction(transaction);
                    }
                }
            }
        }

        // Проблема: статические locks могут вызвать application-wide deadlocks
        private static readonly object _staticLock = new object();
        private static Dictionary<string, BankManager> _instances = new Dictionary<string, BankManager>();

        public static BankManager GetInstance(string bankId) {
            lock (_staticLock) {
                if (_instances.TryGetValue(bankId, out var manager)) {
                    // Проблема: вызов метода экземпляра под статическим lock'ом
                    manager.ValidateInstance();
                    return manager;
                }

                var newManager = new BankManager();
                _instances[bankId] = newManager;
                return newManager;
            }
        }

        private void ValidateInstance() {
            lock (_managerLock) {
                // Проблема: может попытаться взять статический lock изнутри instance lock'а
                if (_instances.Values.Count(m => m == this) > 1) {
                    lock (_staticLock) {
                        // Cleanup duplicate instances
                        var toRemove = _instances.Where(kvp => kvp.Value == this && kvp.Key != "primary").ToList();
                        foreach (var kvp in toRemove) {
                            _instances.Remove(kvp.Key);
                        }
                    }
                }
            }
        }
    }

    public class TransferRequest {
        public string From { get; set; }
        public string To { get; set; }
        public decimal Amount { get; set; }
    }

    class Program {
        static async Task Main() {
            var bankManager = BankManager.GetInstance("main");
            var service = new BankingService();

            // Создаем тестовые аккаунты
            service.CreateAccount("ACC001", 1000);
            service.CreateAccount("ACC002", 1500);
            service.CreateAccount("ACC003", 2000);

            // Проблема: одновременные переводы могут вызвать deadlock
            var tasks = new List<Task>
            {
            // ACC001 -> ACC002
            Task.Run(() => service.TransferAsync("ACC001", "ACC002", 100)),
            // ACC002 -> ACC001 (обратный порядок locks!)
            Task.Run(() => service.TransferAsync("ACC002", "ACC001", 50)),
            
            // ACC002 -> ACC003
            Task.Run(() => service.TransferAsync("ACC002", "ACC003", 200)),
            // ACC003 -> ACC002 (обратный порядок locks!)
            Task.Run(() => service.TransferAsync("ACC003", "ACC002", 75)),
            
            // Долгая операция под lock'ом
            Task.Run(() => service.GenerateAllReports()),
            
            // Операция чтения под другим lock'ом
            Task.Run(() => service.GetTotalBalance())
        };

            try {
                // Проблема: может зависнуть из-за deadlock'а
                await Task.WhenAll(tasks);
                Console.WriteLine("All operations completed successfully");
            } catch (Exception ex) {
                Console.WriteLine($"Error occurred: {ex.Message}");
            }
        }
    }

    /*
    ТИПЫ DEADLOCK'ОВ В ЭТОЙ ЗАДАЧЕ:

    1. **Classic Lock Ordering Deadlock**: Transfer между аккаунтами в разном порядке
    2. **Nested Lock Deadlock**: _accountsLock -> account._lockObject
    3. **Reader-Writer Lock Deadlock**: Попытка upgrade read lock на write lock
    4. **Async/Sync Deadlock**: .Result на async операции под lock'ом
    5. **Producer-Consumer Deadlock**: Monitor.Wait в неправильной последовательности
    6. **Static vs Instance Lock Deadlock**: Статический lock vs instance lock
    7. **Semaphore + Lock Deadlock**: Комбинация семафора и обычных lock'ов

    ЗАДАНИЯ ДЛЯ ИСПРАВЛЕНИЯ:

    1. Реализуйте ordered locking для избежания классических deadlock'ов
    2. Устраните nested locks или сделайте их безопасными
    3. Исправьте проблемы с ReaderWriterLockSlim
    4. Замените .Result на proper async/await
    5. Реализуйте правильный Producer-Consumer паттерн
    6. Устраните или безопасно реализуйте lock hierarchies
    7. Добавьте timeout'ы для всех lock операций
    8. Реализуйте deadlock detection или prevention
    9. Используйте lock-free коллекции где возможно
    10. Добавьте мониторинг и логирование lock'ов для debugging

    БОНУС: Реализуйте banker's algorithm для prevention deadlock'ов
    */
}
