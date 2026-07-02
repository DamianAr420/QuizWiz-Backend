namespace QuizWiz_Backend.DTOs
{
    public record LobbySummaryDto(
        Guid Id,
        string Name,
        int CurrentPlayers,
        int MaxPlayers,
        int QuestionCount,
        string HostName
    );
}