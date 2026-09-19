using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagesDementiaGame
{
    public enum RecipientFlowState
    {
        ExplorePhotograph, InspectPhotograph, ExploreTelevision, LanEntering, ApproachPlayback,
        LunchDialogue, RecipientResponse, Resolution, Transitioning, Complete
    }

    public sealed class RecipientSceneController : MonoBehaviour, IInteractionHost
    {
        private enum DialoguePanel { None, Narration, Response }
        private const float ReferenceWidth = 960f;
        private const float ReferenceHeight = 540f;

        [SerializeField] private RoomArtSet artSet;

        private GameSession session;
        private RoomView roomView;
        private IReadOnlyList<string> beats;
        private DialoguePanel activePanel;
        private InspectionController inspection;
        private WaypointCharacterMover lanMover;
        private string interactionPrompt;
        private string dialogueText;
        private Action continueAction;
        private bool roomBuilt;
        private bool inspectionCanClose;
        private GUIStyle titleStyle, bodyStyle, labelStyle, buttonStyle, promptStyle;

        public RecipientFlowState FlowState { get; private set; }
        public bool IsReplay { get; private set; }
        public bool PhotographIsVisible => roomView != null && roomView.Photograph.activeSelf;
        public bool IsInteractionBlocked => activePanel != DialoguePanel.None ||
                                            (FlowState != RecipientFlowState.ExplorePhotograph &&
                                             FlowState != RecipientFlowState.ExploreTelevision);
        public bool CanExaminePhotograph => FlowState == RecipientFlowState.ExplorePhotograph && activePanel == DialoguePanel.None;
        public bool CanNoticeTelevision => FlowState == RecipientFlowState.ExploreTelevision && activePanel == DialoguePanel.None;

        public static void BuildForCurrentScene()
        {
            var existing = FindFirstObjectByType<RecipientSceneController>();
            if (existing != null) existing.BuildRoom();
            else new GameObject("Recipient Scene").AddComponent<RecipientSceneController>().BuildRoom();
        }

        private void Awake() => session = GameSession.EnsureInstance();
        private void Start() => BuildRoom();

        private void Update()
        {
            HandleInspectionInput();
            HandleDialogueInput();
        }

        public void SetInteractionPrompt(string prompt) => interactionPrompt = prompt;

        public void ExaminePhotograph()
        {
            if (!CanExaminePhotograph) return;
            SetState(RecipientFlowState.InspectPhotograph);
            inspectionCanClose = false;
            inspection.BeginPhotographInspection(PhotographIsVisible, roomView.Table.transform.position, () =>
            {
                inspectionCanClose = false;
                ShowNarration(beats[0], () => SetState(RecipientFlowState.ExploreTelevision));
            });
            StartCoroutine(EnableInspectionClose());
        }

        public void NoticeTelevision()
        {
            if (!CanNoticeTelevision) return;
            ShowNarration(beats[1], BeginLanEntrance);
        }

        private void BuildRoom()
        {
            if (roomBuilt) return;
            roomBuilt = true;
            IsReplay = session.Phase == NarrativePhase.Replay;
            beats = IsReplay ? PhaseOneContent.BuildReplayBeats(session) : PhaseOneContent.BaselineBeats;

            roomView = gameObject.AddComponent<RoomView>();
            roomView.Initialize(artSet);
            roomView.Build(RoomPerspective.Recipient);
            roomView.PlayerInteractionController.Initialize(this);
            roomView.SetLanVisible(false);
            lanMover = roomView.Lan.GetComponent<WaypointCharacterMover>();

            inspection = gameObject.AddComponent<InspectionController>();
            inspection.Initialize(roomView.RoomCamera);

            if (IsReplay) ApplyReplayEnvironment();
            else
            {
                roomView.SetTelevisionState(TelevisionState.On);
                roomView.SetPhotoVisible(false);
            }

            roomView.PhotographSpot.AddComponent<RecipientPhotoInteractable>().Initialize(this);
            roomView.Television.AddComponent<RecipientTelevisionInteractable>().Initialize(this);
            SetState(RecipientFlowState.ExplorePhotograph);
        }

        private void ApplyReplayEnvironment()
        {
            var state = session.SelectedEnvironmentChoice switch
            {
                EnvironmentChoice.LowerTelevision => TelevisionState.Lowered,
                EnvironmentChoice.TurnOffTelevisionAndRestorePhoto => TelevisionState.Off,
                _ => TelevisionState.On
            };
            roomView.SetTelevisionState(state);
            roomView.SetPhotoVisible(session.ReplayEffects.PhotoRestored && session.ReplayEffects.ObjectFamiliarity > 0);
        }

        private IEnumerator EnableInspectionClose()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            inspectionCanClose = FlowState == RecipientFlowState.InspectPhotograph;
        }

        private void HandleInspectionInput()
        {
            if (FlowState != RecipientFlowState.InspectPhotograph || !inspectionCanClose || Keyboard.current == null) return;
            if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                inspectionCanClose = false;
                inspection.FinishInspection();
            }
        }

        private void BeginLanEntrance()
        {
            SetState(RecipientFlowState.LanEntering);
            roomView.Lan.transform.position = roomView.LanDoorwayPosition;
            roomView.SetLanVisible(true);
            lanMover.MoveAlong(roomView.LanEntranceWaypoints, 2.6f, BeginApproachPlayback);
        }

        private void BeginApproachPlayback()
        {
            SetState(RecipientFlowState.ApproachPlayback);
            var approach = IsReplay ? session.SelectedApproachChoice : ApproachChoice.ApproachQuickly;
            if (approach == ApproachChoice.CallFromDistance)
            {
                roomView.Lan.GetComponentInChildren<CharacterVisualController>()?.Face(Vector2.right);
                StartCoroutine(FinishApproachAfterDelay(0.4f));
                return;
            }

            var target = approach == ApproachChoice.ApproachQuickly
                ? roomView.LanCloseApproachPosition : roomView.LanIntroductionPosition;
            var speed = approach == ApproachChoice.ApproachQuickly ? 5f : 2.5f;
            lanMover.MoveAlong(roomView.BuildLanApproachRoute(target), speed, () =>
            {
                if (approach == ApproachChoice.EnterViewAndIntroduce) StartCoroutine(FinishApproachAfterDelay(0.75f));
                else FinishApproach();
            });
        }

        private IEnumerator FinishApproachAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            FinishApproach();
        }

        private void FinishApproach()
        {
            ShowNarration(beats[2], ShowLunchDialogue);
        }

        private void ShowLunchDialogue()
        {
            SetState(RecipientFlowState.LunchDialogue);
            ShowNarration(beats[3], ShowResponseChoices);
        }

        private void ShowResponseChoices()
        {
            SetState(RecipientFlowState.RecipientResponse);
            activePanel = DialoguePanel.Response;
            interactionPrompt = null;
            ApplyMovementLock();
        }

        private void SelectResponse(int index)
        {
            if (activePanel != DialoguePanel.Response) return;
            var responseText = beats[4];
            if (index == 1)
            {
                responseText = session.ReplayEffects.SpeechClarity switch
                {
                    SpeechClarity.Clear => "You ask Lan to repeat herself. She says clearly, \"Lunch is ready. Would you like to come with me?\"",
                    SpeechClarity.Partial => "You ask her to repeat herself. This time you catch: \"Lunch is... would you like to come?\"",
                    _ => "You ask her to repeat herself, but the television still breaks her words apart."
                };
            }
            else if (index == 2)
                responseText = "You ask, \"Can we look at the photograph before lunch?\" Lan gives your concern space in the conversation.";

            SetState(RecipientFlowState.Resolution);
            ShowNarration(responseText, ShowResolution);
        }

        private void ShowResolution() => ShowNarration(beats[5], FinishRecipientAct);

        private void FinishRecipientAct()
        {
            SetState(RecipientFlowState.Transitioning);
            if (IsReplay)
            {
                session.BeginReflection();
                SetState(RecipientFlowState.Complete);
            }
            else session.BeginIntervention();
        }

        private void ShowNarration(string text, Action onContinue)
        {
            dialogueText = text;
            continueAction = onContinue;
            activePanel = DialoguePanel.Narration;
            interactionPrompt = null;
            ApplyMovementLock();
        }

        private void ContinueNarration()
        {
            if (activePanel != DialoguePanel.Narration) return;
            var action = continueAction;
            continueAction = null;
            activePanel = DialoguePanel.None;
            action?.Invoke();
            ApplyMovementLock();
        }

        private void SetState(RecipientFlowState state)
        {
            FlowState = state;
            interactionPrompt = null;
            ApplyMovementLock();
        }

        private void ApplyMovementLock()
        {
            var canMove = activePanel == DialoguePanel.None &&
                          (FlowState == RecipientFlowState.ExplorePhotograph || FlowState == RecipientFlowState.ExploreTelevision);
            roomView?.PlayerController?.SetMovementEnabled(canMove);
        }

        private void HandleDialogueInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (activePanel == DialoguePanel.Narration &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                ContinueNarration();
                return;
            }
            if (activePanel != DialoguePanel.Response) return;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame ||
                (!IsReplay && keyboard.enterKey.wasPressedThisFrame)) SelectResponse(0);
            else if (IsReplay && session.ReplayEffects.Agency >= 1 &&
                     (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)) SelectResponse(1);
            else if (IsReplay && session.ReplayEffects.Agency >= 2 &&
                     (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)) SelectResponse(2);
        }

        private void OnGUI()
        {
            if (!roomBuilt || session.Phase == NarrativePhase.Reflection) return;
            EnsureStyles();
            var scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            var width = Screen.width / scale;
            var height = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            DrawObjective(width);
            DrawCharacterLabel(roomView.Minh, "MINH", scale);
            if (roomView.Lan.activeInHierarchy) DrawCharacterLabel(roomView.Lan, "LAN", scale);
            if (!string.IsNullOrEmpty(interactionPrompt) && activePanel == DialoguePanel.None)
                GUI.Label(new Rect(width * 0.25f, height - 62f, width * 0.5f, 42f), interactionPrompt, promptStyle);
            if (FlowState == RecipientFlowState.InspectPhotograph) DrawInspectionPanel(width, height);
            else if (activePanel != DialoguePanel.None) DrawDialoguePanel(width, height);
        }

        private void DrawObjective(float width)
        {
            var objective = FlowState switch
            {
                RecipientFlowState.ExplorePhotograph => IsReplay && PhotographIsVisible
                    ? "Look at the familiar photograph on the table." : "Something is missing from the table. Look more closely.",
                RecipientFlowState.ExploreTelevision => "The television noise demands your attention.",
                RecipientFlowState.LanEntering => "Someone is entering the room.",
                _ => "Try to understand what the woman wants."
            };
            GUI.Box(new Rect(24f, 18f, width - 48f, 68f), GUIContent.none);
            GUI.Label(new Rect(42f, 25f, width - 84f, 24f),
                IsReplay ? "ACT 3 / MINH'S PERSPECTIVE - REPLAY" : "ACT 1 / MINH'S PERSPECTIVE", titleStyle);
            GUI.Label(new Rect(42f, 50f, width - 84f, 28f), objective, bodyStyle);
        }

        private void DrawCharacterLabel(GameObject character, string text, float scale)
        {
            var point = roomView.RoomCamera.WorldToScreenPoint(character.transform.position);
            GUI.Label(new Rect(point.x / scale - 50f, (Screen.height - point.y) / scale - 14f, 100f, 24f), text, labelStyle);
        }

        private void DrawInspectionPanel(float width, float height)
        {
            var text = PhotographIsVisible
                ? "The family photograph is here. The familiar faces help anchor the room."
                : "The space on the table is empty. You are certain the photograph belongs here.";
            GUI.Box(new Rect(width * 0.2f, height - 118f, width * 0.6f, 86f), text, bodyStyle);
            if (inspectionCanClose && GUI.Button(new Rect(width * 0.38f, height - 72f, width * 0.24f, 32f), "Return  [E / Enter / Escape]", buttonStyle))
            {
                inspectionCanClose = false;
                inspection.FinishInspection();
            }
        }

        private void DrawDialoguePanel(float width, float height)
        {
            var panelHeight = activePanel == DialoguePanel.Response ? 225f : 145f;
            var rect = new Rect(38f, height - panelHeight - 24f, width - 76f, panelHeight);
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 22f, rect.y + 18f, rect.width - 44f, rect.height - 34f));
            if (activePanel == DialoguePanel.Narration)
            {
                GUILayout.Label(dialogueText, bodyStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Continue  [Enter / Space]", buttonStyle, GUILayout.Height(40f))) ContinueNarration();
            }
            else
            {
                GUILayout.Label("How can you respond?", titleStyle);
                if (GUILayout.Button("1  Where is the photograph?", buttonStyle, GUILayout.Height(38f))) SelectResponse(0);
                if (IsReplay && session.ReplayEffects.Agency >= 1 && GUILayout.Button("2  Please say that again.", buttonStyle, GUILayout.Height(38f))) SelectResponse(1);
                if (IsReplay && session.ReplayEffects.Agency >= 2 && GUILayout.Button("3  Can we look at the photograph before lunch?", buttonStyle, GUILayout.Height(38f))) SelectResponse(2);
            }
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.95f, 0.91f, 0.82f) } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.9f, 0.91f, 0.88f) } };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(14, 14, 7, 7) };
            promptStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        }
    }
}
