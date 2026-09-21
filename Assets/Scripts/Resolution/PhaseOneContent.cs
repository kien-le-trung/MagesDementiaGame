using System.Collections.Generic;

namespace MagesDementiaGame
{
    public static class PhaseOneContent
    {
        public static readonly string[] BaselineBeats =
        {
            "The photograph that belongs on the table is gone. You are certain it was just here.",
            "The television fills the room. Its voices overlap with your thoughts.",
            "Someone enters. Her face should mean something, but you cannot place it.",
            "She says: \"Minh, the... is ready. Come... now.\"",
            "You try to ask about the photograph. The woman keeps talking about leaving the room.",
            "With no way to make the situation clearer, you stay where you are."
        };

        public static IReadOnlyList<string> BuildReplayBeats(GameSession session)
        {
            var effects = session.ReplayEffects;
            return new[]
            {
                effects.PhotoRestored
                    ? "The family photograph is back on the table. The room feels more like your room."
                    : "The photograph that belongs on the table is still gone. You search the empty space again.",
                effects.Noise switch
                {
                    0 => "The television is silent. There is space to notice the room and the person entering it.",
                    1 => "The television murmurs in the background, but another voice can reach you.",
                    _ => "The television fills the room. Its voices compete for your attention."
                },
                BuildRecognitionBeat(effects),
                BuildLunchBeat(effects.SpeechClarity),
                BuildResponseBeat(session.SelectedResponseChoice),
                BuildResolutionBeat(effects)
            };
        }

        public static string EnvironmentReflection(EnvironmentChoice choice)
        {
            return choice switch
            {
                EnvironmentChoice.LeaveTelevisionOn =>
                    "Leaving the TV on kept competing speech in the room, so Lan's request remained fragmented.",
                EnvironmentChoice.LowerTelevision =>
                    "Lowering the TV reduced competition. Minh could catch more of Lan's request, though some words were still lost.",
                _ =>
                    "Turning off the TV removed competing speech and made Lan's request easier to hear."
            };
        }

        public static string PhotographReflection(bool restored) => restored
            ? "Finding and restoring the photograph gave Minh a familiar anchor in the room."
            : "The missing photograph left Minh without one of the room's familiar anchors.";

        public static string ApproachReflection(ApproachChoice choice)
        {
            return choice switch
            {
                ApproachChoice.CallFromDistance =>
                    "Calling from a distance made the voice harder to locate and reduced the clarity gained from the environment.",
                ApproachChoice.ApproachQuickly =>
                    "Approaching quickly added urgency before Minh could recognize Lan, increasing uncertainty and distress.",
                _ =>
                    "Entering Minh's view and introducing herself gave him time and information to recognize Lan and build trust."
            };
        }

        public static string ApproachReflection(GameSession session)
        {
            if (!session.HasAiApproachEvaluation) return ApproachReflection(session.SelectedApproachChoice);
            return $"Lan said: \"{session.LanApproachText}\"\n\n{session.ApproachJudgeFeedback}";
        }

        public static string ResponseReflection(ResponseChoice choice)
        {
            return choice switch
            {
                ResponseChoice.CorrectMinh =>
                    "Correcting the facts did not address Minh's concern. He had less trust and fewer ways to participate.",
                ResponseChoice.GenericReassurance =>
                    "Reassurance softened the moment, but did not answer the specific concern about the missing photograph.",
                _ =>
                    "Acknowledging the concern and offering help lowered distress and let Minh take part in what happened next."
            };
        }

        public static string ResolutionInspection(GameSession session, ResolutionInspectionTarget target)
        {
            return target switch
            {
                ResolutionInspectionTarget.Television =>
                    (session.SelectedEnvironmentChoice == EnvironmentChoice.NotChosen
                        ? "The television fills the room with competing speech, making it harder for Minh to hold onto one voice."
                        : EnvironmentReflection(session.SelectedEnvironmentChoice)) +
                    "\n\nReducing competing sound can make it easier for a person with dementia to focus on one voice.",
                ResolutionInspectionTarget.Photograph => PhotographReflection(session.PhotoRestored) +
                    "\n\nFamiliar objects can support orientation and provide a reassuring connection to people and place.",
                _ => (session.SelectedApproachChoice == ApproachChoice.NotChosen
                        ? "Moving into Minh's space before he can recognize Lan increases uncertainty and distress."
                        : ApproachReflection(session)) +
                    "\n\nApproaching within view, allowing time, and identifying yourself can reduce surprise and support recognition."
            };
        }

        private static string BuildRecognitionBeat(ReplayEffects effects)
        {
            if (effects.Recognition >= 2)
            {
                return "A woman enters your view and pauses. \"Hi Grandpa, it's Lan.\" Her name helps her face settle into place.";
            }

            if (effects.Recognition < 0)
            {
                return "Someone moves into your space before you can study her face. You pull back.";
            }

            return "A voice calls from across the room. You cannot yet connect it to a familiar face.";
        }

        private static string BuildLunchBeat(SpeechClarity clarity)
        {
            return clarity switch
            {
                SpeechClarity.Clear => "Lan says: \"Lunch is ready. Would you like to come with me?\"",
                SpeechClarity.Partial => "Lan says: \"Lunch is... Would you like to come...?\"",
                _ => "She says: \"Minh, the... is ready. Come... now.\""
            };
        }

        private static string BuildResponseBeat(ResponseChoice choice)
        {
            return choice switch
            {
                ResponseChoice.CorrectMinh =>
                    "You ask about the photograph. Lan says it was never missing and repeats that it is time for lunch.",
                ResponseChoice.GenericReassurance =>
                    "You ask about the photograph. Lan says, \"Don't worry, everything is fine.\" You still do not know where it went.",
                _ =>
                    "You ask about the photograph. Lan says, \"You're worried because it was moved. Let's look at it together.\""
            };
        }

        private static string BuildResolutionBeat(ReplayEffects effects)
        {
            if (effects.Agency >= 2 && effects.Trust >= 2)
            {
                return "Lan gives you time. You choose to hold the photograph, then walk to lunch with her.";
            }

            if (effects.Distress <= 3)
            {
                return "The situation is not completely clear, but you have enough time and information to decide to go with Lan.";
            }

            return "Too much remains uncertain. You stay where you are and try to protect what still feels familiar.";
        }
    }
}
