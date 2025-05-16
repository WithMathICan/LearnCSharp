using System.ComponentModel.DataAnnotations;

namespace EFPractice01.Models {
    public class Quiz {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        [MinLength(2)]
        public string Title { get; set; } = "";

        [Required]
        [Range(0, 100)]
        public int PassingScore { get; set; }

        public int LessonId { get; set; }
        public Lesson? Lesson { get; set; }

        public List<QuizStudent> StudentResults = [];
    }
}