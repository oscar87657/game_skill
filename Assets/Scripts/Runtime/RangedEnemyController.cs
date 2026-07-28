// GOLDEN STANDARD
// 목적: 고정형 원거리 적이 플레이어를 탐지하고 예고 후 직선 투사체를 발사하도록 제어한다.
// 책임: 거리 판단·공격 선딜과 후딜·투사체 생성·피격·사망·플레이어 재시작 복원을 연결한다.
// 불변식: 발사 순간 유효한 대상만 조준하고 모든 투사체는 플레이어 재시작 때 제거된다.
// 선택 이유: 이동하는 근거리 적과 다른 판단 비용을 보여 주면서 공통 Health·DamageRules·EnemyState 계약을 재사용한다.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Collider))]
    public sealed class RangedEnemyController : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Renderer muzzleRenderer;
        [SerializeField] private Transform muzzleTransform;

        [Header("Decision")]
        [SerializeField, Min(0f)] private float detectionRange = 8f;
        [SerializeField, Min(0f)] private float loseTargetRange = 9.5f;
        [SerializeField, Min(0f)] private float verticalTolerance = 3.8f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float attackWindup = 0.8f;
        [SerializeField, Min(0f)] private float attackRecovery = 1.6f;
        [SerializeField, Min(0f)] private float hitStunDuration = 0.16f;
        [SerializeField, Min(0f)] private float projectileSpeed = 7f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 2.5f;
        [SerializeField, Min(1)] private int projectileDamage = 1;
        [SerializeField] private Material projectileMaterial;

        private readonly List<EnemyProjectile> activeProjectiles =
            new();
        private Health ownHealth;
        private Health targetHealth;
        private PlayerRespawnController targetRespawnController;
        private Collider bodyCollider;
        private Vector3 initialSpawnPosition;
        private float stateTimer;
        private bool healthEventsSubscribed;
        private bool targetEventsSubscribed;
        private bool initialSpawnCaptured;
        private MaterialPropertyBlock visualProperties;
        private MaterialPropertyBlock muzzleProperties;

        public event Action<EnemyState, EnemyState> StateChanged;

        public EnemyState CurrentState { get; private set; } =
            EnemyState.Idle;
        public int FacingDirection { get; private set; } = -1;
        public int FiredProjectileCount { get; private set; }
        public int ActiveProjectileCount =>
            activeProjectiles.Count;
        public float AttackWindupDuration => attackWindup;
        public float AttackRecoveryDuration => attackRecovery;

        private void Awake()
        {
            // 생존·몸 충돌체와 최초 배치를 발사 전에 캐시한다.
            CacheComponents();
            CaptureInitialSpawn();
        }

        private void OnEnable()
        {
            // 활성 수명에 맞춰 Health와 플레이어 재시작 이벤트를 각각 한 번만 구독한다.
            CacheComponents();
            ResolveTargetComponents();
            SubscribeHealthEvents();
            SubscribeTargetEvents();
        }

        private void Start()
        {
            // 씬 역직렬화가 끝난 참조를 다시 확인하고 초기 대기 표현을 적용한다.
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
            // Scene 종료 후 오래된 콜백과 날아가는 투사체가 남지 않게 모두 해제한다.
            UnsubscribeHealthEvents();
            UnsubscribeTargetEvents();
            CancelActiveProjectiles();
        }

        public bool Configure(
            Transform targetTransform,
            Renderer enemyRenderer,
            Renderer chargeRenderer,
            Transform projectileOrigin,
            Material runtimeProjectileMaterial)
        {
            // 빌더와 테스트가 같은 참조 설정 경로를 사용하고 변경 여부만 씬 저장에 전달한다.
            if (target != targetTransform)
            {
                UnsubscribeTargetEvents();
            }

            bool changed =
                target != targetTransform
                || visualRenderer != enemyRenderer
                || muzzleRenderer != chargeRenderer
                || muzzleTransform != projectileOrigin
                || projectileMaterial
                    != runtimeProjectileMaterial;
            target = targetTransform;
            visualRenderer = enemyRenderer;
            muzzleRenderer = chargeRenderer;
            muzzleTransform = projectileOrigin;
            projectileMaterial =
                runtimeProjectileMaterial;

            CacheComponents();
            CaptureInitialSpawn();
            ResolveTargetComponents();
            SubscribeTargetEvents();
            return changed;
        }

        public bool ConfigureAttackTiming(
            float windupDuration,
            float recoveryDuration)
        {
            // 발사 템포를 씬 빌더와 테스트가 같은 공개 경계에서 조정하도록 음수 시간을 제한한다.
            float safeWindup =
                Mathf.Max(0f, windupDuration);
            float safeRecovery =
                Mathf.Max(0f, recoveryDuration);
            if (Mathf.Approximately(
                    attackWindup,
                    safeWindup)
                && Mathf.Approximately(
                    attackRecovery,
                    safeRecovery))
            {
                return false;
            }

            attackWindup = safeWindup;
            attackRecovery = safeRecovery;
            return true;
        }

        public void Tick(float deltaTime)
        {
            // 음수 시간은 선딜과 후딜을 되돌리므로 0으로 제한한다.
            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);
            if (ownHealth == null || ownHealth.IsDead)
            {
                EnterState(EnemyState.Dead, 0f);
                return;
            }

            UpdateFacingDirection();
            switch (CurrentState)
            {
                case EnemyState.AttackWindup:
                    TickAttackWindup(safeDeltaTime);
                    break;
                case EnemyState.AttackRecovery:
                case EnemyState.Hurt:
                    TickTimedState(safeDeltaTime);
                    break;
                case EnemyState.Dead:
                    break;
                default:
                    EvaluateAndEnterState();
                    break;
            }
        }

        public void ResetToSpawn()
        {
            // 플레이어 재시작 시 적과 이 적이 발사한 탄환을 최초 전투 상태로 함께 복원한다.
            CacheComponents();
            CancelActiveProjectiles();
            transform.position = initialSpawnPosition;
            stateTimer = 0f;
            FiredProjectileCount = 0;
            FacingDirection = -1;
            if (bodyCollider != null)
            {
                bodyCollider.enabled = true;
            }

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

        private void EvaluateAndEnterState()
        {
            // 순수 판단 결과를 대기 또는 선딜 타이머가 있는 공격 상태로 변환한다.
            EnemyState nextState = ResolveDecision();
            float duration =
                nextState == EnemyState.AttackWindup
                    ? attackWindup
                    : 0f;
            EnterState(nextState, duration);
        }

        private void TickAttackWindup(
            float deltaTime)
        {
            // 예고 시간이 끝날 때 대상 위치와 범위를 다시 확인한 뒤 한 발만 생성한다.
            stateTimer -= deltaTime;
            if (stateTimer > 0f)
            {
                return;
            }

            TryFireProjectile();
            EnterState(
                EnemyState.AttackRecovery,
                attackRecovery);
        }

        private void TickTimedState(
            float deltaTime)
        {
            // 피격 경직과 공격 후딜이 끝나기 전에는 새 공격 판단으로 덮어쓰지 않는다.
            stateTimer -= deltaTime;
            if (stateTimer <= 0f)
            {
                EvaluateAndEnterState();
            }
        }

        private EnemyState ResolveDecision()
        {
            // 대상이 없으면 무한 거리로 처리해 순수 함수가 안전한 Idle을 반환하게 한다.
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
            return RangedEnemyDecisionMath.ResolveAttackState(
                CurrentState,
                targetAvailable,
                horizontalDistance,
                verticalDistance,
                detectionRange,
                loseTargetRange,
                verticalTolerance);
        }

        private void TryFireProjectile()
        {
            // 발사 시점에 대상이 범위 밖이면 탄환을 만들지 않아 화면 밖 사격을 방지한다.
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
            if (!RangedEnemyDecisionMath.IsInsideFireWindow(
                    targetAvailable,
                    horizontalDistance,
                    verticalDistance,
                    loseTargetRange,
                    verticalTolerance))
            {
                return;
            }

            Vector3 origin = muzzleTransform != null
                ? muzzleTransform.position
                : transform.position + Vector3.up * 0.9f;
            Vector3 aimPoint =
                target.position + Vector3.up * 0.9f;
            Vector3 launchDirection =
                aimPoint - origin;
            launchDirection.z = 0f;

            // 임시 구체는 나중에 Prefab과 풀링으로 교체하되 현재는 동작 계약을 한곳에서 확인한다.
            GameObject projectileObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);
            projectileObject.name =
                "EnemyProjectile_Runtime";
            projectileObject.layer = 0;
            projectileObject.transform.position = origin;
            projectileObject.transform.localScale =
                Vector3.one * 0.3f;
            SphereCollider projectileCollider =
                projectileObject.GetComponent<SphereCollider>();
            projectileCollider.isTrigger = true;
            projectileCollider.radius = 0.5f;
            projectileObject.AddComponent<Rigidbody>();
            Renderer projectileRenderer =
                projectileObject.GetComponent<Renderer>();
            if (projectileMaterial != null)
            {
                projectileRenderer.sharedMaterial =
                    projectileMaterial;
            }

            ApplyProjectileColor(projectileRenderer);
            EnemyProjectile projectile =
                projectileObject.AddComponent<EnemyProjectile>();
            projectile.Configure(
                transform,
                targetHealth,
                launchDirection,
                projectileSpeed,
                projectileLifetime,
                projectileDamage,
                projectileRenderer);
            projectile.Resolved +=
                HandleProjectileResolved;
            activeProjectiles.Add(projectile);
            FiredProjectileCount++;
        }

        private void ApplyProjectileColor(
            Renderer projectileRenderer)
        {
            // 공유 Material을 변경하지 않고 투사체 인스턴스만 청록색으로 표시한다.
            if (projectileRenderer == null)
            {
                return;
            }

            var properties =
                new MaterialPropertyBlock();
            projectileRenderer.GetPropertyBlock(properties);
            properties.SetColor(
                BaseColorId,
                new Color(0.15f, 0.9f, 1f, 1f));
            projectileRenderer.SetPropertyBlock(properties);
        }

        private void CancelActiveProjectiles()
        {
            // 뒤에서부터 제거하면 Resolved 이벤트가 List를 줄여도 아직 방문하지 않은 인덱스가 유지된다.
            for (int index =
                    activeProjectiles.Count - 1;
                 index >= 0;
                 index--)
            {
                EnemyProjectile projectile =
                    activeProjectiles[index];
                if (projectile != null)
                {
                    projectile.Resolved -=
                        HandleProjectileResolved;
                    projectile.Cancel();
                }
            }

            activeProjectiles.Clear();
        }

        private void HandleProjectileResolved(
            EnemyProjectile projectile)
        {
            // 해결된 탄환을 추적 목록에서 즉시 제거해 재시작 정리 비용이 실제 활성 수에 비례하게 한다.
            if (projectile != null)
            {
                projectile.Resolved -=
                    HandleProjectileResolved;
                activeProjectiles.Remove(projectile);
            }
        }

        private void UpdateFacingDirection()
        {
            // 같은 X 좌표에서는 마지막 방향을 유지해 충전 구체가 좌우로 떨리지 않게 한다.
            if (!IsTargetAvailable())
            {
                return;
            }

            float horizontal =
                target.position.x - transform.position.x;
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                FacingDirection =
                    horizontal > 0f ? 1 : -1;
                UpdateMuzzlePosition();
            }
        }

        private void UpdateMuzzlePosition()
        {
            // 발사 원점을 바라보는 방향의 몸 앞에 배치해 직선 투사체의 출발점을 읽기 쉽게 한다.
            if (muzzleTransform != null)
            {
                muzzleTransform.localPosition =
                    new Vector3(
                        FacingDirection * 0.85f,
                        1f,
                        0f);
            }
        }

        private bool IsTargetAvailable()
        {
            // 사망한 플레이어를 향해 충전하거나 투사체를 생성하지 않는다.
            return target != null
                && targetHealth != null
                && !targetHealth.IsDead;
        }

        private void CacheComponents()
        {
            // RequireComponent 참조도 EditMode 구성 순서를 고려해 필요할 때 직접 캐시한다.
            ownHealth ??= GetComponent<Health>();
            bodyCollider ??= GetComponent<Collider>();
        }

        private void CaptureInitialSpawn()
        {
            // Awake가 실행되지 않는 EditMode에서도 Configure 시점의 최초 배치를 한 번만 기록한다.
            if (initialSpawnCaptured)
            {
                return;
            }

            initialSpawnPosition = transform.position;
            initialSpawnCaptured = true;
        }

        private void ResolveTargetComponents()
        {
            // 공격과 재시작에 필요한 플레이어 계약을 대상 루트에서 한 번 캐시한다.
            targetHealth = target != null
                ? target.GetComponent<Health>()
                : null;
            targetRespawnController = target != null
                ? target.GetComponent<PlayerRespawnController>()
                : null;
        }

        private void SubscribeHealthEvents()
        {
            // 중복 구독은 한 번의 피격을 여러 Hurt 전환으로 만들 수 있으므로 플래그로 차단한다.
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
            // 비활성화 후 이전 Health 이벤트가 상태를 바꾸지 못하도록 연결을 해제한다.
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
            // 대상 교체나 Scene 종료 뒤 이전 플레이어가 이 적을 초기화하지 못하게 한다.
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
            // 생존 중 피격은 짧은 경직으로 공격 선딜을 취소하고 새 판단 시간을 만든다.
            if (currentHealth > 0)
            {
                EnterState(
                    EnemyState.Hurt,
                    hitStunDuration);
            }
        }

        private void HandleDied()
        {
            // 사망 즉시 몸 판정과 표현을 끄되 이미 발사된 탄환은 플레이어 재시작 전까지 유지한다.
            EnterState(EnemyState.Dead, 0f);
        }

        private void HandleTargetRespawned(
            Vector3 respawnPosition)
        {
            // 플레이어 도착 위치와 무관하게 원거리 적은 자신의 최초 배치와 탄환 상태로 복원한다.
            ResetToSpawn();
        }

        private void EnterState(
            EnemyState nextState,
            float duration)
        {
            // 같은 상태 재진입은 선딜 타이머를 매 프레임 초기화하므로 무시한다.
            if (CurrentState == nextState)
            {
                return;
            }

            EnemyState previousState = CurrentState;
            CurrentState = nextState;
            stateTimer = Mathf.Max(0f, duration);
            if (nextState == EnemyState.Dead
                && bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            ApplyStatePresentation(nextState);
            StateChanged?.Invoke(
                previousState,
                nextState);
        }

        private void ApplyStatePresentation(
            EnemyState state)
        {
            // 실제 모델과 애니메이션 전에는 몸 색과 충전 구체로 상태 변화를 읽게 한다.
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

            if (muzzleRenderer != null)
            {
                muzzleRenderer.enabled =
                    state == EnemyState.AttackWindup;
                if (muzzleRenderer.enabled)
                {
                    muzzleProperties ??=
                        new MaterialPropertyBlock();
                    muzzleRenderer.GetPropertyBlock(
                        muzzleProperties);
                    muzzleProperties.SetColor(
                        BaseColorId,
                        new Color(0.2f, 0.9f, 1f, 1f));
                    muzzleRenderer.SetPropertyBlock(
                        muzzleProperties);
                }
            }

            UpdateMuzzlePosition();
        }

        private static Color ResolveStateColor(
            EnemyState state)
        {
            // 근거리 적의 주황색과 구분되는 보라색 계열로 원거리 역할을 즉시 식별하게 한다.
            return state switch
            {
                EnemyState.AttackWindup =>
                    new Color(0.9f, 0.25f, 1f, 1f),
                EnemyState.AttackRecovery =>
                    new Color(0.35f, 0.16f, 0.55f, 1f),
                EnemyState.Hurt =>
                    Color.white,
                _ =>
                    new Color(0.55f, 0.28f, 0.9f, 1f)
            };
        }

        private void OnDrawGizmosSelected()
        {
            // 선택한 적의 최초 탐지와 추적 유지 범위를 Scene 뷰에서 비교한다.
            Gizmos.color =
                new Color(0.6f, 0.25f, 1f, 0.3f);
            Gizmos.DrawWireSphere(
                transform.position,
                detectionRange);
            Gizmos.color =
                new Color(0.2f, 0.9f, 1f, 0.25f);
            Gizmos.DrawWireSphere(
                transform.position,
                loseTargetRange);
        }
    }
}
