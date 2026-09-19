using UnityEngine;
using UnityEngine.InputSystem;

namespace MagesDementiaGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TopDownPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;

        private Rigidbody2D body;
        private InputAction moveAction;
        private Vector2 movement;

        public Vector2 FacingDirection { get; private set; } = Vector2.up;
        public bool MovementEnabled { get; private set; } = true;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            moveAction = InputSystem.actions?.FindAction("Player/Move");
            moveAction?.Enable();
        }

        private void Update()
        {
            if (!MovementEnabled)
            {
                movement = Vector2.zero;
                return;
            }

            movement = moveAction != null ? moveAction.ReadValue<Vector2>() : ReadFallbackInput();
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            if (movement.sqrMagnitude > 0.01f)
            {
                FacingDirection = movement.normalized;
            }
        }

        private void FixedUpdate()
        {
            body.MovePosition(body.position + movement * (moveSpeed * Time.fixedDeltaTime));
        }

        public void SetMovementEnabled(bool enabled)
        {
            MovementEnabled = enabled;
            if (!enabled)
            {
                movement = Vector2.zero;
            }
        }

        private static Vector2 ReadFallbackInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                             (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            var vertical = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                           (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(horizontal, vertical);
        }
    }
}
