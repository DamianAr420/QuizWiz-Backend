namespace QuizWiz_Backend.DTOs
{
    public record CreateQuestionDto(string Text, string CorrectAnswer, List<string> Distractors);
    public record CreateQuizDto(
        string Title, 
        string Description, 
        int TimeLimitSeconds, 
        int MaxQuestions,
        bool IsOfficial,
        List<CreateQuestionDto> Questions
    );
    public record QuizListDto(int Id, string Title, string Description, int QuestionsCount, int TimeLimitSeconds, bool IsOfficial);
}