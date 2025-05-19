
using EFPractice01.Data;
using EFPractice01.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspCourses.Controllers {
    [Route("/api/courses")]
    [ApiController]
    public class CoursesController {
        private readonly CourseContext _context;

        internal CoursesController(CourseContext courseContext) {
            _context = courseContext;
        }

        [HttpGet]
        internal async Task<ActionResult<IEnumerable<Course>>> GetCourses() {
            return await _context.Courses
                .AsNoTracking()
                .Include(c => c.Lessons)
                .ToListAsync();
        }
    }
}
