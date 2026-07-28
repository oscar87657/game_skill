// GOLDEN STANDARD
// 목적: 첫 근거리 적의 탐지·추적·공격 상태 전환과 공통 데미지 규칙을 검증한다.
// 책임: 거리 경계·높이 차·탐지 히스테리시스·공격 선딜·무적 거부의 회귀를 확인한다.
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

                controller.Tick(0.01f);
                Assert.That(
                    controller.CurrentState,
                    Is.EqualTo(EnemyState.AttackWindup));
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(5));

                controller.Tick(0.31f);

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
    }
}
