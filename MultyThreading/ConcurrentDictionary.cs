using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultyThreading {
    internal class ConcurrentDictionary {
        internal class Counters {
            private readonly ReaderWriterLockSlim _rw = new(LockRecursionPolicy.SupportsRecursion);
            private readonly Dictionary<string, int> _map = [];

            public int Get(string key) {
                _rw.EnterReadLock();
                try {
                    return _map.TryGetValue(key, out var v) ? v : 0;
                } finally { _rw.ExitReadLock(); }
            }

            public void Increment(string key) {
                _rw.EnterReadLock();
                try {
                    if (!_map.TryGetValue(key, out var v)) {
                        _rw.EnterWriteLock(); // Dead lock here
                        try { _map[key] = 1; } finally { _rw.ExitWriteLock(); }
                    } else {
                        _map[key] = v + 1; // <-- тут записи под read-lock
                    }
                } finally { _rw.ExitReadLock(); }
            }
        }

        internal class Counters_refactored {
            private readonly ReaderWriterLockSlim _rw = new();
            private readonly Dictionary<string, int> _map = [];

            public int Get(string key) {
                _rw.EnterReadLock();
                try {
                    return _map.TryGetValue(key, out var v) ? v : 0;
                } finally { _rw.ExitReadLock(); }
            }

            public void Increment(string key) {
                _rw.EnterUpgradeableReadLock();
                try {
                    if (!_map.TryGetValue(key, out var v)) {
                        _rw.EnterWriteLock();
                        try { _map[key] = 1; } finally { _rw.ExitWriteLock(); }
                    } else {
                        _rw.EnterWriteLock();
                        try { _map[key] = v + 1; } finally { _rw.ExitWriteLock(); }
                    }
                } finally { _rw.ExitUpgradeableReadLock(); }
            }
        }

        internal class CountersConcurrent {
            private readonly ConcurrentDictionary<string, int> _map = new();
            public int Get(string key) => _map.TryGetValue(key, out var v) ? v : 0;
            public void Increment(string key) => _map.AddOrUpdate(key, 1, (_, old) => old + 1);
        }
    }
}
