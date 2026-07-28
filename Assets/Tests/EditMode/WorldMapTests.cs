// GOLDEN STANDARD
// 목적: Canvas 배치와 무관하게 지도 상태 우선순위와 Presenter 이벤트 갱신 계약을 검증한다.
// 책임: 숨김·방문·현재 상태, 구역 재진입과 연결선 발견의 정상 흐름을 확인한다.
// 불변식: 각 테스트는 생성한 Unity 오브젝트와 ScriptableObject를 종료 전에 모두 정리한다.
// 선택 이유: 지도 아트 없이도 진행 ID가 올바른 노드 상태로 변환되는지 빠르게 회귀 테스트할 수 있다.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class WorldMapTests
    {
        [TestCase(false, false, WorldMapVisualState.Hidden)]
        [TestCase(true, false, WorldMapVisualState.Visited)]
        [TestCase(false, true, WorldMapVisualState.Current)]
        [TestCase(true, true, WorldMapVisualState.Current)]
        public void Resolve_PrioritizesCurrentZone(
            bool isVisited,
            bool isCurrent,
            WorldMapVisualState expected)
        {
            // 초기 Trigger 이전의 현재 구역도 방문 노드보다 강하게 표시되는지 검증한다.
            Assert.That(
                WorldMapVisualStateMath.Resolve(
                    isVisited,
                    isCurrent),
                Is.EqualTo(expected));
        }

        [Test]
        public void Presenter_UpdatesNodesAndConnectionFromWorldEvents()
        {
            // 시작 홀에서 실험실로 이동하는 실제 이벤트 순서가 지도 노드와 연결을 함께 갱신하는지 확인한다.
            WorldZoneDefinition startZone =
                CreateZone("start_hall", "시작 홀");
            WorldZoneDefinition traversalZone =
                CreateZone("traversal_lab", "이동 실험실");
            var player = new GameObject("WorldMapStateTestPlayer");
            var presenterObject =
                new GameObject("WorldMapPresenterTest");
            try
            {
                PlayerWorldState state =
                    player.AddComponent<PlayerWorldState>();
                var startNode = new WorldMapNodeView(
                    startZone,
                    "START",
                    null,
                    null);
                var traversalNode = new WorldMapNodeView(
                    traversalZone,
                    "LAB",
                    null,
                    null);
                var connection =
                    new WorldMapConnectionView(
                        startZone,
                        traversalZone,
                        null);
                WorldMapPresenter presenter =
                    presenterObject
                        .AddComponent<WorldMapPresenter>();
                presenter.Configure(
                    state,
                    startZone,
                    new List<WorldMapNodeView>
                    {
                        startNode,
                        traversalNode
                    },
                    new List<WorldMapConnectionView>
                    {
                        connection
                    });

                Assert.That(
                    presenter.GetNodeState("start_hall"),
                    Is.EqualTo(WorldMapVisualState.Current));
                Assert.That(
                    presenter.GetNodeState("traversal_lab"),
                    Is.EqualTo(WorldMapVisualState.Hidden));
                Assert.That(connection.IsRevealed, Is.False);

                Assert.That(state.EnterZone(startZone), Is.True);
                Assert.That(
                    state.EnterZone(traversalZone),
                    Is.True);

                Assert.That(
                    presenter.GetNodeState("start_hall"),
                    Is.EqualTo(WorldMapVisualState.Visited));
                Assert.That(
                    presenter.GetNodeState("traversal_lab"),
                    Is.EqualTo(WorldMapVisualState.Current));
                Assert.That(connection.IsRevealed, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(startZone);
                Object.DestroyImmediate(traversalZone);
            }
        }

        private static WorldZoneDefinition CreateZone(
            string id,
            string displayName)
        {
            // 반복되는 구역 정의 준비를 한곳에 두어 테스트가 지도 표현 규칙에 집중하게 한다.
            WorldZoneDefinition zone =
                ScriptableObject.CreateInstance<WorldZoneDefinition>();
            zone.Configure(id, displayName, $"{displayName} 지도 테스트");
            return zone;
        }
    }
}
