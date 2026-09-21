using System;

namespace MagesDementiaGame
{
    public enum SpeechClarity
    {
        Fragmented,
        Partial,
        Clear
    }

    public enum ResolutionOutcome
    {
        Good,
        Mixed,
        Bad
    }

    public enum EncouragementChoice
    {
        NotChosen,
        ValidateAndWait,
        PressureToLeave
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
            ResponseChoice response,
            bool photoRestored)
        {
            var effects = Baseline;

            switch (environment)
            {
                case EnvironmentChoice.LowerTelevision:
                    effects.Noise = 1;
                    effects.SpeechClarity = SpeechClarity.Partial;
                    effects.Distress -= 1;
                    break;
                case EnvironmentChoice.TurnOffTelevision:
                    effects.Noise = 0;
                    effects.SpeechClarity = SpeechClarity.Clear;
                    effects.Distress -= 2;
                    break;
            }

            if (photoRestored)
            {
                effects.ObjectFamiliarity += 2;
                effects.PhotoRestored = true;
                effects.Distress -= 1;
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

        public static ReplayEffects ApplyApproachEvaluation(
            ReplayEffects effects,
            int recognition,
            int trust,
            int distress,
            int clarity)
        {
            effects.Recognition += Clamp(recognition, 0, 2);
            effects.Trust += Clamp(trust, 0, 2);
            effects.Distress += Clamp(distress, 0, 2) switch
            {
                0 => -1,
                1 => 0,
                _ => 2
            };
            effects.Agency += Clamp(clarity, 0, 2) - 1;
            if (clarity == 0) effects.SpeechClarity = LowerClarity(effects.SpeechClarity);
            effects.Distress = Clamp(effects.Distress, 0, 8);
            effects.Agency = Clamp(effects.Agency, -1, 4);
            return effects;
        }

        public static int CalculateCareScore(
            int recognition,
            int trust,
            int distress,
            int clarity,
            EnvironmentChoice environment,
            bool photoRestored)
        {
            var environmentSupport = environment switch
            {
                EnvironmentChoice.LowerTelevision => 1,
                EnvironmentChoice.TurnOffTelevision => 2,
                _ => 0
            };

            return Clamp(recognition, 0, 2) + Clamp(trust, 0, 2) + Clamp(clarity, 0, 2) +
                   (2 - Clamp(distress, 0, 2)) + environmentSupport + (photoRestored ? 1 : 0);
        }

        public static int CalculateCareScore(
            ApproachChoice approach,
            EnvironmentChoice environment,
            bool photoRestored)
        {
            return approach switch
            {
                ApproachChoice.CallFromDistance =>
                    CalculateCareScore(0, 1, 1, 1, environment, photoRestored),
                ApproachChoice.ApproachQuickly =>
                    CalculateCareScore(0, 0, 2, 0, environment, photoRestored),
                ApproachChoice.EnterViewAndIntroduce =>
                    CalculateCareScore(2, 2, 0, 2, environment, photoRestored),
                _ => CalculateCareScore(1, 1, 1, 1, environment, photoRestored)
            };
        }

        public static ResolutionOutcome DetermineResolutionOutcome(int careScore)
        {
            if (careScore >= 5) return ResolutionOutcome.Good;
            return careScore >= 2 ? ResolutionOutcome.Mixed : ResolutionOutcome.Bad;
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
