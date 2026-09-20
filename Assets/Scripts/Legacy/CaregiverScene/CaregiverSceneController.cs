using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagesDementiaGame
{
    public enum CaregiverFlowState
    {
        PrepareEnvironment, MoveToApproachMarker, ChooseApproach, ApproachPlayback,
        ChooseResponse, ReadyForReplay, Transitioning
    }

    public sealed class CaregiverSceneController : MonoBehaviour, IInteractionHost
    {
        private enum ChoicePanel { None, Environment, Approach, Response, Ready }
        private struct WorldLabel { public string Text; public Transform Anchor; }

        private const float ReferenceWidth = 960f;
        private const float ReferenceHeight = 540f;
        [SerializeField] private RoomView roomView;
        [SerializeField] private EnvironmentController environmentController;

        private readonly List<WorldLabel> worldLabels = new List<WorldLabel>();
        private GameSession session;
        private TopDownPlayerController player;
        private Camera roomCamera;
        private WaypointCharacterMover lanMover;
        private ChoicePanel activePanel;
        private string interactionPrompt;
        private GUIStyle titleStyle, bodyStyle, labelStyle, buttonStyle, selectedButtonStyle, promptStyle;
        private bool roomBuilt;

        public CaregiverFlowState FlowState { get; private set; }
        public bool IsModalOpen => activePanel != ChoicePanel.None;
        public bool CanChooseApproach => FlowState == CaregiverFlowState.MoveToApproachMarker;
        public bool IsInteractionBlocked => IsModalOpen || FlowState == CaregiverFlowState.ApproachPlayback ||
                                            FlowState == CaregiverFlowState.ChooseResponse ||
                                            FlowState == CaregiverFlowState.Transitioning;

        public static void BuildForCurrentScene()
        {
            var existing = FindFirstObjectByType<CaregiverSceneController>();
            if (existing != null) existing.BuildRoom();
            else Debug.LogError("CaregiverScene requires an authored CaregiverSceneController and SharedRoom prefab.");
        }

        private void Awake() => session = GameSession.EnsureInstance();
        private void Start() => BuildRoom();
        private void Update() => HandleModalKeyboardInput();
        public void SetInteractionPrompt(string prompt) => interactionPrompt = prompt;

        public void OpenEnvironmentChoices()
        {
            if (FlowState == CaregiverFlowState.ApproachPlayback || FlowState == CaregiverFlowState.Transitioning) return;
            SetPanel(ChoicePanel.Environment);
        }

        public void OpenApproachChoices()
        {
            if (!CanChooseApproach) return;
            FlowState = CaregiverFlowState.ChooseApproach;
            SetPanel(ChoicePanel.Approach);
        }

        public void OpenMinhChoices()
        {
            if (FlowState == CaregiverFlowState.ChooseResponse) SetPanel(ChoicePanel.Response);
        }

        private void BuildRoom()
        {
            if (roomBuilt) return;
            if (roomView == null || environmentController == null)
            {
                Debug.LogError("CaregiverSceneController is missing its authored RoomView or EnvironmentController reference.", this);
                return;
            }
            if (!roomView.Configure(RoomPerspective.Caregiver, this)) return;
            roomBuilt = true;
            roomCamera = roomView.RoomCamera;
            player = roomView.PlayerController;
            lanMover = roomView.LanMover;

            AddWorldLabel("TELEVISION", roomView.Television.transform);
            AddWorldLabel("TABLE", roomView.Table.transform);
            AddWorldLabel("PHOTO", roomView.Photograph.transform);
            AddWorldLabel("SOFA", roomView.Sofa.transform);
            AddWorldLabel("MINH", roomView.Minh.transform);
            AddWorldLabel("LAN", roomView.Lan.transform);

            roomView.Television.GetComponent<TelevisionInteractable>().Initialize(this);
            roomView.ApproachStagingPoint.GetComponent<ApproachMarkerInteractable>().Initialize(this);
            AddWorldLabel("APPROACH", roomView.ApproachStagingPoint.transform);

            environmentController.Initialize(roomView);
            FlowState = session.SelectedEnvironmentChoice == EnvironmentChoice.NotChosen
                ? CaregiverFlowState.PrepareEnvironment : CaregiverFlowState.MoveToApproachMarker;
            ApplyMovementLock();
        }

        private void AddWorldLabel(string text, Transform anchor) => worldLabels.Add(new WorldLabel { Text = text, Anchor = anchor });

        private void SetPanel(ChoicePanel panel)
        {
            activePanel = panel;
            interactionPrompt = null;
            ApplyMovementLock();
        }

        private void ClosePanel()
        {
            if (activePanel == ChoicePanel.Response || activePanel == ChoicePanel.Ready) return;
            activePanel = ChoicePanel.None;
            if (FlowState == CaregiverFlowState.ChooseApproach) FlowState = CaregiverFlowState.MoveToApproachMarker;
            ApplyMovementLock();
        }

        private void ApplyMovementLock()
        {
            var canMove = activePanel == ChoicePanel.None &&
                          (FlowState == CaregiverFlowState.PrepareEnvironment || FlowState == CaregiverFlowState.MoveToApproachMarker);
            player?.SetMovementEnabled(canMove);
        }

        private void SelectEnvironment(int index)
        {
            var choice = index == 0 ? EnvironmentChoice.LeaveTelevisionOn :
                         index == 1 ? EnvironmentChoice.LowerTelevision : EnvironmentChoice.TurnOffTelevisionAndRestorePhoto;
            session.SetEnvironmentChoice(choice);
            environmentController.ApplyCurrentChoice();
            FlowState = CaregiverFlowState.MoveToApproachMarker;
            SetPanel(ChoicePanel.None);
        }

        private void SelectApproach(int index)
        {
            var choice = index == 0 ? ApproachChoice.CallFromDistance :
                         index == 1 ? ApproachChoice.ApproachQuickly : ApproachChoice.EnterViewAndIntroduce;
            session.SetApproachChoice(choice);
            activePanel = ChoicePanel.None;
            FlowState = CaregiverFlowState.ApproachPlayback;
            ApplyMovementLock();
            PlayApproach(choice);
        }

        private void PlayApproach(ApproachChoice choice)
        {
            if (choice == ApproachChoice.CallFromDistance)
            {
                roomView.Lan.GetComponentInChildren<CharacterVisualController>()?.Face(Vector2.right);
                StartCoroutine(CompleteApproachAfterDelay(0.4f));
                return;
            }
            var target = choice == ApproachChoice.ApproachQuickly ? roomView.LanCloseApproachPosition : roomView.LanIntroductionPosition;
            var speed = choice == ApproachChoice.ApproachQuickly ? 5f : 2.5f;
            lanMover.MoveAlong(roomView.BuildLanApproachRoute(target), speed, () =>
            {
                if (choice == ApproachChoice.EnterViewAndIntroduce) StartCoroutine(CompleteApproachAfterDelay(0.75f));
                else FinishApproach();
            });
        }

        private IEnumerator CompleteApproachAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            FinishApproach();
        }

        private void FinishApproach()
        {
            FlowState = CaregiverFlowState.ChooseResponse;
            SetPanel(ChoicePanel.Response);
        }

        private void SelectResponse(int index)
        {
            var choice = index == 0 ? ResponseChoice.CorrectMinh :
                         index == 1 ? ResponseChoice.GenericReassurance : ResponseChoice.AcknowledgeAndHelp;
            session.SetResponseChoice(choice);
            FlowState = CaregiverFlowState.ReadyForReplay;
            SetPanel(ChoicePanel.Ready);
        }

        private void BeginReplay()
        {
            if (FlowState != CaregiverFlowState.ReadyForReplay) return;
            FlowState = CaregiverFlowState.Transitioning;
            ApplyMovementLock();
            session.BeginReplay();
        }

        private void HandleModalKeyboardInput()
        {
            if (!IsModalOpen || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) { ClosePanel(); return; }
            var choice = ReadNumberChoice();
            if (choice >= 0)
            {
                if (activePanel == ChoicePanel.Environment) SelectEnvironment(choice);
                else if (activePanel == ChoicePanel.Approach) SelectApproach(choice);
                else if (activePanel == ChoicePanel.Response) SelectResponse(choice);
            }
            if (activePanel == ChoicePanel.Ready &&
                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) BeginReplay();
        }

        private static int ReadNumberChoice()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) return 0;
            if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) return 1;
            if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) return 2;
            return -1;
        }

        private void OnGUI()
        {
            if (!roomBuilt) return;
            EnsureStyles();
            var scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            var width = Screen.width / scale;
            var height = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            DrawObjective(width);
            DrawWorldLabels(scale);
            if (!string.IsNullOrEmpty(interactionPrompt) && !IsModalOpen)
                GUI.Label(new Rect(width * 0.25f, height - 62f, width * 0.5f, 42f), interactionPrompt, promptStyle);
            if (IsModalOpen) DrawModal(width, height);
        }

        private void DrawObjective(float width)
        {
            var objective = FlowState switch
            {
                CaregiverFlowState.PrepareEnvironment => "Find the television and prepare the room.",
                CaregiverFlowState.MoveToApproachMarker => "Move to the left-side approach point before addressing Minh.",
                CaregiverFlowState.ApproachPlayback => "Lan carries out the chosen approach.",
                CaregiverFlowState.ChooseResponse => "Respond to Minh's concern about the photograph.",
                _ => "The encounter is ready to replay from Minh's perspective."
            };
            GUI.Box(new Rect(24f, 18f, width - 48f, 68f), GUIContent.none);
            GUI.Label(new Rect(42f, 25f, width - 84f, 24f), "ACT 2 / LAN'S PERSPECTIVE", titleStyle);
            GUI.Label(new Rect(42f, 50f, width - 84f, 28f), objective, bodyStyle);
        }

        private void DrawWorldLabels(float scale)
        {
            foreach (var item in worldLabels)
            {
                if (item.Anchor == null || !item.Anchor.gameObject.activeInHierarchy) continue;
                var point = roomCamera.WorldToScreenPoint(item.Anchor.position);
                GUI.Label(new Rect(point.x / scale - 65f, (Screen.height - point.y) / scale - 12f, 130f, 24f), item.Text, labelStyle);
            }
        }

        private void DrawModal(float width, float height)
        {
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);
            GUI.color = previous;
            var rect = new Rect(width * 0.17f, height * 0.17f, width * 0.66f, height * 0.68f);
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 28f, rect.y + 22f, rect.width - 56f, rect.height - 44f));
            if (activePanel == ChoicePanel.Environment) DrawEnvironmentPanel();
            else if (activePanel == ChoicePanel.Approach) DrawApproachPanel();
            else if (activePanel == ChoicePanel.Response) DrawResponsePanel();
            else if (activePanel == ChoicePanel.Ready) DrawReadyPanel();
            GUILayout.EndArea();
        }

        private void DrawEnvironmentPanel()
        {
            GUILayout.Label("Prepare the room", titleStyle);
            GUILayout.Label("How will Lan handle the television and photograph?", bodyStyle);
            GUILayout.Space(16f);
            DrawButton("1  Leave the television on", 0, session.SelectedEnvironmentChoice == EnvironmentChoice.LeaveTelevisionOn, SelectEnvironment);
            DrawButton("2  Lower the television volume", 1, session.SelectedEnvironmentChoice == EnvironmentChoice.LowerTelevision, SelectEnvironment);
            DrawButton("3  Turn off the TV and restore the photograph", 2, session.SelectedEnvironmentChoice == EnvironmentChoice.TurnOffTelevisionAndRestorePhoto, SelectEnvironment);
            DrawCancelHint();
        }

        private void DrawApproachPanel()
        {
            GUILayout.Label("Approach Minh", titleStyle);
            GUILayout.Label("Choose how Lan will physically approach him.", bodyStyle);
            GUILayout.Space(16f);
            DrawButton("1  Call to him from across the room", 0, false, SelectApproach);
            DrawButton("2  Walk over quickly so lunch is not delayed", 1, false, SelectApproach);
            DrawButton("3  Enter his view, pause, and introduce yourself", 2, false, SelectApproach);
            DrawCancelHint();
        }

        private void DrawResponsePanel()
        {
            GUILayout.Label("Respond to Minh", titleStyle);
            GUILayout.Label("Minh asks where the photograph went.", bodyStyle);
            GUILayout.Space(16f);
            DrawButton("1  Correct him: it was only moved while cleaning", 0, false, SelectResponse);
            DrawButton("2  Reassure him: everything is fine", 1, false, SelectResponse);
            DrawButton("3  Acknowledge his concern and offer to look together", 2, false, SelectResponse);
        }

        private void DrawReadyPanel()
        {
            GUILayout.Label("The encounter is ready", titleStyle);
            GUILayout.Label("Lan's three choices are saved. Replay the same moment from Minh's perspective.", bodyStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Replay as Minh  [Enter]", buttonStyle, GUILayout.Height(52f))) BeginReplay();
        }

        private void DrawButton(string text, int index, bool selected, System.Action<int> action)
        {
            if (GUILayout.Button(text, selected ? selectedButtonStyle : buttonStyle, GUILayout.Height(48f))) action(index);
        }

        private void DrawCancelHint()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Escape closes this panel.", bodyStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.95f, 0.91f, 0.82f) } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = new Color(0.86f, 0.89f, 0.86f) } };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(14, 14, 8, 8), margin = new RectOffset(0, 0, 4, 4) };
            selectedButtonStyle = new GUIStyle(buttonStyle) { fontStyle = FontStyle.Bold };
            selectedButtonStyle.normal.textColor = new Color(0.22f, 0.95f, 0.72f);
            promptStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        }
    }
}
