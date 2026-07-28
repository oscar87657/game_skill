// GOLDEN STANDARD
// 목적: 지도 노드의 숨김·방문·현재 위치 우선순위를 UI 컴포넌트와 분리해 결정한다.
// 책임: 방문 여부와 현재 여부를 하나의 명시적인 시각 상태로 변환한다.
// 불변식: 현재 위치는 방문 여부보다 항상 높은 우선순위를 가지며 모든 입력은 한 상태를 반환한다.
// 선택 이유: 작은 순수 규칙으로 분리하면 지도 아트를 교체해도 진행 상태 표현 계약을 그대로 테스트할 수 있다.
namespace GameSkill
{
    public enum WorldMapVisualState
    {
        Hidden,
        Visited,
        Current
    }

    public static class WorldMapVisualStateMath
    {
        public static WorldMapVisualState Resolve(
            bool isVisited,
            bool isCurrent)
        {
            // 현재 구역은 초기 Trigger 진입 전에도 위치를 보여 줘야 하므로 가장 먼저 판정한다.
            if (isCurrent)
            {
                return WorldMapVisualState.Current;
            }

            return isVisited
                ? WorldMapVisualState.Visited
                : WorldMapVisualState.Hidden;
        }
    }
}
