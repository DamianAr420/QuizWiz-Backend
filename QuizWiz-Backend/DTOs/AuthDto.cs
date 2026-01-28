namespace QuizWiz_Backend.DTOs
{
    public record RegisterDto(string DisplayName, string Email, string Password);
    public record LoginDto(string Identifier, string Password);
    public record AuthResponseDto(object User, string Token);
}