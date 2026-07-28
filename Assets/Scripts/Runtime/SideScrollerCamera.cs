// GOLDEN STANDARD
// 목적: 게임 물리를 변경하지 않고 구역 경계 안에서 2.5D 플레이어를 따라간다.
// 책임: 목표·방향 미리보기·현재 구역 경계를 계산하고 카메라 위치만 부드럽게 보정한다.
// 불변식: 카메라 회전·깊이축은 고정되고 추적 목표 중심점은 활성 구역 허용 범위 안에 있다.
// 선택 이유: SmoothDamp 추적과 데이터 기반 경계를 결합해 방 전환의 가독성과 튜닝 가능성을 함께 얻는다.
using System.Collections.Generic;
using UnityEngine;

namespace GameSkill
{
    public sealed class SideScrollerCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 2.4f, -9f);
        [SerializeField, Min(0f)] private float horizontalLookAhead = 1.35f;
        [SerializeField, Min(0.01f)] private float horizontalSmoothTime = 0.16f;
        [SerializeField, Min(0.01f)] private float verticalSmoothTime = 0.24f;
        [SerializeField] private PlayerWorldState playerWorldState;
        [SerializeField] private WorldZoneDefinition initialZone;
        [SerializeField]
        private List<CameraZoneBounds> zoneBounds = new();

        private SideScrollerMotor motor;
        private float horizontalVelocity;
        private float verticalVelocity;
        private CameraZoneBounds activeBounds;
        private bool isSubscribed;

        public string ActiveZoneId =>
            activeBounds?.Zone?.Id ?? string.Empty;
        public bool HasActiveBounds => activeBounds != null;

        public void Configure(Transform followTarget)
        {
            // 입력값이 아니라 실제 바라보는 방향을 따라가도록 대상의 Motor를 한 번 찾는다.
            target = followTarget;
            motor = target != null ? target.GetComponent<SideScrollerMotor>() : null;
        }

        private void Awake()
        {
            // 씬에서 직접 연결한 참조와 에디터가 생성한 설정을 모두 지원한다.
            Configure(target);
            Subscribe();
            TryApplyZone(initialZone);
        }

        private void OnEnable()
        {
            // 활성 카메라만 구역 진입 이벤트를 받아 중복 경계 전환을 방지한다.
            Subscribe();
        }

        private void Start()
        {
            // 첫 프레임에 원점에서 카메라가 이동하는 현상을 막는다.
            if (activeBounds == null)
            {
                TryApplyZone(initialZone);
            }

            SnapToTarget();
        }

        private void OnDisable()
        {
            // 비활성 카메라가 플레이어 상태 이벤트를 계속 받지 않도록 구독을 해제한다.
            Unsubscribe();
        }

        public bool ConfigureWorldBounds(
            PlayerWorldState worldState,
            WorldZoneDefinition firstZone,
            IEnumerable<CameraZoneBounds> bounds)
        {
            var requestedBounds = new List<CameraZoneBounds>();

            // 호출자가 소유한 컬렉션과 Inspector 직렬화 목록을 분리해 이후 변경 영향을 막는다.
            if (bounds != null)
            {
                foreach (CameraZoneBounds candidate in bounds)
                {
                    if (candidate != null)
                    {
                        requestedBounds.Add(candidate);
                    }
                }
            }

            bool changed = playerWorldState != worldState
                || initialZone != firstZone
                || !BoundsMatch(zoneBounds, requestedBounds);
            if (!changed)
            {
                // 동일 구성에서도 생명주기상 해제된 이벤트가 있으면 다시 연결한다.
                Subscribe();
                TryApplyZone(
                    playerWorldState?.CurrentZone
                    ?? initialZone);
                return false;
            }

            Unsubscribe();
            playerWorldState = worldState;
            initialZone = firstZone;
            zoneBounds.Clear();
            zoneBounds.AddRange(requestedBounds);
            Subscribe();
            TryApplyZone(
                playerWorldState?.CurrentZone
                ?? initialZone);
            return true;
        }

        public bool TryApplyZone(
            WorldZoneDefinition zone)
        {
            // 현재 구역과 ID가 일치하는 첫 바인딩을 활성 경계로 선택한다.
            foreach (CameraZoneBounds candidate in zoneBounds)
            {
                if (candidate != null
                    && candidate.Matches(zone))
                {
                    activeBounds = candidate;
                    return true;
                }
            }

            return false;
        }

        public Vector3 ConstrainPosition(
            Vector3 desiredPosition)
        {
            // 경계가 아직 구성되지 않은 독립 테스트 씬은 기존 자유 추적 동작을 유지한다.
            return activeBounds != null
                ? activeBounds.Constrain(desiredPosition)
                : desiredPosition;
        }

        private void LateUpdate()
        {
            // 플레이어 이동 뒤 실행하여 한 프레임 늦는 카메라 현상을 줄인다.
            if (target == null)
            {
                return;
            }

            float facingDirection = motor != null ? motor.FacingDirection : 0f;
            Vector3 desiredPosition =
                ConstrainPosition(
                    TargetPosition(facingDirection));
            float x = Mathf.SmoothDamp(
                transform.position.x,
                desiredPosition.x,
                ref horizontalVelocity,
                horizontalSmoothTime);
            float y = Mathf.SmoothDamp(
                transform.position.y,
                desiredPosition.y,
                ref verticalVelocity,
                verticalSmoothTime);

            transform.SetPositionAndRotation(
                new Vector3(x, y, desiredPosition.z),
                Quaternion.identity);
        }

        private void SnapToTarget()
        {
            // 씬 진입이나 부활 시 카메라를 즉시 올바른 위치에 둔다.
            if (target == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                ConstrainPosition(
                    TargetPosition(
                        motor != null
                            ? motor.FacingDirection
                            : 0f)),
                Quaternion.identity);
        }

        private Vector3 TargetPosition(float facingDirection)
        {
            // 속도가 아니라 바라보는 방향을 사용해 의도적인 방향 전환을 미리 보여준다.
            return new Vector3(
                target.position.x + offset.x + facingDirection * horizontalLookAhead,
                target.position.y + offset.y,
                target.position.z + offset.z);
        }

        private void HandleZoneEntered(
            WorldZoneDefinition enteredZone)
        {
            // 스트리밍과 같은 ZoneEntered 이벤트를 사용해 시각 콘텐츠와 카메라 방을 동기화한다.
            TryApplyZone(enteredZone);
        }

        private void Subscribe()
        {
            // Awake·OnEnable·Configure가 연속 실행돼도 플레이어 이벤트는 한 번만 구독한다.
            if (isSubscribed || playerWorldState == null)
            {
                return;
            }

            playerWorldState.ZoneEntered += HandleZoneEntered;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            // 아직 구독하지 않았거나 상태가 사라진 경우에도 생명주기 종료를 안전하게 처리한다.
            if (!isSubscribed || playerWorldState == null)
            {
                isSubscribed = false;
                return;
            }

            playerWorldState.ZoneEntered -= HandleZoneEntered;
            isSubscribed = false;
        }

        private static bool BoundsMatch(
            IReadOnlyList<CameraZoneBounds> current,
            IReadOnlyList<CameraZoneBounds> requested)
        {
            // 개수가 다르면 같은 구역별 카메라 데이터 구성이 될 수 없으므로 즉시 실패한다.
            if (current.Count != requested.Count)
            {
                return false;
            }

            // 순서와 구역 참조·좌표를 모두 비교해 빌더 재실행 결과를 결정적으로 유지한다.
            for (int index = 0; index < current.Count; index++)
            {
                CameraZoneBounds currentBounds =
                    current[index];
                CameraZoneBounds requestedBounds =
                    requested[index];
                if (currentBounds == null
                    || requestedBounds == null
                    || currentBounds.Zone
                        != requestedBounds.Zone
                    || currentBounds.MinimumCenter
                        != requestedBounds.MinimumCenter
                    || currentBounds.MaximumCenter
                        != requestedBounds.MaximumCenter)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            // Scene 뷰에서 각 사각형을 그려 디자이너가 카메라 중심 이동 범위를 직접 확인하게 한다.
            foreach (CameraZoneBounds bounds in zoneBounds)
            {
                if (bounds == null || !bounds.IsConfigured)
                {
                    continue;
                }

                Vector2 minimum = Vector2.Min(
                    bounds.MinimumCenter,
                    bounds.MaximumCenter);
                Vector2 maximum = Vector2.Max(
                    bounds.MinimumCenter,
                    bounds.MaximumCenter);
                Vector2 center = (minimum + maximum) * 0.5f;
                Vector2 size = maximum - minimum;
                Gizmos.color = bounds == activeBounds
                    ? new Color(0.25f, 1f, 0.55f, 1f)
                    : new Color(0.25f, 0.75f, 1f, 0.75f);
                Gizmos.DrawWireCube(
                    new Vector3(
                        center.x,
                        center.y,
                        transform.position.z),
                    new Vector3(
                        Mathf.Max(size.x, 0.05f),
                        Mathf.Max(size.y, 0.05f),
                        0.05f));
            }
        }
    }
}
