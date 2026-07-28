// GOLDEN STANDARD
// 목적: 고정형 원거리 적의 탐지와 발사 준비 상태를 Unity 생명주기 없이 계산한다.
// 책임: 대상 유효성·수평 거리·높이 차·기존 인지 상태를 이용해 대기 또는 공격 준비를 선택한다.
// 불변식: 잘못된 범위와 다른 층의 대상은 공격하지 않으며 사망 상태는 거리 변화로 해제되지 않는다.
// 선택 이유: 원거리 적의 판단을 순수 함수로 두면 투사체 표현과 무관하게 경계값을 빠르게 검증할 수 있다.
using System;

namespace GameSkill
{
    public static class RangedEnemyDecisionMath
    {
        public static EnemyState ResolveAttackState(
            EnemyState currentState,
            bool targetAvailable,
            float horizontalDistance,
            float verticalDistance,
            float detectionRange,
            float loseTargetRange,
            float verticalTolerance)
        {
            // Health가 복구되기 전의 사망 상태는 어떤 거리 조건으로도 공격 상태로 돌아가지 않는다.
            if (currentState == EnemyState.Dead)
            {
                return EnemyState.Dead;
            }

            // 유효하지 않은 대상이나 음수 설정은 안전하게 대기 상태로 수렴시킨다.
            if (!targetAvailable
                || detectionRange < 0f
                || loseTargetRange < 0f
                || verticalTolerance < 0f)
            {
                return EnemyState.Idle;
            }

            if (Math.Abs(verticalDistance) > verticalTolerance)
            {
                // 다른 층을 무조건 관통 사격하지 않도록 높이 차가 큰 대상은 인지하지 않는다.
                return EnemyState.Idle;
            }

            bool wasAware =
                currentState != EnemyState.Idle;
            float awarenessRange = wasAware
                ? Math.Max(detectionRange, loseTargetRange)
                : detectionRange;
            return Math.Abs(horizontalDistance)
                    <= awarenessRange
                ? EnemyState.AttackWindup
                : EnemyState.Idle;
        }

        public static bool IsInsideFireWindow(
            bool targetAvailable,
            float horizontalDistance,
            float verticalDistance,
            float maximumFireRange,
            float verticalTolerance)
        {
            // 선딜이 끝난 발사 순간에도 거리를 다시 검사해 화면 밖으로 도망친 대상을 향해 발사하지 않는다.
            return targetAvailable
                && maximumFireRange >= 0f
                && verticalTolerance >= 0f
                && Math.Abs(horizontalDistance)
                    <= maximumFireRange
                && Math.Abs(verticalDistance)
                    <= verticalTolerance;
        }
    }
}
