using System.Collections;
using UnityEngine;

namespace MagesDementiaGame
{
    /// <summary>
    /// Presents the three-part educational reflection after the resolution scene.
    /// </summary>
    public sealed class PhaseOnePrototypeUI : MonoBehaviour
    {
        private enum ReflectionStage
        {
            DementiaContext,
            MinhViewConnections,
            CaregivingChoices
        }

        private const float ReferenceWidth = 900f;
        private const float ReferenceHeight = 700f;
        private const float StageFadeDuration = 0.45f;

        private GameSession session;
        private ReflectionStage stage;
        private Vector2 scrollPosition;
        private float contentAlpha = 1f;
        private bool changingStage;
        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle cardStyle;
        private GUIStyle buttonStyle;
        private GUIStyle sourceStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PhaseOnePrototypeUI>() == null)
            {
                new GameObject("Reflection UI").AddComponent<PhaseOnePrototypeUI>();
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
            if (session.Phase == NarrativePhase.Reflection)
            {
                StopAllCoroutines();
                stage = ReflectionStage.DementiaContext;
                contentAlpha = 1f;
                changingStage = false;
            }
        }

        private void OnGUI()
        {
            if (session.Phase != NarrativePhase.Reflection)
            {
                return;
            }

            EnsureStyles();

            var scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            DrawBackground(new Rect(0f, 0f, scaledWidth, scaledHeight));

            var previousColor = GUI.color;
            var previousEnabled = GUI.enabled;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, contentAlpha);
            GUI.enabled = !changingStage;

            GUILayout.BeginArea(new Rect(60f, 35f, scaledWidth - 120f, scaledHeight - 70f));
            GUILayout.Label($"REFLECTION  {(int)stage + 1} / 3", headingStyle);
            GUILayout.Space(8f);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            switch (stage)
            {
                case ReflectionStage.DementiaContext:
                    DrawDementiaContext();
                    break;
                case ReflectionStage.MinhViewConnections:
                    DrawMinhViewConnections();
                    break;
                default:
                    DrawCaregivingChoices();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.enabled = previousEnabled;
            GUI.color = previousColor;
        }

        private void DrawDementiaContext()
        {
            GUILayout.Label("Understanding dementia", titleStyle);
            GUILayout.Space(14f);
            GUILayout.Label(
                "Dementia describes a group of symptoms caused by conditions that affect the brain. It can change memory, thinking, communication and the ability to manage everyday life. It is not a normal part of ageing.",
                cardStyle);
            GUILayout.Space(16f);
            GUILayout.Label(
                "In Singapore, the Ministry of Health reported that about 74,000 people were living with dementia in 2023. This is projected to rise to about 152,000 by 2030.",
                cardStyle);
            GUILayout.Space(10f);
            GUILayout.Label("Source: Singapore Ministry of Health, 4 November 2025.", sourceStyle);
            DrawContinueButton("Continue to Minh's experience");
        }

        private void DrawMinhViewConnections()
        {
            GUILayout.Label("What Minh was experiencing", titleStyle);
            GUILayout.Label(
                "Dementia affects each person differently. MinhView translated several possible symptoms into play:",
                bodyStyle);
            GUILayout.Space(14f);

            DrawReflectionCard(
                "Recent memory and orientation",
                "Difficulty retaining recent events can make the present feel uncertain. Minh forgets what he was doing and wonders why he is at home instead of at the hospital.");
            DrawReflectionCard(
                "Recognising familiar people",
                "A familiar person may briefly feel unfamiliar. Lan's blurred face reflects Minh's difficulty connecting the person at the door with someone he knows.");
            DrawReflectionCard(
                "Past and present becoming confused",
                "Older memories may feel more immediate than recent ones. Minh's former nursing role feels current, so he believes patients are waiting for him.");
            DrawReflectionCard(
                "Reduced attention",
                "Television sound and several visual cues compete for Minh's attention, making it harder to follow Lan and understand what is happening.");
            DrawReflectionCard(
                "Communication and reassurance",
                "Minh needs more time, repetition and calm reassurance to express himself. His two attempts to speak show how knowing what you mean does not always make the words easy to produce.");

            DrawContinueButton("Continue to your caregiving choices");
        }

