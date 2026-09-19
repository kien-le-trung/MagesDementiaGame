using UnityEngine;

namespace MagesDementiaGame
{
    public sealed class RecipientPhotoInteractable : MonoBehaviour, IInteractable
    {
        private RecipientSceneController controller;

        public string Prompt => controller != null && controller.CanExaminePhotograph
            ? controller.PhotographIsVisible
                ? "[E] Examine the family photograph"
                : "[E] Examine the empty photograph space"
            : "The television noise pulls at your attention";
        public bool CanInteract => controller != null && controller.CanExaminePhotograph;

        public void Initialize(RecipientSceneController sceneController)
        {
            controller = sceneController;
        }

        public void Interact(InteractionController interactor)
        {
            controller.ExaminePhotograph();
        }
    }

    public sealed class RecipientTelevisionInteractable : MonoBehaviour, IInteractable
    {
        private RecipientSceneController controller;

        public string Prompt => controller != null && controller.CanNoticeTelevision
            ? "[E] Listen to the television"
            : "Something important is missing from the table";
        public bool CanInteract => controller != null && controller.CanNoticeTelevision;

        public void Initialize(RecipientSceneController sceneController)
        {
            controller = sceneController;
        }

        public void Interact(InteractionController interactor)
        {
            controller.NoticeTelevision();
        }
    }
}
