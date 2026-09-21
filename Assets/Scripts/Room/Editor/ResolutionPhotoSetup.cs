#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MagesDementiaGame.Editor
{
    [InitializeOnLoad]
    internal static class ResolutionPhotoSetup
    {
        private const string ImagePath = "Assets/Art/Sprites/CaregiverRoom/Furniture/minhlan_image_side.png";
        private const string RoomPrefabPath = "Assets/Prefabs/Rooms/SharedRoom.prefab";

        static ResolutionPhotoSetup()
        {
            EditorApplication.delayCall += Install;
        }

        [MenuItem("MAGES/Room/Refresh Resolution Photograph")]
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            AssetDatabase.ImportAsset(ImagePath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(ImagePath) is TextureImporter importer)
            {
                var changed = importer.textureType != TextureImporterType.Sprite ||
                              importer.spriteImportMode != SpriteImportMode.Single ||
                              importer.mipmapEnabled || !importer.alphaIsTransparency ||
                              !Mathf.Approximately(importer.spritePixelsPerUnit, 100f);
                if (changed)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100f;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ImagePath);
            if (sprite == null)
            {
                Debug.LogError($"Resolution photograph could not be imported from {ImagePath}.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(RoomPrefabPath);
            try
            {
                var photograph = FindChild(root.transform, "Family Photograph");
                var interaction = FindChild(root.transform, "Photograph Interaction Point");
                if (photograph == null || interaction == null)
                {
                    Debug.LogError("SharedRoom is missing the Family Photograph or Photograph Interaction Point.");
                    return;
                }

                var renderer = photograph.GetComponent<SpriteRenderer>();
                var collider = interaction.GetComponent<CircleCollider2D>();
                var alreadyConfigured = renderer != null && renderer.sprite == sprite &&
                                        Vector3.Distance(photograph.localPosition, new Vector3(5.15f, 0.8f, 0f)) < 0.001f &&
                                        Vector3.Distance(photograph.localScale, new Vector3(0.2f, 0.2f, 1f)) < 0.001f &&
                                        collider != null && Mathf.Approximately(collider.radius, 1f);
                if (alreadyConfigured) return;

                if (renderer == null) renderer = photograph.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;
                renderer.sortingOrder = 3;
                photograph.localPosition = new Vector3(5.15f, 0.8f, 0f);
                photograph.localScale = new Vector3(0.2f, 0.2f, 1f);
                if (collider != null) collider.radius = 1f;

                PrefabUtility.SaveAsPrefabAsset(root, RoomPrefabPath);
                Debug.Log("Resolution photograph artwork and interaction point are configured on the right wall.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform FindChild(Transform parent, string objectName)
        {
            if (parent.name == objectName) return parent;
            for (var index = 0; index < parent.childCount; index++)
            {
                var result = FindChild(parent.GetChild(index), objectName);
                if (result != null) return result;
            }
            return null;
        }
    }
}
#endif
