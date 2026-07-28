// GOLDEN STANDARD
// 목적: 일반 적과 첫 보스의 탐지·추적·공격·돌진·패턴 전환과 공통 데미지 규칙을 검증한다.
// 책임: 거리 경계·높이 차·탐지 히스테리시스·공격 선딜·돌진 방향·능력 관문·무적 거부의 회귀를 확인한다.
// 불변식: 각 컴포넌트 테스트는 자신이 생성한 Unity 오브젝트를 종료 전에 모두 정리한다.
// 선택 이유: 순수 판단 테스트와 작은 통합 테스트를 함께 두어 규칙 오류와 씬 배치 오류를 구분한다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class EnemyStateMachineTests
    {
        [Test]
        public void CharacterBodyCollisionPolicy_IgnoresOnlyCharacterBodies()
        {
            // 전용 레이어 이름과 몸 통과 규칙을 적용한 뒤 환경과의 충돌은 남아 있는지 확인한다.
            CharacterBodyCollisionPolicy.Apply();

            Assert.That(
                LayerMask.LayerToName(
                    CharacterBodyCollisionPolicy.PlayerBodyLayer),
                Is.EqualTo(
                    CharacterBodyCollisionPolicy.PlayerBodyLayerName));
            Assert.That(
                LayerMask.LayerToName(
                    CharacterBodyCollisionPolicy.EnemyBodyLayer),
                Is.EqualTo(
                    CharacterBodyCollisionPolicy.EnemyBodyLayerName));
            Assert.That(
                CharacterBodyCollisionPolicy.IsApplied(),
                Is.True);
            Assert.That(
                Physics.GetIgnoreLayerCollision(
                    CharacterBodyCollisionPolicy.PlayerBodyLayer,
                    0),
                Is.False);
            Assert.That(
                Physics.GetIgnoreLayerCollision(
                    CharacterBodyCollisionPolicy.EnemyBodyLayer,
                    0),
                Is.False);
        }

        [TestCase(
            EnemyState.Idle,
            true,
            9f,
            0f,
            EnemyState.Idle)]
        [TestCase(
            EnemyState.Idle,
            true,
            7f,
            0f,
            EnemyState.AttackWindup)]
        [TestCase(
            EnemyState.AttackRecovery,
            true,
            9f,
            0f,
            EnemyState.AttackWindup)]
        [TestCase(
            EnemyState.AttackRecovery,
            true,
            10f,
            0f,
            EnemyState.Idle)]
        [TestCase(
            EnemyState.AttackRecovery,
            true,
            6f,
            4f,
            EnemyState.Idle)]
        [TestCase(
            EnemyState.Dead,
            true,
            2f,
            0f,
            EnemyState.Dead)]
        public void RangedDecision_UsesDetectionHysteresis(
            EnemyState currentState,
            bool targetAvailable,
            float horizontalDistance,
            float verticalDistance,
            EnemyState expected)
        {
            // 실제 기본값인 탐지 8·해제 9.5·높이 3.8을 사용해 원거리 인지 경계를 검증한다.
            EnemyState result =
                RangedEnemyDecisionMath.ResolveAttackState(
                    currentState,
                    targetAvailable,
                    horizontalDistance,
                    verticalDistance,
                    8f,
                    9.5f,
                    3.8f);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(
            EnemyState.Idle,
            true,
            6f,
            0f,
            EnemyState.AttackWindup)]
        [TestCase(
            EnemyState.Idle,
            true,
            7f,
            0f,
            EnemyState.Idle)]
        [TestCase(
            EnemyState.AttackRecovery,
            true,
            7f,
            0f,
            EnemyState.AttackWindup)]
        [TestCase(
            EnemyState.AttackRecovery,
            true,
            8f,
            0f,
            EnemyState.Idle)]
        [TestCase(
            EnemyState.Idle,
            true,
            4f,
            2f,
            EnemyState.Idle)]
        [TestCase(
            EnemyState.Dead,
            true,
            2f,
            0f,
            EnemyState.Dead)]
        public void ChargeDecision_UsesDetectionHysteresis(
            EnemyState currentState,
            bool targetAvailable,
            float horizontalDistance,
            float verticalDistance,
            EnemyState expected)
        {
            // 기본 탐지 6.5·해제 7.5·높이 1.7을 사용해 돌진 준비의 인지 경계를 검증한다.
            EnemyState result =
                ChargeEnemyDecisionMath.ResolveAttackState(
                    currentState,
                    targetAvailable,
                    horizontalDistance,
                    verticalDistance,
                    6.5f,
                    7.5f,
                    1.7f);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(3f, -1, 1)]
        [TestCase(-3f, 1, -1)]
        [TestCase(0f, -1, -1)]
        [TestCase(0f, 1, 1)]
        public void ChargeDirection_LocksTargetSide(
            float horizontalDistance,
            int fallbackDirection,
            int expected)
        {
            // 돌진 시작 순간의 상대 X 방향과 같은 좌표에서 사용할 마지막 방향을 각각 확인한다.
            int result =
                ChargeEnemyDecisionMath.ResolveChargeDirection(
                    horizontalDistance,
                    fallbackDirection);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(true, true, 5f, 0f, true)]
        [TestCase(true, false, 2f, 0f, false)]
        [TestCase(false, true, 2f, 0f, false)]
        [TestCase(true, true, 6f, 0f, false)]
        [TestCase(true, true, 3f, 5f, false)]
        public void BossEngagement_RequiresAbilitiesAndRange(
            bool targetAvailable,
            bool allAbilitiesUnlocked,
            float horizontalDistance,
            float verticalDistance,
            bool expected)
        {
            // 보스 기본 활성 거리 5.5와 높이 4.5에서 능력 관문을 건너뛸 수 없는지 확인한다.
            bool result =
                BossPatternDecisionMath.CanEngage(
                    targetAvailable,
                    allAbilitiesUnlocked,
                    horizontalDistance,
                    verticalDistance,
                    5.5f,
                    4.5f);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(
            BossPattern.GroundWave,
            BossPattern.AirBurst)]
        [TestCase(
            BossPattern.AirBurst,
            BossPattern.GroundPulse)]
        [TestCase(
            BossPattern.GroundPulse,
            BossPattern.GroundWave)]
        public void BossPattern_CyclesPredictably(
            BossPattern current,
            BossPattern expected)
        {
            // 세 능력 시험이 플레이어가 학습 가능한 고정 순서로 순환하는지 검증한다.
            Assert.That(
                BossPatternDecisionMath.NextPattern(
                    current),
                Is.EqualTo(expected));
        }

        [TestCase(5.64f, 3.55f, 2.1f, false)]
        [TestCase(5.65f, 3.55f, 2.1f, true)]
        [TestCase(7f, 3.55f, 2.1f, true)]
        public void GroundPulse_UsesArenaRelativeHeight(
            float targetHeight,
            float floorHeight,
            float requiredHeight,
            bool expected)
        {
            // 월드 원점이 아니라 아레나 바닥을 기준으로 지면 충격의 안전 높이를 판정한다.
            Assert.That(
                BossPatternDecisionMath.IsGroundPulseSafe(
                    targetHeight,
                    floorHeight,
                    requiredHeight),
                Is.EqualTo(expected));
        }

        [TestCase(12, 12, false)]
        [TestCase(7, 12, false)]
        [TestCase(6, 12, true)]
        [TestCase(1, 12, true)]
        [TestCase(0, 12, false)]
        public void BossPhase_ChangesAtHalfHealth(
            int currentHealth,
            int maximumHealth,
            bool expected)
        {
            // 살아 있는 보스의 체력이 절반 이하일 때만 두 번째 페이즈가 되는지 확인한다.
            Assert.That(
                BossPatternDecisionMath.IsSecondPhase(
                    currentHealth,
                    maximumHealth),
                Is.EqualTo(expected));
        }

        [Test]
        public void EnemyProjectile_IgnoresOwnerAndDamagesTargetOnce()
        {
            // 투사체가 발사자는 통과하고 지정된 Health에만 한 번 데미지를 적용하는지 확인한다.
            var owner =
                new GameObject("ProjectileTestOwner");
            var target =
                new GameObject("ProjectileTestTarget");
            GameObject projectileObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);
            try
            {
                Health targetHealth =
                    target.AddComponent<Health>();
                targetHealth.Configure(5);
                projectileObject.AddComponent<Rigidbody>();
                EnemyProjectile projectile =
                    projectileObject
                        .AddComponent<EnemyProjectile>();
                projectile.Configure(
                    owner.transform,
                    targetHealth,
                    Vector3.right,
                    7f,
                    2f,
                    1,
                    projectileObject.GetComponent<Renderer>());

                Assert.That(
                    projectile.TryResolveCollision(owner),
                    Is.False);
                Assert.That(
                    projectile.TryResolveCollision(target),
                    Is.True);
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(4));
                Assert.That(
                    projectile.HasResolved,
                    Is.True);
                Assert.That(
                    projectile.WasDamageApplied,
                    Is.True);
                Assert.That(
                    projectile.TryResolveCollision(target),
                    Is.False);
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(projectileObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void EnemyProjectile_PassesThroughInvulnerableTarget()
        {
            // 무적 중 접촉은 탄환을 소비하지 않고, 같은 탄환의 이후 일반 피격은 정상 해결되는지 확인한다.
            var owner =
                new GameObject("ProjectileInvulnerabilityOwner");
            var target =
                new GameObject("ProjectileInvulnerabilityTarget");
            GameObject projectileObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);
            try
            {
                Health targetHealth =
                    target.AddComponent<Health>();
                targetHealth.Configure(5);
                projectileObject.AddComponent<Rigidbody>();
                EnemyProjectile projectile =
                    projectileObject
                        .AddComponent<EnemyProjectile>();
                Renderer projectileRenderer =
                    projectileObject.GetComponent<Renderer>();
                SphereCollider projectileCollider =
                    projectileObject.GetComponent<SphereCollider>();
                projectile.Configure(
                    owner.transform,
                    targetHealth,
                    Vector3.right,
                    7f,
                    2f,
                    1,
                    projectileRenderer);

                Assert.That(
                    projectile.TryResolveTargetHit(
                        targetHealth,
                        true),
                    Is.False);
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(5));
                Assert.That(
                    projectile.HasResolved,
                    Is.False);
                Assert.That(
                    projectileCollider.enabled,
                    Is.True);
                Assert.That(
                    projectileRenderer.enabled,
                    Is.True);

                Assert.That(
                    projectile.TryResolveTargetHit(
                        targetHealth,
                        false),
                    Is.True);
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(4));
                Assert.That(
                    projectile.HasResolved,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(projectileObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(owner);
            }
        }

        [TestCase(
            EnemyState.Idle,
            true,
            7f,
            0f,
            EnemyState.Idle)]
        [TestCase(
            EnemyState.Idle,
            true,
            4f,
            0f,
            EnemyState.Chase)]
        [TestCase(
            EnemyState.Chase,
            true,
            7f,
            0f,
            EnemyState.Chase)]
        [TestCase(
            EnemyState.Chase,
            true,
            9f,
            0f,
            EnemyState.Idle)]
        [TestCase(
            EnemyState.Chase,
            true,
            1f,
            0f,
            EnemyState.AttackWindup)]
        [TestCase(
            EnemyState.Chase,
            true,
            1f,
            2f,
            EnemyState.Idle)]
        [TestCase(
            EnemyState.Dead,
            true,
            1f,
            0f,
            EnemyState.Dead)]
        public void ResolveLocomotionState_UsesDistanceAndAwareness(
            EnemyState currentState,
            bool targetAvailable,
            float horizontalDistance,
            float verticalDistance,
            EnemyState expected)
        {
            // 탐지 6·이탈 8·공격 1.25·높이 1.6이라는 실제 기본 설정의 경계를 검증한다.
            EnemyState result =
                EnemyDecisionMath.ResolveLocomotionState(
                    currentState,
                    targetAvailable,
                    horizontalDistance,
                    verticalDistance,
                    6f,
                    8f,
                    1.25f,
                    1.6f);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void DamageRules_RejectsInvulnerabilityAndAppliesNormalHit()
        {
            // 같은 Health 대상에서 무적 공격은 거부하고 일반 공격만 한 칸 감소시키는지 확인한다.
            var target =
                new GameObject("DamageRulesTarget");
            try
            {
                Health health = target.AddComponent<Health>();
                health.Configure(5);

                Assert.That(
                    DamageRules.TryApply(health, true, 1),
                    Is.False);
                Assert.That(
                    health.CurrentHealth,
                    Is.EqualTo(5));
                Assert.That(
                    DamageRules.TryApply(health, false, 1),
                    Is.True);
                Assert.That(
                    health.CurrentHealth,
                    Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void MeleeEnemy_WaitsThenDamagesTargetOnce()
        {
            // 실제 컴포넌트가 공격 범위 진입 후 선딜과 후딜 상태를 순서대로 거치는지 검증한다.
            var target =
                new GameObject("MeleeEnemyTestTarget");
            GameObject enemy =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule);
            GameObject ground =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
            try
            {
                Health targetHealth =
                    target.AddComponent<Health>();
                targetHealth.Configure(5);
                target.transform.position =
                    new Vector3(1f, 0.05f, 0f);
                ground.transform.position =
                    new Vector3(0f, -0.5f, 0f);
                ground.transform.localScale =
                    new Vector3(8f, 1f, 3f);
                enemy.transform.position =
                    new Vector3(0f, 0.05f, 0f);

                Object.DestroyImmediate(
                    enemy.GetComponent<CapsuleCollider>());
                CharacterController characterController =
                    enemy.AddComponent<CharacterController>();
                characterController.center =
                    new Vector3(0f, 0.9f, 0f);
                characterController.height = 1.8f;
                characterController.radius = 0.35f;
                Health enemyHealth =
                    enemy.AddComponent<Health>();
                enemyHealth.Configure(3);
                MeleeEnemyController controller =
                    enemy.AddComponent<MeleeEnemyController>();
                controller.Configure(
                    target.transform,
                    enemy.GetComponent<Renderer>());
                controller.ConfigureAttackTiming(
                    0.55f,
                    0.7f);

                controller.Tick(0.01f);
                Assert.That(
                    controller.CurrentState,
                    Is.EqualTo(EnemyState.AttackWindup));
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(5));

                controller.Tick(0.56f);

                Assert.That(
                    controller.CurrentState,
                    Is.EqualTo(EnemyState.AttackRecovery));
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(4));
                Assert.That(
                    controller.SuccessfulAttackCount,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(ground);
            }
        }

        [Test]
        public void MeleeEnemy_ResetToSpawn_RestoresDeadEnemy()
        {
            // 플레이어 재시작에서 사용할 공개 초기화가 사망한 적의 위치·체력·상태·표현을 함께 복원하는지 검증한다.
            var target =
                new GameObject("EnemyResetTestTarget");
            GameObject enemy =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule);
            try
            {
                target.AddComponent<Health>()
                    .Configure(5);
                enemy.transform.position =
                    new Vector3(2f, 0.05f, 0f);
                Vector3 spawnPosition =
                    enemy.transform.position;
                Object.DestroyImmediate(
                    enemy.GetComponent<CapsuleCollider>());
                CharacterController characterController =
                    enemy.AddComponent<CharacterController>();
                Health enemyHealth =
                    enemy.AddComponent<Health>();
                enemyHealth.Configure(3);
                Renderer renderer =
                    enemy.GetComponent<Renderer>();
                MeleeEnemyController controller =
                    enemy.AddComponent<MeleeEnemyController>();
                controller.Configure(
                    target.transform,
                    renderer);

                Assert.That(
                    enemyHealth.TakeDamage(3),
                    Is.True);
                // EditMode에서는 MonoBehaviour 이벤트 생명주기가 재생되지 않으므로 한 틱으로 Health 사망을 상태에 반영한다.
                controller.Tick(0f);
                Assert.That(enemyHealth.IsDead, Is.True);
                Assert.That(
                    controller.CurrentState,
                    Is.EqualTo(EnemyState.Dead));
                Assert.That(
                    characterController.enabled,
                    Is.False);
                enemy.transform.position =
                    new Vector3(9f, 4f, 0f);

                controller.ResetToSpawn();

                Assert.That(
                    enemy.transform.position,
                    Is.EqualTo(spawnPosition));
                Assert.That(
                    enemyHealth.CurrentHealth,
                    Is.EqualTo(3));
                Assert.That(
                    controller.CurrentState,
                    Is.EqualTo(EnemyState.Idle));
                Assert.That(
                    characterController.enabled,
                    Is.True);
                Assert.That(renderer.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ChargeEnemy_WindupLocksDirectionAndDamagesOnce()
        {
            // 실제 CharacterController가 예고 후 방향을 잠그고 이동해 접촉 데미지를 한 번만 적용하는지 검증한다.
            var target =
                new GameObject("ChargeEnemyTestTarget");
            GameObject enemy =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
            GameObject ground =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
            try
            {
                Health targetHealth =
                    target.AddComponent<Health>();
                targetHealth.Configure(5);
                target.transform.position =
                    new Vector3(1.2f, 0.05f, 0f);
                ground.transform.position =
                    new Vector3(0f, -0.5f, 0f);
                ground.transform.localScale =
                    new Vector3(8f, 1f, 3f);
                enemy.transform.position =
                    new Vector3(0f, 0.05f, 0f);

                Object.DestroyImmediate(
                    enemy.GetComponent<BoxCollider>());
                CharacterController characterController =
                    enemy.AddComponent<CharacterController>();
                characterController.center =
                    new Vector3(0f, 0.65f, 0f);
                characterController.height = 1.3f;
                characterController.radius = 0.42f;
                Health enemyHealth =
                    enemy.AddComponent<Health>();
                enemyHealth.Configure(4);
                ChargeEnemyController controller =
                    enemy.AddComponent<ChargeEnemyController>();
                controller.Configure(
                    target.transform,
                    enemy.GetComponent<Renderer>(),
                    null);
                Physics.SyncTransforms();

                controller.Tick(0.01f);
                Assert.That(
                    controller.CurrentState,
                    Is.EqualTo(EnemyState.AttackWindup));
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(5));

                controller.Tick(0.56f);
                Assert.That(
                    controller.CurrentState,
                    Is.EqualTo(EnemyState.Charge));
                Assert.That(
                    controller.ChargeDirection,
                    Is.EqualTo(1));
                Assert.That(
                    controller.StartedChargeCount,
                    Is.EqualTo(1));

                controller.Tick(0.05f);
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(4));
                Assert.That(
                    controller.SuccessfulChargeHitCount,
                    Is.EqualTo(1));
                controller.Tick(0.05f);
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(ground);
            }
        }

        [Test]
        public void AbilityTrialBoss_WaitsForAbilitiesThenExecutesPattern()
        {
            // 실제 보스 컴포넌트가 세 능력 전에는 대기하고 해금 뒤 첫 투사체 패턴을 실행하는지 검증한다.
            var target =
                new GameObject("BossTestTarget");
            GameObject boss =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule);
            var doubleJump =
                ScriptableObject.CreateInstance<AbilityDefinition>();
            var airDash =
                ScriptableObject.CreateInstance<AbilityDefinition>();
            var wallTraversal =
                ScriptableObject.CreateInstance<AbilityDefinition>();
            try
            {
                doubleJump.Configure(
                    "boss_test_double_jump",
                    "2단 점프",
                    string.Empty);
                airDash.Configure(
                    "boss_test_air_dash",
                    "공중 대시",
                    string.Empty);
                wallTraversal.Configure(
                    "boss_test_wall",
                    "벽 잡기",
                    string.Empty);
                PlayerAbilityState abilityState =
                    target.AddComponent<PlayerAbilityState>();
                Health targetHealth =
                    target.AddComponent<Health>();
                targetHealth.Configure(5);
                target.transform.position =
                    new Vector3(-2f, 0.05f, 0f);
                Health bossHealth =
                    boss.AddComponent<Health>();
                bossHealth.Configure(12);
                AbilityTrialBossController controller =
                    boss.AddComponent<AbilityTrialBossController>();
                controller.Configure(
                    target.transform,
                    abilityState,
                    doubleJump,
                    airDash,
                    wallTraversal,
                    boss.GetComponent<Renderer>(),
                    null,
                    null);

                controller.Tick(0.01f);
                Assert.That(
                    controller.CurrentState,
                    Is.EqualTo(EnemyState.Idle));
                Assert.That(
                    controller.IsAbilityGateSatisfied,
                    Is.False);

                Assert.That(
                    abilityState.TryUnlock(doubleJump),
                    Is.True);
                Assert.That(
                    abilityState.TryUnlock(airDash),
                    Is.True);
                Assert.That(
                    abilityState.TryUnlock(
                        wallTraversal),
                    Is.True);
                controller.Tick(0.01f);
                Assert.That(
                    controller.CurrentState,
                    Is.EqualTo(EnemyState.AttackWindup));
                Assert.That(
                    controller.IsAbilityGateSatisfied,
                    Is.True);

                controller.Tick(0.91f);
                Assert.That(
                    controller.CurrentState,
                    Is.EqualTo(EnemyState.AttackRecovery));
                Assert.That(
                    controller.PatternExecutionCount,
                    Is.EqualTo(1));
                Assert.That(
                    controller.ActiveProjectileCount,
                    Is.EqualTo(1));
                controller.ResetToSpawn();
                Assert.That(
                    controller.ActiveProjectileCount,
                    Is.Zero);
            }
            finally
            {
                EnemyProjectile[] projectiles =
                    Object.FindObjectsByType<EnemyProjectile>(
                        FindObjectsSortMode.None);
                // EditMode의 Destroy 예약 없는 투사체를 테스트가 직접 모두 정리한다.
                for (int index = 0;
                     index < projectiles.Length;
                     index++)
                {
                    Object.DestroyImmediate(
                        projectiles[index].gameObject);
                }

                Object.DestroyImmediate(boss);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(doubleJump);
                Object.DestroyImmediate(airDash);
                Object.DestroyImmediate(wallTraversal);
            }
        }
    }
}
