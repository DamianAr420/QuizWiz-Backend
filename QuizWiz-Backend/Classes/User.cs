using System.ComponentModel.DataAnnotations;

namespace QuizWiz_Backend.Classes
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string DisplayName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
