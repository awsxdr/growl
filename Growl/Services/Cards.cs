namespace Growl.Services
{
    using System;
    using Func;

    public interface ICard
    {
        string ImageFileName { get; }
    }

    public delegate void NightCardStateChangedEvent();

    public interface INightCard : ICard
    {
        event NightCardStateChangedEvent OnStateChanged;
    }

    public abstract class NightCardBase : INightCard
    {
        public abstract string ImageFileName { get; }
        public event NightCardStateChangedEvent OnStateChanged;
        protected void StateHasChanged() => OnStateChanged?.Invoke();
    }

    public interface IFinalNightCard : INightCard
    {
    }

    public abstract class FinalNightCardBase : NightCardBase, IFinalNightCard
    {
    }

    public class GoldCard : ICard
    {
        public string ImageFileName => "gold-card.png";
    }

    public class BiteCard : ICard
    {
        public string ImageFileName => "bite-card.png";
    }

    public class WoundCard : ICard
    {
        public string ImageFileName => "wound-card.png";
    }

    public class SalveCard : ICard
    {
        public string ImageFileName => "salve-card.png";
    }

    public class CharmCard : ICard
    {
        public string ImageFileName => "charm-card.png";
    }

    public class BloodHoundNightCard : NightCardBase
    {
        public override string ImageFileName => "night-blood-hound-card.png";

        private Option<Guid> _targetPlayer;
        public Option<Guid> TargetPlayer
        {
            get => _targetPlayer;
            set
            {
                _targetPlayer = value;
                StateHasChanged();
            }
        }
    }

    public class CagedNightCard : NightCardBase
    {
        public override string ImageFileName => "night-caged-card.png";
    }

    public class HypnosisNightCard : NightCardBase
    {
        public override string ImageFileName => "night-hypnosis-card.png";
    }

    public class InsomniaNightCard : NightCardBase
    {
        public override string ImageFileName => "night-insomnia-card.png";
    }

    public class SeanceNightCard : NightCardBase
    {
        public override string ImageFileName => "night-seance-card.png";
    }

    public class SilverBulletNightCard : NightCardBase
    {
        public override string ImageFileName => "night-silver-bullet-card.png";
    }

    public class TheGiftNightCard : NightCardBase
    {
        public override string ImageFileName => "night-the-gift-card.png";
    }

    public class TruthSerumNightCard : NightCardBase
    {
        public override string ImageFileName => "night-truth-serum-card.png";
    }

    public class AllHallowsEveFinalNightCard : FinalNightCardBase
    {
        public override string ImageFileName => "final-night-all-hallows-eve.card.png";
    }

    public class TheAccusedFinalNightCard : FinalNightCardBase
    {
        public override string ImageFileName => "final-night-the-accused-card.png";
    }

    public class ThePurgeFinalNightCard : FinalNightCardBase
    {
        public override string ImageFileName => "final-night-the-purge-card.png";
    }

    public class TheSleepwalkersFinalNightCard : FinalNightCardBase
    {
        public override string ImageFileName => "final-night-the-sleepwalkers-card.png";
    }

    public class TheTempestFinalNightCard : FinalNightCardBase
    {
        public override string ImageFileName => "final-night-the-tempest-card.png";
    }

    public class TheTrustedFinalNightCard : FinalNightCardBase
    {
        public override string ImageFileName => "final-night-the-trusted-card.png";
    }

    public class TheUnsavedFinalNightCard : FinalNightCardBase
    {
        public override string ImageFileName => "final-night-the-unsaved-card.png";
    }

    public class TheUnwantedFinalNightCard : FinalNightCardBase
    {
        public override string ImageFileName => "final-night-the-unwanted-card.png";
    }
}