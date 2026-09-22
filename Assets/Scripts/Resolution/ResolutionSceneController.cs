using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MagesDementiaGame
{
    public enum ResolutionFlowState { InspectConsequences, InviteMinh, EscortMinh, Leaving, Complete }

    public sealed class ResolutionSceneController : MonoBehaviour, IInteractionHost
    {
        private const int MaxPanelCharacters = 180;
        [SerializeField] private RoomView roomView;

        [Header("Scene-authored UI")]
        [SerializeField] private Canvas resolutionCanvas;
        [SerializeField] private Text objectiveText;
        [SerializeField] private GameObject interactionPromptPanel;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private GameObject reflectionPanel;
        [SerializeField] private Text reflectionText;
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject encouragementPanel;
        [SerializeField] private Text encouragementText;
        [SerializeField] private Button validateAndWaitButton;
        [SerializeField] private Button pressureToLeaveButton;

        private readonly bool[] inspected = new bool[3];
        private GameSession session;
        private SceneAudioController audioController;
        private string interactionPrompt;
        private string panelText;
        private Action panelClosedAction;
        private readonly Queue<string> pendingPanelPages = new Queue<string>();
        private bool panelOpen;
        private bool encouragementOpen;
        private bool outcomeCommitted;
        private bool initialized;

        public ResolutionFlowState FlowState { get; private set; }
        public bool IsPhotographVisible { get; private set; }
        public bool IsInteractionBlocked => panelOpen || encouragementOpen || FlowState == ResolutionFlowState.Leaving ||
                                            FlowState == ResolutionFlowState.Complete;

        public static void BuildForCurrentScene()
        {
            var existing = FindFirstObjectByType<ResolutionSceneController>();
            if (existing != null) existing.Initialize();
            else Debug.LogError("ResolutionScene requires an authored ResolutionSceneController and SharedRoom prefab.");
        }

        private void Awake()
        {
            session = GameSession.EnsureInstance();
            audioController = GetComponent<SceneAudioController>();
            continueButton?.onClick.AddListener(ClosePanel);
            validateAndWaitButton?.onClick.AddListener(ChooseValidation);
            pressureToLeaveButton?.onClick.AddListener(ChoosePressure);
        }

        private void OnDestroy()
        {
            continueButton?.onClick.RemoveListener(ClosePanel);
            validateAndWaitButton?.onClick.RemoveListener(ChooseValidation);
            pressureToLeaveButton?.onClick.RemoveListener(ChoosePressure);
        }
        private void Start() => Initialize();

        private void Update()
        {
            if (!initialized) return;
            if (resolutionCanvas != null)
                resolutionCanvas.gameObject.SetActive(session.Phase != NarrativePhase.Reflection);
            if (panelOpen && Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.escapeKey.wasPressedThisFrame)) ClosePanel();
            if (encouragementOpen && Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
                    ChooseValidation();
                else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
                    ChoosePressure();
            }
            if (FlowState == ResolutionFlowState.EscortMinh) CheckExitProgress();
        }

        public void SetInteractionPrompt(string prompt)
        {
            interactionPrompt = prompt;
            RefreshUi();
        }

        public void Inspect(ResolutionInspectionTarget target)
        {
            if (FlowState != ResolutionFlowState.InspectConsequences || panelOpen) return;
            var index = (int)target;
            if (inspected[index]) return;
            audioController?.PlayUiClick();
            inspected[index] = true;
            roomView.SetResolutionInspectionAvailable(target, false);
            ShowPanel(PhaseOneContent.ResolutionInspection(session, target));
            audioController?.PlayConfirm();
            if (CompletedInspectionCount == inspected.Length) FlowState = ResolutionFlowState.InviteMinh;
            RefreshUi();
        }

        public void InviteMinh()
        {
            if (FlowState != ResolutionFlowState.InviteMinh || panelOpen || encouragementOpen || outcomeCommitted) return;
            audioController?.PlayUiClick();
            outcomeCommitted = true;
            switch (session.CalculatedResolutionOutcome)
            {
                case ResolutionOutcome.Mixed:
                    ShowPanel(
                        "Lan invites Minh to walk outside with her. Minh looks uncertain and remains seated. He needs another cue before he can decide.",
                        OpenEncouragementChoices);
                    break;
                case ResolutionOutcome.Bad:
                    ShowPanel(
                        "Lan invites Minh to leave, but he does not feel safe or certain enough to go. He remains on the sofa.",
                        FinishWithoutEscort);
                    break;
                default:
                    ShowPanel(
                        "Lan invites Minh to walk outside. The familiar cues and her approach give him enough confidence to agree.\n\nWalk toward the doorway slowly enough for him to follow.",
                        BeginEscort);
                    break;
            }
        }

        private void OpenEncouragementChoices()
        {
            encouragementOpen = true;
            if (encouragementText != null)
                encouragementText.text = "Minh hesitates. How will Lan encourage him?";
            ApplyMovementLock();
            RefreshUi();
        }

        private void ChooseValidation()
        {
            if (!encouragementOpen) return;
            encouragementOpen = false;
            session.SetEncouragementChoice(EncouragementChoice.ValidateAndWait);
            audioController?.PlayConfirm();
            ShowPanel(
                "Lan identifies herself, reassures Minh, and gives him time. The extra cue helps him feel safe enough to stand and go with her.\n\nWalk toward the doorway slowly enough for him to follow.",
                BeginEscort);
        }

        private void ChoosePressure()
        {
            if (!encouragementOpen) return;
            encouragementOpen = false;
            session.SetEncouragementChoice(EncouragementChoice.PressureToLeave);
            audioController?.PlayUiClick();
            ShowPanel(
                "The urgency adds pressure before Minh understands what is happening. He withdraws and remains seated.",
                FinishWithoutEscort);
        }

        private int CompletedInspectionCount
        {
            get { var count = 0; foreach (var value in inspected) if (value) count++; return count; }
        }

        private void Initialize()
        {
            if (initialized) return;
            if (roomView == null)
            {
                Debug.LogError("ResolutionSceneController is missing its authored RoomView reference.", this);
                return;
            }
            if (resolutionCanvas == null || objectiveText == null || interactionPromptPanel == null ||
                interactionPromptText == null || reflectionPanel == null || reflectionText == null ||
                continueButton == null || encouragementPanel == null || encouragementText == null ||
                validateAndWaitButton == null || pressureToLeaveButton == null)
            {
                Debug.LogError(
                    "ResolutionSceneController is missing its scene-authored Canvas references. " +
                    "Use MAGES > Rebuild Resolution Canvas to repair the scene.", this);
                return;
            }
            if (!roomView.Configure(RoomPerspective.Resolution, this)) return;
            initialized = true;
            audioController?.PlayReflectionMusic();
            FlowState = ResolutionFlowState.InspectConsequences;
            roomView.InitializeResolutionInteractions(this);
            ApplyResolutionEnvironment();
            ApplyMovementLock();
            RefreshUi();
        }

        private void ApplyResolutionEnvironment()
        {
            var state = session.SelectedEnvironmentChoice switch
            {
                EnvironmentChoice.LowerTelevision => TelevisionState.Lowered,
                EnvironmentChoice.TurnOffTelevision => TelevisionState.Off,
                _ => TelevisionState.On
            };
            roomView.SetTelevisionState(state);
            audioController?.SetTelevisionAudio(state, false);
            // Direct Play Mode entry has no completed LanView session, so use the
            // restored photograph as ResolutionScene's authored default.
            IsPhotographVisible = session.Phase != NarrativePhase.Replay || session.PhotoRestored;
            roomView.SetPhotoVisible(IsPhotographVisible);
        }

        private void BeginEscort()
        {
            audioController?.PlayConfirm();
            FlowState = ResolutionFlowState.EscortMinh;
            roomView.BeginResolutionEscort();
            ApplyMovementLock();
            RefreshUi();
        }

        private void CheckExitProgress()
        {
            if (!roomView.IsLanAtResolutionExit || !roomView.IsMinhAtResolutionExit) return;
            FlowState = ResolutionFlowState.Leaving;
            roomView.PlayerController.SetMovementEnabled(false);
            roomView.StopResolutionEscort();
            audioController?.PlayEffect(audioController.Library?.DoorOpening);
            FlowState = ResolutionFlowState.Complete;
            session.RecordResolutionResult(true);
            session.BeginReflection();
        }

        private void FinishWithoutEscort()
        {
            FlowState = ResolutionFlowState.Complete;
            roomView.PlayerController.SetMovementEnabled(false);
            roomView.StopResolutionEscort();
            session.RecordResolutionResult(false);
            RefreshUi();
            session.BeginReflection();
        }

        private void ShowPanel(string text, Action onClose = null)
        {
            pendingPanelPages.Clear();
            foreach (var page in Paginate(text)) pendingPanelPages.Enqueue(page);
            panelClosedAction = onClose;
            panelOpen = true;
            encouragementOpen = false;
            interactionPrompt = null;
            ShowNextPanelPage();
            ApplyMovementLock();
            RefreshUi();
        }

        private void ClosePanel()
        {
            if (!panelOpen) return;
            audioController?.PlayUiClick();
            if (pendingPanelPages.Count > 0)
            {
                ShowNextPanelPage();
                return;
            }
            panelOpen = false;
            var action = panelClosedAction;
            panelClosedAction = null;
            action?.Invoke();
            ApplyMovementLock();
            RefreshUi();
        }

        private void ShowNextPanelPage()
        {
            panelText = pendingPanelPages.Count > 0 ? pendingPanelPages.Dequeue() : string.Empty;
            if (reflectionText != null) reflectionText.text = panelText;
            audioController?.PlayDialogueBlip();
            RefreshUi();
        }

        private static IEnumerable<string> Paginate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield return string.Empty;
                yield break;
            }

            var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var paragraphValue in paragraphs)
            {
                var paragraph = paragraphValue.Trim();
                while (paragraph.Length > MaxPanelCharacters)
                {
                    var split = paragraph.LastIndexOf(' ', MaxPanelCharacters);
                    if (split < MaxPanelCharacters / 2) split = MaxPanelCharacters;
                    yield return paragraph.Substring(0, split).Trim();
                    paragraph = paragraph.Substring(split).Trim();
                }
                if (paragraph.Length > 0) yield return paragraph;
            }
        }

        private void ApplyMovementLock() => roomView?.PlayerController?.SetMovementEnabled(initialized && !IsInteractionBlocked);

        private void RefreshUi()
        {
            if (objectiveText != null) objectiveText.text = $"ACT 3 / RESOLUTION\n{ObjectiveText()}";
            if (interactionPromptText != null) interactionPromptText.text = interactionPrompt ?? string.Empty;
            interactionPromptPanel?.SetActive(!panelOpen && !string.IsNullOrWhiteSpace(interactionPrompt));
            if (reflectionText != null) reflectionText.text = panelText ?? string.Empty;
            reflectionPanel?.SetActive(panelOpen);
            encouragementPanel?.SetActive(encouragementOpen);
        }

        private string ObjectiveText() => FlowState switch
        {
            ResolutionFlowState.InspectConsequences => $"Understand what changed ({CompletedInspectionCount}/{inspected.Length})",
            ResolutionFlowState.InviteMinh => "Return to Minh and invite him to walk with you.",
            ResolutionFlowState.EscortMinh => "Walk with Minh to the doorway.",
            ResolutionFlowState.Leaving => "Lan and Minh leave the room together.",
            _ => "Resolution complete."
        };

    }
}
