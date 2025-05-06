using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patterns {

    interface IArithmeticOperations {
        int Add(int x, int y);
        int Multiply(int x, int y);
    }

    class AOImplementation : IArithmeticOperations {
        public int Add(int x, int y) => x + y;
        public int Multiply(int x, int y) => x * y;
    }

    class AOLoggerImplementation : IArithmeticOperations {
        IArithmeticOperations BaseImplementation;

        public AOLoggerImplementation(IArithmeticOperations arithmetic) {
            BaseImplementation = arithmetic;
        }

        public int Add(int x, int y) {
            Console.WriteLine("Add method called");
            return BaseImplementation.Add(x, y);
        }

        public int Multiply(int x, int y) {
            Console.WriteLine("Multiply method called");
            return BaseImplementation.Multiply(x, y);
        }
    }

    class AOIncrementImplementation : IArithmeticOperations {
        IArithmeticOperations BaseImplementation;

        public AOIncrementImplementation(IArithmeticOperations arithmetic) {
            BaseImplementation = arithmetic;
        }

        public int Add(int x, int y) {
            Console.WriteLine("Add method called AOIncrementImplementation");
            return BaseImplementation.Add(x, y) + 1;
        }

        public int Multiply(int x, int y) {
            Console.WriteLine("Multiply method called AOIncrementImplementation");
            return BaseImplementation.Multiply(x, y) + 1;
        }
    }

    internal class Decorator {

        static void Main() {
            IArithmeticOperations ao = new AOImplementation();
            ao = new AOLoggerImplementation(ao);
            ao = new AOIncrementImplementation(ao);
            Console.WriteLine(ao.Add(3, 4));
        }
    }
}
