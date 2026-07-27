// GOLDEN STANDARD
// 목적: 프로토타입의 작고 테스트 가능한 플레이어 공격 루프를 조정한다.
// 책임: 공격 입력·시간을 관리하고 히트박스를 조회해 Health에 데미지를 전달한다.
// 불변식: 한 공격은 대상에게 최대 한 번만 데미지를 주며 이동을 직접 변경하지 않는다.
// 선택 이유: Physics.OverlapBox로 판정 타이밍을 결정적으로 만들고 표현과 분리한다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameSkill
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(SideScrollerMotor))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.01f)] private float attackDuration = 0.38f;
        [SerializeField, Min(0f)] private float hitDelay = 0.13f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.42f;
        [SerializeField, Min(0f)] private float airAttackHoverDuration = 0.06f;
        [SerializeField, Min(0f)] private float maxAirAttackHoverDuration = 0.1f;

        [Header("Hit Box")]
        [SerializeField] private Vector3 hitBoxOffset = new(0.9f, 0.9f, 0f);
        [SerializeField] private Vector3 hitBoxHalfExtents = new(0.65f, 0.7f, 0.8f);
        [SerializeField] private LayerMask damageLayers = ~0;

        private readonly HashSet<Health> damagedTargets = new();
        private SideScrollerMotor motor;
        private InputAction attackAction;
        private float attackTimer;
        private float hitTimer;
        private float cooldownTimer;
        private bool hitApplied;

        public bool IsAttacking => attackTimer > 0f;

        private void Awake()
        {
            // 의존성을 한 번만 캐시하며 전투는 이동 상태를 읽기만 하고 소유하지 않는다.
            motor = GetComponent<SideScrollerMotor>();
            attackAction = GetComponent<PlayerInput>().actions.FindAction("Attack", true);
        }

        private void Update()
        {
            // 새 입력을 받기 전에 쿨다운과 현재 공격 상태를 먼저 해결한다.
            float deltaTime = Time.deltaTime;
            cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);

            if (motor.IsDashing)
            {
                CancelAttack();
                return;
            }

            UpdateActiveAttack(deltaTime);

            if (attackAction.WasPressedThisFrame()
                && !IsAttacking
                && cooldownTimer <= 0f)
            {
                StartAttack();
            }
        }

        private void StartAttack()
        {
            // 새 공격이 이전 공격의 대상 목록을 물려받지 않도록 타격별 상태를 초기화한다.
            attackTimer = attackDuration;
            hitTimer = Mathf.Min(hitDelay, attackDuration);
            cooldownTimer = attackCooldown;
            hitApplied = false;
            damagedTargets.Clear();
            if (!motor.IsGrounded)
            {
                motor.RequestAirAttackHover(
                    airAttackHoverDuration,
                    maxAirAttackHoverDuration);
            }
        }

        private void UpdateActiveAttack(float deltaTime)
        {
            // 같은 프레임 시간으로 애니메이션 지속시간과 타격 지연을 함께 감소시킨다.
            if (!IsAttacking)
            {
                return;
            }

            attackTimer = Mathf.Max(0f, attackTimer - deltaTime);
            hitTimer -= deltaTime;

            if (!hitApplied && hitTimer <= 0f)
            {
                ApplyHit();
                hitApplied = true;
            }
        }

        private void ApplyHit()
        {
            // 지정된 타격 순간에 한 번만 조회하고 아래 HashSet으로 중복 피격을 막는다.
            Vector3 facingOffset = hitBoxOffset;
            facingOffset.x *= motor.FacingDirection;
            Vector3 center = transform.position + facingOffset;

            Collider[] hits = Physics.OverlapBox(
                center,
                hitBoxHalfExtents,
                Quaternion.identity,
                damageLayers,
                QueryTriggerInteraction.Collide);

            // 여러 콜라이더를 가진 적도 있으므로 모든 콜라이더를 순회한다.
            foreach (Collider hit in hits)
            {
                Health target = hit.GetComponentInParent<Health>();
                if (target == null
                    || target.transform == transform
                    || !damagedTargets.Add(target))
                {
                    continue;
                }

                target.TakeDamage(damage);
            }
        }

        private void CancelAttack()
        {
            // 대시 같은 중단 상황에서는 모든 임시 공격 상태를 제거한다.
            attackTimer = 0f;
            hitTimer = 0f;
            hitApplied = false;
            damagedTargets.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            SideScrollerMotor currentMotor =
                motor != null ? motor : GetComponent<SideScrollerMotor>();
            float facing = currentMotor != null ? currentMotor.FacingDirection : 1f;
            Vector3 facingOffset = hitBoxOffset;
            facingOffset.x *= facing;

            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.35f);
            Gizmos.DrawCube(
                transform.position + facingOffset,
                hitBoxHalfExtents * 2f);
        }
    }
}
