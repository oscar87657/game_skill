// GOLDEN STANDARD
// 목적: 2.5D 프로토타입의 X/Y 평면 플레이어 이동을 담당한다.
// 책임: 이동 입력을 읽고 점프·대시 상태를 해결한 뒤 CharacterController를 이동시킨다.
// 불변식: Z 깊이는 고정되며 이동 능력이 전투나 애니메이션을 직접 소유하지 않는다.
// 선택 이유: 명시적인 타이머로 코요테 타임·버퍼·쿨다운·무적을 확인하고 테스트할 수 있다.
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
        [SerializeField, Min(0f)] private float dashSpeed = 8f;
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
        private float dashElapsed;
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
        public bool IsControlLocked { get; private set; }
        public int AirJumpsRemaining => airJumpsRemaining;
        public bool IsRunning { get; private set; }
        public float HorizontalSpeed => horizontalSpeed;
        public float NormalizedSpeed { get; private set; }
        public float VerticalSpeed => verticalSpeed;
        public float FacingDirection { get; private set; } = 1f;

        private void Awake()
        {
            // 첫 Update 전에 입력 액션과 카운터를 캐시하고 초기화한다.
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
            // Update는 타이머 → 입력 결정 → 속도 계산 → 충돌 이동 순서로 구성한다.
            float deltaTime = Time.deltaTime;
            if (IsControlLocked)
            {
                // 사망 연출 중에는 입력·중력·CharacterController 이동을 모두 멈춘다.
                IsRunning = false;
                NormalizedSpeed = 0f;
                return;
            }

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
            UpdateVerticalSpeed(deltaTime, !IsDashing);

            if (IsDashing)
            {
                IsRunning = false;
                verticalSpeed = 0f;
                float dashProgress = dashDuration <= Mathf.Epsilon
                    ? 1f
                    : Mathf.Clamp01(dashElapsed / dashDuration);
                float curveMultiplier = 0.72f
                    + 0.28f * Mathf.Sin(dashProgress * Mathf.PI);
                horizontalSpeed = dashDirection * dashSpeed * curveMultiplier;
                dashElapsed += deltaTime;
                UpdateFacing(dashDirection, deltaTime);
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

            if (isAirAttackHovering)
            {
                verticalSpeed = Mathf.MoveTowards(
                    verticalSpeed,
                    0f,
                    Mathf.Abs(gravity) * 2f * deltaTime);
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
            // 타이머를 0 아래로 내려가지 않게 하여 호출자가 단순한 > 0 검사를 사용할 수 있게 한다.
            dashTimer = Mathf.Max(0f, dashTimer - deltaTime);
            dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - deltaTime);
            invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - deltaTime);
        }

        private void TryStartDash(float horizontalInput)
        {
            // 대시는 누른 순간에만 시작하며 쿨다운이 끝났다고 홀드 입력으로 재시작하지 않는다.
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
            dashElapsed = 0f;
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
            // 전투는 짧은 수직 보정만 요청하고 수평 이동은 계속 Motor가 소유한다.
            if (!IsGrounded && !IsDashing)
            {
                airAttackHoverTimer = Mathf.Clamp(
                    duration,
                    0f,
                    Mathf.Max(duration, maximumDuration));
            }
        }

        public void StopAirAttackHover()
        {
            // 대시·착지·추후 경직이 전투를 끊을 때 명시적으로 취소할 수 있다.
            airAttackHoverTimer = 0f;
        }

        public void SetControlLocked(bool locked)
        {
            // 잠그는 순간 남은 관성과 대시 상태를 지워 재시작 지점에서 미끄러지지 않게 한다.
            IsControlLocked = locked;
            if (locked)
            {
                ResetMotionState();
            }
        }

        public void Teleport(Vector3 destination)
        {
            // CharacterController를 잠시 꺼야 충돌 해결이 목적지 변경을 이전 위치로 되돌리지 않는다.
            characterController ??= GetComponent<CharacterController>();
            bool wasEnabled = characterController.enabled;
            characterController.enabled = false;
            transform.position = destination;
            lockedDepth = destination.z;
            characterController.enabled = wasEnabled;
            ResetMotionState();
        }

        public void ResetMotionState()
        {
            // 재시작 시 프레임 타이머와 능력 횟수를 한곳에서 초기화해 부분 상태 잔존을 막는다.
            horizontalSpeed = 0f;
            verticalSpeed = 0f;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            dashTimer = 0f;
            dashElapsed = 0f;
            dashCooldownTimer = 0f;
            invulnerabilityTimer = 0f;
            airAttackHoverTimer = 0f;
            airJumpsRemaining = maxAirJumps;
            dashRunChainActive = false;
            airDashAvailable = true;
            IsRunning = false;
            NormalizedSpeed = 0f;
        }

        private void UpdateVerticalSpeed(float deltaTime, bool canJump)
        {
            // 이번 프레임 중력을 적분하기 전에 점프 버퍼와 코요테 타임을 해결한다.
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
            // 의미 있는 수평 의도를 따라가며 디자이너가 조정한 속도로 회전한다.
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
