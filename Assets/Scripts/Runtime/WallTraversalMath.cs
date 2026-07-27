// GOLDEN STANDARD
// 목적: 벽 잡기·미끄러짐·벽 점프에 필요한 방향 판정을 순수 계산으로 제공한다.
// 책임: 벽 표면 판별, 벽 방향 입력, 낙하 속도 제한과 점프 반발 속도를 계산한다.
// 불변식: Unity 씬과 프레임 상태를 읽지 않으며 같은 입력에는 항상 같은 결과를 반환한다.
// 선택 이유: 물리 접촉 수집과 이동 규칙을 분리하면 경계 각도와 방향 오류를 EditMode에서 검증할 수 있다.
using UnityEngine;

namespace GameSkill
{
    public static class WallTraversalMath
    {
        public static bool IsWallSurface(
            Vector3 surfaceNormal,
            float minimumHorizontalNormal)
        {
            // 바닥·천장 접촉을 벽으로 오인하지 않도록 X축 법선 비율을 임계값과 비교한다.
            float threshold = Mathf.Clamp01(minimumHorizontalNormal);
            return Mathf.Abs(surfaceNormal.x) >= threshold
                && Mathf.Abs(surfaceNormal.y) < threshold;
        }

        public static bool IsHoldingTowardWall(
            float horizontalInput,
            float wallDirection,
            float deadZone)
        {
            // 입력과 벽 방향의 내적이 양수일 때만 플레이어가 벽을 향해 누르는 것으로 판단한다.
            float threshold = Mathf.Max(0f, deadZone);
            return horizontalInput * Mathf.Sign(wallDirection) > threshold;
        }

        public static float ClampWallSlideSpeed(
            float verticalSpeed,
            float maximumFallSpeed)
        {
            // 최대 낙하 속도를 음수 방향으로 정규화해 더 빠른 하강만 제한한다.
            float downwardLimit = -Mathf.Abs(maximumFallSpeed);
            return Mathf.Max(verticalSpeed, downwardLimit);
        }

        public static float WallJumpHorizontalSpeed(
            float wallDirection,
            float jumpSpeed)
        {
            // 벽이 있는 방향의 반대로 수평 속도를 만들어 충돌면에서 확실히 떨어뜨린다.
            return -Mathf.Sign(wallDirection) * Mathf.Abs(jumpSpeed);
        }
    }
}
