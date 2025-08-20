using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultyThreading {
    public class SharedResource {
        private Dictionary<int, string> _data = new();
        public void AddOrUpdate(int key, string value) {
            if (_data.ContainsKey(key))
                _data[key] = value;
            else
                _data.Add(key, value);
        }
        public string? GetValue(int key) {
            return _data.TryGetValue(key, out var value) ? value : null;
        }
    }

    public class SharedResource_ThreadSafe {
        private readonly Dictionary<int, string> _data = new();
        private readonly object _lock = new();
        public void AddOrUpdate(int key, string value) {
            lock (_lock) {
                _data[key] = value;
            }
        }
        public string? GetValue(int key) {
            lock (_lock) {
                return _data.TryGetValue(key, out var value) ? value : null;
            }
        }
    }

    public class SharedResource_RWLock {
        private readonly Dictionary<int, string> _data = new();
        private readonly ReaderWriterLockSlim _rwLock = new();

        public void AddOrUpdate(int key, string value) {
            _rwLock.EnterWriteLock();
            try {
                _data[key] = value;
            } finally {
                _rwLock.ExitWriteLock();
            }
        }

        public string? GetValue(int key) {
            _rwLock.EnterReadLock();
            try {
                return _data.TryGetValue(key, out var value) ? value : null;
            } finally {
                _rwLock.ExitReadLock();
            }
        }
    }

    public class SharedResource_Concurrent {
        private readonly ConcurrentDictionary<int, string> _data = new();
        public void AddOrUpdate(int key, string value) {
            _data.AddOrUpdate(key, value, (k, oldValue) => value);
        }
        public string? GetValue(int key) {
            return _data.TryGetValue(key, out var value) ? value : null;
        }
    }
}
