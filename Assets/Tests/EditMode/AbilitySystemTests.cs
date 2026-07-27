// GOLDEN STANDARD
// 목적: 씬 진행과 무관하게 능력 정의·보유·픽업·게이트의 핵심 계약을 검증한다.
// 책임: 잘못된 정의, 중복 해금, 픽업 소비, 게이트 개방의 정상·경계 흐름을 확인한다.
// 불변식: 각 테스트는 자신이 만든 Unity 오브젝트를 종료 전에 모두 정리한다.
// 선택 이유: 진행 시스템의 결정적 규칙을 EditMode에서 검증하면 월드 배치 오류와 도메인 오류를 구분할 수 있다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class AbilitySystemTests
    {
        [Test]
        public void AbilityDefinition_RejectsEmptyId()
        {
            // 저장 키가 없는 능력이 유효한 에셋으로 사용되는 회귀를 막는다.
            AbilityDefinition ability =
                ScriptableObject.CreateInstance<AbilityDefinition>();
            try
            {
                bool configured = ability.Configure(
                    "  ",
                    "잘못된 능력",
                    "ID가 없어야 한다.");

                Assert.That(configured, Is.False);
                Assert.That(ability.IsConfigured, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(ability);
            }
        }

        [Test]
        public void PlayerAbilityState_UnlocksAbilityOnlyOnce()
        {
            // 같은 ID의 중복 획득이 보유 수와 이벤트를 두 번 늘리지 않는지 검증한다.
            AbilityDefinition ability = CreateAbility(
                "double_jump",
                "2단 점프");
            AbilityDefinition sameIdAbility = CreateAbility(
                "double_jump",
                "같은 ID의 다른 정의");
            var player = new GameObject("AbilityStateTestPlayer");
            try
            {
                PlayerAbilityState state =
                    player.AddComponent<PlayerAbilityState>();
                int unlockEvents = 0;
                state.AbilityUnlocked += _ => unlockEvents++;

                bool firstUnlock = state.TryUnlock(ability);
                bool duplicateUnlock = state.TryUnlock(ability);

                Assert.That(firstUnlock, Is.True);
                Assert.That(duplicateUnlock, Is.False);
                Assert.That(state.HasAbility(ability), Is.True);
                Assert.That(state.HasAbility(sameIdAbility), Is.True);
                Assert.That(state.HasAbilityId("double_jump"), Is.True);
                Assert.That(state.UnlockedCount, Is.EqualTo(1));
                Assert.That(unlockEvents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(ability);
                Object.DestroyImmediate(sameIdAbility);
            }
        }

        [Test]
        public void AbilityPickup_UnlocksAndConsumesPresentation()
        {
            // 정상 획득이 플레이어 상태와 월드 표현을 같은 호출에서 일관되게 갱신하는지 확인한다.
            AbilityDefinition ability = CreateAbility(
                "air_dash",
                "공중 대시");
            var player = new GameObject("AbilityPickupTestPlayer");
            var pickupObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                PlayerAbilityState state =
                    player.AddComponent<PlayerAbilityState>();
                AbilityPickup pickup =
                    pickupObject.AddComponent<AbilityPickup>();
                MeshRenderer renderer =
                    pickupObject.GetComponent<MeshRenderer>();
                pickup.Configure(ability, renderer);

                bool collected = pickup.Collect(state);

                Assert.That(collected, Is.True);
                Assert.That(pickup.IsCollected, Is.True);
                Assert.That(state.HasAbility(ability), Is.True);
                Assert.That(
                    pickupObject.GetComponent<Collider>().enabled,
                    Is.False);
                Assert.That(renderer.enabled, Is.False);
                Assert.That(pickup.Collect(state), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(pickupObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(ability);
            }
        }

        [Test]
        public void AbilityGate_OpensWhenRequiredAbilityIsUnlocked()
        {
            // 잠긴 물리 통로가 요구 능력 이벤트 직후 열리는 핵심 백트래킹 흐름을 검증한다.
            AbilityDefinition ability = CreateAbility(
                "wall_cling",
                "벽 잡기");
            var player = new GameObject("AbilityGateTestPlayer");
            var gateObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                PlayerAbilityState state =
                    player.AddComponent<PlayerAbilityState>();
                AbilityGate gate =
                    gateObject.AddComponent<AbilityGate>();
                Collider gateCollider =
                    gateObject.GetComponent<Collider>();
                gate.Configure(
                    ability,
                    state,
                    gateObject.GetComponent<MeshRenderer>());

                Assert.That(gate.IsLocked, Is.True);
                Assert.That(gateCollider.enabled, Is.True);

                Assert.That(state.TryUnlock(ability), Is.True);

                Assert.That(gate.IsLocked, Is.False);
                Assert.That(gateCollider.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gateObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(ability);
            }
        }

        private static AbilityDefinition CreateAbility(
            string id,
            string displayName)
        {
            // 반복되는 ScriptableObject 준비를 한곳에 두어 각 테스트가 검증 의도에 집중하게 한다.
            AbilityDefinition ability =
                ScriptableObject.CreateInstance<AbilityDefinition>();
            ability.Configure(id, displayName, $"{displayName} 테스트 정의");
            return ability;
        }
    }
}
