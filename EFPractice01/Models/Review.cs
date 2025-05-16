using System.ComponentModel.DataAnnotations;

namespace EFPractice01.Models {
    public class Review {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        private string _comment = "";
        [MaxLength(500)]
        public string Comment { 
            get => _comment; 
            set => _comment = value.Trim(); 
        }

        [Required]
        public DateTime SubmissionDate { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }
        public int StudentId { get; set; }
        public Student? Student { get; set; }
    }
}