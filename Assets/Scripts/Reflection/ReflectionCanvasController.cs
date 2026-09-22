using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MagesDementiaGame
{
    /// <summary>Displays the final educational reflection using scene-authored Unity UI.</summary>
    public sealed class ReflectionCanvasController : MonoBehaviour
    {
        private enum ReflectionStage { DementiaContext, MinhViewConnections, CaregivingChoices }

        private const float StageFadeDuration = 0.45f;

        [SerializeField] private GameObject reflectionRoot;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private Text stageText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text continueButtonText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Text restartButtonText;

        private GameSession session;
        private ReflectionStage stage;
        private bool changingStage;
        private bool reflectionVisible;

        private void Awake()
        {
            session = GameSession.EnsureInstance();
            session.StateChanged += HandleStateChanged;
            continueButton?.onClick.AddListener(Continue);
            restartButton?.onClick.AddListener(Restart);
            if (session.Phase == NarrativePhase.Reflection)
            {
                reflectionVisible = true;
                RenderStage();
            }
            else
            {
                SetVisible(false);
            }
        }

        private void Start() => RefreshVisibility();

        private void OnDestroy()
        {
            if (session != null) session.StateChanged -= HandleStateChanged;
            continueButton?.onClick.RemoveListener(Continue);
            restartButton?.onClick.RemoveListener(Restart);
        }

        private void HandleStateChanged() => RefreshVisibility();

        private void RefreshVisibility()
        {
            var shouldShow = session != null && session.Phase == NarrativePhase.Reflection;
            if (!shouldShow)
            {
                SetVisible(false);
                return;
            }

            if (!reflectionVisible)
            {
                StopAllCoroutines();
                stage = ReflectionStage.DementiaContext;
                changingStage = false;
                if (contentGroup != null) contentGroup.alpha = 1f;
                SetVisible(true);
                RenderStage();
            }
        }

        private void SetVisible(bool visible)
        {
            reflectionVisible = visible;
            if (reflectionRoot != null && reflectionRoot != gameObject) reflectionRoot.SetActive(visible);
            else gameObject.SetActive(visible);
        }

        private void Continue()
        {
            if (changingStage || stage == ReflectionStage.CaregivingChoices) return;
            FindFirstObjectByType<SceneAudioController>()?.PlayUiClick();
            StartCoroutine(FadeToNextStage());
        }

        private void Restart()
        {
            if (changingStage) return;
            FindFirstObjectByType<SceneAudioController>()?.PlayUiClick();
            session.Restart();
        }

        private IEnumerator FadeToNextStage()
        {
            changingStage = true;
            if (contentGroup != null) contentGroup.interactable = false;
            yield return FadeContent(1f, 0f);
            stage++;
            RenderStage();
            yield return FadeContent(0f, 1f);
            if (contentGroup != null) contentGroup.interactable = true;
            changingStage = false;
        }

        private IEnumerator FadeContent(float from, float to)
        {
            if (contentGroup == null) yield break;
            for (var elapsed = 0f; elapsed < StageFadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                contentGroup.alpha = Mathf.Lerp(from, to, elapsed / StageFadeDuration);
                yield return null;
            }
            contentGroup.alpha = to;
        }

        private void RenderStage()
        {
            stageText.text = $"REFLECTION  {(int)stage + 1} / 3";
            switch (stage)
            {
                case ReflectionStage.DementiaContext:
                    titleText.text = "Understanding dementia";
                    bodyText.text =
                        "Dementia describes a group of symptoms caused by conditions that affect the brain. It can change memory, thinking, communication and the ability to manage everyday life. It is not a normal part of ageing.\n\n" +
                        "In Singapore, the Ministry of Health reported that about 74,000 people were living with dementia in 2023. This is projected to rise to about 152,000 by 2030.\n\n" +
                        "<i>Source: Singapore Ministry of Health, 4 November 2025.</i>";
                    continueButtonText.text = "Continue to Minh's experience";
                    break;
                case ReflectionStage.MinhViewConnections:
                    titleText.text = "What Minh was experiencing";
                    bodyText.text = BuildExperiencePage();
                    continueButtonText.text = "Continue to your caregiving choices";
                    break;
                default:
                    titleText.text = "How your choices affected Minh";
                    bodyText.text = BuildChoicesPage();
                    restartButtonText.text = session.MinhLeftWithLan
                        ? "Restart and try different choices"
                        : "Try again with a different approach";
                    break;
            }

            continueButton.gameObject.SetActive(stage != ReflectionStage.CaregivingChoices);
            restartButton.gameObject.SetActive(stage == ReflectionStage.CaregivingChoices);
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        }

        private static string BuildExperiencePage()
        {
            var text = new StringBuilder("Dementia affects each person differently. MinhView translated several possible symptoms into play:\n\n");
            AddSection(text, "Recent memory and orientation", "Difficulty retaining recent events can make the present feel uncertain. Minh forgets what he was doing and wonders why he is at home instead of at the hospital.");
            AddSection(text, "Recognising familiar people", "A familiar person may briefly feel unfamiliar. Lan's blurred face reflects Minh's difficulty connecting the person at the door with someone he knows.");
            AddSection(text, "Past and present becoming confused", "Older memories may feel more immediate than recent ones. Minh's former nursing role feels current, so he believes patients are waiting for him.");
            AddSection(text, "Reduced attention", "Television sound and several visual cues compete for Minh's attention, making it harder to follow Lan and understand what is happening.");
            AddSection(text, "Communication and reassurance", "Minh needs more time, repetition and calm reassurance to express himself. His two attempts to speak show how knowing what you mean does not always make the words easy to produce.");
            return text.ToString();
        }

        private string BuildChoicesPage()
        {
            var text = new StringBuilder("Small changes to the surroundings and to the way care is offered can protect a person's comfort, recognition and ability to participate.\n\n");
            AddSection(text, "Your outcome", BuildOutcomeSummary());
            AddSection(text, "The family photograph", PhaseOneContent.PhotographReflection(session.PhotoRestored) + (session.PhotoRestored
                ? " Familiar personal objects can offer clues about people, place and relationships, helping the surroundings feel safer and more recognisable."
                : " Looking for and returning a meaningful personal object could have provided a reassuring clue about people, place and relationships."));
            AddSection(text, "How Lan approached", PhaseOneContent.ApproachReflection(session) + " Approaching within view, identifying yourself, speaking calmly and allowing time can reduce surprise and support recognition and trust.");
            AddSection(text, "The television", PhaseOneContent.EnvironmentReflection(session.SelectedEnvironmentChoice) + " Reducing competing sound can help a person focus on one voice and use more of their attention to understand the conversation.");
            text.Append("Dementia experiences vary between people and from moment to moment. These actions do not cure dementia, but person-centred support can reduce avoidable distress and make an interaction easier to navigate.");
            return text.ToString();
        }

        private static void AddSection(StringBuilder text, string heading, string content) =>
            text.Append("<b>").Append(heading).Append("</b>\n").Append(content).Append("\n\n");

        private string BuildOutcomeSummary()
        {
            return session.CalculatedResolutionOutcome switch
            {
                ResolutionOutcome.Mixed when session.SelectedEncouragementChoice == EncouragementChoice.ValidateAndWait && session.MinhLeftWithLan =>
                    "Minh initially hesitated, but Lan identified herself, acknowledged his uncertainty, and gave him additional time. That repair helped him feel safe enough to participate and leave with her.",
                ResolutionOutcome.Mixed =>
                    "Minh's hesitation showed that he needed another calm cue. Adding urgency made the situation less clear, so he chose to remain somewhere familiar. Try identifying yourself, validating his concern, and allowing more time.",
                ResolutionOutcome.Bad =>
                    "Minh did not have enough familiar or reassuring information to feel safe leaving. Reducing competing noise, restoring familiar cues, entering his view, identifying yourself, and waiting can make a future invitation easier to understand.",
                _ =>
                    "The environment, familiar photograph, and Lan's approach worked together to support recognition and trust. Minh had enough time and information to accept the invitation and walk with her."
            };
        }
    }
}
