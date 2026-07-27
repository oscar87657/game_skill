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

        [Test]
        public void PlayerCheckpointState_RecordsPositionAndRestoresHealth()
        {
            // 체크포인트 활성화 한 번으로 위치 기록·회복·이벤트가 함께 일어나는지 검증한다.
            var player = new GameObject("CheckpointStateTestPlayer");
            try
            {
                Health health = player.AddComponent<Health>();
                health.Configure(5);
                Assert.That(health.TakeDamage(3), Is.True);
                PlayerCheckpointState checkpointState =
                    player.AddComponent<PlayerCheckpointState>();
                int activationEvents = 0;
                int restorationEvents = 0;
                checkpointState.CheckpointActivated += (_, _) =>
                    activationEvents++;
                health.Restored += (_, _) => restorationEvents++;
                Vector3 respawnPosition = new(4f, 1.05f, 0f);

                bool activated = checkpointState.ActivateCheckpoint(
                    "test_hall",
                    respawnPosition);

                Assert.That(activated, Is.True);
                Assert.That(checkpointState.HasCheckpoint, Is.True);
                Assert.That(
                    checkpointState.LastCheckpointId,
                    Is.EqualTo("test_hall"));
                Assert.That(
                    checkpointState.LastRespawnPosition,
                    Is.EqualTo(respawnPosition));
                Assert.That(health.CurrentHealth, Is.EqualTo(5));
                Assert.That(activationEvents, Is.EqualTo(1));
                Assert.That(restorationEvents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        public void PlayerCheckpointState_RejectsEmptyId(string checkpointId)
        {
            // 영구 저장 키로 사용할 수 없는 빈 ID는 상태와 체력을 바꾸지 않아야 한다.
            var player = new GameObject("InvalidCheckpointTestPlayer");
            try
            {
                Health health = player.AddComponent<Health>();
                health.Configure(5);
                health.TakeDamage(2);
                PlayerCheckpointState checkpointState =
                    player.AddComponent<PlayerCheckpointState>();

                bool activated = checkpointState.ActivateCheckpoint(
                    checkpointId,
                    Vector3.zero);

                Assert.That(activated, Is.False);
                Assert.That(checkpointState.HasCheckpoint, Is.False);
                Assert.That(health.CurrentHealth, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerCheckpointState_RejectsInvalidPosition()
        {
            // NaN 재시작 좌표가 저장되면 복구가 불가능하므로 입력 경계에서 차단한다.
            var player = new GameObject("InvalidCheckpointPositionTestPlayer");
            try
            {
                Health health = player.AddComponent<Health>();
                health.Configure(3);
                PlayerCheckpointState checkpointState =
                    player.AddComponent<PlayerCheckpointState>();

                bool activated = checkpointState.ActivateCheckpoint(
                    "invalid_position",
                    new Vector3(float.NaN, 0f, 0f));

                Assert.That(activated, Is.False);
                Assert.That(checkpointState.HasCheckpoint, Is.False);
                Assert.That(
                    health.CurrentHealth,
                    Is.EqualTo(health.MaxHealth));
            }
            finally
            {
                Object.DestroyImmediate(player);
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
