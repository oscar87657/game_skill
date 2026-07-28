// GOLDEN STANDARD
// 목적: 하나의 능력 정의와 HUD 이미지·문구를 묶어 보유 상태를 표현한다.
// 책임: 능력 ID 매칭, 잠금·해금 색상과 레이블 갱신, 직렬화 참조 조회를 제공한다.
// 불변식: 슬롯은 능력을 변경하지 않고 PlayerAbilityState에서 계산된 보유 여부만 표시한다.
// 선택 이유: 슬롯 표현을 데이터 객체로 분리하면 HUD 본체의 능력 수와 배치가 코드 분기 없이 늘어난다.
using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameSkill
{
    [Serializable]
    public sealed class AbilityHudSlot
    {
        [SerializeField] private AbilityDefinition ability;
        [SerializeField] private string unlockedLabel;
        [SerializeField] private Image background;
        [SerializeField] private Text label;

        public AbilityDefinition Ability => ability;
        public string UnlockedLabel => unlockedLabel;
        public Image Background => background;
        public Text Label => label;
        public bool IsUnlocked { get; private set; }
        public bool IsConfigured =>
            ability != null
            && ability.IsConfigured;

        public AbilityHudSlot(
            AbilityDefinition abilityDefinition,
            string displayLabel,
            Image backgroundImage,
            Text labelText)
        {
            // 빌더가 만든 에셋·View 참조를 한 슬롯의 직렬화 데이터로 고정한다.
            ability = abilityDefinition;
            unlockedLabel =
                string.IsNullOrWhiteSpace(displayLabel)
                    ? abilityDefinition?.DisplayName
                        ?? string.Empty
                    : displayLabel.Trim();
            background = backgroundImage;
            label = labelText;
        }

        public void Apply(bool isUnlocked)
        {
            // 같은 상태를 다시 받아도 View 전체를 결정적으로 덮어써 복원 순서 차이를 없앤다.
            IsUnlocked = isUnlocked;
            if (background != null)
            {
                background.color = isUnlocked
                    ? new Color(
                        0.12f,
                        0.72f,
                        0.58f,
                        0.92f)
                    : new Color(
                        0.12f,
                        0.14f,
                        0.19f,
                        0.92f);
            }

            if (label != null)
            {
                label.text = isUnlocked
                    ? unlockedLabel
                    : "?";
                label.color = isUnlocked
                    ? Color.white
                    : new Color(
                        0.55f,
                        0.58f,
                        0.65f,
                        1f);
            }
        }

        public bool Matches(string abilityId)
        {
            // 테스트와 다른 UI가 ScriptableObject 참조 없이 영구 ID로 슬롯을 찾게 한다.
            return IsConfigured
                && !string.IsNullOrWhiteSpace(abilityId)
                && string.Equals(
                    ability.Id,
                    abilityId.Trim(),
                    StringComparison.Ordinal);
        }
    }
}
