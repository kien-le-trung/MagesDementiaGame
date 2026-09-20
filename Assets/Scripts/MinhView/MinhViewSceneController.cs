using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

namespace MagesDementiaGame
{
    /// <summary>Baseline Act 1: a static, first-person room view from Minh's position on the sofa.</summary>
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

        [SerializeField] private Texture2D background;
        [SerializeField] private Texture2D televisionStand;
        [SerializeField] private Texture2D televisionStandOn;
        [SerializeField] private Texture2D doorCloseup;
        [SerializeField] private Texture2D blurredLan;
        [SerializeField] private Texture2D woodenSurface;
        [SerializeField] private Texture2D[] individualTableItems;

        private readonly bool[] inspected = new bool[3];
        private readonly bool[] doorInspected = new bool[3];
        private readonly bool[] tableItemsPlaced = new bool[4];
        private readonly bool[] tableChecklist = new bool[4];
        private readonly string[] tableItemNames = { "Water", "Clean cloth", "Notes", "Medicine" };
        private GameSession session;
        private string narration;
        private bool opening = true;
        private bool readyToLeave;
        private bool televisionOn;
        private bool womanVisible;
        private CommunicationStage communicationStage;
        private string typedThought = "";
        private char[] speechMapping;
        private ViewMode viewMode;
        private float transitionAlpha;
        private int draggedTableItem = -1;
        private Vector2 tableDragOffset;
        private GUIStyle objectiveStyle, narrationStyle, hotspotLabelStyle, completedLabelStyle, buttonStyle;
        private GUIStyle inputStyle, inputLabelStyle;

        private void Awake() => session = GameSession.EnsureInstance();

        private void Start()
        {
            narration = "I need to finish this. Patients are waiting for me to care for them.";
        }

