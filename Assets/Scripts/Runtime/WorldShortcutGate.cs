// GOLDEN STANDARD
// 목적: 영구 ID 기반 지름길의 잠금 상태를 물리 게이트로 표현한다.
// 책임: 플레이어 월드 상태를 구독하고 해금 시 Collider와 시각 표현을 비활성화한다.
// 불변식: 유효한 ID와 상태가 없으면 잠기며 게이트는 자신의 해금 ID 외 진행을 변경하지 않는다.
// 선택 이유: 이벤트 기반 게이트는 매 프레임 조회하지 않고 세이브 복원과 현장 해금을 같은 상태로 처리한다.
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class WorldShortcutGate : MonoBehaviour
    {
        [SerializeField] private string shortcutId;
        [SerializeField] private PlayerWorldState playerState;
        [SerializeField] private Renderer visualRenderer;

        private Collider blockingCollider;
        private bool isSubscribed;

        public string ShortcutId => shortcutId;
        public bool IsLocked { get; private set; } = true;

        private void Awake()
        {
            // 상태 전환마다 컴포넌트를 검색하지 않도록 물리·시각 참조를 한 번 캐시한다.
            blockingCollider = GetComponent<Collider>();
            visualRenderer ??= GetComponentInChildren<Renderer>();
            RefreshLockState();
        }

        private void OnEnable()
        {
            // 활성화된 게이트만 플레이어 진행 이벤트를 구독해 불필요한 참조를 남기지 않는다.
            Subscribe();
            RefreshLockState();
        }

        private void Start()
        {
            // 모든 Awake가 끝난 뒤 한 번 더 계산해 초기 세이브 복원 순서와 무관하게 상태를 맞춘다.
            RefreshLockState();
        }

        private void OnDisable()
        {
            // 구독과 해제를 Unity 생명주기에서 대칭으로 유지해 이벤트 중복과 참조 누수를 막는다.
            Unsubscribe();
        }

        public bool Configure(
            string id,
            PlayerWorldState state,
            Renderer renderer)
        {
            string normalizedId = id?.Trim() ?? string.Empty;
            bool changed = shortcutId != normalizedId
                || playerState != state
                || visualRenderer != renderer;

            // 이전 상태의 이벤트를 먼저 해제해야 재구성 뒤에도 한 번만 알림을 받는다.
            Unsubscribe();
            shortcutId = normalizedId;
            playerState = state;
            visualRenderer = renderer;
            blockingCollider ??= GetComponent<Collider>();
            Subscribe();
            RefreshLockState();
            return changed;
        }

        public bool TryUnlock(PlayerWorldState sourceState)
        {
            // 구성된 플레이어가 아닌 오브젝트나 빈 ID는 지름길 진행을 변경할 수 없다.
            if (sourceState == null
                || sourceState != playerState
                || string.IsNullOrWhiteSpace(shortcutId))
            {
                return false;
            }

            bool newlyUnlocked =
                playerState.TryUnlockShortcut(shortcutId);
            RefreshLockState();
            return newlyUnlocked;
        }

        public bool RefreshLockState()
        {
            // 구성 오류는 진행 순서를 우회하지 않도록 실패 시 잠기는 정책으로 처리한다.
            IsLocked = string.IsNullOrWhiteSpace(shortcutId)
                || playerState == null
                || !playerState.IsShortcutUnlocked(shortcutId);

            if (blockingCollider != null)
            {
                blockingCollider.enabled = IsLocked;
                blockingCollider.isTrigger = false;
            }

            if (visualRenderer != null)
            {
                // 열린 지름길은 게이트 형상을 숨겨 통과 가능 상태를 명확히 보여 준다.
                visualRenderer.enabled = IsLocked;
            }

            return IsLocked;
        }

        private void HandleShortcutUnlocked(string unlockedId)
        {
            // 다른 지름길 이벤트는 무시해 이 게이트의 표현만 필요한 순간에 갱신한다.
            if (string.Equals(
                shortcutId,
                unlockedId,
                System.StringComparison.Ordinal))
            {
                RefreshLockState();
            }
        }

        private void Subscribe()
        {
            // Configure와 OnEnable이 연속 호출돼도 이벤트는 한 번만 구독한다.
            if (isSubscribed || playerState == null)
            {
                return;
            }

            playerState.ShortcutUnlocked += HandleShortcutUnlocked;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            // 상태 참조가 없거나 이미 해제된 경우에도 생명주기 종료를 안전하게 처리한다.
            if (!isSubscribed || playerState == null)
            {
                isSubscribed = false;
                return;
            }

            playerState.ShortcutUnlocked -= HandleShortcutUnlocked;
            isSubscribed = false;
        }
    }
}
