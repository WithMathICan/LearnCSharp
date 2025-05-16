using System.ComponentModel.DataAnnotations;

namespace EFPractice01.Models {
    public class Lesson {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "";

        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = "";

        [Required]
        public int OrderNumber { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public List<Quiz> Quizzes { get; set; } = [];
    }
}


