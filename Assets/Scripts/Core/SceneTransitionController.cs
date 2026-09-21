using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MagesDementiaGame
{
    public sealed class SceneTransitionController : MonoBehaviour
    {
        private const float FadeOutDuration = 0.5f;
        private const float BlackHoldDuration = 0.15f;
        private const float FadeInDuration = 0.6f;
        private const float AudioFadeDuration = 0.35f;

        private CanvasGroup overlay;
        public bool IsRunning { get; private set; }

        private void Awake()
        {
            BuildOverlay();
        }

        public bool TransitionToScene(string sceneName, Action completed)
        {
            if (IsRunning) return false;
            StartCoroutine(SceneTransition(sceneName, completed));
            return true;
        }

        public bool TransitionWithinScene(Action atBlack, Action completed, bool preserveMusic)
        {
            if (IsRunning) return false;
            StartCoroutine(InSceneTransition(atBlack, completed, preserveMusic));
            return true;
        }

        private IEnumerator SceneTransition(string sceneName, Action completed)
        {
            IsRunning = true;
            overlay.blocksRaycasts = true;
            FadeSceneAudio(false);
            yield return Fade(0f, 1f, FadeOutDuration);

            var load = SceneManager.LoadSceneAsync(sceneName);
            if (load == null)
            {
                Debug.LogError($"Could not begin loading scene '{sceneName}'.", this);
                yield return Fade(1f, 0f, FadeInDuration);
                Finish(completed);
                yield break;
            }

            load.allowSceneActivation = false;
            while (load.progress < 0.9f) yield return null;
            yield return new WaitForSecondsRealtime(BlackHoldDuration);
            load.allowSceneActivation = true;
            while (!load.isDone) yield return null;

            yield return Fade(1f, 0f, FadeInDuration);
            Finish(completed);
        }

        private IEnumerator InSceneTransition(Action atBlack, Action completed, bool preserveMusic)
        {
            IsRunning = true;
            overlay.blocksRaycasts = true;
            FadeSceneAudio(preserveMusic);
            yield return Fade(0f, 1f, FadeOutDuration);
            atBlack?.Invoke();
            yield return new WaitForSecondsRealtime(BlackHoldDuration);
            yield return Fade(1f, 0f, FadeInDuration);
            Finish(completed);
        }

        private void FadeSceneAudio(bool preserveMusic)
        {
            var sceneAudio = FindFirstObjectByType<SceneAudioController>();
            sceneAudio?.FadeOut(AudioFadeDuration, preserveMusic);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            overlay.alpha = from;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                overlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            overlay.alpha = to;
        }

        private void Finish(Action completed)
        {
            overlay.blocksRaycasts = false;
            IsRunning = false;
            completed?.Invoke();
        }

        private void BuildOverlay()
        {
            var canvasObject = new GameObject("Scene Transition Overlay", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            overlay = canvasObject.GetComponent<CanvasGroup>();
            overlay.alpha = 0f;
            overlay.blocksRaycasts = false;
            overlay.interactable = false;

            var imageObject = new GameObject("Black", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = imageObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;
        }
    }
}
