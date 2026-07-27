// GOLDEN STANDARD
// 목적: 공격 순간에 플레이어가 바라보는 방향의 가장 적합한 Health 대상을 선택한다.
// 책임: 물리 후보를 수집하고 순수 조준 수식으로 필터링해 현재 대상과 조준 방향을 제공한다.
// 불변식: 뒤쪽·죽은 대상·자기 자신은 선택하지 않으며 조준 방향의 Z 성분은 항상 0이다.
// 선택 이유: 타깃 탐색을 PlayerCombat과 분리해 공격 방식이나 적 종류가 바뀌어도 재사용한다.
using System.Collections.Generic;
using UnityEngine;

namespace GameSkill
{
    [RequireComponent(typeof(SideScrollerMotor))]
    public sealed class SideScrollerTargeting : MonoBehaviour
    {
        [Header("Target Search")]
        [SerializeField, Min(0.1f)] private float maximumRange = 1.65f;
        [SerializeField, Min(0f)] private float maximumHeightDifference = 1.5f;
        [SerializeField, Min(0f)] private float maximumDepthDifference = 0.8f;
        [SerializeField] private LayerMask targetLayers = ~0;

        [Header("Target Priority")]
        [SerializeField, Min(0f)] private float verticalPenalty = 0.75f;
        [SerializeField, Min(0f)] private float depthPenalty = 2f;
        [SerializeField, Range(0f, 75f)] private float maximumVerticalAimAngle = 35f;

        // NonAlloc 물리 조회 버퍼를 재사용해 공격마다 새 배열이 생기는 가비지를 피한다.
        private readonly Collider[] candidateBuffer = new Collider[24];
        private readonly HashSet<Health> evaluatedTargets = new();

        public Health CurrentTarget { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.right;
        public Vector3 AimPoint { get; private set; }

        public Health AcquireTarget(Vector3 attackOrigin, float facingDirection)
        {
            // 매 공격마다 결과를 초기화하여 이전 공격에서 죽거나 이동한 대상을 재사용하지 않는다.
            float facing = facingDirection < 0f ? -1f : 1f;
            CurrentTarget = null;
            AimDirection = new Vector3(facing, 0f, 0f);
            AimPoint = attackOrigin + AimDirection * maximumRange;
            evaluatedTargets.Clear();

            int candidateCount = Physics.OverlapSphereNonAlloc(
                attackOrigin,
                maximumRange,
                candidateBuffer,
                targetLayers,
                QueryTriggerInteraction.Collide);
            float bestScore = float.PositiveInfinity;

            // 하나의 적이 여러 콜라이더를 가져도 Health 단위로 한 번만 평가한다.
            for (int index = 0; index < candidateCount; index++)
            {
                Collider candidateCollider = candidateBuffer[index];
                Health candidate = candidateCollider != null
                    ? candidateCollider.GetComponentInParent<Health>()
                    : null;
                if (candidate == null
                    || candidate.IsDead
                    || candidate.transform == transform
                    || candidate.transform.IsChildOf(transform)
                    || !evaluatedTargets.Add(candidate))
                {
                    continue;
                }

                Vector3 candidatePoint = candidateCollider.bounds.center;
                Vector3 offset = candidatePoint - attackOrigin;
                if (!TargetingMath.IsCandidate(
                        offset,
                        facing,
                        maximumRange,
                        maximumHeightDifference,
                        maximumDepthDifference))
                {
                    continue;
                }

                float score = TargetingMath.CandidateScore(
                    offset,
                    verticalPenalty,
                    depthPenalty);
                if (score >= bestScore)
                {
                    // 지금까지의 최적 후보보다 불리하면 결과를 교체하지 않는다.
                    continue;
                }

                bestScore = score;
                CurrentTarget = candidate;
                AimPoint = candidatePoint;
            }

            if (CurrentTarget != null)
            {
                AimDirection = TargetingMath.ClampedAimDirection(
                    AimPoint - attackOrigin,
                    facing,
                    maximumVerticalAimAngle);
            }

            return CurrentTarget;
        }

        public void ClearTarget(float facingDirection)
        {
            // 공격 취소 시 디버그 표시와 다음 공격의 기준 방향을 함께 초기화한다.
            float facing = facingDirection < 0f ? -1f : 1f;
            CurrentTarget = null;
            AimDirection = new Vector3(facing, 0f, 0f);
            AimPoint = transform.position + AimDirection * maximumRange;
            evaluatedTargets.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            // Scene 뷰에서 탐색 반경과 실제 조준선을 보여 튜닝 근거를 확인할 수 있게 한다.
            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, maximumRange);

            Gizmos.color = CurrentTarget != null ? Color.cyan : Color.gray;
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Gizmos.DrawLine(origin, origin + AimDirection * maximumRange);
        }
    }
}
