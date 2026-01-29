namespace QuizWiz_Backend.DTOs
{
    public record RegisterDto(string DisplayName, string Email, string Password);
    public record LoginDto(string Identifier, string Password);
    public record UserDto(int Id, string DisplayName, string Email, string Role);
    public record AuthResponseDto(UserDto User, string Token);
}