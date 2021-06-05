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
        private GameState _gameState = new(DateTime.UtcNow, GameStatus.Lobby, Array.Empty<PlayerState>(), Array.Empty<ICard>());
        private readonly object _gameStateLock = new();
        private readonly ConcurrentDictionary<Guid, PlayerService> _playerServices = new();

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

        private static T Construct<T>(Type type) =>
            (T) type.GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object>());

        private static readonly IReadOnlyCollection<INightCard> NightCards =
            typeof(INightCard).Assembly
                .GetImplementationsOf<INightCard>()
                .Where(x => !x.IsAssignableTo(typeof(IFinalNightCard)))
                .Select(Construct<INightCard>)
                .ToArray();

        private static readonly IReadOnlyCollection<IFinalNightCard> FinalNightCards =
            typeof(IFinalNightCard).Assembly
                .GetImplementationsOf<IFinalNightCard>()
                .Select(Construct<IFinalNightCard>)
                .ToArray();

        public string GameCode { get; }
        public GameStatus Status => _gameState.Status;
        public Guid CurrentPlayer => _gameState.Players.ElementAt(_gameState.CurrentPlayer).SessionId;
        public ICard TopDeckCard => _gameState.Deck.First();

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

        public IEnumerable<PlayerState> GetPlayers() =>
            _gameState.Players;

        public Result AddPlayer(Guid sessionId, string playerName)
        {
            if (GetPlayers().Any(x => x.Name.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)))
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

        public void SetStatus(GameStatus status)
        {
            lock (_gameStateLock)
            {
                _gameState = _gameState with
                {
                    Status = status
                };
            }

            OnGameStateUpdated?.Invoke(_gameState);
        }

        public Option<PlayerState> GetPlayer(Guid sessionId)
        {
            var player = _gameState.Players.Where(x => x.SessionId == sessionId).ToArray();

            return player.Any()
                ? Some(player.Single())
                : None<PlayerState>();
        }

        public PlayerService GetPlayerService(Guid sessionId) =>
            GetPlayer(sessionId) is Some
                ? _playerServices.GetOrAdd(sessionId, CreatePlayerService)
                : throw new Exception("Player does not exist in game");

        private PlayerService CreatePlayerService(Guid sessionId) =>
            new(this, sessionId);

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
                            ? x with {Allegiance = Allegiance.Werewolf, IsAlphaWolf = true}
                            : x);

                var remainingDeck = (_gameState.Players.Count() switch
                {
                    var n when n < 6 => CreateDeck(goldCount: 12 - goldCount, biteCount: 12 - biteCount, charmCount: 5, woundCount: 8, salveCount: 5),
                    var n when n < 9 => CreateDeck(goldCount: 12 - goldCount, biteCount: 16 - biteCount, charmCount: 5, woundCount: 12, salveCount: 5),
                    _ => CreateDeck(goldCount: 12 - goldCount, biteCount: 20 - biteCount, charmCount: 5, woundCount: 16, salveCount: 5),
                }).Shuffle();
                
                (newPlayerCollection, remainingDeck) = remainingDeck.Deal(newPlayerCollection, 3);
                newPlayerCollection = newPlayerCollection.ToArray();

                // Ensure no-one starts with 3 wound cards
                int playersWith3WoundsCount;
                while ((playersWith3WoundsCount = newPlayerCollection.Count(x => x.Hand.Count(c => c is WoundCard) == 3)) > 0)
                {
                    newPlayerCollection = newPlayerCollection
                        .Select(x => x.Hand.Count(c => c is WoundCard) == 3 ? x with {Hand = x.Hand.Take(1)} : x)
                        .ToArray();

                    remainingDeck = remainingDeck.Concat(CreateMultiple<WoundCard>(playersWith3WoundsCount * 3)).Shuffle();

                    (newPlayerCollection, remainingDeck) = remainingDeck.DealToMaximum(newPlayerCollection, 3, 4);
                }

                remainingDeck = remainingDeck.ToArray();

                var random = new Random();
                var firstNightCardIndex = random.Next(-3, 4) + remainingDeck.Count() / 3;
                var secondNightCardIndex = random.Next(-3, 4) + (remainingDeck.Count() / 3) * 2;

                var selectedNightCards = NightCards.Shuffle().Take(2).ToArray();
                
                remainingDeck =
                    remainingDeck.Take(firstNightCardIndex)
                        .Enqueue(selectedNightCards[0])
                        .Enqueue(remainingDeck.TakeBetween(firstNightCardIndex, secondNightCardIndex))
                        .Enqueue(selectedNightCards[1])
                        .Enqueue(remainingDeck.Skip(secondNightCardIndex))
                        .Enqueue(FinalNightCards.Shuffle().First());

                // Convert any humans with 3 bites into werewolves
                newPlayerCollection = newPlayerCollection
                    .Select(x => x.Hand.Count(c => c is BiteCard) >= 3 
                        ? x with 
                        {
                            Allegiance = Allegiance.Werewolf,
                            IsAlphaWolf = true,
                        }
                        : x)
                    .ToArray();

                _gameState = _gameState with
                {
                    Status = GameStatus.Sniff,
                    CurrentPlayer = 0,
                    Players = newPlayerCollection.Shuffle().ToArray(),
                    Deck = remainingDeck.Prepend(new BloodHoundNightCard()).Prepend(new GoldCard()),
                };
            }

            OnGameStateUpdated?.Invoke(_gameState);

            return Succeed();
        }

        private static IEnumerable<TItem> CreateMultiple<TItem>(int count) where TItem : new() =>
            Enumerable.Repeat(0, count).Select(_ => new TItem());

        private static IEnumerable<ICard> CreateDeck(
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

        public void DiscardTopCard()
        {
            lock (_gameStateLock)
            {
                _gameState = _gameState with
                {
                    Deck = _gameState.Deck.Dequeue(out _),
                };
            }

            OnGameStateUpdated?.Invoke(_gameState);
        }

        public void GiveTopCardToPlayer(PlayerState player)
        {
            var deck = _gameState.Deck.Dequeue(out var card).ToArray();

            var newHand = player.Hand.Append(card).ToArray();

            var playerIsAlive = 
                player.IsAlive 
                && newHand.OfType<WoundCard>().Count() - newHand.OfType<SalveCard>().Count() < 3;

            var playerIsWerewolf =
                player.Allegiance == Allegiance.Werewolf
                || newHand.OfType<BiteCard>().Count() - newHand.OfType<CharmCard>().Count() >= 3;

            var players = ModifyPlayer(player.SessionId, p => p with
            {
                Hand = newHand,
                Allegiance = playerIsWerewolf ? Allegiance.Werewolf : Allegiance.Human,
                IsAlive = playerIsAlive,
            }).ToArray();

            if (players.Count(p => p.IsAlive) <= 2)
            {
                throw new Exception("Game over");
                //TODO: Handle game over condition
            }

            var nextPlayerIndex =
                players
                    .Select((p, i) => (Index: i, Player: p))
                    .ToArray()
                    .Map(x => x
                        .Skip(_gameState.CurrentPlayer + 1)
                        .Concat(x.Take(_gameState.CurrentPlayer + 1)))
                    .First(x => x.Player.IsAlive)
                    .Index;

            var status =
                deck[0] is INightCard
                    ? GameStatus.Night
                    : GameStatus.Day;

            lock (_gameStateLock)
            {
                _gameState = _gameState with
                {
                    Deck = deck,
                    CurrentPlayer = nextPlayerIndex,
                    Players = players,
                    Status = status,
                };
            }

            OnGameStateUpdated?.Invoke(_gameState);
        }

        public void PassCard(ICard card, Guid fromPlayer, Guid toPlayer)
        {
            lock (_gameStateLock)
            {
                var players = ModifyPlayer(toPlayer, p => p with
                {
                    PassedCards = (p.PassedCards ?? Array.Empty<(ICard, Guid)>()).Append((card, fromPlayer)),
                });

                players = ModifyPlayer(players, fromPlayer, p => p with
                {
                    Hand = p.Hand
                        .TakeWhile(c => c.GetType() != card.GetType())
                        .Concat(p.Hand
                            .SkipWhile(c => c.GetType() != card.GetType())
                            .Skip(1))
                        .ToArray(),
                });

                _gameState = _gameState with
                {
                    Players = players.ToArray(),
                };
            }

            OnGameStateUpdated?.Invoke(_gameState);
        }

        public void SetHasPassed(Guid playerId)
        {
            lock (_gameStateLock)
            {
                _gameState = _gameState with
                {
                    Players = ModifyPlayer(playerId, p => p with {HasSwapped = true}),
                };
            }
        }

        public void CompleteCardPass()
        {
            lock (_gameStateLock)
            {
                var players = (
                    from player in _gameState.Players
                    let newHand = player.Hand.Concat(player.PassedCards.Select(c => c.Card).Shuffle()).ToArray()
                    let playerIsAlive = player.IsAlive && newHand.OfType<WoundCard>().Count() - newHand.OfType<SalveCard>().Count() < 3
                    let playerIsWerewolf = player.Allegiance == Allegiance.Werewolf || newHand.OfType<BiteCard>().Count() - newHand.OfType<CharmCard>().Count() >= 3
                    select player with
                    {
                        Hand = newHand,
                        PassedCards = Array.Empty<(ICard, Guid)>(),
                        HasSwapped = false,
                        Allegiance = playerIsWerewolf ? Allegiance.Werewolf : Allegiance.Human,
                        IsAlive = playerIsAlive,
                    }).ToArray();

                var nextPlayerIndex =
                    players
                        .Select((p, i) => (Index: i, Player: p))
                        .ToArray()
                        .Map(x => x
                            .Skip(_gameState.CurrentPlayer + 1)
                            .Concat(x.Take(_gameState.CurrentPlayer + 1)))
                        .First(x => x.Player.IsAlive)
                        .Index;

                _gameState = _gameState with
                {
                    Players = players,
                    CurrentPlayer = nextPlayerIndex,
                };
            }

            OnGameStateUpdated?.Invoke(_gameState);
        }

        public PlayerState GetNextLivingPlayer(Guid fromPlayer) =>
            _gameState.Players.SkipWhile(p => p.SessionId != fromPlayer)
                .Skip(1)
                .Concat(_gameState.Players.TakeWhile(p => p.SessionId != fromPlayer).Take(1))
                .First(p => p.IsAlive);

        public PlayerState GetPreviousLivingPlayer(Guid fromPlayer) =>
            _gameState.Players.TakeWhile(p => p.SessionId != fromPlayer).Reverse()
                .Concat(_gameState.Players.SkipWhile(p => p.SessionId != fromPlayer).Reverse())
                .First(p => p.IsAlive);

        private IEnumerable<PlayerState> ModifyPlayer(Guid sessionId, Func<PlayerState, PlayerState> modifier) =>
            ModifyPlayer(_gameState.Players, sessionId, modifier);

        private static IEnumerable<PlayerState> ModifyPlayer(IEnumerable<PlayerState> players, Guid sessionId, Func<PlayerState, PlayerState> modifier) =>
            players.Select(player =>
                    player.SessionId == sessionId
                        ? modifier(player)
                        : player)
                .ToArray();
    }

    public class TooManyPlayersError : ResultError { }
    public class PlayerNotFoundError : ResultError { }
    public class NameTakenError : ResultError { }
    public class GameAlreadyStartedError : ResultError { }
}