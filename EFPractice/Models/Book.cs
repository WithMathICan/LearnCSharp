using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFPractice.Models {
    internal class Book {
        public int Id { get; set; }
        public string Name { get; set; }
        public int AuthorId { get; set; } // Foreign key
        public Author Author { get; set; } // Navigation property
    }
}
