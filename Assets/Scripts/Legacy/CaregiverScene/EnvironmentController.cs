using UnityEngine;

namespace MagesDementiaGame
{
    public sealed class EnvironmentController : MonoBehaviour
    {
        private RoomView roomView;
        private GameSession session;

        public void Initialize(RoomView view)
        {
            roomView = view;
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
            if (roomView == null || session == null)
            {
                return;
            }

            switch (session.SelectedEnvironmentChoice)
            {
                case EnvironmentChoice.LowerTelevision:
                    roomView.SetTelevisionState(TelevisionState.Lowered);
                    roomView.SetPhotoVisible(false);
                    break;
                case EnvironmentChoice.TurnOffTelevisionAndRestorePhoto:
                    roomView.SetTelevisionState(TelevisionState.Off);
                    roomView.SetPhotoVisible(true);
                    break;
                default:
                    roomView.SetTelevisionState(TelevisionState.On);
                    roomView.SetPhotoVisible(false);
                    break;
            }
        }
    }
}
