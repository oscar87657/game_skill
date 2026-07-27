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
            Assert.That(player.GetComponent<PlayerCombat>(), Is.Not.Null);
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

            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.GetComponent<SideScrollerCamera>(), Is.Not.Null);
            Assert.That(camera.orthographic, Is.True);

            Assert.That(GameObject.Find("SideScrollerGraybox"), Is.Not.Null);
            Assert.That(GameObject.Find("Wall_Gate"), Is.Not.Null);
            Assert.That(GameObject.Find("Slope_Test"), Is.Not.Null);
            GameObject dummy = GameObject.Find("TrainingDummy");
            Assert.That(dummy, Is.Not.Null);
            Assert.That(dummy.GetComponent<Health>(), Is.Not.Null);
            Assert.That(player.transform.position.z, Is.EqualTo(0f).Within(0.001f));
        }
    }
}
