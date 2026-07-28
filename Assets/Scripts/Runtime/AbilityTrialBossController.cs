// GOLDEN STANDARD
// 목적: 세 이동 능력을 전투 회피에 활용하게 만드는 첫 포트폴리오 보스를 제어한다.
// 책임: 능력 관문·패턴 순환·투사체·지면 충격·페이즈·피격·사망·영구 처치 복원을 연결한다.
// 불변식: 모든 요구 능력 전에는 공격하지 않고 영구 처치된 보스는 재시작과 저장 복원 뒤 되살아나지 않는다.
// 선택 이유: 일반 적의 공통 Health·DamageRules·EnemyProjectile을 재사용하면서 패턴 선택만 별도 계층으로 확장한다.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Collider))]
    public sealed class AbilityTrialBossController : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private PlayerAbilityState abilityState;
        [SerializeField] private AbilityDefinition doubleJumpAbility;
        [SerializeField] private AbilityDefinition airDashAbility;
        [SerializeField] private AbilityDefinition wallTraversalAbility;

        [Header("Persistent Progress")]
        [SerializeField] private string bossId =
            "ability_warden";
        [SerializeField]
        private PlayerWorldState worldState;

        [Header("Presentation")]
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Renderer warningRenderer;
        [SerializeField] private Material projectileMaterial;

        [Header("Decision")]
        [SerializeField, Min(0f)] private float activationRange = 5.5f;
        [SerializeField, Min(0f)] private float verticalTolerance = 4.5f;
        [SerializeField, Min(0f)] private float attackWindup = 0.9f;
        [SerializeField, Min(0f)] private float secondPhaseWindup = 0.68f;
        [SerializeField, Min(0f)] private float attackRecovery = 1.15f;
        [SerializeField, Min(0f)] private float hitStunDuration = 0.18f;

        [Header("Pattern")]
        [SerializeField, Min(0f)] private float projectileSpeed = 6.5f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 3f;
        [SerializeField, Min(1)] private int attackDamage = 1;
        [SerializeField, Min(0f)] private float pulseSafeHeight = 2.1f;
        [SerializeField] private float arenaFloorHeight = 3.55f;

        private readonly List<EnemyProjectile> activeProjectiles =
            new();
        private Health ownHealth;
        private Health targetHealth;
        private SideScrollerMotor targetMotor;
        private PlayerRespawnController targetRespawnController;
        private Collider bodyCollider;
        private Vector3 initialSpawnPosition;
        private float stateTimer;
        private bool healthEventsSubscribed;
        private bool targetEventsSubscribed;
        private bool worldEventsSubscribed;
        private bool initialSpawnCaptured;
        private MaterialPropertyBlock visualProperties;
        private MaterialPropertyBlock warningProperties;

        public event Action<EnemyState, EnemyState> StateChanged;

        public EnemyState CurrentState { get; private set; } =
            EnemyState.Idle;
        public BossPattern CurrentPattern { get; private set; } =
            BossPattern.GroundWave;
        public int PatternExecutionCount { get; private set; }
        public int SuccessfulPulseCount { get; private set; }
        public int ActiveProjectileCount =>
            activeProjectiles.Count;
        public bool IsAbilityGateSatisfied =>
            HasAllRequiredAbilities();
        public string BossId => bossId;
        public bool IsPersistentlyDefeated =>
            worldState != null
            && worldState.IsBossDefeated(
                bossId);
        public bool IsSecondPhase =>
            ownHealth != null
            && BossPatternDecisionMath.IsSecondPhase(
                ownHealth.CurrentHealth,
                ownHealth.MaxHealth);

        private void Awake()
        {
            // 생존·충돌·최초 배치를 어떤 패턴보다 먼저 캐시한다.
            CacheComponents();
            CaptureInitialSpawn();
        }

        private void OnEnable()
        {
            // 활성 수명에 맞춰 보스 체력과 플레이어 재시작 이벤트를 정확히 한 번 연결한다.
            CacheComponents();
            ResolveTargetComponents();
            SubscribeHealthEvents();
            SubscribeTargetEvents();
            SubscribeWorldEvents();
        }

        private void Start()
        {
            // 씬 역직렬화가 끝난 참조를 다시 확인하고 잠금 또는 대기 표현을 적용한다.
            ResolveTargetComponents();
            SubscribeTargetEvents();
            SubscribeWorldEvents();
            ApplyPersistentProgress();
            ApplyStatePresentation(CurrentState);
        }

        private void Update()
        {
            // Unity 프레임 진입점은 테스트 가능한 Tick에 시간만 전달한다.
            Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            // Scene 종료 뒤 오래된 이벤트와 투사체가 남지 않게 구독과 생성물을 함께 정리한다.
            UnsubscribeHealthEvents();
            UnsubscribeTargetEvents();
            UnsubscribeWorldEvents();
            CancelActiveProjectiles();
        }

        public bool Configure(
            Transform targetTransform,
            PlayerAbilityState playerAbilityState,
            AbilityDefinition requiredDoubleJump,
            AbilityDefinition requiredAirDash,
            AbilityDefinition requiredWallTraversal,
            Renderer bossRenderer,
            Renderer patternWarningRenderer,
            Material runtimeProjectileMaterial)
        {
            // 빌더와 테스트가 같은 참조 경계를 사용하고 변경된 씬 참조만 저장하게 한다.
            if (target != targetTransform)
            {
                // 대상 교체 전에 이전 플레이어의 재시작 이벤트부터 끊는다.
                UnsubscribeTargetEvents();
            }

            bool changed =
                target != targetTransform
                || abilityState != playerAbilityState
                || doubleJumpAbility
                    != requiredDoubleJump
                || airDashAbility
                    != requiredAirDash
                || wallTraversalAbility
                    != requiredWallTraversal
                || visualRenderer != bossRenderer
                || warningRenderer
                    != patternWarningRenderer
                || projectileMaterial
                    != runtimeProjectileMaterial;
            target = targetTransform;
            abilityState = playerAbilityState;
            doubleJumpAbility =
                requiredDoubleJump;
            airDashAbility = requiredAirDash;
            wallTraversalAbility =
                requiredWallTraversal;
            visualRenderer = bossRenderer;
            warningRenderer =
                patternWarningRenderer;
            projectileMaterial =
                runtimeProjectileMaterial;

            CacheComponents();
            CaptureInitialSpawn();
            ResolveTargetComponents();
            SubscribeTargetEvents();
            ApplyStatePresentation(CurrentState);
            return changed;
        }

        public bool ConfigureProgress(
            string persistentBossId,
            PlayerWorldState playerWorldState)
        {
            // 저장 키나 상태 소유자가 바뀌기 전에 이전 복원 이벤트 연결을 해제한다.
            string normalizedId =
                string.IsNullOrWhiteSpace(
                    persistentBossId)
                    ? "ability_warden"
                    : persistentBossId.Trim();
            bool changed =
                bossId != normalizedId
                || worldState
                    != playerWorldState;
            if (!changed)
            {
                return false;
            }

            UnsubscribeWorldEvents();
            bossId = normalizedId;
            worldState = playerWorldState;
            SubscribeWorldEvents();
            ApplyPersistentProgress();
            return true;
        }

        public void Tick(float deltaTime)
        {
            // 음수 시간은 선딜과 후딜을 역행시키므로 0으로 제한한다.
            float safeDeltaTime =
                Mathf.Max(0f, deltaTime);
            PruneDestroyedProjectiles();
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
                case EnemyState.AttackRecovery:
                case EnemyState.Hurt:
                    TickTimedState(safeDeltaTime);
                    break;
                case EnemyState.Dead:
                    break;
                default:
                    EvaluateEngagement();
                    break;
            }
        }

        public void ResetToSpawn()
        {
            // 플레이어 재시작 때 보스·패턴·투사체를 최초 조우와 같은 원자적 상태로 복원한다.
            if (IsPersistentlyDefeated)
            {
                // 이미 저장 가능한 처치 상태라면 재시작이 보스를 부활시키지 않도록 사망 표현만 재확정한다.
                ApplyPersistentProgress();
                return;
            }

            CacheComponents();
            CancelActiveProjectiles();
            transform.position = initialSpawnPosition;
            stateTimer = 0f;
            CurrentPattern = BossPattern.GroundWave;
            PatternExecutionCount = 0;
            SuccessfulPulseCount = 0;
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

        private void EvaluateEngagement()
        {
            // 모든 능력과 거리 조건을 만족할 때만 현재 패턴의 선딜을 시작한다.
            if (!CanEngage())
            {
                ApplyStatePresentation(EnemyState.Idle);
                return;
            }

            float windupDuration = IsSecondPhase
                ? secondPhaseWindup
                : attackWindup;
            EnterState(
                EnemyState.AttackWindup,
                windupDuration);
        }

        private void TickAttackWindup(
            float deltaTime)
        {
            // 선딜 종료까지 패턴 경고를 유지하고 타격 프레임에 한 번만 실행한다.
            stateTimer -= deltaTime;
            if (stateTimer > 0f)
            {
                return;
            }

            ExecuteCurrentPattern();
            PatternExecutionCount++;
            EnterState(
                EnemyState.AttackRecovery,
                attackRecovery);
        }

        private void TickTimedState(
            float deltaTime)
        {
            // 피격 경직과 공격 후딜이 끝나기 전에는 새 패턴으로 덮어쓰지 않는다.
            stateTimer -= deltaTime;
            if (stateTimer > 0f)
            {
                return;
            }

            if (CurrentState
                == EnemyState.AttackRecovery)
            {
                CurrentPattern =
                    BossPatternDecisionMath.NextPattern(
                        CurrentPattern);
            }

            EnterState(EnemyState.Idle, 0f);
        }

        private void ExecuteCurrentPattern()
        {
            // enum 패턴을 실제 투사체 또는 높이 판정으로 변환한다.
            switch (CurrentPattern)
            {
                case BossPattern.AirBurst:
                    FireAirBurst();
                    break;
                case BossPattern.GroundPulse:
                    ResolveGroundPulse();
                    break;
                default:
                    FireGroundWave();
                    break;
            }
        }

        private void FireGroundWave()
        {
            // 발목 높이의 직선 탄환은 점프와 2단 점프로 넘는 첫 패턴이다.
            if (!IsTargetAvailable())
            {
                return;
            }

            int direction =
                target.position.x
                    < transform.position.x
                ? -1
                : 1;
            Vector3 origin =
                transform.position
                + new Vector3(
                    direction * 0.9f,
                    0.45f,
                    0f);
            CreateProjectile(
                origin,
                new Vector3(direction, 0f, 0f),
                new Color(1f, 0.35f, 0.08f, 1f));
        }

        private void FireAirBurst()
        {
            // 세 높이의 부채꼴 탄환은 공중 대시로 궤도를 가로지르는 두 번째 패턴이다.
            if (!IsTargetAvailable())
            {
                return;
            }

            Vector3 origin =
                transform.position
                + Vector3.up * 1.1f;
            float[] aimOffsets =
            {
                0.25f,
                1.05f,
                1.85f
            };
            // 높이 배열을 순회해 같은 발사 프레임에 서로 다른 회피 경로를 만든다.
            for (int index = 0;
                 index < aimOffsets.Length;
                 index++)
            {
                Vector3 aimPoint =
                    target.position
                    + Vector3.up
                        * aimOffsets[index];
                CreateProjectile(
                    origin,
                    aimPoint - origin,
                    new Color(0.2f, 0.85f, 1f, 1f));
            }
        }

        private void ResolveGroundPulse()
        {
            // 지면 충격은 안전 높이보다 낮은 플레이어만 맞혀 2단 점프와 벽 잡기를 요구한다.
            if (!IsTargetAvailable()
                || BossPatternDecisionMath.IsGroundPulseSafe(
                    target.position.y,
                    arenaFloorHeight,
                    pulseSafeHeight))
            {
                return;
            }

            bool isInvulnerable =
                targetMotor != null
                && targetMotor.IsInvulnerable;
            if (DamageRules.TryApply(
                    targetHealth,
                    isInvulnerable,
                    attackDamage))
            {
                SuccessfulPulseCount++;
            }
        }

        private void CreateProjectile(
            Vector3 origin,
            Vector3 launchDirection,
            Color color)
        {
            // 일반 원거리 적과 같은 투사체 계약을 재사용하되 패턴별 색만 인스턴스에 적용한다.
            GameObject projectileObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);
            projectileObject.name =
                "BossProjectile_Runtime";
            projectileObject.layer = 0;
            projectileObject.transform.position =
                origin;
            projectileObject.transform.localScale =
                Vector3.one * 0.34f;
            SphereCollider projectileCollider =
                projectileObject
                    .GetComponent<SphereCollider>();
            projectileCollider.isTrigger = true;
            projectileObject.AddComponent<Rigidbody>();
            Renderer projectileRenderer =
                projectileObject.GetComponent<Renderer>();
            if (projectileMaterial != null)
            {
                projectileRenderer.sharedMaterial =
                    projectileMaterial;
            }

            var properties =
                new MaterialPropertyBlock();
            projectileRenderer.GetPropertyBlock(
                properties);
            properties.SetColor(
                BaseColorId,
                color);
            projectileRenderer.SetPropertyBlock(
                properties);

            EnemyProjectile projectile =
                projectileObject
                    .AddComponent<EnemyProjectile>();
            projectile.Configure(
                transform,
                targetHealth,
                launchDirection,
                projectileSpeed,
                projectileLifetime,
                attackDamage,
                projectileRenderer);
            projectile.Resolved +=
                HandleProjectileResolved;
            activeProjectiles.Add(projectile);
        }

        private bool CanEngage()
        {
            // MonoBehaviour 참조를 순수 활성 조건의 값 인자로 변환한다.
            bool targetAvailable =
                IsTargetAvailable();
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
            return BossPatternDecisionMath.CanEngage(
                targetAvailable,
                HasAllRequiredAbilities(),
                horizontalDistance,
                verticalDistance,
                activationRange,
                verticalTolerance);
        }

        private bool HasAllRequiredAbilities()
        {
            // 세 정의 중 하나라도 없거나 미해금이면 보스 관문을 닫아 순서를 건너뛰지 못하게 한다.
            return abilityState != null
                && abilityState.HasAbility(
                    doubleJumpAbility)
                && abilityState.HasAbility(
                    airDashAbility)
                && abilityState.HasAbility(
                    wallTraversalAbility);
        }

        private bool IsTargetAvailable()
        {
            // 사망한 플레이어를 향해서는 패턴을 시작하거나 데미지를 적용하지 않는다.
            return target != null
                && targetHealth != null
                && !targetHealth.IsDead;
        }

        private void PruneDestroyedProjectiles()
        {
            // Destroy 예약으로 Unity null이 된 항목을 뒤에서 제거해 활성 수를 정확히 유지한다.
            for (int index =
                    activeProjectiles.Count - 1;
                 index >= 0;
                 index--)
            {
                if (activeProjectiles[index] == null)
                {
                    activeProjectiles.RemoveAt(index);
                }
            }
        }

        private void CancelActiveProjectiles()
        {
            // 뒤에서부터 이벤트를 끊고 취소해 콜백이 목록을 바꾸더라도 아직 방문하지 않은 인덱스를 보존한다.
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
            // 해결된 투사체를 즉시 추적 목록에서 제거해 재시작 비용을 실제 활성 수에 맞춘다.
            if (projectile == null)
            {
                return;
            }

            projectile.Resolved -=
                HandleProjectileResolved;
            activeProjectiles.Remove(projectile);
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

            initialSpawnPosition =
                transform.position;
            arenaFloorHeight =
                initialSpawnPosition.y;
            initialSpawnCaptured = true;
        }

        private void ResolveTargetComponents()
        {
            // 공격·무적·재시작에 필요한 플레이어 계약을 대상 루트에서 한 번 캐시한다.
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
            // 비활성화 뒤 이전 Health 이벤트가 보스 상태를 바꾸지 못하도록 연결을 해제한다.
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
            // 플레이어 재시작 완료 이벤트를 한 번만 구독해 보스 초기화가 중복되지 않게 한다.
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
            // 대상 교체나 Scene 종료 뒤 이전 플레이어가 이 보스를 초기화하지 못하게 한다.
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

        private void SubscribeWorldEvents()
        {
            // 저장 복원 이벤트를 한 번만 구독해 보스 표현이 중복 재구성되지 않게 한다.
            if (worldEventsSubscribed
                || worldState == null)
            {
                return;
            }

            worldState.WorldStateRestored +=
                HandleWorldStateRestored;
            worldEventsSubscribed = true;
        }

        private void UnsubscribeWorldEvents()
        {
            // Scene 종료나 상태 소유자 교체 뒤 이전 월드 복원이 보스를 변경하지 못하게 한다.
            if (!worldEventsSubscribed
                || worldState == null)
            {
                worldEventsSubscribed = false;
                return;
            }

            worldState.WorldStateRestored -=
                HandleWorldStateRestored;
            worldEventsSubscribed = false;
        }

        private void HandleDamaged(
            int currentHealth,
            int maximumHealth)
        {
            // 생존 중 피격은 현재 선딜을 취소해 공격적인 플레이에 짧은 보상을 제공한다.
            if (currentHealth > 0)
            {
                EnterState(
                    EnemyState.Hurt,
                    hitStunDuration);
            }
        }

        private void HandleDied()
        {
            // 사망 즉시 몸 판정·표현·활성 투사체를 모두 중단한다.
            worldState?.TryDefeatBoss(
                bossId);
            CancelActiveProjectiles();
            EnterState(EnemyState.Dead, 0f);
        }

        private void HandleTargetRespawned(
            Vector3 respawnPosition)
        {
            // 플레이어 도착 위치와 무관하게 미처치 보스만 최초 조우 상태로 되돌린다.
            if (IsPersistentlyDefeated)
            {
                ApplyPersistentProgress();
            }
            else
            {
                ResetToSpawn();
            }
        }

        private void HandleWorldStateRestored()
        {
            // 저장 데이터 전체 적용이 끝난 뒤 보스의 생존·사망 표현을 복원된 ID와 동기화한다.
            ApplyPersistentProgress();
        }

        private void ApplyPersistentProgress()
        {
            // 진행 상태가 연결되지 않은 독립 테스트 보스는 기존 생존 주기를 그대로 사용한다.
            if (worldState == null
                || string.IsNullOrWhiteSpace(
                    bossId))
            {
                return;
            }

            CacheComponents();
            if (!IsPersistentlyDefeated)
            {
                // 더 이른 저장 데이터를 불러와 처치 ID가 사라졌다면 보스를 정상 초기 상태로 복구한다.
                if (CurrentState
                        == EnemyState.Dead
                    || (ownHealth != null
                        && ownHealth.IsDead))
                {
                    ResetToSpawn();
                }

                return;
            }

            CancelActiveProjectiles();
            if (ownHealth != null
                && !ownHealth.IsDead)
            {
                // Health도 0으로 만들어 보스 상태와 공통 생존 계약이 서로 어긋나지 않게 한다.
                ownHealth.TakeDamage(
                    ownHealth.CurrentHealth);
            }

            EnterState(
                EnemyState.Dead,
                0f);
            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            ApplyStatePresentation(
                EnemyState.Dead);
        }

        private void EnterState(
            EnemyState nextState,
            float duration)
        {
            // 같은 상태 재진입은 선딜과 후딜 타이머를 매 프레임 초기화하므로 무시한다.
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
            // 몸 색은 능력 잠금·페이즈·상태를, 별도 경고 오브젝트는 다음 패턴을 나타낸다.
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

            RefreshWarning();
        }

        private void RefreshWarning()
        {
            // 선딜 중에만 패턴별 크기와 색을 표시해 필요한 이동 능력을 미리 읽게 한다.
            if (warningRenderer == null)
            {
                return;
            }

            bool isVisible =
                CurrentState == EnemyState.AttackWindup;
            warningRenderer.enabled = isVisible;
            if (!isVisible)
            {
                return;
            }

            Transform warningTransform =
                warningRenderer.transform;
            switch (CurrentPattern)
            {
                case BossPattern.AirBurst:
                    warningTransform.localPosition =
                        new Vector3(-1.1f, 1.3f, 0f);
                    warningTransform.localScale =
                        Vector3.one * 0.8f;
                    break;
                case BossPattern.GroundPulse:
                    warningTransform.localPosition =
                        new Vector3(-3.7f, 0.12f, 0f);
                    warningTransform.localScale =
                        new Vector3(7.4f, 0.14f, 1f);
                    break;
                default:
                    warningTransform.localPosition =
                        new Vector3(-1.3f, 0.45f, 0f);
                    warningTransform.localScale =
                        new Vector3(1.8f, 0.22f, 0.8f);
                    break;
            }

            warningProperties ??=
                new MaterialPropertyBlock();
            warningRenderer.GetPropertyBlock(
                warningProperties);
            warningProperties.SetColor(
                BaseColorId,
                ResolvePatternColor(
                    CurrentPattern));
            warningRenderer.SetPropertyBlock(
                warningProperties);
        }

        private Color ResolveStateColor(
            EnemyState state)
        {
            // 미해금 회색과 전투 중 자홍·흰색을 구분해 능력 관문과 피격을 애니메이션 전에도 읽게 한다.
            if (!HasAllRequiredAbilities())
            {
                return new Color(
                    0.22f,
                    0.24f,
                    0.28f,
                    1f);
            }

            return state switch
            {
                EnemyState.AttackWindup =>
                    ResolvePatternColor(
                        CurrentPattern),
                EnemyState.AttackRecovery =>
                    new Color(0.35f, 0.08f, 0.45f, 1f),
                EnemyState.Hurt =>
                    Color.white,
                _ when IsSecondPhase =>
                    new Color(1f, 0.12f, 0.5f, 1f),
                _ =>
                    new Color(0.72f, 0.12f, 0.86f, 1f)
            };
        }

        private static Color ResolvePatternColor(
            BossPattern pattern)
        {
            // 주황·청록·노랑을 각 능력 시험의 일관된 시각 언어로 사용한다.
            return pattern switch
            {
                BossPattern.AirBurst =>
                    new Color(0.15f, 0.85f, 1f, 1f),
                BossPattern.GroundPulse =>
                    new Color(1f, 0.9f, 0.08f, 1f),
                _ =>
                    new Color(1f, 0.35f, 0.08f, 1f)
            };
        }

        private void OnDrawGizmosSelected()
        {
            // 선택한 보스의 활성 반경과 지면 충격 안전 높이를 Scene 뷰에서 확인한다.
            Gizmos.color =
                new Color(0.8f, 0.15f, 1f, 0.3f);
            Gizmos.DrawWireSphere(
                transform.position,
                activationRange);
            Gizmos.color =
                new Color(1f, 0.9f, 0.08f, 0.5f);
            Gizmos.DrawLine(
                new Vector3(
                    transform.position.x - 8f,
                    arenaFloorHeight
                        + pulseSafeHeight,
                    transform.position.z),
                new Vector3(
                    transform.position.x + 1f,
                    arenaFloorHeight
                        + pulseSafeHeight,
                    transform.position.z));
        }
    }
}
