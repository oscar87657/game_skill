// GOLDEN STANDARD
// 목적: Unity 생명주기와 분리해 횡스크롤 카메라 중심점의 구역 경계 제한을 계산한다.
// 책임: 뒤집힌 최소·최대 입력을 정규화하고 원하는 X/Y 중심점을 허용 범위에 고정한다.
// 불변식: 반환 좌표는 정규화된 최소값 이상, 최대값 이하이며 Z 깊이는 입력값을 보존한다.
// 선택 이유: 순수 계산으로 분리하면 카메라 이동 연출과 경계 규칙을 독립적으로 빠르게 테스트할 수 있다.
using UnityEngine;

namespace GameSkill
{
    public static class CameraBoundsMath
    {
        public static Vector3 ClampCenter(
            Vector3 desiredPosition,
            Vector2 minimumCenter,
            Vector2 maximumCenter)
        {
            // 디자이너가 Inspector에서 최소·최대를 반대로 넣어도 축별 허용 범위를 복구한다.
            float minimumX =
                Mathf.Min(minimumCenter.x, maximumCenter.x);
            float maximumX =
                Mathf.Max(minimumCenter.x, maximumCenter.x);
            float minimumY =
                Mathf.Min(minimumCenter.y, maximumCenter.y);
            float maximumY =
                Mathf.Max(minimumCenter.y, maximumCenter.y);

            return new Vector3(
                Mathf.Clamp(
                    desiredPosition.x,
                    minimumX,
                    maximumX),
                Mathf.Clamp(
                    desiredPosition.y,
                    minimumY,
                    maximumY),
                desiredPosition.z);
        }
    }
}