        private void DrawCaregivingChoices()
        {
            GUILayout.Label("How your choices affected Minh", titleStyle);
            GUILayout.Label(
                "Small changes to the surroundings and to the way care is offered can protect a person's comfort, recognition and ability to participate.",
                bodyStyle);
            GUILayout.Space(14f);

            DrawReflectionCard("Your outcome", BuildOutcomeSummary());
            DrawReflectionCard("The family photograph", BuildPhotographLesson());
            DrawReflectionCard("How Lan approached", BuildApproachLesson());
            DrawReflectionCard("The television", BuildTelevisionLesson());

            GUILayout.Space(12f);
            GUILayout.Label(
                "Dementia experiences vary between people and from moment to moment. These actions do not cure dementia, but person-centred support can reduce avoidable distress and make an interaction easier to navigate.",
                cardStyle);

            GUILayout.Space(20f);
            var restartLabel = session.MinhLeftWithLan
                ? "Restart and try different choices"
                : "Try again with a different approach";
            if (GUILayout.Button(restartLabel, buttonStyle, GUILayout.Height(54f)))
            {
                FindFirstObjectByType<SceneAudioController>()?.PlayUiClick();
                session.Restart();
            }
        }

        private string BuildOutcomeSummary()
        {
            switch (session.CalculatedResolutionOutcome)
            {
                case ResolutionOutcome.Mixed when
                    session.SelectedEncouragementChoice == EncouragementChoice.ValidateAndWait &&
                    session.MinhLeftWithLan:
                    return "Minh initially hesitated, but Lan identified herself, acknowledged his uncertainty, and gave him additional time. That repair helped him feel safe enough to participate and leave with her.";
                case ResolutionOutcome.Mixed:
                    return "Minh's hesitation showed that he needed another calm cue. Adding urgency made the situation less clear, so he chose to remain somewhere familiar. Try identifying yourself, validating his concern, and allowing more time.";
                case ResolutionOutcome.Bad:
                    return "Minh did not have enough familiar or reassuring information to feel safe leaving. Reducing competing noise, restoring familiar cues, entering his view, identifying yourself, and waiting can make a future invitation easier to understand.";
                default:
                    return "The environment, familiar photograph, and Lan's approach worked together to support recognition and trust. Minh had enough time and information to accept the invitation and walk with her.";
            }
        }

        private string BuildPhotographLesson()
        {
            return PhaseOneContent.PhotographReflection(session.PhotoRestored) +
                   (session.PhotoRestored
                       ? " Familiar personal objects can offer clues about people, place and relationships, helping the surroundings feel safer and more recognisable."
                       : " Looking for and returning a meaningful personal object could have provided a reassuring clue about people, place and relationships.");
        }

        private string BuildApproachLesson()
        {
            return PhaseOneContent.ApproachReflection(session) +
                   " Approaching within view, identifying yourself, speaking calmly and allowing time can reduce surprise and support recognition and trust.";
        }

        private string BuildTelevisionLesson()
        {
            return PhaseOneContent.EnvironmentReflection(session.SelectedEnvironmentChoice) +
                   " Reducing competing sound can help a person focus on one voice and use more of their attention to understand the conversation.";
        }

        private void DrawContinueButton(string label)
        {
            GUILayout.Space(24f);
            if (GUILayout.Button(label, buttonStyle, GUILayout.Height(54f)))
            {
                FindFirstObjectByType<SceneAudioController>()?.PlayUiClick();
                StartCoroutine(FadeToNextStage());
            }
        }

        private IEnumerator FadeToNextStage()
        {
            if (changingStage || stage == ReflectionStage.CaregivingChoices)
            {
                yield break;
            }

            changingStage = true;
            for (var elapsed = 0f; elapsed < StageFadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                contentAlpha = 1f - Mathf.Clamp01(elapsed / StageFadeDuration);
                yield return null;
            }

            contentAlpha = 0f;
            stage++;
            scrollPosition = Vector2.zero;

            for (var elapsed = 0f; elapsed < StageFadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                contentAlpha = Mathf.Clamp01(elapsed / StageFadeDuration);
                yield return null;
            }

            contentAlpha = 1f;
            changingStage = false;
        }

        private void DrawReflectionCard(string label, string explanation)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label(label, headingStyle);
            GUILayout.Label(explanation, bodyStyle);
            GUILayout.EndVertical();
            GUILayout.Space(12f);
        }

        private void DrawBackground(Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.09f, 0.14f, 0.15f);
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
                fontSize = 40,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.95f, 0.91f, 0.82f) }
            };
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.83f, 0.89f, 0.86f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 21,
                wordWrap = true,
                normal = { textColor = new Color(0.83f, 0.89f, 0.86f) }
            };
            cardStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 21,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(20, 20, 17, 17),
                normal = { textColor = new Color(0.92f, 0.92f, 0.88f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 21,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(16, 16, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
            sourceStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 16,
                fontStyle = FontStyle.Italic
            };
        }
    }
}
