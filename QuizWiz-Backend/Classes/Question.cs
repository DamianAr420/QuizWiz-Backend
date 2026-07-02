using System.ComponentModel.DataAnnotations.Schema;

namespace QuizWiz_Backend.Classes
{
    public class Question
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public List<string> Distractors { get; set; } = [];

        public int QuizId { get; set; }
    }
}