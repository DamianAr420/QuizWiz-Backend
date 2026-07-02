using System;
using System.Collections.Generic;

namespace QuizWiz_Backend.Classes
{
    public class GameState
    {
        public Guid LobbyId { get; set; }
        public List<Guid> QuestionIds { get; set; } = [];
        public int CurrentQuestionIndex { get; set; } = 0;
        public DateTime QuestionEndTime { get; set; }
        public Guid CurrentTurnToken { get; set; } = Guid.NewGuid();
        public Dictionary<int, int> PlayerScores { get; set; } = new();
        public Dictionary<int, int> CorrectAnswers { get; set; } = new();
        public HashSet<int> PlayersWhoAnswered { get; set; } = [];
        public bool IsLastQuestion => CurrentQuestionIndex >= QuestionIds.Count - 1;
        public bool IsProcessingTransition { get; set; } = false;
    }
}