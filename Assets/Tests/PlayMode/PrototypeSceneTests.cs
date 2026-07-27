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
            PlayerWorldState worldState =
                player.GetComponent<PlayerWorldState>();
            Assert.That(worldState, Is.Not.Null);
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
            Assert.That(camera.GetComponent<SideScrollerCamera>(), Is.Not.Null);
            Assert.That(camera.orthographic, Is.True);

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
            Assert.That(
                GameObject.Find("Backtrack_Reward").transform.position,
                Is.EqualTo(new Vector3(-11f, 6.5f, 0f)));

            // 세 Graybox 구역의 맞닿는 Trigger와 영구 ID 기반 방문 흐름을 실제 씬에서 검증한다.
            WorldZoneVolume startHall =
                GameObject.Find("Zone_StartHall")
                    .GetComponent<WorldZoneVolume>();
            WorldZoneVolume traversalLab =
                GameObject.Find("Zone_TraversalLab")
                    .GetComponent<WorldZoneVolume>();
            WorldZoneVolume backtrackShaft =
                GameObject.Find("Zone_BacktrackShaft")
                    .GetComponent<WorldZoneVolume>();
            Assert.That(startHall, Is.Not.Null);
            Assert.That(traversalLab, Is.Not.Null);
            Assert.That(backtrackShaft, Is.Not.Null);
            Assert.That(startHall.Zone.Id, Is.EqualTo("start_hall"));
            Assert.That(
                traversalLab.Zone.Id,
                Is.EqualTo("traversal_lab"));
            Assert.That(
                backtrackShaft.Zone.Id,
                Is.EqualTo("backtrack_shaft"));
            Assert.That(
                startHall.GetComponent<Collider>().isTrigger,
                Is.True);
            worldState.ConfigureInitialZones(null);
            Assert.That(worldState.VisitedCount, Is.Zero);
            Assert.That(startHall.Enter(worldState), Is.True);
            Assert.That(startHall.Enter(worldState), Is.False);
            Assert.That(traversalLab.Enter(worldState), Is.True);
            Assert.That(backtrackShaft.Enter(worldState), Is.True);
            Assert.That(worldState.VisitedCount, Is.EqualTo(3));
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
            Health dummyHealth = dummy.GetComponent<Health>();
            Assert.That(dummyHealth, Is.Not.Null);
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
