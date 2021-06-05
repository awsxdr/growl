namespace Growl.Services
{
    using System;
    using System.Collections.Generic;

    public record GameState(
        DateTime CreationDate,
        GameStatus Status,
        IEnumerable<PlayerState> Players,
        IEnumerable<ICard> Deck,
        int CurrentPlayer = 0,
        Guid NightTargetPlayer = default);

    public enum GameStatus
    {
        Lobby,
        Sniff,
        Day,
        Night,
        NightSwap,
        SwapReveal,
        Finished,
    }
}