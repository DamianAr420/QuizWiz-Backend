namespace QuizWiz_Backend.DTOs
{
    public record UserDto(int Id, string DisplayName, string Email, string Role, string? CloudinaryPublicId);
    public record UpdateUserDto(string DisplayName);
}