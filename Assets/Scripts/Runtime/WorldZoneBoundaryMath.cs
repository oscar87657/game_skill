// GOLDEN STANDARD
// 목적: 겹치는 물리 Collider와 분리해 구역 전환을 확정할 수평 내부 범위를 계산한다.
// 책임: 최소·최대·여유 폭을 정규화하고 캐릭터 중심이 완충 구간을 통과했는지 판정한다.
// 불변식: 여유 폭은 구역 반너비를 넘지 않으며 경계 완충 구간에서는 어느 새 구역도 확정하지 않는다.
// 선택 이유: 히스테리시스 판정은 경계에서 Trigger 호출 순서가 바뀌어도 현재 구역이 흔들리지 않게 한다.
using UnityEngine;

namespace GameSkill
{
    public static class WorldZoneBoundaryMath
    {
        public static bool IsInsideHorizontalInterior(
            float positionX,
            float boundaryA,
            float boundaryB,
            float requestedInset)
        {
            // 뒤집힌 경계와 음수 여유 폭을 정규화해 Inspector 입력 순서와 무관하게 처리한다.
            float minimum = Mathf.Min(boundaryA, boundaryB);
            float maximum = Mathf.Max(boundaryA, boundaryB);
            float halfWidth = (maximum - minimum) * 0.5f;
            float inset = Mathf.Clamp(
                requestedInset,
                0f,
                halfWidth);

            return positionX >= minimum + inset
                && positionX <= maximum - inset;
        }
    }
}
