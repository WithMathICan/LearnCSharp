using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurency {
    internal class Async {
       
        static async Task Main() {
            object x = 5;
            object y = 6;
            object z = x;
            Console.WriteLine($"{x == y}, {z == x}");
            z = 8;
            Console.WriteLine($"{z == x}, {z}, {x}");
            TestAsync ta = new();
            //ta.PrintValue2();
            await ta.PrintValue3();
            Console.ReadLine();
        }

        static void Main1() {
            TestAsync ta = new();
            Console.WriteLine($"Main Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            ta.PrintValue2();
            Console.WriteLine($"Main after PrintValue2 Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            Console.ReadLine();
        }
    }

    class TestAsync {
        object obj = new object();
        public async Task PrintValue() {
            //lock (obj) {
                Console.WriteLine($"Start PrintValue Thread ID : {Thread.CurrentThread.ManagedThreadId}");
                Task t = Task.Delay(1000);
                await t;
                Console.WriteLine($"End PrintValue Thread ID : {Thread.CurrentThread.ManagedThreadId}");
            //}
        }

        public void PrintValue2() {
            Console.WriteLine($"Start PrintValue2 Thread ID : {Thread.CurrentThread.ManagedThreadId}");
            Task t = Task.Delay(1000);
            System.Runtime.CompilerServices.TaskAwaiter awaiter = t.GetAwaiter();
            awaiter.OnCompleted(() => {
                Console.WriteLine($"End PrintValue2 Thread ID : {Thread.CurrentThread.ManagedThreadId}");
            });
        }

        public async Task PrintValue3() {
            Console.WriteLine($"Start PrintValue3 Thread ID : {Thread.CurrentThread.ManagedThreadId}");
            Task<int> t1 = Task.FromResult(1);
            int x = await t1;
            Console.WriteLine($"StaPrintValue3 Thread ID : {Thread.CurrentThread.ManagedThreadId}");

            Task t = Task.Delay(1000);
            await t;
            Console.WriteLine($"End PrintValue3 Thread ID : {Thread.CurrentThread.ManagedThreadId}");

        }
    }
}
