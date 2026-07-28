// GOLDEN STANDARD
// 목적: 능력 시험 보스의 활성 조건·패턴 순환·지면 충격 안전 판정을 순수 계산한다.
// 책임: 능력 보유·거리·높이·체력·플레이어 높이로 보스 판단 결과를 반환한다.
// 불변식: 음수 범위와 유효하지 않은 대상은 활성화하지 않으며 패턴은 정해진 순서로 순환한다.
// 선택 이유: 보스 패턴 선택을 표현과 투사체 생성에서 분리해 경계값을 빠르고 결정적으로 검증한다.
using System;

namespace GameSkill
{
    public static class BossPatternDecisionMath
    {
        public static bool CanEngage(
            bool targetAvailable,
            bool allAbilitiesUnlocked,
            float horizontalDistance,
            float verticalDistance,
            float activationRange,
            float verticalTolerance)
        {
            // 플레이어 생존·능력 관문·거리·높이를 모두 만족해야 전투를 시작한다.
            return targetAvailable
                && allAbilitiesUnlocked
                && activationRange >= 0f
                && verticalTolerance >= 0f
                && Math.Abs(horizontalDistance)
                    <= activationRange
                && Math.Abs(verticalDistance)
                    <= verticalTolerance;
        }

        public static BossPattern NextPattern(
            BossPattern currentPattern)
        {
            // 세 능력 시험을 고정된 순서로 반복해 플레이어가 다음 대응을 학습할 수 있게 한다.
            return currentPattern switch
            {
                BossPattern.GroundWave =>
                    BossPattern.AirBurst,
                BossPattern.AirBurst =>
                    BossPattern.GroundPulse,
                _ =>
                    BossPattern.GroundWave
            };
        }

        public static bool IsGroundPulseSafe(
            float targetFootHeight,
            float arenaFloorHeight,
            float requiredHeight)
        {
            // 지면 기준 높이가 안전선 이상이면 2단 점프나 벽 잡기로 충격을 피한 것으로 판정한다.
            return requiredHeight >= 0f
                && targetFootHeight - arenaFloorHeight
                    >= requiredHeight;
        }

        public static bool IsSecondPhase(
            int currentHealth,
            int maximumHealth)
        {
            // 살아 있는 동안 체력이 절반 이하가 된 경우에만 두 번째 페이즈로 분류한다.
            return maximumHealth > 0
                && currentHealth > 0
                && currentHealth * 2 <= maximumHealth;
        }
    }
}
