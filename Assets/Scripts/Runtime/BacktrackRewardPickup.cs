// GOLDEN STANDARD
// 목적: 능력으로 다시 방문한 구역의 보상 Trigger를 영구 획득 상태와 최대 체력 증가로 변환한다.
// 책임: 요구 능력·플레이어 참조·보상 ID를 검증하고 성공 시 효과와 표현을 한 번만 적용한다.
// 불변식: 요구 능력이 없거나 이미 획득한 보상은 체력을 바꾸지 않으며 성공한 픽업은 다시 충돌하지 않는다.
// 선택 이유: 위치 기반 게이트와 상태 기반 검증을 함께 사용하면 레벨 우회에도 안전하고 효과 교체도 쉽다.
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class BacktrackRewardPickup : MonoBehaviour
    {
        [SerializeField] private string rewardId;
        [SerializeField, Min(1)] private int maximumHealthBonus = 1;
        [SerializeField] private AbilityDefinition requiredAbility;
        [SerializeField] private PlayerWorldState playerWorldState;
        [SerializeField] private PlayerAbilityState playerAbilityState;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Renderer visualRenderer;

        private Collider triggerCollider;

        public string RewardId => rewardId;
        public int MaximumHealthBonus => maximumHealthBonus;
        public AbilityDefinition RequiredAbility => requiredAbility;
        public bool IsCollected { get; private set; }
        public bool IsRequirementMet =>
            requiredAbility != null
            && requiredAbility.IsConfigured
            && playerAbilityState != null
            && playerAbilityState.HasAbility(requiredAbility);

        private void Awake()
        {
            // 물리 설정이 잘못 저장돼도 보상 구체가 플레이어를 막지 않도록 Trigger를 강제한다.
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
            visualRenderer ??= GetComponentInChildren<Renderer>();
        }

        private void Start()
        {
            // 세이브 상태가 Awake 이후 복원돼도 첫 프레임에 이미 획득한 보상 표현을 숨긴다.
            RefreshPresentation();
        }

        public bool Configure(
            string id,
            int healthBonus,
            AbilityDefinition abilityRequirement,
            PlayerWorldState worldState,
            PlayerAbilityState abilityState,
            Health health,
            Renderer renderer)
        {
            string normalizedId = id?.Trim() ?? string.Empty;
            int normalizedBonus = Mathf.Max(1, healthBonus);
            bool changed = rewardId != normalizedId
                || maximumHealthBonus != normalizedBonus
                || requiredAbility != abilityRequirement
                || playerWorldState != worldState
                || playerAbilityState != abilityState
                || playerHealth != health
                || visualRenderer != renderer;

            rewardId = normalizedId;
            maximumHealthBonus = normalizedBonus;
            requiredAbility = abilityRequirement;
            playerWorldState = worldState;
            playerAbilityState = abilityState;
            playerHealth = health;
            visualRenderer = renderer;
            triggerCollider ??= GetComponent<Collider>();
            if (triggerCollider != null)
            {
                // 에디터 빌더가 Awake 전에 구성해도 이동을 막지 않는 계약을 동일하게 보장한다.
                triggerCollider.isTrigger = true;
            }

            RefreshPresentation();
            return changed;
        }

        public bool Collect()
        {
            // 필수 진행·효과 참조와 최대 체력 범위를 먼저 검증해 부분 적용을 막는다.
            if (IsCollected
                || string.IsNullOrWhiteSpace(rewardId)
                || maximumHealthBonus <= 0
                || playerWorldState == null
                || playerAbilityState == null
                || playerHealth == null
                || !IsRequirementMet
                || playerHealth.IsDead
                || playerHealth.MaxHealth
                    > int.MaxValue - maximumHealthBonus)
            {
                return false;
            }

            // 영구 ID를 먼저 등록해 같은 프레임의 중복 Trigger 콜백이 효과를 두 번 적용하지 못하게 한다.
            if (!playerWorldState.TryCollectReward(rewardId))
            {
                RefreshPresentation();
                return false;
            }

            if (!playerHealth.TryIncreaseMaximum(
                maximumHealthBonus))
            {
                // 앞선 범위·사망 검증으로 정상 플레이에서는 도달하지 않는 방어 분기다.
                return false;
            }

            IsCollected = true;
            SetPresentation(false);
            Debug.Log(
                $"백트래킹 보상 획득: 최대 체력 +{maximumHealthBonus} ({rewardId})",
                this);
            return true;
        }

        public bool RefreshPresentation()
        {
            // 저장 복원으로 이미 수집된 ID라면 효과를 재적용하지 않고 월드 표현만 숨긴다.
            IsCollected = playerWorldState != null
                && playerWorldState.IsRewardCollected(rewardId);
            SetPresentation(!IsCollected);
            return IsCollected;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 구성된 플레이어의 루트나 자식 Collider만 보상 획득 요청으로 인정한다.
            PlayerWorldState enteredState =
                other.GetComponentInParent<PlayerWorldState>();
            if (enteredState == playerWorldState)
            {
                Collect();
            }
        }

        private void SetPresentation(bool visible)
        {
            // 보이지 않는 보상이 중복 접촉하지 않도록 Collider와 Renderer를 함께 전환한다.
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
