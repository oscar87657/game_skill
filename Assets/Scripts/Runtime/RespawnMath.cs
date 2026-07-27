// GOLDEN STANDARD
// 목적: 플레이어 재시작 목적지를 씬 상태 없이 결정하는 순수 수식을 제공한다.
// 책임: 체크포인트 보유 여부에 따라 마지막 체크포인트 또는 최초 시작 위치를 선택한다.
// 불변식: 입력 위치를 변경하지 않으며 같은 입력에는 항상 같은 위치를 반환한다.
// 선택 이유: 사망 연출과 위치 선택 규칙을 분리하면 체크포인트가 없는 예외 흐름을 빠르게 테스트할 수 있다.
using UnityEngine;

namespace GameSkill
{
    public static class RespawnMath
    {
        public static Vector3 ResolveDestination(
            bool hasCheckpoint,
            Vector3 checkpointPosition,
            Vector3 initialPosition)
        {
            // 체크포인트를 활성화하기 전 사망하면 씬 최초 위치를 안전한 대체 지점으로 사용한다.
            return hasCheckpoint ? checkpointPosition : initialPosition;
        }
    }
}
