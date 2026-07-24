using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class MovementMathTests
    {
        [Test]
        public void CameraRelativeDirection_UsesFlattenedCameraAxes()
        {
            Vector3 direction = MovementMath.CameraRelativeDirection(
                Vector2.up,
                new Vector3(0f, 1f, 1f),
                Vector3.right);

            Assert.That(direction.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(direction.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(direction.z, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void CameraRelativeDirection_ClampsDiagonalInput()
        {
            Vector3 direction = MovementMath.CameraRelativeDirection(
                Vector2.one,
                Vector3.forward,
                Vector3.right);

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void JumpSpeed_ReturnsExpectedBallisticSpeed()
        {
            float speed = MovementMath.JumpSpeed(2f, -9.81f);

            Assert.That(speed, Is.EqualTo(Mathf.Sqrt(39.24f)).Within(0.0001f));
        }

        [TestCase(0f, -9.81f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, -9.81f)]
        public void JumpSpeed_ReturnsZeroForInvalidConfiguration(float height, float gravity)
        {
            Assert.That(MovementMath.JumpSpeed(height, gravity), Is.Zero);
        }
    }
}
