using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagesDementiaGame
{
    public enum ResolutionFlowState { InspectConsequences, InviteMinh, EscortMinh, Leaving, Complete }

    public sealed class ResolutionSceneController : MonoBehaviour, IInteractionHost
    {
        private const float ReferenceWidth = 960f;
        private const float ReferenceHeight = 540f;
        [SerializeField] private RoomView roomView;

        private readonly bool[] inspected = new bool[3];
        private GameSession session;
        private SceneAudioController audioController;
        private string interactionPrompt;
        private string panelText;
        private Action panelClosedAction;
        private bool panelOpen;
        private bool initialized;
        private GUIStyle titleStyle, bodyStyle, buttonStyle, promptStyle;

        public ResolutionFlowState FlowState { get; private set; }
        public bool IsInteractionBlocked => panelOpen || FlowState == ResolutionFlowState.Leaving ||
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
        }
        private void Start() => Initialize();

        private void Update()
        {
            if (!initialized) return;
            if (panelOpen && Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.escapeKey.wasPressedThisFrame)) ClosePanel();
            if (FlowState == ResolutionFlowState.EscortMinh) CheckExitProgress();
        }

        public void SetInteractionPrompt(string prompt) => interactionPrompt = prompt;

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
        }

        public void InviteMinh()
        {
            if (FlowState != ResolutionFlowState.InviteMinh || panelOpen) return;
            audioController?.PlayUiClick();
            ShowPanel("Lan gives Minh time to stand. \"Would you like to walk outside with me?\"\n\n" +
                      "Minh agrees. Walk toward the doorway slowly enough for him to follow.", BeginEscort);
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
            if (!roomView.Configure(RoomPerspective.Resolution, this)) return;
            initialized = true;
            audioController?.PlayReflectionMusic();
            FlowState = ResolutionFlowState.InspectConsequences;
            roomView.InitializeResolutionInteractions(this);
            ApplyResolutionEnvironment();
            ApplyMovementLock();
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
            roomView.SetPhotoVisible(session.PhotoRestored);
        }

        private void BeginEscort()
        {
            audioController?.PlayConfirm();
            FlowState = ResolutionFlowState.EscortMinh;
            roomView.BeginResolutionEscort();
            ApplyMovementLock();
        }

        private void CheckExitProgress()
        {
            if (!roomView.IsLanAtResolutionExit || !roomView.IsMinhAtResolutionExit) return;
            FlowState = ResolutionFlowState.Leaving;
            roomView.PlayerController.SetMovementEnabled(false);
            roomView.StopResolutionEscort();
            audioController?.PlayEffect(audioController.Library?.DoorOpening);
            FlowState = ResolutionFlowState.Complete;
            session.BeginReflection();
        }

        private void ShowPanel(string text, Action onClose = null)
        {
            panelText = text;
            panelClosedAction = onClose;
            panelOpen = true;
            interactionPrompt = null;
            ApplyMovementLock();
            audioController?.PlayDialogueBlip();
        }

        private void ClosePanel()
        {
            if (!panelOpen) return;
            audioController?.PlayUiClick();
            panelOpen = false;
            var action = panelClosedAction;
            panelClosedAction = null;
            action?.Invoke();
            ApplyMovementLock();
        }

        private void ApplyMovementLock() => roomView?.PlayerController?.SetMovementEnabled(initialized && !IsInteractionBlocked);

        private void OnGUI()
        {
            if (!initialized || session.Phase == NarrativePhase.Reflection) return;
            EnsureStyles();
            var scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            var width = Screen.width / scale;
            var height = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.Box(new Rect(24f, 18f, width - 48f, 68f), GUIContent.none);
            GUI.Label(new Rect(42f, 25f, width - 84f, 24f), "ACT 3 / RESOLUTION", titleStyle);
            GUI.Label(new Rect(42f, 50f, width - 84f, 28f), ObjectiveText(), bodyStyle);
            if (!string.IsNullOrEmpty(interactionPrompt) && !panelOpen)
                GUI.Label(new Rect(width * 0.25f, height - 62f, width * 0.5f, 42f), interactionPrompt, promptStyle);
            if (panelOpen)
            {
                var rect = new Rect(38f, height - 190f, width - 76f, 160f);
                GUI.Box(rect, GUIContent.none);
                GUI.Label(new Rect(rect.x + 22f, rect.y + 18f, rect.width - 44f, 92f), panelText, bodyStyle);
                if (GUI.Button(new Rect(rect.x + rect.width - 210f, rect.y + 112f, 188f, 34f), "Continue  [Enter]", buttonStyle)) ClosePanel();
            }
        }

        private string ObjectiveText() => FlowState switch
        {
            ResolutionFlowState.InspectConsequences => $"Understand what changed ({CompletedInspectionCount}/{inspected.Length})",
            ResolutionFlowState.InviteMinh => "Return to Minh and invite him to walk with you.",
            ResolutionFlowState.EscortMinh => "Walk with Minh to the doorway.",
            ResolutionFlowState.Leaving => "Lan and Minh leave the room together.",
            _ => "Resolution complete."
        };

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.95f, 0.91f, 0.82f) } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.9f, 0.91f, 0.88f) } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
            promptStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        }
    }
}
