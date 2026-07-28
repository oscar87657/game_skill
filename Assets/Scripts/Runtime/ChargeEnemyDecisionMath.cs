// GOLDEN STANDARD
// 목적: 돌진 적의 탐지·방향 잠금·접촉 판정을 Unity 생명주기 없이 계산한다.
// 책임: 대상 유효성·거리·높이·인지 이력으로 공격 준비 여부와 돌진 방향·접촉 범위를 결정한다.
// 불변식: 음수 범위와 유효하지 않은 대상은 공격하지 않으며 사망 상태는 거리 변화로 해제되지 않는다.
// 선택 이유: 순간 판단을 순수 함수로 분리하면 CharacterController와 프레임 시간 없이 경계값을 검증할 수 있다.
using System;

namespace GameSkill
{
    public static class ChargeEnemyDecisionMath
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
            // Health가 복구되기 전에는 어떤 거리 조건도 사망 상태를 공격 준비로 바꾸지 않는다.
            if (currentState == EnemyState.Dead)
            {
                return EnemyState.Dead;
            }

            // 대상이나 설정이 유효하지 않으면 예측 가능한 대기 상태로 수렴시킨다.
            if (!targetAvailable
                || detectionRange < 0f
                || loseTargetRange < 0f
                || verticalTolerance < 0f)
            {
                return EnemyState.Idle;
            }

            if (Math.Abs(verticalDistance) > verticalTolerance)
            {
                // 다른 층의 플레이어를 향해 발판 밖으로 돌진하지 않도록 높이 차를 제한한다.
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

        public static int ResolveChargeDirection(
            float horizontalDistance,
            int fallbackDirection)
        {
            // 거의 같은 X 좌표에서는 마지막 방향을 유지해 0 방향 돌진이 생기지 않게 한다.
            if (Math.Abs(horizontalDistance) <= 0.01f)
            {
                return fallbackDirection < 0 ? -1 : 1;
            }

            return horizontalDistance < 0f ? -1 : 1;
        }

        public static bool IsInsideContactWindow(
            bool targetAvailable,
            float horizontalDistance,
            float verticalDistance,
            float horizontalRange,
            float verticalRange)
        {
            // 돌진 중 몸 접촉은 X와 Y 허용 범위를 모두 만족할 때만 한 번 성립한다.
            return targetAvailable
                && horizontalRange >= 0f
                && verticalRange >= 0f
                && Math.Abs(horizontalDistance)
                    <= horizontalRange
                && Math.Abs(verticalDistance)
                    <= verticalRange;
        }
    }
}
