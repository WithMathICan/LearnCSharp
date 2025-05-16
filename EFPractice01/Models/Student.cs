using System.ComponentModel.DataAnnotations;

namespace EFPractice01.Models {
    public class Student {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public DateTime EnrollmentDate { get; set; }

        public List<CourseStudent> Courses { get; set; } = [];

        public List<Review> Reviews { get; set; } = [];
    }
}
