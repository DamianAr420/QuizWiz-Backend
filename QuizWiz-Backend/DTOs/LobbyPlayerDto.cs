namespace QuizWiz_Backend.DTOs
{
    public record LobbyPlayerDto(
        int UserId,
        string DisplayName,
        bool IsReady,
        string? CloudinaryPublicId,
        string? SelectedFrame,
        string? SelectedBackground,
        int Score,
        int Progress
    );
}