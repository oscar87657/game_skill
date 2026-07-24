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
            Assert.That(player.GetComponent<PlayerInput>(), Is.Not.Null);
            Assert.That(player.GetComponent<ThirdPersonMotor>(), Is.Not.Null);

            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.GetComponent<ThirdPersonOrbitCamera>(), Is.Not.Null);

            Assert.That(GameObject.Find("Graybox"), Is.Not.Null);
            Assert.That(GameObject.Find("Wall_Gate"), Is.Not.Null);
        }
    }
}
