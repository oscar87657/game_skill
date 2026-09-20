// GOLDEN STANDARD
// 목적: 후보를 다르게 만드는 해제·시간 곡선과 탄도 적분 계약을 고정한다.
// 책임: 손맛이 아니라 계산 결과와 프레임 분할 독립성을 검증한다.
using GameSkill.Experiments;
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class JumpExperimentMathTests
    {
        [Test]
        public void JumpCandidates_PreserveBallisticApexAndExposeReleaseAndCurveDifferences()
        {
            // 같은 중력과 초기 속도라면 한 번 또는 여러 번 적분해도 정점 높이는 같다.
            float singleVelocity = 12f;
            float whole = GravityJump.Step(ref singleVelocity, 24f, 0.5f);
            float splitVelocity = 12f;
            float split = 0f;
            for (int i = 0; i < 60; i++)
                split += GravityJump.Step(ref splitVelocity, 24f, 0.5f / 60f);
            Assert.That(whole, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(split, Is.EqualTo(whole).Within(0.0001f));
            Assert.That(singleVelocity, Is.EqualTo(0f));

            float heldVelocity = 12f;
            float releasedVelocity = 12f;
            float held = ReleaseCutJump.Step(ref heldVelocity, 24f, 2f, true, 0.1f);
            float released = ReleaseCutJump.Step(ref releasedVelocity, 24f, 2f, false, 0.1f);
            Assert.That(released, Is.LessThan(held));
            Assert.That(releasedVelocity, Is.LessThan(heldVelocity));
            float fallingVelocity = -5f;
            ReleaseCutJump.Step(ref fallingVelocity, 24f, 2f, false, 0.1f);
            Assert.That(fallingVelocity, Is.EqualTo(-7.4f).Within(0.0001f));

            // 곡선은 정점 시점을 바꿀 수 있지만 같은 높이로 돌아오면 누적 이동량은 0이다.
            var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0f));
            Assert.That(TimedCurveJump.Step(curve, 3f, 1f, 0f, 0.4f), Is.EqualTo(3f));
            float displacement = 0f;
            for (int i = 0; i < 120; i++)
                displacement += TimedCurveJump.Step(curve, 3f, 1f, i / 120f, 1f / 120f);
            Assert.That(displacement, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(TimedCurveJump.Step(curve, 3f, 1f, 2f, 0.1f), Is.EqualTo(0f));
        }
    }
}
