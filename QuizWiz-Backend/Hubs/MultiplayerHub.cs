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
        IHubContext<MultiplayerHub> hubContext,
        IServiceProvider serviceProvider,
        RewardService rewardService) : Hub
    {
        private readonly AppDbContext _context = context;
        private readonly GameManager _gameManager = gameManager;
        private readonly IHubContext<MultiplayerHub> _hubContext = hubContext;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly RewardService _rewardService = rewardService;

        private int CurrentUserId =>
            int.TryParse(Context.UserIdentifier, out var id) ? id : 0;

        public async Task JoinLobbyGroup(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId) || !Guid.TryParse(lobbyId, out var lobbyGuid))
                throw new HubException("Nieprawidłowe ID lobby.");

            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId.ToLower());

            if (_gameManager.GetGame(lobbyGuid) is GameState state)
            {
                await SyncClientState(state);
            }

            await _hubContext.Clients.Group(lobbyId.ToLower()).SendAsync("PlayerJoined", CurrentUserId);
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
                    AnsweredPlayerIds = state.PlayersWhoAnswered.Select(id => id.ToString()).ToList() // <-- Tutaj jako stringi
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
                await _hubContext.Clients.Group(lobbyId.ToLower()).SendAsync("UserReadyStatusChanged", userId, isReady);
            }
        }

        public async Task StartGame(string lobbyId)
        {
            if (!Guid.TryParse(lobbyId, out var lobbyGuid)) return;
            var lobby = await _context.Lobbies.FirstOrDefaultAsync(l => l.Id == lobbyGuid);

            if (lobby != null && lobby.HostId == CurrentUserId)
            {
                var totalQuestionsInQuiz = await _context.Questions
                    .CountAsync(q => q.QuizId == lobby.QuizId);

                var questionsToTake = Math.Min(
                    lobby.QuestionCount,
                    totalQuestionsInQuiz
                );

                List<Guid> questionIds = await _context.Questions
                    .Where(q => q.QuizId == lobby.QuizId)
                    .OrderBy(q => Guid.NewGuid())
                    .Take(questionsToTake)
                    .Select(q => q.Id)
                    .ToListAsync();

                if (questionIds.Count == 0) throw new HubException("Quiz nie posiada pytań.");

                List<int> userIds = await _context.LobbyPlayers
                    .Where(p => p.LobbyId == lobbyGuid)
                    .Select(p => p.UserId)
                    .ToListAsync();

                _gameManager.StartGame(lobbyGuid, questionIds, userIds);

                lobby.Status = "InGame";
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group(lobbyId.ToLower()).SendAsync("GameStarted");
                await SendNextQuestionInternal(lobbyGuid);
            }
        }

        public async Task<List<LobbyUserDto>> GetLobbyUsers(string lobbyId)
        {
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
                          CloudinaryPublicId = u.CloudinaryPublicId ?? ""
                      })
                .ToListAsync();

            return users;
        }

        public async Task SubmitAnswer(string lobbyId, int questionIndex, string selectedAnswer)
        {
            if (!Guid.TryParse(lobbyId, out var lobbyGuid)) return;
            var groupName = lobbyGuid.ToString().ToLower();
            var state = _gameManager.GetGame(lobbyGuid);
            var userId = CurrentUserId;

            if (state is null) return;

            var totalPlayersCount = await _context.LobbyPlayers.AsNoTracking().CountAsync(p => p.LobbyId == lobbyGuid);

            Guid currentQuestionId;
            bool allAnswered = false;

            lock (state)
            {
                if (state.CurrentQuestionIndex != questionIndex || state.CurrentQuestionIndex == 0) return;
                if (state.PlayersWhoAnswered.Contains(userId)) return;

                if (!state.PlayerScores.ContainsKey(userId)) state.PlayerScores[userId] = 0;
                if (!state.CorrectAnswers.ContainsKey(userId)) state.CorrectAnswers[userId] = 0;

                state.PlayersWhoAnswered.Add(userId);
                currentQuestionId = state.QuestionIds[state.CurrentQuestionIndex - 1];

                if (state.PlayersWhoAnswered.Count >= totalPlayersCount)
                {
                    allAnswered = true;
                }
            }

            var question = await _context.Questions
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == currentQuestionId);
            if (question == null) return;

            if (question.CorrectAnswer == selectedAnswer)
            {
                var remainingTime = Math.Max(0, (state.QuestionEndTime - DateTime.UtcNow).TotalSeconds);
                const double questionTime = 15.0;
                const int basePoints = 5;
                const int maxTimeBonus = 5;

                var timeBonus = (int)Math.Round((remainingTime / questionTime) * maxTimeBonus);
                var points = basePoints + timeBonus;

                lock (state)
                {
                    if (state.CurrentQuestionIndex == questionIndex)
                    {
                        state.PlayerScores[userId] += points;
                        state.CorrectAnswers[userId]++;
                    }
                }
            }

            await Clients.Group(groupName).SendAsync("PlayerSubmitted", userId);

            if (allAnswered)
            {
                await SendNextQuestionInternal(lobbyGuid);
            }
        }

        private async Task SendNextQuestionInternal(Guid lobbyGuid)
        {
            var state = _gameManager.GetGame(lobbyGuid);
            if (state == null) return;

            var groupName = lobbyGuid.ToString().ToLower();
            var dbQuestionId = Guid.Empty;
            var totalQuestionsCount = 0;
            var capturedIndexForTimer = 0;
            var capturedTurnToken = Guid.Empty;
            bool isGameFinished = false;

            lock (state)
            {
                if (state.CurrentQuestionIndex >= state.QuestionIds.Count)
                {
                    isGameFinished = true;
                }
                else
                {
                    dbQuestionId = state.QuestionIds[state.CurrentQuestionIndex];
                    state.PlayersWhoAnswered.Clear();

                    state.CurrentQuestionIndex++;
                    state.QuestionEndTime = DateTime.UtcNow.AddSeconds(15);
                    state.CurrentTurnToken = Guid.NewGuid();

                    totalQuestionsCount = state.QuestionIds.Count;
                    capturedIndexForTimer = state.CurrentQuestionIndex;
                    capturedTurnToken = state.CurrentTurnToken;
                }
            }

            if (isGameFinished)
            {
                await EndGameInternal(lobbyGuid, state);
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var question = await db.Questions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.Id == dbQuestionId);
                if (question == null) return;

                var allAnswers = question.Distractors.Append(question.CorrectAnswer).OrderBy(_ => Guid.NewGuid()).ToList();

                await _hubContext.Clients.Group(groupName).SendAsync("NewQuestion", new
                {
                    question.Text,
                    Answers = allAnswers,
                    CurrentIndex = capturedIndexForTimer,
                    TotalQuestions = totalQuestionsCount,
                    EndTime = state.QuestionEndTime
                });

                _ = Task.Run(async () =>
                {
                    await Task.Delay(15500);
                    var currentState = _gameManager.GetGame(lobbyGuid);
                    if (currentState != null)
                    {
                        bool triggerNext = false;
                        lock (currentState)
                        {
                            if (currentState.CurrentTurnToken == capturedTurnToken)
                            {
                                triggerNext = true;
                            }
                        }
                        if (triggerNext)
                        {
                            await SendNextQuestionInternal(lobbyGuid);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KRYTYCZNY BŁĄD HUB] SendNextQuestionInternal: {ex.Message}");
            }
        }

        private async Task EndGameInternal(Guid lobbyGuid, GameState state)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var lobby = await db.Lobbies.FindAsync(lobbyGuid);
                if (lobby == null) return;

                var quiz = await db.Quizzes.FindAsync(lobby.QuizId);
                if (quiz == null) return;

                var maxScore = state.PlayerScores.Count > 0 ? state.PlayerScores.Max(x => x.Value) : 0;

                var winners = state.PlayerScores
                    .Where(x => x.Value == maxScore && maxScore > 0)
                    .Select(x => x.Key)
                    .ToHashSet();

                var today = DateTime.UtcNow.Date;

                var users = await db.Users
                    .Where(u => state.CorrectAnswers.Keys.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                var lobbyPlayers = await db.LobbyPlayers
                    .Where(p => p.LobbyId == lobbyGuid)
                    .ToDictionaryAsync(p => p.UserId);

                var completedToday = (await db.QuizAttempts
                    .Where(a => a.QuizId == quiz.Id && a.CompletedAt >= today)
                    .Select(a => a.UserId)
                    .ToListAsync())
                    .ToHashSet();

                var rewardsDict = new Dictionary<string, PlayerRewardDto>();

                foreach (var (userId, correctAnswers) in state.CorrectAnswers)
                {
                    if (!users.TryGetValue(userId, out var user))
                        continue;

                    bool alreadyDoneToday = completedToday.Contains(userId);

                    var rewardResult = _rewardService.CalculateRewards(
                        correctAnswers,
                        state.QuestionIds.Count,
                        quiz.IsOfficial,
                        quiz.IsVerified,
                        alreadyDoneToday,
                        isWinner: winners.Contains(userId));

                    int levelBefore = user.Level;

                    user.Experience += rewardResult.Experience;
                    user.Points += rewardResult.Points;

                    rewardsDict[userId.ToString()] = new PlayerRewardDto
                    {
                        Experience = rewardResult.Experience,
                        Points = rewardResult.Points,
                        IsWinner = winners.Contains(userId),
                        CorrectAnswers = correctAnswers,
                        IsLevelUp = user.Level > levelBefore
                    };

                    if (lobbyPlayers.TryGetValue(userId, out var lobbyPlayer))
                    {
                        lobbyPlayer.Score = state.PlayerScores[userId];
                    }

                    db.QuizAttempts.Add(new QuizAttempt
                    {
                        UserId = userId,
                        QuizId = quiz.Id,
                        Score = correctAnswers,
                        TotalQuestions = state.QuestionIds.Count,
                        CompletedAt = DateTime.UtcNow
                    });
                }

                lobby.Status = "Finished";

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                var stringScores = state.PlayerScores.ToDictionary(k => k.Key.ToString(), v => v.Value);

                var finishDto = new GameFinishedDto
                {
                    Scores = stringScores,
                    Winners = winners.ToList(),
                    Rewards = rewardsDict
                };

                await _hubContext.Clients
                    .Group(lobbyGuid.ToString().ToLower())
                    .SendAsync("GameFinished", finishDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"[KRYTYCZNY BŁĄD ENDGAME] Lobby {lobbyGuid}: {ex.Message}");
                throw;
            }
            finally
            {
                _gameManager.RemoveGame(lobbyGuid);
            }
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
                        await _hubContext.Clients.Group(lobbyId.ToString().ToLower()).SendAsync("PlayerLeft", userId);
                    }
                    else
                    {
                        await _hubContext.Clients.Group(lobbyId.ToString().ToLower()).SendAsync("PlayerConnectionLost", userId);
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task LeaveLobby(string lobbyId)
        {
            if (!Guid.TryParse(lobbyId, out var lobbyGuid)) return;
            var userId = CurrentUserId;

            var lobby = await _context.Lobbies.FirstOrDefaultAsync(l => l.Id == lobbyGuid);
            if (lobby == null) return;

            if (lobby.Status == "InGame")
            {
                await _hubContext.Clients.Group(lobbyId.ToLower()).SendAsync("PlayerConnectionLost", userId);
                return;
            }

            var player = await _context.LobbyPlayers.FirstOrDefaultAsync(p => p.LobbyId == lobbyGuid && p.UserId == userId);

            if (player != null)
            {
                _context.LobbyPlayers.Remove(player);
                await _context.SaveChangesAsync();

                if (_gameManager.GetGame(lobbyGuid) is GameState state)
                {
                    lock (state)
                    {
                        state.PlayerScores.Remove(userId);
                        state.CorrectAnswers.Remove(userId);
                        state.PlayersWhoAnswered.Remove(userId);
                    }
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId.ToLower());
                await _hubContext.Clients.Group(lobbyId.ToLower()).SendAsync("PlayerLeft", userId);

                if (lobby.HostId == userId)
                {
                    var remainingPlayers = await _context.LobbyPlayers
                        .Where(p => p.LobbyId == lobbyGuid)
                        .ToListAsync();

                    if (remainingPlayers.Any())
                    {
                        var newHost = remainingPlayers.First();
                        lobby.HostId = newHost.UserId;
                        await _context.SaveChangesAsync();

                        await _hubContext.Clients.Group(lobbyId.ToLower()).SendAsync("HostChanged", newHost.UserId);
                    }
                    else
                    {
                        _context.Lobbies.Remove(lobby);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}