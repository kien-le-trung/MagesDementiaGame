using System.Linq;
using MagesDementiaGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MagesDementiaGameEditor
{
    [InitializeOnLoad]
    public static class LanViewOptionalPhotoSetup
    {
        private const string ScenePath = "Assets/Scenes/LanView.unity";

        static LanViewOptionalPhotoSetup()
        {
            EditorApplication.delayCall += InstallIfMissing;
        }

        [MenuItem("MAGES/Repair LanView Optional Photo UI")]
        public static void RepairFromMenu() => Install(true);

        private static void InstallIfMissing()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) Install(false);
        }

        private static void Install(bool force)
        {
            var openScene = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).FirstOrDefault(scene => scene.path == ScenePath);
            var openedHere = !openScene.IsValid();
            var scene = openedHere ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : openScene;
            if (!scene.IsValid()) return;

            var controller = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<LanViewSceneController>(true)).FirstOrDefault();
            var canvas = FindInScene(scene, "LanViewCanvas");
            if (controller == null || canvas == null)
            {
                Debug.LogError("LanView requires its authored controller and LanViewCanvas.");
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            var buttonObject = FindInScene(scene, "FinishEnvironmentButton");
            if (force && buttonObject != null)
            {
                Object.DestroyImmediate(buttonObject);
                buttonObject = null;
            }

            if (buttonObject == null) buttonObject = CreateButton(canvas.transform);
            var button = buttonObject.GetComponent<Button>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("finishActionsButton").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("LanView optional-photograph UI is configured.");
        }

        private static GameObject CreateButton(Transform parent)
        {
            var root = new GameObject("FinishEnvironmentButton", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            root.layer = 5;
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.73f, 0.025f);
            rect.anchorMax = new Vector2(0.97f, 0.105f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = root.GetComponent<Image>();
            image.color = new Color(0.16f, 0.27f, 0.27f, 0.96f);
            var button = root.GetComponent<Button>();
            button.targetGraphic = image;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.layer = 5;
            labelObject.transform.SetParent(root.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 6f);
            labelRect.offsetMax = new Vector2(-12f, -6f);
            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.96f, 0.94f, 0.88f, 1f);
            label.text = "Continue to Minh";
            label.raycastTarget = false;
            root.SetActive(false);
            return root;
        }

        private static GameObject FindInScene(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == name)?.gameObject;
    }
}
