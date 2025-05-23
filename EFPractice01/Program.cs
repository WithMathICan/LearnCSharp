﻿using Bogus;
using EFPractice01;
using EFPractice01.Data;
using EFPractice01.Models;
using Microsoft.EntityFrameworkCore;

internal class Program {
    static async Task Main() {
        var testingEF = new TestingEF();
        using var context = CourseContext.Create();
        ConcurencyHandling ch = new();
        OperationId testOperationId = new();
        try {
            //await testingEF.CreateCourses();
            //await testingEF.InsertNewInstructors(100, context);
            //await ch.TestConcurencyToken();
            //await testOperationId.TestOperationId();
        } catch (Exception ex) {
            Console.WriteLine(ex);
        }
    }
}

class Exercises {
    internal async Task ReadCoursesAndLessons() {
        using var context = CourseContext.Create();
        try {
            var courses = await context.Courses.Include(c => c.Lessons).AsNoTracking().ToListAsync();
            foreach (var c in courses) {
                Console.WriteLine($"Course: '{c.Name}' has {c.Lessons.Count} lessons");
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error reading courses: {ex.Message}");
        }
    }

    internal async Task AddStudentsWithEnrollment() {
        using var context = CourseContext.Create();
        Faker<Student> studentFaker = new Faker<Student>()
                .RuleFor(c => c.Name, f => f.Person.FullName)
                .RuleFor(c => c.Email, f => f.Internet.Email())
                .RuleFor(c => c.EnrollmentDate, f => DateTime.Now);
        var students = studentFaker.Generate(20);
        var courses = await context.Courses.Skip(10).Take(20).ToListAsync();
        if (courses.Count < 20) {
            throw new InvalidOperationException("Not enough courses");
        }
        for (int i = 0; i < 20; i++) {
            students[i].Courses.Add(courses[i]);
        }
        context.Students.AddRange(students);
        var transaction = await context.Database.BeginTransactionAsync();
        try {
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            Console.WriteLine("Students successfully added");
        } catch (Exception ex) {
            Console.WriteLine($"Error in students adding {ex.Message}");
            await transaction.RollbackAsync();
        }
    }

    internal async Task UpdateInstructorEmail(int instructorId, string newEmail) {
        using var context = CourseContext.Create();
        var instructor = await context.Instructors.FirstOrDefaultAsync(i => i.Id == instructorId);
        if (instructor == null) {
            Console.WriteLine("Instructor does not exists");
            return;
        }
        int count = await context.Instructors.Select(i => i.Email).Where(email => email == newEmail).CountAsync();
        if (count > 0) {
            Console.WriteLine("Could not set email, because instructor with such email already exists");
            return;
        }
        instructor.Email = newEmail;
        using (var transaction = context.Database.BeginTransaction(System.Data.IsolationLevel.RepeatableRead)) {
            try {
                var existing = await context.Instructors.FirstOrDefaultAsync(i => i.Id == instructorId);
                if (existing?.RowVersion != instructor.RowVersion) {
                    Console.WriteLine("Could not change Email. Record was changed by another user");
                    return;
                }
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                Console.WriteLine("Email is succesfully changed");
            } catch (Exception ex) {
                Console.WriteLine($"Error in changing email {ex.Message}");
            }
        }
    }

    internal async Task SoftCourseDelete(int courseId) {
        using var context = CourseContext.Create();
        var course = await context.Courses.FirstOrDefaultAsync(i => i.Id == courseId);
        if (course == null) {
            Console.WriteLine("Course does not exists");
            return;
        }
        course.IsDeleted = true;
        try {
            await context.SaveChangesAsync();
            Console.WriteLine("Course succesfully deleted");
        } catch (Exception ex) {
            Console.WriteLine($"Error while deleting {ex.Message}");
        }
    }

    public async Task<UpdateInstructorResult> UpdateInstructorEmail_Claude(int instructorId, string newEmail) {
        using var context = CourseContext.Create();

        // Start transaction to cover all operations
        using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);

        try {
            // Get the instructor
            var instructor = await context.Instructors.FindAsync(instructorId);
            if (instructor == null) {
                return new UpdateInstructorResult { Success = false, Message = "Instructor does not exist" };
            }

            // Check if email is already in use
            bool emailExists = await context.Instructors
                .AnyAsync(i => i.Email == newEmail && i.Id != instructorId);

            if (emailExists) {
                return new UpdateInstructorResult { Success = false, Message = "Email already in use by another instructor" };
            }

            // Update email
            instructor.Email = newEmail;

            // Try to save - this will automatically check RowVersion
            await context.SaveChangesAsync();

            // Commit transaction
            await transaction.CommitAsync();

            return new UpdateInstructorResult { Success = true, Message = "Email successfully updated" };
        } catch (DbUpdateConcurrencyException) {
            // This is thrown when EF detects a concurrency conflict
            return new UpdateInstructorResult { Success = false, Message = "Could not update email. Record was modified by another user" };
        } catch (Exception ex) {
            return new UpdateInstructorResult { Success = false, Message = $"Error updating email: {ex.Message}" };
        }
    }

