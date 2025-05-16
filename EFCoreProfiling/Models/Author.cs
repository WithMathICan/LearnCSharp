using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCoreProfiling.Models {
    public class Author {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }

        // Navigation property
        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
