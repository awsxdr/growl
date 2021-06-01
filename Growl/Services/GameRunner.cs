namespace Growl.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Func;
    using static Func.Option;
    using static Func.Result;

    public class GameRunner
    {
        private GameState _gameState = new(DateTime.UtcNow, GameStatus.Lobby, Array.Empty<PlayerState>(), Array.Empty<ICard>());
        private readonly object _gameStateLock = new();

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
            _gameState.Players.Any(x => x.SessionId == sessionId);

        public IEnumerable<string> GetPlayerNames() =>
            _gameState.Players.Select(x => x.Name);

        public Result AddPlayer(Guid sessionId, string playerName)
        {
            if (GetPlayerNames().Any(x => x.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)))
                return Fail<NameTakenError>();

            lock (_gameStateLock)
            {
                var newPlayerCollection =
                    _gameState.Players
                        .Where(x => x.SessionId != sessionId)
                        .Append(new PlayerState(sessionId, playerName, Allegiance.Human, Array.Empty<ICard>()))
                        .ToArray();

                if (newPlayerCollection.Length > 10)
                    return Fail<TooManyPlayersError>();

                _gameState = _gameState with
                {
                    Players = newPlayerCollection,
                };
            }

            OnGameStateUpdated?.Invoke(_gameState);

            return Succeed();
        }

        public Result SetPlayerName(Guid sessionId, string playerName)
        {
            var player =
                _gameState.Players
                    .SingleOrDefault(x => x.SessionId == sessionId);

            if(player == default)
                return Fail<PlayerNotFoundError>();

            player = player with {Name = playerName};

            lock (_gameStateLock)
            {
                _gameState = _gameState with
                {
                    Players = _gameState.Players
                        .Where(x => x.SessionId != sessionId)
                        .Append(player),
                };
            }

            return Succeed();
        }

        public Option<string> GetPlayerName(Guid sessionId)
        {
            var player = _gameState.Players.Where(x => x.SessionId == sessionId).ToArray();

            return player.Any()
                ? Some(player.Single().Name)
                : None<string>();
        }

        public Result StartGame()
        {
            lock (_gameStateLock)
            {
                if (_gameState.Status != GameStatus.Lobby)
                    return Fail<GameAlreadyStartedError>();

                var (goldCount, biteCount) = InfectionDeckCounts[_gameState.Players.Count()];
                var infectionDeck = CreateDeck(goldCount: goldCount, biteCount: biteCount);

                var (newPlayerCollection, _) = infectionDeck.Deal(_gameState.Players, 1);

                newPlayerCollection = newPlayerCollection
                    .Select(x =>
                        x.Hand.First() is BiteCard
                            ? x with {Allegiance = Allegiance.Werewolf}
                            : x);

                var remainingDeck = (_gameState.Players.Count() switch
                {
                    var n when n < 6 => CreateDeck(goldCount: 12 - goldCount, biteCount: 12 - biteCount, charmCount: 5, woundCount: 8, salveCount: 5),
                    var n when n < 9 => CreateDeck(goldCount: 12 - goldCount, biteCount: 16 - biteCount, charmCount: 5, woundCount: 12, salveCount: 5),
                    _ => CreateDeck(goldCount: 12 - goldCount, biteCount: 20 - biteCount, charmCount: 5, woundCount: 16, salveCount: 5),
                }).Shuffle();
                
                (newPlayerCollection, remainingDeck) = remainingDeck.Deal(newPlayerCollection, 3);

                // Ensure no-one starts with 3 wound cards
                int playersWith3WoundsCount;
                while ((playersWith3WoundsCount = newPlayerCollection.Count(x => x.Hand.Count(c => c is WoundCard) == 3)) > 0)
                {
                    newPlayerCollection = newPlayerCollection.Select(x =>
                        x.Hand.Count(c => c is WoundCard) == 3 ? x with {Hand = x.Hand.Take(1)} : x);

                    remainingDeck = remainingDeck.Concat(CreateMultiple<WoundCard>(playersWith3WoundsCount * 3)).Shuffle();

                    (newPlayerCollection, remainingDeck) = remainingDeck.DealToMaximum(newPlayerCollection, 3, 4);
                }

                // Convert any humans with 3 bites into werewolves
                newPlayerCollection = newPlayerCollection.Select(x => x.Hand.Count(c => c is BiteCard) >= 3 ? x with {Allegiance = Allegiance.Werewolf} : x);

                _gameState = _gameState with
                {
                    Status = GameStatus.Running,
                    CurrentPlayer = new Random().Next(0, _gameState.Players.Count()),
                    Players = newPlayerCollection,
                    Deck = remainingDeck,
                };
            }

            OnGameStateUpdated?.Invoke(_gameState);

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