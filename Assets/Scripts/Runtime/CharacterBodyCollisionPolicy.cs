// GOLDEN STANDARD
// 목적: 플레이어와 적의 몸 충돌만 통과시키고 환경·공격 판정 충돌은 유지한다.
// 책임: 전용 Body 레이어 번호를 정의하고 런타임 시작 전에 레이어 충돌 정책을 적용한다.
// 불변식: PlayerBody와 EnemyBody, EnemyBody끼리만 무시하며 두 레이어와 Default 환경은 계속 충돌한다.
// 선택 이유: 개별 Collider 쌍을 등록하는 방식보다 생성되는 적 수에 무관하고 물리 비용과 관리 지점이 일정하다.
using UnityEngine;

namespace GameSkill
{
    public static class CharacterBodyCollisionPolicy
    {
        public const int PlayerBodyLayer = 6;
        public const int EnemyBodyLayer = 7;
        public const string PlayerBodyLayerName =
            "PlayerBody";
        public const string EnemyBodyLayerName =
            "EnemyBody";

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            // 첫 Scene의 CharacterController가 움직이기 전에 전역 몸 충돌 정책을 적용한다.
            Apply();
        }

        public static void Apply()
        {
            // 플레이어와 적은 서로 밀지 않지만 각자의 공격 조회와 환경 접촉은 그대로 유지한다.
            Physics.IgnoreLayerCollision(
                PlayerBodyLayer,
                EnemyBodyLayer,
                true);
            Physics.IgnoreLayerCollision(
                EnemyBodyLayer,
                EnemyBodyLayer,
                true);
        }

        public static bool IsApplied()
        {
            // 자동 테스트와 진단 코드가 두 필수 규칙을 한 번에 확인할 수 있게 한다.
            return Physics.GetIgnoreLayerCollision(
                    PlayerBodyLayer,
                    EnemyBodyLayer)
                && Physics.GetIgnoreLayerCollision(
                    EnemyBodyLayer,
                    EnemyBodyLayer);
        }
    }
}
