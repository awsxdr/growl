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

        private static readonly IReadOnlyDictionary<int, (int GoldCount, int BiteCount)> InfectionDeckCounts =
            new Dictionary<int, (int GoldCount, int BiteCount)>
            {
                [4] = (3, 1),
                [5] = (4, 1),
                [6] = (4, 2),
                [7] = (5, 2),
                [8] = (6, 2),
                [9] = (6, 3),
                [10] = (7, 3),
            };

        public string GameCode { get; }
        public GameStatus Status => _gameState.Status;

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

        public Result StartGame()
        {
            if (_gameState.Status != GameStatus.Lobby)
                return Fail<GameAlreadyStartedError>();
                
            _gameState = _gameState with
            {
                Status = GameStatus.Running,
                CurrentPlayer = new Random().Next(0, _gameState.PlayerNames.Count())
            };

            OnGameStateUpdated?.Invoke(_gameState);

            var (goldCount, biteCount) = InfectionDeckCounts[_gameState.PlayerNames.Count()];
            var infectionDeck = CreateDeck(goldCount: goldCount, biteCount: biteCount);

            return Succeed();
        }

        private IEnumerable<TItem> CreateMultiple<TItem>(int count) where TItem : new() =>
            Enumerable.Repeat(0, count).Select(_ => new TItem());

        private IEnumerable<ICard> CreateDeck(
            int goldCount = 0,
            int biteCount = 0, 
            int charmCount = 0, 
            int woundCount = 0, 
            int salveCount = 0)
            =>
            CreateMultiple<GoldCard>(goldCount).Cast<ICard>()
                .Concat(CreateMultiple<BiteCard>(biteCount))
                .Concat(CreateMultiple<CharmCard>(charmCount))
                .Concat(CreateMultiple<WoundCard>(woundCount))
                .Concat(CreateMultiple<SalveCard>(salveCount))
                .Shuffle();
    }

    public class TooManyPlayersError : ResultError { }
    public class PlayerNotFoundError : ResultError { }
    public class NameTakenError : ResultError { }
    public class GameAlreadyStartedError : ResultError { }
}