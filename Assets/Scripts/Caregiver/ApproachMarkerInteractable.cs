using UnityEngine;

namespace MagesDementiaGame
{
    public sealed class ApproachMarkerInteractable : MonoBehaviour, IInteractable
    {
        private CaregiverSceneController sceneController;

        public string Prompt => "[E] Decide how to approach Minh";
        public bool CanInteract => sceneController != null && sceneController.CanChooseApproach;

        public void Initialize(CaregiverSceneController controller) => sceneController = controller;
        public void Interact(InteractionController interactor) => sceneController?.OpenApproachChoices();
    }
}
