namespace QuizWiz_Backend.DTOs
{
    public record CreateQuestionDto(string Text, string CorrectAnswer, List<string> Distractors);
    public record CreateQuizDto(
        string Title, 
        string Description, 
        int TimeLimitSeconds, 
        int MaxQuestions,
        bool IsOfficial,
        bool IsPlayable,
        List<CreateQuestionDto> Questions
    );
    public record UpdateQuizDto(
    string Title, string Description, int TimeLimitSeconds,
    int MaxQuestions, bool IsVisible, bool IsPlayable, List<CreateQuestionDto> Questions
    );
    public record QuizListDto(
        int Id, string Title, string Description, int QuestionsCount,
        int TimeLimitSeconds, bool IsOfficial, bool IsVisible, bool IsPlayable, string? AuthorId
    );
}