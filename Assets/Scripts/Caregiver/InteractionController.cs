using UnityEngine;
using UnityEngine.InputSystem;

namespace MagesDementiaGame
{
    [RequireComponent(typeof(TopDownPlayerController))]
    public sealed class InteractionController : MonoBehaviour
    {
        [SerializeField] private float forwardOffset = 0.65f;
        [SerializeField] private float interactionRadius = 0.8f;

        private TopDownPlayerController player;
        private CaregiverSceneController sceneController;
        private InputAction interactAction;
        private IInteractable currentInteractable;

        public void Initialize(CaregiverSceneController controller)
        {
            sceneController = controller;
        }

        private void Awake()
        {
            player = GetComponent<TopDownPlayerController>();
            interactAction = InputSystem.actions?.FindAction("Player/Interact");
            interactAction?.Enable();
        }

        private void Update()
        {
            if (sceneController == null || sceneController.IsModalOpen)
            {
                SetCurrent(null);
                return;
            }

            FindNearestInteractable();
            if (currentInteractable != null && currentInteractable.CanInteract && WasInteractPressed())
            {
                currentInteractable.Interact(this);
            }
        }

        private void FindNearestInteractable()
        {
            var center = (Vector2)transform.position + player.FacingDirection * forwardOffset;
            var hits = Physics2D.OverlapCircleAll(center, interactionRadius);
            IInteractable nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                var behaviours = hit.GetComponents<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (!(behaviour is IInteractable candidate))
                    {
                        continue;
                    }

                    var distance = ((Vector2)hit.transform.position - center).sqrMagnitude;
                    if (distance < nearestDistance)
                    {
                        nearest = candidate;
                        nearestDistance = distance;
                    }
                }
            }

            SetCurrent(nearest);
        }

        private void SetCurrent(IInteractable interactable)
        {
            currentInteractable = interactable;
            sceneController?.SetInteractionPrompt(interactable?.Prompt);
        }

        private bool WasInteractPressed()
        {
            if (interactAction != null)
            {
                return interactAction.WasPressedThisFrame();
            }

            return Keyboard.current?.eKey.wasPressedThisFrame == true ||
                   Gamepad.current?.buttonNorth.wasPressedThisFrame == true;
        }
    }
}
