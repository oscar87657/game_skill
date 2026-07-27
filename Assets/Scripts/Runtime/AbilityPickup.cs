// GOLDEN STANDARD
// 목적: 월드의 능력 오브젝트 접촉을 플레이어 능력 해금으로 변환한다.
// 책임: 플레이어 상태를 탐색해 능력을 등록하고 성공 시 충돌·시각 표현을 비활성화한다.
// 불변식: 유효한 능력은 한 번만 소비되며 픽업은 이동이나 게이트를 직접 제어하지 않는다.
// 선택 이유: 획득 표현을 상태 저장과 분리하면 보상 연출을 바꿔도 진행 규칙을 재사용할 수 있다.
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class AbilityPickup : MonoBehaviour
    {
        [SerializeField] private AbilityDefinition ability;
        [SerializeField] private Renderer visualRenderer;

        private Collider pickupCollider;

        public AbilityDefinition Ability => ability;
        public bool IsCollected { get; private set; }

        private void Awake()
        {
            // 물리 설정이 잘못 저장되어도 픽업이 플레이어를 막는 벽이 되지 않게 강제한다.
            pickupCollider = GetComponent<Collider>();
            pickupCollider.isTrigger = true;
            visualRenderer ??= GetComponentInChildren<Renderer>();
        }

        public void Configure(
            AbilityDefinition abilityDefinition,
            Renderer renderer)
        {
            ability = abilityDefinition;
            visualRenderer = renderer;
            pickupCollider ??= GetComponent<Collider>();

            // 테스트나 빌더가 Awake 이전에 호출해도 Trigger 계약을 동일하게 보장한다.
            if (pickupCollider != null)
            {
                pickupCollider.isTrigger = true;
            }
        }

        public bool Collect(PlayerAbilityState playerState)
        {
            // 이미 소비됐거나 필수 참조가 없으면 상태를 바꾸지 않는다.
            if (IsCollected
                || playerState == null
                || ability == null
                || !ability.IsConfigured)
            {
                return false;
            }

            bool alreadyOwned = playerState.HasAbility(ability);
            bool newlyUnlocked = playerState.TryUnlock(ability);

            // 저장 복원으로 이미 가진 능력도 다시 보일 필요가 없으므로 픽업을 소비한다.
            if (!alreadyOwned && !newlyUnlocked)
            {
                return false;
            }

            IsCollected = true;
            SetPresentation(false);
            Debug.Log($"능력 획득: {ability.DisplayName} ({ability.Id})", this);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 자식 콜라이더가 접촉해도 부모 플레이어의 단일 능력 상태를 찾는다.
            PlayerAbilityState playerState =
                other.GetComponentInParent<PlayerAbilityState>();
            if (playerState != null)
            {
                Collect(playerState);
            }
        }

        private void SetPresentation(bool visible)
        {
            // 콜라이더와 렌더러를 함께 전환해 보이지 않는 픽업의 중복 접촉을 막는다.
            if (pickupCollider != null)
            {
                pickupCollider.enabled = visible;
            }

            if (visualRenderer != null)
            {
                visualRenderer.enabled = visible;
            }
        }
    }
}
