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
            animator = targetAnimator;
        }

        private void Awake()
        {
            motor = GetComponent<SideScrollerMotor>();
            combat = GetComponent<PlayerCombat>();
            animator ??= GetComponentInChildren<Animator>();
        }

        private void Update()
        {
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
