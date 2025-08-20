using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultyThreading {
    /*Есть код, который должен обеспечивать потокобезопасное получение и изменение значения конфигурационного параметра:*/
    /*Код компилируется, вроде бы все доступы к _config под lock. 
     * Но тут есть серьёзная проблема с многопоточкой, которая проявится в реальном приложении.
     * Найди её и скажи, как исправить.
     */
    internal class ConfigManager {
        private Dictionary<string, string> _config = new();
        private readonly object _lock = new();

        public string? Get(string key) {
            lock (_lock) {
                if (_config.TryGetValue(key, out var value)) {
                    return value;
                }
                return null;
            }
        }

        public void Set(string key, string value) {
            lock (_lock) {
                _config[key] = value;
            }
        }

        public Dictionary<string, string> GetAll() {
            lock (_lock) {
                return _config;
            }
        }

        // Refactor
        public FrozenDictionary<string, string> GetAll_1() {
            lock (_lock) {
                return _config.ToFrozenDictionary();
            }
        }

        public Dictionary<string, string> GetAll_2() {
            lock (_lock) {
                return new Dictionary<string, string>(_config);
            }
        }

        public IReadOnlyDictionary<string, string> GetAll_3() {
            lock (_lock) {
                return new ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(_config)
                );
            }
        }
    }

}
