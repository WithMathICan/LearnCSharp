using System.ComponentModel.DataAnnotations;

namespace EFPractice01.Models {
    public class QuizStudent {
        public int StudentId { get; set; }
        public Student? Student { get; set; }
        public int QuizId { get; set; }
        public Quiz? Quiz { get; set; }
        public DateTime QuizAttemptedAt { get; set; }

        [Range(0, 100)]
        public int Score { get; set; } = 0;
    }
}
