// GOLDEN STANDARD
// 목적: 물리 이벤트 순서와 무관하게 구역 경계 완충 판정이 현재 구역을 안정적으로 유지하는지 검증한다.
// 책임: 내부 임계값, 겹친 캡슐 위치와 양방향 전환의 정상·경계 흐름을 확인한다.
// 불변식: 각 테스트는 생성한 Unity 오브젝트와 ScriptableObject를 종료 전에 모두 정리한다.
// 선택 이유: 카메라 이탈 버그의 원인인 Trigger 동시 접촉을 순수 좌표와 실제 Collider Bounds로 재현할 수 있다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class WorldZoneBoundaryTests
    {
        [TestCase(6.4f, -8.5f, 6.5f, 0.45f, false)]
        [TestCase(6f, -8.5f, 6.5f, 0.45f, true)]
        [TestCase(6.6f, 6.5f, 24.5f, 0.45f, false)]
        [TestCase(7f, 6.5f, 24.5f, 0.45f, true)]
        public void IsInsideHorizontalInterior_UsesEntryInset(
            float positionX,
            float minimum,
            float maximum,
            float inset,
            bool expected)
        {
            // 맞닿은 Trigger 사이에서 양쪽 내부 임계점을 넘기 전까지 중립 구간이 유지되는지 검증한다.
            bool result =
                WorldZoneBoundaryMath
                    .IsInsideHorizontalInterior(
                        positionX,
                        minimum,
                        maximum,
                        inset);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void OverlappingBoundary_KeepsTraversalUntilStartInterior()
        {
            // 실험실에서 왼쪽으로 살짝 이동한 캡슐이 시작 홀 판정을 빼앗는 회귀를 재현한다.
            WorldZoneDefinition startZone =
                CreateZone("start_hall", "시작 홀");
            WorldZoneDefinition traversalZone =
                CreateZone("traversal_lab", "이동 실험실");
            var player = new GameObject("BoundaryTestPlayer");
            GameObject startObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject traversalObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                PlayerWorldState state =
                    player.AddComponent<PlayerWorldState>();
                WorldZoneVolume startVolume =
                    startObject.AddComponent<WorldZoneVolume>();
                WorldZoneVolume traversalVolume =
                    traversalObject.AddComponent<WorldZoneVolume>();
                startVolume.Configure(startZone, 0.45f);
                traversalVolume.Configure(
                    traversalZone,
                    0.45f);
                startObject.transform.position =
                    new Vector3(-1f, 4f, 0f);
                startObject.transform.localScale =
                    new Vector3(15f, 10f, 3.5f);
                traversalObject.transform.position =
                    new Vector3(15.5f, 4f, 0f);
                traversalObject.transform.localScale =
                    new Vector3(18f, 10f, 3.5f);
                Physics.SyncTransforms();

                Assert.That(
                    state.EnterZone(traversalZone),
                    Is.True);
                player.transform.position =
                    new Vector3(6.4f, 0.05f, 0f);

                Assert.That(
                    startVolume.IsInsideEntryInterior(
                        player.transform.position),
                    Is.False);
                Assert.That(
                    traversalVolume.IsInsideEntryInterior(
                        player.transform.position),
                    Is.False);
                Assert.That(
                    state.CurrentZone,
                    Is.SameAs(traversalZone));

                player.transform.position =
                    new Vector3(6f, 0.05f, 0f);
                Assert.That(
                    startVolume.IsInsideEntryInterior(
                        player.transform.position),
                    Is.True);
                Assert.That(startVolume.Enter(state), Is.True);
                Assert.That(
                    state.CurrentZone,
                    Is.SameAs(startZone));
            }
            finally
            {
                Object.DestroyImmediate(traversalObject);
                Object.DestroyImmediate(startObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(startZone);
                Object.DestroyImmediate(traversalZone);
            }
        }

        private static WorldZoneDefinition CreateZone(
            string id,
            string displayName)
        {
            // 반복되는 구역 정의 준비를 한곳에 두어 테스트가 경계 판정에 집중하게 한다.
            WorldZoneDefinition zone =
                ScriptableObject.CreateInstance<WorldZoneDefinition>();
            zone.Configure(id, displayName, $"{displayName} 경계 테스트");
            return zone;
        }
    }
}
