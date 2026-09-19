using UnityEngine;

namespace MagesDementiaGame
{
    /// <summary>
    /// Reflection presentation retained after the playable recipient and caregiver scenes.
    /// </summary>
    public sealed class PhaseOnePrototypeUI : MonoBehaviour
    {
        private const float ReferenceWidth = 900f;
        private const float ReferenceHeight = 700f;

        private GameSession session;
        private Vector2 scrollPosition;
        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle cardStyle;
        private GUIStyle buttonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PhaseOnePrototypeUI>() == null)
            {
                new GameObject("Reflection UI").AddComponent<PhaseOnePrototypeUI>();
            }
        }

        private void Awake()
        {
            session = GameSession.EnsureInstance();
            session.StateChanged += HandleStateChanged;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.StateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged()
        {
            scrollPosition = Vector2.zero;
        }

        private void OnGUI()
        {
            if (session.Phase != NarrativePhase.Reflection)
            {
                return;
            }

            EnsureStyles();

            var scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            var scaledWidth = Screen.width / scale;
            var scaledHeight = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            DrawBackground(new Rect(0f, 0f, scaledWidth, scaledHeight));
            GUILayout.BeginArea(new Rect(60f, 35f, scaledWidth - 120f, scaledHeight - 70f));
            GUILayout.Label("REFLECTION", bodyStyle);
            GUILayout.Space(8f);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            DrawReflection();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawReflection()
        {
            GUILayout.Label("What changed, and why", titleStyle);
            GUILayout.Label("There is no empathy score. Each action changed a specific part of Minh's experience.", headingStyle);
            GUILayout.Space(20f);

            DrawReflectionCard("Environment", PhaseOneContent.EnvironmentReflection(session.SelectedEnvironmentChoice));
            DrawReflectionCard("Approach", PhaseOneContent.ApproachReflection(session.SelectedApproachChoice));
            DrawReflectionCard("Response", PhaseOneContent.ResponseReflection(session.SelectedResponseChoice));

            GUILayout.Space(20f);
            GUILayout.Label(
                "Dementia experiences vary between people and from moment to moment. These strategies do not fix dementia; they can make an interaction easier to navigate and preserve the person's ability to participate.",
                cardStyle);

            GUILayout.Space(24f);
            if (GUILayout.Button("Restart and try different choices", buttonStyle, GUILayout.Height(50f)))
            {
                session.Restart();
            }
        }

        private void DrawReflectionCard(string label, string explanation)
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label(label, headingStyle);
            GUILayout.Label(explanation, bodyStyle);
            GUILayout.EndVertical();
            GUILayout.Space(12f);
        }

        private void DrawBackground(Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(0.09f, 0.14f, 0.15f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.95f, 0.91f, 0.82f) }
            };
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                wordWrap = true,
                normal = { textColor = new Color(0.83f, 0.89f, 0.86f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = new Color(0.83f, 0.89f, 0.86f) }
            };
            cardStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(18, 18, 15, 15),
                normal = { textColor = new Color(0.92f, 0.92f, 0.88f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(16, 16, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
        }
    }
}
