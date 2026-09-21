using UnityEngine;

namespace MagesDementiaGame
{
    public sealed class RecipientTelevisionInteractable : MonoBehaviour, IInteractable
    {
        private ResolutionSceneController controller;

        public string Prompt => "This interaction belongs to the retired recipient flow";
        public bool CanInteract => false;

        public void Initialize(ResolutionSceneController sceneController) => controller = sceneController;
        public void Interact(InteractionController interactor) { }
    }
}
