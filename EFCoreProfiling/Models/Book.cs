using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCoreProfiling.Models {
    public class Book {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public int Year { get; set; }
        public string Genre { get; set; } = "";
        public decimal Price { get; set; }

        // Foreign key
        public int AuthorId { get; set; }

        // Navigation property
        public virtual Author? Author { get; set; }
    }
}
