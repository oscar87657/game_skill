// GOLDEN STANDARD
// 목적: 플레이어와 적이 공유하는 데미지 허용 규칙을 한 경로로 통합한다.
// 책임: 대상 생존·양수 데미지·무적 여부를 검사한 뒤 Health에 데미지를 위임한다.
// 불변식: Health 내부 값을 직접 수정하지 않으며 무적 대상과 사망 대상에는 데미지를 적용하지 않는다.
// 선택 이유: 환경 위험과 적 공격이 서로 다른 무적 판정을 갖는 규칙 중복을 방지한다.
namespace GameSkill
{
    public static class DamageRules
    {
        public static bool TryApply(
            Health targetHealth,
            bool isInvulnerable,
            int damage)
        {
            // 공통 입구에서 잘못된 공격을 거부하면 모든 공격 구현이 같은 안전 조건을 갖는다.
            if (targetHealth == null
                || targetHealth.IsDead
                || isInvulnerable
                || damage <= 0)
            {
                return false;
            }

            return targetHealth.TakeDamage(damage);
        }
    }
}
