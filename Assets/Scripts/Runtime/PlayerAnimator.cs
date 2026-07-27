// GOLDEN STANDARD
// Purpose: Translate gameplay state into Animator parameters only.
// Responsibility: Read motor/combat state and write animation parameters every frame.
// Invariant: This component never decides movement, damage, or transitions itself.
// Design choice: Hash IDs avoid repeated string lookups while keeping Animator setup data-driven.
using UnityEngine;

namespace GameSkill
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(SideScrollerMotor))]
    public sealed class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private static readonly int DodgingId = Animator.StringToHash("Dodging");
        private static readonly int AttackingId = Animator.StringToHash("Attacking");

        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float speedDamping = 0.1f;

        private SideScrollerMotor motor;
        private PlayerCombat combat;

        public void Configure(Animator targetAnimator)
        {
            // Editor builders call this after instantiating a visual model at runtime/editor time.
            animator = targetAnimator;
        }

        private void Awake()
        {
            // Cache dependencies once; GetComponentInChildren is intentionally a setup fallback.
            motor = GetComponent<SideScrollerMotor>();
            combat = GetComponent<PlayerCombat>();
            animator ??= GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            // Animation is a presentation projection of state, so it must not mutate gameplay state.
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(SpeedId, motor.NormalizedSpeed, speedDamping, Time.deltaTime);
            animator.SetBool(GroundedId, motor.IsGrounded);
            animator.SetFloat(VerticalSpeedId, motor.VerticalSpeed);
            animator.SetBool(DodgingId, motor.IsDashing);
            animator.SetBool(AttackingId, combat != null && combat.IsAttacking);
        }
    }
}
