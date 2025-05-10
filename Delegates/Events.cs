using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Delegates {
    internal class Events {

        internal event Action<int, int> AddNumbers;

        void LogAdding(int x, int y) {
            Console.WriteLine($"Add {x} {y}");
        }

        internal int Add(int x, int y) {
            AddNumbers?.Invoke(x, y);
            LogAdding(x, y);
            return x + y;
        }
    }
}
