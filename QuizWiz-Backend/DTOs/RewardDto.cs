namespace QuizWiz_Backend.DTOs
{
    public class GameFinishedDto
    {
        public Dictionary<string, int> Scores { get; set; } = [];
        public List<int> Winners { get; set; } = [];
        public Dictionary<string, PlayerRewardDto> Rewards { get; set; } = [];
    }

    public class PlayerRewardDto
    {
        public int Experience { get; set; }
        public int Points { get; set; }
        public bool IsWinner { get; set; }
        public int CorrectAnswers { get; set; }
        public bool IsLevelUp { get; set; }
    }
}
