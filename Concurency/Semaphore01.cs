using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurency {
    internal class Semaphore01(int startValue, int reservedValue) {
        private int StartValue = startValue;
        private readonly int ReservedValue = reservedValue;
        private bool IsBlocked = false;
        private object _lock = new();

        public async Task Wait() {
            while(IsBlocked) {
                await Task.Delay(100);
            }
            if (StartValue >= ReservedValue) {
                throw new InvalidOperationException("StartValue >= ReservedValue");
            }
            lock (_lock) {
                StartValue++;
                if (StartValue == ReservedValue) {
                    IsBlocked = true;
                }
            }
        }

        public void Release() {
            lock (_lock) {
                StartValue = Math.Max(0, StartValue - 1);
                IsBlocked = false;
            }
        }
    }

    internal class TestSemaphore01 {

    }
}
