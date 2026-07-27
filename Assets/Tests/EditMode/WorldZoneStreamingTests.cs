// GOLDEN STANDARD
// 목적: 실제 Scene 로딩과 분리해 구역 진입 이벤트·Scene 바인딩·스트리밍 요청 계약을 검증한다.
// 책임: 재방문 전환, 잘못된 경로, ID 조회와 컨트롤러 구성의 정상·경계 흐름을 확인한다.
// 불변식: 각 테스트는 생성한 Unity 오브젝트와 ScriptableObject를 종료 전에 모두 정리한다.
// 선택 이유: 데이터와 상태 전환을 EditMode에서 검증하면 비동기 Scene 실패와 도메인 오류를 구분할 수 있다.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class WorldZoneStreamingTests
    {
        [Test]
        public void PlayerWorldState_EmitsEntryAgainAfterZoneTransition()
        {
            // 방문 이벤트는 최초 두 번, 진입 이벤트는 A→B→A 세 번 발생하는지 검증한다.
            WorldZoneDefinition startZone =
                CreateZone("start_hall", "시작 홀");
            WorldZoneDefinition traversalZone =
                CreateZone("traversal_lab", "이동 실험실");
            var player = new GameObject("ZoneEntryStateTestPlayer");
            try
            {
                PlayerWorldState state =
                    player.AddComponent<PlayerWorldState>();
                int visitEvents = 0;
                int entryEvents = 0;
                state.ZoneVisited += _ => visitEvents++;
                state.ZoneEntered += _ => entryEvents++;

                Assert.That(state.EnterZone(startZone), Is.True);
                Assert.That(state.EnterZone(startZone), Is.False);
                Assert.That(state.EnterZone(traversalZone), Is.True);
                Assert.That(state.EnterZone(startZone), Is.True);

                Assert.That(state.VisitedCount, Is.EqualTo(2));
                Assert.That(visitEvents, Is.EqualTo(2));
                Assert.That(entryEvents, Is.EqualTo(3));
                Assert.That(state.CurrentZone, Is.SameAs(startZone));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(startZone);
                Object.DestroyImmediate(traversalZone);
            }
        }

        [Test]
        public void WorldZoneSceneBinding_RequiresZoneAndScenePath()
        {
            // Scene 경로가 없는 매핑이 로드 가능한 구역으로 노출되지 않는지 확인한다.
            WorldZoneDefinition zone =
                CreateZone("start_hall", "시작 홀");
            try
            {
                var invalidBinding =
                    new WorldZoneSceneBinding(zone, "  ");
                var validBinding =
                    new WorldZoneSceneBinding(
                        zone,
                        "Assets/Scenes/Zones/Zone_StartHall.unity");

                Assert.That(invalidBinding.IsConfigured, Is.False);
                Assert.That(validBinding.IsConfigured, Is.True);
                Assert.That(validBinding.Matches("start_hall"), Is.True);
                Assert.That(validBinding.Matches("other_zone"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(zone);
            }
        }

        [Test]
        public void WorldZoneStreamController_ResolvesConfiguredPaths()
        {
            // 에디터 구성 단계에서도 구역 ID가 결정적인 Additive Scene 경로로 조회되는지 검증한다.
            WorldZoneDefinition startZone =
                CreateZone("start_hall", "시작 홀");
            WorldZoneDefinition traversalZone =
                CreateZone("traversal_lab", "이동 실험실");
            var player = new GameObject("StreamingStateTestPlayer");
            var controllerObject =
                new GameObject("StreamingControllerTest");
            try
            {
                PlayerWorldState state =
                    player.AddComponent<PlayerWorldState>();
                WorldZoneStreamController controller =
                    controllerObject
                        .AddComponent<WorldZoneStreamController>();
                var bindings = new List<WorldZoneSceneBinding>
                {
                    new(
                        startZone,
                        "Assets/Scenes/Zones/Zone_StartHall.unity"),
                    new(
                        traversalZone,
                        "Assets/Scenes/Zones/Zone_TraversalLab.unity")
                };

                Assert.That(
                    controller.Configure(
                        state,
                        startZone,
                        bindings),
                    Is.True);
                Assert.That(controller.BindingCount, Is.EqualTo(2));
                Assert.That(
                    controller.TryGetScenePath(
                        "traversal_lab",
                        out string scenePath),
                    Is.True);
                Assert.That(
                    scenePath,
                    Is.EqualTo(
                        "Assets/Scenes/Zones/Zone_TraversalLab.unity"));
                Assert.That(controller.RequestZone(traversalZone), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(startZone);
                Object.DestroyImmediate(traversalZone);
            }
        }

        private static WorldZoneDefinition CreateZone(
            string id,
            string displayName)
        {
            // 반복되는 구역 정의 준비를 한곳에 두어 각 테스트가 스트리밍 계약에 집중하게 한다.
            WorldZoneDefinition zone =
                ScriptableObject.CreateInstance<WorldZoneDefinition>();
            zone.Configure(id, displayName, $"{displayName} 테스트 정의");
            return zone;
        }
    }
}
