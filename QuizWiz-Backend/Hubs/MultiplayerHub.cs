using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;
using QuizWiz_Backend.Services;

namespace QuizWiz_Backend.Hubs
{
    [Authorize]
    public class MultiplayerHub(
        AppDbContext context,
        GameManager gameManager,
        GameFlowService gameFlow) : Hub
    {
        private readonly AppDbContext _context = context;
        private readonly GameManager _gameManager = gameManager;
        private readonly GameFlowService _gameFlow = gameFlow;

        private int CurrentUserId =>
            int.TryParse(Context.UserIdentifier, out var id) ? id : 0;

        public async Task JoinLobbyGroup(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId) || !Guid.TryParse(lobbyId, out var lobbyGuid))
                throw new HubException("Nieprawidłowe ID lobby.");

            var lobby = await _context.Lobbies
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.Id == lobbyGuid);

            if (lobby == null)
                throw new HubException("Lobby nie istnieje.");

            var isMember = lobby.Players.Any(p => p.UserId == CurrentUserId);

            if (lobby.Status != "Waiting" && !isMember)
                throw new HubException("Gra już się rozpoczęła.");

            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId.ToLower());

            if (_gameManager.GetGame(lobbyGuid) is GameState state)
            {
                await SyncClientState(state);
            }

            await Clients.Group(lobbyId.ToLower())
                .SendAsync("PlayerJoined", CurrentUserId);
        }

        public async Task RequestGameState(string lobbyId)
        {
            if (!Guid.TryParse(lobbyId, out var lobbyGuid)) return;

            if (_gameManager.GetGame(lobbyGuid) is GameState state)
            {
                await SyncClientState(state);
            }
        }

        private async Task SyncClientState(GameState state)
        {
            var stringScores = state.PlayerScores.ToDictionary(k => k.Key.ToString(), v => v.Value);

            if (state.CurrentQuestionIndex <= 0)
            {
                await Clients.Caller.SendAsync("ReconnectedToGame", new { IsStarted = false, Scores = stringScores });
                return;
            }

            var currentQuestionId = state.QuestionIds[state.CurrentQuestionIndex - 1];
            var question = await _context.Questions
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == currentQuestionId);

            if (question != null)
            {
                var allAnswers = question.Distractors.Append(question.CorrectAnswer).ToList();
                await Clients.Caller.SendAsync("ReconnectedToGame", new
                {
                    QuestionText = question.Text,
                    Answers = allAnswers,
                    CurrentIndex = state.CurrentQuestionIndex,
                    TotalQuestions = state.QuestionIds.Count,
                    EndTime = state.QuestionEndTime,
                    Scores = stringScores,
                    AnsweredPlayerIds = state.PlayersWhoAnswered.Select(id => id.ToString()).ToList()
                });
            }
        }

        public async Task ToggleReady(string lobbyId, bool isReady)
        {
            if (!Guid.TryParse(lobbyId, out var lobbyGuid)) return;
            var userId = CurrentUserId;
            if (userId == 0) return;

            var player = await _context.LobbyPlayers.FirstOrDefaultAsync(p => p.LobbyId == lobbyGuid && p.UserId == userId);
            if (player != null)
            {
                player.IsReady = isReady;
                await _context.SaveChangesAsync();
                await Clients.Group(lobbyId.ToLower()).SendAsync("UserReadyStatusChanged", userId, isReady);
            }
        }

        public async Task<List<LobbyUserDto>> GetLobbyUsers(string lobbyId)
        {
            var all = await _context.LobbyPlayers.ToListAsync();

            if (!Guid.TryParse(lobbyId, out var lobbyGuid))
            {
                return new List<LobbyUserDto>();
            }

            var users = await _context.LobbyPlayers
                .Where(p => p.LobbyId == lobbyGuid)
                .Join(_context.Users,
                      lp => lp.UserId,
                      u => u.Id,
                      (lp, u) => new LobbyUserDto
                      {
                          Id = u.Id.ToString(),
                          Username = u.DisplayName,
                          CloudinaryPublicId = u.CloudinaryPublicId ?? "",

                          SelectedFrame = u.SelectedFrame,
                          SelectedBackground = u.SelectedBackground
                      })
                .ToListAsync();

            return users;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = CurrentUserId;
            var player = await _context.LobbyPlayers.FirstOrDefaultAsync(p => p.UserId == userId);

            if (player != null)
            {
                var lobbyId = player.LobbyId;
                var lobby = await _context.Lobbies.FindAsync(lobbyId);
                if (lobby != null)
                {
                    if (lobby.Status == "Waiting")
                    {
                        _context.LobbyPlayers.Remove(player);
                        await _context.SaveChangesAsync();
                        await Clients.Group(lobbyId.ToString().ToLower()).SendAsync("PlayerLeft", userId);
                    }
                    else
                    {
                        await Clients.Group(lobbyId.ToString().ToLower()).SendAsync("PlayerConnectionLost", userId);
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task StartGame(string lobbyId)
        {
            await _gameFlow.StartGame(lobbyId, CurrentUserId);
        }

        public async Task SubmitAnswer(string lobbyId, string answer)
        {
            await _gameFlow.SubmitAnswer(lobbyId, CurrentUserId, answer);
        }

        public async Task LeaveLobby(string lobbyId)
        {
            await _gameFlow.LeaveLobby(lobbyId, CurrentUserId, Context.ConnectionId);
        }
    }
}