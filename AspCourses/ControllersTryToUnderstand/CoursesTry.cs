using EFPractice01.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AspCourses.ControllersTryToUnderstand {
    [Route("try-api/courses")]
    public class CoursesTry : ControllerBase {
        private CourseContext _context;

        public CoursesTry(CourseContext courseContext)  {
            _context = courseContext ?? throw new ArgumentNullException(nameof(courseContext));
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses() {
            try {
                var courses = await _context.Courses.Take(10).ToListAsync();
                var json = JsonSerializer.Serialize(courses);
                return Content(json, "application/json", System.Text.Encoding.UTF8);
            } catch(Exception ex) {
                return StatusCode(500, $"Internal server error {ex.Message}");
            }
        }
    }
}
