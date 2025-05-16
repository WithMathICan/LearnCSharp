using System.ComponentModel.DataAnnotations;

namespace EFPractice01.Models {
    public class Review {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string Comment { get; set; }

        [Required]
        public DateTime SubmissionDate { get; set; }

        public int CourseId { get; set; }
        
        public int StudentId { get; set; }
    }
}