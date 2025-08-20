using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MultyThreading {
    /*Поле _stopRequested сделано volatile, чтобы другой поток видел изменения.
Кажется, что гонок нет — один поток читает, другой пишет, и volatile гарантирует видимость.
Почему этот код всё равно может быть некорректен и выдавать неожиданные значения Counter?
Подсказка: дело не в том, что while бесконечный, и не в том, что volatile работает неправильно.*/
    internal class VolatileExample {
        private volatile bool _stopRequested;
        private int _counter;

        public void Start() {
            var t1 = new Thread(Worker);
            t1.Start();

            Thread.Sleep(100);
            _stopRequested = true;
            t1.Join();
            Console.WriteLine($"Counter: {string.Format(CultureInfo.InvariantCulture, "{0:N0}", _counter)}");
        }

        private void Worker() {
            while (!_stopRequested) {
                _counter++;
            }
        }
    }

}
