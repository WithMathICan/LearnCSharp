using Microsoft.EntityFrameworkCore;
using EFCoreProfiling.Models;
using EFCoreProfiling.Helper;

namespace EFCoreProfiling.Data {
    public class BookStoreContext : DbContext {
        public QueryProfiler QueryLogger { get; private set; }

        public BookStoreContext(DbContextOptions<BookStoreContext> options)
            : base(options) {
            QueryLogger = new QueryProfiler();
        }

        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            // Configure entity relationships
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId);

            // Seed data
            modelBuilder.Entity<Author>().HasData(
                new Author { Id = 1, Name = "George Orwell", BirthDate = new DateTime(1903, 6, 25) },
                new Author { Id = 2, Name = "J.K. Rowling", BirthDate = new DateTime(1965, 7, 31) },
                new Author { Id = 3, Name = "Stephen King", BirthDate = new DateTime(1947, 9, 21) }
            );

            modelBuilder.Entity<Book>().HasData(
                new Book { Id = 1, Title = "1984", Year = 1949, Genre = "Dystopian", Price = 12.99m, AuthorId = 1 },
                new Book { Id = 2, Title = "Animal Farm", Year = 1945, Genre = "Political Satire", Price = 9.99m, AuthorId = 1 },
                new Book { Id = 3, Title = "Harry Potter and the Philosopher's Stone", Year = 1997, Genre = "Fantasy", Price = 15.99m, AuthorId = 2 },
                new Book { Id = 4, Title = "Harry Potter and the Chamber of Secrets", Year = 1998, Genre = "Fantasy", Price = 15.99m, AuthorId = 2 },
                new Book { Id = 5, Title = "The Shining", Year = 1977, Genre = "Horror", Price = 14.99m, AuthorId = 3 },
                new Book { Id = 6, Title = "It", Year = 1986, Genre = "Horror", Price = 19.99m, AuthorId = 3 }
            );
        }
    }
}
