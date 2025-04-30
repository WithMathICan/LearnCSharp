using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures {
    interface IExample {
        void Print();
    }

    struct MyStruct3 : IExample {
        public string message;
        public void Print() {
            Console.WriteLine(message);
        }
    }


    internal class StructInterfaces {

        // With boxing
        static void Method1(IExample ex) {

        }

        // Without boxing
        static void Method2<T>(T ex) where T : IExample {

        }

        static void Test() {
            MyStruct3 myStruct = new MyStruct3() { message = "Test" };
            Method1(myStruct);
            Method2(myStruct);
            IExample myStruct1 = myStruct; // Boxing
            myStruct.Print();
        }
    }
}
