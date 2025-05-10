using Delegates;

class Program {
    delegate int AddDelegate(int x, int y);
    delegate int MyFunc(int x);

    static void FindValue(int x, int y, AddDelegate func) {
        Console.WriteLine(func(x, y));
    }

    static void Main() {
        //AddDelegate add = new AddDelegate((int x, int y) => x + y);
        //Console.WriteLine(add(4, 6));
        //FindValue(1, 2, add);

        //Func<int, int> inc = (int x) => {
        //    Console.WriteLine("From inc");
        //    return x + 1;
        //};
        //Func<int, int> inc5 = (int x) => {
        //    Console.WriteLine("From inc5");
        //    return x + 5;
        //};
        //Func<int, int> f = inc;
        //f += inc;
        //f += inc5;


        //Console.WriteLine(f(4));

        var events = new Events();
        events.AddNumbers += delegate (int x, int y) {
            Console.WriteLine($"Adding called with arguments {x}, {y}");
        };
        events.AddNumbers += delegate (int x, int y) {
            Console.WriteLine($"!!!!  Adding called with arguments {x}, {y}");
        };
        int result = events.Add(3, 4);
        Console.WriteLine($"Result of Add(3, 4) = {result}");
    }
}
