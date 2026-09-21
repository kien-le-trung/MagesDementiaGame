using System.Linq;
using MagesDementiaGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MagesDementiaGameEditor
{
    [InitializeOnLoad]
    public static class ResolutionCanvasBuilder
    {
        private const string ScenePath = "Assets/Scenes/ResolutionScene.unity";
        private static readonly Color PanelColor = new Color(0.04f, 0.07f, 0.08f, 0.92f);
        private static readonly Color TextColor = new Color(0.96f, 0.94f, 0.88f, 1f);
        private static Font font;

        static ResolutionCanvasBuilder()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        [MenuItem("MAGES/Rebuild Resolution Canvas")]
        public static void RebuildFromMenu() => Build(true);

        private static void BuildIfMissing()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) Build(false);
        }

        private static void Build(bool force)
        {
            var openScene = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).FirstOrDefault(scene => scene.path == ScenePath);
            var openedHere = !openScene.IsValid();
            var scene = openedHere ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : openScene;
            if (!scene.IsValid()) return;

            var existingCanvas = FindInScene(scene, "ResolutionCanvas");
            if (!force && existingCanvas != null)
            {
                if (FindInScene(scene, "EncouragementPanel") == null)
                    AddEncouragementPanel(scene, existingCanvas);
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            if (existingCanvas != null) Object.DestroyImmediate(existingCanvas);
            var existingEventSystem = FindInScene(scene, "ResolutionEventSystem");
            if (force && existingEventSystem != null) Object.DestroyImmediate(existingEventSystem);

            var controller = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ResolutionSceneController>(true)).FirstOrDefault();
            if (controller == null)
            {
                Debug.LogError("ResolutionScene is missing ResolutionSceneController.");
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("ResolutionCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var objectivePanel = CreatePanel("ObjectivePanel", canvasObject.transform, new Color(0.05f, 0.08f, 0.09f, 0.84f));
            SetAnchors((RectTransform)objectivePanel.transform, new Vector2(0.025f, 0.875f), new Vector2(0.57f, 0.975f));
            var objectiveText = CreateText("ObjectiveText", objectivePanel.transform,
                "ACT 3 / RESOLUTION\nUnderstand what changed (0/3)", 27, TextAnchor.MiddleLeft);
            SetAnchors(objectiveText.rectTransform, new Vector2(0.035f, 0.08f), new Vector2(0.97f, 0.92f));

            var promptPanel = CreatePanel("InteractionPromptPanel", canvasObject.transform, new Color(0.07f, 0.11f, 0.12f, 0.94f));
            SetAnchors((RectTransform)promptPanel.transform, new Vector2(0.25f, 0.025f), new Vector2(0.75f, 0.10f));
            var promptText = CreateText("InteractionPromptText", promptPanel.transform, string.Empty, 27, TextAnchor.MiddleCenter);
            SetAnchors(promptText.rectTransform, new Vector2(0.025f, 0.08f), new Vector2(0.975f, 0.92f));
            promptPanel.SetActive(false);

            var reflectionPanel = CreatePanel("ConsequencePanel", canvasObject.transform, PanelColor);
            SetAnchors((RectTransform)reflectionPanel.transform, new Vector2(0.06f, 0.055f), new Vector2(0.94f, 0.50f));
            var reflectionText = CreateText("ConsequenceText", reflectionPanel.transform, string.Empty, 40, TextAnchor.UpperLeft);
            SetAnchors(reflectionText.rectTransform, new Vector2(0.035f, 0.20f), new Vector2(0.965f, 0.93f));
            var continueButton = CreateButton("ContinueButton", reflectionPanel.transform, "Continue  [Enter]",
                new Vector2(0.72f, 0.045f), new Vector2(0.965f, 0.18f));
            reflectionPanel.SetActive(false);

            var encouragementPanel = CreateEncouragementPanel(canvasObject.transform, out var encouragementText,
                out var validateButton, out var pressureButton);

            if (FindEventSystem(scene) == null)
            {
                var eventSystem = new GameObject("ResolutionEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
            }

            var serialized = new SerializedObject(controller);
            Set(serialized, "resolutionCanvas", canvas);
            Set(serialized, "objectiveText", objectiveText);
            Set(serialized, "interactionPromptPanel", promptPanel);
            Set(serialized, "interactionPromptText", promptText);
            Set(serialized, "reflectionPanel", reflectionPanel);
            Set(serialized, "reflectionText", reflectionText);
            Set(serialized, "continueButton", continueButton);
            Set(serialized, "encouragementPanel", encouragementPanel);
            Set(serialized, "encouragementText", encouragementText);
            Set(serialized, "validateAndWaitButton", validateButton);
            Set(serialized, "pressureToLeaveButton", pressureButton);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("ResolutionScene Canvas migration completed.");
        }

        private static void AddEncouragementPanel(Scene scene, GameObject canvasObject)
        {
            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var controller = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ResolutionSceneController>(true)).FirstOrDefault();
            if (controller == null) return;

            var panel = CreateEncouragementPanel(canvasObject.transform, out var prompt,
                out var validateButton, out var pressureButton);
            var serialized = new SerializedObject(controller);
            Set(serialized, "encouragementPanel", panel);
            Set(serialized, "encouragementText", prompt);
            Set(serialized, "validateAndWaitButton", validateButton);
            Set(serialized, "pressureToLeaveButton", pressureButton);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Added the outcome encouragement panel to ResolutionScene.");
        }

        private static GameObject CreateEncouragementPanel(
            Transform parent,
            out Text prompt,
            out Button validateButton,
            out Button pressureButton)
        {
            var panel = CreatePanel("EncouragementPanel", parent, PanelColor);
            SetAnchors((RectTransform)panel.transform, new Vector2(0.17f, 0.24f), new Vector2(0.83f, 0.62f));
            prompt = CreateText("EncouragementText", panel.transform,
                "Minh hesitates. How will Lan encourage him?", 32, TextAnchor.MiddleCenter);
            SetAnchors(prompt.rectTransform, new Vector2(0.055f, 0.67f), new Vector2(0.945f, 0.94f));
            validateButton = CreateButton("ValidateAndWaitButton", panel.transform,
                "1  You're safe, Minh. It's Lan. We can take our time.",
                new Vector2(0.055f, 0.38f), new Vector2(0.945f, 0.62f));
            pressureButton = CreateButton("PressureToLeaveButton", panel.transform,
                "2  Come on, Minh. We need to go now.",
                new Vector2(0.055f, 0.09f), new Vector2(0.945f, 0.33f));
            panel.SetActive(false);
            return panel;
        }

        private static void Set(SerializedObject serialized, string name, Object value) =>
            serialized.FindProperty(name).objectReferenceValue = value;

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var result = CreateUiObject(name, parent);
            result.AddComponent<Image>().color = color;
            return result;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment)
        {
            var result = CreateUiObject(name, parent);
            Stretch((RectTransform)result.transform);
            var text = result.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max)
        {
            var result = CreatePanel(name, parent, new Color(0.16f, 0.27f, 0.27f, 0.98f));
            SetAnchors((RectTransform)result.transform, min, max);
            var button = result.AddComponent<Button>();
            button.targetGraphic = result.GetComponent<Image>();
            CreateText("Label", result.transform, label, 24, TextAnchor.MiddleCenter);
            return button;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var result = new GameObject(name, typeof(RectTransform));
            result.layer = 5;
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static EventSystem FindEventSystem(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).FirstOrDefault();

        private static GameObject FindInScene(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == name)?.gameObject;
    }
}
