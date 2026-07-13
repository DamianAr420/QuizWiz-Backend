using System;
using System.Collections.Generic;

namespace QuizWiz_Backend.Classes
{
    public class GameState
    {
        public Guid LobbyId { get; set; }

        public List<Guid> QuestionIds { get; set; } = [];

        public int CurrentQuestionIndex { get; set; }

        public DateTime QuestionEndTime { get; set; }

        public Guid CurrentTurnToken { get; set; } = Guid.NewGuid();

        public int PlayerCount { get; set; }

        public bool QuestionResolved { get; set; }

        public bool GameEnded { get; set; }

        public Dictionary<int, int> PlayerScores { get; set; } = new();

        public Dictionary<int, int> CorrectAnswers { get; set; } = new();

        public HashSet<int> PlayersWhoAnswered { get; set; } = [];
    }
}