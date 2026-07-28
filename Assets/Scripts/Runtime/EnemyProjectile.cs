// GOLDEN STANDARD
// 목적: 원거리 적이 발사한 투사체를 이동시키고 플레이어·환경과의 접촉을 해결한다.
// 책임: 고정 방향 이동, 수명 종료, 소유자 필터, 대시 무적 데미지와 환경 충돌 소멸을 처리한다.
// 불변식: 한 투사체는 최대 한 번만 해결되며 소유자와 다른 적에게 데미지를 주지 않는다.
// 선택 이유: 투사체를 적 상태 머신에서 분리하면 발사 패턴과 실제 충돌 규칙을 독립적으로 교체할 수 있다.
using System;
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private Transform owner;
        private Health targetHealth;
        private SideScrollerMotor targetMotor;
        private Renderer visualRenderer;
        private SphereCollider triggerCollider;
        private Rigidbody projectileBody;
        private Vector3 direction = Vector3.left;
        private float speed;
        private float remainingLifetime;
        private int damage;

        public event Action<EnemyProjectile> Resolved;

        public bool IsConfigured { get; private set; }
        public bool HasResolved { get; private set; }
        public bool WasDamageApplied { get; private set; }

        private void Awake()
        {
            // 물리 구성 요소를 한 번 캐시하고 Trigger 투사체의 기본 조건을 보장한다.
            CacheComponents();
            ConfigurePhysicsBody();
        }

        private void FixedUpdate()
        {
            // 물리 프레임마다 고정 방향으로 이동해 렌더 프레임과 충돌 검사의 차이를 줄인다.
            if (!IsConfigured || HasResolved)
            {
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            remainingLifetime -= deltaTime;
            if (remainingLifetime <= 0f)
            {
                Finish(false);
                return;
            }

            projectileBody.MovePosition(
                projectileBody.position
                + direction * speed * deltaTime);
        }

        public void Configure(
            Transform ownerTransform,
            Health target,
            Vector3 launchDirection,
            float moveSpeed,
            float lifetime,
            int damageAmount,
            Renderer projectileRenderer)
        {
            // 생성자 대신 공개 설정 경계를 사용해 런타임 생성과 자동 테스트가 같은 초기화 경로를 공유한다.
            CacheComponents();
            ConfigurePhysicsBody();
            owner = ownerTransform;
            targetHealth = target;
            targetMotor = target != null
                ? target.GetComponent<SideScrollerMotor>()
                : null;
            visualRenderer = projectileRenderer;

            Vector3 planarDirection =
                new Vector3(
                    launchDirection.x,
                    launchDirection.y,
                    0f);
            direction = planarDirection.sqrMagnitude
                    > 0.000001f
                ? planarDirection.normalized
                : Vector3.left;
            speed = Mathf.Max(0f, moveSpeed);
            remainingLifetime =
                Mathf.Max(0.01f, lifetime);
            damage = Mathf.Max(1, damageAmount);
            IsConfigured = true;
            HasResolved = false;
            WasDamageApplied = false;
        }

        public bool TryResolveCollision(
            GameObject collisionObject)
        {
            // 물리 콜백과 테스트가 같은 대상 분류 규칙을 사용하도록 충돌 해결을 공개 함수로 분리한다.
            if (HasResolved || collisionObject == null)
            {
                return false;
            }

            Transform collisionTransform =
                collisionObject.transform;
            if (owner != null
                && (collisionTransform == owner
                    || collisionTransform.IsChildOf(owner)))
            {
                // 발사 직후 소유자의 몸 안에서 시작해도 자기 투사체로 소멸하거나 피격되지 않는다.
                return false;
            }

            Health hitHealth =
                collisionObject.GetComponentInParent<Health>();
            if (hitHealth != null)
            {
                if (hitHealth != targetHealth)
                {
                    // 다른 적과 훈련용 더미는 아군 방패가 되지 않도록 그대로 통과한다.
                    return false;
                }

                bool isInvulnerable =
                    targetMotor != null
                    && targetMotor.IsInvulnerable;
                bool damageApplied = DamageRules.TryApply(
                    targetHealth,
                    isInvulnerable,
                    damage);
                Finish(damageApplied);
                return true;
            }

            // Health가 없는 비 Trigger Collider는 바닥·벽으로 취급해 투사체를 제거한다.
            Finish(false);
            return true;
        }

        public void Cancel()
        {
            // 플레이어 재시작이나 Scene 종료에서 남은 투사체를 데미지 없이 정리한다.
            if (!HasResolved)
            {
                Finish(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 체크포인트·구역 Trigger는 탄환을 막지 않고 실제 몸·환경 Collider만 해결한다.
            if (other == null || other.isTrigger)
            {
                return;
            }

            TryResolveCollision(other.gameObject);
        }

        private void CacheComponents()
        {
            // RequireComponent가 보장한 참조도 EditMode 생명주기를 위해 필요할 때 직접 캐시한다.
            triggerCollider ??=
                GetComponent<SphereCollider>();
            projectileBody ??=
                GetComponent<Rigidbody>();
        }

        private void ConfigurePhysicsBody()
        {
            // 중력 없는 연속 추적 Trigger로 구성해 빠른 탄환이 얇은 CharacterController를 놓칠 가능성을 줄인다.
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            if (projectileBody != null)
            {
                projectileBody.useGravity = false;
                projectileBody.isKinematic = true;
                projectileBody.interpolation =
                    RigidbodyInterpolation.Interpolate;
                projectileBody.collisionDetectionMode =
                    CollisionDetectionMode.ContinuousSpeculative;
            }
        }

        private void Finish(bool damageApplied)
        {
            // 해결 플래그를 먼저 세워 같은 물리 프레임의 복수 접촉이 데미지를 중복 적용하지 않게 한다.
            if (HasResolved)
            {
                return;
            }

            HasResolved = true;
            WasDamageApplied = damageApplied;
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            if (visualRenderer != null)
            {
                visualRenderer.enabled = false;
            }

            Resolved?.Invoke(this);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            // EditMode에서는 테스트가 결과 속성을 읽은 뒤 자신이 만든 오브젝트를 직접 정리한다.
        }
    }
}
