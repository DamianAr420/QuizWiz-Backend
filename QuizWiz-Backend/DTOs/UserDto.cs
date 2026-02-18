namespace QuizWiz_Backend.DTOs
{
    public record UserDto(
        int Id,
        string DisplayName,
        string Email,
        string Role,
        string? CloudinaryPublicId,
        string? SelectedFrame,
        string? SelectedBackground,
        DateTime CreatedAt,
        int Points,
        int Experience,
        int Level
    );
    public record UpdateUserDto(
        string DisplayName,
        string? SelectedFrame,
        string? SelectedBackground
    );
}