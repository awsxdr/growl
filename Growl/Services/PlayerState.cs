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
        int Coins = 0);

    public enum Allegiance
    {
        Human,
        Werewolf,
    }

    public interface ICard
    { }

    public class GoldCard : ICard
    { }

    public class BiteCard : ICard
    { }

    public class WoundCard : ICard
    { }

    public class SalveCard : ICard
    { }

    public class CharmCard : ICard
    { }
}