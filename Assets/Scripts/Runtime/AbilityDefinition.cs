// GOLDEN STANDARD
// 목적: 메트로바니아 진행에 사용되는 능력의 변하지 않는 정의를 에셋으로 제공한다.
// 책임: 저장에 사용할 고유 ID와 화면 표시용 이름·설명을 검증해 노출한다.
// 불변식: 유효한 능력은 공백이 아닌 ID를 가지며 런타임 보유 상태를 직접 저장하지 않는다.
// 선택 이유: ScriptableObject 정의와 플레이어 상태를 분리하면 같은 능력을 픽업·게이트·UI가 공유할 수 있다.
using UnityEngine;

namespace GameSkill
{
    [CreateAssetMenu(
        fileName = "Ability_",
        menuName = "Game Skill/Ability Definition")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        [SerializeField] private string abilityId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;

        public string Id => abilityId;
        public string DisplayName => displayName;
        public string Description => description;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(abilityId);

        public bool Configure(
            string id,
            string abilityDisplayName,
            string abilityDescription)
        {
            // 저장 키가 될 수 없는 빈 ID는 에셋 생성 단계에서부터 거부한다.
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            abilityId = id.Trim();
            displayName = string.IsNullOrWhiteSpace(abilityDisplayName)
                ? abilityId
                : abilityDisplayName.Trim();
            description = abilityDescription?.Trim() ?? string.Empty;
            return true;
        }

        private void OnValidate()
        {
            // Inspector에서 붙은 앞뒤 공백이 서로 다른 저장 키를 만들지 않도록 정규화한다.
            abilityId = abilityId?.Trim() ?? string.Empty;
            displayName = displayName?.Trim() ?? string.Empty;
            description = description?.Trim() ?? string.Empty;
        }
    }
}
