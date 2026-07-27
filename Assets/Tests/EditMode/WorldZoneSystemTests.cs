// GOLDEN STANDARD
// 목적: 씬 배치와 무관하게 월드 구역 정의·방문 상태·진입 볼륨의 핵심 계약을 검증한다.
// 책임: 잘못된 ID, 중복 방문, 최초 방문 이벤트와 Trigger 설정의 정상·경계 흐름을 확인한다.
// 불변식: 각 테스트는 자신이 만든 Unity 오브젝트와 ScriptableObject를 종료 전에 모두 정리한다.
// 선택 이유: 구역 진행 규칙을 EditMode에서 검증하면 지도·저장 오류와 물리 배치 오류를 구분할 수 있다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class WorldZoneSystemTests
    {
        [Test]
        public void WorldZoneDefinition_RejectsEmptyId()
        {
            // 저장과 지도에서 식별할 수 없는 구역 정의가 유효해지는 회귀를 막는다.
            WorldZoneDefinition zone =
                ScriptableObject.CreateInstance<WorldZoneDefinition>();
            try
            {
                bool configured = zone.Configure(
                    "  ",
                    "잘못된 구역",
                    "ID가 없어야 한다.");

                Assert.That(configured, Is.False);
                Assert.That(zone.IsConfigured, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(zone);
            }
        }

        [Test]
        public void PlayerWorldState_VisitsZoneOnlyOnce()
        {
            // 같은 ID의 중복 진입이 방문 수와 지도 갱신 이벤트를 두 번 늘리지 않는지 검증한다.
            WorldZoneDefinition zone = CreateZone(
                "start_hall",
                "시작 홀");
            WorldZoneDefinition sameIdZone = CreateZone(
                "start_hall",
                "같은 ID의 다른 정의");
            var player = new GameObject("WorldStateTestPlayer");
            try
            {
                PlayerWorldState state =
                    player.AddComponent<PlayerWorldState>();
                int visitEvents = 0;
                state.ZoneVisited += _ => visitEvents++;

                bool firstVisit = state.TryVisit(zone);
                bool duplicateVisit = state.TryVisit(sameIdZone);

                Assert.That(firstVisit, Is.True);
                Assert.That(duplicateVisit, Is.False);
                Assert.That(state.HasVisited(zone), Is.True);
                Assert.That(state.HasVisited(sameIdZone), Is.True);
                Assert.That(state.HasVisitedId("start_hall"), Is.True);
                Assert.That(state.VisitedCount, Is.EqualTo(1));
                Assert.That(visitEvents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(zone);
                Object.DestroyImmediate(sameIdZone);
            }
        }

        [Test]
        public void WorldZoneVolume_RecordsEntryAndKeepsTrigger()
        {
            // 실제 볼륨 호출이 방문 상태를 기록하고 이동을 막지 않는지 함께 확인한다.
            WorldZoneDefinition zone = CreateZone(
                "traversal_lab",
                "이동 실험실");
            var player = new GameObject("ZoneVolumeTestPlayer");
            var volumeObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                PlayerWorldState state =
                    player.AddComponent<PlayerWorldState>();
                WorldZoneVolume volume =
                    volumeObject.AddComponent<WorldZoneVolume>();
                Assert.That(volume.Configure(zone), Is.True);

                Assert.That(volume.Enter(state), Is.True);
                Assert.That(volume.Enter(state), Is.False);
                Assert.That(state.HasVisited(zone), Is.True);
                Assert.That(
                    volumeObject.GetComponent<Collider>().isTrigger,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(volumeObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(zone);
            }
        }

        private static WorldZoneDefinition CreateZone(
            string id,
            string displayName)
        {
            // 반복되는 ScriptableObject 준비를 한곳에 두어 각 테스트가 방문 규칙에 집중하게 한다.
            WorldZoneDefinition zone =
                ScriptableObject.CreateInstance<WorldZoneDefinition>();
            zone.Configure(id, displayName, $"{displayName} 테스트 정의");
            return zone;
        }
    }
}
