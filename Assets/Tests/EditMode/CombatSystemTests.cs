// GOLDEN STANDARD
// 목적: 콤보 계산, 공중 공격 체공 제한과 2.5D 자동 조준 규칙을 씬 없이 검증한다.
// 책임: 콤보 길이 경계, 마무리 데미지, 체공 상한, 후보 필터와 조준 각도를 확인한다.
// 불변식: 프레임 시간, 물리 Collider와 MonoBehaviour 생명주기에 의존하지 않는다.
// 선택 이유: 공격 실행과 분리된 규칙을 한 테스트 묶음에 모아 전투 계약의 변경 범위를 드러낸다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class CombatSystemTests
    {
        [TestCase(0, 3, 1)]
        [TestCase(1, 3, 2)]
        [TestCase(2, 3, 3)]
        [TestCase(3, 3, 1)]
        [TestCase(4, 5, 5)]
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

        [TestCase(1, 3, 1)]
        [TestCase(2, 3, 1)]
        [TestCase(3, 3, 2)]
        [TestCase(3, 4, 1)]
        [TestCase(4, 4, 2)]
        public void DamageForComboStep_AddsBonusOnlyToConfiguredFinisher(
            int comboStep,
            int comboLength,
            int expectedDamage)
        {
            // 콤보 길이를 바꿔도 하드코딩된 3타가 아니라 설정된 마지막 타격만 강화되어야 한다.
            Assert.That(
                CombatMath.DamageForComboStep(
                    1,
                    comboStep,
                    comboLength,
                    1),
                Is.EqualTo(expectedDamage));
        }

        [TestCase(0.06f, 0.1f, 0.06f)]
        [TestCase(0.2f, 0.1f, 0.1f)]
        [TestCase(-0.1f, 0.1f, 0f)]
        [TestCase(0.1f, -1f, 0f)]
        public void AirAttackHoverDuration_ClampsRequestToMaximum(
            float requestedDuration,
            float maximumDuration,
            float expectedDuration)
        {
            // 연속 공중 공격 설정이 커져도 한 번의 요청이 체공 상한을 우회하면 안 된다.
            Assert.That(
                MovementMath.AirAttackHoverDuration(
                    requestedDuration,
                    maximumDuration),
                Is.EqualTo(expectedDuration).Within(0.0001f));
        }

        [TestCase(1f, 0f, 0f, 1f, true)]
        [TestCase(-1f, 0f, 0f, 1f, false)]
        [TestCase(1f, 2f, 0f, 1f, false)]
        [TestCase(1f, 0f, 1f, 1f, false)]
        [TestCase(3f, 0f, 0f, 1f, false)]
        public void IsCandidate_FiltersSideScrollerTargetSpace(
            float x,
            float y,
            float z,
            float facingDirection,
            bool expected)
        {
            // 정면·사거리·높이·깊이 경계를 하나씩 넘겨 자동 조준 후보 계약을 검증한다.
            bool result = TargetingMath.IsCandidate(
                new Vector3(x, y, z),
                facingDirection,
                2.4f,
                1.5f,
                0.8f);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CandidateScore_PrefersTargetNearAttackPlane()
        {
            // 수평 거리가 비슷하면 높이와 깊이 차이가 작은 적을 먼저 골라야 한다.
            float centeredScore = TargetingMath.CandidateScore(
                new Vector3(1.5f, 0.1f, 0f),
                0.75f,
                2f);
            float offsetScore = TargetingMath.CandidateScore(
                new Vector3(1.5f, 0.8f, 0.4f),
                0.75f,
                2f);

            Assert.That(centeredScore, Is.LessThan(offsetScore));
        }

        [Test]
        public void ClampedAimDirection_LimitsVerticalAngleAndKeepsFacing()
        {
            // 매우 높은 대상도 설정된 35도까지만 조준하며 왼쪽 방향을 유지해야 한다.
            Vector3 direction = TargetingMath.ClampedAimDirection(
                new Vector3(-0.2f, 3f, 0.5f),
                -1f,
                35f);
            float angle = Mathf.Atan2(
                direction.y,
                Mathf.Abs(direction.x)) * Mathf.Rad2Deg;

            Assert.That(direction.x, Is.LessThan(0f));
            Assert.That(direction.z, Is.Zero.Within(0.0001f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(angle, Is.EqualTo(35f).Within(0.0001f));
        }
    }
}
