namespace MultyThreading {
    namespace CM1 {
        internal class Connection {
            private readonly string _name;
            private readonly Action<string> _onClosed;

            public Connection(string name, Action<string> onClosed) {
                _name = name;
                _onClosed = onClosed;
            }

            public void Close() {
                // ... здесь реальное закрытие сокетов/ресурсов ...
                // А затем уведомляем менеджер, что соединение можно убрать из словаря:
                _onClosed(_name);
            }
        }

        internal class ConnectionManager {
            private readonly object _lock = new();
            private readonly Dictionary<string, Connection> _connections = new();

            public void Add(string name) {
                // Для простоты создаём Connection тут, прокидывая колбэк Remove
                var conn = new Connection(name, Remove);

                lock (_lock) {
                    _connections[name] = conn;
                }
            }

            public void Remove(string name) {
                lock (_lock) {
                    _connections.Remove(name);
                }
            }

            public void CloseAll() {
                lock (_lock) {
                    foreach (var conn in _connections.Values) {
                        conn.Close(); // во время этого вызова может прийти Remove(name)
                    }
                }
            }
        }

        internal class ConnectionManager_Refactored {
            private readonly object _lock = new();
            private readonly Dictionary<string, Connection> _connections = new();

            public void Add(string name) {
                var conn = new Connection(name, Remove);

                lock (_lock) {
                    _connections[name] = conn;
                }
            }

            public void Remove(string name) {
                lock (_lock) {
                    _connections.Remove(name);
                }
            }

            public void CloseAll() {
                List<Connection> snapshot;
                lock (_lock) {
                    snapshot = _connections.Values.ToList();
                }

                foreach (var conn in snapshot) {
                    conn.Close();
                }
            }

        }
    }

    namespace CM2 {
        class Connection {
            private readonly ConnectionManager _manager;
            private readonly string _name;
            private bool _isClosed;

            public Connection(ConnectionManager manager, string name) {
                _manager = manager;
                _name = name;
            }

            public void Close() {
                if (_isClosed) return;
                _isClosed = true;
                _manager.Remove(_name); // ← обратный вызов в менеджер!
            }
        }

        class ConnectionManager {
            private readonly object _lock = new();
            private readonly Dictionary<string, Connection> _connections = new();

            public void Add(string name, Connection conn) {
                lock (_lock) { _connections[name] = conn; }
            }

            public void Remove(string name) {
                lock (_lock) { _connections.Remove(name); }
            }

            public void CloseAll() {
                List<Connection> snapshot;
                lock (_lock) {
                    snapshot = _connections.Values.ToList();
                }

                foreach (var conn in snapshot) {
                    conn.Close();
                }
            }

            public void CloseDefinitelyAll() {
                while (true) {
                    List<Connection> snapshot;
                    lock (_lock) {
                        if (_connections.Count == 0)
                            break; // все закрыли — выходим

                        snapshot = _connections.Values.ToList();
                    }

                    foreach (var conn in snapshot) {
                        conn.Close();
                        // conn.Close() внутри сам вызовет Remove(name) под своим lock
                    }
                }
            }

        }

    }

}
