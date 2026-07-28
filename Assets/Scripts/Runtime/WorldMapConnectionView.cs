// GOLDEN STANDARD
// 목적: 두 구역 사이의 연결 발견 상태를 지도 선의 색상으로 표현한다.
// 책임: 양 끝 구역과 uGUI 선을 보관하고 두 구역이 모두 방문됐을 때 연결을 강조한다.
// 불변식: 한쪽이라도 미방문이면 연결은 숨김색을 유지하며 월드 진행 상태를 직접 변경하지 않는다.
// 선택 이유: 노드와 연결 표현을 분리하면 이후 지름길·잠긴 통로 선을 독립적인 규칙으로 확장할 수 있다.
using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameSkill
{
    [Serializable]
    public sealed class WorldMapConnectionView
    {
        private static readonly Color HiddenColor =
            new(0.2f, 0.22f, 0.27f, 0.65f);
        private static readonly Color RevealedColor =
            new(0.2f, 0.7f, 0.8f, 0.95f);

        [SerializeField] private WorldZoneDefinition firstZone;
        [SerializeField] private WorldZoneDefinition secondZone;
        [SerializeField] private Image line;

        public WorldZoneDefinition FirstZone => firstZone;
        public WorldZoneDefinition SecondZone => secondZone;
        public Image Line => line;
        public bool IsRevealed { get; private set; }

        public WorldMapConnectionView(
            WorldZoneDefinition first,
            WorldZoneDefinition second,
            Image lineImage)
        {
            // 생성과 재구성이 같은 데이터 저장 규칙을 사용하도록 Configure에 위임한다.
            Configure(first, second, lineImage);
        }

        public bool Configure(
            WorldZoneDefinition first,
            WorldZoneDefinition second,
            Image lineImage)
        {
            bool changed = firstZone != first
                || secondZone != second
                || line != lineImage;
            firstZone = first;
            secondZone = second;
            line = lineImage;
            return changed;
        }

        public void Apply(PlayerWorldState worldState)
        {
            // 양 끝 구역을 모두 방문해야 실제 연결을 확인한 것으로 지도에 표시한다.
            IsRevealed = worldState != null
                && worldState.HasVisited(firstZone)
                && worldState.HasVisited(secondZone);
            if (line != null)
            {
                line.color = IsRevealed
                    ? RevealedColor
                    : HiddenColor;
            }
        }
    }
}
