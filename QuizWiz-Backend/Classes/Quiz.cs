namespace QuizWiz_Backend.Classes
{
    public class Quiz
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string Description { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; } = 30;
        public int MaxQuestions { get; set; } = 10;
        public bool IsOfficial { get; set; } = false;
        public List<Question> Questions { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}