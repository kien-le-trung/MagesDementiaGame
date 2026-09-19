using UnityEngine;

namespace MagesDementiaGame
{
    public sealed class EnvironmentController : MonoBehaviour
    {
        private static readonly Color TelevisionOnColor = new Color(0.18f, 0.72f, 0.95f);
        private static readonly Color TelevisionLoweredColor = new Color(0.16f, 0.36f, 0.48f);
        private static readonly Color TelevisionOffColor = new Color(0.07f, 0.09f, 0.11f);

        private SpriteRenderer televisionRenderer;
        private GameObject photograph;
        private GameSession session;

        public void Initialize(SpriteRenderer television, GameObject photographObject)
        {
            televisionRenderer = television;
            photograph = photographObject;
            session = GameSession.EnsureInstance();
            session.StateChanged += ApplyCurrentChoice;
            ApplyCurrentChoice();
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.StateChanged -= ApplyCurrentChoice;
            }
        }

        public void ApplyCurrentChoice()
        {
            if (televisionRenderer == null || photograph == null || session == null)
            {
                return;
            }

            switch (session.SelectedEnvironmentChoice)
            {
                case EnvironmentChoice.LowerTelevision:
                    televisionRenderer.color = TelevisionLoweredColor;
                    photograph.SetActive(false);
                    break;
                case EnvironmentChoice.TurnOffTelevisionAndRestorePhoto:
                    televisionRenderer.color = TelevisionOffColor;
                    photograph.SetActive(true);
                    break;
                default:
                    televisionRenderer.color = TelevisionOnColor;
                    photograph.SetActive(false);
                    break;
            }
        }
    }
}
