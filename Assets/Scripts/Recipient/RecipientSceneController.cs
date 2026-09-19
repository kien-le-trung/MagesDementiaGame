using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MagesDementiaGame
{
    public enum RecipientBeat
    {
        ExaminePhotograph,
        NoticeTelevision,
        LanEntering,
        LunchDialogue,
        RecipientResponse,
        Resolution,
        Complete
    }

    public sealed class RecipientSceneController : MonoBehaviour, IInteractionHost
    {
        private enum DialoguePanel
        {
            None,
            Narration,
            Response
        }

        private const float ReferenceWidth = 960f;
        private const float ReferenceHeight = 540f;

        [SerializeField] private RoomArtSet artSet;

        private GameSession session;
        private RoomView roomView;
        private IReadOnlyList<string> beats;
        private DialoguePanel activePanel;
        private RecipientBeat currentBeat;
        private string interactionPrompt;
        private string dialogueText;
        private Action continueAction;
        private bool roomBuilt;
        private bool lanEntering;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle promptStyle;

        public bool IsReplay { get; private set; }
        public bool PhotographIsVisible => roomView != null && roomView.Photograph.activeSelf;
        public bool IsInteractionBlocked => activePanel != DialoguePanel.None || lanEntering ||
                                             currentBeat == RecipientBeat.Complete ||
                                             session.Phase == NarrativePhase.Reflection;
        public bool CanExaminePhotograph => currentBeat == RecipientBeat.ExaminePhotograph && !IsInteractionBlocked;
        public bool CanNoticeTelevision => currentBeat == RecipientBeat.NoticeTelevision && !IsInteractionBlocked;

        public static void BuildForCurrentScene()
        {
            var existing = FindFirstObjectByType<RecipientSceneController>();
            if (existing != null)
            {
                existing.BuildRoom();
                return;
            }

            var root = new GameObject("Recipient Scene");
            var controller = root.AddComponent<RecipientSceneController>();
            controller.BuildRoom();
        }

        private void Awake()
        {
            session = GameSession.EnsureInstance();
        }

        private void Start()
        {
            BuildRoom();
        }

        private void Update()
        {
            HandleDialogueInput();
        }

        public void SetInteractionPrompt(string prompt)
        {
            interactionPrompt = prompt;
        }

        public void ExaminePhotograph()
        {
            if (!CanExaminePhotograph)
            {
                return;
            }

            ShowNarration(beats[0], () => currentBeat = RecipientBeat.NoticeTelevision);
        }

        public void NoticeTelevision()
        {
            if (!CanNoticeTelevision)
            {
                return;
            }

            ShowNarration(beats[1], BeginLanEntrance);
        }

        private void BuildRoom()
        {
            if (roomBuilt)
            {
                return;
            }

            roomBuilt = true;
            IsReplay = session.Phase == NarrativePhase.Replay;
            beats = IsReplay
                ? PhaseOneContent.BuildReplayBeats(session)
                : PhaseOneContent.BaselineBeats;
            currentBeat = RecipientBeat.ExaminePhotograph;

            roomView = gameObject.AddComponent<RoomView>();
            roomView.Initialize(artSet);
            roomView.Build(RoomPerspective.Recipient);
            roomView.PlayerInteractionController.Initialize(this);
            roomView.SetLanVisible(false);

            if (IsReplay)
            {
                ApplyReplayEnvironment();
            }
            else
            {
                roomView.SetTelevisionState(TelevisionState.On);
                roomView.SetPhotoVisible(false);
            }

            var photoInteraction = roomView.PhotographSpot.AddComponent<RecipientPhotoInteractable>();
            photoInteraction.Initialize(this);
            var televisionInteraction = roomView.Television.AddComponent<RecipientTelevisionInteractable>();
            televisionInteraction.Initialize(this);
        }

        private void ApplyReplayEnvironment()
        {
            var televisionState = session.SelectedEnvironmentChoice switch
            {
                EnvironmentChoice.LowerTelevision => TelevisionState.Lowered,
                EnvironmentChoice.TurnOffTelevisionAndRestorePhoto => TelevisionState.Off,
                _ => TelevisionState.On
            };
            roomView.SetTelevisionState(televisionState);
            roomView.SetPhotoVisible(session.ReplayEffects.PhotoRestored);
        }

        private void BeginLanEntrance()
        {
            currentBeat = RecipientBeat.LanEntering;
            StartCoroutine(MoveLanIntoRoom());
        }

        private IEnumerator MoveLanIntoRoom()
        {
            lanEntering = true;
            roomView.PlayerController.SetMovementEnabled(false);
            roomView.Lan.transform.position = roomView.LanDoorwayPosition;
            roomView.SetLanVisible(true);

            const float duration = 1.5f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var amount = Mathf.Clamp01(elapsed / duration);
                roomView.Lan.transform.position = Vector3.Lerp(
                    roomView.LanDoorwayPosition,
                    roomView.LanConversationPosition,
                    amount);
                yield return null;
            }

            lanEntering = false;
            ShowNarration(beats[2], ShowLunchDialogue);
        }

        private void ShowLunchDialogue()
        {
            currentBeat = RecipientBeat.LunchDialogue;
            ShowNarration(beats[3], ShowResponseChoices);
        }

        private void ShowResponseChoices()
        {
            currentBeat = RecipientBeat.RecipientResponse;
            activePanel = DialoguePanel.Response;
            interactionPrompt = null;
            roomView.PlayerController.SetMovementEnabled(false);
        }

        private void SelectResponse(int index)
        {
            if (activePanel != DialoguePanel.Response)
            {
                return;
            }

            var responseText = beats[4];
            if (index == 1)
            {
                responseText = session.ReplayEffects.SpeechClarity switch
                {
                    SpeechClarity.Clear => "You ask Lan to repeat herself. She says clearly, \"Lunch is ready. Would you like to come with me?\"",
                    SpeechClarity.Partial => "You ask her to repeat herself. This time you catch: \"Lunch is... would you like to come?\"",
                    _ => "You ask her to repeat herself, but the television still breaks her words apart."
                };
            }
            else if (index == 2)
            {
                responseText = "You ask, \"Can we look at the photograph before lunch?\" Lan gives your concern space in the conversation.";
            }

            ShowNarration(responseText, ShowResolution);
        }

        private void ShowResolution()
        {
            currentBeat = RecipientBeat.Resolution;
            ShowNarration(beats[5], FinishRecipientAct);
        }

        private void FinishRecipientAct()
        {
            currentBeat = RecipientBeat.Complete;
            roomView.PlayerController.SetMovementEnabled(false);
            if (IsReplay)
            {
                session.BeginReflection();
            }
            else
            {
                session.BeginIntervention();
            }
        }

        private void ShowNarration(string text, Action onContinue)
        {
            dialogueText = text;
            continueAction = onContinue;
            activePanel = DialoguePanel.Narration;
            interactionPrompt = null;
            roomView.PlayerController.SetMovementEnabled(false);
        }

        private void ContinueNarration()
        {
            if (activePanel != DialoguePanel.Narration)
            {
                return;
            }

            var action = continueAction;
            continueAction = null;
            activePanel = DialoguePanel.None;
            roomView.PlayerController.SetMovementEnabled(true);
            action?.Invoke();
        }

        private void HandleDialogueInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (activePanel == DialoguePanel.Narration &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                ContinueNarration();
                return;
            }

            if (activePanel != DialoguePanel.Response)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame ||
                (!IsReplay && keyboard.enterKey.wasPressedThisFrame))
            {
                SelectResponse(0);
            }
            else if (IsReplay && session.ReplayEffects.Agency >= 1 &&
                     (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame))
            {
                SelectResponse(1);
            }
            else if (IsReplay && session.ReplayEffects.Agency >= 2 &&
                     (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame))
            {
                SelectResponse(2);
            }
        }

        private void OnGUI()
        {
            if (!roomBuilt || session.Phase == NarrativePhase.Reflection)
            {
                return;
            }

            EnsureStyles();
            var scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            var width = Screen.width / scale;
            var height = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            DrawObjective(width);
            DrawCharacterLabel(roomView.Minh, "MINH", scale);
            if (roomView.Lan.activeInHierarchy)
            {
                DrawCharacterLabel(roomView.Lan, "LAN", scale);
            }

            if (!string.IsNullOrEmpty(interactionPrompt) && activePanel == DialoguePanel.None && !lanEntering)
            {
                GUI.Label(new Rect(width * 0.25f, height - 62f, width * 0.5f, 42f), interactionPrompt, promptStyle);
            }

            if (activePanel != DialoguePanel.None)
            {
                DrawDialoguePanel(width, height);
            }
        }

        private void DrawObjective(float width)
        {
            var objective = currentBeat switch
            {
                RecipientBeat.ExaminePhotograph => IsReplay && roomView.Photograph.activeSelf
                    ? "Look at the familiar photograph on the table."
                    : "Something is missing from the table. Look more closely.",
                RecipientBeat.NoticeTelevision => "The television noise demands your attention.",
                RecipientBeat.LanEntering => "Someone is entering the room.",
                _ => "Try to understand what the woman wants."
            };

            GUI.Box(new Rect(24f, 18f, width - 48f, 68f), GUIContent.none);
            GUI.Label(new Rect(42f, 25f, width - 84f, 24f),
                IsReplay ? "ACT 3 / MINH'S PERSPECTIVE — REPLAY" : "ACT 1 / MINH'S PERSPECTIVE", titleStyle);
            GUI.Label(new Rect(42f, 50f, width - 84f, 28f), objective, bodyStyle);
        }

        private void DrawCharacterLabel(GameObject character, string text, float scale)
        {
            var point = roomView.RoomCamera.WorldToScreenPoint(character.transform.position);
            var x = point.x / scale;
            var y = (Screen.height - point.y) / scale;
            GUI.Label(new Rect(x - 50f, y - 14f, 100f, 24f), text, labelStyle);
        }

        private void DrawDialoguePanel(float width, float height)
        {
            var panelHeight = activePanel == DialoguePanel.Response ? 225f : 145f;
            var rect = new Rect(38f, height - panelHeight - 24f, width - 76f, panelHeight);
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 22f, rect.y + 18f, rect.width - 44f, rect.height - 34f));

            if (activePanel == DialoguePanel.Narration)
            {
                GUILayout.Label(dialogueText, bodyStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Continue  [Enter / Space]", buttonStyle, GUILayout.Height(40f)))
                {
                    ContinueNarration();
                }
            }
            else
            {
                GUILayout.Label("How can you respond?", titleStyle);
                if (GUILayout.Button("1  Where is the photograph?", buttonStyle, GUILayout.Height(38f))) SelectResponse(0);
                if (IsReplay && session.ReplayEffects.Agency >= 1 &&
                    GUILayout.Button("2  Please say that again.", buttonStyle, GUILayout.Height(38f))) SelectResponse(1);
                if (IsReplay && session.ReplayEffects.Agency >= 2 &&
                    GUILayout.Button("3  Can we look at the photograph before lunch?", buttonStyle, GUILayout.Height(38f))) SelectResponse(2);
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.91f, 0.82f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.91f, 0.88f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(14, 14, 7, 7)
            };
            promptStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
    }
}
