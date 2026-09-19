using UnityEngine;

namespace MagesDementiaGame
{
    public sealed class TelevisionInteractable : MonoBehaviour, IInteractable
    {
        private CaregiverSceneController sceneController;

        public string Prompt => "[E] Adjust television";
        public bool CanInteract => true;

        public void Initialize(CaregiverSceneController controller)
        {
            sceneController = controller;
        }

        public void Interact(InteractionController interactor)
        {
            sceneController.OpenEnvironmentChoices();
        }
    }

    public sealed class MinhInteractable : MonoBehaviour, IInteractable
    {
        private CaregiverSceneController sceneController;
        private GameSession session;

        public string Prompt => CanInteract
            ? "[E] Approach Minh"
            : "Prepare the room before approaching Minh";

        public bool CanInteract => session != null &&
                                   session.SelectedEnvironmentChoice != EnvironmentChoice.NotChosen;

        public void Initialize(CaregiverSceneController controller)
        {
            sceneController = controller;
            session = GameSession.EnsureInstance();
        }

        public void Interact(InteractionController interactor)
        {
            sceneController.OpenMinhChoices();
        }
    }

    public sealed class ApproachMarkerInteractable : MonoBehaviour, IInteractable
    {
        private CaregiverSceneController sceneController;

        public string Prompt => "[E] Decide how to approach Minh";
        public bool CanInteract => sceneController != null && sceneController.CanChooseApproach;

        public void Initialize(CaregiverSceneController controller)
        {
            sceneController = controller;
        }

        public void Interact(InteractionController interactor)
        {
            sceneController.OpenApproachChoices();
        }
    }
}
