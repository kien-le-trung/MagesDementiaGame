using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagesDementiaGame
{
    /// <summary>
    /// Intentionally plain Phase 1 presentation: text, buttons, and flat backgrounds.
    /// It can be replaced without changing GameSession or OutcomeCalculator.
    /// </summary>
    public sealed class PhaseOnePrototypeUI : MonoBehaviour
    {
        private const float ReferenceWidth = 900f;
        private const float ReferenceHeight = 700f;

        private GameSession session;
        private Vector2 scrollPosition;
        private int baselineBeat;
        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle cardStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PhaseOnePrototypeUI>() == null)
            {
                new GameObject("Phase 1 Prototype UI").AddComponent<PhaseOnePrototypeUI>();
            }
        }

        private void Awake()
        {
            session = GameSession.EnsureInstance();
            session.StateChanged += HandleStateChanged;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.StateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged()
        {
            scrollPosition = Vector2.zero;
            if (session.Phase == NarrativePhase.Baseline)
            {
                baselineBeat = 0;
            }
        }

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().name == GameSession.CaregiverSceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RecipientSceneController>() != null &&
                session.Phase != NarrativePhase.Reflection)
            {
                return;
            }

            EnsureStyles();

            var scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            DrawBackground(new Rect(0f, 0f, scaledWidth, scaledHeight));
            GUILayout.BeginArea(new Rect(60f, 35f, scaledWidth - 120f, scaledHeight - 70f));
            DrawPhaseLabel();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            switch (session.Phase)
            {
                case NarrativePhase.Baseline:
                    DrawBaseline();
                    break;
                case NarrativePhase.Intervention:
                    DrawIntervention();
                    break;
                case NarrativePhase.Replay:
                    DrawReplay();
                    break;
                case NarrativePhase.Reflection:
                    DrawReflection();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawBaseline()
        {
            GUILayout.Label("A missing photograph", titleStyle);
            GUILayout.Label("You are Minh. Take in the moment as he experiences it.", headingStyle);
            GUILayout.Space(24f);

            for (var index = 0; index <= baselineBeat; index++)
            {
                GUILayout.Label(PhaseOneContent.BaselineBeats[index], cardStyle);
                GUILayout.Space(10f);
            }

            GUILayout.Space(15f);
            if (baselineBeat < PhaseOneContent.BaselineBeats.Length - 1)
            {
                if (GUILayout.Button("Continue", buttonStyle, GUILayout.Height(48f)))
                {
                    baselineBeat++;
                }
            }
            else if (GUILayout.Button("See what happened before", buttonStyle, GUILayout.Height(48f)))
            {
                session.BeginIntervention();
            }
        }

        private void DrawIntervention()
        {
            GUILayout.Label("Earlier, from Lan's perspective", titleStyle);
            GUILayout.Label(
                "You are Lan, Minh's granddaughter. You moved the family photograph while cleaning. Lunch is ready, and you need to invite Minh to come with you.",
                cardStyle);
            GUILayout.Space(20f);

            GUILayout.Label("1. Prepare the room", headingStyle);
            DrawChoiceButton("Leave the television on", EnvironmentChoice.LeaveTelevisionOn);
            DrawChoiceButton("Lower the television volume", EnvironmentChoice.LowerTelevision);
            DrawChoiceButton("Turn off the television and restore the photograph", EnvironmentChoice.TurnOffTelevisionAndRestorePhoto);

            GUILayout.Space(20f);
            GUILayout.Label("2. Approach Minh", headingStyle);
            DrawChoiceButton("Call to him from across the room", ApproachChoice.CallFromDistance);
            DrawChoiceButton("Walk over quickly so lunch is not delayed", ApproachChoice.ApproachQuickly);
            DrawChoiceButton("Enter his view, pause, and introduce yourself", ApproachChoice.EnterViewAndIntroduce);

            GUILayout.Space(20f);
            GUILayout.Label("3. Respond when he asks about the photograph", headingStyle);
            DrawChoiceButton("Correct him: it was only moved while cleaning", ResponseChoice.CorrectMinh);
            DrawChoiceButton("Reassure him: everything is fine", ResponseChoice.GenericReassurance);
            DrawChoiceButton("Acknowledge his concern and offer to look together", ResponseChoice.AcknowledgeAndHelp);

            GUILayout.Space(28f);
            GUI.enabled = session.HasAllChoices;
            if (GUILayout.Button(
                    session.HasAllChoices ? "Replay the encounter as Minh" : "Make all three choices to continue",
                    buttonStyle,
                    GUILayout.Height(52f)))
            {
                session.BeginReplay();
            }
            GUI.enabled = true;
        }

        private void DrawReplay()
        {
            GUILayout.Label("The same encounter, changed", titleStyle);
            GUILayout.Label("You are Minh again. The events are the same; Lan's earlier choices shape how you can experience and respond to them.", headingStyle);
            GUILayout.Space(20f);

            IReadOnlyList<string> beats = PhaseOneContent.BuildReplayBeats(session);
            for (var index = 0; index < beats.Count; index++)
            {
                GUILayout.Label(beats[index], cardStyle);
                GUILayout.Space(10f);
            }

            GUILayout.Space(22f);
            if (GUILayout.Button("Reflect on Lan's choices", buttonStyle, GUILayout.Height(48f)))
            {
                session.BeginReflection();
            }
        }

        private void DrawReflection()
        {
            GUILayout.Label("What changed, and why", titleStyle);
            GUILayout.Label("There is no empathy score. Each action changed a specific part of Minh's experience.", headingStyle);
            GUILayout.Space(20f);

            DrawReflectionCard("Environment", PhaseOneContent.EnvironmentReflection(session.SelectedEnvironmentChoice));
            DrawReflectionCard("Approach", PhaseOneContent.ApproachReflection(session.SelectedApproachChoice));
            DrawReflectionCard("Response", PhaseOneContent.ResponseReflection(session.SelectedResponseChoice));

            GUILayout.Space(20f);
            GUILayout.Label(
                "Dementia experiences vary between people and from moment to moment. These strategies do not fix dementia; they can make an interaction easier to navigate and preserve the person's ability to participate.",
                cardStyle);

            GUILayout.Space(24f);
            if (GUILayout.Button("Restart and try different choices", buttonStyle, GUILayout.Height(50f)))
            {
                session.Restart();
            }
        }

        private void DrawReflectionCard(string label, string explanation)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label(label, headingStyle);
            GUILayout.Label(explanation, bodyStyle);
            GUILayout.EndVertical();
            GUILayout.Space(12f);
        }

        private void DrawChoiceButton(string text, EnvironmentChoice choice)
        {
            if (GUILayout.Button(text, session.SelectedEnvironmentChoice == choice ? selectedButtonStyle : buttonStyle, GUILayout.Height(44f)))
            {
                session.SetEnvironmentChoice(choice);
            }
        }

        private void DrawChoiceButton(string text, ApproachChoice choice)
        {
            if (GUILayout.Button(text, session.SelectedApproachChoice == choice ? selectedButtonStyle : buttonStyle, GUILayout.Height(44f)))
            {
                session.SetApproachChoice(choice);
            }
        }

        private void DrawChoiceButton(string text, ResponseChoice choice)
        {
            if (GUILayout.Button(text, session.SelectedResponseChoice == choice ? selectedButtonStyle : buttonStyle, GUILayout.Height(44f)))
            {
                session.SetResponseChoice(choice);
            }
        }

        private void DrawPhaseLabel()
        {
            var phaseText = session.Phase switch
            {
                NarrativePhase.Baseline => "ACT 1  /  RECIPIENT EXPERIENCE",
                NarrativePhase.Intervention => "ACT 2  /  CAREGIVER DECISIONS",
                NarrativePhase.Replay => "ACT 3  /  RECIPIENT REPLAY",
                _ => "REFLECTION"
            };
            GUILayout.Label(phaseText, bodyStyle);
            GUILayout.Space(8f);
        }

        private void DrawBackground(Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = session.Phase == NarrativePhase.Baseline
                ? new Color(0.11f, 0.12f, 0.16f)
                : new Color(0.09f, 0.14f, 0.15f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.95f, 0.91f, 0.82f) }
            };
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                wordWrap = true,
                normal = { textColor = new Color(0.83f, 0.89f, 0.86f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = new Color(0.83f, 0.89f, 0.86f) }
            };
            cardStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(18, 18, 15, 15),
                normal = { textColor = new Color(0.92f, 0.92f, 0.88f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(16, 16, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
            selectedButtonStyle = new GUIStyle(buttonStyle);
            selectedButtonStyle.normal.textColor = new Color(0.22f, 0.95f, 0.72f);
            selectedButtonStyle.fontStyle = FontStyle.Bold;
        }
    }
}
