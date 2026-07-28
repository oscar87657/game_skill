// GOLDEN STANDARD
// 목적: 적 인공지능의 현재 행동을 명시적인 상태 이름으로 표현한다.
// 책임: 일반 적이 공유할 대기·추적·공격·돌진·피격·사망 상태 계약만 정의한다.
// 불변식: 상태 값은 행동의 의미만 나타내며 시간과 Unity 오브젝트 참조를 소유하지 않는다.
// 선택 이유: 문자열과 Animator 상태에 판단을 숨기지 않아 전환 규칙을 코드와 테스트에서 함께 읽을 수 있다.
namespace GameSkill
{
    public enum EnemyState
    {
        Idle,
        Chase,
        AttackWindup,
        Charge,
        AttackRecovery,
        Hurt,
        Dead
    }
}
