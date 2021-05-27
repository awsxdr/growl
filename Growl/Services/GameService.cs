namespace Growl.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Func;
    using static Func.Option;

    public class GameService
    {
        public const int GameCodeLength = 6;

        private readonly Random _random = new();
        private readonly ConcurrentDictionary<string, GameRunner> _games = new();

        public GameRunner CreateNewGame()
        {
            var gameCode = GenerateGameCode();

            _games[gameCode] = new GameRunner(gameCode);

            CleanExpiredGames();

            return _games[gameCode];
        }

        public Option<GameRunner> GetGame(string gameCode) =>
            _games.TryGetValue(gameCode, out var runner)
                ? Some(runner)
                : None<GameRunner>();

        private void CleanExpiredGames()
        {
            var keysToRemove = _games
                .Where(x => x.Value.HasExpired())
                .Select(x => x.Key).ToArray();

            foreach (var key in keysToRemove)
            {
                _games.Remove(key, out _);
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