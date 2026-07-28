// GOLDEN STANDARD
// 목적: 2.5D 전투의 첫 근거리 적이 플레이어를 탐지·추적·공격하도록 제어한다.
// 책임: 순수 판단 결과를 상태 전환·수평 이동·중력·공격 데미지와 연결한다.
// 불변식: Z축은 생성 깊이에 고정하고 공격은 준비 시간이 끝난 순간 범위를 다시 확인하며 무적을 존중한다.
// 선택 이유: 판단과 Unity 실행을 분리한 작은 상태 머신은 원거리·돌진 적으로 확장하며 비교하기 쉽다.
using System;
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class MeleeEnemyController : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Renderer visualRenderer;

        [Header("Decision")]
        [SerializeField, Min(0f)] private float detectionRange = 6f;
        [SerializeField, Min(0f)] private float loseTargetRange = 8f;
        [SerializeField, Min(0f)] private float attackRange = 1.25f;
        [SerializeField, Min(0f)] private float verticalTolerance = 1.6f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 2.2f;
        [SerializeField] private float gravity = -25f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float attackWindup = 0.3f;
        [SerializeField, Min(0f)] private float attackRecovery = 0.55f;
        [SerializeField, Min(0f)] private float hitStunDuration = 0.16f;
        [SerializeField, Min(1)] private int attackDamage = 1;

        private CharacterController characterController;
        private Health ownHealth;
        private Health targetHealth;
        private SideScrollerMotor targetMotor;
        private float lockedDepth;
        private float verticalSpeed;
        private float stateTimer;
        private bool eventsSubscribed;
        private MaterialPropertyBlock visualProperties;

        public event Action<EnemyState, EnemyState> StateChanged;

        public EnemyState CurrentState { get; private set; } =
            EnemyState.Idle;
        public int FacingDirection { get; private set; } = -1;
        public int SuccessfulAttackCount { get; private set; }

        private void Awake()
        {
            // 필수 컴포넌트와 고정 깊이를 한 번 캐시해 매 프레임 탐색 비용을 없앤다.
            CacheComponents();
            lockedDepth = transform.position.z;
        }

        private void OnEnable()
        {
            // 비활성화 후 다시 켜지는 경우에도 Health 이벤트를 정확히 한 번만 연결한다.
            CacheComponents();
            SubscribeHealthEvents();
        }

        private void Start()
        {
            // 씬 역직렬화가 끝난 뒤 대상의 체력과 이동기를 확정한다.
            ResolveTargetComponents();
            ApplyStatePresentation(CurrentState);
        }

        private void Update()
        {
            // Unity 프레임 진입점은 테스트 가능한 Tick 함수에 시간만 전달한다.
            Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            // 파괴되거나 비활성화될 때 이벤트 연결을 해제해 유령 콜백을 막는다.
            UnsubscribeHealthEvents();
        }

        public bool Configure(
            Transform targetTransform,
            Renderer enemyRenderer)
        {
            // 빌더와 테스트가 같은 구성 경로를 사용하도록 씬 참조 설정을 공개한다.
            bool changed = target != targetTransform
                || visualRenderer != enemyRenderer;
            target = targetTransform;
            visualRenderer = enemyRenderer;
            CacheComponents();
            ResolveTargetComponents();
            return changed;
        }

        public void Tick(float deltaTime)
        {
            // 음수 시간은 상태 타이머를 역행시키므로 0으로 제한한다.
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (ownHealth == null || ownHealth.IsDead)
            {
                EnterState(EnemyState.Dead, 0f);
                return;
            }

            UpdateFacingDirection();

            switch (CurrentState)
            {
                case EnemyState.Hurt:
                    TickTimedState(safeDeltaTime, false);
                    break;
                case EnemyState.AttackWindup:
                    TickAttackWindup(safeDeltaTime);
                    break;
                case EnemyState.AttackRecovery:
                    TickTimedState(safeDeltaTime, false);
                    break;
                case EnemyState.Dead:
                    break;
                default:
                    TickLocomotion(safeDeltaTime);
                    break;
            }
        }

        private void TickLocomotion(float deltaTime)
        {
            // 순수 판단 함수가 선택한 상태를 실제 이동과 공격 준비 상태로 변환한다.
            EnemyState desiredState = ResolveDecision();
            if (desiredState != CurrentState)
            {
                float timer = desiredState == EnemyState.AttackWindup
                    ? attackWindup
                    : 0f;
                EnterState(desiredState, timer);
            }

            float horizontalSpeed = CurrentState == EnemyState.Chase
                ? FacingDirection * moveSpeed
                : 0f;
            ApplyMovement(horizontalSpeed, deltaTime);
        }

        private void TickAttackWindup(float deltaTime)
        {
            // 준비 중에는 미끄러지지 않고 중력만 적용해 공격 예고를 읽기 쉽게 만든다.
            ApplyMovement(0f, deltaTime);
            stateTimer -= deltaTime;
            if (stateTimer > 0f)
            {
                return;
            }

            TryDamageTarget();
            EnterState(
                EnemyState.AttackRecovery,
                attackRecovery);
        }

        private void TickTimedState(
            float deltaTime,
            bool allowHorizontalMovement)
        {
            // 피격·후딜 타이머가 끝나기 전에는 새 판단으로 상태를 덮어쓰지 않는다.
            float horizontalSpeed = allowHorizontalMovement
                ? FacingDirection * moveSpeed
                : 0f;
            ApplyMovement(horizontalSpeed, deltaTime);
            stateTimer -= deltaTime;
            if (stateTimer <= 0f)
            {
                EnterState(ResolveDecision(), 0f);
            }
        }

        private EnemyState ResolveDecision()
        {
            // 대상 좌표를 얻을 수 없을 때는 거리를 무한대로 취급해 안전하게 대기한다.
            bool targetAvailable = IsTargetAvailable();
            float horizontalDistance = targetAvailable
                ? target.position.x - transform.position.x
                : float.PositiveInfinity;
            float verticalDistance = targetAvailable
                ? target.position.y - transform.position.y
                : float.PositiveInfinity;
            return EnemyDecisionMath.ResolveLocomotionState(
                CurrentState,
                targetAvailable,
                horizontalDistance,
                verticalDistance,
                detectionRange,
                loseTargetRange,
                attackRange,
                verticalTolerance);
        }

        private void TryDamageTarget()
        {
            // 준비 동작 사이에 플레이어가 빠져나갈 수 있으므로 타격 프레임에 거리를 다시 잰다.
            bool targetAvailable = IsTargetAvailable();
            float horizontalDistance = targetAvailable
                ? target.position.x - transform.position.x
                : float.PositiveInfinity;
            float verticalDistance = targetAvailable
                ? target.position.y - transform.position.y
                : float.PositiveInfinity;
            if (!EnemyDecisionMath.IsInsideAttackRange(
                    targetAvailable,
                    horizontalDistance,
                    verticalDistance,
                    attackRange,
                    verticalTolerance))
            {
                return;
            }

            bool isInvulnerable =
                targetMotor != null && targetMotor.IsInvulnerable;
            if (DamageRules.TryApply(
                    targetHealth,
                    isInvulnerable,
                    attackDamage))
            {
                SuccessfulAttackCount++;
            }
        }

        private void ApplyMovement(
            float horizontalSpeed,
            float deltaTime)
        {
            // CharacterController가 비활성화된 사망 상태에서는 물리 이동을 호출하지 않는다.
            if (characterController == null
                || !characterController.enabled
                || deltaTime <= 0f)
            {
                return;
            }

            if (characterController.isGrounded
                && verticalSpeed < 0f)
            {
                // 작은 하향 속도로 지면 접촉을 유지해 계단 가장자리에서 튀는 현상을 줄인다.
                verticalSpeed = -2f;
            }
            else
            {
                verticalSpeed += gravity * deltaTime;
            }

            Vector3 motion = new(
                horizontalSpeed * deltaTime,
                verticalSpeed * deltaTime,
                0f);
            characterController.Move(motion);
            Vector3 lockedPosition = transform.position;
            lockedPosition.z = lockedDepth;
            transform.position = lockedPosition;
        }

        private void UpdateFacingDirection()
        {
            // 대상이 정확히 같은 X에 있으면 마지막 방향을 유지해 시각이 떨리지 않게 한다.
            if (!IsTargetAvailable())
            {
                return;
            }

            float horizontal = target.position.x - transform.position.x;
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                FacingDirection = horizontal > 0f ? 1 : -1;
            }
        }

        private bool IsTargetAvailable()
        {
            // 사망한 플레이어를 계속 추적하거나 공격하지 않는다.
            return target != null
                && targetHealth != null
                && !targetHealth.IsDead;
        }

        private void CacheComponents()
        {
            // RequireComponent를 사용하더라도 에디터 테스트 생명주기에 대비해 null일 때만 캐시한다.
            characterController ??=
                GetComponent<CharacterController>();
            ownHealth ??= GetComponent<Health>();
        }

        private void ResolveTargetComponents()
        {
            // 대상 루트에서 공통 전투 계약을 캐시해 공격 시 반복 탐색하지 않는다.
            targetHealth = target != null
                ? target.GetComponent<Health>()
                : null;
            targetMotor = target != null
                ? target.GetComponent<SideScrollerMotor>()
                : null;
        }

        private void SubscribeHealthEvents()
        {
            // 중복 구독은 한 번의 피격이 여러 상태 전환을 만드는 원인이므로 플래그로 차단한다.
            if (eventsSubscribed || ownHealth == null)
            {
                return;
            }

            ownHealth.Damaged += HandleDamaged;
            ownHealth.Died += HandleDied;
            eventsSubscribed = true;
        }

        private void UnsubscribeHealthEvents()
        {
            // 이미 해제됐거나 Health가 없는 경우에는 아무 작업도 하지 않는다.
            if (!eventsSubscribed || ownHealth == null)
            {
                return;
            }

            ownHealth.Damaged -= HandleDamaged;
            ownHealth.Died -= HandleDied;
            eventsSubscribed = false;
        }

        private void HandleDamaged(
            int currentHealth,
            int maximumHealth)
        {
            // 남은 체력 값은 Health가 소유하고 적은 생존 중 피격 경직만 시작한다.
            if (currentHealth > 0)
            {
                EnterState(EnemyState.Hurt, hitStunDuration);
            }
        }

        private void HandleDied()
        {
            // 사망 이벤트를 받는 즉시 이동과 렌더링을 중단한다.
            EnterState(EnemyState.Dead, 0f);
        }

        private void EnterState(
            EnemyState nextState,
            float duration)
        {
            // 같은 상태 재진입은 공격 준비 타이머를 매 프레임 초기화하므로 무시한다.
            if (CurrentState == nextState)
            {
                return;
            }

            EnemyState previousState = CurrentState;
            CurrentState = nextState;
            stateTimer = Mathf.Max(0f, duration);

            if (nextState == EnemyState.Dead)
            {
                if (characterController != null)
                {
                    characterController.enabled = false;
                }

                if (visualRenderer != null)
                {
                    visualRenderer.enabled = false;
                }
            }

            ApplyStatePresentation(nextState);
            StateChanged?.Invoke(previousState, nextState);
        }

        private void ApplyStatePresentation(
            EnemyState state)
        {
            // 공유 Material을 복제하지 않고 이 적의 상태만 색으로 표시해 프로토타입 판독성을 높인다.
            if (visualRenderer == null)
            {
                return;
            }

            visualRenderer.enabled =
                state != EnemyState.Dead;
            if (state == EnemyState.Dead)
            {
                return;
            }

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

        private static Color ResolveStateColor(
            EnemyState state)
        {
            // 공격 선딜의 빨강과 피격의 흰색을 강하게 구분해 애니메이션 전에도 상태를 읽게 한다.
            return state switch
            {
                EnemyState.Chase =>
                    new Color(1f, 0.72f, 0.12f, 1f),
                EnemyState.AttackWindup =>
                    new Color(1f, 0.12f, 0.06f, 1f),
                EnemyState.AttackRecovery =>
                    new Color(0.62f, 0.24f, 0.07f, 1f),
                EnemyState.Hurt =>
                    Color.white,
                _ =>
                    new Color(1f, 0.45f, 0.12f, 1f)
            };
        }

        private void OnDrawGizmosSelected()
        {
            // 선택한 적의 탐지·공격 반경을 씬 뷰에서 비교할 수 있게 표시한다.
            Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = new Color(1f, 0.15f, 0.1f, 0.55f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
