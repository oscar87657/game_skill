// GOLDEN STANDARD
// 목적: 정사영 기준 화면 높이를 유지하면서 원근 카메라의 거리와 보이는 범위를 계산한다.
// 책임: 세로 반높이·시야각·거리 사이의 투영 관계를 Unity 생명주기 없이 변환한다.
// 불변식: 정규화된 입력에서 거리와 반높이는 항상 양수이며 시야각은 1도 이상 179도 이하이다.
// 선택 이유: 투영 수학을 순수 함수로 분리하면 카메라 손맛 조정과 화면 크기 회귀를 독립적으로 검증할 수 있다.
using UnityEngine;

namespace GameSkill
{
    public static class CameraPerspectiveMath
    {
        public static float DistanceForVerticalFraming(
            float verticalHalfExtent,
            float verticalFieldOfView)
        {
            // 0 이하 입력도 카메라가 플레이 평면을 통과하지 않도록 안전한 양수 범위로 정규화한다.
            float safeHalfExtent =
                Mathf.Max(0.01f, Mathf.Abs(verticalHalfExtent));
            float safeFieldOfView =
                Mathf.Clamp(
                    Mathf.Abs(verticalFieldOfView),
                    1f,
                    179f);
            float halfAngleRadians =
                safeFieldOfView
                * 0.5f
                * Mathf.Deg2Rad;
            return safeHalfExtent
                / Mathf.Tan(halfAngleRadians);
        }

        public static float VerticalHalfExtent(
            float distance,
            float verticalFieldOfView)
        {
            // 거리에서 다시 화면 반높이를 구해 설정한 구도가 왕복 계산에서도 유지되는지 확인한다.
            float safeDistance =
                Mathf.Max(0.01f, Mathf.Abs(distance));
            float safeFieldOfView =
                Mathf.Clamp(
                    Mathf.Abs(verticalFieldOfView),
                    1f,
                    179f);
            return safeDistance
                * Mathf.Tan(
                    safeFieldOfView
                    * 0.5f
                    * Mathf.Deg2Rad);
        }
    }
}
