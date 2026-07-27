// GOLDEN STANDARD
// 목적: 씬을 로드하지 않고 결정적인 이동 수식을 검증한다.
// 책임: 정상값·데드존·방향·대시 방향·잘못된 물리 입력을 확인한다.
// 불변식: 프레임 시간과 Unity 씬 상태에 의존하지 않는다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class MovementMathTests
    {
        [Test]
        public void HorizontalInput_UsesOnlyHorizontalAxis()
        {
            // 회귀 방지: 수직 스틱 잡음이 횡스크롤 이동에 영향을 주면 안 된다.
            float horizontal = MovementMath.HorizontalInput(
                new Vector2(0.75f, 1f),
                0.1f);

            Assert.That(horizontal, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void HorizontalInput_AppliesDeadZone()
        {
            // 회귀 방지: 작은 아날로그 드리프트는 중립 입력으로 처리해야 한다.
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
            // 이 프로토타입에서 허용하는 시각 방향은 두 yaw뿐이다.
            Assert.That(
                MovementMath.SideScrollerFacingYaw(direction),
                Is.EqualTo(expectedYaw));
        }

        [TestCase(-0.5f, 1f, -1f)]
        [TestCase(0.5f, -1f, 1f)]
        [TestCase(0f, -1f, -1f)]
        [TestCase(0f, 1f, 1f)]
        public void DodgeDirection_PrefersInputThenFallsBackToFacing(
            float horizontalInput,
            float facingDirection,
            float expectedDirection)
        {
            // 방향은 현재 의도를 따르고 스틱이 중립이면 바라보는 방향을 사용해야 한다.
            Assert.That(
                MovementMath.DodgeDirection(horizontalInput, facingDirection),
                Is.EqualTo(expectedDirection));
        }

        [Test]
        public void JumpSpeed_ReturnsExpectedBallisticSpeed()
        {
            // 씬 시뮬레이션 대신 분석적인 포물선 공식을 기준으로 비교한다.
            float speed = MovementMath.JumpSpeed(2f, -9.81f);

            Assert.That(speed, Is.EqualTo(Mathf.Sqrt(39.24f)).Within(0.0001f));
        }

        [TestCase(0f, -9.81f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, -9.81f)]
        public void JumpSpeed_ReturnsZeroForInvalidConfiguration(float height, float gravity)
        {
            // 잘못된 디자이너 값은 NaN을 만들지 말고 안전하게 실패해야 한다.
            Assert.That(MovementMath.JumpSpeed(height, gravity), Is.Zero);
        }

        [Test]
        public void Health_TakeDamageClampsAndReportsDeath()
        {
            // 데미지·사망 알림·제한·사망 후 거부를 하나의 계약으로 검증한다.
            var target = new GameObject("HealthTestTarget");
            try
            {
                Health health = target.AddComponent<Health>();
                health.Configure(3);
                int damageEvents = 0;
                int deathEvents = 0;
                health.Damaged += (_, _) => damageEvents++;
                health.Died += () => deathEvents++;

                Assert.That(health.TakeDamage(2), Is.True);
                Assert.That(health.CurrentHealth, Is.EqualTo(1));
                Assert.That(health.TakeDamage(4), Is.True);
                Assert.That(health.CurrentHealth, Is.Zero);
                Assert.That(health.IsDead, Is.True);
                Assert.That(damageEvents, Is.EqualTo(2));
                Assert.That(deathEvents, Is.EqualTo(1));
                Assert.That(health.TakeDamage(1), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [TestCase(0, 3, 1)]
        [TestCase(1, 3, 2)]
        [TestCase(2, 3, 3)]
        [TestCase(3, 3, 1)]
        public void NextComboStep_AdvancesAndWraps(
            int currentStep,
            int comboLength,
            int expectedStep)
        {
            // 마지막 단계 다음에는 첫 단계로 돌아가야 반복 입력이 새 콤보 사이클을 만든다.
            Assert.That(
                CombatMath.NextComboStep(currentStep, comboLength),
                Is.EqualTo(expectedStep));
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        public void DamageForComboStep_AddsBonusOnlyToFinisher(
            int comboStep,
            int expectedDamage)
        {
            // 기본 데미지 1과 마무리 보너스 1을 사용해 3타만 강화되는지 검증한다.
            Assert.That(
                CombatMath.DamageForComboStep(1, comboStep, 1),
                Is.EqualTo(expectedDamage));
        }
    }
}
