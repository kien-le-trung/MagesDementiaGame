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
        TurnOffTelevisionAndRestorePhoto
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
        public const string RecipientSceneName = "RecipientScene";
        public const string CaregiverSceneName = "CaregiverScene";

        public static GameSession Instance { get; private set; }

        public NarrativePhase Phase { get; private set; } = NarrativePhase.Baseline;
        public EnvironmentChoice SelectedEnvironmentChoice { get; private set; }
        public ApproachChoice SelectedApproachChoice { get; private set; }
        public ResponseChoice SelectedResponseChoice { get; private set; }
        public ReplayEffects ReplayEffects { get; private set; }

        public bool HasAllChoices =>
            SelectedEnvironmentChoice != EnvironmentChoice.NotChosen &&
            SelectedApproachChoice != ApproachChoice.NotChosen &&
            SelectedResponseChoice != ResponseChoice.NotChosen;

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
            if (activeSceneName == RecipientSceneName || activeSceneName == CaregiverSceneName)
            {
                return;
            }

            SceneManager.LoadScene(RecipientSceneName);
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
            Phase = NarrativePhase.Intervention;
            NotifyChanged();

            if (SceneManager.GetActiveScene().name != CaregiverSceneName)
            {
                SceneManager.LoadScene(CaregiverSceneName);
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

        public bool BeginReplay()
        {
            if (!HasAllChoices)
            {
                return false;
            }

            ReplayEffects = OutcomeCalculator.Calculate(
                SelectedEnvironmentChoice,
                SelectedApproachChoice,
                SelectedResponseChoice);
            Phase = NarrativePhase.Replay;
            NotifyChanged();

            if (SceneManager.GetActiveScene().name != RecipientSceneName)
            {
                SceneManager.LoadScene(RecipientSceneName);
            }
            return true;
        }

        public void BeginReflection()
        {
            Phase = NarrativePhase.Reflection;
            NotifyChanged();
        }

        public void Restart()
        {
            ResetSession();
            NotifyChanged();
            SceneManager.LoadScene(RecipientSceneName);
        }

        private void ResetSession()
        {
            Phase = NarrativePhase.Baseline;
            SelectedEnvironmentChoice = EnvironmentChoice.NotChosen;
            SelectedApproachChoice = ApproachChoice.NotChosen;
            SelectedResponseChoice = ResponseChoice.NotChosen;
            ReplayEffects = OutcomeCalculator.Baseline;
        }

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == RecipientSceneName)
            {
                RecipientSceneController.BuildForCurrentScene();
                return;
            }

            if (scene.name != CaregiverSceneName)
            {
                return;
            }

            // Entering this scene directly is useful while building and testing it.
            if (Phase == NarrativePhase.Baseline)
            {
                Phase = NarrativePhase.Intervention;
                NotifyChanged();
            }

            CaregiverSceneController.BuildForCurrentScene();
        }
    }
}
