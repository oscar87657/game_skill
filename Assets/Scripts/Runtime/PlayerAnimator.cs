// GOLDEN STANDARD
// 목적: 게임플레이 상태를 Animator 파라미터로 변환한다.
// 책임: 매 프레임 이동·전투 상태를 읽어 애니메이션 파라미터에 기록한다.
// 불변식: 이동·데미지·상태 전환 자체를 이 컴포넌트가 결정하지 않는다.
// 선택 이유: 해시 ID로 문자열 조회를 반복하지 않으면서 Animator 설정은 데이터로 유지한다.
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
        private static readonly int ComboStepId = Animator.StringToHash("ComboStep");

        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float speedDamping = 0.1f;

        private SideScrollerMotor motor;
        private PlayerCombat combat;

        public void Configure(Animator targetAnimator)
        {
            // 에디터 빌더가 시각 모델을 생성한 뒤 대상 Animator를 주입한다.
            animator = targetAnimator;
        }

        private void Awake()
        {
            // 의존성을 한 번만 캐시하며 GetComponentInChildren은 설정용 예비 경로다.
            motor = GetComponent<SideScrollerMotor>();
            combat = GetComponent<PlayerCombat>();
            animator ??= GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            // 애니메이션은 상태를 표현하는 계층이므로 게임플레이 상태를 변경하지 않는다.
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(SpeedId, motor.NormalizedSpeed, speedDamping, Time.deltaTime);
            animator.SetBool(GroundedId, motor.IsGrounded);
            animator.SetFloat(VerticalSpeedId, motor.VerticalSpeed);
            animator.SetBool(DodgingId, motor.IsDashing);
            animator.SetBool(AttackingId, combat != null && combat.IsAttacking);
            animator.SetInteger(ComboStepId, combat != null ? combat.ComboStep : 0);
        }
    }
}
