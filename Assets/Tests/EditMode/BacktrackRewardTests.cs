// GOLDEN STANDARD
// 목적: 씬 배치와 무관하게 능력 기반 백트래킹 보상의 영구 ID와 체력 효과 계약을 검증한다.
// 책임: 중복 획득 방지, 요구 능력 검증, 최대 체력 증가와 픽업 표현 소비를 확인한다.
// 불변식: 각 테스트는 생성한 Unity 오브젝트와 ScriptableObject를 종료 전에 모두 정리한다.
// 선택 이유: 레벨 도달 가능성과 보상 도메인 규칙을 분리해 실패 원인을 빠르게 찾을 수 있다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class BacktrackRewardTests
    {
        [Test]
        public void PlayerWorldState_CollectsRewardOnlyOnce()
        {
            // 같은 영구 ID의 중복 획득이 저장 수와 이벤트를 두 번 늘리지 않는지 검증한다.
            var player =
                new GameObject("RewardStateTestPlayer");
            try
            {
                PlayerWorldState state =
                    player.AddComponent<PlayerWorldState>();
                int collectedEvents = 0;
                state.RewardCollected += _ =>
                    collectedEvents++;

                Assert.That(
                    state.TryCollectReward("shaft_health"),
                    Is.True);
                Assert.That(
                    state.TryCollectReward("shaft_health"),
                    Is.False);
                Assert.That(
                    state.TryCollectReward("  "),
                    Is.False);
                Assert.That(
                    state.IsRewardCollected("shaft_health"),
                    Is.True);
                Assert.That(
                    state.CollectedRewardCount,
                    Is.EqualTo(1));
                Assert.That(collectedEvents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void Pickup_RequiresAbilityAndIncreasesMaximumHealthOnce()
        {
            // 능력 획득 전 거부와 획득 후 체력 효과·표현 소비를 한 흐름에서 검증한다.
            AbilityDefinition wallTraversal =
                CreateAbility(
                    "wall_traversal",
                    "벽 잡기");
            var player =
                new GameObject("RewardPickupTestPlayer");
            GameObject pickupObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);
            try
            {
                Health health = player.AddComponent<Health>();
                health.Configure(5);
                PlayerAbilityState abilityState =
                    player.AddComponent<PlayerAbilityState>();
                PlayerWorldState worldState =
                    player.AddComponent<PlayerWorldState>();
                BacktrackRewardPickup pickup =
                    pickupObject
                        .AddComponent<BacktrackRewardPickup>();
                Renderer renderer =
                    pickupObject.GetComponent<Renderer>();
                pickup.Configure(
                    "shaft_health",
                    1,
                    wallTraversal,
                    worldState,
                    abilityState,
                    health,
                    renderer);

                Assert.That(pickup.Collect(), Is.False);
                Assert.That(health.MaxHealth, Is.EqualTo(5));
                Assert.That(
                    worldState.CollectedRewardCount,
                    Is.Zero);

                Assert.That(
                    abilityState.TryUnlock(wallTraversal),
                    Is.True);
                Assert.That(health.TakeDamage(2), Is.True);
                Assert.That(pickup.Collect(), Is.True);

                Assert.That(health.MaxHealth, Is.EqualTo(6));
                Assert.That(
                    health.CurrentHealth,
                    Is.EqualTo(4));
                Assert.That(pickup.IsCollected, Is.True);
                Assert.That(
                    worldState.IsRewardCollected(
                        "shaft_health"),
                    Is.True);
                Assert.That(
                    pickupObject.GetComponent<Collider>().enabled,
                    Is.False);
                Assert.That(renderer.enabled, Is.False);
                Assert.That(pickup.Collect(), Is.False);
                Assert.That(health.MaxHealth, Is.EqualTo(6));
            }
            finally
            {
                Object.DestroyImmediate(pickupObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(wallTraversal);
            }
        }

        private static AbilityDefinition CreateAbility(
            string id,
            string displayName)
        {
            // 반복되는 능력 정의 준비를 한곳에 두어 테스트가 보상 규칙에 집중하게 한다.
            AbilityDefinition ability =
                ScriptableObject
                    .CreateInstance<AbilityDefinition>();
            ability.Configure(
                id,
                displayName,
                $"{displayName} 보상 테스트 정의");
            return ability;
        }
    }
}
