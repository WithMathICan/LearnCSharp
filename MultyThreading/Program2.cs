using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultyThreading {
    internal class Program2 {
        static void Main2() {
            var orderManager = new OrderManager2 ();

            List<Task> tasks = [];
            for (int i = 0; i < 1000000; i++) {
                tasks.Add(Task.Run(() => orderManager.AddOrder("X")));
            }
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine(orderManager.GetOrders().Count);

            IReadOnlyList<string> orders = orderManager.GetOrders();

            Console.WriteLine(orderManager.GetOrders().Count);
        }
    }

    public class OrderManager2 {
        private readonly List<string> _orders = [];
        private readonly object _lock = new();

        public void AddOrder(string order) {
            lock (_lock) {
                _orders.Add(order);
            }
        }

        public IReadOnlyList<string> GetOrders() {
            lock (_lock) {
                return _orders.AsReadOnly();
            }
        }
    }

    //public class OrderManager3 {
    //    private readonly ConcurrentBag<string> _orders = [];

    //    public void AddOrder(string order) {
    //        _orders.Add(order);
    //    }

    //    public List<string> GetOrders() {
    //        return _orders.ToList();
    //    }
    //}
}
