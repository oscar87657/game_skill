// GOLDEN STANDARD
// 목적: MonoBehaviour 상태와 분리된 결정적 이동 수식을 보관한다.
// 책임: 입력 정규화, 바라보는 방향, 대시 방향, 포물선 점프 속도를 계산한다.
// 불변식: 잘못된 물리 매개변수에는 NaN이나 Infinity 대신 안전한 값을 반환한다.
// 선택 이유: 순수 함수는 단위 테스트가 쉽고 다른 이동 컨트롤러에서도 재사용할 수 있다.
using UnityEngine;

namespace GameSkill
{
    public static class MovementMath
    {
        public static float HorizontalInput(Vector2 input, float deadZone)
        {
            // 먼저 입력을 제한한 뒤 데드존을 적용하여 키보드와 아날로그 입력의 계약을 통일한다.
            float clampedInput = Mathf.Clamp(input.x, -1f, 1f);
            return Mathf.Abs(clampedInput) < Mathf.Clamp01(deadZone)
                ? 0f
                : clampedInput;
        }

        public static float SideScrollerFacingYaw(float horizontalDirection)
        {
            // 횡스크롤러는 두 방향만 필요하므로 부호를 바로 회전값으로 매핑한다.
            return horizontalDirection < 0f ? -90f : 90f;
        }

        public static float DodgeDirection(
            float horizontalInput,
            float facingDirection)
        {
            // 현재 입력을 우선하고 입력이 없으면 캐릭터의 마지막 바라보는 방향을 유지한다.
            if (Mathf.Abs(horizontalInput) > Mathf.Epsilon)
            {
                return Mathf.Sign(horizontalInput);
            }

            return facingDirection < 0f ? -1f : 1f;
        }

        public static float JumpSpeed(float jumpHeight, float gravity)
        {
            // v² = u² + 2as 공식으로 초기 속도를 구하며, 중력은 아래 방향이어야 한다.
            if (jumpHeight <= 0f || gravity >= 0f)
            {
                return 0f;
            }

            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
