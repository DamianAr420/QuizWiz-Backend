using System.ComponentModel.DataAnnotations;

namespace QuizWiz_Backend.DTOs
{
    public record RegisterDto(
        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Nazwa musi mieć od 3 do 20 znaków.")]
        [RegularExpression(@"^[a-zA-Z0-9._]+$", ErrorMessage = "Nazwa może zawierać tylko litery, cyfry, kropkę i podkreślnik.")]
        string DisplayName,

        [Required(ErrorMessage = "Email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Niepoprawny format adresu email.")]
        [StringLength(100, ErrorMessage = "Email jest za długi.")]
        string Email,

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]
        string Password
    );

    public record LoginDto(
        [Required(ErrorMessage = "Login jest wymagany.")]
        string Identifier,

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        string Password
    );

    public record AuthResponseDto(UserDto User, string Token);
}