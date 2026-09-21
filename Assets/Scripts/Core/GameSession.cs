using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagesDementiaGame
{
    public enum NarrativePhase
    {
        Baseline,
        Intervention,
        Replay,
        Reflection
    }

    public enum EnvironmentChoice
    {
        NotChosen,
        LeaveTelevisionOn,
        LowerTelevision,
        TurnOffTelevision
    }

    public enum ApproachChoice
    {
        NotChosen,
        CallFromDistance,
        ApproachQuickly,
        EnterViewAndIntroduce
    }

    public enum ResponseChoice
    {
        NotChosen,
        CorrectMinh,
        GenericReassurance,
        AcknowledgeAndHelp
    }


    public sealed class GameSession : MonoBehaviour
    {
        public const string MinhViewSceneName = "MinhView";
        public const string LanViewSceneName = "LanView";
        public const string ResolutionSceneName = "ResolutionScene";

        public static GameSession Instance { get; private set; }

        public NarrativePhase Phase { get; private set; } = NarrativePhase.Baseline;
        public EnvironmentChoice SelectedEnvironmentChoice { get; private set; }
        public ApproachChoice SelectedApproachChoice { get; private set; }
        public ResponseChoice SelectedResponseChoice { get; private set; }
        public bool PhotoRestored { get; private set; }
        public string MinhFirstSpokenLine { get; private set; }
        public string MinhSecondSpokenLine { get; private set; }
        public ReplayEffects ReplayEffects { get; private set; }
        public bool IsTransitioning { get; private set; }

        public bool HasAllChoices =>
            SelectedEnvironmentChoice != EnvironmentChoice.NotChosen &&
            SelectedApproachChoice != ApproachChoice.NotChosen &&
            SelectedResponseChoice != ResponseChoice.NotChosen &&
            PhotoRestored;

        public event Action StateChanged;

        // clear static singleton reference
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        // ensure game session exists before a scene is loaded
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        // Unity can enter Play Mode from an unsaved backup or other non-game scene.
        // In that case, start the game from Act 1 instead of showing an empty camera.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePlayableStartingScene()
        {
            var activeSceneName = SceneManager.GetActiveScene().name;
            if (activeSceneName == MinhViewSceneName || activeSceneName == LanViewSceneName ||
                activeSceneName == ResolutionSceneName)
            {
                return;
            }

            SceneManager.LoadScene(MinhViewSceneName);
        }

        public static GameSession EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindFirstObjectByType<GameSession>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            var sessionObject = new GameObject("Game Session");
            return sessionObject.AddComponent<GameSession>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetSession();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        public void BeginIntervention()
        {
            if (IsTransitioning)
            {
                return;
            }

            IsTransitioning = true;
            Phase = NarrativePhase.Intervention;
            NotifyChanged();

            if (SceneManager.GetActiveScene().name != LanViewSceneName)
            {
                SceneManager.LoadScene(LanViewSceneName);
            }
            else
            {
                IsTransitioning = false;
            }
        }

        public void SetEnvironmentChoice(EnvironmentChoice choice)
        {
            SelectedEnvironmentChoice = choice;
            NotifyChanged();
        }

        public void SetApproachChoice(ApproachChoice choice)
        {
            SelectedApproachChoice = choice;
            NotifyChanged();
        }

        public void SetResponseChoice(ResponseChoice choice)
        {
            SelectedResponseChoice = choice;
            NotifyChanged();
        }

        public void SetPhotoRestored(bool restored)
        {
            PhotoRestored = restored;
            NotifyChanged();
        }

        public void SaveMinhSpokenLine(bool firstAttempt, string spokenLine)
        {
            if (firstAttempt) MinhFirstSpokenLine = spokenLine;
            else MinhSecondSpokenLine = spokenLine;
        }

        public bool BeginReplay()
        {
            if (!HasAllChoices || IsTransitioning)
            {
                return false;
            }

            IsTransitioning = true;
            ReplayEffects = OutcomeCalculator.Calculate(
                SelectedEnvironmentChoice,
                SelectedApproachChoice,
                SelectedResponseChoice,
                PhotoRestored);
            Phase = NarrativePhase.Replay;
            NotifyChanged();

            if (SceneManager.GetActiveScene().name != ResolutionSceneName)
            {
                SceneManager.LoadScene(ResolutionSceneName);
            }
            else
            {
                IsTransitioning = false;
            }
            return true;
        }

        public void BeginReflection()
        {
            if (IsTransitioning)
            {
                return;
            }

            Phase = NarrativePhase.Reflection;
            NotifyChanged();
        }

        public void Restart()
        {
            if (IsTransitioning)
            {
                return;
            }

            IsTransitioning = true;
            ResetSession();
            NotifyChanged();
            SceneManager.LoadScene(MinhViewSceneName);
        }

        private void ResetSession()
        {
            Phase = NarrativePhase.Baseline;
            SelectedEnvironmentChoice = EnvironmentChoice.NotChosen;
            SelectedApproachChoice = ApproachChoice.NotChosen;
            SelectedResponseChoice = ResponseChoice.NotChosen;
            PhotoRestored = false;
            MinhFirstSpokenLine = null;
            MinhSecondSpokenLine = null;
            ReplayEffects = OutcomeCalculator.Baseline;
        }

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            IsTransitioning = false;
            if (scene.name == MinhViewSceneName)
            {
                return;
            }

            if (scene.name == ResolutionSceneName)
            {
                ResolutionSceneController.BuildForCurrentScene();
                return;
            }

            if (scene.name == LanViewSceneName)
            {
                if (Phase == NarrativePhase.Baseline)
                {
                    Phase = NarrativePhase.Intervention;
                    NotifyChanged();
                }
                return;
            }

        }
    }
}
