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
        [SerializeField, Min(0f)] private float airAttackHoverDuration = 0.1f;
        [SerializeField, Min(0f)] private float maxAirAttackHoverDuration = 0.3f;

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
            motor = GetComponent<SideScrollerMotor>();
            attackAction = GetComponent<PlayerInput>().actions.FindAction("Attack", true);
        }

        private void Update()
        {
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
            Vector3 facingOffset = hitBoxOffset;
            facingOffset.x *= motor.FacingDirection;
            Vector3 center = transform.position + facingOffset;

            Collider[] hits = Physics.OverlapBox(
                center,
                hitBoxHalfExtents,
                Quaternion.identity,
                damageLayers,
                QueryTriggerInteraction.Collide);

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
