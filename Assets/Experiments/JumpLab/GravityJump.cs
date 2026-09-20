// GOLDEN STANDARD
// 목적: 일정 중력 점프의 한 프레임 이동량을 계산한다.
// 책임: 속도 적분만 담당하며 입력·충돌·애니메이션을 모른다.
namespace GameSkill.Experiments
{
    public static class GravityJump
    {
        public static float Step(ref float velocity, float gravity, float deltaTime)
        {
            // 평균 속도를 사용해 프레임 간격에 따른 탄도 오차를 줄인다.
            float displacement = velocity * deltaTime - 0.5f * gravity * deltaTime * deltaTime;
            velocity -= gravity * deltaTime;
            return displacement;
        }
    }
}
