using UnityEngine;
using UnityEngine.InputSystem;

namespace GameSkill
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class SideScrollerMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float runSpeed = 5.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 8f;
        [SerializeField, Min(0f)] private float groundAcceleration = 60f;
        [SerializeField, Min(0f)] private float airAcceleration = 35f;
        [SerializeField, Range(0f, 0.9f)] private float inputDeadZone = 0.1f;
        [SerializeField, Min(0f)] private float facingTurnSpeed = 1080f;

        [Header("Air")]
        [SerializeField, Min(0f)] private float jumpHeight = 2.2f;
        [SerializeField] private float gravity = -30f;
        [SerializeField] private float groundedGravity = -2f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        [Header("Dodge")]
        [SerializeField, Min(0f)] private float dodgeSpeed = 10f;
        [SerializeField, Min(0.01f)] private float dodgeDuration = 0.36f;
        [SerializeField, Min(0f)] private float dodgeCooldown = 0.55f;
        [SerializeField, Min(0f)] private float dodgeInvulnerabilityDuration = 0.24f;

        private CharacterController characterController;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction dodgeAction;
        private float horizontalSpeed;
        private float verticalSpeed;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float dodgeTimer;
        private float dodgeCooldownTimer;
        private float invulnerabilityTimer;
        private float dodgeDirection = 1f;
        private float lockedDepth;

        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public bool IsDodging => dodgeTimer > 0f;
        public bool IsInvulnerable => invulnerabilityTimer > 0f;
        public float HorizontalSpeed => horizontalSpeed;
        public float NormalizedSpeed { get; private set; }
        public float VerticalSpeed => verticalSpeed;
        public float FacingDirection { get; private set; } = 1f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            PlayerInput playerInput = GetComponent<PlayerInput>();

            moveAction = playerInput.actions.FindAction("Move", true);
            jumpAction = playerInput.actions.FindAction("Jump", true);
            sprintAction = playerInput.actions.FindAction("Sprint", false);
            dodgeAction = playerInput.actions.FindAction("Dodge", true);
            lockedDepth = transform.position.z;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            UpdateDodgeTimers(deltaTime);

            float horizontalInput = MovementMath.HorizontalInput(
                moveAction.ReadValue<Vector2>(),
                inputDeadZone);
            TryStartDodge(horizontalInput);
            UpdateVerticalSpeed(deltaTime, !IsDodging);

            if (IsDodging)
            {
                horizontalSpeed = dodgeDirection * dodgeSpeed;
                UpdateFacing(dodgeDirection, deltaTime);
            }
            else
            {
                bool isSprinting = sprintAction?.IsPressed() == true;
                float maximumSpeed = isSprinting ? sprintSpeed : runSpeed;
                float targetSpeed = horizontalInput * maximumSpeed;
                float acceleration = IsGrounded ? groundAcceleration : airAcceleration;

                horizontalSpeed = Mathf.MoveTowards(
                    horizontalSpeed,
                    targetSpeed,
                    acceleration * deltaTime);
                UpdateFacing(horizontalInput, deltaTime);
            }

            Vector3 velocity = new(horizontalSpeed, verticalSpeed, 0f);
            characterController.Move(velocity * deltaTime);

            Vector3 position = transform.position;
            position.z = lockedDepth;
            transform.position = position;

            NormalizedSpeed = sprintSpeed <= Mathf.Epsilon
                ? 0f
                : Mathf.Clamp01(Mathf.Abs(horizontalSpeed) / sprintSpeed);
        }

        private void UpdateDodgeTimers(float deltaTime)
        {
            dodgeTimer = Mathf.Max(0f, dodgeTimer - deltaTime);
            dodgeCooldownTimer = Mathf.Max(0f, dodgeCooldownTimer - deltaTime);
            invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - deltaTime);
        }

        private void TryStartDodge(float horizontalInput)
        {
            if (!dodgeAction.WasPressedThisFrame()
                || IsDodging
                || !IsGrounded
                || dodgeCooldownTimer > 0f)
            {
                return;
            }

            dodgeDirection = MovementMath.DodgeDirection(
                horizontalInput,
                FacingDirection);
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;
            invulnerabilityTimer = Mathf.Min(
                dodgeInvulnerabilityDuration,
                dodgeDuration);
            jumpBufferTimer = 0f;
        }

        private void UpdateVerticalSpeed(float deltaTime, bool canJump)
        {
            if (jumpAction.WasPressedThisFrame())
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer -= deltaTime;
            }

            if (IsGrounded)
            {
                coyoteTimer = coyoteTime;
            }
            else
            {
                coyoteTimer -= deltaTime;
            }

            if (IsGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = groundedGravity;
            }

            if (canJump && jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                verticalSpeed = MovementMath.JumpSpeed(jumpHeight, gravity);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }

            verticalSpeed += gravity * deltaTime;
        }

        private void UpdateFacing(float horizontalInput, float deltaTime)
        {
            if (Mathf.Abs(horizontalInput) <= Mathf.Epsilon)
            {
                return;
            }

            FacingDirection = Mathf.Sign(horizontalInput);
            Quaternion targetRotation = Quaternion.Euler(
                0f,
                MovementMath.SideScrollerFacingYaw(FacingDirection),
                0f);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                facingTurnSpeed * deltaTime);
        }
    }
}
