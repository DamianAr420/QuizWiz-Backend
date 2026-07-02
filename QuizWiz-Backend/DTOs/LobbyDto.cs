namespace QuizWiz_Backend.DTOs
{
    public record LobbyDto(
        Guid Id,
        string Name,
        int HostId,
        int QuizId,
        int MaxPlayers,
        int QuestionCount,
        bool IsPrivate,
        string Status,
        List<LobbyPlayerDto> Players
    );
}