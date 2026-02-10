namespace QuizWiz_Backend.DTOs
{
    public record UserDto(
        int Id,
        string DisplayName,
        string Email,
        string Role,
        string? CloudinaryPublicId,
        DateTime CreatedAt,
        int Points,
        int Experience,
        int Level
    );
    public record UpdateUserDto(string DisplayName);
}