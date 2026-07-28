// GOLDEN STANDARD
// 목적: 능력 시험 보스가 순환할 공격 패턴을 명시적인 이름으로 표현한다.
// 책임: 지상 파동·공중 탄막·지면 충격의 의미만 정의한다.
// 불변식: 패턴 값은 실행 시간과 Unity 오브젝트 참조를 소유하지 않는다.
// 선택 이유: 패턴 순서를 숫자나 문자열 대신 타입으로 표현해 문서와 테스트를 같은 용어로 유지한다.
namespace GameSkill
{
    public enum BossPattern
    {
        GroundWave,
        AirBurst,
        GroundPulse
    }
}
