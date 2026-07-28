// GOLDEN STANDARD
// 목적: 원거리 적 투사체의 실제 물리 이동과 Trigger 피격 경로를 PlayMode에서 검증한다.
// 책임: Kinematic Rigidbody가 목표 Collider에 도달해 데미지를 한 번 적용하는지 확인한다.
// 불변식: 테스트가 만든 모든 오브젝트는 성공·실패와 관계없이 종료 전에 정리한다.
// 선택 이유: 직접 호출 테스트만으로 놓칠 수 있는 FixedUpdate·Trigger·Rigidbody 연결 오류를 분리해 잡는다.
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameSkill.Tests
{
    public sealed class EnemyProjectilePlayModeTests
    {
        [UnityTest]
        public IEnumerator Projectile_MovesAndDamagesTargetCollider()
        {
            // 실제 구체 Trigger를 목표 캡슐로 이동시켜 물리 콜백부터 Health까지 한 경로로 검증한다.
            var owner =
                new GameObject("ProjectilePhysicsOwner");
            GameObject target =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule);
            GameObject projectileObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);
            try
            {
                owner.transform.position =
                    new Vector3(-1f, 0f, 0f);
                target.transform.position =
                    new Vector3(1.2f, 0f, 0f);
                Health targetHealth =
                    target.AddComponent<Health>();
                targetHealth.Configure(5);

                projectileObject.transform.position =
                    Vector3.zero;
                projectileObject.transform.localScale =
                    Vector3.one * 0.3f;
                projectileObject.AddComponent<Rigidbody>();
                EnemyProjectile projectile =
                    projectileObject
                        .AddComponent<EnemyProjectile>();
                bool resolved = false;
                bool damageApplied = false;
                projectile.Resolved += result =>
                {
                    // Destroy 예약 전에 결과를 값으로 복사해 다음 프레임에도 안전하게 검증한다.
                    resolved = true;
                    damageApplied =
                        result.WasDamageApplied;
                };
                projectile.Configure(
                    owner.transform,
                    targetHealth,
                    Vector3.right,
                    8f,
                    1f,
                    1,
                    projectileObject.GetComponent<Renderer>());
                Physics.SyncTransforms();

                // 최대 20 물리 프레임까지만 기다려 실패가 무한 대기로 바뀌지 않게 한다.
                for (int frame = 0;
                     frame < 20 && !resolved;
                     frame++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(resolved, Is.True);
                Assert.That(damageApplied, Is.True);
                Assert.That(
                    targetHealth.CurrentHealth,
                    Is.EqualTo(4));
            }
            finally
            {
                if (projectileObject != null)
                {
                    Object.Destroy(projectileObject);
                }

                Object.Destroy(target);
                Object.Destroy(owner);
            }
        }
    }
}
