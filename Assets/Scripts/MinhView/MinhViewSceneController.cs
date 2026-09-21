using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MagesDementiaGame
{
    public sealed class MinhViewSceneController : MonoBehaviour
    {
        private enum Hotspot { LeftWall, Television, Table }
        private enum ViewMode { RoomOverview, DoorCloseup, TableCloseup }
        private enum DoorHotspot { FishTank, Photograph, Door }
        private enum CommunicationStage
        {
            None, WomanSpeaking, FirstTyping, FirstResult, Clarification,
            SecondTyping, SecondResult, Acknowledgement
        }

        [Header("Views")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private GameObject roomView;
        [SerializeField] private GameObject doorCloseupView;
        [SerializeField] private GameObject tableCloseupView;
        [SerializeField] private GameObject televisionOff;
        [SerializeField] private GameObject televisionOn;
        [SerializeField] private GameObject blurredLan;

        [Header("Room and door hotspots (enum order)")]
        [SerializeField] private Button[] roomHotspotButtons;
        [SerializeField] private Button[] doorHotspotButtons;
        [SerializeField] private Button doorBackButton;

        [Header("Shared presentation")]
        [SerializeField] private Text objectiveText;
        [SerializeField] private GameObject narrationPanel;
        [SerializeField] private Text narrationText;
        [SerializeField] private Text continueButtonText;
        [SerializeField] private Button continueButton;
        [SerializeField] private CanvasGroup fadeOverlay;

        [Header("Speech entry")]
        [SerializeField] private GameObject speechEntryPanel;
        [SerializeField] private InputField speechInput;
        [SerializeField] private Button suggestedResponseButton;
        [SerializeField] private Button speakButton;

        [Header("Table task")]
        [SerializeField] private UIDraggableItem[] tableItems;
        [SerializeField] private UIDropTarget[] tableTargets;
        [SerializeField] private Toggle[] tableChecklist;
        [SerializeField] private Button tableBackButton;
        [SerializeField] private Button finishTableButton;

        private readonly bool[] inspected = new bool[3];
        private readonly bool[] doorInspected = new bool[3];
        private readonly bool[] tableItemsPlaced = new bool[4];
        private readonly bool[] tableChecklistCompleted = new bool[4];
        private GameSession session;
        private bool opening = true;
        private bool readyToLeave;
        private CommunicationStage communicationStage;
        private char[] speechMapping;
        private ViewMode viewMode;

        private void Awake()
        {
            session = GameSession.EnsureInstance();
            BindControls();
        }

        private void Start()
        {
            ConfigureInitialState();
            ShowNarration("I need to finish this. Patients are waiting for me to care for them.");
        }

        private void Update()
        {
            if (fadeOverlay != null && fadeOverlay.alpha > 0f)
                fadeOverlay.alpha = Mathf.MoveTowards(fadeOverlay.alpha, 0f, Time.unscaledDeltaTime * 4f);

            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (viewMode != ViewMode.RoomOverview && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (viewMode == ViewMode.DoorCloseup) CloseDoorCloseup();
                else CloseTableCloseup(false);
                return;
            }

            if (IsTyping())
            {
                if (keyboard.enterKey.wasPressedThisFrame) SubmitThought();
                return;
            }

            if ((keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) &&
                narrationPanel != null && narrationPanel.activeSelf)
                ContinueNarration();
        }

        private void BindControls()
        {
            BindButtonArray(roomHotspotButtons, index => SelectRoomHotspot((Hotspot)index));
            BindButtonArray(doorHotspotButtons, index => SelectDoorHotspot((DoorHotspot)index));
            continueButton?.onClick.AddListener(ContinueNarration);
            doorBackButton?.onClick.AddListener(CloseDoorCloseup);
            tableBackButton?.onClick.AddListener(() => CloseTableCloseup(false));
            finishTableButton?.onClick.AddListener(() => CloseTableCloseup(true));
            suggestedResponseButton?.onClick.AddListener(UseSuggestedResponse);
            speakButton?.onClick.AddListener(SubmitThought);
            speechInput?.onValueChanged.AddListener(_ => UpdateSpeakButton());

            for (var index = 0; index < tableItems.Length; index++)
            {
                tableItems[index]?.Initialize(this, index, rootCanvas);
                if (index < tableTargets.Length) tableTargets[index]?.Configure(index);
                if (index >= tableChecklist.Length || tableChecklist[index] == null) continue;
                var capturedIndex = index;
                tableChecklist[index].onValueChanged.AddListener(value => ChecklistChanged(capturedIndex, value));
            }
        }

        private static void BindButtonArray(Button[] buttons, System.Action<int> action)
        {
            if (buttons == null) return;
            for (var index = 0; index < buttons.Length; index++)
            {
                var capturedIndex = index;
                buttons[index]?.onClick.AddListener(() => action(capturedIndex));
            }
        }

        private void ConfigureInitialState()
        {
            viewMode = ViewMode.RoomOverview;
            SetView(roomView);
            televisionOff?.SetActive(true);
            televisionOn?.SetActive(false);
            blurredLan?.SetActive(false);
            speechEntryPanel?.SetActive(false);
            SetRoomHotspotsVisible(false);
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            foreach (var toggle in tableChecklist)
            {
                if (toggle == null) continue;
                toggle.isOn = false;
                toggle.interactable = false;
            }
            UpdateTableFinishButton();
            UpdateObjective();
        }

        private void SelectRoomHotspot(Hotspot hotspot)
        {
            if (opening || readyToLeave || viewMode != ViewMode.RoomOverview || inspected[(int)hotspot]) return;
            if (hotspot == Hotspot.LeftWall) OpenDoorCloseup();
            else if (hotspot == Hotspot.Table) OpenTableCloseup();
            else InspectRoomHotspot(hotspot);
        }

        private void SelectDoorHotspot(DoorHotspot hotspot)
        {
            if (viewMode != ViewMode.DoorCloseup || doorInspected[(int)hotspot]) return;
            doorInspected[(int)hotspot] = true;
            HideCompletedButton(doorHotspotButtons, (int)hotspot);
            ShowNarration(hotspot switch
            {
                DoorHotspot.FishTank => "Have I fed the fish yet?",
                DoorHotspot.Photograph => "Who is that in this photo?",
                _ => "It's getting late. I have to go care for my patients"
            });
            UpdateObjective();
        }

        private void InspectRoomHotspot(Hotspot hotspot)
        {
            inspected[(int)hotspot] = true;
            HideCompletedButton(roomHotspotButtons, (int)hotspot);
            ShowNarration(hotspot switch
            {
                Hotspot.Television => "The clock says one thing. The calendar says another. The ward clock must be slow again.",
                Hotspot.Table => "Water. A clean cloth. Notes. I should have prepared these already... Did I?",
                _ => "Something belongs here. A photograph? No—perhaps someone moved the patient file."
            });
            CompleteRoomIfReady();
        }

        private void OpenDoorCloseup()
        {
            viewMode = ViewMode.DoorCloseup;
            HideNarration();
            SetView(doorCloseupView);
            BeginFade();
            UpdateObjective();
        }

        private void CloseDoorCloseup()
        {
            if (viewMode != ViewMode.DoorCloseup) return;
            if (DoorInspectedCount() == doorInspected.Length)
            {
                inspected[(int)Hotspot.LeftWall] = true;
                HideCompletedButton(roomHotspotButtons, (int)Hotspot.LeftWall);
            }
            viewMode = ViewMode.RoomOverview;
            SetView(roomView);
            BeginFade();
            CompleteRoomIfReady();
            UpdateObjective();
        }

        private void OpenTableCloseup()
        {
            viewMode = ViewMode.TableCloseup;
            HideNarration();
            SetView(tableCloseupView);
            BeginFade();
            UpdateObjective();
        }

        private void CloseTableCloseup(bool finishTask)
        {
            if (viewMode != ViewMode.TableCloseup) return;
            if (finishTask && IsTableTaskComplete())
            {
                inspected[(int)Hotspot.Table] = true;
                HideCompletedButton(roomHotspotButtons, (int)Hotspot.Table);
            }
            viewMode = ViewMode.RoomOverview;
            SetView(roomView);
            BeginFade();
            CompleteRoomIfReady();
            UpdateObjective();
        }

        public void NotifyTableItemPlaced(int index)
        {
            if (index < 0 || index >= tableItemsPlaced.Length) return;
            tableItemsPlaced[index] = true;
            tableChecklist[index].interactable = true;
            UpdateTableFinishButton();
        }

        public void NotifyTableItemRemoved(int index)
        {
            if (index < 0 || index >= tableItemsPlaced.Length) return;
            tableItemsPlaced[index] = false;
            tableChecklistCompleted[index] = false;
            tableChecklist[index].SetIsOnWithoutNotify(false);
            tableChecklist[index].interactable = false;
            UpdateTableFinishButton();
        }

        private void ChecklistChanged(int index, bool value)
        {
            tableChecklistCompleted[index] = value && tableItemsPlaced[index];
            UpdateTableFinishButton();
        }

        private void UpdateTableFinishButton()
        {
            if (finishTableButton != null) finishTableButton.interactable = IsTableTaskComplete();
        }

        private bool IsTableTaskComplete()
        {
            for (var index = 0; index < tableItemsPlaced.Length; index++)
                if (!tableItemsPlaced[index] || !tableChecklistCompleted[index]) return false;
            return true;
        }

        private void CompleteRoomIfReady()
        {
            if (InspectedCount() != inspected.Length) return;
            readyToLeave = true;
            televisionOff?.SetActive(false);
            televisionOn?.SetActive(true);
            ShowNarration("That's everything... Oh, yes. I have to be at the hospital. Why am I here?");
            UpdateObjective();
        }

        private void ContinueNarration()
        {
            if (readyToLeave) AdvanceEndingBeat();
            else
            {
                var wasOpening = opening;
                opening = false;
                HideNarration();
                if (wasOpening) SetRoomHotspotsVisible(true);
                UpdateObjective();
            }
        }

        private void AdvanceEndingBeat()
        {
            switch (communicationStage)
            {
                case CommunicationStage.None:
                    blurredLan?.SetActive(true);
                    communicationStage = CommunicationStage.WomanSpeaking;
                    ShowNarration("WOMAN: \"Minh... I'm— ... lunch is... You need to— ... with me.\"");
                    BeginFade();
                    break;
                case CommunicationStage.WomanSpeaking:
                    BeginTyping(false);
                    break;
                case CommunicationStage.FirstResult:
                    communicationStage = CommunicationStage.Clarification;
                    ShowNarration("WOMAN: \"Dad, I'm sorry—I didn't understand. Take your time. Can you try again?\"");
                    break;
                case CommunicationStage.Clarification:
                    BeginTyping(true);
                    break;
                case CommunicationStage.SecondResult:
                    communicationStage = CommunicationStage.Acknowledgement;
                    ShowNarration("WOMAN: \"You're worried about your patients. I hear you. We can talk about them over lunch.\"");
                    break;
                case CommunicationStage.Acknowledgement:
                    if (!session.IsTransitioning) session.BeginIntervention();
                    break;
            }
            UpdateObjective();
        }

        private void BeginTyping(bool secondAttempt)
        {
            communicationStage = secondAttempt ? CommunicationStage.SecondTyping : CommunicationStage.FirstTyping;
            speechMapping = CreateSpeechMapping(secondAttempt ? 6 : 12);
            HideNarration();
            speechEntryPanel?.SetActive(true);
            speechInput.text = string.Empty;
            speechInput.Select();
            speechInput.ActivateInputField();
            UpdateSpeakButton();
        }

        private void UseSuggestedResponse()
        {
            speechInput.text = communicationStage == CommunicationStage.FirstTyping
                ? "I need to get back to the hospital. My patients are waiting."
                : "The patients need me. I have to go.";
            speechInput.ActivateInputField();
        }

        private void SubmitThought()
        {
            var thought = speechInput.text.Trim();
            if (!IsTyping() || thought.Length < 3) return;
            var spoken = TransformSpeech(thought);
            var firstAttempt = communicationStage == CommunicationStage.FirstTyping;
            session.SaveMinhSpokenLine(firstAttempt, spoken);
            communicationStage = firstAttempt ? CommunicationStage.FirstResult : CommunicationStage.SecondResult;
            speechEntryPanel.SetActive(false);
            ShowNarration($"MINH: \"{spoken}\"");
            UpdateObjective();
        }

        private void UpdateSpeakButton()
        {
            if (speakButton != null) speakButton.interactable = speechInput != null && speechInput.text.Trim().Length >= 3;
        }

        private void ShowNarration(string text)
        {
            narrationText.text = text;
            continueButtonText.text = readyToLeave && !blurredLan.activeSelf ? "Listen  [Enter / Space]" : "Continue  [Enter / Space]";
            narrationPanel.SetActive(true);
        }

        private void HideNarration() => narrationPanel?.SetActive(false);

        private void SetView(GameObject activeView)
        {
            roomView?.SetActive(activeView == roomView);
            doorCloseupView?.SetActive(activeView == doorCloseupView);
            tableCloseupView?.SetActive(activeView == tableCloseupView);
        }

        private void BeginFade()
        {
            if (fadeOverlay != null) fadeOverlay.alpha = 1f;
        }

        private void UpdateObjective()
        {
            if (objectiveText == null) return;
            objectiveText.text = opening ? "ACT 1" : blurredLan != null && blurredLan.activeSelf ? "Someone is at the door." :
                readyToLeave ? "The television cuts through the room." :
                viewMode == ViewMode.DoorCloseup
                    ? $"Look more closely. {doorInspected.Length - DoorInspectedCount()} details remaining."
                    : viewMode == ViewMode.TableCloseup
                        ? "Arrange the items, then complete the checklist."
                        : $"Inspect the room and finish the task. {inspected.Length - InspectedCount()} areas remaining.";
        }

        private static void HideCompletedButton(Button[] buttons, int index)
        {
            if (buttons != null && index >= 0 && index < buttons.Length && buttons[index] != null)
                buttons[index].gameObject.SetActive(false);
        }

        private void SetRoomHotspotsVisible(bool visible)
        {
            if (roomHotspotButtons == null) return;
            for (var index = 0; index < roomHotspotButtons.Length; index++)
                if (roomHotspotButtons[index] != null)
                    roomHotspotButtons[index].gameObject.SetActive(visible && !inspected[index]);
        }

        private bool IsTyping() => communicationStage == CommunicationStage.FirstTyping ||
                                   communicationStage == CommunicationStage.SecondTyping;

        private int InspectedCount() => CountTrue(inspected);
        private int DoorInspectedCount() => CountTrue(doorInspected);

        private static int CountTrue(bool[] values)
        {
            var count = 0;
            foreach (var value in values) if (value) count++;
            return count;
        }

        private static char[] CreateSpeechMapping(int transformedLetterCount)
        {
            var mapping = new char[26];
            var indices = new int[26];
            for (var index = 0; index < 26; index++) { mapping[index] = (char)('a' + index); indices[index] = index; }
            for (var index = 25; index > 0; index--)
            {
                var swap = Random.Range(0, index + 1);
                (indices[index], indices[swap]) = (indices[swap], indices[index]);
            }
            transformedLetterCount = Mathf.Clamp(transformedLetterCount, 2, 26);
            for (var index = 0; index < transformedLetterCount; index++)
            {
                var source = indices[index];
                var destination = indices[(index + 1) % transformedLetterCount];
                mapping[source] = (char)('a' + destination);
            }
            return mapping;
        }

        private string TransformSpeech(string thought)
        {
            var result = new StringBuilder(thought.Length);
            foreach (var character in thought)
            {
                var lower = char.ToLowerInvariant(character);
                if (lower < 'a' || lower > 'z') { result.Append(character); continue; }
                var mapped = speechMapping[lower - 'a'];
                result.Append(char.IsUpper(character) ? char.ToUpperInvariant(mapped) : mapped);
            }
            return result.ToString();
        }
    }
}
