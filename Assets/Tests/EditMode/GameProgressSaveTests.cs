// GOLDEN STANDARD
// 목적: 능력·체크포인트 저장 데이터의 버전형 JSON 왕복과 안전한 복원을 검증한다.
// 책임: 캡처, 직렬화, 알려진 능력 필터, 체크포인트 좌표와 손상·이전 버전 거부를 확인한다.
// 불변식: 각 테스트는 생성한 GameObject와 ScriptableObject를 종료 전에 모두 정리한다.
// 선택 이유: 파일 시스템과 분리된 Codec 테스트로 저장 계약 오류를 빠르고 결정적으로 찾는다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class GameProgressSaveTests
    {
        [Test]
        public void JsonRoundTrip_RestoresAbilitiesAndCheckpoint()
        {
            // 서로 다른 원본·복원 플레이어를 사용해 런타임 참조가 JSON에 섞이지 않는지 확인한다.
            AbilityDefinition doubleJump =
                CreateAbility("double_jump");
            AbilityDefinition airDash =
                CreateAbility("air_dash");
            var source =
                new GameObject("SaveSource");
            var destination =
                new GameObject("SaveDestination");
            try
            {
                PlayerAbilityState sourceAbilities =
                    source.AddComponent<PlayerAbilityState>();
                PlayerCheckpointState sourceCheckpoint =
                    source.AddComponent<PlayerCheckpointState>();
                Assert.That(
                    sourceAbilities.TryUnlock(doubleJump),
                    Is.True);
                Assert.That(
                    sourceAbilities.TryUnlock(airDash),
                    Is.True);
                Assert.That(
                    sourceCheckpoint.ActivateCheckpoint(
                        "start_hall",
                        new Vector3(2f, 1.25f, 0f)),
                    Is.True);

                string json =
                    GameProgressSaveCodec.ToJson(
                        GameProgressSaveCodec.Capture(
                            sourceAbilities,
                            sourceCheckpoint));
                Assert.That(
                    GameProgressSaveCodec.TryFromJson(
                        json,
                        out GameProgressSaveData loaded),
                    Is.True);
                PlayerAbilityState restoredAbilities =
                    destination
                        .AddComponent<PlayerAbilityState>();
                PlayerCheckpointState restoredCheckpoint =
                    destination
                        .AddComponent<PlayerCheckpointState>();
                Assert.That(
                    GameProgressSaveCodec.Apply(
                        loaded,
                        restoredAbilities,
                        restoredCheckpoint,
                        new[]
                        {
                            doubleJump,
                            airDash
                        }),
                    Is.True);
                Assert.That(
                    restoredAbilities.HasAbility(doubleJump),
                    Is.True);
                Assert.That(
                    restoredAbilities.HasAbility(airDash),
                    Is.True);
                Assert.That(
                    restoredCheckpoint.LastCheckpointId,
                    Is.EqualTo("start_hall"));
                Assert.That(
                    restoredCheckpoint.LastRespawnPosition,
                    Is.EqualTo(
                        new Vector3(2f, 1.25f, 0f)));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(destination);
                Object.DestroyImmediate(doubleJump);
                Object.DestroyImmediate(airDash);
            }
        }

        [Test]
        public void TryFromJson_RejectsUnsupportedVersionAndDamage()
        {
            // 알 수 없는 버전과 잘린 JSON이 현재 진행 상태에 적용 가능한 데이터로 통과하지 않는지 확인한다.
            Assert.That(
                GameProgressSaveCodec.TryFromJson(
                    "{\"version\":999}",
                    out _),
                Is.False);
            Assert.That(
                GameProgressSaveCodec.TryFromJson(
                    "{\"version\":1",
                    out _),
                Is.False);
            Assert.That(
                GameProgressSaveCodec.TryFromJson(
                    "{}",
                    out _),
                Is.False);
        }

        [Test]
        public void Apply_IgnoresUnknownAbilityId()
        {
            // 세이브에 남았지만 현재 빌드에서 제거된 능력 ID는 유령 능력으로 복원하지 않는다.
            AbilityDefinition knownAbility =
                CreateAbility("double_jump");
            var player =
                new GameObject("UnknownAbilityPlayer");
            try
            {
                PlayerAbilityState abilityState =
                    player.AddComponent<PlayerAbilityState>();
                PlayerCheckpointState checkpointState =
                    player.AddComponent<PlayerCheckpointState>();
                var data = new GameProgressSaveData
                {
                    unlockedAbilityIds =
                        new()
                        {
                            "removed_ability",
                            "double_jump"
                        }
                };

                Assert.That(
                    GameProgressSaveCodec.Apply(
                        data,
                        abilityState,
                        checkpointState,
                        new[]
                        {
                            knownAbility
                        }),
                    Is.True);
                Assert.That(
                    abilityState.UnlockedCount,
                    Is.EqualTo(1));
                Assert.That(
                    abilityState.HasAbilityId(
                        "removed_ability"),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(knownAbility);
            }
        }

        [Test]
        public void RestoreAbilities_RelocksGateAfterEmptySave()
        {
            // 더 적은 능력을 가진 저장 데이터를 불러올 때 이미 열린 게이트도 다시 계산되는지 확인한다.
            AbilityDefinition requiredAbility =
                CreateAbility("air_dash");
            var player =
                new GameObject("GateRestorePlayer");
            var gateObject =
                new GameObject("GateRestoreTarget");
            try
            {
                PlayerAbilityState abilityState =
                    player.AddComponent<PlayerAbilityState>();
                BoxCollider collider =
                    gateObject.AddComponent<BoxCollider>();
                AbilityGate gate =
                    gateObject.AddComponent<AbilityGate>();
                gate.Configure(
                    requiredAbility,
                    abilityState,
                    null);

                Assert.That(
                    abilityState.TryUnlock(requiredAbility),
                    Is.True);
                Assert.That(gate.IsLocked, Is.False);
                Assert.That(collider.enabled, Is.False);

                abilityState.RestoreUnlockedAbilities(
                    new[]
                    {
                        requiredAbility
                    },
                    System.Array.Empty<string>());

                Assert.That(gate.IsLocked, Is.True);
                Assert.That(collider.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(gateObject);
                Object.DestroyImmediate(requiredAbility);
            }
        }

        [Test]
        public void Apply_InvalidCheckpointDoesNotPartiallyChangeAbilities()
        {
            // 잘못된 체크포인트가 있는 데이터가 능력만 먼저 바꾸는 부분 복원을 만들지 않는지 확인한다.
            AbilityDefinition existingAbility =
                CreateAbility("double_jump");
            AbilityDefinition requestedAbility =
                CreateAbility("air_dash");
            var player =
                new GameObject("AtomicApplyPlayer");
            try
            {
                PlayerAbilityState abilityState =
                    player.AddComponent<PlayerAbilityState>();
                PlayerCheckpointState checkpointState =
                    player.AddComponent<PlayerCheckpointState>();
                Assert.That(
                    abilityState.TryUnlock(existingAbility),
                    Is.True);
                var invalidData =
                    new GameProgressSaveData
                    {
                        hasCheckpoint = true,
                        checkpointId = string.Empty,
                        unlockedAbilityIds =
                            new()
                            {
                                requestedAbility.Id
                            }
                    };

                Assert.That(
                    GameProgressSaveCodec.Apply(
                        invalidData,
                        abilityState,
                        checkpointState,
                        new[]
                        {
                            existingAbility,
                            requestedAbility
                        }),
                    Is.False);
                Assert.That(
                    abilityState.HasAbility(
                        existingAbility),
                    Is.True);
                Assert.That(
                    abilityState.HasAbility(
                        requestedAbility),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(existingAbility);
                Object.DestroyImmediate(requestedAbility);
            }
        }

        [Test]
        public void WorldRoundTrip_RefreshesGateRewardAndMap()
        {
            // 월드 ID 복원이 데이터 집합뿐 아니라 게이트·보상 효과·지도 표현까지 갱신하는지 확인한다.
            WorldZoneDefinition startZone =
                CreateZone("start_hall");
            WorldZoneDefinition bossZone =
                CreateZone("boss_room");
            AbilityDefinition wallTraversal =
                CreateAbility("wall_traversal");
            var source =
                new GameObject("WorldSaveSource");
            var destination =
                new GameObject("WorldSaveDestination");
            GameObject gateObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
            GameObject activatorObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);
            GameObject rewardObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);
            var mapObject =
                new GameObject("WorldSaveMap");
            try
            {
                PlayerAbilityState sourceAbilities =
                    source.AddComponent<PlayerAbilityState>();
                PlayerCheckpointState sourceCheckpoint =
                    source.AddComponent<PlayerCheckpointState>();
                PlayerWorldState sourceWorld =
                    source.AddComponent<PlayerWorldState>();
                Assert.That(
                    sourceWorld.TryVisit(startZone),
                    Is.True);
                Assert.That(
                    sourceWorld.TryVisit(bossZone),
                    Is.True);
                Assert.That(
                    sourceWorld.TryUnlockShortcut(
                        "shortcut_shaft_return"),
                    Is.True);
                Assert.That(
                    sourceWorld.TryCollectReward(
                        "reward_shaft_health_fragment"),
                    Is.True);

                string json =
                    GameProgressSaveCodec.ToJson(
                        GameProgressSaveCodec.Capture(
                            sourceAbilities,
                            sourceCheckpoint,
                            sourceWorld));
                Assert.That(
                    GameProgressSaveCodec.TryFromJson(
                        json,
                        out GameProgressSaveData loaded),
                    Is.True);
                loaded.visitedZoneIds.Add(
                    "removed_zone");

                Health destinationHealth =
                    destination.AddComponent<Health>();
                destinationHealth.Configure(5);
                PlayerAbilityState destinationAbilities =
                    destination.AddComponent<PlayerAbilityState>();
                PlayerCheckpointState destinationCheckpoint =
                    destination.AddComponent<PlayerCheckpointState>();
                PlayerWorldState destinationWorld =
                    destination.AddComponent<PlayerWorldState>();
                WorldShortcutGate gate =
                    gateObject.AddComponent<WorldShortcutGate>();
                gate.Configure(
                    "shortcut_shaft_return",
                    destinationWorld,
                    gateObject.GetComponent<Renderer>());
                ShortcutUnlockVolume activator =
                    activatorObject
                        .AddComponent<ShortcutUnlockVolume>();
                activator.Configure(
                    gate,
                    activatorObject.GetComponent<Renderer>());
                BacktrackRewardPickup reward =
                    rewardObject
                        .AddComponent<BacktrackRewardPickup>();
                reward.Configure(
                    "reward_shaft_health_fragment",
                    1,
                    wallTraversal,
                    destinationWorld,
                    destinationAbilities,
                    destinationHealth,
                    rewardObject.GetComponent<Renderer>());

                WorldMapPresenter map =
                    mapObject.AddComponent<WorldMapPresenter>();
                map.Configure(
                    destinationWorld,
                    startZone,
                    new[]
                    {
                        new WorldMapNodeView(
                            startZone,
                            "START",
                            null,
                            null),
                        new WorldMapNodeView(
                            bossZone,
                            "BOSS",
                            null,
                            null)
                    },
                    System.Array.Empty<WorldMapConnectionView>());

                Assert.That(
                    GameProgressSaveCodec.Apply(
                        loaded,
                        destinationAbilities,
                        destinationCheckpoint,
                        new[]
                        {
                            wallTraversal
                        },
                        destinationWorld,
                        new[]
                        {
                            startZone,
                            bossZone
                        }),
                    Is.True);
                Assert.That(
                    destinationWorld.HasVisited(
                        bossZone),
                    Is.True);
                Assert.That(
                    destinationWorld.HasVisitedId(
                        "removed_zone"),
                    Is.False);
                Assert.That(gate.IsLocked, Is.False);
                Assert.That(activator.IsActivated, Is.True);
                Assert.That(reward.IsCollected, Is.True);
                Assert.That(
                    destinationHealth.MaxHealth,
                    Is.EqualTo(6));
                Assert.That(
                    map.GetNodeState("boss_room"),
                    Is.EqualTo(
                        WorldMapVisualState.Visited));

                var emptyWorldData =
                    new GameProgressSaveData();
                Assert.That(
                    GameProgressSaveCodec.Apply(
                        emptyWorldData,
                        destinationAbilities,
                        destinationCheckpoint,
                        new[]
                        {
                            wallTraversal
                        },
                        destinationWorld,
                        new[]
                        {
                            startZone,
                            bossZone
                        }),
                    Is.True);
                Assert.That(gate.IsLocked, Is.True);
                Assert.That(activator.IsActivated, Is.False);
                Assert.That(reward.IsCollected, Is.False);
                Assert.That(
                    destinationHealth.MaxHealth,
                    Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(mapObject);
                Object.DestroyImmediate(rewardObject);
                Object.DestroyImmediate(activatorObject);
                Object.DestroyImmediate(gateObject);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(destination);
                Object.DestroyImmediate(wallTraversal);
                Object.DestroyImmediate(startZone);
                Object.DestroyImmediate(bossZone);
            }
        }

        private static AbilityDefinition CreateAbility(
            string id)
        {
            // 테스트마다 독립 정의를 만들어 AssetDatabase나 프로젝트 에셋 상태에 의존하지 않는다.
            AbilityDefinition ability =
                ScriptableObject
                    .CreateInstance<AbilityDefinition>();
            ability.Configure(id, id, string.Empty);
            return ability;
        }

        private static WorldZoneDefinition CreateZone(
            string id)
        {
            // 테스트용 구역 정의는 프로젝트 에셋과 분리해 저장 카탈로그 필터를 독립 검증한다.
            WorldZoneDefinition zone =
                ScriptableObject
                    .CreateInstance<WorldZoneDefinition>();
            zone.Configure(id, id, string.Empty);
            return zone;
        }
    }
}
