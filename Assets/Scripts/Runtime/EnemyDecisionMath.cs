// GOLDEN STANDARD
// 목적: 적의 거리 기반 상태 판단을 Unity 생명주기와 분리해 결정적으로 계산한다.
// 책임: 대상 유효성·수평 거리·높이 차·탐지 이력으로 대기·추적·공격 상태를 선택한다.
// 불변식: 음수 범위와 유효하지 않은 대상은 공격으로 판정하지 않으며 사망 상태는 되돌리지 않는다.
// 선택 이유: 순수 함수로 분리하면 물리 프레임 없이 경계값과 탐지 히스테리시스를 빠르게 검증할 수 있다.
using System;

namespace GameSkill
{
    public static class EnemyDecisionMath
    {
        public static EnemyState ResolveLocomotionState(
            EnemyState currentState,
            bool targetAvailable,
            float horizontalDistance,
            float verticalDistance,
            float detectionRange,
            float loseTargetRange,
            float attackRange,
            float verticalTolerance)
        {
            // 사망은 Health가 명시적으로 복구되기 전에는 어떤 거리 조건으로도 해제하지 않는다.
            if (currentState == EnemyState.Dead)
            {
                return EnemyState.Dead;
            }

            // 잘못된 설정이나 대상은 안전한 대기 상태로 수렴시킨다.
            if (!targetAvailable
                || detectionRange < 0f
                || loseTargetRange < 0f
                || attackRange < 0f
                || verticalTolerance < 0f)
            {
                return EnemyState.Idle;
            }

            float horizontal = Math.Abs(horizontalDistance);
            float vertical = Math.Abs(verticalDistance);
            if (vertical > verticalTolerance)
            {
                // 횡스크롤 전투에서 다른 층의 플레이어를 벽 너머로 추적하지 않는다.
                return EnemyState.Idle;
            }

            if (horizontal <= attackRange)
            {
                return EnemyState.AttackWindup;
            }

            bool wasAware = currentState != EnemyState.Idle;
            float awarenessRange = wasAware
                ? Math.Max(detectionRange, loseTargetRange)
                : detectionRange;
            return horizontal <= awarenessRange
                ? EnemyState.Chase
                : EnemyState.Idle;
        }

        public static bool IsInsideAttackRange(
            bool targetAvailable,
            float horizontalDistance,
            float verticalDistance,
            float attackRange,
            float verticalTolerance)
        {
            // 실제 타격 순간에도 같은 경계 규칙을 사용해 준비 중 멀어진 대상을 맞히지 않는다.
            return targetAvailable
                && attackRange >= 0f
                && verticalTolerance >= 0f
                && Math.Abs(horizontalDistance) <= attackRange
                && Math.Abs(verticalDistance) <= verticalTolerance;
        }
    }
}
