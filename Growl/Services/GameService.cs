namespace Growl.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Func;

    using static Func.Result;

    public class GameService
    {
        public const int GameCodeLength = 6;

        private readonly Random _random = new();
        private readonly Dictionary<string, GameState> _games = new();

        public delegate void GameStateUpdatedEvent(string gameCode, GameState state);
        public event GameStateUpdatedEvent OnGameStateUpdated;

        public string CreateNewGame()
        {
            var gameCode = GenerateGameCode();

            lock (_games)
            {
                _games[gameCode] = new GameState(DateTime.UtcNow, GameStatus.Lobby, new string[0]);
            }

            CleanExpiredGames();

            return gameCode;
        }

        public bool HasPlayer(string gameCode, string playerName)
        {
            lock (_games)
            {
                return _games[gameCode].PlayerNames.Contains(playerName);
            }
        }

        public IEnumerable<string> GetPlayers(string gameCode)
        {
            lock (_games)
            {
                return _games[gameCode].PlayerNames;
            }
        }

        public Result AddPlayer(string gameCode, string playerName)
        {
            lock (_games)
            {
                var newState = _games[gameCode] with
                {
                    PlayerNames = _games[gameCode].PlayerNames.Append(playerName)
                };

                if (newState.PlayerNames.Count() > 10)
                    return Fail<TooManyPlayersError>();

                _games[gameCode] = newState;

                OnGameStateUpdated?.Invoke(gameCode, newState);
            }

            return Succeed();
        }

        public Result ReplacePlayer(string gameCode, string previousPlayerName, string newPlayerName)
        {
            lock (_games)
            {
                if(!_games[gameCode].PlayerNames.Any(x => x.Equals(previousPlayerName, StringComparison.InvariantCultureIgnoreCase)))
                    return Fail<PlayerNotFoundError>();

                var newState = _games[gameCode] with
                {
                    PlayerNames =
                        _games[gameCode].PlayerNames
                            .Where(x => !x.Equals(previousPlayerName, StringComparison.InvariantCultureIgnoreCase))
                            .Append(newPlayerName),
                };

                _games[gameCode] = newState;

                OnGameStateUpdated?.Invoke(gameCode, newState);
            }

            return Succeed();
        }

        private void CleanExpiredGames()
        {
            lock (_games)
            {
                var keysToRemove = _games
                    .Where(x => 
                        x.Value.CreationDate < DateTime.UtcNow.AddDays(-1)
                        || x.Value.Status == GameStatus.Finished )
                    .Select(x => x.Key).ToArray();

                foreach (var key in keysToRemove)
                {
                    _games.Remove(key);
                }
            }
        }

        private string GenerateGameCode() =>
            Enumerable.Range(0, GameCodeLength)
                .Select(_ => _random.Next(26))
                .Select(x => (char)('A' + x))
                .ToArray()
                .Map(x => new string(x));
    }

    public class TooManyPlayersError : ResultError { }
    public class PlayerNotFoundError : ResultError { }

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