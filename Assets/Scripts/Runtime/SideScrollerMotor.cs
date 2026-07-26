using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

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
        [SerializeField, Range(0, 2)] private int maxAirJumps = 1;

        [Header("Dash")]
        [FormerlySerializedAs("dodgeSpeed")]
        [SerializeField, Min(0f)] private float dashSpeed = 9.5f;
        [FormerlySerializedAs("dodgeDuration")]
        [SerializeField, Min(0.01f)] private float dashDuration = 0.2f;
        [FormerlySerializedAs("dodgeCooldown")]
        [SerializeField, Min(0f)] private float dashCooldown = 0.48f;
        [FormerlySerializedAs("dodgeInvulnerabilityDuration")]
        [SerializeField, Min(0f)] private float dashInvulnerabilityDuration = 0.2f;

        private CharacterController characterController;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction dashAction;
        private float horizontalSpeed;
        private float verticalSpeed;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float dashTimer;
        private float dashCooldownTimer;
        private float invulnerabilityTimer;
        private float airAttackHoverTimer;
        private int airJumpsRemaining;
        private float dashDirection = 1f;
        private bool dashRunChainActive;
        private bool airDashAvailable = true;
        private float lockedDepth;

        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public bool IsDashing => dashTimer > 0f;
        public bool IsInvulnerable => invulnerabilityTimer > 0f;
        public bool IsAirAttackHovering => airAttackHoverTimer > 0f;
        public bool CanAirDash => airDashAvailable;
        public int AirJumpsRemaining => airJumpsRemaining;
        public bool IsRunning { get; private set; }
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
            dashAction = playerInput.actions.FindAction("Dash", true);
            airJumpsRemaining = maxAirJumps;
            lockedDepth = transform.position.z;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            UpdateDashTimers(deltaTime);
            airAttackHoverTimer = Mathf.Max(0f, airAttackHoverTimer - deltaTime);

            float horizontalInput = MovementMath.HorizontalInput(
                moveAction.ReadValue<Vector2>(),
                inputDeadZone);
            if (IsGrounded)
            {
                airDashAvailable = true;
                airJumpsRemaining = maxAirJumps;
            }

            if (dashAction.WasReleasedThisFrame())
            {
                dashRunChainActive = false;
            }

            TryStartDash(horizontalInput);
            bool isAirAttackHovering = IsAirAttackHovering && !IsDashing;
            UpdateVerticalSpeed(deltaTime, !IsDashing && !isAirAttackHovering);

            if (IsDashing)
            {
                IsRunning = false;
                verticalSpeed = 0f;
                horizontalSpeed = dashDirection * dashSpeed;
                UpdateFacing(dashDirection, deltaTime);
            }
            else if (isAirAttackHovering)
            {
                verticalSpeed = 0f;
            }
            else
            {
                IsRunning = dashRunChainActive && dashAction.IsPressed();
                float maximumSpeed = IsRunning ? sprintSpeed : runSpeed;
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

        private void UpdateDashTimers(float deltaTime)
        {
            dashTimer = Mathf.Max(0f, dashTimer - deltaTime);
            dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - deltaTime);
            invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - deltaTime);
        }

        private void TryStartDash(float horizontalInput)
        {
            if (!dashAction.WasPressedThisFrame()
                || IsDashing
                || dashCooldownTimer > 0f)
            {
                return;
            }

            bool isGrounded = IsGrounded;
            if (!isGrounded && !airDashAvailable)
            {
                return;
            }

            dashDirection = MovementMath.DodgeDirection(
                horizontalInput,
                FacingDirection);
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            invulnerabilityTimer = Mathf.Min(
                dashInvulnerabilityDuration,
                dashDuration);
            dashRunChainActive = true;
            jumpBufferTimer = 0f;
            if (!isGrounded)
            {
                airDashAvailable = false;
            }
        }

        public void RequestAirAttackHover(float duration, float maximumDuration)
        {
            if (!IsGrounded && !IsDashing)
            {
                airAttackHoverTimer = Mathf.Clamp(
                    airAttackHoverTimer + duration,
                    duration,
                    Mathf.Max(duration, maximumDuration));
            }
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

            bool canGroundJump = coyoteTimer > 0f;
            bool canAirJump = !IsGrounded && airJumpsRemaining > 0;
            if (canJump
                && jumpBufferTimer > 0f
                && (canGroundJump || canAirJump))
            {
                verticalSpeed = MovementMath.JumpSpeed(jumpHeight, gravity);
                if (canAirJump && !canGroundJump)
                {
                    airJumpsRemaining--;
                }

                airDashAvailable = true;
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
