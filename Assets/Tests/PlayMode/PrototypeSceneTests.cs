// GOLDEN STANDARD
// 목적: 통합 Main 씬을 포트폴리오 시연용 플레이 가능 씬으로 스모크 테스트한다.
// 책임: 필수 컴포넌트·에셋·애니메이터 파라미터·시연 대상을 확인한다.
// 불변식: 세밀한 이동 타이밍이 아니라 씬 연결 상태를 검사한다.
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameSkill.Tests
{
    public sealed class PrototypeSceneTests
    {
        [UnityTest]
        public IEnumerator MainScene_HasPlayablePrototype()
        {
            // 포트폴리오 사용자가 실행할 씬을 로드하고 외부 계약을 검증한다.
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameObject player = GameObject.Find("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(
                player.layer,
                Is.EqualTo(
                    CharacterBodyCollisionPolicy.PlayerBodyLayer));
            Assert.That(
                CharacterBodyCollisionPolicy.IsApplied(),
                Is.True);
            Assert.That(
                Physics.GetIgnoreLayerCollision(
                    CharacterBodyCollisionPolicy.PlayerBodyLayer,
                    0),
                Is.False);
            CharacterController characterController =
                player.GetComponent<CharacterController>();
            Assert.That(characterController, Is.Not.Null);
            Assert.That(characterController.slopeLimit, Is.EqualTo(45f).Within(0.001f));
            Assert.That(characterController.stepOffset, Is.EqualTo(0.3f).Within(0.001f));
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            Assert.That(playerInput, Is.Not.Null);
            Assert.That(playerInput.actions.FindAction("Dash"), Is.Not.Null);
            SideScrollerMotor motor = player.GetComponent<SideScrollerMotor>();
            Assert.That(motor, Is.Not.Null);
            Assert.That(motor.IsDashing, Is.False);
            Assert.That(motor.IsInvulnerable, Is.False);
            Assert.That(motor.CanAirDash, Is.False);
            Assert.That(motor.IsDoubleJumpUnlocked, Is.False);
            Assert.That(motor.IsAirDashUnlocked, Is.False);
            Assert.That(motor.IsWallTraversalUnlocked, Is.False);
            Assert.That(motor.IsWallClinging, Is.False);
            Assert.That(motor.IsWallSliding, Is.False);
            Assert.That(motor.AirJumpsRemaining, Is.EqualTo(1));
            Assert.That(
                motor.DashDuration,
                Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(
                motor.DashInvulnerabilityDuration,
                Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(
                motor.DashInvulnerabilityDuration,
                Is.GreaterThan(motor.DashDuration));
            Assert.That(
                motor.WallJumpHorizontalSpeed,
                Is.EqualTo(3.6f).Within(0.001f));
            Assert.That(
                motor.WallJumpControlLockTime,
                Is.EqualTo(0.04f).Within(0.001f));
            PlayerAbilityState abilityState =
                player.GetComponent<PlayerAbilityState>();
            Assert.That(abilityState, Is.Not.Null);
            Assert.That(abilityState.UnlockedCount, Is.Zero);
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            Assert.That(combat, Is.Not.Null);
            SideScrollerTargeting targeting =
                player.GetComponent<SideScrollerTargeting>();
            Assert.That(targeting, Is.Not.Null);
            Health playerHealth = player.GetComponent<Health>();
            Assert.That(playerHealth, Is.Not.Null);
            PlayerCheckpointState checkpointState =
                player.GetComponent<PlayerCheckpointState>();
            Assert.That(checkpointState, Is.Not.Null);
            GameProgressSaveController saveController =
                player.GetComponent<GameProgressSaveController>();
            Assert.That(saveController, Is.Not.Null);
            Assert.That(
                saveController.AbilityCatalogCount,
                Is.EqualTo(3));
            Assert.That(
                saveController.WorldZoneCatalogCount,
                Is.EqualTo(4));
            Assert.That(
                saveController.CaptureJson(),
                Does.Contain("\"version\": 1"));
            Assert.That(
                saveController.CaptureJson(),
                Does.Contain("\"visitedZoneIds\""));
            PlayerWorldState worldState =
                player.GetComponent<PlayerWorldState>();
            Assert.That(worldState, Is.Not.Null);
            GameObject worldMapHud =
                GameObject.Find("WorldMapHUD");
            Assert.That(worldMapHud, Is.Not.Null);
            Assert.That(
                worldMapHud.GetComponent<Canvas>(),
                Is.Not.Null);
            WorldMapPresenter mapPresenter =
                worldMapHud.GetComponent<WorldMapPresenter>();
            Assert.That(mapPresenter, Is.Not.Null);
            Assert.That(mapPresenter.NodeCount, Is.EqualTo(4));
            Assert.That(
                mapPresenter.ConnectionCount,
                Is.EqualTo(3));
            Assert.That(
                mapPresenter.GetNodeState("start_hall"),
                Is.EqualTo(WorldMapVisualState.Current));
            Assert.That(
                mapPresenter.GetNodeState("traversal_lab"),
                Is.EqualTo(WorldMapVisualState.Hidden));
            Assert.That(
                mapPresenter.GetNodeState("boss_room"),
                Is.EqualTo(WorldMapVisualState.Hidden));
            PlayerRespawnController respawnController =
                player.GetComponent<PlayerRespawnController>();
            Assert.That(respawnController, Is.Not.Null);
            Assert.That(player.GetComponent<PlayerAnimator>(), Is.Not.Null);
            Animator animator = player.GetComponentInChildren<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.avatar, Is.Not.Null);
            Assert.That(animator.avatar.isValid, Is.True);
            Assert.That(animator.avatar.isHuman, Is.True);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(
                System.Array.Exists(
                    animator.parameters,
                    parameter => parameter.name == "Dodging"
                        && parameter.type == AnimatorControllerParameterType.Bool),
                Is.True);
            Assert.That(
                System.Array.Exists(
                    animator.parameters,
                    parameter => parameter.name == "Attacking"
                        && parameter.type == AnimatorControllerParameterType.Bool),
                Is.True);
            Assert.That(
                System.Array.Exists(
                    animator.parameters,
                    parameter => parameter.name == "ComboStep"
                        && parameter.type == AnimatorControllerParameterType.Int),
                Is.True);

            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            SideScrollerCamera sideScrollerCamera =
                camera.GetComponent<SideScrollerCamera>();
            Assert.That(sideScrollerCamera, Is.Not.Null);
            Assert.That(camera.orthographic, Is.False);
            Assert.That(
                camera.fieldOfView,
                Is.EqualTo(35f).Within(0.001f));
            Assert.That(
                sideScrollerCamera.CameraDistance,
                Is.EqualTo(16.4923f).Within(0.001f));
            Assert.That(
                sideScrollerCamera
                    .ReferenceVerticalHalfExtent,
                Is.EqualTo(5.2f).Within(0.001f));
            Assert.That(
                camera.transform.position.z,
                Is.EqualTo(-16.4923f).Within(0.001f));
            Assert.That(
                sideScrollerCamera.ActiveZoneId,
                Is.EqualTo("start_hall"));
            Assert.That(
                sideScrollerCamera.ConstrainPosition(
                    new Vector3(100f, -100f, -9f)),
                Is.EqualTo(
                    new Vector3(2f, 2.4f, -9f)));
            Assert.That(
                sideScrollerCamera.ConstrainPosition(
                    new Vector3(0f, 6f, -9f)),
                Is.EqualTo(
                    new Vector3(0f, 6f, -9f)));

            Assert.That(GameObject.Find("SideScrollerGraybox"), Is.Not.Null);
            GameObject gateObject = GameObject.Find("Wall_Gate");
            Assert.That(gateObject, Is.Not.Null);
            AbilityGate gate = gateObject.GetComponent<AbilityGate>();
            Assert.That(gate, Is.Not.Null);
            Assert.That(gate.IsLocked, Is.True);
            Assert.That(gateObject.GetComponent<Collider>().enabled, Is.True);
            GameObject airDashGateObject =
                GameObject.Find("AirDash_Gate");
            Assert.That(airDashGateObject, Is.Not.Null);
            AbilityGate airDashGate =
                airDashGateObject.GetComponent<AbilityGate>();
            Assert.That(airDashGate, Is.Not.Null);
            Assert.That(airDashGate.IsLocked, Is.True);
            Assert.That(GameObject.Find("Slope_Test"), Is.Not.Null);
            Assert.That(GameObject.Find("WallTraversal_Left"), Is.Not.Null);
            Assert.That(
                GameObject.Find("WallTraversal_RightUpper"),
                Is.Not.Null);
            GameObject backtrackRewardObject =
                GameObject.Find("Backtrack_Reward");
            Assert.That(backtrackRewardObject, Is.Not.Null);
            Assert.That(
                backtrackRewardObject.transform.position,
                Is.EqualTo(
                    new Vector3(-10.25f, 7.15f, 0f)));
            BacktrackRewardPickup backtrackReward =
                backtrackRewardObject
                    .GetComponent<BacktrackRewardPickup>();
            Assert.That(backtrackReward, Is.Not.Null);
            Assert.That(
                backtrackReward.RewardId,
                Is.EqualTo(
                    "reward_shaft_health_fragment"));
            Assert.That(
                backtrackReward.MaximumHealthBonus,
                Is.EqualTo(1));
            Assert.That(
                backtrackReward.RequiredAbility.Id,
                Is.EqualTo("wall_traversal"));
            Assert.That(
                backtrackRewardObject
                    .GetComponent<Collider>().isTrigger,
                Is.True);
            Assert.That(backtrackReward.Collect(), Is.False);
            Assert.That(playerHealth.MaxHealth, Is.EqualTo(5));

            // 네 Graybox 구역의 맞닿는 Trigger와 영구 ID 기반 방문 흐름을 실제 씬에서 검증한다.
            WorldZoneVolume startHall =
                GameObject.Find("Zone_StartHall")
                    .GetComponent<WorldZoneVolume>();
            WorldZoneVolume traversalLab =
                GameObject.Find("Zone_TraversalLab")
                    .GetComponent<WorldZoneVolume>();
            WorldZoneVolume backtrackShaft =
                GameObject.Find("Zone_BacktrackShaft")
                    .GetComponent<WorldZoneVolume>();
            WorldZoneVolume bossRoom =
                GameObject.Find("Zone_BossRoom")
                    .GetComponent<WorldZoneVolume>();
            Assert.That(startHall, Is.Not.Null);
            Assert.That(traversalLab, Is.Not.Null);
            Assert.That(backtrackShaft, Is.Not.Null);
            Assert.That(bossRoom, Is.Not.Null);
            Assert.That(startHall.Zone.Id, Is.EqualTo("start_hall"));
            Assert.That(
                traversalLab.Zone.Id,
                Is.EqualTo("traversal_lab"));
            Assert.That(
                backtrackShaft.Zone.Id,
                Is.EqualTo("backtrack_shaft"));
            Assert.That(
                bossRoom.Zone.Id,
                Is.EqualTo("boss_room"));
            Assert.That(
                startHall.GetComponent<Collider>().isTrigger,
                Is.True);
            worldState.ConfigureInitialZones(null);
            Assert.That(worldState.VisitedCount, Is.Zero);
            Assert.That(startHall.Enter(worldState), Is.True);
            Assert.That(startHall.Enter(worldState), Is.False);
            Assert.That(traversalLab.Enter(worldState), Is.True);
            Assert.That(bossRoom.Enter(worldState), Is.True);
            Assert.That(
                sideScrollerCamera.ActiveZoneId,
                Is.EqualTo("boss_room"));
            Assert.That(
                sideScrollerCamera.ConstrainPosition(
                    new Vector3(100f, -100f, -9f)),
                Is.EqualTo(
                    new Vector3(32f, 5.4f, -9f)));
            Assert.That(backtrackShaft.Enter(worldState), Is.True);
            Assert.That(worldState.VisitedCount, Is.EqualTo(4));
            Assert.That(
                sideScrollerCamera.ActiveZoneId,
                Is.EqualTo("backtrack_shaft"));
            Assert.That(
                sideScrollerCamera.ConstrainPosition(
                    new Vector3(100f, 100f, -9f)),
                Is.EqualTo(
                    new Vector3(-10.75f, 9f, -9f)));
            Assert.That(
                mapPresenter.GetNodeState("backtrack_shaft"),
                Is.EqualTo(WorldMapVisualState.Current));
            Assert.That(
                mapPresenter.GetNodeState("start_hall"),
                Is.EqualTo(WorldMapVisualState.Visited));
            Assert.That(
                mapPresenter.GetNodeState("traversal_lab"),
                Is.EqualTo(WorldMapVisualState.Visited));
            Assert.That(
                mapPresenter.GetNodeState("boss_room"),
                Is.EqualTo(WorldMapVisualState.Visited));
            Assert.That(
                GameObject.Find("Shortcut_ShaftLanding"),
                Is.Not.Null);
            Assert.That(
                GameObject.Find("Shortcut_ReturnBridge"),
                Is.Not.Null);
            GameObject shortcutGateObject =
                GameObject.Find("ShortcutGate_ShaftReturn");
            Assert.That(shortcutGateObject, Is.Not.Null);
            WorldShortcutGate shortcutGate =
                shortcutGateObject.GetComponent<WorldShortcutGate>();
            Assert.That(shortcutGate, Is.Not.Null);
            Assert.That(shortcutGate.IsLocked, Is.True);
            ShortcutUnlockVolume shortcutActivator =
                GameObject.Find("ShortcutActivator_ShaftTop")
                    .GetComponent<ShortcutUnlockVolume>();
            Assert.That(shortcutActivator, Is.Not.Null);
            GameObject dummy = GameObject.Find("TrainingDummy");
            Assert.That(dummy, Is.Not.Null);
            Assert.That(
                dummy.layer,
                Is.EqualTo(
                    CharacterBodyCollisionPolicy.EnemyBodyLayer));
            Health dummyHealth = dummy.GetComponent<Health>();
            Assert.That(dummyHealth, Is.Not.Null);
            GameObject meleeEnemyObject =
                GameObject.Find("MeleeEnemy_Grunt");
            Assert.That(meleeEnemyObject, Is.Not.Null);
            Assert.That(
                meleeEnemyObject.layer,
                Is.EqualTo(
                    CharacterBodyCollisionPolicy.EnemyBodyLayer));
            Assert.That(
                Physics.GetIgnoreLayerCollision(
                    CharacterBodyCollisionPolicy.EnemyBodyLayer,
                    CharacterBodyCollisionPolicy.EnemyBodyLayer),
                Is.True);
            MeleeEnemyController meleeEnemy =
                meleeEnemyObject
                    .GetComponent<MeleeEnemyController>();
            Assert.That(meleeEnemy, Is.Not.Null);
            Assert.That(
                meleeEnemyObject
                    .GetComponent<CharacterController>(),
                Is.Not.Null);
            Health meleeEnemyHealth =
                meleeEnemyObject.GetComponent<Health>();
            Assert.That(meleeEnemyHealth, Is.Not.Null);
            Assert.That(
                meleeEnemyHealth.MaxHealth,
                Is.EqualTo(3));
            Assert.That(
                meleeEnemy.CurrentState,
                Is.EqualTo(EnemyState.Idle));
            Assert.That(
                meleeEnemy.AttackWindupDuration,
                Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(
                meleeEnemy.AttackRecoveryDuration,
                Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(
                meleeEnemyObject.transform.localScale,
                Is.EqualTo(Vector3.one));
            Transform meleeEnemyVisual =
                meleeEnemyObject.transform.Find(
                    "MeleeEnemy_Visual");
            Assert.That(meleeEnemyVisual, Is.Not.Null);
            Assert.That(
                meleeEnemyVisual.localPosition.y,
                Is.EqualTo(0.9f).Within(0.001f));
            Renderer meleeEnemyRenderer =
                meleeEnemyVisual.GetComponent<Renderer>();
            Assert.That(meleeEnemyRenderer, Is.Not.Null);
            Transform attackIndicator =
                meleeEnemyObject.transform.Find(
                    "MeleeEnemy_AttackIndicator");
            Assert.That(attackIndicator, Is.Not.Null);
            Assert.That(
                attackIndicator
                    .GetComponent<Renderer>().enabled,
                Is.False);
            Assert.That(
                attackIndicator.localScale.x,
                Is.EqualTo(0.65f).Within(0.001f));
            Assert.That(
                meleeEnemyObject.transform.position.z,
                Is.Zero.Within(0.0001f));
            Vector3 meleeEnemySpawnPosition =
                meleeEnemyObject.transform.position;
            GameObject rangedEnemyObject =
                GameObject.Find("RangedEnemy_Sentry");
            Assert.That(rangedEnemyObject, Is.Not.Null);
            Assert.That(
                rangedEnemyObject.layer,
                Is.EqualTo(
                    CharacterBodyCollisionPolicy.EnemyBodyLayer));
            RangedEnemyController rangedEnemy =
                rangedEnemyObject
                    .GetComponent<RangedEnemyController>();
            Assert.That(rangedEnemy, Is.Not.Null);
            Health rangedEnemyHealth =
                rangedEnemyObject.GetComponent<Health>();
            Assert.That(rangedEnemyHealth, Is.Not.Null);
            Assert.That(
                rangedEnemyHealth.MaxHealth,
                Is.EqualTo(3));
            Assert.That(
                rangedEnemy.CurrentState,
                Is.EqualTo(EnemyState.Idle));
            Assert.That(
                rangedEnemy.AttackWindupDuration,
                Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(
                rangedEnemy.AttackRecoveryDuration,
                Is.EqualTo(1.6f).Within(0.001f));
            CapsuleCollider rangedBody =
                rangedEnemyObject
                    .GetComponent<CapsuleCollider>();
            Assert.That(rangedBody, Is.Not.Null);
            Assert.That(rangedBody.isTrigger, Is.False);
            Transform rangedVisual =
                rangedEnemyObject.transform.Find(
                    "RangedEnemy_Visual");
            Assert.That(rangedVisual, Is.Not.Null);
            Assert.That(
                rangedVisual.localPosition.y,
                Is.EqualTo(0.9f).Within(0.001f));
            Renderer rangedRenderer =
                rangedVisual.GetComponent<Renderer>();
            Assert.That(rangedRenderer, Is.Not.Null);
            Renderer rangedMuzzleRenderer =
                rangedEnemyObject.transform
                    .Find("RangedEnemy_Muzzle")
                    .GetComponent<Renderer>();
            Assert.That(
                rangedMuzzleRenderer.enabled,
                Is.False);
            Vector3 rangedEnemySpawnPosition =
                rangedEnemyObject.transform.position;
            GameObject chargeEnemyObject =
                GameObject.Find("ChargeEnemy_Rusher");
            Assert.That(chargeEnemyObject, Is.Not.Null);
            Assert.That(
                chargeEnemyObject.layer,
                Is.EqualTo(
                    CharacterBodyCollisionPolicy.EnemyBodyLayer));
            ChargeEnemyController chargeEnemy =
                chargeEnemyObject
                    .GetComponent<ChargeEnemyController>();
            Assert.That(chargeEnemy, Is.Not.Null);
            Health chargeEnemyHealth =
                chargeEnemyObject.GetComponent<Health>();
            Assert.That(chargeEnemyHealth, Is.Not.Null);
            Assert.That(
                chargeEnemyHealth.MaxHealth,
                Is.EqualTo(4));
            CharacterController chargeBody =
                chargeEnemyObject
                    .GetComponent<CharacterController>();
            Assert.That(chargeBody, Is.Not.Null);
            Assert.That(
                chargeEnemy.CurrentState,
                Is.EqualTo(EnemyState.Idle));
            Transform chargeVisual =
                chargeEnemyObject.transform.Find(
                    "ChargeEnemy_Visual");
            Assert.That(chargeVisual, Is.Not.Null);
            Renderer chargeRenderer =
                chargeVisual.GetComponent<Renderer>();
            Assert.That(chargeRenderer, Is.Not.Null);
            Renderer chargeIndicatorRenderer =
                chargeEnemyObject.transform
                    .Find(
                        "ChargeEnemy_DirectionIndicator")
                    .GetComponent<Renderer>();
            Assert.That(
                chargeIndicatorRenderer.enabled,
                Is.False);
            Vector3 chargeEnemySpawnPosition =
                chargeEnemyObject.transform.position;
            GameObject bossObject =
                GameObject.Find("Boss_AbilityWarden");
            Assert.That(bossObject, Is.Not.Null);
            Assert.That(
                bossObject.layer,
                Is.EqualTo(
                    CharacterBodyCollisionPolicy.EnemyBodyLayer));
            AbilityTrialBossController boss =
                bossObject
                    .GetComponent<AbilityTrialBossController>();
            Assert.That(boss, Is.Not.Null);
            Health bossHealth =
                bossObject.GetComponent<Health>();
            Assert.That(bossHealth, Is.Not.Null);
            Assert.That(
                bossHealth.MaxHealth,
                Is.EqualTo(12));
            CapsuleCollider bossBody =
                bossObject.GetComponent<CapsuleCollider>();
            Assert.That(bossBody, Is.Not.Null);
            Assert.That(
                boss.CurrentState,
                Is.EqualTo(EnemyState.Idle));
            Assert.That(
                boss.IsAbilityGateSatisfied,
                Is.False);
            Transform bossVisual =
                bossObject.transform.Find(
                    "Boss_Visual");
            Assert.That(bossVisual, Is.Not.Null);
            Renderer bossRenderer =
                bossVisual.GetComponent<Renderer>();
            Assert.That(bossRenderer, Is.Not.Null);
            Renderer bossWarningRenderer =
                bossObject.transform
                    .Find("Boss_PatternWarning")
                    .GetComponent<Renderer>();
            Assert.That(
                bossWarningRenderer.enabled,
                Is.False);
            Vector3 bossSpawnPosition =
                bossObject.transform.position;
            Assert.That(
                GameObject.Find("BossArena_Floor"),
                Is.Not.Null);
            Assert.That(
                GameObject.Find("BossArena_WallPillar"),
                Is.Not.Null);
            Assert.That(
                GameObject.Find("BossArena_RightWall"),
                Is.Not.Null);

            // 실제 Main 참조로 선딜·투사체 생성·초기화까지 원거리 공격 한 사이클을 검증한다.
            Vector3 initialPlayerPosition =
                player.transform.position;
            motor.Teleport(
                rangedEnemySpawnPosition
                + new Vector3(-2f, 0f, 0f));
            rangedEnemy.Tick(0.01f);
            Assert.That(
                rangedEnemy.CurrentState,
                Is.EqualTo(EnemyState.AttackWindup));
            Assert.That(
                rangedMuzzleRenderer.enabled,
                Is.True);
            rangedEnemy.Tick(0.81f);
            Assert.That(
                rangedEnemy.CurrentState,
                Is.EqualTo(EnemyState.AttackRecovery));
            Assert.That(
                rangedEnemy.FiredProjectileCount,
                Is.EqualTo(1));
            Assert.That(
                rangedEnemy.ActiveProjectileCount,
                Is.EqualTo(1));
            rangedEnemy.ResetToSpawn();
            Assert.That(
                rangedEnemy.ActiveProjectileCount,
                Is.Zero);

            // 실제 Main 배치에서 방향 예고가 한 번의 고정 방향 돌진으로 전환되는지 확인한다.
            motor.Teleport(
                chargeEnemySpawnPosition
                + new Vector3(-2f, 0f, 0f));
            chargeEnemy.Tick(0.01f);
            Assert.That(
                chargeEnemy.CurrentState,
                Is.EqualTo(EnemyState.AttackWindup));
            Assert.That(
                chargeIndicatorRenderer.enabled,
                Is.True);
            chargeEnemy.Tick(0.56f);
            Assert.That(
                chargeEnemy.CurrentState,
                Is.EqualTo(EnemyState.Charge));
            Assert.That(
                chargeEnemy.ChargeDirection,
                Is.EqualTo(-1));
            Assert.That(
                chargeEnemy.StartedChargeCount,
                Is.EqualTo(1));
            chargeEnemy.ResetToSpawn();
            Assert.That(
                chargeEnemy.CurrentState,
                Is.EqualTo(EnemyState.Idle));
            Assert.That(
                chargeIndicatorRenderer.enabled,
                Is.False);
            motor.Teleport(initialPlayerPosition);
            Assert.That(player.transform.position.z, Is.EqualTo(0f).Within(0.001f));

            // 실제 픽업을 순서대로 소비해 보유 상태·이동 잠금·백트래킹 게이트를 함께 검증한다.
            GameObject doubleJumpPickupObject =
                GameObject.Find("AbilityPickup_DoubleJump");
            Assert.That(doubleJumpPickupObject, Is.Not.Null);
            AbilityPickup doubleJumpPickup =
                doubleJumpPickupObject.GetComponent<AbilityPickup>();
            Assert.That(doubleJumpPickup, Is.Not.Null);
            Assert.That(doubleJumpPickup.Collect(abilityState), Is.True);
            Assert.That(motor.IsDoubleJumpUnlocked, Is.True);
            Assert.That(gate.IsLocked, Is.False);
            Assert.That(gateObject.GetComponent<Collider>().enabled, Is.False);

            GameObject airDashPickupObject =
                GameObject.Find("AbilityPickup_AirDash");
            Assert.That(airDashPickupObject, Is.Not.Null);
            AbilityPickup airDashPickup =
                airDashPickupObject.GetComponent<AbilityPickup>();
            Assert.That(airDashPickup, Is.Not.Null);
            Assert.That(airDashPickup.Collect(abilityState), Is.True);
            Assert.That(motor.IsAirDashUnlocked, Is.True);
            Assert.That(motor.CanAirDash, Is.True);
            Assert.That(airDashGate.IsLocked, Is.False);
            Assert.That(
                airDashGateObject.GetComponent<Collider>().enabled,
                Is.False);

            GameObject wallPickupObject =
                GameObject.Find("AbilityPickup_WallTraversal");
            Assert.That(wallPickupObject, Is.Not.Null);
            AbilityPickup wallPickup =
                wallPickupObject.GetComponent<AbilityPickup>();
            Assert.That(wallPickup, Is.Not.Null);
            Assert.That(wallPickup.Collect(abilityState), Is.True);
            Assert.That(motor.IsWallTraversalUnlocked, Is.True);
            Assert.That(abilityState.UnlockedCount, Is.EqualTo(3));

            // 세 능력 해금 뒤에만 실제 보스가 첫 패턴을 예고하고 지상 파동을 생성하는지 확인한다.
            motor.Teleport(
                bossSpawnPosition
                + new Vector3(-2f, 0f, 0f));
            boss.Tick(0.01f);
            Assert.That(
                boss.IsAbilityGateSatisfied,
                Is.True);
            Assert.That(
                boss.CurrentState,
                Is.EqualTo(EnemyState.AttackWindup));
            Assert.That(
                bossWarningRenderer.enabled,
                Is.True);
            Assert.That(
                boss.CurrentPattern,
                Is.EqualTo(BossPattern.GroundWave));
            boss.Tick(0.91f);
            Assert.That(
                boss.CurrentState,
                Is.EqualTo(EnemyState.AttackRecovery));
            Assert.That(
                boss.PatternExecutionCount,
                Is.EqualTo(1));
            Assert.That(
                boss.ActiveProjectileCount,
                Is.EqualTo(1));
            boss.ResetToSpawn();
            Assert.That(
                boss.ActiveProjectileCount,
                Is.Zero);
            Assert.That(
                bossWarningRenderer.enabled,
                Is.False);
            motor.Teleport(initialPlayerPosition);

            // 벽 잡기로 되돌아온 샤프트 정상 보상이 최대 체력과 영구 획득 ID를 한 번만 갱신하는지 확인한다.
            Assert.That(backtrackReward.IsRequirementMet, Is.True);
            Assert.That(backtrackReward.Collect(), Is.True);
            Assert.That(playerHealth.MaxHealth, Is.EqualTo(6));
            Assert.That(
                playerHealth.CurrentHealth,
                Is.EqualTo(6));
            Assert.That(
                worldState.IsRewardCollected(
                    "reward_shaft_health_fragment"),
                Is.True);
            Assert.That(
                worldState.CollectedRewardCount,
                Is.EqualTo(1));
            Assert.That(
                backtrackRewardObject
                    .GetComponent<Collider>().enabled,
                Is.False);
            Assert.That(backtrackReward.Collect(), Is.False);

            // 벽 샤프트 정상의 실제 활성 장치로 귀환 다리를 열고 영구 ID 상태를 확인한다.
            Assert.That(
                shortcutActivator.Activate(worldState),
                Is.True);
            Assert.That(shortcutGate.IsLocked, Is.False);
            Assert.That(
                worldState.IsShortcutUnlocked(
                    "shortcut_shaft_return"),
                Is.True);
            Assert.That(
                shortcutGateObject.GetComponent<Collider>().enabled,
                Is.False);

            // 실제 Main 씬 체크포인트를 활성화해 플레이어 체력과 재시작 위치가 함께 갱신되는지 확인한다.
            GameObject checkpointObject = GameObject.Find("Checkpoint_Start");
            Assert.That(checkpointObject, Is.Not.Null);
            Checkpoint checkpoint = checkpointObject.GetComponent<Checkpoint>();
            Assert.That(checkpoint, Is.Not.Null);
            Assert.That(playerHealth.TakeDamage(2), Is.True);
            Assert.That(checkpoint.Activate(checkpointState), Is.True);
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(playerHealth.MaxHealth));
            Assert.That(checkpointState.LastCheckpointId, Is.EqualTo("start_hall"));
            Assert.That(
                checkpointState.LastRespawnPosition,
                Is.EqualTo(checkpoint.RespawnPosition));
            Assert.That(checkpoint.IsActivated, Is.True);

            // 실제 위험 지대의 즉사 데미지로 사망 이벤트부터 체크포인트 재시작까지 통합 검증한다.
            Assert.That(
                meleeEnemyHealth.TakeDamage(3),
                Is.True);
            Assert.That(meleeEnemyHealth.IsDead, Is.True);
            Assert.That(
                meleeEnemy.CurrentState,
                Is.EqualTo(EnemyState.Dead));
            meleeEnemyObject.transform.position +=
                new Vector3(-2f, 1f, 0f);
            Assert.That(
                rangedEnemyHealth.TakeDamage(3),
                Is.True);
            Assert.That(rangedEnemyHealth.IsDead, Is.True);
            Assert.That(
                rangedEnemy.CurrentState,
                Is.EqualTo(EnemyState.Dead));
            rangedEnemyObject.transform.position +=
                new Vector3(2f, 1f, 0f);
            Assert.That(
                chargeEnemyHealth.TakeDamage(4),
                Is.True);
            Assert.That(
                chargeEnemyHealth.IsDead,
                Is.True);
            Assert.That(
                chargeEnemy.CurrentState,
                Is.EqualTo(EnemyState.Dead));
            chargeEnemyObject.transform.position +=
                new Vector3(-2f, 1f, 0f);
            Assert.That(
                bossHealth.TakeDamage(12),
                Is.True);
            Assert.That(bossHealth.IsDead, Is.True);
            Assert.That(
                boss.CurrentState,
                Is.EqualTo(EnemyState.Dead));
            bossObject.transform.position +=
                new Vector3(2f, 1f, 0f);
            GameObject hazardObject = GameObject.Find("RespawnHazard");
            Assert.That(hazardObject, Is.Not.Null);
            DamageVolume hazard = hazardObject.GetComponent<DamageVolume>();
            Assert.That(hazard, Is.Not.Null);
            Assert.That(
                hazardObject.transform.localScale.x,
                Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(
                GameObject.Find("Step_A").transform.position.x,
                Is.EqualTo(10f).Within(0.001f));
            Assert.That(
                GameObject.Find("High_Platform")
                    .transform.localScale.x,
                Is.EqualTo(12f).Within(0.001f));
            respawnController.Configure(0f);
            motor.Teleport(new Vector3(4.5f, 0.05f, 0f));
            Assert.That(hazard.TryApply(player), Is.True);
            Assert.That(playerHealth.IsDead, Is.True);
            Assert.That(respawnController.IsRespawning, Is.True);
            Assert.That(motor.IsControlLocked, Is.True);
            Assert.That(combat.enabled, Is.False);

            // 0초 재시작도 사망 상태 관찰을 위해 한 프레임 양보하므로 완료될 때까지 제한적으로 기다린다.
            for (int frame = 0;
                 frame < 3 && respawnController.IsRespawning;
                 frame++)
            {
                yield return null;
            }

            Assert.That(respawnController.IsRespawning, Is.False);
            Assert.That(respawnController.RespawnCount, Is.EqualTo(1));
            Assert.That(playerHealth.IsDead, Is.False);
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(playerHealth.MaxHealth));
            Assert.That(player.transform.position, Is.EqualTo(checkpoint.RespawnPosition));
            Assert.That(motor.IsControlLocked, Is.False);
            Assert.That(combat.enabled, Is.True);
            Assert.That(abilityState.UnlockedCount, Is.EqualTo(3));
            Assert.That(gate.IsLocked, Is.False);
            Assert.That(airDashGate.IsLocked, Is.False);
            Assert.That(shortcutGate.IsLocked, Is.False);
            Assert.That(
                worldState.UnlockedShortcutCount,
                Is.EqualTo(1));
            Assert.That(
                worldState.CollectedRewardCount,
                Is.EqualTo(1));
            Assert.That(playerHealth.MaxHealth, Is.EqualTo(6));
            Assert.That(meleeEnemyHealth.IsDead, Is.False);
            Assert.That(
                meleeEnemyHealth.CurrentHealth,
                Is.EqualTo(3));
            Assert.That(
                meleeEnemy.CurrentState,
                Is.EqualTo(EnemyState.Idle));
            Assert.That(
                meleeEnemyObject.transform.position,
                Is.EqualTo(meleeEnemySpawnPosition));
            Assert.That(
                meleeEnemyRenderer.enabled,
                Is.True);
            Assert.That(
                meleeEnemyObject
                    .GetComponent<CharacterController>()
                    .enabled,
                Is.True);
            Assert.That(rangedEnemyHealth.IsDead, Is.False);
            Assert.That(
                rangedEnemyHealth.CurrentHealth,
                Is.EqualTo(3));
            Assert.That(
                rangedEnemy.CurrentState,
                Is.EqualTo(EnemyState.Idle));
            Assert.That(
                rangedEnemyObject.transform.position,
                Is.EqualTo(rangedEnemySpawnPosition));
            Assert.That(rangedRenderer.enabled, Is.True);
            Assert.That(rangedBody.enabled, Is.True);
            Assert.That(
                rangedEnemy.ActiveProjectileCount,
                Is.Zero);
            Assert.That(
                chargeEnemyHealth.IsDead,
                Is.False);
            Assert.That(
                chargeEnemyHealth.CurrentHealth,
                Is.EqualTo(4));
            Assert.That(
                chargeEnemy.CurrentState,
                Is.EqualTo(EnemyState.Idle));
            Assert.That(
                chargeEnemyObject.transform.position,
                Is.EqualTo(chargeEnemySpawnPosition));
            Assert.That(
                chargeRenderer.enabled,
                Is.True);
            Assert.That(
                chargeBody.enabled,
                Is.True);
            Assert.That(bossHealth.IsDead, Is.False);
            Assert.That(
                bossHealth.CurrentHealth,
                Is.EqualTo(12));
            Assert.That(
                boss.CurrentState,
                Is.EqualTo(EnemyState.Idle));
            Assert.That(
                bossObject.transform.position,
                Is.EqualTo(bossSpawnPosition));
            Assert.That(
                bossRenderer.enabled,
                Is.True);
            Assert.That(
                bossBody.enabled,
                Is.True);
            Assert.That(
                boss.ActiveProjectileCount,
                Is.Zero);

            // 더미 가까이에서 실제 물리 탐색을 실행해 씬의 콜라이더와 자동 조준 연결을 검증한다.
            motor.Teleport(new Vector3(
                dummy.transform.position.x - 1.5f,
                dummy.transform.position.y - 0.95f,
                0f));
            Physics.SyncTransforms();
            Health selectedTarget = targeting.AcquireTarget(
                player.transform.position + Vector3.up * 0.9f,
                1f);

            Assert.That(selectedTarget, Is.SameAs(dummyHealth));
            Assert.That(targeting.AimDirection.x, Is.GreaterThan(0f));
            Assert.That(targeting.AimDirection.z, Is.Zero.Within(0.0001f));
        }
    }
}
