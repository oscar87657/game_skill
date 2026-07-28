// GOLDEN STANDARD
// 목적: 하나의 월드 구역 진행 상태를 지도 노드의 색상과 짧은 레이블로 표현한다.
// 책임: 구역·표시 문자열·uGUI 참조를 보관하고 숨김·방문·현재 상태를 적용한다.
// 불변식: 미방문 노드는 이름을 공개하지 않으며 현재 노드는 방문 노드보다 강한 강조색을 사용한다.
// 선택 이유: 직렬화 가능한 View 객체로 분리하면 Presenter가 UI 계층 검색 없이 진행 데이터에만 집중할 수 있다.
using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameSkill
{
    [Serializable]
    public sealed class WorldMapNodeView
    {
        private static readonly Color HiddenColor =
            new(0.16f, 0.18f, 0.22f, 0.9f);
        private static readonly Color VisitedColor =
            new(0.18f, 0.72f, 0.82f, 1f);
        private static readonly Color CurrentColor =
            new(1f, 0.5f, 0.12f, 1f);

        [SerializeField] private WorldZoneDefinition zone;
        [SerializeField] private string mapLabel;
        [SerializeField] private Image background;
        [SerializeField] private Text label;

        public WorldZoneDefinition Zone => zone;
        public string MapLabel => mapLabel;
        public Image Background => background;
        public Text Label => label;
        public WorldMapVisualState State { get; private set; }
        public bool IsConfigured =>
            zone != null && zone.IsConfigured;

        public WorldMapNodeView(
            WorldZoneDefinition zoneDefinition,
            string nodeLabel,
            Image backgroundImage,
            Text labelText)
        {
            // 에디터 빌더와 테스트가 같은 구성 경로를 사용하도록 생성자에서 모든 참조를 저장한다.
            Configure(
                zoneDefinition,
                nodeLabel,
                backgroundImage,
                labelText);
        }

        public bool Configure(
            WorldZoneDefinition zoneDefinition,
            string nodeLabel,
            Image backgroundImage,
            Text labelText)
        {
            string normalizedLabel =
                nodeLabel?.Trim() ?? string.Empty;
            bool changed = zone != zoneDefinition
                || mapLabel != normalizedLabel
                || background != backgroundImage
                || label != labelText;
            zone = zoneDefinition;
            mapLabel = normalizedLabel;
            background = backgroundImage;
            label = labelText;
            return changed;
        }

        public bool Matches(string zoneId)
        {
            // 빈 조회 ID와 설정되지 않은 노드는 어떤 구역에도 일치하지 않는다.
            if (!IsConfigured
                || string.IsNullOrWhiteSpace(zoneId))
            {
                return false;
            }

            return string.Equals(
                zone.Id,
                zoneId.Trim(),
                StringComparison.Ordinal);
        }

        public void Apply(
            bool isVisited,
            bool isCurrent)
        {
            // 순수 우선순위 규칙으로 상태를 결정한 뒤 uGUI 표현은 선택적으로 갱신한다.
            State = WorldMapVisualStateMath.Resolve(
                isVisited,
                isCurrent);
            if (background != null)
            {
                background.color = State switch
                {
                    WorldMapVisualState.Current => CurrentColor,
                    WorldMapVisualState.Visited => VisitedColor,
                    _ => HiddenColor
                };
            }

            if (label != null)
            {
                // 미방문 구역은 물음표만 보여 탐색 전 이름과 구조의 과도한 노출을 막는다.
                label.text = State == WorldMapVisualState.Hidden
                    ? "?"
                    : mapLabel;
            }
        }
    }
}
