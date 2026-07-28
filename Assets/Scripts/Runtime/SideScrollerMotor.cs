// GOLDEN STANDARD
// 목적: 2.5D 프로토타입의 X/Y 평면 플레이어 이동을 담당한다.
// 책임: 이동 입력을 읽고 점프·대시 상태를 해결한 뒤 CharacterController를 이동시킨다.
// 불변식: Z 깊이는 고정되며 이동 능력이 전투나 애니메이션을 직접 소유하지 않는다.
// 선택 이유: 명시적인 타이머로 코요테 타임·버퍼·쿨다운·무적을 확인하고 테스트할 수 있다.
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace GameSkill
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class SideScrollerMotor : MonoBehaviour
    {
        // 순간 입력을 폴링하지 않는 표현·튜토리얼 계층에는 성공한 이동 이벤트만 공개한다.
        public event Action<float> DashStarted;
        public event Action Jumped;

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

        [Header("Ability Progression")]
        [SerializeField] private PlayerAbilityState abilityState;
        [SerializeField] private AbilityDefinition doubleJumpAbility;
        [SerializeField] private AbilityDefinition airDashAbility;
        [SerializeField] private AbilityDefinition wallTraversalAbility;

        [Header("Wall Traversal")]
        [SerializeField, Min(0f)] private float wallClingDuration = 0.22f;
        [SerializeField, Min(0f)] private float wallSlideSpeed = 2.4f;
        [SerializeField, Min(0f)] private float wallJumpHorizontalSpeed = 3.6f;
        [SerializeField, Min(0f)] private float wallJumpControlLockTime = 0.04f;
        [SerializeField, Range(0.5f, 1f)]
        private float minimumWallNormal = 0.75f;

        [Header("Dash")]
        [FormerlySerializedAs("dodgeSpeed")]
        [SerializeField, Min(0f)] private float dashSpeed = 8f;
        [FormerlySerializedAs("dodgeDuration")]
        [SerializeField, Min(0.01f)] private float dashDuration = 0.2f;
        [FormerlySerializedAs("dodgeCooldown")]
        [SerializeField, Min(0f)] private float dashCooldown = 0.48f;
        [FormerlySerializedAs("dodgeInvulnerabilityDuration")]
        [SerializeField, Min(0f)] private float dashInvulnerabilityDuration = 0.3f;

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
        private float wallClingTimer;
        private float wallJumpControlLockTimer;
        private float wallContactDirection;
        private int wallContactFrame = -100;
        private int airJumpsRemaining;
        private float dashDirection = 1f;
        private bool dashRunChainActive;
        private bool airDashAvailable = true;
        private float lockedDepth;

        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public bool IsDashing => dashTimer > 0f;
        public bool IsInvulnerable => invulnerabilityTimer > 0f;
        public bool IsAirAttackHovering => airAttackHoverTimer > 0f;
        public bool CanAirDash => airDashAvailable && IsAirDashUnlocked;
        public bool IsDoubleJumpUnlocked =>
            IsAbilityAvailable(doubleJumpAbility);
        public bool IsAirDashUnlocked =>
            IsAbilityAvailable(airDashAbility);
        public bool IsWallTraversalUnlocked =>
            IsAbilityAvailable(wallTraversalAbility);
        public bool IsWallClinging { get; private set; }
        public bool IsWallSliding { get; private set; }
        public float WallContactDirection =>
            HasRecentWallContact ? wallContactDirection : 0f;
        public bool IsControlLocked { get; private set; }
        public int AirJumpsRemaining => airJumpsRemaining;
        public bool IsRunning { get; private set; }
        public float HorizontalSpeed => horizontalSpeed;
        public float NormalizedSpeed { get; private set; }
        public float VerticalSpeed => verticalSpeed;
        public float FacingDirection { get; private set; } = 1f;
        public float DashDuration => dashDuration;
        public float DashInvulnerabilityDuration =>
            dashInvulnerabilityDuration;
        public float WallJumpHorizontalSpeed =>
            wallJumpHorizontalSpeed;
        public float WallJumpControlLockTime =>
            wallJumpControlLockTime;

        private void Awake()
        {
            // 첫 Update 전에 입력 액션과 카운터를 캐시하고 초기화한다.
            characterController = GetComponent<CharacterController>();
            PlayerInput playerInput = GetComponent<PlayerInput>();

            moveAction = playerInput.actions.FindAction("Move", true);
            jumpAction = playerInput.actions.FindAction("Jump", true);
            dashAction = playerInput.actions.FindAction("Dash", true);
            airJumpsRemaining = maxAirJumps;
            wallClingTimer = wallClingDuration;
            lockedDepth = transform.position.z;
        }

        public bool ConfigureAbilityRequirements(
            PlayerAbilityState state,
            AbilityDefinition requiredDoubleJump,
            AbilityDefinition requiredAirDash,
            AbilityDefinition requiredWallTraversal)
        {
            // 같은 참조로 다시 구성할 때 씬을 불필요하게 Dirty 상태로 만들지 않는다.
            if (abilityState == state
                && doubleJumpAbility == requiredDoubleJump
                && airDashAbility == requiredAirDash
                && wallTraversalAbility == requiredWallTraversal)
            {
                return false;
            }

            // 에디터 빌더가 이동 코드의 private 직렬화 필드를 우회하지 않고 진행 조건을 연결한다.
            abilityState = state;
            doubleJumpAbility = requiredDoubleJump;
            airDashAbility = requiredAirDash;
            wallTraversalAbility = requiredWallTraversal;
            return true;
        }

        public bool ConfigureDashTiming(
            float duration,
            float cooldown,
            float invulnerabilityDuration)
        {
            // 빌더와 테스트가 private 직렬화 필드를 우회하지 않고 같은 대시 시간 계약을 사용하게 한다.
            float safeDuration =
                Mathf.Max(0.01f, duration);
            float safeCooldown =
                Mathf.Max(0f, cooldown);
            float safeInvulnerabilityDuration =
                Mathf.Max(0f, invulnerabilityDuration);
            if (Mathf.Approximately(
                    dashDuration,
                    safeDuration)
                && Mathf.Approximately(
                    dashCooldown,
                    safeCooldown)
                && Mathf.Approximately(
                    dashInvulnerabilityDuration,
                    safeInvulnerabilityDuration))
            {
                return false;
            }

            dashDuration = safeDuration;
            dashCooldown = safeCooldown;
            dashInvulnerabilityDuration =
                safeInvulnerabilityDuration;
            return true;
        }

        public bool ConfigureWallJump(
            float horizontalJumpSpeed,
            float controlLockDuration)
        {
            // 반발 거리와 입력 복귀 시간을 함께 조정해 씬과 코드 기본값이 서로 어긋나지 않게 한다.
            float safeHorizontalSpeed =
                Mathf.Max(0f, horizontalJumpSpeed);
            float safeControlLockDuration =
                Mathf.Max(0f, controlLockDuration);
            if (Mathf.Approximately(
                    wallJumpHorizontalSpeed,
                    safeHorizontalSpeed)
                && Mathf.Approximately(
                    wallJumpControlLockTime,
                    safeControlLockDuration))
            {
                return false;
            }

            wallJumpHorizontalSpeed =
                safeHorizontalSpeed;
            wallJumpControlLockTime =
                safeControlLockDuration;
            return true;
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
            wallJumpControlLockTimer = Mathf.Max(
                0f,
                wallJumpControlLockTimer - deltaTime);

            float horizontalInput = MovementMath.HorizontalInput(
                moveAction.ReadValue<Vector2>(),
                inputDeadZone);
            if (IsGrounded)
            {
                // 착지는 모든 공중 능력과 벽 체공 시간을 다음 시도에 맞게 초기화한다.
                airDashAvailable = true;
                airJumpsRemaining = maxAirJumps;
                wallClingTimer = wallClingDuration;
            }

            if (dashAction.WasReleasedThisFrame())
            {
                dashRunChainActive = false;
            }

            TryStartDash(horizontalInput);
            bool isAirAttackHovering = IsAirAttackHovering && !IsDashing;
            UpdateVerticalSpeed(
                deltaTime,
                !IsDashing);
            UpdateWallTraversal(
                horizontalInput,
                deltaTime);

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

                // 벽 점프 직후 짧은 시간은 반발 속도를 보존하고 이후에 일반 공중 조작으로 복귀한다.
                if (wallJumpControlLockTimer <= 0f)
                {
                    horizontalSpeed = Mathf.MoveTowards(
                        horizontalSpeed,
                        targetSpeed,
                        acceleration * deltaTime);
                    UpdateFacing(horizontalInput, deltaTime);
                }
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
            // 공중에서는 충전 횟수와 진행 능력을 모두 만족해야 대시를 시작할 수 있다.
            if (!isGrounded
                && (!airDashAvailable || !IsAirDashUnlocked))
            {
                return;
            }

            dashDirection = MovementMath.DodgeDirection(
                horizontalInput,
                FacingDirection);
            dashTimer = dashDuration;
            dashElapsed = 0f;
            dashCooldownTimer = dashCooldown;
            // 이동이 끝난 직후에도 짧은 회피 여유를 허용하므로 무적 시간을 대시 이동 시간에 제한하지 않는다.
            invulnerabilityTimer =
                dashInvulnerabilityDuration;
            dashRunChainActive = true;
            jumpBufferTimer = 0f;
            if (!isGrounded)
            {
                airDashAvailable = false;
            }

            // 이동 상태를 소유하지 않는 VFX·SFX 계층에 확정된 대시 방향만 전달한다.
            DashStarted?.Invoke(
                dashDirection);
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
            wallClingTimer = wallClingDuration;
            wallJumpControlLockTimer = 0f;
            wallContactDirection = 0f;
            wallContactFrame = -100;
            dashRunChainActive = false;
            airDashAvailable = true;
            IsWallClinging = false;
            IsWallSliding = false;
            IsRunning = false;
            NormalizedSpeed = 0f;
        }

        private void UpdateVerticalSpeed(
            float deltaTime,
            bool canJump)
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
            bool canAirJump = !IsGrounded
                && airJumpsRemaining > 0
                && IsDoubleJumpUnlocked;
            bool canWallJump = !IsGrounded
                && HasRecentWallContact
                && IsWallTraversalUnlocked;
            if (canJump
                && jumpBufferTimer > 0f
                && (canGroundJump || canAirJump || canWallJump))
            {
                verticalSpeed = MovementMath.JumpSpeed(jumpHeight, gravity);

                // 벽 점프는 일반 공중 점프 횟수를 소비하지 않고 벽의 반대 방향으로 반발한다.
                if (canWallJump && !canGroundJump)
                {
                    horizontalSpeed =
                        WallTraversalMath.WallJumpHorizontalSpeed(
                            wallContactDirection,
                            wallJumpHorizontalSpeed);
                    wallJumpControlLockTimer =
                        wallJumpControlLockTime;
                    FacingDirection = -wallContactDirection;
                    wallContactFrame = -100;
                    IsWallClinging = false;
                    IsWallSliding = false;
                }
                else if (canAirJump && !canGroundJump)
                {
                    airJumpsRemaining--;
                }

                airDashAvailable = true;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                // 지상·공중·벽 점프 중 하나가 실제 승인된 뒤에만 튜토리얼 진행을 알린다.
                Jumped?.Invoke();
            }

            verticalSpeed += gravity * deltaTime;
        }

        private void UpdateWallTraversal(
            float horizontalInput,
            float deltaTime)
        {
            // 대시·지상·미해금 상태에서는 벽 이동 보정을 적용하지 않고 표시 상태도 즉시 해제한다.
            if (IsDashing
                || IsGrounded
                || !HasRecentWallContact
                || !IsWallTraversalUnlocked)
            {
                IsWallClinging = false;
                IsWallSliding = false;
                return;
            }

            bool isHoldingTowardWall =
                WallTraversalMath.IsHoldingTowardWall(
                    horizontalInput,
                    wallContactDirection,
                    inputDeadZone);
            if (!isHoldingTowardWall || verticalSpeed > 0f)
            {
                // 벽을 향한 의도를 놓거나 상승 중이면 일반 공중 이동을 유지한다.
                IsWallClinging = false;
                IsWallSliding = false;
                return;
            }

            if (wallClingTimer > 0f)
            {
                // 최초 접촉의 짧은 잡기 구간은 낙하를 멈춰 다음 벽 점프 입력 시간을 제공한다.
                wallClingTimer = Mathf.Max(
                    0f,
                    wallClingTimer - deltaTime);
                verticalSpeed = 0f;
                IsWallClinging = true;
                IsWallSliding = false;
                return;
            }

            // 잡기 시간이 끝나면 하강 속도만 제한해 벽에서 무한 체공하지 않게 한다.
            verticalSpeed =
                WallTraversalMath.ClampWallSlideSpeed(
                    verticalSpeed,
                    wallSlideSpeed);
            IsWallClinging = false;
            IsWallSliding = true;
        }

        private bool IsAbilityAvailable(AbilityDefinition requirement)
        {
            // 요구 에셋이 없는 기존 씬은 이전 동작을 유지하고, 명시된 요구 조건은 보유 상태로 판정한다.
            if (requirement == null || !requirement.IsConfigured)
            {
                return true;
            }

            return abilityState != null && abilityState.HasAbility(requirement);
        }

        private bool HasRecentWallContact =>
            Time.frameCount - wallContactFrame <= 1;

        private void OnControllerColliderHit(
            ControllerColliderHit hit)
        {
            // CharacterController 접촉 중 충분히 수직인 표면만 벽 이동 후보로 기록한다.
            if (!WallTraversalMath.IsWallSurface(
                hit.normal,
                minimumWallNormal))
            {
                return;
            }

            wallContactDirection = -Mathf.Sign(hit.normal.x);
            wallContactFrame = Time.frameCount;
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
