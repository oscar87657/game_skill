// GOLDEN STANDARD
// 목적: 게임 물리를 변경하지 않고 고정된 2.5D 평면에서 플레이어를 따라간다.
// 책임: 바라볼 목표 위치를 계산하고 카메라 위치만 부드럽게 보정한다.
// 불변식: 횡스크롤 가독성을 위해 카메라 회전과 깊이축은 고정한다.
// 선택 이유: SmoothDamp는 프레임률에 따른 Lerp 차이를 줄이고 디자이너 튜닝값을 제공한다.
using UnityEngine;

namespace GameSkill
{
    public sealed class SideScrollerCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 2.4f, -9f);
        [SerializeField, Min(0f)] private float horizontalLookAhead = 1.35f;
        [SerializeField, Min(0.01f)] private float horizontalSmoothTime = 0.16f;
        [SerializeField, Min(0.01f)] private float verticalSmoothTime = 0.24f;

        private SideScrollerMotor motor;
        private float horizontalVelocity;
        private float verticalVelocity;

        public void Configure(Transform followTarget)
        {
            // 입력값이 아니라 실제 바라보는 방향을 따라가도록 대상의 Motor를 한 번 찾는다.
            target = followTarget;
            motor = target != null ? target.GetComponent<SideScrollerMotor>() : null;
        }

        private void Awake()
        {
            // 씬에서 직접 연결한 참조와 에디터가 생성한 설정을 모두 지원한다.
            Configure(target);
        }

        private void Start()
        {
            // 첫 프레임에 원점에서 카메라가 이동하는 현상을 막는다.
            SnapToTarget();
        }

        private void LateUpdate()
        {
            // 플레이어 이동 뒤 실행하여 한 프레임 늦는 카메라 현상을 줄인다.
            if (target == null)
            {
                return;
            }

            float facingDirection = motor != null ? motor.FacingDirection : 0f;
            Vector3 desiredPosition = TargetPosition(facingDirection);
            float x = Mathf.SmoothDamp(
                transform.position.x,
                desiredPosition.x,
                ref horizontalVelocity,
                horizontalSmoothTime);
            float y = Mathf.SmoothDamp(
                transform.position.y,
                desiredPosition.y,
                ref verticalVelocity,
                verticalSmoothTime);

            transform.SetPositionAndRotation(
                new Vector3(x, y, desiredPosition.z),
                Quaternion.identity);
        }

        private void SnapToTarget()
        {
            // 씬 진입이나 부활 시 카메라를 즉시 올바른 위치에 둔다.
            if (target == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                TargetPosition(motor != null ? motor.FacingDirection : 0f),
                Quaternion.identity);
        }

        private Vector3 TargetPosition(float facingDirection)
        {
            // 속도가 아니라 바라보는 방향을 사용해 의도적인 방향 전환을 미리 보여준다.
            return new Vector3(
                target.position.x + offset.x + facingDirection * horizontalLookAhead,
                target.position.y + offset.y,
                target.position.z + offset.z);
        }
    }
}
