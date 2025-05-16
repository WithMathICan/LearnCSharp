using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EFPractice.Data {
    internal class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {
        public AppDbContext CreateDbContext(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            //optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=EFPracticeDB;Trusted_Connection=True;");
            optionsBuilder.UseSqlServer("Server=DESKTOP-7NQF8CQ\\SQLEXPRESS;Database=EFPract02;Trusted_Connection=True;TrustServerCertificate=True;");
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
