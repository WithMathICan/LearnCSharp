using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultyThreading {
    /*
     * Код вроде использует double-check locking (двойную проверку), но под нагрузкой иногда падает с NullReferenceException на строке:
        return _cache.TryGetValue(key, out var value)
    Почему так происходит?
    Объясни, что именно ломает потокобезопасность.
    Скажи, как это правильно исправить.
    */
    internal class CacheManager {
        private object _lock = new();
        private Dictionary<string, string>? _cache;

        public string GetValue(string key) {
            if (_cache == null) {
                lock (_lock) {
                    if (_cache == null) {
                        _cache = LoadCacheFromDb();
                    }
                }
            }

            return _cache.TryGetValue(key, out var value)
                ? value
                : "Not found";
        }

        public void ClearCache() {
            _cache = null;
        }

        private Dictionary<string, string> LoadCacheFromDb() {
            // Здесь мы просто имитируем загрузку
            Thread.Sleep(100);
            return new Dictionary<string, string> {
                ["A"] = "ValueA",
                ["B"] = "ValueB"
            };
        }
    }

    internal class CacheManager_Refactored1 {
        private object _lock = new();
        private Dictionary<string, string>? _cache;

        public string GetValue(string key) {
            lock (_lock) {
                if (_cache == null) {
                    _cache = LoadCacheFromDb();
                }

                return _cache.TryGetValue(key, out var value)
                    ? value
                    : "Not found";
            }
        }

        public void ClearCache() {
            lock (_lock) {
                _cache = null;
            }
        }

        private Dictionary<string, string> LoadCacheFromDb() {
            // Здесь мы просто имитируем загрузку
            Thread.Sleep(100);
            return new Dictionary<string, string> {
                ["A"] = "ValueA",
                ["B"] = "ValueB"
            };
        }
    }

    internal class CacheManager_Refactored2 {
        private object _lock = new();
        private Lazy<Dictionary<string, string>> _cache = new(LoadCacheFromDb, LazyThreadSafetyMode.ExecutionAndPublication);

        public string GetValue(string key) => _cache.Value.TryGetValue(key, out var value) ? value : "Not found";

        public void ClearCache() {
            lock (_lock) {
                _cache = new(LoadCacheFromDb, LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }

        private static Dictionary<string, string> LoadCacheFromDb() {
            Thread.Sleep(100);
            return new Dictionary<string, string> {
                ["A"] = "ValueA",
                ["B"] = "ValueB"
            };
        }
    }

}
