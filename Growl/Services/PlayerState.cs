namespace Growl.Services
{
    using System;
    using System.Collections.Generic;

    public record PlayerState(
        Guid SessionId,
        string Name,
        Allegiance Allegiance,
        IEnumerable<ICard> Hand,
        bool IsAlive = true,
        bool IsAlphaWolf = false,
        int Coins = 0,
        IEnumerable<(ICard Card, Guid FromPlayer)> PassedCards = default,
        bool HasSwapped = false,
        bool IsInCage = false);

    public enum Allegiance
    {
        Human,
        Werewolf,
    }
}