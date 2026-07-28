// GOLDEN STANDARD
// 목적: 예고한 방향으로 빠르게 돌진해 점프와 대시 회피를 요구하는 지상 적을 제어한다.
// 책임: 탐지·방향 잠금·돌진 이동·접촉 데미지·발판 끝 정지·피격 중단·재시작 복원을 연결한다.
// 불변식: Z축과 최초 배치를 유지하고 한 번의 돌진은 플레이어와 최대 한 번만 접촉 판정한다.
// 선택 이유: 방향 판단과 실행 이동을 분리해 추적형·투사체형 적과 다른 상태 중단 전략을 비교할 수 있다.
using System;
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class ChargeEnemyController : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Renderer directionIndicatorRenderer;

        [Header("Decision")]
        [SerializeField, Min(0f)] private float detectionRange = 6.5f;
        [SerializeField, Min(0f)] private float loseTargetRange = 7.5f;
        [SerializeField, Min(0f)] private float verticalTolerance = 1.7f;

        [Header("Charge")]
        [SerializeField, Min(0f)] private float attackWindup = 0.55f;
        [SerializeField, Min(0f)] private float chargeSpeed = 8.5f;
        [SerializeField, Min(0.01f)] private float maximumChargeDuration = 0.55f;
        [SerializeField, Min(0f)] private float attackRecovery = 0.75f;
        [SerializeField, Min(0f)] private float contactHorizontalRange = 0.85f;
        [SerializeField, Min(0f)] private float contactVerticalRange = 1.25f;
        [SerializeField, Min(1)] private int contactDamage = 1;

        [Header("Movement")]
        [SerializeField] private float gravity = -25f;
        [SerializeField, Min(0f)] private float hitStunDuration = 0.18f;
        [SerializeField, Min(0.05f)] private float groundProbeDistance = 0.85f;
        [SerializeField] private LayerMask environmentLayers = 1 << 0;

        private CharacterController characterController;
        private Health ownHealth;
        private Health targetHealth;
        private SideScrollerMotor targetMotor;
        private PlayerRespawnController targetRespawnController;
        private Vector3 initialSpawnPosition;
        private float lockedDepth;
        private float verticalSpeed;
        private float stateTimer;
        private int chargeDirection = -1;
        private bool contactConsumed;
        private bool healthEventsSubscribed;
        private bool targetEventsSubscribed;
        private bool initialSpawnCaptured;
        private MaterialPropertyBlock visualProperties;
        private MaterialPropertyBlock indicatorProperties;

        public event Action<EnemyState, EnemyState> StateChanged;

        public EnemyState CurrentState { get; private set; } =
            EnemyState.Idle;
        public int FacingDirection { get; private set; } = -1;
        public int ChargeDirection => chargeDirection;
        public int StartedChargeCount { get; private set; }
        public int SuccessfulChargeHitCount { get; private set; }

        private void Awake()
        {
            // 필수 컴포넌트와 최초 배치를 상태 판단보다 먼저 캐시한다.
            CacheComponents();
            CaptureInitialSpawn();
        }

        private void OnEnable()
        {
            // 활성 수명에 맞춰 적 체력과 플레이어 재시작 이벤트를 정확히 한 번 연결한다.
            CacheComponents();
            ResolveTargetComponents();
            SubscribeHealthEvents();
            SubscribeTargetEvents();
        }

        private void Start()
        {
            // 씬 역직렬화가 끝난 참조를 다시 확인하고 대기 상태 표현을 적용한다.
            ResolveTargetComponents();
            SubscribeTargetEvents();
            ApplyStatePresentation(CurrentState);
        }

        private void Update()
        {
            // Unity 프레임 진입점은 테스트 가능한 Tick에 시간만 전달한다.
            Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            // 비활성화 뒤 이전 Health와 플레이어가 이 적의 상태를 바꾸지 못하도록 구독을 해제한다.
            UnsubscribeHealthEvents();
            UnsubscribeTargetEvents();
        }

        public bool Configure(
            Transform targetTransform,
            Renderer enemyRenderer,
            Renderer chargeDirectionRenderer)
        {
            // 빌더와 테스트가 같은 참조 설정 경로를 사용하고 실제 변경 여부만 씬 저장에 전달한다.
            if (target != targetTransform)
            {
                // 대상 교체 전에 이전 플레이어의 재시작 이벤트부터 해제한다.
                UnsubscribeTargetEvents();
            }

            bool changed =
                target != targetTransform
                || visualRenderer != enemyRenderer
                || directionIndicatorRenderer
                    != chargeDirectionRenderer;
            target = targetTransform;
            visualRenderer = enemyRenderer;
            directionIndicatorRenderer =
                chargeDirectionRenderer;
            CacheComponents();
            CaptureInitialSpawn();
            ResolveTargetComponents();
            SubscribeTargetEvents();
            ApplyStatePresentation(CurrentState);
            return changed;
        }

        public void Tick(float deltaTime)
        {
            // 음수 시간은 상태 타이머와 중력을 역행시키므로 0으로 제한한다.
            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);
            if (ownHealth == null || ownHealth.IsDead)
            {
                EnterState(EnemyState.Dead, 0f);
                return;
            }

            switch (CurrentState)
            {
                case EnemyState.AttackWindup:
                    TickAttackWindup(safeDeltaTime);
                    break;
                case EnemyState.Charge:
                    TickCharge(safeDeltaTime);
                    break;
                case EnemyState.AttackRecovery:
                case EnemyState.Hurt:
                    TickTimedState(safeDeltaTime);
                    break;
                case EnemyState.Dead:
                    break;
                default:
                    TickIdle(safeDeltaTime);
                    break;
            }
        }

        public void ResetToSpawn()
        {
            // 플레이어 재시작 시 위치·체력·상태·돌진별 기록을 최초 배치와 같은 상태로 복원한다.
            CacheComponents();
            if (characterController != null)
            {
                // 활성 CharacterController를 잠시 끄면 충돌 보정 없이 정확한 위치로 되돌릴 수 있다.
                characterController.enabled = false;
            }

            transform.position = initialSpawnPosition;
            lockedDepth = initialSpawnPosition.z;
            if (characterController != null)
            {
                characterController.enabled = true;
            }

            verticalSpeed = 0f;
            stateTimer = 0f;
            contactConsumed = false;
            FacingDirection = -1;
            chargeDirection = -1;
            StartedChargeCount = 0;
            SuccessfulChargeHitCount = 0;
            ownHealth?.RestoreFullHealth();

            EnemyState previousState = CurrentState;
            CurrentState = EnemyState.Idle;
            ApplyStatePresentation(CurrentState);
            if (previousState != CurrentState)
            {
                StateChanged?.Invoke(
                    previousState,
                    CurrentState);
            }
        }

        private void TickIdle(float deltaTime)
        {
            // 대기 중에는 중력으로 발판을 유지하면서 대상 방향과 새 공격 기회를 평가한다.
            UpdateFacingDirection();
            ApplyMovement(0f, deltaTime);
            EnemyState desiredState = ResolveDecision();
            if (desiredState == EnemyState.AttackWindup)
            {
                EnterState(
                    EnemyState.AttackWindup,
                    attackWindup);
            }
        }

        private void TickAttackWindup(
            float deltaTime)
        {
            // 예고 중에는 이동하지 않되 플레이어가 공격 유지 범위를 벗어나면 돌진을 취소한다.
            ApplyMovement(0f, deltaTime);
            UpdateFacingDirection();
            if (ResolveDecision()
                != EnemyState.AttackWindup)
            {
                EnterState(EnemyState.Idle, 0f);
                return;
            }

            stateTimer -= deltaTime;
            if (stateTimer > 0f)
            {
                return;
            }

            float horizontalDistance =
                target.position.x - transform.position.x;
            chargeDirection =
                ChargeEnemyDecisionMath.ResolveChargeDirection(
                    horizontalDistance,
                    FacingDirection);
            FacingDirection = chargeDirection;
            contactConsumed = false;
            StartedChargeCount++;
            EnterState(
                EnemyState.Charge,
                maximumChargeDuration);
        }

        private void TickCharge(float deltaTime)
        {
            // 돌진 방향은 시작 순간 값으로 고정하고 발판 끝이나 벽을 만나면 즉시 후딜로 전환한다.
            if (stateTimer <= 0f
                || !HasGroundAhead())
            {
                EnterState(
                    EnemyState.AttackRecovery,
                    attackRecovery);
                return;
            }

            CollisionFlags collisionFlags =
                ApplyMovement(
                    chargeDirection * chargeSpeed,
                    deltaTime);
            TryDamageTarget();
            stateTimer -= deltaTime;
            if ((collisionFlags
                    & CollisionFlags.Sides) != 0
                || stateTimer <= 0f)
            {
                EnterState(
                    EnemyState.AttackRecovery,
                    attackRecovery);
            }
        }

        private void TickTimedState(
            float deltaTime)
        {
            // 피격 경직과 돌진 후딜 중에는 수평 이동 없이 중력만 적용하고 새 판단을 미룬다.
            ApplyMovement(0f, deltaTime);
            stateTimer -= deltaTime;
            if (stateTimer <= 0f)
            {
                EnterState(EnemyState.Idle, 0f);
            }
        }

        private EnemyState ResolveDecision()
        {
            // 대상이 없으면 무한 거리로 처리해 순수 판단 함수가 안전한 Idle을 반환하게 한다.
            bool targetAvailable = IsTargetAvailable();
            float horizontalDistance =
                targetAvailable
                    ? target.position.x
                        - transform.position.x
                    : float.PositiveInfinity;
            float verticalDistance =
                targetAvailable
                    ? target.position.y
                        - transform.position.y
                    : float.PositiveInfinity;
            return ChargeEnemyDecisionMath.ResolveAttackState(
                CurrentState,
                targetAvailable,
                horizontalDistance,
                verticalDistance,
                detectionRange,
                loseTargetRange,
                verticalTolerance);
        }

        private CollisionFlags ApplyMovement(
            float horizontalSpeed,
            float deltaTime)
        {
            // CharacterController 한 번의 Move에 수평 돌진·중력·깊이 보정을 합쳐 충돌 결과를 일관되게 얻는다.
            if (characterController == null
                || !characterController.enabled)
            {
                return CollisionFlags.None;
            }

            if (characterController.isGrounded
                && verticalSpeed < 0f)
            {
                verticalSpeed = -2f;
            }
            else
            {
                verticalSpeed += gravity * deltaTime;
            }

            Vector3 displacement =
                new(
                    horizontalSpeed * deltaTime,
                    verticalSpeed * deltaTime,
                    lockedDepth - transform.position.z);
            CollisionFlags result =
                characterController.Move(displacement);
            if ((result & CollisionFlags.Below) != 0)
            {
                verticalSpeed = -2f;
            }

            return result;
        }

        private bool HasGroundAhead()
        {
            // 몸 앞 발밑에 환경 Collider가 있는지 검사해 발판 밖으로 떨어지는 돌진을 막는다.
            if (characterController == null)
            {
                return false;
            }

            float horizontalOffset =
                characterController.radius + 0.18f;
            Vector3 origin =
                transform.position
                + Vector3.up * 0.3f
                + Vector3.right
                    * chargeDirection
                    * horizontalOffset;
            return Physics.Raycast(
                origin,
                Vector3.down,
                groundProbeDistance,
                environmentLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void TryDamageTarget()
        {
            // 한 돌진에서 접촉 판정을 소비했다면 무적 여부와 관계없이 같은 몸에 늦은 중복 데미지를 주지 않는다.
            if (contactConsumed)
            {
                return;
            }

            bool targetAvailable = IsTargetAvailable();
            float horizontalDistance =
                targetAvailable
                    ? target.position.x
                        - transform.position.x
                    : float.PositiveInfinity;
            float verticalDistance =
                targetAvailable
                    ? target.position.y
                        - transform.position.y
                    : float.PositiveInfinity;
            if (!ChargeEnemyDecisionMath.IsInsideContactWindow(
                    targetAvailable,
                    horizontalDistance,
                    verticalDistance,
                    contactHorizontalRange,
                    contactVerticalRange))
            {
                return;
            }

            contactConsumed = true;
            bool isInvulnerable =
                targetMotor != null
                && targetMotor.IsInvulnerable;
            if (DamageRules.TryApply(
                    targetHealth,
                    isInvulnerable,
                    contactDamage))
            {
                SuccessfulChargeHitCount++;
            }
        }

        private void UpdateFacingDirection()
        {
            // 같은 X 좌표에서는 마지막 방향을 유지해 표시기가 좌우로 떨리지 않게 한다.
            if (!IsTargetAvailable())
            {
                return;
            }

            float horizontalDistance =
                target.position.x - transform.position.x;
            FacingDirection =
                ChargeEnemyDecisionMath.ResolveChargeDirection(
                    horizontalDistance,
                    FacingDirection);
            UpdateDirectionIndicator();
        }

        private bool IsTargetAvailable()
        {
            // 사망한 플레이어를 향해서는 새 돌진을 준비하거나 접촉 데미지를 적용하지 않는다.
            return target != null
                && targetHealth != null
                && !targetHealth.IsDead;
        }

        private void CacheComponents()
        {
            // RequireComponent 참조도 EditMode 구성 순서를 고려해 필요할 때 직접 캐시한다.
            characterController ??=
                GetComponent<CharacterController>();
            ownHealth ??= GetComponent<Health>();
        }

        private void CaptureInitialSpawn()
        {
            // Awake가 실행되지 않는 EditMode에서도 Configure 시점의 최초 배치를 한 번만 기록한다.
            if (initialSpawnCaptured)
            {
                return;
            }

            initialSpawnPosition = transform.position;
            lockedDepth = initialSpawnPosition.z;
            initialSpawnCaptured = true;
        }

        private void ResolveTargetComponents()
        {
            // 돌진 데미지·무적·재시작에 필요한 플레이어 계약을 대상 루트에서 한 번 캐시한다.
            targetHealth = target != null
                ? target.GetComponent<Health>()
                : null;
            targetMotor = target != null
                ? target.GetComponent<SideScrollerMotor>()
                : null;
            targetRespawnController = target != null
                ? target.GetComponent<PlayerRespawnController>()
                : null;
        }

        private void SubscribeHealthEvents()
        {
            // 중복 구독은 한 번의 피격을 여러 상태 전환으로 만들므로 플래그로 차단한다.
            if (healthEventsSubscribed
                || ownHealth == null)
            {
                return;
            }

            ownHealth.Damaged += HandleDamaged;
            ownHealth.Died += HandleDied;
            healthEventsSubscribed = true;
        }

        private void UnsubscribeHealthEvents()
        {
            // 비활성화 뒤 이전 Health 이벤트가 돌진 상태를 바꾸지 못하도록 연결을 해제한다.
            if (!healthEventsSubscribed
                || ownHealth == null)
            {
                return;
            }

            ownHealth.Damaged -= HandleDamaged;
            ownHealth.Died -= HandleDied;
            healthEventsSubscribed = false;
        }

        private void SubscribeTargetEvents()
        {
            // 플레이어 재시작 완료 이벤트를 한 번만 구독해 적 초기화가 중복되지 않게 한다.
            if (targetEventsSubscribed
                || targetRespawnController == null)
            {
                return;
            }

            targetRespawnController.Respawned +=
                HandleTargetRespawned;
            targetEventsSubscribed = true;
        }

        private void UnsubscribeTargetEvents()
        {
            // 대상 교체나 씬 종료 뒤 이전 플레이어가 이 적을 초기화하지 못하도록 연결을 해제한다.
            if (!targetEventsSubscribed
                || targetRespawnController == null)
            {
                targetEventsSubscribed = false;
                return;
            }

            targetRespawnController.Respawned -=
                HandleTargetRespawned;
            targetEventsSubscribed = false;
        }

        private void HandleDamaged(
            int currentHealth,
            int maximumHealth)
        {
            // 생존 중 피격은 예고와 돌진을 모두 취소해 플레이어 공격으로 상태를 중단할 수 있게 한다.
            if (currentHealth > 0)
            {
                EnterState(
                    EnemyState.Hurt,
                    hitStunDuration);
            }
        }

        private void HandleDied()
        {
            // 사망 즉시 이동 충돌과 시각 표현을 모두 중단한다.
            EnterState(EnemyState.Dead, 0f);
        }

        private void HandleTargetRespawned(
            Vector3 respawnPosition)
        {
            // 플레이어 도착 위치와 무관하게 돌진 적은 자신이 기록한 최초 배치로 돌아간다.
            ResetToSpawn();
        }

        private void EnterState(
            EnemyState nextState,
            float duration)
        {
            // 같은 상태 재진입은 선딜과 돌진 타이머를 매 프레임 초기화하므로 무시한다.
            if (CurrentState == nextState)
            {
                return;
            }

            EnemyState previousState = CurrentState;
            CurrentState = nextState;
            stateTimer = Mathf.Max(0f, duration);
            if (nextState == EnemyState.Dead
                && characterController != null)
            {
                characterController.enabled = false;
            }

            ApplyStatePresentation(nextState);
            StateChanged?.Invoke(
                previousState,
                nextState);
        }

        private void ApplyStatePresentation(
            EnemyState state)
        {
            // 실제 모델과 애니메이션 전에는 몸 색과 방향 표시기로 행동 상태를 읽게 한다.
            if (visualRenderer != null)
            {
                visualRenderer.enabled =
                    state != EnemyState.Dead;
                if (visualRenderer.enabled)
                {
                    visualProperties ??=
                        new MaterialPropertyBlock();
                    visualRenderer.GetPropertyBlock(
                        visualProperties);
                    visualProperties.SetColor(
                        BaseColorId,
                        ResolveStateColor(state));
                    visualRenderer.SetPropertyBlock(
                        visualProperties);
                }
            }

            UpdateDirectionIndicator();
        }

        private void UpdateDirectionIndicator()
        {
            // 노란 막대는 선딜 방향을, 붉은 막대는 현재 돌진 방향을 나타낸다.
            if (directionIndicatorRenderer == null)
            {
                return;
            }

            bool isVisible =
                CurrentState == EnemyState.AttackWindup
                || CurrentState == EnemyState.Charge;
            directionIndicatorRenderer.enabled =
                isVisible;
            if (!isVisible)
            {
                return;
            }

            int displayDirection =
                CurrentState == EnemyState.Charge
                    ? chargeDirection
                    : FacingDirection;
            directionIndicatorRenderer.transform.localPosition =
                new Vector3(
                    displayDirection * 0.95f,
                    0.65f,
                    0f);
            indicatorProperties ??=
                new MaterialPropertyBlock();
            directionIndicatorRenderer.GetPropertyBlock(
                indicatorProperties);
            indicatorProperties.SetColor(
                BaseColorId,
                CurrentState == EnemyState.Charge
                    ? new Color(1f, 0.08f, 0.04f, 1f)
                    : new Color(1f, 0.9f, 0.08f, 1f));
            directionIndicatorRenderer.SetPropertyBlock(
                indicatorProperties);
        }

        private static Color ResolveStateColor(
            EnemyState state)
        {
            // 청록색 계열 몸에 노랑·빨강 공격 상태를 사용해 기존 근거리·원거리 적과 구분한다.
            return state switch
            {
                EnemyState.AttackWindup =>
                    new Color(0.95f, 0.7f, 0.08f, 1f),
                EnemyState.Charge =>
                    new Color(1f, 0.15f, 0.06f, 1f),
                EnemyState.AttackRecovery =>
                    new Color(0.08f, 0.32f, 0.4f, 1f),
                EnemyState.Hurt =>
                    Color.white,
                _ =>
                    new Color(0.08f, 0.75f, 0.72f, 1f)
            };
        }

        private void OnDrawGizmosSelected()
        {
            // 선택한 적의 탐지 범위와 돌진 최대 거리를 Scene 뷰에서 비교할 수 있게 표시한다.
            Gizmos.color =
                new Color(0.05f, 0.85f, 0.8f, 0.3f);
            Gizmos.DrawWireSphere(
                transform.position,
                detectionRange);
            Gizmos.color =
                new Color(1f, 0.15f, 0.05f, 0.45f);
            Gizmos.DrawLine(
                transform.position,
                transform.position
                + Vector3.right
                    * FacingDirection
                    * chargeSpeed
                    * maximumChargeDuration);
        }
    }
}