    public class UpdateInstructorResult {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}

class TestingEF {
    Faker<Course> courseFaker = new Faker<Course>()
                .RuleFor(c => c.Name, f => f.Lorem.Sentence(3))
                .RuleFor(c => c.Description, f => f.Lorem.Paragraph())
                .RuleFor(c => c.Price, f => f.Random.Decimal(10, 200));

    Faker<Lesson> lessonFaker = new Faker<Lesson>()
                .RuleFor(c => c.Title, f => f.Lorem.Sentence(3))
                .RuleFor(c => c.Content, f => f.Lorem.Paragraph())
                .RuleFor(c => c.OrderNumber, f => 1);

    Faker<Instructor> instructorFaker = new Faker<Instructor>()
                .RuleFor(c => c.Name, f => $"{f.Person.FirstName} {f.Person.LastName}")
                .RuleFor(c => c.Email, f => f.Internet.Email());

    internal async Task CreateCourses() {
        using var context = CourseContext.Create();
        //var course = new Course() {
        //    Name = "Course 1",
        //    Description = "description 1",
        //    Price = 10.99M,
        //};
        //context.Courses.Add(course);
        //course.Lessons.Add(new Lesson() { Title = "Lesson 1", Content = "Content 1", OrderNumber = 1 });
        //context.Courses.AddRange(courseFaker.Generate(20000));
        foreach(var c in context.Courses.ToList()) {
            c.Lessons = lessonFaker.Generate(20);
        }
        await context.SaveChangesAsync();
    }

    internal async Task CreateInstructors(CourseContext context) {
        var list = instructorFaker.Generate(1000)
            .GroupBy(i => i.Email)
            .Select(g => g.First())
            .ToList();
        var instructorsEmails = context.Instructors.ToList().Select(i => i.Email).ToList();
        list = list.Where(i => !instructorsEmails.Contains(i.Email)).ToList();
        context.Instructors.AddRange(list);
        await context.SaveChangesAsync();
        Console.WriteLine(list.Count);
    }

    internal async Task InsertNewInstructors(int count, CourseContext context) {
        var instructorEmails = new HashSet<string>(await context.Instructors.Select(i => i.Email).ToListAsync());
        int batchSize = 1000;
        using var transaction = await context.Database.BeginTransactionAsync();
        try {
            for (int i = 0; i < count; i += batchSize) {
                int c = Math.Min(batchSize, count - i);
                var list = instructorFaker.Generate(c)
                    .GroupBy(i => i.Email)
                    .Select(g => g.First())
                    .Where(i => !instructorEmails.Contains(i.Email));
                context.Instructors.AddRange(list);
                await context.SaveChangesAsync();
                foreach (var instructor in list) instructorEmails.Add(instructor.Email);
            }
            await transaction.CommitAsync();
        } catch(Exception ex) {
            Console.WriteLine(ex);
            await transaction.RollbackAsync();
        }
    }
}
