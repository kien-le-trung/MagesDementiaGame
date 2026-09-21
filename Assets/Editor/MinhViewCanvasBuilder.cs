using System;
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
    public static class MinhViewCanvasBuilder
    {
        private const string ScenePath = "Assets/Scenes/MinhView.unity";
        private const string ArtRoot = "Assets/Art/MinhView/";
        private static readonly Color PanelColor = new Color(0.04f, 0.07f, 0.08f, 0.92f);
        private static readonly Color TextColor = new Color(0.96f, 0.94f, 0.88f, 1f);
        private static Font font;

        [MenuItem("MAGES/Rebuild MinhView Canvas")]
        public static void RebuildFromMenu() => Build(true);

        private static void Build(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var existingScene = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).FirstOrDefault(scene => scene.path == ScenePath);
            var openedHere = !existingScene.IsValid();
            var scene = openedHere ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : existingScene;
            if (!scene.IsValid()) return;
            if (!force && FindInScene(scene, "MinhViewCanvas") != null)
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            if (force)
            {
                var previous = FindInScene(scene, "MinhViewCanvas");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
                var previousEvents = FindInScene(scene, "MinhViewEventSystem");
                if (previousEvents != null) UnityEngine.Object.DestroyImmediate(previousEvents);
            }

            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var controller = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MinhViewSceneController>(true)).FirstOrDefault();
            if (controller == null)
            {
                Debug.LogError("MinhView scene is missing MinhViewSceneController.");
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            var canvasObject = new GameObject("MinhViewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var frame = CreateUIObject("BackgroundFrame", canvasObject.transform);
            Stretch(frame);
            var fitter = frame.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var backgroundSprite = LoadSprite("background_minhview.png");
            fitter.aspectRatio = backgroundSprite != null ? backgroundSprite.rect.width / backgroundSprite.rect.height : 16f / 9f;

            var room = CreateGroup("RoomView", frame.transform, true);
            CreateImage("Background", room.transform, backgroundSprite, Color.white, false);
            var tvOff = CreateImage("TelevisionOff", room.transform, LoadSprite("tv_stand_minhview.png"), Color.white, false);
            SetAnchors(tvOff.rectTransform, new Vector2(0.30f, 0.29f), new Vector2(0.70f, 0.70f));
            var tvOn = CreateImage("TelevisionOn", room.transform, LoadSprite("tv_stand_on_minhview.png"), Color.white, false);
            SetAnchors(tvOn.rectTransform, new Vector2(0.30f, 0.29f), new Vector2(0.70f, 0.70f));
            tvOn.gameObject.SetActive(false);
            var woman = CreateImage("BlurredLan", room.transform, LoadSprite("lan_blurred_minhview.png"), Color.white, false);
            SetAnchors(woman.rectTransform, new Vector2(0.075f, 0.12f), new Vector2(0.32f, 0.80f));
            woman.preserveAspect = true;
            woman.gameObject.SetActive(false);

            var roomHotspots = CreateGroup("RoomHotspots", room.transform, true);
            var roomButtons = new[]
            {
                CreateHotspot("LeftWallHotspot", roomHotspots.transform, new Vector2(0.015f, 0.37f), new Vector2(0.285f, 0.90f)),
                CreateHotspot("TelevisionHotspot", roomHotspots.transform, new Vector2(0.29f, 0.38f), new Vector2(0.71f, 0.72f)),
                CreateHotspot("TableHotspot", roomHotspots.transform, new Vector2(0.30f, 0.11f), new Vector2(0.70f, 0.36f))
            };

            var door = CreateGroup("DoorCloseupView", frame.transform, false);
            CreateImage("DoorCloseup", door.transform, LoadSprite("door_closeup_minhview.png"), Color.white, false);
            var doorButtons = new[]
            {
                CreateHotspot("FishTankHotspot", door.transform, new Vector2(0.20f, 0.31f), new Vector2(0.38f, 0.55f)),
                CreateHotspot("PhotographHotspot", door.transform, new Vector2(0.16f, 0.62f), new Vector2(0.34f, 0.96f)),
                CreateHotspot("DoorHotspot", door.transform, new Vector2(0.44f, 0.08f), new Vector2(0.74f, 0.98f))
            };
            var doorBack = CreateButton("DoorBackButton", door.transform, "Back  [Esc]", new Vector2(0.84f, 0.91f), new Vector2(0.98f, 0.98f));

            var table = CreateGroup("TableCloseupView", frame.transform, false);
            CreateImage("WoodenSurface", table.transform, LoadSprite("wooden_surface_minhview.png"), Color.white, false);
            var items = new UIDraggableItem[4];
            var targets = new UIDropTarget[4];
            var toggles = new Toggle[4];
            var itemNames = new[] { "Water", "Clean cloth", "Notes", "Medicine" };
            var itemFiles = new[] { "water_glass_minhview.png", "table_cloth_minhview.png", "notebook_minhview.png", "medicine_minhview.png" };
            for (var index = 0; index < 4; index++)
            {
                var row = index / 2;
                var column = index % 2;
                var targetObject = CreatePanel("Target_" + itemNames[index], table.transform, new Color(0.2f, 0.12f, 0.05f, 0.38f));
                SetAnchors((RectTransform)targetObject.transform,
                    new Vector2(0.35f + column * 0.22f, 0.48f - row * 0.30f),
                    new Vector2(0.52f + column * 0.22f, 0.70f - row * 0.30f));
                targets[index] = targetObject.AddComponent<UIDropTarget>();
                CreateText("TargetLabel", targetObject.transform, itemNames[index], 20, TextAnchor.MiddleCenter);

                var itemImage = CreateImage("Item_" + itemNames[index], table.transform, LoadSprite(itemFiles[index]), Color.white, true);
                var itemRect = itemImage.rectTransform;
                SetAnchors(itemRect,
                    new Vector2(0.035f + column * 0.115f, 0.58f - row * 0.34f),
                    new Vector2(0.14f + column * 0.115f, 0.78f - row * 0.34f));
                itemImage.preserveAspect = true;
                itemImage.gameObject.AddComponent<CanvasGroup>();
                items[index] = itemImage.gameObject.AddComponent<UIDraggableItem>();

                toggles[index] = CreateToggle("Checklist_" + itemNames[index], table.transform, itemNames[index],
                    new Vector2(0.79f, 0.70f - index * 0.09f), new Vector2(0.98f, 0.77f - index * 0.09f));
            }
            var tableBack = CreateButton("TableBackButton", table.transform, "Back  [Esc]", new Vector2(0.02f, 0.91f), new Vector2(0.16f, 0.98f));
            var finishTable = CreateButton("FinishTableButton", table.transform, "Finish task", new Vector2(0.79f, 0.12f), new Vector2(0.98f, 0.20f));

            var objectivePanel = CreatePanel("ObjectivePanel", canvasObject.transform, new Color(0.05f, 0.08f, 0.09f, 0.84f));
            SetAnchors((RectTransform)objectivePanel.transform, new Vector2(0.02f, 0.91f), new Vector2(0.56f, 0.98f));
            var objective = CreateText("ObjectiveText", objectivePanel.transform, "ACT 1", 26, TextAnchor.MiddleLeft);

            var narrationPanel = CreatePanel("NarrationPanel", canvasObject.transform, PanelColor);
            SetAnchors((RectTransform)narrationPanel.transform, new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.23f));
            var narration = CreateText("NarrationText", narrationPanel.transform, string.Empty, 27, TextAnchor.MiddleLeft);
            SetAnchors(narration.rectTransform, new Vector2(0.03f, 0.28f), new Vector2(0.72f, 0.90f));
            var continueButton = CreateButton("ContinueButton", narrationPanel.transform, "Continue  [Enter / Space]", new Vector2(0.74f, 0.20f), new Vector2(0.97f, 0.76f));
            var continueText = continueButton.GetComponentInChildren<Text>();

            var speechPanel = CreatePanel("SpeechEntryPanel", canvasObject.transform, PanelColor);
            SetAnchors((RectTransform)speechPanel.transform, new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.27f));
            var speechLabel = CreateText("SpeechPrompt", speechPanel.transform, "What do you want to tell her?", 25, TextAnchor.MiddleLeft);
            SetAnchors(speechLabel.rectTransform, new Vector2(0.03f, 0.72f), new Vector2(0.97f, 0.94f));
            var input = CreateInputField(speechPanel.transform);
            var suggested = CreateButton("SuggestedResponseButton", speechPanel.transform, "Use suggested response", new Vector2(0.03f, 0.10f), new Vector2(0.32f, 0.30f));
            var speak = CreateButton("SpeakButton", speechPanel.transform, "Speak  [Enter]", new Vector2(0.76f, 0.10f), new Vector2(0.97f, 0.30f));
            speechPanel.SetActive(false);

            var fade = CreatePanel("FadeOverlay", canvasObject.transform, Color.black);
            Stretch(fade);
            var fadeGroup = fade.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fade.transform.SetAsLastSibling();

            CreateEventSystem(scene);
            AssignController(controller, canvas, room, door, table, tvOff.gameObject, tvOn.gameObject, woman.gameObject,
                roomButtons, doorButtons, doorBack, objective, narrationPanel, narration, continueText, continueButton,
                fadeGroup, speechPanel, input, suggested, speak, items, targets, toggles, tableBack, finishTable);

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("MinhView Canvas migration completed.");
        }

        private static void AssignController(MinhViewSceneController controller, Canvas canvas, GameObject room, GameObject door,
            GameObject table, GameObject tvOff, GameObject tvOn, GameObject woman, Button[] roomButtons, Button[] doorButtons,
            Button doorBack, Text objective, GameObject narrationPanel, Text narration, Text continueText, Button continueButton,
            CanvasGroup fade, GameObject speechPanel, InputField input, Button suggested, Button speak,
            UIDraggableItem[] items, UIDropTarget[] targets, Toggle[] toggles, Button tableBack, Button finishTable)
        {
            var serialized = new SerializedObject(controller);
            Set(serialized, "rootCanvas", canvas); Set(serialized, "roomView", room); Set(serialized, "doorCloseupView", door);
            Set(serialized, "tableCloseupView", table); Set(serialized, "televisionOff", tvOff); Set(serialized, "televisionOn", tvOn);
            Set(serialized, "blurredLan", woman); SetArray(serialized, "roomHotspotButtons", roomButtons);
            SetArray(serialized, "doorHotspotButtons", doorButtons); Set(serialized, "doorBackButton", doorBack);
            Set(serialized, "objectiveText", objective); Set(serialized, "narrationPanel", narrationPanel);
            Set(serialized, "narrationText", narration); Set(serialized, "continueButtonText", continueText);
            Set(serialized, "continueButton", continueButton); Set(serialized, "fadeOverlay", fade);
            Set(serialized, "speechEntryPanel", speechPanel); Set(serialized, "speechInput", input);
            Set(serialized, "suggestedResponseButton", suggested); Set(serialized, "speakButton", speak);
            SetArray(serialized, "tableItems", items); SetArray(serialized, "tableTargets", targets);
            SetArray(serialized, "tableChecklist", toggles); Set(serialized, "tableBackButton", tableBack);
            Set(serialized, "finishTableButton", finishTable); serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(SerializedObject serialized, string name, UnityEngine.Object value) =>
            serialized.FindProperty(name).objectReferenceValue = value;

        private static void SetArray<T>(SerializedObject serialized, string name, T[] values) where T : UnityEngine.Object
        {
            var property = serialized.FindProperty(name); property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static GameObject CreateGroup(string name, Transform parent, bool active)
        {
            var result = CreateUIObject(name, parent); Stretch(result); result.SetActive(active); return result;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            var go = CreateUIObject(name, parent); Stretch(go); var image = go.AddComponent<Image>();
            image.sprite = sprite; image.color = color; image.raycastTarget = raycast; return image;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = CreateUIObject(name, parent); var image = go.AddComponent<Image>(); image.color = color; return go;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            var go = CreateUIObject(name, parent); Stretch(go); var text = go.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.color = TextColor; text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false; text.text = value; return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max)
        {
            var go = CreatePanel(name, parent, new Color(0.16f, 0.27f, 0.27f, 0.96f)); SetAnchors((RectTransform)go.transform, min, max);
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            CreateText("Label", go.transform, label, 22, TextAnchor.MiddleCenter); return button;
        }

        private static Button CreateHotspot(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = CreatePanel(name, parent, Color.clear); SetAnchors((RectTransform)go.transform, min, max);
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            var marker = CreatePanel("OrangeMarker", go.transform, new Color(1f, 0.55f, 0.12f, 0.82f));
            var rect = (RectTransform)marker.transform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(18f, 18f); rect.anchoredPosition = Vector2.zero; rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            marker.GetComponent<Image>().raycastTarget = false; return button;
        }

        private static Toggle CreateToggle(string name, Transform parent, string label, Vector2 min, Vector2 max)
        {
            var root = CreateUIObject(name, parent); SetAnchors((RectTransform)root.transform, min, max);
            var toggle = root.AddComponent<Toggle>();
            var background = CreatePanel("Background", root.transform, new Color(0.15f, 0.15f, 0.13f, 1f));
            var bgRect = (RectTransform)background.transform; bgRect.anchorMin = new Vector2(0f, 0.2f); bgRect.anchorMax = new Vector2(0.18f, 0.8f); bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            var checkmark = CreatePanel("Checkmark", background.transform, new Color(1f, 0.55f, 0.12f, 1f)); Stretch(checkmark);
            var text = CreateText("Label", root.transform, label, 19, TextAnchor.MiddleLeft); SetAnchors(text.rectTransform, new Vector2(0.22f, 0f), Vector2.one);
            toggle.targetGraphic = background.GetComponent<Image>(); toggle.graphic = checkmark.GetComponent<Image>(); return toggle;
        }

        private static InputField CreateInputField(Transform parent)
        {
            var root = CreatePanel("SpeechInput", parent, new Color(0.12f, 0.14f, 0.14f, 1f));
            SetAnchors((RectTransform)root.transform, new Vector2(0.03f, 0.38f), new Vector2(0.97f, 0.66f));
            var text = CreateText("Text", root.transform, string.Empty, 24, TextAnchor.MiddleLeft);
            SetAnchors(text.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.92f));
            var placeholder = CreateText("Placeholder", root.transform, "Type what Minh wants to say...", 22, TextAnchor.MiddleLeft);
            placeholder.color = new Color(0.7f, 0.7f, 0.68f, 0.65f); SetAnchors(placeholder.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.92f));
            var input = root.AddComponent<InputField>(); input.targetGraphic = root.GetComponent<Image>(); input.textComponent = text; input.placeholder = placeholder;
            input.characterLimit = 120; return input;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false); return go;
        }

        private static void Stretch(GameObject go)
        {
            var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string fileName)
        {
            var path = ArtRoot + fileName; var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void CreateEventSystem(Scene scene)
        {
            var go = new GameObject("MinhViewEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        private static GameObject FindInScene(Scene scene, string name) =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name)?.gameObject;
    }
}
