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
    [RequireComponent(typeof(SideScrollerTargeting))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.01f)] private float attackDuration = 0.38f;
        [SerializeField, Min(0f)] private float hitDelay = 0.13f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.42f;
        [SerializeField, Range(1, 5)] private int comboLength = 3;
        [SerializeField, Min(0f)] private int finisherDamageBonus = 1;
        [SerializeField, Min(0f)] private float comboInputBufferTime = 0.24f;
        [SerializeField, Min(0f)] private float comboResetDelay = 0.5f;
        [SerializeField, Min(0f)] private float airAttackHoverDuration = 0.06f;
        [SerializeField, Min(0f)] private float maxAirAttackHoverDuration = 0.1f;

        [Header("Hit Box")]
        [SerializeField] private Vector3 hitBoxOffset = new(0.9f, 0.9f, 0f);
        [SerializeField] private Vector3 hitBoxHalfExtents = new(0.65f, 0.7f, 0.8f);
        [SerializeField] private LayerMask damageLayers = ~0;

        private readonly HashSet<Health> damagedTargets = new();
        private SideScrollerMotor motor;
        private SideScrollerTargeting targeting;
        private InputAction attackAction;
        private float attackTimer;
        private float hitTimer;
        private float cooldownTimer;
        private float comboBufferTimer;
        private float comboResetTimer;
        private bool hitApplied;
        private bool comboInputQueued;

        public bool IsAttacking => attackTimer > 0f;
        public int ComboStep { get; private set; }

        private void Awake()
        {
            // 의존성을 한 번만 캐시하며 전투는 이동 상태를 읽기만 하고 소유하지 않는다.
            motor = GetComponent<SideScrollerMotor>();
            targeting = GetComponent<SideScrollerTargeting>();
            attackAction = GetComponent<PlayerInput>().actions.FindAction("Attack", true);
        }

        private void Update()
        {
            // 새 입력을 받기 전에 쿨다운과 현재 공격 상태를 먼저 해결한다.
            float deltaTime = Time.deltaTime;
            cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);
            comboBufferTimer = Mathf.Max(0f, comboBufferTimer - deltaTime);
            comboResetTimer = Mathf.Max(0f, comboResetTimer - deltaTime);

            if (motor.IsDashing)
            {
                CancelAttack();
                return;
            }

            ReadAttackInput();
            UpdateActiveAttack(deltaTime);

            if (!IsAttacking
                && comboInputQueued
                && comboBufferTimer > 0f
                && cooldownTimer <= 0f)
            {
                StartAttack();
            }

            if (comboInputQueued && comboBufferTimer <= 0f)
            {
                // 버퍼 시간이 끝난 입력은 폐기하여 오래된 입력이 뒤늦게 공격을 실행하지 않게 한다.
                comboInputQueued = false;
            }

            if (!IsAttacking
                && !comboInputQueued
                && comboResetTimer <= 0f)
            {
                ComboStep = 0;
            }
        }

        private void OnDisable()
        {
            // 사망이나 씬 종료로 전투가 꺼질 때 버퍼와 공격 판정이 재활성화 후 남지 않게 한다.
            if (motor != null)
            {
                CancelAttack();
            }
        }

        private void ReadAttackInput()
        {
            // 공격 입력을 즉시 실행하지 않고 짧게 저장하여 프레임 단위 입력 누락을 줄인다.
            if (!attackAction.WasPressedThisFrame())
            {
                return;
            }

            comboInputQueued = true;
            comboBufferTimer = comboInputBufferTime;
        }

        private void StartAttack()
        {
            // 새 공격이 이전 공격의 대상 목록을 물려받지 않도록 타격별 상태를 초기화한다.
            ComboStep = CombatMath.NextComboStep(ComboStep, comboLength);
            attackTimer = attackDuration;
            hitTimer = Mathf.Min(hitDelay, attackDuration);
            cooldownTimer = attackCooldown;
            comboResetTimer = comboResetDelay;
            comboInputQueued = false;
            comboBufferTimer = 0f;
            hitApplied = false;
            damagedTargets.Clear();
            targeting.AcquireTarget(GetAttackOrigin(), motor.FacingDirection);
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

            if (attackTimer <= 0f
                && comboInputQueued
                && comboBufferTimer > 0f)
            {
                StartAttack();
            }
        }

        private void ApplyHit()
        {
            // 지정된 타격 순간에 한 번만 조회하고 아래 HashSet으로 중복 피격을 막는다.
            Vector3 aimDirection = targeting != null
                ? targeting.AimDirection
                : new Vector3(motor.FacingDirection, 0f, 0f);
            Vector3 center = GetAttackOrigin()
                + aimDirection * Mathf.Abs(hitBoxOffset.x);
            float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x)
                * Mathf.Rad2Deg;
            Quaternion hitBoxRotation = Quaternion.Euler(0f, 0f, aimAngle);

            Collider[] hits = Physics.OverlapBox(
                center,
                hitBoxHalfExtents,
                hitBoxRotation,
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

                target.TakeDamage(CombatMath.DamageForComboStep(
                    damage,
                    ComboStep,
                    finisherDamageBonus));
            }
        }

        private void CancelAttack()
        {
            // 대시 같은 중단 상황에서는 모든 임시 공격 상태를 제거한다.
            attackTimer = 0f;
            hitTimer = 0f;
            comboBufferTimer = 0f;
            comboResetTimer = 0f;
            comboInputQueued = false;
            ComboStep = 0;
            hitApplied = false;
            damagedTargets.Clear();
            targeting?.ClearTarget(motor.FacingDirection);
        }

        private Vector3 GetAttackOrigin()
        {
            // 공격 원점을 별도 함수로 두어 탐색과 실제 히트박스가 같은 좌표 기준을 공유한다.
            return transform.position
                + new Vector3(0f, hitBoxOffset.y, hitBoxOffset.z);
        }

        private void OnDrawGizmosSelected()
        {
            SideScrollerMotor currentMotor =
                motor != null ? motor : GetComponent<SideScrollerMotor>();
            float facing = currentMotor != null ? currentMotor.FacingDirection : 1f;
            SideScrollerTargeting currentTargeting =
                targeting != null ? targeting : GetComponent<SideScrollerTargeting>();
            Vector3 aimDirection = currentTargeting != null
                ? currentTargeting.AimDirection
                : new Vector3(facing, 0f, 0f);
            float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x)
                * Mathf.Rad2Deg;
            Vector3 center = GetAttackOrigin()
                + aimDirection * Mathf.Abs(hitBoxOffset.x);

            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.35f);
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                center,
                Quaternion.Euler(0f, 0f, aimAngle),
                Vector3.one);
            Gizmos.DrawCube(Vector3.zero, hitBoxHalfExtents * 2f);
            Gizmos.matrix = previousMatrix;
        }
    }
}
