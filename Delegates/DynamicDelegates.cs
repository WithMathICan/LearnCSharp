using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Delegates {
    internal class DynamicDelegates {

        delegate int Calculator(int x, int y);

        static int Add(int a, int b) => a + b;

        static void Main1() {
            Calculator calc = Add;

            // Normal invocation
            int result1 = calc(5, 3);
            Console.WriteLine($"Normal invocation: {result1}");

            // Dynamic invocation
            object result2 = calc.DynamicInvoke(5, 3);
            Console.WriteLine($"Dynamic invocation: {result2}");

            // Dynamic invocation with object array
            object[] parameters = { 10, 20 };
            object result3 = calc.DynamicInvoke(parameters);
            Console.WriteLine($"Dynamic invocation with array: {result3}");
        }
    }
}
