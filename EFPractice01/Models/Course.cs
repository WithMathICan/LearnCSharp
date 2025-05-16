using System.ComponentModel.DataAnnotations;

namespace EFPractice01.Models {
    public class Course {

        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Name { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Description { get; set; }

        [Required]
        [Range(0, 10000.00)]
        public decimal Price { get; set; }

        public List<Instructor> Instructors { get; set; } = [];

        public List<Lesson> Lessons { get; set; } = [];

        public List<Student> Students { get; set; } = [];

        public List<Review> Reviews { get; set; } = [];
    }
}
