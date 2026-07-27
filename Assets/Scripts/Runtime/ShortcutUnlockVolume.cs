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

        public WorldShortcutGate Gate => gate;
        public bool IsActivated { get; private set; }

        private void Awake()
        {
            // 잘못 저장된 물리 설정도 이동을 막지 않는 Trigger 계약으로 복구한다.
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
            visualRenderer ??= GetComponentInChildren<Renderer>();
        }

        public bool Configure(
            WorldShortcutGate shortcutGate,
            Renderer renderer)
        {
            bool changed = gate != shortcutGate
                || visualRenderer != renderer;
            gate = shortcutGate;
            visualRenderer = renderer;
            triggerCollider ??= GetComponent<Collider>();
            if (triggerCollider != null)
            {
                // Awake 전 빌더 호출에서도 활성 장치가 플레이어를 막지 않도록 강제한다.
                triggerCollider.isTrigger = true;
            }

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
