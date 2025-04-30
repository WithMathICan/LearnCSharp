using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class CustomAttribute : Attribute {
        // Поля для хранения данных
        public string Description { get; }
        public int Version { get; }

        // Конструктор
        public CustomAttribute(string description, int version) {
            Description = description;
            Version = version;
        }
    }

    [Serializable]
    class Test : IComparable<Test> { // Updated to implement IComparable<Test>
        double x;
        public Test(double x) {
            this.x = x;
        }

        public int CompareTo(Test? other) { // Updated CompareTo to use Test instead of object
            if (other == null) return 1;
            return x.CompareTo(other.x);
        }
    }

    [Custom("Class Set", 3)]
    internal class Set {
        static public T Max<T>(T x, T y) where T : IComparable<T> {
            return x.CompareTo(y) > 0 ? x : y;
        }
        
        public int Max1(int x, int y) {
            return x.CompareTo(y) > 0 ? x : y;
        }
        [Custom("Method Print", 1)]
        static void Print(IEnumerable<int> items) {
            foreach (var item in items) {
                Console.WriteLine(item);
            }
            Console.WriteLine();
        }
        [Custom("Method TestSet", 2)]
        public static void TestSet() {
            var set = new HashSet<int> {
                       10, 30,2, 4, 15, 1000, 12677, 10, 36, 15
                   };
            Print(set);

            var set1 = new SortedSet<int> {
                       10, 30,2, 4, 15, 1000, 12677, 10, 36, 15
                   };
            Print(set1);
            Test x1 = new(6);
            Test x2 = new(12);
            Test x3 = Max(x1, x2); // This now works as Test implements IComparable<Test>

            

            var type = typeof(Set);
            foreach(var a in type.GetCustomAttributes<CustomAttribute>()) {
                Console.WriteLine($"{a.Description} {a.Version}");
            }
        }

        HashSet<Test> x = new HashSet<Test>();
    }
}
