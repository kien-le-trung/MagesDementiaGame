using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MagesDementiaGame
{
    public sealed class StartSceneController : MonoBehaviour
    {
        [Header("Scene-authored UI")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private GameObject titleScreen;
        [SerializeField] private Button startButton;
        [SerializeField] private RectTransform upperEyelid;
        [SerializeField] private RectTransform lowerEyelid;

        [Header("Timing")]
        [SerializeField] private float closingDuration = 0.22f;
        [SerializeField] private float openingDuration = 1.35f;

        private bool starting;

        private void Awake()
        {
            startButton?.onClick.AddListener(BeginGame);
        }

        private void OnDestroy()
        {
            startButton?.onClick.RemoveListener(BeginGame);
        }

        private void Start()
        {
            if (!ValidateReferences()) return;
            SetEyelidsOpen();
        }

        private void BeginGame()
        {
            if (starting || !ValidateReferences()) return;
            starting = true;
            startButton.interactable = false;
            StartCoroutine(LoadAndOpenEyes());
        }

        private IEnumerator LoadAndOpenEyes()
        {
            yield return AnimateEyelids(false, closingDuration);

            DontDestroyOnLoad(rootCanvas.gameObject);
            var operation = SceneManager.LoadSceneAsync(GameSession.MinhViewSceneName);
            if (operation == null)
            {
                Debug.LogError("StartScene could not load MinhView.", this);
                yield return AnimateEyelids(true, openingDuration);
                starting = false;
                startButton.interactable = true;
                yield break;
            }

            while (!operation.isDone) yield return null;
            titleScreen.SetActive(false);
            yield return null;
            yield return AnimateEyelids(true, openingDuration);
            Destroy(rootCanvas.gameObject);
        }

        private IEnumerator AnimateEyelids(bool opening, float duration)
        {
            var halfHeight = rootCanvas.GetComponent<RectTransform>().rect.height * 0.5f + 40f;
            var upperFrom = opening ? 0f : halfHeight;
            var upperTo = opening ? halfHeight : 0f;
            var lowerFrom = -upperFrom;
            var lowerTo = -upperTo;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                upperEyelid.anchoredPosition = new Vector2(0f, Mathf.Lerp(upperFrom, upperTo, t));
                lowerEyelid.anchoredPosition = new Vector2(0f, Mathf.Lerp(lowerFrom, lowerTo, t));
                yield return null;
            }

            upperEyelid.anchoredPosition = new Vector2(0f, upperTo);
            lowerEyelid.anchoredPosition = new Vector2(0f, lowerTo);
        }

        private void SetEyelidsOpen()
        {
            var halfHeight = rootCanvas.GetComponent<RectTransform>().rect.height * 0.5f + 40f;
            upperEyelid.anchoredPosition = new Vector2(0f, halfHeight);
            lowerEyelid.anchoredPosition = new Vector2(0f, -halfHeight);
        }

        private bool ValidateReferences()
        {
            if (rootCanvas != null && titleScreen != null && startButton != null &&
                upperEyelid != null && lowerEyelid != null) return true;

            Debug.LogError("StartSceneController is missing one or more scene-authored UI references.", this);
            enabled = false;
            return false;
        }
    }
}
