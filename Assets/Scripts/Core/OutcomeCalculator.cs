using System;

namespace MagesDementiaGame
{
    public enum SpeechClarity
    {
        Fragmented,
        Partial,
        Clear
    }

    [Serializable]
    public struct ReplayEffects
    {
        public int Noise;
        public SpeechClarity SpeechClarity;
        public int Recognition;
        public int Trust;
        public int Distress;
        public int Agency;
        public int ObjectFamiliarity;
        public bool PhotoRestored;
    }

    public static class OutcomeCalculator
    {
        public static ReplayEffects Baseline => new ReplayEffects
        {
            Noise = 2,
            SpeechClarity = SpeechClarity.Fragmented,
            Recognition = 0,
            Trust = 0,
            Distress = 4,
            Agency = 0,
            ObjectFamiliarity = 0,
            PhotoRestored = false
        };

        public static ReplayEffects Calculate(
            EnvironmentChoice environment,
            ApproachChoice approach,
            ResponseChoice response)
        {
            var effects = Baseline;

            switch (environment)
            {
                case EnvironmentChoice.LowerTelevision:
                    effects.Noise = 1;
                    effects.SpeechClarity = SpeechClarity.Partial;
                    effects.Distress -= 1;
                    break;
                case EnvironmentChoice.TurnOffTelevisionAndRestorePhoto:
                    effects.Noise = 0;
                    effects.SpeechClarity = SpeechClarity.Clear;
                    effects.Distress -= 2;
                    effects.ObjectFamiliarity += 2;
                    effects.PhotoRestored = true;
                    break;
            }

            switch (approach)
            {
                case ApproachChoice.CallFromDistance:
                    effects.SpeechClarity = LowerClarity(effects.SpeechClarity);
                    effects.Distress += 1;
                    break;
                case ApproachChoice.ApproachQuickly:
                    effects.Recognition -= 1;
                    effects.Trust -= 1;
                    effects.Distress += 2;
                    break;
                case ApproachChoice.EnterViewAndIntroduce:
                    effects.Recognition += 2;
                    effects.Trust += 2;
                    effects.Distress -= 1;
                    effects.Agency += 1;
                    break;
            }

            switch (response)
            {
                case ResponseChoice.CorrectMinh:
                    effects.Trust -= 2;
                    effects.Distress += 2;
                    effects.Agency -= 1;
                    break;
                case ResponseChoice.GenericReassurance:
                    effects.Distress -= 1;
                    break;
                case ResponseChoice.AcknowledgeAndHelp:
                    effects.Trust += 2;
                    effects.Distress -= 2;
                    effects.Agency += 2;
                    break;
            }

            effects.Distress = Clamp(effects.Distress, 0, 8);
            effects.Agency = Clamp(effects.Agency, -1, 4);
            return effects;
        }

        private static SpeechClarity LowerClarity(SpeechClarity clarity)
        {
            return clarity == SpeechClarity.Clear ? SpeechClarity.Partial : SpeechClarity.Fragmented;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
    }
}
