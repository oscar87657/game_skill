// GOLDEN STANDARD
// 목적: 특정 능력 보유 여부를 월드 통로의 잠금 상태로 표현한다.
// 책임: 플레이어 능력 상태를 구독하고 요구 능력이 해금되면 충돌과 잠금 시각을 갱신한다.
// 불변식: 요구 능력이나 플레이어 상태가 없으면 안전하게 잠기며 게이트는 능력을 직접 지급하지 않는다.
// 선택 이유: 이벤트 기반 갱신은 매 프레임 조회하지 않으면서 획득 즉시 기존 통로를 열 수 있다.
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class AbilityGate : MonoBehaviour
    {
        private static readonly int BaseColorProperty =
            Shader.PropertyToID("_BaseColor");

        [SerializeField] private AbilityDefinition requiredAbility;
        [SerializeField] private PlayerAbilityState playerState;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Color lockedColor =
            new(1f, 0.32f, 0.15f, 1f);
        [SerializeField] private Color unlockedColor =
            new(0.2f, 0.9f, 0.75f, 0.25f);

        private Collider blockingCollider;
        private MaterialPropertyBlock visualProperties;
        private bool isSubscribed;

        public AbilityDefinition RequiredAbility => requiredAbility;
        public bool IsLocked { get; private set; } = true;

        private void Awake()
        {
            // 게이트 자신의 콜라이더와 렌더러를 한 번 캐시해 상태 갱신 비용을 일정하게 만든다.
            blockingCollider = GetComponent<Collider>();
            visualRenderer ??= GetComponentInChildren<Renderer>();
            RefreshLockState();
        }

        private void OnEnable()
        {
            // 활성화 중에만 이벤트를 구독해 비활성 오브젝트가 플레이어 상태를 붙잡지 않게 한다.
            Subscribe();
            RefreshLockState();
        }

        private void Start()
        {
            // 모든 오브젝트의 Awake 이후 한 번 더 계산해 시작 능력이 있는 씬의 실행 순서 차이를 흡수한다.
            RefreshLockState();
        }

        private void OnDisable()
        {
            // Unity 생명주기에서 구독과 해제를 대칭으로 유지해 중복 호출과 참조 누수를 막는다.
            Unsubscribe();
        }

        public bool Configure(
            AbilityDefinition ability,
            PlayerAbilityState state,
            Renderer renderer)
        {
            bool changed = requiredAbility != ability
                || playerState != state
                || visualRenderer != renderer;

            // 기존 상태에서 먼저 구독을 해제해야 재설정 시 이전 플레이어 이벤트가 남지 않는다.
            Unsubscribe();
            requiredAbility = ability;
            playerState = state;
            visualRenderer = renderer;
            blockingCollider ??= GetComponent<Collider>();
            Subscribe();
            RefreshLockState();
            return changed;
        }

        public bool RefreshLockState()
        {
            // 잘못 구성된 게이트는 길을 잘못 개방하지 않도록 실패 시 잠기는 정책을 사용한다.
            bool hasRequirement = requiredAbility != null
                && requiredAbility.IsConfigured;
            bool hasState = playerState != null;
            IsLocked = !hasRequirement
                || !hasState
                || !playerState.HasAbility(requiredAbility);

            if (blockingCollider != null)
            {
                blockingCollider.enabled = IsLocked;
                blockingCollider.isTrigger = false;
            }

            SetVisual(IsLocked);
            return IsLocked;
        }

        private void HandleAbilityUnlocked(AbilityDefinition unlockedAbility)
        {
            // 어떤 능력이 해금돼도 현재 요구 조건을 다시 계산하면 동일 ID 정의도 올바르게 처리된다.
            RefreshLockState();
        }

        private void Subscribe()
        {
            // 같은 컴포넌트가 Configure와 OnEnable을 모두 거쳐도 이벤트는 한 번만 구독한다.
            if (isSubscribed || playerState == null)
            {
                return;
            }

            playerState.AbilityUnlocked += HandleAbilityUnlocked;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            // 구독되지 않았거나 상태가 사라진 경우에는 안전하게 아무 작업도 하지 않는다.
            if (!isSubscribed || playerState == null)
            {
                isSubscribed = false;
                return;
            }

            playerState.AbilityUnlocked -= HandleAbilityUnlocked;
            isSubscribed = false;
        }

        private void SetVisual(bool locked)
        {
            // 공유 Material을 복제하지 않고 이 게이트 인스턴스의 색만 바꾼다.
            if (visualRenderer == null)
            {
                return;
            }

            visualProperties ??= new MaterialPropertyBlock();
            visualRenderer.GetPropertyBlock(visualProperties);
            visualProperties.SetColor(
                BaseColorProperty,
                locked ? lockedColor : unlockedColor);
            visualRenderer.SetPropertyBlock(visualProperties);
        }
    }
}
