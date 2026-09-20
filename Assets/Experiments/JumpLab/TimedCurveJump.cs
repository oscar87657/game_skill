// GOLDEN STANDARD
// 목적: 속도 대신 시간-높이 곡선으로 점프 이동량을 만든다.
// 책임: 두 시점의 높이 차이만 계산하며 충돌 시 곡선 중단은 실행 계층이 결정한다.
using UnityEngine;

namespace GameSkill.Experiments
{
    public static class TimedCurveJump
    {
        public static float Step(AnimationCurve curve, float height, float duration,
            float elapsed, float deltaTime)
        {
            // 정규화된 시간으로 끝점을 고정하고 실제 위치 대신 이동량을 충돌 계층에 전달한다.
            return height * (curve.Evaluate(Mathf.Clamp01((elapsed + deltaTime) / duration))
                - curve.Evaluate(Mathf.Clamp01(elapsed / duration)));
        }
    }
}
