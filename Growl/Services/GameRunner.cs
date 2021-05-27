namespace Growl.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Func;

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

        public bool HasPlayer(string playerName) =>
            _playerNames.Values.Any(x => x.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));

        public IEnumerable<string> GetPlayers() => _playerNames.Values;

        public Result AddPlayer(Guid sessionId, string playerName)
        {
            _playerNames.AddOrUpdate(sessionId, playerName, (_, _) => playerName);

            if (_playerNames.Count > 10)
            {
                _playerNames.Remove(sessionId, out _);
                return Result.Fail<TooManyPlayersError>();
            }

            _gameState = _gameState with {PlayerNames = _playerNames.Values};

            OnGameStateUpdated?.Invoke(_gameState);

            return Result.Succeed();
        }

        public Result SetPlayerName(Guid sessionId, string playerName)
        {
            if (_playerNames.ContainsKey(sessionId))
                return Result.Fail<PlayerNotFoundError>();

            _playerNames[sessionId] = playerName;

            return Result.Succeed();
        }
    }
}