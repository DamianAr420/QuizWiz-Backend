namespace QuizWiz_Backend.Classes
{
    public class Lobby
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int HostId { get; set; }
        public int QuizId { get; set; }
        public int MaxPlayers { get; set; } = 4;
        public int QuestionCount { get; set; }
        public bool IsPrivate { get; set; }
        public string Status { get; set; } = "Waiting";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<LobbyPlayer> Players { get; set; } = [];
    }
}