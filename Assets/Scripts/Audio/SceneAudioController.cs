using System.Collections;
using UnityEngine;

namespace MagesDementiaGame
{
    public sealed class SceneAudioController : MonoBehaviour
    {
        private const float AmbienceVolume = 0.25f;
        private const float ReflectionVolume = 0.30f;
        private const float TelevisionOnVolume = 0.18f;
        private const float TelevisionLoweredVolume = 0.06f;
        private const float UiVolume = 0.55f;
        private const float EffectsVolume = 0.65f;

        [SerializeField] private AudioLibrary library;
        [SerializeField] private AudioSource backgroundSource;
        [SerializeField] private AudioSource televisionSource;
        [SerializeField] private AudioSource oneShotSource;

        private Coroutine fadeRoutine;

        public AudioLibrary Library => library;

        public void PlayRoomAmbience() => PlayLoop(backgroundSource, library?.RoomAmbience, AmbienceVolume);
        public void PlayReflectionMusic() => PlayLoop(backgroundSource, library?.ReflectionTheme, ReflectionVolume);

        public void PlayUiClick() => PlayOneShot(library?.UiClick, UiVolume);
        public void PlayConfirm() => PlayOneShot(library?.UiConfirm, UiVolume);
        public void PlayDialogueBlip() => PlayOneShot(library?.DialogueBlip, UiVolume);
        public void PlayEffect(AudioClip clip) => PlayOneShot(clip, EffectsVolume);

        public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || oneShotSource == null) return;
            oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void SetTelevisionAudio(TelevisionState state, bool playTransitionSound)
        {
            if (library == null) return;
            switch (state)
            {
                case TelevisionState.Off:
                    if (playTransitionSound) PlayEffect(library.TelevisionOff);
                    StopTelevision();
                    break;
                case TelevisionState.Lowered:
                    if (playTransitionSound) PlayEffect(library.TelevisionVolumeDown);
                    PlayLoop(televisionSource, library.TelevisionStaticLoop, TelevisionLoweredVolume);
                    break;
                default:
                    PlayLoop(televisionSource, library.TelevisionStaticLoop, TelevisionOnVolume);
                    break;
            }
        }

        public void PlayTelevisionPowerOn()
        {
            PlayEffect(library?.TelevisionOn);
            SetTelevisionAudio(TelevisionState.On, false);
        }

        public void StopTelevision()
        {
            if (televisionSource != null) televisionSource.Stop();
        }

        public void StopAll()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            backgroundSource?.Stop();
            televisionSource?.Stop();
            oneShotSource?.Stop();
        }

        public void FadeOut(float duration, bool preserveMusic = false)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(Mathf.Max(0.01f, duration), preserveMusic));
        }

        public void SetReflectionVolume(float volume)
        {
            if (backgroundSource != null) backgroundSource.volume = Mathf.Clamp01(volume);
        }

        private static void PlayLoop(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null) return;
            if (source.clip != clip)
            {
                source.Stop();
                source.clip = clip;
            }
            source.loop = true;
            source.volume = volume;
            if (!source.isPlaying) source.Play();
        }

        private IEnumerator FadeRoutine(float duration, bool preserveMusic)
        {
            var backgroundStart = backgroundSource != null ? backgroundSource.volume : 0f;
            var televisionStart = televisionSource != null ? televisionSource.volume : 0f;
            var backgroundTarget = preserveMusic ? 0.12f : 0f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var amount = 1f - progress;
                if (backgroundSource != null)
                    backgroundSource.volume = Mathf.Lerp(backgroundStart, backgroundTarget, progress);
                if (televisionSource != null) televisionSource.volume = televisionStart * amount;
                yield return null;
            }

            televisionSource?.Stop();
            if (backgroundSource != null)
            {
                if (preserveMusic)
                {
                    backgroundSource.volume = backgroundTarget;
                    if (!backgroundSource.isPlaying) backgroundSource.Play();
                }
                else backgroundSource.Stop();
            }
            fadeRoutine = null;
        }

#if UNITY_EDITOR
        public void EditorConfigure(AudioLibrary audioLibrary, AudioSource background,
            AudioSource television, AudioSource oneShot)
        {
            library = audioLibrary;
            backgroundSource = background;
            televisionSource = television;
            oneShotSource = oneShot;
        }
#endif
    }
}
