// GOLDEN STANDARD
// Purpose: Coordinate a small, testable player attack loop for the prototype.
// Responsibility: Read attack input, manage timing, query hitboxes, and send damage to Health.
// Invariant: Each attack damages a target at most once and never changes locomotion directly.
// Design choice: Physics.OverlapBox keeps hit timing deterministic while leaving animation presentation separate.
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
            // Cache dependencies once; combat reads movement state but does not own movement.
            motor = GetComponent<SideScrollerMotor>();
            attackAction = GetComponent<PlayerInput>().actions.FindAction("Attack", true);
        }

        private void Update()
        {
            // Resolve cooldown and active attack before accepting a new attack input.
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
            // Reset per-swing state so a new attack cannot inherit an old target set.
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
            // Decrement both animation duration and hit delay using the same frame clock.
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
            // Query once at the authored hit moment; the HashSet below prevents multi-hit overlap.
            Vector3 facingOffset = hitBoxOffset;
            facingOffset.x *= motor.FacingDirection;
            Vector3 center = transform.position + facingOffset;

            Collider[] hits = Physics.OverlapBox(
                center,
                hitBoxHalfExtents,
                Quaternion.identity,
                damageLayers,
                QueryTriggerInteraction.Collide);

            // Iterate every collider because compound enemies may expose multiple body colliders.
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
            // Interruptions such as dashing must clear all transient attack state.
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
