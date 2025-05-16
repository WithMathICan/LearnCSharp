using System.ComponentModel.DataAnnotations;

namespace EFPractice01.Models {
    public class Student {
        [Key]
        public int Id { get; set; }

        private string _name = "";
        [Required]
        [MaxLength(100)]
        [MinLength(2)]
        public string Name { 
            get => _name; 
            set => _name = value.Trim(); 
        }

        private string _email = "";
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { 
            get => _email; 
            set => _email = value.Trim(); 
        }

        [Required]
        public DateTime EnrollmentDate { get; set; }

        public List<Course> Courses { get; set; } = [];

        public List<Review> Reviews { get; set; } = [];

        public List<QuizStudent> QuizesResults { get; set; } = [];
    }
}
