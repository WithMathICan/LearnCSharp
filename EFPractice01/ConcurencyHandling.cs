using EFPractice01.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFPractice01 {
    internal class ConcurencyHandling {

        internal async Task TestConcurencyToken() {
            using var context = CourseContext.Create();
            var instructor1 = context.Instructors.First();
            var instructor2 = context.Instructors.First();
            instructor1.Name += "!";
            Console.WriteLine(instructor1.Name);
            Console.WriteLine(instructor2.Name);

        }
    }
}
