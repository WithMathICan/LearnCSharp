using System.ComponentModel.DataAnnotations;

namespace EFPractice01.Models {
    public class Quiz {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [Range(0, 100)]
        public int PassingScore { get; set; }

        public int LessonId { get; set; }

        public List<Student> Students = [];
    }
}