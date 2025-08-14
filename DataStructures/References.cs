using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures {
    internal record class Book(int Id, string Name);
    internal class References {
        static readonly List<Book> books = [
                new Book(1, "Name01"),
                new Book(2, "Name02"),
            ];
        public static void Task5(Book model) {
            var item = books.FirstOrDefault(s => s.Id == model.Id);
            books.FirstOrDefault(s => s.Id == model.Id);
            item = model;
        }
        static void Main() {

            Task5(new Book(1, "Name001"));
            foreach (var item in books) {
                Console.WriteLine(item);
            }
        }
    }
}
