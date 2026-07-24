using UnityEngine;
using UnityEngine.InputSystem;

namespace GameSkill
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class ThirdPersonMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7f;
        [SerializeField, Min(0f)] private float rotationSmoothTime = 0.08f;

        [Header("Air")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedGravity = -2f;

        private CharacterController characterController;
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private Transform cameraTransform;
        private float verticalSpeed;
        private float rotationVelocity;

        public bool IsGrounded => characterController != null && characterController.isGrounded;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();
            cameraTransform = Camera.main != null ? Camera.main.transform : null;

            moveAction = playerInput.actions.FindAction("Move", true);
            jumpAction = playerInput.actions.FindAction("Jump", true);
            sprintAction = playerInput.actions.FindAction("Sprint", true);
        }

        private void Update()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (cameraTransform == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            UpdateVerticalSpeed(deltaTime);

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            Vector3 direction = MovementMath.CameraRelativeDirection(
                moveInput,
                cameraTransform.forward,
                cameraTransform.right);

            if (direction.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref rotationVelocity,
                    rotationSmoothTime);

                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
            Vector3 velocity = direction * speed;
            velocity.y = verticalSpeed;
            characterController.Move(velocity * deltaTime);
        }

        private void UpdateVerticalSpeed(float deltaTime)
        {
            if (characterController.isGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = groundedGravity;
            }

            if (characterController.isGrounded && jumpAction.WasPressedThisFrame())
            {
                verticalSpeed = MovementMath.JumpSpeed(jumpHeight, gravity);
            }

            verticalSpeed += gravity * deltaTime;
        }
    }
}
