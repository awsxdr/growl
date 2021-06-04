namespace Growl.Services
{
    public interface ICard
    {
        string ImageFileName { get; }
    }

    public interface INightCard : ICard
    {
    }

    public interface IFinalNightCard : INightCard
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

    public class BloodHoundNightCard : INightCard
    {
        public string ImageFileName => "night-blood-hound-card.png";
    }

    public class CagedNightCard : INightCard
    {
        public string ImageFileName => "night-caged-card.png";
    }

    public class HypnosisNightCard : INightCard
    {
        public string ImageFileName => "night-hypnosis-card.png";
    }

    public class InsomniaNightCard : INightCard
    {
        public string ImageFileName => "night-insomnia-card.png";
    }

    public class SeanceNightCard : INightCard
    {
        public string ImageFileName => "night-seance-card.png";
    }

    public class SilverBulletNightCard : INightCard
    {
        public string ImageFileName => "night-silver-bullet-card.png";
    }

    public class TheGiftNightCard : INightCard
    {
        public string ImageFileName => "night-the-gift-card.png";
    }

    public class TruthSerumNightCard : INightCard
    {
        public string ImageFileName => "night-truth-serum-card.png";
    }

    public class AllHallowsEveFinalNightCard : IFinalNightCard
    {
        public string ImageFileName => "final-night-all-hallows-eve.card.png";
    }

    public class TheAccusedFinalNightCard : IFinalNightCard
    {
        public string ImageFileName => "final-night-the-accused-card.png";
    }

    public class ThePurgeFinalNightCard : IFinalNightCard
    {
        public string ImageFileName => "final-night-the-purge-card.png";
    }

    public class TheSleepwalkersFinalNightCard : IFinalNightCard
    {
        public string ImageFileName => "final-night-the-sleepwalkers-card.png";
    }

    public class TheTempestFinalNightCard : IFinalNightCard
    {
        public string ImageFileName => "final-night-the-tempest-card.png";
    }

    public class TheTrustedFinalNightCard : IFinalNightCard
    {
        public string ImageFileName => "final-night-the-trusted-card.png";
    }

    public class TheUnsavedFinalNightCard : IFinalNightCard
    {
        public string ImageFileName => "final-night-the-unsaved-card.png";
    }

    public class TheUnwantedFinalNightCard : IFinalNightCard
    {
        public string ImageFileName => "final-night-the-unwanted-card.png";
    }
}