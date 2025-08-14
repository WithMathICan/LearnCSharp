using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Linq;
using System.Runtime.CompilerServices;
using DataStructures;

public class Logger {
    public void Log(string message,
                    [CallerMemberName] string callerName = "",
                    [CallerFilePath] string filePath = "",
                    [CallerLineNumber] int lineNumber = 0) {
        Console.WriteLine($"Сообщение: {message}");
        Console.WriteLine($"Вызвано из: {callerName}");
        Console.WriteLine($"Файл: {filePath}");
        Console.WriteLine($"Строка: {lineNumber}");
    }
}

struct MyStruct {
    int x;
    int y;

    public MyStruct(int _x, int _y) {
        x = _x;
        y = _y;
    }

    public override string ToString() {
        return $"{x} -- {y}";
    }
}

struct MyStruct2 : IComparable<MyStruct2> {
    int x;

    public MyStruct2(int _x) {
        x = _x;
    }

    public override string ToString() {
        return x.ToString();
    }

    public int CompareTo(MyStruct2 other) {
        if (x > other.x) return 1;
        if (x < other.x) return -1;
        return 0;
    }

    public static implicit operator MyStruct2(int v) {
        return new MyStruct2(v);
    }

    public static bool operator < (MyStruct2 one, MyStruct2 other) {
        return one.x < other.x; 
    }

    public static bool operator <=(MyStruct2 one, MyStruct2 other) {
        return one.x <= other.x;
    }

    public static bool operator >(MyStruct2 one, MyStruct2 other) {
        return one.x > other.x;
    }

    public static bool operator >=(MyStruct2 one, MyStruct2 other) {
        return one.x > other.x;
    }
}

enum T {
    T1, T2
}

class Program {

    static void TestFunc(MyStruct2 m) {
        Console.WriteLine(m);
    }

    static void TestFunc(MyStruct m) {
        Console.WriteLine(m);
    }

    static void Main1() {
        //Set.TestSet();
        //Logger logger = new Logger();
        //logger.Log("Тестовое сообщение");
        //AnotherMethod();
        //const int size = 100 * 1000;

        //Stopwatch stopWatch = new Stopwatch();
        //stopWatch.Start();
        //List<MyStruct> list = [];
        ////ArrayList list = new ArrayList();
        //for (int i = 0; i < size; i++) {
        //    MyStruct s = new(i, i + 1); // Stack
        //    list.Add(s);
        //}
        //stopWatch.Stop();

        //int x = (int)1.2;
        //double y = 2;
        //float z = 1.2f;

        //MyStruct2 m = new(6);
        //MyStruct2 m2 = 6;
        //TestFunc(7);

        ////var arr = new MyStruct[size];
        //Console.WriteLine(stopWatch.Elapsed);

        //SortedSet<MyStruct2> sortedSet = [new MyStruct2(1), new MyStruct2(3)];

        //var s1 = new { time=1, value=3 };
        //int vv = s1.time;
        //vv = 5;

        //List<string> names = ["Alice", "Bob"];
        //List<int> ages = [21, 25];
        //var x = names.Where(c => c.Contains("Alice"));
        //var y = from n in names
        //        from p in ages
        //        select new { n, p };
        //int a = int.MaxValue;
        //int b = a + 1;


        //Number x1 = new Number(10);
        //Number x2 = x1.Clone();
        //x2.x = 20;
        Disposable.TestDisposable();
    }

    class Number {
        public int x;

        public Number(int _x) {
            x = _x;
        }

        public Number Clone() {
            return new Number(x);
        }
    }



    static void AnotherMethod() {
        Logger logger = new Logger();
        logger.Log("Сообщение из другого метода");
    }
}



