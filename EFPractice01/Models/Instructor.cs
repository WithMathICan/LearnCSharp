using System.ComponentModel.DataAnnotations;

namespace EFPractice01.Models {
    public class Instructor {

        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        public int? MentorId { get; set; }

        public Instructor? Mentor { get; set; }

        public List<Course> Courses = [];

        public List<Instructor> Mentees = [];
    }
}
