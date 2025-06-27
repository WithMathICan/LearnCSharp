using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures {
    internal class MyList : IDisposable {
        int size;
        List<int> list;

        public MyList(int _size) {
            size = _size;
            list = new List<int>(size);
            for (int i = 0; i < size; i++) list.Add(i);
        }

        public void Dispose() {
            list.Clear();
            list.Capacity = 0;
            Console.WriteLine($"Current memory usage: {GC.GetTotalMemory(true)}");
        }
    }

    class List2 : IDisposable {
        List<MyList> list;
        public MyList l = new MyList(20);

        public List2(int size) {
            list = new List<MyList>(size);
            for (int i = 0; i < size; i++) {
                list.Add(new MyList(1000 * 1000 * 100));
            }
        }

        public void Dispose() {
            list.ForEach(x => x.Dispose());
        }
    }

    internal class Disposable {
        public static void TestDisposable() {
            //MyList l = new MyList(1000 * 1000 * 100);
            //List<MyList> myLists = new List<MyList>(10);
            //for (int i = 0; i < 10; i++) myLists.Add(new MyList(1000 * 1000 * 100));
            //foreach (var x in myLists) {
            //    x.Dispose();
            //}
            Console.WriteLine($"Current memory usage: {GC.GetTotalMemory(true)}");
            using (List2 l = new List2(10)) {
                Console.WriteLine($"Current memory usage: {GC.GetTotalMemory(true)}");
            }
            Console.WriteLine(6);
        }
    }
}
