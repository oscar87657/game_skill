// GOLDEN STANDARD
// 목적: 전투 콤보에서 사용하는 결정적인 계산 규칙을 MonoBehaviour 상태와 분리한다.
// 책임: 다음 콤보 단계와 단계별 데미지를 안전한 범위에서 계산한다.
// 불변식: 잘못된 단계나 콤보 길이가 들어와도 예외 대신 유효한 값을 반환한다.
// 선택 이유: 순수 함수로 분리하면 프레임 시간이나 씬 없이 EditMode 테스트가 가능하다.
using UnityEngine;

namespace GameSkill
{
    public static class CombatMath
    {
        public static int NextComboStep(int currentStep, int comboLength)
        {
            // 콤보 길이가 잘못 설정되어도 최소 한 단계의 공격은 유지한다.
            int safeLength = Mathf.Max(1, comboLength);
            int safeCurrentStep = Mathf.Clamp(currentStep, 0, safeLength);
            return safeCurrentStep >= safeLength ? 1 : safeCurrentStep + 1;
        }

        public static int DamageForComboStep(
            int baseDamage,
            int comboStep,
            int finisherBonus)
        {
            // 마지막 단계 판정은 호출부 데이터와 분리하고, 데미지는 항상 1 이상으로 제한한다.
            int safeBaseDamage = Mathf.Max(1, baseDamage);
            int safeStep = Mathf.Max(1, comboStep);
            return safeStep >= 3
                ? safeBaseDamage + Mathf.Max(0, finisherBonus)
                : safeBaseDamage;
        }
    }
}
