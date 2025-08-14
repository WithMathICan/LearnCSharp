using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Concurency {
    //internal class ThreadPlusEvent {
    //    static void Main() {
    //        int y = GetYear();
    //        Console.WriteLine(y);
    //    }

    //    static int GetYear() {
    //        return 2025;
    //    }
    //}

    //internal class ThreadPlusEvent {

    //    static event EventHandler<int> RaizeYear;

    //    static void Main() {
    //        RaizeYear += (x, y) => {
    //            Console.WriteLine(y);
    //        };
    //        Thread th = new Thread(new ThreadStart(GetYear));
    //        th.Start();
    //        Console.WriteLine("Thread started");
    //    }

    //    static void GetYear() {
    //        Thread.Sleep(100);
    //        RaizeYear?.Invoke(null, 2025);
    //    }
    //}

    //internal class ThreadPlusEvent {

    //    public async Task<int> GetDataAsync() {
    //        Console.WriteLine("Starting...");
    //        await Task.Delay(1000); // Имитация долгой операции
    //        Console.WriteLine("Finished!");
    //        return 42;
    //    }


    //    static void Main() {
    //        int x = 5;
    //        object obj = x;
    //        Console.WriteLine($"Size of int {Marshal.SizeOf(x)}, {GC.GetTotalMemory(true)}");
    //        Console.WriteLine($"Start Main {Thread.CurrentThread.ManagedThreadId}");

    //        Task t = new Task(() => {
    //            Console.WriteLine($"Start {Thread.CurrentThread.ManagedThreadId}");
    //            Thread.Sleep(1000);
    //            Console.WriteLine($"End {Thread.CurrentThread.ManagedThreadId}");
    //        });
    //        t.Start();
    //        t.ContinueWith(c => Console.WriteLine($"End Of Task {Thread.CurrentThread.ManagedThreadId}"));
    //        Console.WriteLine($"End Main {Thread.CurrentThread.ManagedThreadId}");
    //        t.Wait();
    //    }

    //}
}
