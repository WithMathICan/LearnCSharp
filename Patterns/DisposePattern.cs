using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patterns {
    internal class BaseClass : IDisposable {
        private List<int> list;
        private bool isDisposeDone = false;

        internal BaseClass() {
            Console.WriteLine("Constructor BaseClass");
            list = new List<int>(1000);
            for (int i = 0; i < 1000; i++) list.Add(i);
        }

        protected virtual void Dispose(bool disposing) {
            if (!isDisposeDone) {
                if (disposing) {
                    Console.WriteLine("(Dispose) BaseClass");
                    list.Clear();
                    list.Capacity = 0;
                }
                Console.WriteLine("~BaseClass");
                isDisposeDone = true;
            }
        }
        ~BaseClass() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal class MainClass : BaseClass {
        private List<int> list;
        private bool isDisposeDone = false;

        internal MainClass() {
            Console.WriteLine("Constructor MainClass");
            list = new List<int>(1000);
            for (int i = 0; i < 1000; i++) list.Add(i);
        }

        protected override void Dispose(bool disposing) {
            if (!isDisposeDone) {
                if (disposing) {
                    Console.WriteLine("(Dispose) MainClass");
                    list.Clear();
                    list.Capacity = 0;
                }
                Console.WriteLine("~MainClass");
                isDisposeDone = true;
            }
            base.Dispose(disposing);
        }
        ~MainClass() {
            Dispose(false);
        }
    }

    internal class DisposePattern {
        static void Main() {
            using (new MainClass());
        }
    }
}
