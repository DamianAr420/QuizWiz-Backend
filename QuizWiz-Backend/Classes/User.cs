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
        public string? CloudinaryPublicId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Points { get; set; } = 0;
        public int Experience { get; set; } = 0;
        public int Level => (Experience / 1000) + 1;
        public string? SelectedFrame { get; set; }
        public string? SelectedBackground { get; set; }
    }
}
