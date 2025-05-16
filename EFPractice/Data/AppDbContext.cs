using EFPractice.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFPractice.Data {
    internal class AppDbContext : DbContext {
        public DbSet<Author> Author { get; set; }
        public DbSet<Book> Book { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            // Configure relationships (optional, EF can infer this)
            modelBuilder.Entity<Author>()
                .HasMany(a => a.Books)
                .WithOne(b => b.Author)
                .HasForeignKey(b => b.AuthorId);
        }
    }
}
