using EFPractice01.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace EFPractice01.Data {
    public class CourseContext : DbContext{
        public DbSet<Course> Courses { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<QuizStudent> QuizStudents { get; set; }
        public DbSet<Operation> Operations { get; set; }
        //public DbSet<CourseInstructor> CourseInstructors { get; set; }
        //public DbSet<CourseStudent> CourseStudents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            optionsBuilder
                .UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                //.LogTo(Console.WriteLine, LogLevel.Information);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<QuizStudent>()
                .HasKey(qs => new { qs.StudentId, qs.QuizId, qs.QuizAttemptedAt });

            // Course
            modelBuilder.Entity<Course>()
                .HasMany(course => course.Instructors)
                .WithMany(instructor => instructor.Courses);

            modelBuilder.Entity<Course>()
                .HasMany(course => course.Students)
                .WithMany(student => student.Courses);

            modelBuilder.Entity<Course>()
                .HasMany(course => course.Reviews)
                .WithOne(review => review.Course)
                .HasForeignKey(review => review.CourseId);

            modelBuilder.Entity<Course>()
                .HasMany(course => course.Lessons)
                .WithOne(lesson => lesson.Course)
                .HasForeignKey(lesson => lesson.CourseId);

            modelBuilder.Entity<Course>()
                .Property(c => c.Price)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Course>()
                .HasQueryFilter(c => !c.IsDeleted); 

            // Instructor
            modelBuilder.Entity<Instructor>()
                .HasMany(i => i.Mentees)
                .WithOne(i => i.Mentor)
                .HasForeignKey(i => i.MentorId);

            modelBuilder.Entity<Instructor>()
                .HasIndex(i => i.Email)
                .IsUnique();

            modelBuilder.Entity<Instructor>()
                .Property(i => i.RowVersion)
                .IsRowVersion();

            // Lesson
            modelBuilder.Entity<Lesson>()
                .HasMany(l => l.Quizzes)
                .WithOne(q => q.Lesson)
                .HasForeignKey(q => q.LessonId);

            // Quiz
            modelBuilder.Entity<Quiz>()
                .HasMany(q => q.StudentResults)
                .WithOne(sr => sr.Quiz)
                .HasForeignKey(sr => sr.QuizId);

            // Student
            modelBuilder.Entity<Student>()
                .HasMany(s => s.QuizesResults)
                .WithOne(qr => qr.Student)
                .HasForeignKey(qr => qr.StudentId);

            modelBuilder.Entity<Student>()
                .HasMany(s => s.Reviews)
                .WithOne(r => r.Student)
                .HasForeignKey(r => r.StudentId);

            modelBuilder.Entity<Operation>()
                .HasIndex(o => o.OperationId)
                .IsUnique();
        }
    }
}
