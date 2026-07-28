// GOLDEN STANDARD
// 목적: 튜토리얼과 수직 슬라이스 진행 상태가 올바른 현재 목표로 변환되는지 검증한다.
// 책임: 기본 조작 순서, 능력 순서, 샤프트 분기, 보스 완료와 표시 콘텐츠 계약을 확인한다.
// 불변식: 테스트는 씬·입력 장치·프레임 시간에 의존하지 않는다.
// 선택 이유: 길 찾기 규칙을 순수 함수로 검증하면 UI와 월드 표식 배치 변경의 영향을 받지 않는다.
using System;
using NUnit.Framework;

namespace GameSkill.Tests
{
    public sealed class GuidanceProgressionTests
    {
        [TestCase(
            false,
            false,
            false,
            false,
            GuidanceStage.LearnMove)]
        [TestCase(
            true,
            false,
            false,
            false,
            GuidanceStage.LearnJump)]
        [TestCase(
            true,
            true,
            false,
            false,
            GuidanceStage.LearnDash)]
        [TestCase(
            true,
            true,
            true,
            false,
            GuidanceStage.LearnAttack)]
        [TestCase(
            true,
            true,
            true,
            true,
            GuidanceStage.ReachDoubleJump)]
        public void Resolve_AdvancesBasicTutorialFromSuccessfulActions(
            bool moved,
            bool jumped,
            bool dashed,
            bool attacked,
            GuidanceStage expected)
        {
            // 능력이 없는 새 세션은 완료한 실제 조작 다음의 첫 미완료 단계만 안내해야 한다.
            GuidanceStage result =
                GuidanceProgression.Resolve(
                    moved,
                    jumped,
                    dashed,
                    attacked,
                    false,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    false);

            Assert.That(
                result,
                Is.EqualTo(expected));
        }

        [TestCase(
            false,
            false,
            false,
            GuidanceStage.ReachDoubleJump)]
        [TestCase(
            true,
            false,
            false,
            GuidanceStage.ReachAirDash)]
        [TestCase(
            true,
            true,
            false,
            GuidanceStage.ReachWallTraversal)]
        public void Resolve_FollowsAbilityUnlockOrder(
            bool hasDoubleJump,
            bool hasAirDash,
            bool hasWallTraversal,
            GuidanceStage expected)
        {
            // 기존 능력이 하나라도 있으면 반복 기본 튜토리얼을 건너뛰고 첫 미해금 능력을 찾는다.
            GuidanceStage result =
                GuidanceProgression.Resolve(
                    true,
                    true,
                    true,
                    true,
                    hasDoubleJump,
                    hasAirDash,
                    hasWallTraversal,
                    string.Empty,
                    false,
                    false,
                    false);

            Assert.That(
                result,
                Is.EqualTo(expected));
        }

        [TestCase(
            "start_hall",
            false,
            false,
            false,
            GuidanceStage.ReturnToShaft)]
        [TestCase(
            "backtrack_shaft",
            false,
            false,
            false,
            GuidanceStage.ClimbShaft)]
        [TestCase(
            "backtrack_shaft",
            true,
            false,
            false,
            GuidanceStage.ActivateShortcut)]
        [TestCase(
            "start_hall",
            true,
            true,
            false,
            GuidanceStage.ReachBossRoom)]
        [TestCase(
            "boss_room",
            true,
            true,
            false,
            GuidanceStage.DefeatBoss)]
        [TestCase(
            "start_hall",
            true,
            true,
            true,
            GuidanceStage.Complete)]
        public void Resolve_UsesWorldProgressForBacktrackAndBoss(
            string currentZoneId,
            bool rewardCollected,
            bool shortcutUnlocked,
            bool bossDefeated,
            GuidanceStage expected)
        {
            // 세 능력 이후에는 현재 구역과 영구 월드 상태가 백트래킹·보스 목표를 결정해야 한다.
            GuidanceStage result =
                GuidanceProgression.Resolve(
                    true,
                    true,
                    true,
                    true,
                    true,
                    true,
                    true,
                    currentZoneId,
                    rewardCollected,
                    shortcutUnlocked,
                    bossDefeated);

            Assert.That(
                result,
                Is.EqualTo(expected));
        }

        [Test]
        public void ContentFor_ProvidesReadableTextForEveryStage()
        {
            // enum에 단계가 추가됐는데 문구 매핑이 빠지는 회귀를 전체 값 순회로 찾는다.
            foreach (GuidanceStage stage
                in Enum.GetValues(
                    typeof(GuidanceStage)))
            {
                GuidanceContent content =
                    GuidanceProgression.ContentFor(
                        stage);
                Assert.That(
                    content.Stage,
                    Is.EqualTo(stage));
                Assert.That(
                    content.Objective,
                    Is.Not.Empty);
                Assert.That(
                    content.Hint,
                    Is.Not.Empty);
            }

            Assert.That(
                GuidanceProgression.ContentFor(
                    GuidanceStage.ReachAirDash)
                    .MarkerId,
                Is.EqualTo("air_dash"));
            Assert.That(
                GuidanceProgression.ContentFor(
                    GuidanceStage.Complete)
                    .HasMarker,
                Is.False);
        }

        [Test]
        public void Waypoint_MatchesOnlyNormalizedNonEmptyId()
        {
            // 직렬화 목적지는 앞뒤 공백을 허용하되 빈 안내 단계와는 일치하지 않아야 한다.
            var waypoint =
                new GuidanceWaypoint(
                    " boss_target ",
                    UnityEngine.Vector3.one);

            Assert.That(
                waypoint.Matches(
                    "boss_target"),
                Is.True);
            Assert.That(
                waypoint.Matches(
                    "  "),
                Is.False);
            Assert.That(
                waypoint.Matches(
                    "boss_entrance"),
                Is.False);
        }
    }
}
