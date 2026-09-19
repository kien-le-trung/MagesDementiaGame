using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagesDementiaGame
{
    public sealed class CaregiverSceneController : MonoBehaviour, IInteractionHost
    {
        private enum ChoicePanel
        {
            None,
            Environment,
            Approach,
            Response,
            Ready
        }

        private struct WorldLabel
        {
            public string Text;
            public Transform Anchor;
        }

        private const float ReferenceWidth = 960f;
        private const float ReferenceHeight = 540f;

        [Header("Caregiver room artwork")]
        [SerializeField] private RoomArtSet artSet;

        private readonly List<WorldLabel> worldLabels = new List<WorldLabel>();
        private GameSession session;
        private TopDownPlayerController player;
        private EnvironmentController environmentController;
        private RoomView roomView;
        private Camera roomCamera;
        private ChoicePanel activePanel;
        private string interactionPrompt;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;
        private GUIStyle promptStyle;
        private bool roomBuilt;

        public bool IsModalOpen => activePanel != ChoicePanel.None;
        public bool IsInteractionBlocked => IsModalOpen;

        public static void BuildForCurrentScene()
        {
            var existing = FindFirstObjectByType<CaregiverSceneController>();
            if (existing != null)
            {
                existing.BuildRoom();
                return;
            }

            var root = new GameObject("Caregiver Scene");
            var controller = root.AddComponent<CaregiverSceneController>();
            controller.BuildRoom();
        }

        private void Awake()
        {
            session = GameSession.EnsureInstance();
        }

        private void Start()
        {
            // Also builds when this scene is entered directly and scene callbacks run in an unusual order.
            BuildRoom();
        }

        private void Update()
        {
            HandleModalKeyboardInput();
        }

        public void SetInteractionPrompt(string prompt)
        {
            interactionPrompt = prompt;
        }

        public void OpenEnvironmentChoices()
        {
            SetPanel(ChoicePanel.Environment);
        }

        public void OpenMinhChoices()
        {
            if (session.SelectedEnvironmentChoice == EnvironmentChoice.NotChosen)
            {
                return;
            }

            // Always start at approach so both saved choices can be reviewed or changed.
            SetPanel(ChoicePanel.Approach);
        }

        private void BuildRoom()
        {
            if (roomBuilt)
            {
                return;
            }

            roomBuilt = true;
            roomView = gameObject.AddComponent<RoomView>();
            roomView.Initialize(artSet);
            roomView.Build(RoomPerspective.Caregiver);
            roomCamera = roomView.RoomCamera;

            AddWorldLabel("TELEVISION", roomView.Television.transform);
            AddWorldLabel("TABLE", roomView.Table.transform);
            AddWorldLabel("PHOTO", roomView.Photograph.transform);
            AddWorldLabel("SOFA", roomView.Sofa.transform);
            AddWorldLabel("MINH", roomView.Minh.transform);
            AddWorldLabel("LAN", roomView.Lan.transform);

            var televisionInteractable = roomView.Television.AddComponent<TelevisionInteractable>();
            televisionInteractable.Initialize(this);
            var minhInteractable = roomView.Minh.AddComponent<MinhInteractable>();
            minhInteractable.Initialize(this);

            player = roomView.PlayerController;
            roomView.PlayerInteractionController.Initialize(this);

            environmentController = gameObject.AddComponent<EnvironmentController>();
            environmentController.Initialize(roomView);
        }

        private void AddWorldLabel(string text, Transform anchor)
        {
            worldLabels.Add(new WorldLabel { Text = text, Anchor = anchor });
        }

        private void SetPanel(ChoicePanel panel)
        {
            activePanel = panel;
            player?.SetMovementEnabled(panel == ChoicePanel.None);
            interactionPrompt = null;
        }

        private void ClosePanel()
        {
            SetPanel(ChoicePanel.None);
        }

        private void SelectEnvironment(int index)
        {
            var choice = index == 0
                ? EnvironmentChoice.LeaveTelevisionOn
                : index == 1
                    ? EnvironmentChoice.LowerTelevision
                    : EnvironmentChoice.TurnOffTelevisionAndRestorePhoto;
            session.SetEnvironmentChoice(choice);
            environmentController.ApplyCurrentChoice();
            ClosePanel();
        }

        private void SelectApproach(int index)
        {
            var choice = index == 0
                ? ApproachChoice.CallFromDistance
                : index == 1
                    ? ApproachChoice.ApproachQuickly
                    : ApproachChoice.EnterViewAndIntroduce;
            session.SetApproachChoice(choice);
            SetPanel(ChoicePanel.Response);
        }

        private void SelectResponse(int index)
        {
            var choice = index == 0
                ? ResponseChoice.CorrectMinh
                : index == 1
                    ? ResponseChoice.GenericReassurance
                    : ResponseChoice.AcknowledgeAndHelp;
            session.SetResponseChoice(choice);
            SetPanel(ChoicePanel.Ready);
        }

        private void HandleModalKeyboardInput()
        {
            if (!IsModalOpen || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClosePanel();
                return;
            }

            var choice = ReadNumberChoice();
            if (choice >= 0)
            {
                switch (activePanel)
                {
                    case ChoicePanel.Environment:
                        SelectEnvironment(choice);
                        break;
                    case ChoicePanel.Approach:
                        SelectApproach(choice);
                        break;
                    case ChoicePanel.Response:
                        SelectResponse(choice);
                        break;
                }
            }

            if (activePanel == ChoicePanel.Ready &&
                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                session.BeginReplay();
            }
        }

        private static int ReadNumberChoice()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                return 0;
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            {
                return 1;
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            {
                return 2;
            }
            return -1;
        }

        private void OnGUI()
        {
            EnsureStyles();

            var scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            var width = Screen.width / scale;
            var height = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            DrawObjective(width);
            DrawWorldLabels(scale);

            if (!string.IsNullOrEmpty(interactionPrompt) && !IsModalOpen)
            {
                GUI.Label(new Rect(width * 0.25f, height - 62f, width * 0.5f, 42f), interactionPrompt, promptStyle);
            }

            if (IsModalOpen)
            {
                DrawModal(width, height);
            }
        }

        private void DrawObjective(float width)
        {
            var objective = session.SelectedEnvironmentChoice == EnvironmentChoice.NotChosen
                ? "Find the television and prepare the room before approaching Minh."
                : "The room is prepared. Approach Minh near the sofa.";

            GUI.Box(new Rect(24f, 18f, width - 48f, 68f), GUIContent.none);
            GUI.Label(new Rect(42f, 25f, width - 84f, 24f), "ACT 2 / LAN'S PERSPECTIVE", titleStyle);
            GUI.Label(new Rect(42f, 50f, width - 84f, 28f), objective, bodyStyle);
        }

        private void DrawWorldLabels(float scale)
        {
            if (roomCamera == null)
            {
                return;
            }

            foreach (var worldLabel in worldLabels)
            {
                if (worldLabel.Anchor == null || !worldLabel.Anchor.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var screenPoint = roomCamera.WorldToScreenPoint(worldLabel.Anchor.position);
                var x = screenPoint.x / scale;
                var y = (Screen.height - screenPoint.y) / scale;
                GUI.Label(new Rect(x - 65f, y - 12f, 130f, 24f), worldLabel.Text, labelStyle);
            }
        }

        private void DrawModal(float width, float height)
        {
            var oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            var panelRect = new Rect(width * 0.17f, height * 0.17f, width * 0.66f, height * 0.68f);
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(panelRect.x + 28f, panelRect.y + 22f, panelRect.width - 56f, panelRect.height - 44f));

            switch (activePanel)
            {
                case ChoicePanel.Environment:
                    DrawEnvironmentPanel();
                    break;
                case ChoicePanel.Approach:
                    DrawApproachPanel();
                    break;
                case ChoicePanel.Response:
                    DrawResponsePanel();
                    break;
                case ChoicePanel.Ready:
                    DrawReadyPanel();
                    break;
            }

            GUILayout.EndArea();
        }

        private void DrawEnvironmentPanel()
        {
            GUILayout.Label("Prepare the room", titleStyle);
            GUILayout.Label("How will Lan handle the television and photograph? Press 1-3 or choose with the mouse.", bodyStyle);
            GUILayout.Space(16f);
            DrawEnvironmentButton("1  Leave the television on", EnvironmentChoice.LeaveTelevisionOn, 0);
            DrawEnvironmentButton("2  Lower the television volume", EnvironmentChoice.LowerTelevision, 1);
            DrawEnvironmentButton("3  Turn off the TV and restore the photograph", EnvironmentChoice.TurnOffTelevisionAndRestorePhoto, 2);
            DrawCancelHint();
        }

        private void DrawApproachPanel()
        {
            GUILayout.Label("Approach Minh", titleStyle);
            GUILayout.Label("Lan is ready to invite Minh to lunch. Press 1-3 or choose with the mouse.", bodyStyle);
            GUILayout.Space(16f);
            DrawApproachButton("1  Call to him from across the room", ApproachChoice.CallFromDistance, 0);
            DrawApproachButton("2  Walk over quickly so lunch is not delayed", ApproachChoice.ApproachQuickly, 1);
            DrawApproachButton("3  Enter his view, pause, and introduce yourself", ApproachChoice.EnterViewAndIntroduce, 2);
            DrawCancelHint();
        }

        private void DrawResponsePanel()
        {
            GUILayout.Label("Respond to Minh", titleStyle);
            GUILayout.Label("Minh asks where the photograph went. Press 1-3 or choose with the mouse.", bodyStyle);
            GUILayout.Space(16f);
            DrawResponseButton("1  Correct him: it was only moved while cleaning", ResponseChoice.CorrectMinh, 0);
            DrawResponseButton("2  Reassure him: everything is fine", ResponseChoice.GenericReassurance, 1);
            DrawResponseButton("3  Acknowledge his concern and offer to look together", ResponseChoice.AcknowledgeAndHelp, 2);
            DrawCancelHint();
        }

        private void DrawReadyPanel()
        {
            GUILayout.Label("The encounter is ready", titleStyle);
            GUILayout.Label("Lan's three choices are saved. Replay the same moment from Minh's perspective to see their combined effects.", bodyStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Replay as Minh  [Enter]", buttonStyle, GUILayout.Height(52f)))
            {
                session.BeginReplay();
            }
            DrawCancelHint();
        }

        private void DrawEnvironmentButton(string text, EnvironmentChoice choice, int index)
        {
            var style = session.SelectedEnvironmentChoice == choice ? selectedButtonStyle : buttonStyle;
            if (GUILayout.Button(text, style, GUILayout.Height(48f)))
            {
                SelectEnvironment(index);
            }
        }

        private void DrawApproachButton(string text, ApproachChoice choice, int index)
        {
            var style = session.SelectedApproachChoice == choice ? selectedButtonStyle : buttonStyle;
            if (GUILayout.Button(text, style, GUILayout.Height(48f)))
            {
                SelectApproach(index);
            }
        }

        private void DrawResponseButton(string text, ResponseChoice choice, int index)
        {
            var style = session.SelectedResponseChoice == choice ? selectedButtonStyle : buttonStyle;
            if (GUILayout.Button(text, style, GUILayout.Height(48f)))
            {
                SelectResponse(index);
            }
        }

        private void DrawCancelHint()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Escape closes this panel. Saved choices can be changed before replay.", bodyStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.91f, 0.82f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                normal = { textColor = new Color(0.86f, 0.89f, 0.86f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(14, 14, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
            selectedButtonStyle = new GUIStyle(buttonStyle);
            selectedButtonStyle.normal.textColor = new Color(0.22f, 0.95f, 0.72f);
            selectedButtonStyle.fontStyle = FontStyle.Bold;
            promptStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
    }
}
