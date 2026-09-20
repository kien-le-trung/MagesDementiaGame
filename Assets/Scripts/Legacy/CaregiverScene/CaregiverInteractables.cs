using UnityEngine;

namespace MagesDementiaGame
{
    public sealed class TelevisionInteractable : MonoBehaviour, IInteractable
    {
        private CaregiverSceneController sceneController;

        public string Prompt => "[E] Adjust television";
        public bool CanInteract => sceneController != null;

        public void Initialize(CaregiverSceneController controller)
        {
            sceneController = controller;
        }

        public void Interact(InteractionController interactor)
        {
            sceneController.OpenEnvironmentChoices();
        }
    }

}
