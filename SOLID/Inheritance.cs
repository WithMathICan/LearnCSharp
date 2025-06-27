using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOLID {
    internal class Inheritance {
        static void Main() {
            Employee zam = new ZamDirector();
            Console.WriteLine(zam.GetRole());
            Employee zam1 = new ZamDirector1();
            Console.WriteLine(zam1.ToString());
        }
    }

    public class Employee {
        public virtual string GetRole() => "Employee";
    }

    public class Manager : Employee {
        public override string GetRole() => "Manager";
    }

    public class Director : Manager {
        public override string GetRole() => "Director";
    }

    public class ZamDirector : Director {
        public new string GetRole() => "ZamDirector";
    }

    public class ZamDirector1 : ZamDirector {
        public override string ToString() => GetRole();
    }
}
