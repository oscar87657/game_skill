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
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameObject player = GameObject.Find("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<CharacterController>(), Is.Not.Null);
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            Assert.That(playerInput, Is.Not.Null);
            Assert.That(playerInput.actions.FindAction("Dash"), Is.Not.Null);
            SideScrollerMotor motor = player.GetComponent<SideScrollerMotor>();
            Assert.That(motor, Is.Not.Null);
            Assert.That(motor.IsDashing, Is.False);
            Assert.That(motor.IsInvulnerable, Is.False);
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
            GameObject dummy = GameObject.Find("TrainingDummy");
            Assert.That(dummy, Is.Not.Null);
            Assert.That(dummy.GetComponent<Health>(), Is.Not.Null);
            Assert.That(player.transform.position.z, Is.EqualTo(0f).Within(0.001f));
        }
    }
}
