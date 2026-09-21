using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MagesDementiaGame
{
    public sealed class LanViewSceneController : MonoBehaviour
    {
        private enum State
        {
            Entered,
            Observing,
            Acting,
            Conversing
        }

        private enum ObservationHotspot
        {
            Minh,
            Table,
            Television,
            Wall
        }

        private enum ActionHotspot
        {
            Television,
            Wall,
            Cabinet
        }

        private enum CommunicationStage
        {
            None,
            ChoosingApproach,
            SubmittingApproach,
            LanSpeaking,
            MinhResponding,
            ChoosingResponse,
            LanResponding,
            Complete
        }

        private readonly bool[] observationsCompleted = new bool[4];
        private readonly bool[] actionsCompleted = new bool[3];
        private State currentState = State.Entered;
        private CommunicationStage communicationStage = CommunicationStage.None;

        [Header("Scene UI")]
        [SerializeField] private GameObject observationHotspots;
        [SerializeField] private GameObject actionHotspots;
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private Text promptText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject televisionChoicePanel;
        [SerializeField] private Button[] televisionChoiceButtons;
        [SerializeField] private Text choiceTitleText;
        [SerializeField] private Text[] choiceButtonTexts;
        [SerializeField] private GameObject cabinetSearchArea;
        [SerializeField] private GameObject wallPhotograph;

        [Header("Natural-language approach")]
        [Tooltip("Local development: http://127.0.0.1:8787/evaluate-approach. Replace with the deployed workers.dev URL before building.")]
        [SerializeField] private string approachJudgeUrl = "http://127.0.0.1:8787/evaluate-approach";
        [SerializeField] private GameObject approachEntryPanel;
        [SerializeField] private InputField approachInput;
        [SerializeField] private Button submitApproachButton;
        [SerializeField] private Button fallbackApproachButton;
        [SerializeField] private Text approachStatusText;

        [Header("Hotspots (indexed by enums)")]
        [SerializeField] private Button[] observationButtons;
        [SerializeField] private Button[] actionButtons;

        private bool advanceAfterPrompt;
        private GameSession session;
        private SceneAudioController audioController;
        private string[] enteredReplayLines;
        private int enteredReplayIndex;

        private void Awake()
        {
            session = GameSession.EnsureInstance();
            audioController = GetComponent<SceneAudioController>();
            BindButtons();
            continueButton?.onClick.AddListener(ClosePrompt);
            submitApproachButton?.onClick.AddListener(SubmitNaturalLanguageApproach);
            fallbackApproachButton?.onClick.AddListener(() => OpenFallbackChoices(null));
        }

        private void Start()
        {
            audioController?.PlayRoomAmbience();
            audioController?.PlayEffect(audioController.Library?.DoorOpening);
            currentState = State.Entered;
            communicationStage = CommunicationStage.None;
            enteredReplayLines = new[]
            {
                "WOMAN: \"Dad, lunch is ready. You need to come with me.\"",
                $"MINH: \"{FallbackSpeech(session.MinhFirstSpokenLine, "I... hospital... patients... waiting.")}\"",
                "WOMAN: \"Dad, I'm sorry. I didn't understand. Take your time. Can you try again?\"",
                $"MINH: \"{FallbackSpeech(session.MinhSecondSpokenLine, "The patients... need me. I have to go.")}\"",
                "WOMAN: \"You're worried about your patients. I hear you. We can talk about them over lunch.\""
            };
            enteredReplayIndex = 0;
            ApplyState();
            ShowPrompt(enteredReplayLines[enteredReplayIndex], false);
        }

        private void BindButtons()
        {
            if (observationButtons != null)
            {
                for (var index = 0; index < observationButtons.Length; index++)
                {
                    var capturedIndex = index;
                    observationButtons[index]?.onClick.AddListener(
                        () => Observe((ObservationHotspot)capturedIndex));
                }
            }

            if (actionButtons != null)
            {
                for (var index = 0; index < actionButtons.Length; index++)
                {
                    var capturedIndex = index;
                    actionButtons[index]?.onClick.AddListener(
                        () => Act((ActionHotspot)capturedIndex));
                }
            }

            if (televisionChoiceButtons == null) return;
            for (var index = 0; index < televisionChoiceButtons.Length; index++)
            {
                var capturedIndex = index;
                televisionChoiceButtons[index]?.onClick.AddListener(() => SelectChoiceOption(capturedIndex));
            }
        }

        private void Observe(ObservationHotspot hotspot)
        {
            if (currentState != State.Observing) return;
            audioController?.PlayUiClick();
            observationsCompleted[(int)hotspot] = true;
            if ((int)hotspot < observationButtons.Length)
                observationButtons[(int)hotspot].gameObject.SetActive(false);
            var text = hotspot switch
            {
                ObservationHotspot.Minh => "Minh is seated on the sofa. He seems focused on something you cannot see.",
                ObservationHotspot.Table => "Water, medicine, notes, and a cloth are spread across the table.",
                ObservationHotspot.Television => "The television and clock compete for attention at the edge of the room.",
                _ => "The wall feels bare. Something familiar may be missing."
            };
            ApplyState();
            ShowPrompt(text, AllComplete(observationsCompleted));
        }

        private void Act(ActionHotspot hotspot)
        {
            if (currentState != State.Acting) return;
            if (hotspot != ActionHotspot.Cabinet) audioController?.PlayUiClick();
            if (hotspot == ActionHotspot.Television)
            {
                ConfigureTelevisionChoices();
                televisionChoicePanel?.SetActive(true);
                promptPanel?.SetActive(false);
                return;
            }

            if (hotspot == ActionHotspot.Cabinet && !actionsCompleted[(int)ActionHotspot.Wall]) return;
            actionsCompleted[(int)hotspot] = true;
            if ((int)hotspot < actionButtons.Length)
                actionButtons[(int)hotspot].gameObject.SetActive(false);
            var text = hotspot switch
            {
                ActionHotspot.Wall => "The wall is empty. The family photograph should be hanging here. It must be somewhere in the room.",
                _ => "The photograph was underneath the cabinet. You hang it back in its familiar place."
            };
            if (hotspot == ActionHotspot.Cabinet)
            {
                session.SetPhotoRestored(true);
                audioController?.PlayConfirm();
            }
            ApplyState();
            ShowPrompt(text, AllComplete(actionsCompleted));
        }

        private void SelectChoiceOption(int index)
        {
            if (currentState == State.Conversing)
            {
                SelectApproachOption(index);
                return;
            }

            SelectTelevisionOption(index);
        }

        private void SelectTelevisionOption(int index)
        {
            if (currentState != State.Acting || index < 0 || index > 2) return;
            var choice = index switch
            {
                0 => EnvironmentChoice.LeaveTelevisionOn,
                1 => EnvironmentChoice.LowerTelevision,
                _ => EnvironmentChoice.TurnOffTelevision
            };
            session.SetEnvironmentChoice(choice);
            actionsCompleted[(int)ActionHotspot.Television] = true;
            if (actionButtons.Length > (int)ActionHotspot.Television)
                actionButtons[(int)ActionHotspot.Television].gameObject.SetActive(false);
            televisionChoicePanel?.SetActive(false);
            switch (choice)
            {
                case EnvironmentChoice.LowerTelevision:
                    audioController?.PlayEffect(audioController.Library?.TelevisionVolumeDown);
                    break;
                case EnvironmentChoice.TurnOffTelevision:
                    audioController?.PlayEffect(audioController.Library?.TelevisionOff);
                    audioController?.StopTelevision();
                    break;
                default:
                    audioController?.PlayConfirm();
                    break;
            }
            ApplyState();
            var text = choice switch
            {
                EnvironmentChoice.LeaveTelevisionOn => "You leave the television at its current volume.",
                EnvironmentChoice.LowerTelevision => "You lower the television so voices in the room are easier to hear.",
                _ => "You turn the television off, removing the competing voices."
            };
            ShowPrompt(text, AllComplete(actionsCompleted));
        }

        private void ConfigureTelevisionChoices()
        {
            if (choiceTitleText != null) choiceTitleText.text = "What will you do with the television?";
            SetChoiceText(0, "Leave the television on");
            SetChoiceText(1, "Lower the television volume");
            SetChoiceText(2, "Turn the television off");
        }

        private void OpenApproachEntry()
        {
            communicationStage = CommunicationStage.ChoosingApproach;
            televisionChoicePanel?.SetActive(false);
            approachEntryPanel?.SetActive(true);
            if (approachInput != null)
            {
                approachInput.text = string.Empty;
                approachInput.interactable = true;
                approachInput.ActivateInputField();
            }
            if (submitApproachButton != null) submitApproachButton.interactable = true;
            if (fallbackApproachButton != null) fallbackApproachButton.interactable = true;
            if (approachStatusText != null)
                approachStatusText.text = "What would you like to say to Minh?";
        }

        private void OpenFallbackChoices(string reason)
        {
            if (communicationStage != CommunicationStage.ChoosingApproach &&
                communicationStage != CommunicationStage.SubmittingApproach) return;

            communicationStage = CommunicationStage.ChoosingApproach;
            approachEntryPanel?.SetActive(false);
            if (choiceTitleText != null)
                choiceTitleText.text = string.IsNullOrWhiteSpace(reason)
                    ? "How will you approach Minh?"
                    : "AI unavailable — choose the closest approach";
            SetChoiceText(0, "Call to him from across the room");
            SetChoiceText(1, "Walk over quickly so lunch is not delayed");
            SetChoiceText(2, "Enter his view, pause, and introduce yourself");
            televisionChoicePanel?.SetActive(true);
            if (!string.IsNullOrWhiteSpace(reason))
                Debug.LogWarning($"AI approach judging unavailable; using deterministic choices. {reason}", this);
        }

        private void SubmitNaturalLanguageApproach()
        {
            if (communicationStage != CommunicationStage.ChoosingApproach || approachInput == null) return;
            var playerText = approachInput.text.Trim();
            if (playerText.Length < 2)
            {
                if (approachStatusText != null) approachStatusText.text = "Please enter what Lan wants to say.";
                approachInput.ActivateInputField();
                return;
            }
            if (playerText.Length > 600)
            {
                if (approachStatusText != null) approachStatusText.text = "Please keep the response under 600 characters.";
                return;
            }

            communicationStage = CommunicationStage.SubmittingApproach;
            approachInput.interactable = false;
            if (submitApproachButton != null) submitApproachButton.interactable = false;
            if (fallbackApproachButton != null) fallbackApproachButton.interactable = false;
            if (approachStatusText != null) approachStatusText.text = "Considering your approach...";
            StartCoroutine(ApproachJudgeClient.Evaluate(
                approachJudgeUrl,
                playerText,
                result => CompleteAiApproach(playerText, result),
                error => OpenFallbackChoices(error)));
        }

        private void CompleteAiApproach(string playerText, ApproachJudgeResult result)
        {
            if (communicationStage != CommunicationStage.SubmittingApproach) return;
            session.SaveApproachEvaluation(playerText, result);
            CompleteApproach(ApproachChoice.NaturalLanguage, result.feedback);
        }

        private void SelectApproachOption(int index)
        {
            if (communicationStage != CommunicationStage.ChoosingApproach || index < 0 || index > 2) return;

            var choice = index switch
            {
                0 => ApproachChoice.CallFromDistance,
                1 => ApproachChoice.ApproachQuickly,
                _ => ApproachChoice.EnterViewAndIntroduce
            };
            session.ClearApproachEvaluation();
            CompleteApproach(choice, "This preset approach was evaluated using the authored game rules.");
        }

        private void CompleteApproach(ApproachChoice choice, string feedback)
        {
            session.SetApproachChoice(choice);
            session.SetResponseChoice(ResponseChoice.GenericReassurance);
            audioController?.PlayConfirm();
            communicationStage = CommunicationStage.Complete;
            approachEntryPanel?.SetActive(false);
            televisionChoicePanel?.SetActive(false);
            if (continueButton != null) continueButton.interactable = true;
            ApplyState();
            ShowPrompt(feedback + "\n\nContinue to see how the interaction resolves.", true);
        }

        private void SetChoiceText(int index, string text)
        {
            if (choiceButtonTexts != null && index >= 0 && index < choiceButtonTexts.Length &&
                choiceButtonTexts[index] != null)
            {
                choiceButtonTexts[index].text = text;
            }
        }

        private void ShowPrompt(string text, bool advanceWhenClosed)
        {
            advanceAfterPrompt = advanceWhenClosed;
            if (promptText != null) promptText.text = text;
            promptPanel?.SetActive(true);
            audioController?.PlayDialogueBlip();
        }

        private void ClosePrompt()
        {
            audioController?.PlayUiClick();
            promptPanel?.SetActive(false);
            if (currentState == State.Entered)
            {
                enteredReplayIndex++;
                if (enteredReplayIndex < enteredReplayLines.Length)
                {
                    ApplyState();
                    ShowPrompt(enteredReplayLines[enteredReplayIndex], false);
                }
                else
                {
                    currentState = State.Observing;
                    ApplyState();
                }
                return;
            }
            if (!advanceAfterPrompt) return;
            advanceAfterPrompt = false;
            if (currentState == State.Entered) currentState = State.Observing;
            else if (currentState == State.Observing) currentState = State.Acting;
            else if (currentState == State.Acting)
            {
                currentState = State.Conversing;
                ApplyState();
                OpenApproachEntry();
                return;
            }
            else if (currentState == State.Conversing)
            {
                if (!session.BeginReplay())
                    Debug.LogError("LanView could not transition to ResolutionScene because a required decision is missing.", this);
                return;
            }
            ApplyState();
        }

        private void ApplyState()
        {
            observationHotspots?.SetActive(currentState == State.Observing);
            actionHotspots?.SetActive(currentState == State.Acting);
            cabinetSearchArea?.SetActive(currentState == State.Acting &&
                                         actionsCompleted[(int)ActionHotspot.Wall] &&
                                         !actionsCompleted[(int)ActionHotspot.Cabinet]);
            wallPhotograph?.SetActive(actionsCompleted[(int)ActionHotspot.Cabinet]);
            if (currentState != State.Acting && currentState != State.Conversing)
                televisionChoicePanel?.SetActive(false);
            if (currentState != State.Conversing)
                approachEntryPanel?.SetActive(false);
            if (objectiveText == null) return;
            objectiveText.text = currentState switch
            {
                State.Entered => $"Replay the conversation ({enteredReplayIndex + 1}/{enteredReplayLines?.Length ?? 5})",
                State.Observing => $"Observe Minh and the room ({CompletedCount(observationsCompleted)}/{observationsCompleted.Length})",
                State.Acting when actionsCompleted[(int)ActionHotspot.Wall] && !actionsCompleted[(int)ActionHotspot.Cabinet] =>
                    "Find the missing photograph.",
                State.Acting => $"Prepare the environment ({CompletedCount(actionsCompleted)}/{actionsCompleted.Length})",
                State.Conversing when communicationStage == CommunicationStage.Complete => "Continue to the resolution",
                State.Conversing when communicationStage == CommunicationStage.SubmittingApproach => "Listen and consider your approach",
                _ => "Choose how to approach Minh"
            };
        }

        private static string FallbackSpeech(string saved, string fallback) =>
            string.IsNullOrWhiteSpace(saved) ? fallback : saved;

        private static bool AllComplete(bool[] values)
        {
            foreach (var value in values)
                if (!value) return false;
            return true;
        }

        private static int CompletedCount(bool[] values)
        {
            var count = 0;
            foreach (var value in values)
                if (value) count++;
            return count;
        }
    }
}
