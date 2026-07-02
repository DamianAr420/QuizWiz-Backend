using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using QuizWiz_Backend.Classes;

namespace QuizWiz_Backend.Services
{
    public class GameManager
    {
        private readonly ConcurrentDictionary<Guid, GameState> _games = new();

        public void StartGame(Guid lobbyId, List<Guid> questionIds, List<int> playerIds)
        {
            var state = new GameState
            {
                LobbyId = lobbyId,
                QuestionIds = questionIds,
                CurrentQuestionIndex = 0
            };

            foreach (var playerId in playerIds)
            {
                state.PlayerScores[playerId] = 0;
                state.CorrectAnswers[playerId] = 0;
            }

            _games[lobbyId] = state;
        }

        public GameState? GetGame(Guid lobbyId)
        {
            return _games.TryGetValue(lobbyId, out var state) ? state : null;
        }

        public void RemoveGame(Guid lobbyId)
        {
            _games.TryRemove(lobbyId, out _);
        }
    }
}