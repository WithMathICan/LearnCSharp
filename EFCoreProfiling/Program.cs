using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EFCoreProfiling.Data;
using EFCoreProfiling.Models;
using EFCoreProfiling.Helper;

namespace EFCoreProfiling {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Entity Framework Core with Query Profiling Demo");

            // Set up dependency injection
            var serviceCollection = new ServiceCollection();

            // Configure the DbContext with profiling
            var profiler = new QueryProfiler();

            serviceCollection.AddDbContext<BookStoreContext>(options => {
                options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=BookStoreDemo;Trusted_Connection=True;");
                // Enable query logging
                options.EnableSensitiveDataLogging()
                      .EnableDetailedErrors()
                      .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);

                // Add our interceptor for profiling
                options.AddInterceptors(new ProfilerDbCommandInterceptor(profiler));
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Get the DbContext
            using (var scope = serviceProvider.CreateScope()) {
                var db = scope.ServiceProvider.GetRequiredService<BookStoreContext>();

                // Create the database and schema if it doesn't exist
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                // Example 1: Basic Query - Get all books
                QueryProfiler.ProfileQuery("Get All Books", db, () => {
                    var books = db.Books.ToList();
                    Console.WriteLine($"Found {books.Count} books");
                });

                // Example 2: Inefficient Query with N+1 problem
                QueryProfiler.ProfileQuery("N+1 Problem Demo", db, () => {
                    var books = db.Books.ToList();
                    foreach (var book in books) {
                        // This causes N+1 problem - separate query for each author
                        Console.WriteLine($"Book: {book.Title}, Author: {db?.Authors?.Find(book.AuthorId)?.Name}");
                    }
                });

                // Example 3: Optimized Query with Include
                QueryProfiler.ProfileQuery("Optimized with Include", db, () => {
                    var books = db.Books.Include(b => b.Author).ToList();
                    foreach (var book in books) {
                        // No additional queries needed
                        Console.WriteLine($"Book: {book.Title}, Author: {book.Author.Name}");
                    }
                });

                // Example 4: Projection to reduce data transfer
                QueryProfiler.ProfileQuery("Using Projection", db, () => {
                    var bookDtos = db.Books
                        .Select(b => new {
                            Title = b.Title,
                            AuthorName = b.Author.Name
                        })
                        .ToList();

                    foreach (var book in bookDtos) {
                        Console.WriteLine($"Book: {book.Title}, Author: {book.AuthorName}");
                    }
                });

                // Example 5: Using async methods
                QueryProfiler.ProfileQuery("Async Query Demo", db, () => {
                    var booksTask = db.Books
                        .Include(b => b.Author)
                        .Where(b => b.Price > 10)
                        .ToListAsync();

                    booksTask.Wait(); // Just for demo - typically you'd use await

                    var expensiveBooks = booksTask.Result;
                    Console.WriteLine($"Found {expensiveBooks.Count} books over $10");
                });

                // Example 6: Raw SQL query for complex scenarios
                QueryProfiler.ProfileQuery("Raw SQL Query", db, () => {
                    var books = db.Books
                        .FromSqlRaw("SELECT * FROM Books WHERE Year > 1980 ORDER BY Year")
                        .Include(b => b.Author)
                        .ToList();

                    Console.WriteLine($"Found {books.Count} books published after 1980");
                });

                // Example 7: Deferred execution vs immediate execution
                QueryProfiler.ProfileQuery("Deferred vs Immediate Execution", db, () => {
                    // Deferred - query not executed yet
                    var booksQuery = db.Books.Where(b => b.Genre == "Fantasy");

                    Console.WriteLine("Query defined but not executed yet");

                    // Now the query executes
                    var fantasyBooks = booksQuery.ToList();
                    Console.WriteLine($"Found {fantasyBooks.Count} fantasy books");
                });

                // Example 8: Using compiled queries for better performance
                var getBooksByGenreQuery = EF.CompileQuery(
                    (BookStoreContext context, string genre) =>
                        context.Books.Where(b => b.Genre == genre));

                QueryProfiler.ProfileQuery("Compiled Query", db, () => {
                    var horrorBooks = getBooksByGenreQuery(db, "Horror").ToList();
                    Console.WriteLine($"Found {horrorBooks.Count} horror books using compiled query");
                });
            }

            Console.WriteLine("\nDemo completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}