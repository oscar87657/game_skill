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
            Assert.That(motor.CanAirDash, Is.True);
            Assert.That(motor.AirJumpsRemaining, Is.EqualTo(1));
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
            Assert.That(GameObject.Find("Wall_Gate"), Is.Not.Null);
            Assert.That(GameObject.Find("Slope_Test"), Is.Not.Null);
            GameObject dummy = GameObject.Find("TrainingDummy");
            Assert.That(dummy, Is.Not.Null);
            Health dummyHealth = dummy.GetComponent<Health>();
            Assert.That(dummyHealth, Is.Not.Null);
            Assert.That(player.transform.position.z, Is.EqualTo(0f).Within(0.001f));

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

            // 더미 가까이에서 실제 물리 탐색을 실행해 씬의 콜라이더와 자동 조준 연결을 검증한다.
            motor.Teleport(new Vector3(
                dummy.transform.position.x - 1.5f,
                0.05f,
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
