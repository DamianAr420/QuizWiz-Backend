using System.ComponentModel.DataAnnotations;

namespace QuizWiz_Backend.Classes
{
    public class QuizAttempt
    {
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public int QuizId { get; set; }
        public virtual User? User { get; set; }
        public virtual Quiz? Quiz { get; set; }
        [Required]
        public int Score { get; set; }
        [Required]
        public int TotalQuestions { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}