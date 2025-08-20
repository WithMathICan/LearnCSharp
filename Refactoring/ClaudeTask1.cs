using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefactoringClaudeTask1 {
    // ЗАДАЧА 1: Система кэширования с множественными проблемами
    // Проблемы: утечки памяти, boxing/unboxing, неправильная многопоточность, 
    // нарушение принципов ООП, неэффективная работа со строками

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    public class CacheManager {
        private static CacheManager _instance;
        private Hashtable _cache;
        private ArrayList _keys;
        private Thread _cleanupThread;
        private bool _isRunning;

        // Проблема: не thread-safe singleton
        public static CacheManager Instance {
            get {
                if (_instance == null) {
                    _instance = new CacheManager();
                }
                return _instance;
            }
        }

        private CacheManager() {
            _cache = new Hashtable();
            _keys = new ArrayList();
            StartCleanup();
        }

        // Проблема: финализатор без IDisposable
        ~CacheManager() {
            _isRunning = false;
            _cleanupThread?.Join();
        }

        // Проблема: boxing/unboxing, нет валидации, race conditions
        public void Set(string key, object value, int ttlSeconds) {
            string cacheKey = key + "_" + DateTime.Now.Ticks.ToString(); // Проблема: конкатенация строк

            // Проблема: boxing для примитивных типов
            var entry = new CacheEntry {
                Value = value,
                ExpiresAt = DateTime.Now.AddSeconds(ttlSeconds),
                AccessCount = 0
            };

            _cache[cacheKey] = entry; // Проблема: нет синхронизации
            _keys.Add(cacheKey); // Проблема: может быть race condition
        }

        // Проблема: возвращает object вместо generic, нет thread-safety
        public object Get(string key) {
            // Проблема: неэффективный поиск по всем ключам
            foreach (string cacheKey in _keys) {
                if (cacheKey.StartsWith(key + "_")) {
                    if (_cache.Contains(cacheKey)) {
                        var entry = (CacheEntry)_cache[cacheKey];

                        if (entry.ExpiresAt > DateTime.Now) {
                            entry.AccessCount++; // Проблема: race condition
                            return entry.Value;
                        } else {
                            _cache.Remove(cacheKey); // Проблема: удаление во время итерации
                            _keys.Remove(cacheKey);
                        }
                    }
                }
            }
            return null;
        }

        // Проблема: неэффективная очистка, нет proper cancellation
        private void StartCleanup() {
            _isRunning = true;
            _cleanupThread = new Thread(() =>
            {
                while (_isRunning) {
                    Thread.Sleep(1000);
                    CleanupExpired();
                }
            });
            _cleanupThread.Start();
        }

        // Проблема: модификация коллекции во время итерации
        private void CleanupExpired() {
            foreach (string key in _keys) {
                if (_cache.Contains(key)) {
                    var entry = (CacheEntry)_cache[key];
                    if (entry.ExpiresAt <= DateTime.Now) {
                        _cache.Remove(key);
                        _keys.Remove(key);
                    }
                }
            }
        }

        // Проблема: использует конкатенацию строк для статистики
        public string GetStatistics() {
            string stats = "Cache Statistics:\n";
            stats += "Total entries: " + _cache.Count + "\n";
            stats += "Keys count: " + _keys.Count + "\n";

            foreach (DictionaryEntry entry in _cache) {
                var cacheEntry = (CacheEntry)entry.Value;
                stats += "Key: " + entry.Key + ", Access count: " + cacheEntry.AccessCount + "\n";
            }

            return stats;
        }
    }

    // Проблема: не реализует IEquatable, нет readonly полей где нужно
    public class CacheEntry {
        public object Value { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int AccessCount { get; set; }
    }

    // Пример использования с проблемами
    class Program {
        static void Main1() {
            var cache = CacheManager.Instance;

            // Проблема: boxing для int
            cache.Set("user:1", 42, 60);
            cache.Set("user:2", "John Doe", 120);

            // Проблема: unboxing и cast
            int userId = (int)cache.Get("user:1");
            string userName = (string)cache.Get("user:2");

            Console.WriteLine(cache.GetStatistics());
        }
    }

    /*
    ЗАДАНИЯ ДЛЯ РЕФАКТОРИНГА:

    1. Исправьте проблемы с многопоточностью (thread-safety, race conditions)
    2. Устраните boxing/unboxing, сделайте систему generic
    3. Реализуйте правильный паттерн Disposable вместо финализатора
    4. Оптимизируйте работу со строками
    5. Замените устаревшие коллекции на современные generic
    6. Исправьте проблемы с архитектурой (singleton, нарушение SRP)
    7. Добавьте proper exception handling
    8. Оптимизируйте алгоритмы поиска и очистки
    9. Добавьте валидацию входных данных
    10. Реализуйте cancellation для фоновых задач

    БОНУС: Реализуйте систему как async/await с использованием современных подходов
    */
}

namespace RefactoringClaudeTask1_Solution {
    // ЗАДАЧА 1: Система кэширования с множественными проблемами
    // Проблемы: утечки памяти, boxing/unboxing, неправильная многопоточность, 
    // нарушение принципов ООП, неэффективная работа со строками

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    public class CacheManager {
        private static Lazy<CacheManager> _instance = new(() => new CacheManager());
        private Hashtable _cache;
        private ArrayList _keys;
        private Thread _cleanupThread;
        private bool _isRunning;

        // Проблема: не thread-safe singleton
        public static CacheManager Instance => _instance.Value;

        private CacheManager() {
            _cache = new Hashtable();
            _keys = new ArrayList();
            StartCleanup();
        }

        // Проблема: финализатор без IDisposable
        ~CacheManager() {
            _isRunning = false;
            _cleanupThread?.Join();
        }

        // Проблема: boxing/unboxing, нет валидации, race conditions
        public void Set(string key, object value, int ttlSeconds) {
            string cacheKey = key + "_" + DateTime.Now.Ticks.ToString(); // Проблема: конкатенация строк

            // Проблема: boxing для примитивных типов
            var entry = new CacheEntry {
                Value = value,
                ExpiresAt = DateTime.Now.AddSeconds(ttlSeconds),
                AccessCount = 0
            };

            _cache[cacheKey] = entry; // Проблема: нет синхронизации
            _keys.Add(cacheKey); // Проблема: может быть race condition
        }

        // Проблема: возвращает object вместо generic, нет thread-safety
        public object Get(string key) {
            // Проблема: неэффективный поиск по всем ключам
            foreach (string cacheKey in _keys) {
                if (cacheKey.StartsWith(key + "_")) {
                    if (_cache.Contains(cacheKey)) {
                        var entry = (CacheEntry)_cache[cacheKey];

                        if (entry.ExpiresAt > DateTime.Now) {
                            entry.AccessCount++; // Проблема: race condition
                            return entry.Value;
                        } else {
                            _cache.Remove(cacheKey); // Проблема: удаление во время итерации
                            _keys.Remove(cacheKey);
                        }
                    }
                }
            }
            return null;
        }

        // Проблема: неэффективная очистка, нет proper cancellation
        private void StartCleanup() {
            _isRunning = true;
            _cleanupThread = new Thread(() => {
                while (_isRunning) {
                    Thread.Sleep(1000);
                    CleanupExpired();
                }
            });
            _cleanupThread.Start();
        }

        // Проблема: модификация коллекции во время итерации
        private void CleanupExpired() {
            foreach (string key in _keys) {
                if (_cache.Contains(key)) {
                    var entry = (CacheEntry)_cache[key];
                    if (entry.ExpiresAt <= DateTime.Now) {
                        _cache.Remove(key);
                        _keys.Remove(key);
                    }
                }
            }
        }

        // Проблема: использует конкатенацию строк для статистики
        public string GetStatistics() {
            string stats = "Cache Statistics:\n";
            stats += "Total entries: " + _cache.Count + "\n";
            stats += "Keys count: " + _keys.Count + "\n";

            foreach (DictionaryEntry entry in _cache) {
                var cacheEntry = (CacheEntry)entry.Value;
                stats += "Key: " + entry.Key + ", Access count: " + cacheEntry.AccessCount + "\n";
            }

            return stats;
        }
    }

    // Проблема: не реализует IEquatable, нет readonly полей где нужно
    public class CacheEntry {
        public object Value { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int AccessCount { get; set; }
    }

    // Пример использования с проблемами
    class Program {
        static void Main() {
            var cache = CacheManager.Instance;

            // Проблема: boxing для int
            cache.Set("user:1", 42, 60);
            cache.Set("user:2", "John Doe", 120);

            // Проблема: unboxing и cast
            int userId = (int)cache.Get("user:1");
            string userName = (string)cache.Get("user:2");

            Console.WriteLine(cache.GetStatistics());
        }
    }

    /*
    ЗАДАНИЯ ДЛЯ РЕФАКТОРИНГА:

    1. Исправьте проблемы с многопоточностью (thread-safety, race conditions)
    2. Устраните boxing/unboxing, сделайте систему generic
    3. Реализуйте правильный паттерн Disposable вместо финализатора
    4. Оптимизируйте работу со строками
    5. Замените устаревшие коллекции на современные generic
    6. Исправьте проблемы с архитектурой (singleton, нарушение SRP)
    7. Добавьте proper exception handling
    8. Оптимизируйте алгоритмы поиска и очистки
    9. Добавьте валидацию входных данных
    10. Реализуйте cancellation для фоновых задач

    БОНУС: Реализуйте систему как async/await с использованием современных подходов
    */
}