        private void Update()
        {
            transitionAlpha = Mathf.MoveTowards(transitionAlpha, 0f, Time.unscaledDeltaTime * 4f);
            if (viewMode != ViewMode.RoomOverview && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if (viewMode == ViewMode.DoorCloseup) CloseDoorCloseup();
                else CloseTableCloseup(false);
                return;
            }

            if (IsTyping())
            {
                if (Keyboard.current?.enterKey.wasPressedThisFrame == true) SubmitThought();
                return;
            }

            if (Keyboard.current?.enterKey.wasPressedThisFrame == true ||
                Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            {
                if (readyToLeave) AdvanceEndingBeat();
                else if (opening) { opening = false; narration = null; }
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUI.color = Color.white;
            var backgroundRect = CalculateBackgroundRect();
            if (viewMode == ViewMode.DoorCloseup)
                GUI.DrawTexture(backgroundRect, doorCloseup, ScaleMode.ScaleToFit, false);
            else if (viewMode == ViewMode.TableCloseup)
                DrawTableTask();
            else
            {
                GUI.DrawTexture(backgroundRect, background, ScaleMode.ScaleToFit, false);
                DrawTelevisionStand(backgroundRect, televisionOn ? televisionStandOn : televisionStand);
                if (womanVisible) DrawBlurredWoman(backgroundRect);
            }
            DrawObjective();

            if (!opening && !readyToLeave && viewMode == ViewMode.RoomOverview)
            {
                DrawHotspot(Hotspot.LeftWall, new Rect(0.015f, 0.10f, 0.27f, 0.53f), "Inspect the left side");
                DrawHotspot(Hotspot.Television, new Rect(0.29f, 0.28f, 0.42f, 0.34f), "Inspect the television area");
                DrawHotspot(Hotspot.Table, new Rect(0.30f, 0.64f, 0.40f, 0.25f), "Inspect the table");
            }
            else if (viewMode == ViewMode.DoorCloseup)
            {
                DrawDoorHotspot(DoorHotspot.Photograph, new Rect(0.16f, 0.04f, 0.18f, 0.34f), "Photograph");
                DrawDoorHotspot(DoorHotspot.FishTank, new Rect(0.20f, 0.45f, 0.18f, 0.24f), "Fish tank");
                DrawDoorHotspot(DoorHotspot.Door, new Rect(0.44f, 0.02f, 0.30f, 0.90f), "Door");
                if (GUI.Button(new Rect(Screen.width - 156f, 22f, 132f, 40f), "Back  [Esc]", buttonStyle))
                    CloseDoorCloseup();
            }

            if (!string.IsNullOrEmpty(narration)) DrawNarration();
            if (IsTyping()) DrawThoughtEntry();
            if (transitionAlpha > 0f)
            {
                var previous = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, transitionAlpha);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = previous;
            }
        }

        private void DrawObjective()
        {
            var remaining = 3 - InspectedCount();
            var doorRemaining = 3 - DoorInspectedCount();
            var text = opening ? "ACT 1" : womanVisible ? "Someone is at the door." :
                readyToLeave ? "The television cuts through the room." :
                viewMode == ViewMode.DoorCloseup
                    ? $"Look more closely.  {doorRemaining} detail{(doorRemaining == 1 ? "" : "s")} remaining."
                    : viewMode == ViewMode.TableCloseup
                        ? "Arrange the items, then complete the checklist."
                    : $"Inspect the room and finish the task.  {remaining} area{(remaining == 1 ? "" : "s")} remaining.";
            GUI.Box(new Rect(24f, 20f, Mathf.Min(720f, Screen.width - 48f), 54f), text, objectiveStyle);
        }

        private void DrawHotspot(Hotspot hotspot, Rect normalized, string label)
        {
            var backgroundRect = CalculateBackgroundRect();
            var rect = new Rect(
                backgroundRect.x + normalized.x * backgroundRect.width,
                backgroundRect.y + normalized.y * backgroundRect.height,
                normalized.width * backgroundRect.width,
                normalized.height * backgroundRect.height);
            var done = inspected[(int)hotspot];
            var hovered = rect.Contains(Event.current.mousePosition);
            if (hovered || done) DrawHotspotOutline(rect, done);
            if (hovered || done)
                GUI.Label(new Rect(rect.x, rect.yMax - 32f, rect.width, 30f), done ? "✓ " + label : label,
                    done ? completedLabelStyle : hotspotLabelStyle);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none) && !done)
            {
                if (hotspot == Hotspot.LeftWall) OpenDoorCloseup();
                else if (hotspot == Hotspot.Table) OpenTableCloseup();
                else Inspect(hotspot);
            }
        }

        private void DrawDoorHotspot(DoorHotspot hotspot, Rect normalized, string label)
        {
            var backgroundRect = CalculateBackgroundRect();
            var rect = new Rect(
                backgroundRect.x + normalized.x * backgroundRect.width,
                backgroundRect.y + normalized.y * backgroundRect.height,
                normalized.width * backgroundRect.width,
                normalized.height * backgroundRect.height);
            var done = doorInspected[(int)hotspot];
            var hovered = rect.Contains(Event.current.mousePosition);
            if (hovered || done) DrawHotspotOutline(rect, done);
            if (hovered || done)
                GUI.Label(new Rect(rect.x, rect.yMax - 32f, rect.width, 30f), done ? "✓ " + label : label,
                    done ? completedLabelStyle : hotspotLabelStyle);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none) && !done) InspectDoorHotspot(hotspot);
        }

        private void OpenDoorCloseup()
        {
            viewMode = ViewMode.DoorCloseup;
            narration = null;
            transitionAlpha = 1f;
        }

        private void CloseDoorCloseup()
        {
            if (DoorInspectedCount() == doorInspected.Length)
                inspected[(int)Hotspot.LeftWall] = true;
            viewMode = ViewMode.RoomOverview;
            transitionAlpha = 1f;
            if (InspectedCount() == inspected.Length)
            {
                readyToLeave = true;
                televisionOn = true;
                narration = "That's everything... Oh, yes. I have to be at the hospital. Why am I here?";
            }
            else narration = null;
        }

        private void InspectDoorHotspot(DoorHotspot hotspot)
        {
            doorInspected[(int)hotspot] = true;
            narration = hotspot switch
            {
                DoorHotspot.FishTank => "Have I fed the fish yet?",
                DoorHotspot.Photograph => "Who is that in this photo?",
                _ => "It's getting late. I have to go care for my patients"
            };
        }

        private void OpenTableCloseup()
        {
            viewMode = ViewMode.TableCloseup;
            narration = null;
            transitionAlpha = 1f;
        }

        private void CloseTableCloseup(bool finishTask)
        {
            if (finishTask && IsTableTaskComplete()) inspected[(int)Hotspot.Table] = true;
            draggedTableItem = -1;
            viewMode = ViewMode.RoomOverview;
            transitionAlpha = 1f;
            CompleteRoomIfReady();
        }

        private void DrawTableTask()
        {
            var surface = CalculateTextureRect(woodenSurface);
            GUI.DrawTexture(surface, woodenSurface, ScaleMode.ScaleToFit, false);

            var targets = new Rect[tableItemNames.Length];
            var inventory = new Rect[tableItemNames.Length];
            for (var index = 0; index < tableItemNames.Length; index++)
            {
                var row = index / 2;
                var column = index % 2;
                targets[index] = RelativeRect(surface, new Rect(0.35f + column * 0.22f, 0.30f + row * 0.30f, 0.17f, 0.22f));
                inventory[index] = RelativeRect(surface, new Rect(0.035f + column * 0.115f, 0.22f + row * 0.34f, 0.105f, 0.20f));
                GUI.Box(targets[index], tableItemsPlaced[index] ? "" : tableItemNames[index], hotspotLabelStyle);
            }

            var checklistRect = RelativeRect(surface, new Rect(0.79f, 0.16f, 0.19f, 0.69f));
            GUI.Box(checklistRect, GUIContent.none);
            GUI.Label(new Rect(checklistRect.x + 12f, checklistRect.y + 10f, checklistRect.width - 24f, 28f), "CHECKLIST", inputLabelStyle);
            for (var index = 0; index < tableItemNames.Length; index++)
            {
                var row = new Rect(checklistRect.x + 12f, checklistRect.y + 48f + index * 43f, checklistRect.width - 24f, 34f);
                GUI.enabled = tableItemsPlaced[index];
                tableChecklist[index] = GUI.Toggle(row, tableChecklist[index], tableItemNames[index]);
                GUI.enabled = true;
            }

            HandleTableDrag(inventory, targets);
            for (var index = 0; index < tableItemNames.Length; index++)
            {
                if (index == draggedTableItem) continue;
                DrawTableItem(index, tableItemsPlaced[index] ? targets[index] : inventory[index]);
            }
            if (draggedTableItem >= 0)
            {
                var source = tableItemsPlaced[draggedTableItem] ? targets[draggedTableItem] : inventory[draggedTableItem];
                var dragged = new Rect(Event.current.mousePosition - tableDragOffset, source.size);
                DrawTableItem(draggedTableItem, dragged);
            }

            if (GUI.Button(new Rect(Screen.width - 156f, 22f, 132f, 40f), "Back  [Esc]", buttonStyle))
                CloseTableCloseup(false);
            GUI.enabled = IsTableTaskComplete();
            if (GUI.Button(new Rect(checklistRect.x + 12f, checklistRect.yMax - 52f, checklistRect.width - 24f, 38f),
                    "Finish task", buttonStyle)) CloseTableCloseup(true);
            GUI.enabled = true;
        }

        private void HandleTableDrag(Rect[] inventory, Rect[] targets)
        {
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                for (var index = tableItemNames.Length - 1; index >= 0; index--)
                {
                    var rect = tableItemsPlaced[index] ? targets[index] : inventory[index];
                    if (!rect.Contains(currentEvent.mousePosition)) continue;
                    draggedTableItem = index;
                    tableDragOffset = currentEvent.mousePosition - rect.position;
                    currentEvent.Use();
                    break;
                }
            }
            else if (currentEvent.type == EventType.MouseUp && draggedTableItem >= 0)
            {
                tableItemsPlaced[draggedTableItem] = targets[draggedTableItem].Contains(currentEvent.mousePosition);
                if (!tableItemsPlaced[draggedTableItem]) tableChecklist[draggedTableItem] = false;
                draggedTableItem = -1;
                currentEvent.Use();
            }
        }

        private void DrawTableItem(int index, Rect destination)
        {
            if (individualTableItems == null || index < 0 || index >= individualTableItems.Length ||
                individualTableItems[index] == null) return;
            GUI.DrawTexture(destination, individualTableItems[index], ScaleMode.ScaleToFit, true);
        }

        private bool IsTableTaskComplete()
        {
            for (var index = 0; index < tableItemNames.Length; index++)
                if (!tableItemsPlaced[index] || !tableChecklist[index]) return false;
            return true;
        }

        private void Inspect(Hotspot hotspot)
        {
            inspected[(int)hotspot] = true;
            narration = hotspot switch
            {
                Hotspot.Table => "Water. A clean cloth. Notes. I should have prepared these already... Did I?",
                Hotspot.Television => "The clock says one thing. The calendar says another. The ward clock must be slow again.",
                _ => "Something belongs here. A photograph? No—perhaps someone moved the patient file."
            };

            if (InspectedCount() == inspected.Length)
                CompleteRoomIfReady();
        }

        private void CompleteRoomIfReady()
        {
            if (InspectedCount() != inspected.Length) { narration = null; return; }
            readyToLeave = true;
            televisionOn = true;
            narration = "That's everything... Oh, yes. I have to be at the hospital. Why am I here?";
        }

        private void DrawNarration()
        {
            var showingSpeechResult = communicationStage == CommunicationStage.FirstResult ||
                                      communicationStage == CommunicationStage.SecondResult;
            var height = showingSpeechResult ? 190f : readyToLeave ? 150f : 125f;
            var rect = new Rect(Screen.width * 0.12f, Screen.height - height - 28f, Screen.width * 0.76f, height);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 22f, rect.y + 16f, rect.width - 44f, showingSpeechResult ? 105f : 58f), narration, narrationStyle);
            var caption = womanVisible ? "Continue  [Enter / Space]" :
                readyToLeave ? "Listen  [Enter / Space]" : "Continue  [Enter / Space]";
            if (GUI.Button(new Rect(rect.x + rect.width - 310f, rect.y + rect.height - 52f, 288f, 36f), caption, buttonStyle))
            {
                if (readyToLeave) AdvanceEndingBeat();
                else { opening = false; narration = null; }
            }
        }

        private void BeginActTwo()
        {
            if (session == null || session.IsTransitioning) return;
            session.BeginIntervention();
        }

        private void AdvanceEndingBeat()
        {
            switch (communicationStage)
            {
                case CommunicationStage.None:
                    womanVisible = true;
                    communicationStage = CommunicationStage.WomanSpeaking;
                    narration = "WOMAN: \"Minh... I'm— ... lunch is... You need to— ... with me.\"";
                    transitionAlpha = 1f;
                    break;
                case CommunicationStage.WomanSpeaking:
                    BeginTyping(false);
                    break;
                case CommunicationStage.FirstResult:
                    communicationStage = CommunicationStage.Clarification;
                    narration = "WOMAN: \"Dad, I'm sorry—I didn't understand. Take your time. Can you try again?\"";
                    break;
                case CommunicationStage.Clarification:
                    BeginTyping(true);
                    break;
                case CommunicationStage.SecondResult:
                    communicationStage = CommunicationStage.Acknowledgement;
                    narration = "WOMAN: \"You're worried about your patients. I hear you. We can talk about them over lunch.\"";
                    break;
                case CommunicationStage.Acknowledgement:
                    BeginActTwo();
                    break;
            }
        }

        private bool IsTyping() => communicationStage == CommunicationStage.FirstTyping ||
                                   communicationStage == CommunicationStage.SecondTyping;

        private void BeginTyping(bool secondAttempt)
        {
            communicationStage = secondAttempt ? CommunicationStage.SecondTyping : CommunicationStage.FirstTyping;
            typedThought = "";
            speechMapping = CreateSpeechMapping(secondAttempt ? 6 : 12);
            narration = null;
        }

        private void DrawThoughtEntry()
        {
            var rect = new Rect(Screen.width * 0.12f, Screen.height - 220f, Screen.width * 0.76f, 192f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 22f, rect.y + 14f, rect.width - 44f, 28f),
                "What do you want to tell her?", inputLabelStyle);
            GUI.SetNextControlName("MinhThoughtInput");
            typedThought = GUI.TextField(new Rect(rect.x + 22f, rect.y + 48f, rect.width - 44f, 42f),
                typedThought, 120, inputStyle);

            if (GUI.Button(new Rect(rect.x + 22f, rect.y + 116f, 235f, 40f), "Use suggested response", buttonStyle))
            {
                typedThought = communicationStage == CommunicationStage.FirstTyping
                    ? "I need to get back to the hospital. My patients are waiting."
                    : "The patients need me. I have to go.";
            }
            GUI.enabled = typedThought.Trim().Length >= 3;
            if (GUI.Button(new Rect(rect.x + rect.width - 190f, rect.y + 116f, 168f, 40f),
                    "Speak  [Enter]", buttonStyle)) SubmitThought();
            GUI.enabled = true;
            GUI.FocusControl("MinhThoughtInput");
        }

        private void SubmitThought()
        {
            var thought = typedThought.Trim();
            if (!IsTyping() || thought.Length < 3) return;
            var spoken = TransformSpeech(thought);
            var firstAttempt = communicationStage == CommunicationStage.FirstTyping;
            communicationStage = firstAttempt ? CommunicationStage.FirstResult : CommunicationStage.SecondResult;
            narration = $"MINH: \"{spoken}\"";
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

        private int InspectedCount()
        {
            var count = 0;
            foreach (var value in inspected) if (value) count++;
            return count;
        }

        private int DoorInspectedCount()
        {
            var count = 0;
            foreach (var value in doorInspected) if (value) count++;
            return count;
        }

        private Rect CalculateBackgroundRect()
        {
            if (background == null) return new Rect(0f, 0f, Screen.width, Screen.height);
            var sourceAspect = (float)background.width / background.height;
            var screenAspect = (float)Screen.width / Screen.height;
            if (screenAspect > sourceAspect)
            {
                var width = Screen.height * sourceAspect;
                return new Rect((Screen.width - width) * 0.5f, 0f, width, Screen.height);
            }
            var height = Screen.width / sourceAspect;
            return new Rect(0f, (Screen.height - height) * 0.5f, Screen.width, height);
        }

        private Rect CalculateTextureRect(Texture2D texture)
        {
            if (texture == null) return new Rect(0f, 0f, Screen.width, Screen.height);
            var sourceAspect = (float)texture.width / texture.height;
            var screenAspect = (float)Screen.width / Screen.height;
            if (screenAspect > sourceAspect)
            {
                var width = Screen.height * sourceAspect;
                return new Rect((Screen.width - width) * 0.5f, 0f, width, Screen.height);
            }
            var height = Screen.width / sourceAspect;
            return new Rect(0f, (Screen.height - height) * 0.5f, Screen.width, height);
        }

        private static Rect RelativeRect(Rect parent, Rect normalized) => new Rect(
            parent.x + normalized.x * parent.width,
            parent.y + normalized.y * parent.height,
            normalized.width * parent.width,
            normalized.height * parent.height);

        private void DrawTelevisionStand(Rect backgroundRect, Texture2D stand)
        {
            if (stand == null) return;
            var width = backgroundRect.width * 0.4f;
            var height = width * stand.height / stand.width;
            var rect = new Rect(
                backgroundRect.x + backgroundRect.width * 0.3f,
                backgroundRect.y + backgroundRect.height * 0.29f,
                width,
                height);
            GUI.DrawTexture(rect, stand, ScaleMode.ScaleToFit, true);
        }

        private void DrawBlurredWoman(Rect backgroundRect)
        {
            if (blurredLan == null) return;
            var height = backgroundRect.height * 0.68f;
            var width = height * blurredLan.width / blurredLan.height;
            var rect = new Rect(
                backgroundRect.x + backgroundRect.width * 0.075f,
                backgroundRect.y + backgroundRect.height * 0.20f,
                width,
                height);
            GUI.DrawTexture(rect, blurredLan, ScaleMode.ScaleToFit, true);
        }

        private static void DrawHotspotOutline(Rect rect, bool completed)
        {
            var previous = GUI.color;
            GUI.color = completed ? new Color(0.4f, 1f, 0.65f, 0.8f) : new Color(1f, 0.84f, 0.25f, 0.9f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (objectiveStyle != null) return;
            objectiveStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(18, 18, 8, 8) };
            narrationStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true, normal = { textColor = Color.white } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            hotspotLabelStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            hotspotLabelStyle.normal.textColor = new Color(1f, 0.88f, 0.35f, 1f);
            completedLabelStyle = new GUIStyle(hotspotLabelStyle);
            completedLabelStyle.normal.textColor = new Color(0.55f, 1f, 0.7f, 1f);
            inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 18, padding = new RectOffset(10, 10, 7, 7) };
            inputLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        }
    }
}
