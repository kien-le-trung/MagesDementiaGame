using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagesDementiaGame
{
    public sealed class CaregiverSceneController : MonoBehaviour
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

        private static Sprite rectangleSprite;

        private readonly List<WorldLabel> worldLabels = new List<WorldLabel>();
        private GameSession session;
        private TopDownPlayerController player;
        private EnvironmentController environmentController;
        private Camera roomCamera;
        private ChoicePanel activePanel;
        private string interactionPrompt;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;
        private GUIStyle promptStyle;

        public bool IsModalOpen => activePanel != ChoicePanel.None;

        public static void BuildForCurrentScene()
        {
            var existing = FindFirstObjectByType<CaregiverSceneController>();
            if (existing != null)
            {
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
            CreateCamera();

            CreateRectangle("Floor", Vector2.zero, new Vector2(15.5f, 8.4f), new Color(0.25f, 0.23f, 0.21f), -10, false);
            CreateRectangle("Rug", new Vector2(0.6f, -0.2f), new Vector2(7.4f, 4.5f), new Color(0.28f, 0.14f, 0.16f), -8, false);

            CreateWall("Top Wall", new Vector2(0f, 4.45f), new Vector2(16.4f, 0.6f));
            CreateWall("Bottom Wall Left", new Vector2(-4.7f, -4.45f), new Vector2(7f, 0.6f));
            CreateWall("Bottom Wall Right", new Vector2(4.7f, -4.45f), new Vector2(7f, 0.6f));
            CreateWall("Left Wall", new Vector2(-8f, 0f), new Vector2(0.6f, 9.5f));
            CreateWall("Right Wall", new Vector2(8f, 0f), new Vector2(0.6f, 9.5f));

            var doorway = CreateRectangle("Doorway", new Vector2(0f, -4.25f), new Vector2(2.2f, 0.45f), new Color(0.52f, 0.37f, 0.22f), -1, true);
            AddWorldLabel("DOORWAY", doorway.transform);

            var television = CreateRectangle("Television", new Vector2(-6.55f, 2.4f), new Vector2(1.8f, 1.15f), new Color(0.18f, 0.72f, 0.95f), 1, true);
            AddWorldLabel("TELEVISION", television.transform);
            var televisionInteractable = television.AddComponent<TelevisionInteractable>();
            televisionInteractable.Initialize(this);

            var table = CreateRectangle("Table", new Vector2(-1.4f, 1.05f), new Vector2(2.6f, 1.25f), new Color(0.48f, 0.31f, 0.18f), 0, true);
            AddWorldLabel("TABLE", table.transform);
            var photograph = CreateRectangle("Family Photograph", new Vector2(-1.4f, 1.05f), new Vector2(0.55f, 0.42f), new Color(0.96f, 0.78f, 0.24f), 3, false);
            AddWorldLabel("PHOTO", photograph.transform);

            var sofa = CreateRectangle("Sofa", new Vector2(4.7f, 2.65f), new Vector2(4.1f, 1.3f), new Color(0.40f, 0.17f, 0.21f), 0, true);
            AddWorldLabel("SOFA", sofa.transform);

            var minh = CreateRectangle("Minh", new Vector2(4.65f, 1.35f), new Vector2(0.75f, 1.0f), new Color(0.93f, 0.65f, 0.25f), 3, true);
            AddWorldLabel("MINH", minh.transform);
            var minhInteractable = minh.AddComponent<MinhInteractable>();
            minhInteractable.Initialize(this);

            var sideTable = CreateRectangle("Side Table", new Vector2(6.7f, 0.3f), new Vector2(1.0f, 1.0f), new Color(0.42f, 0.27f, 0.16f), 0, true);
            AddWorldLabel("SIDE TABLE", sideTable.transform);

            CreatePlayer();

            environmentController = gameObject.AddComponent<EnvironmentController>();
            environmentController.Initialize(television.GetComponent<SpriteRenderer>(), photograph);
        }

        private void CreateCamera()
        {
            roomCamera = Camera.main;
            if (roomCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                roomCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            roomCamera.orthographic = true;
            roomCamera.orthographicSize = 5.3f;
            roomCamera.transform.position = new Vector3(0f, 0f, -10f);
            roomCamera.clearFlags = CameraClearFlags.SolidColor;
            roomCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        }

        private void CreatePlayer()
        {
            var lan = CreateRectangle("Lan", new Vector2(0f, -3.35f), new Vector2(0.7f, 0.95f), new Color(0.20f, 0.82f, 0.68f), 4, false);
            AddWorldLabel("LAN", lan.transform);

            var collider = lan.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.82f, 0.82f);

            var body = lan.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            player = lan.AddComponent<TopDownPlayerController>();
            var interaction = lan.AddComponent<InteractionController>();
            interaction.Initialize(this);
        }

        private GameObject CreateWall(string name, Vector2 position, Vector2 size)
        {
            return CreateRectangle(name, position, size, new Color(0.68f, 0.62f, 0.53f), 0, true);
        }

        private static GameObject CreateRectangle(
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            int sortingOrder,
            bool addCollider)
        {
            var rectangle = new GameObject(name);
            rectangle.transform.position = new Vector3(position.x, position.y, 0f);
            rectangle.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = rectangle.AddComponent<SpriteRenderer>();
            renderer.sprite = GetRectangleSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (addCollider)
            {
                rectangle.AddComponent<BoxCollider2D>();
            }

            return rectangle;
        }

        private static Sprite GetRectangleSprite()
        {
            if (rectangleSprite == null)
            {
                rectangleSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                rectangleSprite.name = "Runtime Rectangle";
            }

            return rectangleSprite;
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
