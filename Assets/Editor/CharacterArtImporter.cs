using System;
using System.Linq;
using MagesDementiaGame;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace MagesDementiaGameEditor
{
    public static class CharacterArtImporter
    {
        private const string Root = "Assets/Art/Sprites/CaregiverRoom/Characters/";
        private const string ArtSetPath = "Assets/Art/Sprites/CaregiverRoom/RoomArtSet.asset";

        [InitializeOnLoadMethod]
        private static void ScheduleSetup()
        {
            EditorApplication.delayCall += SetupIfNeeded;
        }

        [MenuItem("MAGES/Refresh Character Art")]
        public static void RefreshCharacterArt()
        {
            Setup(true);
        }

        private static void SetupIfNeeded()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Setup(false);
            }
        }

        private static void Setup(bool force)
        {
            var artSet = AssetDatabase.LoadAssetAtPath<RoomArtSet>(ArtSetPath);
            if (artSet == null)
            {
                return;
            }

            if (!force && artSet.Lan != null && artSet.Minh != null &&
                artSet.Lan.WalkDown != null && artSet.Minh.WalkDown != null &&
                artSet.Lan.WalkDown.Length == 4 && artSet.Minh.WalkDown.Length == 4 &&
                Mathf.Approximately(artSet.Lan.WorldHeight, 4f) &&
                Mathf.Approximately(artSet.Minh.WorldHeight, 3.8f))
            {
                return;
            }

            artSet.Lan = BuildSet("lan", 4f, force);
            artSet.Minh = BuildSet("minh", 3.8f, force);
            EditorUtility.SetDirty(artSet);
            AssetDatabase.SaveAssets();
        }

        private static CharacterSpriteSet BuildSet(string character, float height, bool force)
        {
            return new CharacterSpriteSet
            {
                Idle = ImportSingle(character + "_stand", true),
                Portrait = ImportSingle(character + "_portrait", false),
                WalkUp = ImportSheet(character + "_backward", force),
                WalkDown = ImportSheet(character + "_forward", force),
                WalkLeft = ImportSheet(character + "_left", force),
                WalkRight = ImportSheet(character + "_right", force),
                WorldHeight = height
            };
        }

        private static Sprite ImportSingle(string fileName, bool bottomPivot)
        {
            var path = Root + fileName + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = bottomPivot ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite[] ImportSheet(string fileName, bool force)
        {
            var path = Root + fileName + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (texture == null || importer == null)
            {
                return Array.Empty<Sprite>();
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            var existing = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().ToArray();
            if (force || existing.Length != 4 || existing.Any(sprite => !sprite.name.StartsWith(fileName + "_frame_")))
            {
                var factories = new SpriteDataProviderFactories();
                factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var rects = new SpriteRect[4];
                for (var index = 0; index < 4; index++)
                {
                    var left = Mathf.RoundToInt(texture.width * index / 4f);
                    var right = Mathf.RoundToInt(texture.width * (index + 1) / 4f);
                    rects[index] = new SpriteRect
                    {
                        name = fileName + "_frame_" + index,
                        rect = new Rect(left, 0f, right - left, texture.height),
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0f),
                        spriteID = GUID.Generate()
                    };
                }
                provider.SetSpriteRects(rects);
                provider.Apply();
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                .OfType<Sprite>()
                .Where(sprite => sprite.name.StartsWith(fileName + "_frame_"))
                .OrderBy(sprite => sprite.name)
                .ToArray();
        }
    }
}
