namespace QuizWiz_Backend.DTOs
{
    public record CreateLobbyDto(
        string Name,
        int MaxPlayers,
        int QuestionCount,
        bool IsPrivate,
        int QuizId
    );
}