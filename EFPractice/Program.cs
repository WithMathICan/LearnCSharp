using Microsoft.EntityFrameworkCore;
using EFPractice.Data;
using EFPractice.Models;

namespace EFPractice {
    class Program {
        static async Task Main(string[] args) {
            var contextFactory = new AppDbContextFactory();

            using (var context = contextFactory.CreateDbContext([])) {
                //await context.Database.EnsureCreatedAsync(); // Creates DB if it doesn’t exist

                // Seed data
                //if (!context.Authors.Any()) {
                //    var author = new Author { Name = "John Doe" };
                //    context.Authors.Add(author);
                //    context.Books.Add(new Book { Title = "My First Book", Author = author });
                //    await context.SaveChangesAsync();
                //}

                // Query data
                var authors = await context.Author
                    .Include(a => a.Books)
                    .ToListAsync();
                foreach (var author in authors) {
                    Console.WriteLine($"Author: {author.Name}");
                    foreach (var book in author.Books) {
                        Console.WriteLine($"- Book: {book.Name}");
                    }
                }
            }
        }
    }
}