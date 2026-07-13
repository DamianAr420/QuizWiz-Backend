namespace QuizWiz_Backend.Classes
{
    public class LobbyPlayer
    {
        public int Id { get; set; }
        public Guid LobbyId { get; set; }
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool IsReady { get; set; } = false;
        public int Score { get; set; } = 0;
        public int Progress { get; set; } = 0;
        public string CloudinaryPublicId { get; set; } = string.Empty;
        public string SelectedFrame { get; set; } = string.Empty;
        public string SelectedBackground { get; set; } = string.Empty;
    }
}