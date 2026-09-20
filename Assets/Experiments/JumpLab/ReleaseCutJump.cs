// GOLDEN STANDARD
// 목적: 버튼을 놓으면 상승을 줄이는 가변 높이 점프를 비교한다.
// 책임: 상승 속도 제한만 추가하고 중력 적분은 동일한 기준 계산을 사용한다.
namespace GameSkill.Experiments
{
    public static class ReleaseCutJump
    {
        public static float Step(ref float velocity, float gravity, float releaseSpeed,
            bool held, float deltaTime)
        {
            // 하강은 바꾸지 않고 남은 상승 속도만 제한해 탭과 홀드를 구분한다.
            if (!held && velocity > releaseSpeed)
                velocity = releaseSpeed;
            return GravityJump.Step(ref velocity, gravity, deltaTime);
        }
    }
}
