// GOLDEN STANDARD
// 목적: 씬 배치와 무관하게 지름길 진행 상태·게이트·활성 장치의 핵심 계약을 검증한다.
// 책임: 빈 ID, 중복 해금, 이벤트 갱신과 성공한 Trigger 소비 흐름을 확인한다.
// 불변식: 각 테스트는 생성한 Unity 오브젝트를 종료 전에 모두 정리한다.
// 선택 이유: 영구 진행 규칙을 EditMode에서 검증하면 물리 배치 문제와 상태 오류를 분리할 수 있다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class WorldShortcutSystemTests
    {
        [Test]
        public void PlayerWorldState_UnlocksShortcutOnlyOnce()
        {
            // 같은 ID의 중복 활성화가 해금 수와 저장 갱신 이벤트를 두 번 늘리지 않는지 검증한다.
            var player = new GameObject("ShortcutStateTestPlayer");
            try
            {
                PlayerWorldState state =
                    player.AddComponent<PlayerWorldState>();
                int unlockEvents = 0;
                state.ShortcutUnlocked += _ => unlockEvents++;

                Assert.That(
                    state.TryUnlockShortcut("shaft_return"),
                    Is.True);
                Assert.That(
                    state.TryUnlockShortcut("shaft_return"),
                    Is.False);
                Assert.That(
                    state.TryUnlockShortcut("  "),
                    Is.False);
                Assert.That(
                    state.IsShortcutUnlocked("shaft_return"),
                    Is.True);
                Assert.That(
                    state.UnlockedShortcutCount,
                    Is.EqualTo(1));
                Assert.That(unlockEvents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void WorldShortcutGate_OpensFromPlayerStateEvent()
        {
            // 세이브 복원처럼 상태가 먼저 해금돼도 구독 중인 게이트가 즉시 열리는지 확인한다.
            var player = new GameObject("ShortcutGateTestPlayer");
            GameObject gateObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                PlayerWorldState state =
                    player.AddComponent<PlayerWorldState>();
                WorldShortcutGate gate =
                    gateObject.AddComponent<WorldShortcutGate>();
                Renderer renderer =
                    gateObject.GetComponent<Renderer>();
                gate.Configure("shaft_return", state, renderer);

                Assert.That(gate.IsLocked, Is.True);
                Assert.That(
                    gateObject.GetComponent<Collider>().enabled,
                    Is.True);

                Assert.That(
                    state.TryUnlockShortcut("shaft_return"),
                    Is.True);

                Assert.That(gate.IsLocked, Is.False);
                Assert.That(
                    gateObject.GetComponent<Collider>().enabled,
                    Is.False);
                Assert.That(renderer.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gateObject);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ShortcutUnlockVolume_UnlocksGateAndConsumesItself()
        {
            // 반대편 Trigger 접촉이 영구 상태·게이트 충돌·활성 장치 표현을 함께 갱신하는지 검증한다.
            var player = new GameObject("ShortcutVolumeTestPlayer");
            GameObject gateObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject activatorObject =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                PlayerWorldState state =
                    player.AddComponent<PlayerWorldState>();
                WorldShortcutGate gate =
                    gateObject.AddComponent<WorldShortcutGate>();
                gate.Configure(
                    "shaft_return",
                    state,
                    gateObject.GetComponent<Renderer>());

                ShortcutUnlockVolume activator =
                    activatorObject.AddComponent<ShortcutUnlockVolume>();
                Renderer activatorRenderer =
                    activatorObject.GetComponent<Renderer>();
                activator.Configure(gate, activatorRenderer);

                Assert.That(activator.Activate(state), Is.True);
                Assert.That(activator.Activate(state), Is.False);
                Assert.That(activator.IsActivated, Is.True);
                Assert.That(gate.IsLocked, Is.False);
                Assert.That(
                    state.IsShortcutUnlocked("shaft_return"),
                    Is.True);
                Assert.That(
                    activatorObject.GetComponent<Collider>().enabled,
                    Is.False);
                Assert.That(activatorRenderer.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(activatorObject);
                Object.DestroyImmediate(gateObject);
                Object.DestroyImmediate(player);
            }
        }
    }
}
