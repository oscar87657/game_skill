// GOLDEN STANDARD
// 목적: 횡스크롤 자동 조준의 후보 판정과 조준 방향 계산을 순수 수식으로 제공한다.
// 책임: 거리·높이·깊이·바라보는 방향 조건을 검사하고 제한된 조준 벡터를 반환한다.
// 불변식: 씬 객체와 물리 상태를 참조하지 않으며 같은 입력에는 항상 같은 결과를 반환한다.
// 선택 이유: 탐색과 점수 계산을 순수 함수로 분리하면 물리 콜라이더 없이 경계값을 테스트할 수 있다.
using UnityEngine;

namespace GameSkill
{
    public static class TargetingMath
    {
        public static bool IsCandidate(
            Vector3 offset,
            float facingDirection,
            float maximumRange,
            float maximumHeightDifference,
            float maximumDepthDifference)
        {
            // 잘못된 설정값은 후보를 허용하지 않아 예기치 않은 전역 조준을 방지한다.
            if (maximumRange <= 0f
                || maximumHeightDifference < 0f
                || maximumDepthDifference < 0f)
            {
                return false;
            }

            float facing = facingDirection < 0f ? -1f : 1f;
            if (offset.x * facing <= 0f)
            {
                // 횡스크롤 전투에서는 뒤쪽 대상을 자동 선택하지 않아 플레이어 의도를 보존한다.
                return false;
            }

            if (Mathf.Abs(offset.y) > maximumHeightDifference
                || Mathf.Abs(offset.z) > maximumDepthDifference)
            {
                return false;
            }

            // 실제 전투 평면인 X/Y 거리만 사거리로 사용하고 Z는 별도 허용 오차로 검사한다.
            float planarSqrDistance =
                offset.x * offset.x + offset.y * offset.y;
            return planarSqrDistance <= maximumRange * maximumRange;
        }

        public static float CandidateScore(
            Vector3 offset,
            float verticalPenalty,
            float depthPenalty)
        {
            // 낮은 점수가 우선이며 높이·깊이 차이에 가중치를 주어 같은 거리면 정면 적을 선호한다.
            float planarSqrDistance =
                offset.x * offset.x + offset.y * offset.y;
            return planarSqrDistance
                + Mathf.Abs(offset.y) * Mathf.Max(0f, verticalPenalty)
                + Mathf.Abs(offset.z) * Mathf.Max(0f, depthPenalty);
        }

        public static Vector3 ClampedAimDirection(
            Vector3 offset,
            float facingDirection,
            float maximumVerticalAngle)
        {
            float facing = facingDirection < 0f ? -1f : 1f;
            if (offset.sqrMagnitude <= Mathf.Epsilon)
            {
                // 대상 중심이 공격 원점과 같으면 정면을 사용해 0 벡터 정규화를 피한다.
                return new Vector3(facing, 0f, 0f);
            }

            float horizontalDistance = Mathf.Abs(offset.x);
            float requestedAngle = Mathf.Atan2(
                offset.y,
                Mathf.Max(horizontalDistance, Mathf.Epsilon))
                * Mathf.Rad2Deg;
            float clampedAngle = Mathf.Clamp(
                requestedAngle,
                -Mathf.Abs(maximumVerticalAngle),
                Mathf.Abs(maximumVerticalAngle));
            float radians = clampedAngle * Mathf.Deg2Rad;

            // Z축 성분을 제거해 공격 판정이 2.5D 전투 평면 밖으로 기울지 않게 한다.
            return new Vector3(
                Mathf.Cos(radians) * facing,
                Mathf.Sin(radians),
                0f);
        }
    }
}
