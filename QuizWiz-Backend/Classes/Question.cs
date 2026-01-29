namespace QuizWiz_Backend.Classes
{
    public class Question
    {
        public int Id { get; set; }
        public required string Text { get; set; }
        public required string CorrectAnswer { get; set; }
        public List<string> Distractors { get; set; } = new();
        public int QuizId { get; set; }
    }
}