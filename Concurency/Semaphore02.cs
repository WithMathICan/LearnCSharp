
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurency {
    internal class Semaphore02(int initial, int max) {
        private int _initial = initial;
        private readonly int _max = max;
        private object _lock = new object();

        public void Wait() {
            lock (_lock) {
                while (_initial <= 0) {
                    Monitor.Wait(_lock); // Block if no slots available
                }
                _initial--;
            }
        }

        //public async Task WaitAsync() {
        //    await Task.Run(() => Wait());
        //}

        public void Release() {
            lock (_lock) {
                if (_initial < _max) { 
                    _initial++;
                    Monitor.Pulse(_lock);
                }
            }
        }
    }
}
