namespace Growl.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Func;
    using static Func.Option;
    using static Func.Result;

    public class GameRunner
    {
        private GameState _gameState = new(DateTime.UtcNow, GameStatus.Lobby, Array.Empty<string>());
        private ConcurrentDictionary<Guid, string> _playerNames = new();

        public string GameCode { get; }

        public delegate void GameStateUpdatedEvent(GameState state);
        public event GameStateUpdatedEvent OnGameStateUpdated;

        public GameRunner(string gameCode)
        {
            GameCode = gameCode;
        }

        public bool HasExpired() =>
            _gameState.CreationDate < DateTime.UtcNow.AddDays(-1)
            || _gameState.Status == GameStatus.Finished;

        public bool HasPlayer(Guid sessionId) =>
            _playerNames.ContainsKey(sessionId);

        public IEnumerable<string> GetPlayers() => _playerNames.Values;

        public Result AddPlayer(Guid sessionId, string playerName)
        {
            if (_playerNames.Any(x => x.Value.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return Fail<NameTakenError>();
            }

            _playerNames.AddOrUpdate(sessionId, playerName, (_, _) => playerName);

            if (_playerNames.Count > 10)
            {
                _playerNames.Remove(sessionId, out _);
                return Fail<TooManyPlayersError>();
            }

            _gameState = _gameState with {PlayerNames = _playerNames.Values};

            OnGameStateUpdated?.Invoke(_gameState);

            return Succeed();
        }

        public Result SetPlayerName(Guid sessionId, string playerName)
        {
            if (_playerNames.ContainsKey(sessionId))
                return Fail<PlayerNotFoundError>();

            _playerNames[sessionId] = playerName;

            return Succeed();
        }

        public Option<string> GetPlayerName(Guid sessionId) =>
            _playerNames.TryGetValue(sessionId, out var name)
                ? Some(name)
                : None<string>();
    }

    public class TooManyPlayersError : ResultError { }
    public class PlayerNotFoundError : ResultError { }
    public class NameTakenError : ResultError { }

    public record GameState(
        DateTime CreationDate,
        GameStatus Status,
        IEnumerable<string> PlayerNames);

    public enum GameStatus
    {
        Lobby,
        Running,
        Finished,
    }

}