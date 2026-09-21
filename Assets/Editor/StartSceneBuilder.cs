using System.IO;
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
    public static class StartSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/StartScene.unity";
        private const string BackgroundPath = "Assets/Art/StartScene/background_blurred.png";
        private static Font font;

        static StartSceneBuilder()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        [MenuItem("MAGES/Rebuild Start Scene")]
        public static void RebuildFromMenu() => Build(true);

        private static void BuildIfMissing()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) Build(!File.Exists(ScenePath));
        }

        private static void Build(bool force)
        {
            if (!force && File.Exists(ScenePath))
            {
                EnsureFirstBuildScene();
                return;
            }

            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var sprite = ImportBackground();
            if (sprite == null)
            {
                Debug.LogError($"StartScene background is missing or could not be imported: {BackgroundPath}");
                return;
            }

            var previousActive = SceneManager.GetActiveScene();
            var alreadyOpen = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).FirstOrDefault(item => item.path == ScenePath);
            var openedHere = !alreadyOpen.IsValid();
            var scene = alreadyOpen.IsValid()
                ? alreadyOpen
                : File.Exists(ScenePath)
                    ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);

            var canvasObject = new GameObject("StartSceneCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(StartSceneController));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var blackBackdrop = CreatePanel("BlackBackdrop", canvasObject.transform, Color.black);
            Stretch((RectTransform)blackBackdrop.transform);

            var titleScreen = CreateUiObject("TitleScreen", canvasObject.transform);
            Stretch((RectTransform)titleScreen.transform);
            var backgroundFrame = CreateUiObject("BackgroundFrame", titleScreen.transform);
            Stretch((RectTransform)backgroundFrame.transform);
            var fitter = backgroundFrame.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            var background = backgroundFrame.AddComponent<Image>();
            background.sprite = sprite;
            background.color = Color.white;
            background.raycastTarget = false;

            var startButton = CreateButton("StartButton", titleScreen.transform, "START",
                new Vector2(0.415f, 0.40f), new Vector2(0.585f, 0.505f));

            var upperEyelid = CreatePanel("UpperEyelid", canvasObject.transform, Color.black).GetComponent<RectTransform>();
            upperEyelid.anchorMin = new Vector2(0f, 0.5f);
            upperEyelid.anchorMax = Vector2.one;
            upperEyelid.offsetMin = Vector2.zero;
            upperEyelid.offsetMax = Vector2.zero;
            upperEyelid.anchoredPosition = new Vector2(0f, 580f);

            var lowerEyelid = CreatePanel("LowerEyelid", canvasObject.transform, Color.black).GetComponent<RectTransform>();
            lowerEyelid.anchorMin = Vector2.zero;
            lowerEyelid.anchorMax = new Vector2(1f, 0.5f);
            lowerEyelid.offsetMin = Vector2.zero;
            lowerEyelid.offsetMax = Vector2.zero;
            lowerEyelid.anchoredPosition = new Vector2(0f, -580f);

            var eventSystem = new GameObject("StartSceneEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);

            var controller = canvasObject.GetComponent<StartSceneController>();
            var serialized = new SerializedObject(controller);
            Set(serialized, "rootCanvas", canvas);
            Set(serialized, "titleScreen", titleScreen);
            Set(serialized, "startButton", startButton);
            Set(serialized, "upperEyelid", upperEyelid);
            Set(serialized, "lowerEyelid", lowerEyelid);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            EnsureFirstBuildScene();
            AssetDatabase.SaveAssets();
            Debug.Log("StartScene created and placed first in Build Settings.");
        }

        private static Sprite ImportBackground()
        {
            AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite ||
                                     importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        }

        private static void EnsureFirstBuildScene()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(item => item.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void Set(SerializedObject serialized, string name, Object value) =>
            serialized.FindProperty(name).objectReferenceValue = value;

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var result = CreateUiObject(name, parent);
            var image = result.AddComponent<Image>();
            image.color = color;
            return result;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max)
        {
            var result = CreatePanel(name, parent, new Color(0.10f, 0.16f, 0.16f, 0.94f));
            SetAnchors((RectTransform)result.transform, min, max);
            var button = result.AddComponent<Button>();
            button.targetGraphic = result.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.25f, 0.42f, 0.39f, 1f);
            colors.pressedColor = new Color(0.08f, 0.12f, 0.12f, 1f);
            button.colors = colors;

            var labelObject = CreateUiObject("Label", result.transform);
            Stretch((RectTransform)labelObject.transform);
            var text = labelObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 36;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            text.raycastTarget = false;
            text.text = label;
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
    }
}
