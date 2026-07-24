using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class MovementMathTests
    {
        [Test]
        public void HorizontalInput_UsesOnlyHorizontalAxis()
        {
            float horizontal = MovementMath.HorizontalInput(
                new Vector2(0.75f, 1f),
                0.1f);

            Assert.That(horizontal, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void HorizontalInput_AppliesDeadZone()
        {
            float horizontal = MovementMath.HorizontalInput(
                new Vector2(0.05f, 0f),
                0.1f);

            Assert.That(horizontal, Is.Zero);
        }

        [TestCase(-1f, -90f)]
        [TestCase(0f, 90f)]
        [TestCase(1f, 90f)]
        public void SideScrollerFacingYaw_ReturnsSideViewRotation(
            float direction,
            float expectedYaw)
        {
            Assert.That(
                MovementMath.SideScrollerFacingYaw(direction),
                Is.EqualTo(expectedYaw));
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
