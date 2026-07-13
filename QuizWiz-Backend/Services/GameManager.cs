using System.Collections.Concurrent;
using QuizWiz_Backend.Classes;

namespace QuizWiz_Backend.Services;

public class GameManager
{
    private readonly ConcurrentDictionary<Guid, GameState> _games = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _timers = new();

    public void StartGame(Guid lobbyId, List<Guid> questionIds, List<int> playerIds)
    {
        CancelTimer(lobbyId);

        var state = new GameState
        {
            LobbyId = lobbyId,
            QuestionIds = questionIds,
            CurrentQuestionIndex = 0,
            PlayerCount = playerIds.Count,
            QuestionResolved = false,
            GameEnded = false
        };

        foreach (var playerId in playerIds)
        {
            state.PlayerScores[playerId] = 0;
            state.CorrectAnswers[playerId] = 0;
        }

        _games[lobbyId] = state;
    }

    public GameState? GetGame(Guid lobbyId)
        => _games.GetValueOrDefault(lobbyId);

    public void RemoveGame(Guid lobbyId)
    {
        CancelTimer(lobbyId);
        _games.TryRemove(lobbyId, out _);
    }

    public CancellationToken StartQuestionTimer(Guid lobbyId)
    {
        CancelTimer(lobbyId);

        var cts = new CancellationTokenSource();

        _timers[lobbyId] = cts;

        return cts.Token;
    }

    public void CancelTimer(Guid lobbyId)
    {
        if (_timers.TryRemove(lobbyId, out var timer))
        {
            try
            {
                timer.Cancel();
            }
            finally
            {
                timer.Dispose();
            }
        }
    }
}