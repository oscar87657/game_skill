// GOLDEN STANDARD
// 목적: 플레이어가 지름길 반대편에 도달한 사건을 영구 게이트 해금으로 변환한다.
// 책임: Trigger 진입 플레이어를 검증하고 연결된 게이트를 한 번 해금한 뒤 자신의 표현을 끈다.
// 불변식: 플레이어 월드 상태만 지름길을 열 수 있으며 성공한 활성 장치는 중복 작동하지 않는다.
// 선택 이유: 자동 Trigger는 상호작용 UI 없이도 Graybox의 한쪽 개방 지름길 흐름을 빠르게 검증한다.
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ShortcutUnlockVolume : MonoBehaviour
    {
        [SerializeField] private WorldShortcutGate gate;
        [SerializeField] private Renderer visualRenderer;

        private Collider triggerCollider;
        private bool isSubscribed;

        public WorldShortcutGate Gate => gate;
        public bool IsActivated { get; private set; }

        private void Awake()
        {
            // 잘못 저장된 물리 설정도 이동을 막지 않는 Trigger 계약으로 복구한다.
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
            visualRenderer ??= GetComponentInChildren<Renderer>();
        }

        private void OnEnable()
        {
            // 활성 장치만 연결 게이트의 잠금 전환을 구독해 복원 뒤 표현을 즉시 동기화한다.
            Subscribe();
            RefreshActivationState();
        }

        private void OnDisable()
        {
            // 비활성화된 장치가 게이트 이벤트 참조로 남지 않도록 생명주기에서 구독을 해제한다.
            Unsubscribe();
        }

        public bool Configure(
            WorldShortcutGate shortcutGate,
            Renderer renderer)
        {
            bool changed = gate != shortcutGate
                || visualRenderer != renderer;
            Unsubscribe();
            gate = shortcutGate;
            visualRenderer = renderer;
            triggerCollider ??= GetComponent<Collider>();
            if (triggerCollider != null)
            {
                // Awake 전 빌더 호출에서도 활성 장치가 플레이어를 막지 않도록 강제한다.
                triggerCollider.isTrigger = true;
            }

            Subscribe();
            RefreshActivationState();
            return changed;
        }

        public bool Activate(PlayerWorldState worldState)
        {
            // 이미 사용했거나 연결·플레이어 상태가 없으면 월드 진행을 변경하지 않는다.
            if (IsActivated
                || gate == null
                || worldState == null
                || !gate.TryUnlock(worldState))
            {
                return false;
            }

            IsActivated = true;
            SetPresentation(false);
            Debug.Log($"지름길 해금: {gate.ShortcutId}", this);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 자식 콜라이더가 들어와도 부모 플레이어의 단일 월드 상태를 찾아 해금을 요청한다.
            PlayerWorldState worldState =
                other.GetComponentInParent<PlayerWorldState>();
            if (worldState != null)
            {
                Activate(worldState);
            }
        }

        public bool RefreshActivationState()
        {
            // 저장 복원으로 이미 열린 게이트라면 활성 장치도 소비된 상태로 맞춘다.
            IsActivated = gate != null
                && !gate.IsLocked;
            SetPresentation(!IsActivated);
            return IsActivated;
        }

        private void HandleGateLockChanged(bool isLocked)
        {
            // 게이트가 다시 잠기는 다른 세이브를 적용한 경우 활성 장치도 재사용 가능하게 복구한다.
            RefreshActivationState();
        }

        private void Subscribe()
        {
            // Configure와 OnEnable이 연속 호출돼도 같은 게이트 이벤트는 한 번만 구독한다.
            if (isSubscribed || gate == null)
            {
                return;
            }

            gate.LockStateChanged +=
                HandleGateLockChanged;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            // 게이트가 없거나 이미 해제된 경우에도 비활성화를 안전하게 처리한다.
            if (!isSubscribed || gate == null)
            {
                isSubscribed = false;
                return;
            }

            gate.LockStateChanged -=
                HandleGateLockChanged;
            isSubscribed = false;
        }

        private void SetPresentation(bool visible)
        {
            // 성공한 활성 장치의 Trigger와 렌더러를 함께 꺼 중복 접촉과 시각 혼동을 막는다.
            if (triggerCollider != null)
            {
                triggerCollider.enabled = visible;
            }

            if (visualRenderer != null)
            {
                visualRenderer.enabled = visible;
            }
        }
    }
}
