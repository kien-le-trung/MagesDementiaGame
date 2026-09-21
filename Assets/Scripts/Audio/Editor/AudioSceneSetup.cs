#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagesDementiaGame.Editor
{
    [InitializeOnLoad]
    internal static class AudioSceneSetup
    {
        private const string LibraryPath = "Assets/Audio/AudioLibrary.asset";

        static AudioSceneSetup()
        {
            EditorApplication.delayCall += InstallIfNeeded;
        }

        [MenuItem("MAGES/Audio/Rebuild Scene Audio Setup")]
        private static void Rebuild()
        {
            Install(true);
        }

        private static void InstallIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath) != null) return;
            Install(false);
        }

        private static void Install(bool force)
        {
            if (HasDirtyScenes())
            {
                Debug.LogWarning("Audio setup paused because an open scene has unsaved changes. Save it, then use MAGES > Audio > Rebuild Scene Audio Setup.");
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                PopulateLibrary(library);
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            else if (force)
            {
                PopulateLibrary(library);
                EditorUtility.SetDirty(library);
            }

            ConfigureStreaming("Assets/Audio/Ambience/room_ambience.mp3");
            ConfigureStreaming("Assets/Audio/Music/reflection_theme.mp3");
            ConfigureStreaming("Assets/Audio/SFX/Environment/television_static_loop.mp3");

            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                ConfigureScene<MinhViewSceneController>("Assets/Scenes/MinhView.unity", library);
                ConfigureScene<LanViewSceneController>("Assets/Scenes/LanView.unity", library);
                ConfigureScene<ResolutionSceneController>("Assets/Scenes/ResolutionScene.unity", library);
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Audio library and all three active scenes are wired.");
        }

        private static bool HasDirtyScenes()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index).isDirty) return true;
            return false;
        }

        private static void PopulateLibrary(AudioLibrary library)
        {
            library.RoomAmbience = Clip("Assets/Audio/Ambience/room_ambience.mp3");
            library.ReflectionTheme = Clip("Assets/Audio/Music/reflection_theme.mp3");
            library.UiClick = Clip("Assets/Audio/SFX/UI/ui_click.ogg");
            library.UiConfirm = Clip("Assets/Audio/SFX/UI/ui_confirm.ogg");
            library.DialogueBlip = Clip("Assets/Audio/SFX/UI/dialogue_blip.ogg");
            library.ItemPickup = Clip("Assets/Audio/SFX/Interaction/item_pickup.ogg");
            library.ItemPlace = Clip("Assets/Audio/SFX/Interaction/item_place.ogg");
            library.InvalidDrop = Clip("Assets/Audio/SFX/Interaction/invalid_drop.ogg");
            library.ChecklistTick = Clip("Assets/Audio/SFX/Interaction/checklist_tick.ogg");
            library.TelevisionOn = Clip("Assets/Audio/SFX/Environment/television_on.mp3");
            library.TelevisionOff = Clip("Assets/Audio/SFX/Environment/television_off.mp3");
            library.TelevisionVolumeDown = Clip("Assets/Audio/SFX/Environment/television_volume_down.ogg");
            library.TelevisionStaticLoop = Clip("Assets/Audio/SFX/Environment/television_static_loop.mp3");
            library.DoorOpening = Clip("Assets/Audio/SFX/Environment/door_opening.mp3");
        }

        private static AudioClip Clip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogWarning($"Audio clip could not be loaded at {path}.");
            return clip;
        }

        private static void ConfigureStreaming(string path)
        {
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer) return;
            var settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.Streaming) return;
            settings.loadType = AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static void ConfigureScene<T>(string path, AudioLibrary library) where T : MonoBehaviour
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var owner = FindInScene<T>(scene);
            if (owner == null)
            {
                Debug.LogError($"Could not find {typeof(T).Name} in {path}; audio was not installed in that scene.");
                return;
            }

            var audio = owner.GetComponent<SceneAudioController>();
            if (audio == null) audio = owner.gameObject.AddComponent<SceneAudioController>();
            var sources = owner.GetComponents<AudioSource>();
            while (sources.Length < 3)
            {
                owner.gameObject.AddComponent<AudioSource>();
                sources = owner.GetComponents<AudioSource>();
            }

            foreach (var source in sources)
            {
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
            }
            audio.EditorConfigure(library, sources[0], sources[1], sources[2]);
            EditorUtility.SetDirty(audio);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var result = root.GetComponentInChildren<T>(true);
                if (result != null) return result;
            }
            return null;
        }
    }
}
#endif
