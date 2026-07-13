using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;
using QuizWiz_Backend.Hubs;

namespace QuizWiz_Backend.Services;

public class GameFlowService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MultiplayerHub> _hub;
    private readonly GameManager _gameManager;
    private readonly RewardService _rewardService;

    public GameFlowService(
        IServiceScopeFactory scopeFactory,
        IHubContext<MultiplayerHub> hub,
        GameManager gameManager,
        RewardService rewardService)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _gameManager = gameManager;
        _rewardService = rewardService;
    }

    public async Task StartQuestionTimer(Guid lobbyId, Guid questionId, Guid turnToken)
    {
        var token = _gameManager.StartQuestionTimer(lobbyId);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), token);

            var state = _gameManager.GetGame(lobbyId);
            if (state == null) return;

            lock (state)
            {
                if (state.CurrentTurnToken != turnToken) return;
                if (state.QuestionResolved) return;
                state.QuestionResolved = true;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var question = await db.Questions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == questionId);

            if (question == null) return;

            await _hub.Clients.Group(lobbyId.ToString().ToLower())
                .SendAsync("RevealAnswer", new
                {
                    CorrectAnswer = question.CorrectAnswer
                });

            await Task.Delay(2000, token);
            await SendNextQuestionInternal(lobbyId);
        }
        catch (TaskCanceledException) { }
    }

    public void CancelQuestion(Guid lobbyId)
        => _gameManager.CancelTimer(lobbyId);

    public async Task StartGame(string lobbyId, int currentUserId)
    {
        if (!Guid.TryParse(lobbyId, out var lobbyGuid))
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var lobby = await db.Lobbies.FirstOrDefaultAsync(l => l.Id == lobbyGuid);
        if (lobby == null || lobby.HostId != currentUserId)
            return;

        var totalQuestionsInQuiz = await db.Questions
            .CountAsync(q => q.QuizId == lobby.QuizId);

        var questionsToTake = Math.Min(lobby.QuestionCount, totalQuestionsInQuiz);

        var questionIds = await db.Questions
            .Where(q => q.QuizId == lobby.QuizId)
            .OrderBy(q => Guid.NewGuid())
            .Take(questionsToTake)
            .Select(q => q.Id)
            .ToListAsync();

        if (questionIds.Count == 0)
            throw new HubException("Quiz nie posiada pytań.");

        var userIds = await db.LobbyPlayers
            .Where(p => p.LobbyId == lobbyGuid)
            .Select(p => p.UserId)
            .ToListAsync();

        _gameManager.StartGame(lobbyGuid, questionIds, userIds);

        lobby.Status = "Starting";
        await db.SaveChangesAsync();

        await _hub.Clients.Group(lobbyId.ToLower())
            .SendAsync("LobbyStatusChanged", "Starting");

        for (int i = 3; i >= 1; i--)
        {
            await _hub.Clients.Group(lobbyId.ToLower())
                .SendAsync("GameCountdown", i);

            await Task.Delay(1000);
        }

        lobby.Status = "InGame";
        await db.SaveChangesAsync();

        await _hub.Clients.Group(lobbyId.ToLower())
            .SendAsync("LobbyStatusChanged", "InGame");

        await _hub.Clients.Group(lobbyId.ToLower())
            .SendAsync("GameStarted");

        await SendNextQuestionInternal(lobbyGuid);
    }

    public async Task SendNextQuestionInternal(string lobbyId)
    {
        if (Guid.TryParse(lobbyId, out var lobbyGuid))
            await SendNextQuestionInternal(lobbyGuid);
    }

    private async Task SendNextQuestionInternal(Guid lobbyGuid)
    {
        var state = _gameManager.GetGame(lobbyGuid);
        if (state == null) return;

        var groupName = lobbyGuid.ToString().ToLower();

        Guid questionId = Guid.Empty;
        int total = 0;
        int index = 0;
        bool finished = false;

        lock (state)
        {
            finished = state.CurrentQuestionIndex >= state.QuestionIds.Count;

            if (!finished)
            {
                state.QuestionResolved = false;

                questionId = state.QuestionIds[state.CurrentQuestionIndex];
                state.CurrentQuestionIndex++;

                state.PlayersWhoAnswered.Clear();

                state.QuestionEndTime = DateTime.UtcNow.AddSeconds(15);
                state.CurrentTurnToken = Guid.NewGuid();

                total = state.QuestionIds.Count;
                index = state.CurrentQuestionIndex;
            }
        }

        if (finished)
        {
            await EndGameInternal(lobbyGuid, state);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var question = await db.Questions
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null) return;

        var answers = question.Distractors
            .Append(question.CorrectAnswer)
            .OrderBy(_ => Guid.NewGuid())
            .ToList();

        await _hub.Clients.Group(groupName)
            .SendAsync("NewQuestion", new
            {
                question.Text,
                Answers = answers,
                CurrentIndex = index,
                TotalQuestions = total,
                EndTime = state.QuestionEndTime
            });

        _ = StartQuestionTimer(
            lobbyGuid,
            questionId,
            state.CurrentTurnToken);
    }

    public async Task SubmitAnswer(string lobbyId, int userId, string answer)
    {
        if (!Guid.TryParse(lobbyId, out var lobbyGuid)) return;

        var state = _gameManager.GetGame(lobbyGuid);
        if (state == null) return;

        Guid questionId;
        bool allAnswered = false;
        DateTime endTime;

        lock (state)
        {
            if (state.QuestionResolved) return;
            if (DateTime.UtcNow >= state.QuestionEndTime) return;
            if (state.CurrentQuestionIndex == 0) return;
            if (state.PlayersWhoAnswered.Contains(userId)) return;

            state.PlayersWhoAnswered.Add(userId);

            questionId = state.QuestionIds[state.CurrentQuestionIndex - 1];
            endTime = state.QuestionEndTime;

            state.PlayerScores.TryAdd(userId, 0);
            state.CorrectAnswers.TryAdd(userId, 0);

            if (state.PlayersWhoAnswered.Count >= state.PlayerCount)
            {
                state.QuestionResolved = true;
                allAnswered = true;
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var question = await db.Questions
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null) return;

        if (question.CorrectAnswer == answer)
        {
            var remaining = Math.Max(0, (endTime - DateTime.UtcNow).TotalSeconds);

            const double questionTime = 15.0;
            const int basePoints = 5;
            const int maxBonus = 5;

            var bonus = (int)Math.Round((remaining / questionTime) * maxBonus);
            var points = basePoints + bonus;

            lock (state)
            {
                state.PlayerScores[userId] += points;
                state.CorrectAnswers[userId]++;
            }
        }

        await _hub.Clients.Group(lobbyGuid.ToString().ToLower())
            .SendAsync("PlayerSubmitted", userId);

        if (allAnswered)
        {
            _gameManager.CancelTimer(lobbyGuid);

            await Task.Delay(1000);

            await _hub.Clients.Group(lobbyGuid.ToString().ToLower())
                .SendAsync("RevealAnswer", new
                {
                    CorrectAnswer = question.CorrectAnswer
                });

            await Task.Delay(2000);

            await SendNextQuestionInternal(lobbyGuid);
        }
    }

    public async Task LeaveLobby(string lobbyId, int userId, string connectionId)
    {
        if (!Guid.TryParse(lobbyId, out var lobbyGuid)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var lobby = await db.Lobbies.FirstOrDefaultAsync(l => l.Id == lobbyGuid);
        if (lobby == null) return;

        var player = await db.LobbyPlayers
            .FirstOrDefaultAsync(p => p.LobbyId == lobbyGuid && p.UserId == userId);

        if (player != null)
        {
            db.LobbyPlayers.Remove(player);
            await db.SaveChangesAsync();

            var state = _gameManager.GetGame(lobbyGuid);
            if (state != null)
            {
                lock (state)
                {
                    state.PlayerScores.Remove(userId);
                    state.CorrectAnswers.Remove(userId);
                    state.PlayersWhoAnswered.Remove(userId);
                    if (state.PlayerCount > 0)
                        state.PlayerCount--;
                }
            }

            await _hub.Groups.RemoveFromGroupAsync(connectionId, lobbyId.ToLower());

            await _hub.Clients.Group(lobbyId.ToLower())
                .SendAsync("PlayerLeft", userId);
        }
    }

    private async Task EndGameInternal(Guid lobbyGuid, GameState state)
    {
        lock (state)
        {
            if (state.GameEnded)
                return;

            state.GameEnded = true;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var lobby = await db.Lobbies.FirstOrDefaultAsync(l => l.Id == lobbyGuid);
            if (lobby == null) return;

            var quiz = await db.Quizzes.FindAsync(lobby.QuizId);
            if (quiz == null) return;

            var maxScore = state.PlayerScores.Count > 0
                ? state.PlayerScores.Max(x => x.Value)
                : 0;

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

            var rewards = new Dictionary<string, PlayerRewardDto>();

            foreach (var (userId, correctAnswers) in state.CorrectAnswers)
            {
                if (!users.TryGetValue(userId, out var user))
                    continue;

                bool alreadyCompleted = completedToday.Contains(userId);

                var reward = _rewardService.CalculateRewards(
                    correctAnswers,
                    state.QuestionIds.Count,
                    quiz.IsOfficial,
                    quiz.IsVerified,
                    alreadyCompleted,
                    winners.Contains(userId));

                int levelBefore = user.Level;

                user.Experience += reward.Experience;
                user.Points += reward.Points;

                rewards[userId.ToString()] = new PlayerRewardDto
                {
                    Experience = reward.Experience,
                    Points = reward.Points,
                    CorrectAnswers = correctAnswers,
                    IsWinner = winners.Contains(userId),
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

            await _hub.Clients
                .Group(lobbyGuid.ToString().ToLower())
                .SendAsync("GameFinished", new GameFinishedDto
                {
                    Scores = state.PlayerScores.ToDictionary(x => x.Key.ToString(), x => x.Value),
                    Winners = winners.ToList(),
                    Rewards = rewards
                });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            _gameManager.RemoveGame(lobbyGuid);
        }
    }
}