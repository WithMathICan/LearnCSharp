
using EFPractice01.Data;
using EFPractice01.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspCourses.Controllers {
    [Route("/api/courses")]
    [ApiController]
    public class CoursesController : ControllerBase {
        private readonly CourseContext _context;

        public CoursesController(CourseContext courseContext) {
            _context = courseContext ?? throw new ArgumentNullException(nameof(courseContext));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses(int page = 1, int pageSize = 10) {
            return await _context.Courses
                .Skip(pageSize * (page - 1))
                .Take(pageSize)
                .AsNoTracking()
                .Include(c => c.Lessons)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Course>> CreateCourse(Course course) {
            _context.Add(course);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCourses), course);
        }
    }
}
