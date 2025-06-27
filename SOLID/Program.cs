
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SOLID {



    class Program() {

       

        public static void M2() {
            //int a = 10;
            //int b = a++;
            int a = 10;
            int b = a;
            a += 1;
        }

        public static void M1() {
            //int a = 10;
            //int b = ++a;
            int a = 10;
            a += 1;
            int b = a;
        }

        public static int PPOp(ref int a) {
            a += 1;
            return a;
        }

        public static int OpPP(ref int a) {
            int x = a;
            a += 1;
            return x;
        }

        

        static void Main1() {
            //int a = 10;
            //int b = a++;
            //int b = PPOp(ref a);
            //int b = OpPP(ref a);
            //Console.WriteLine($"a = {a}, b = {b}");
            //Console.WriteLine($"(100, 2) => {powerSum(100, 2)}");
            
        }

        

        public static int powerSum(int X, int N, int initialVal = 1) {
            int count = 0;
            for (int i = initialVal; i <= X; i++) {
                int pow = (int)Math.Pow(i, N);
                if (pow > X) {
                    break;
                } else if (pow == X) {
                    return 1;
                } else {
                    int Y = X - pow;
                    count += powerSum(Y, N, i+1);
                }
            }
            
            return count;
        }
    }
}
