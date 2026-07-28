// GOLDEN STANDARD
// 목적: 플레이 중 필요한 체력·능력·저장 상태를 하나의 이벤트 기반 HUD로 표시한다.
// 책임: 체력 바, 능력 슬롯, 저장·불러오기 버튼과 결과 메시지를 구독·갱신한다.
// 불변식: HUD는 진행 규칙을 직접 소유하지 않고 상태 컴포넌트와 저장 제어기의 공개 API만 사용한다.
// 선택 이유: 이벤트 기반 Presenter는 매 프레임 폴링하지 않으며 최종 UI 아트로 View를 교체하기 쉽다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameSkill
{
    [DisallowMultipleComponent]
    public sealed class GameProgressHud : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField]
        private PlayerAbilityState abilityState;
        [SerializeField]
        private GameProgressSaveController saveController;
        [SerializeField] private Image healthFill;
        [SerializeField] private Text healthLabel;
        [SerializeField]
        private List<AbilityHudSlot> abilitySlots = new();
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Text saveStatusLabel;

        private bool isSubscribed;

        public int AbilitySlotCount =>
            abilitySlots.Count;
        public string HealthLabelText =>
            healthLabel != null
                ? healthLabel.text
                : string.Empty;
        public string SaveStatusText =>
            saveStatusLabel != null
                ? saveStatusLabel.text
                : string.Empty;

        private void OnEnable()
        {
            // 활성 HUD만 상태와 버튼 이벤트를 구독해 비활성 UI의 중복 호출을 막는다.
            Subscribe();
            RefreshAll();
        }

        private void Start()
        {
            // 모든 플레이어 Awake 이후 한 번 더 그려 초기 체력과 능력 상태를 확정한다.
            RefreshAll();
        }

        private void OnDisable()
        {
            // Canvas가 사라진 뒤에도 저장 버튼이나 상태 이벤트가 HUD를 호출하지 않게 해제한다.
            Unsubscribe();
        }

        public bool Configure(
            Health playerHealth,
            PlayerAbilityState playerAbilityState,
            GameProgressSaveController progressSaveController,
            Image healthFillImage,
            Text healthText,
            IEnumerable<AbilityHudSlot> slots,
            Button saveActionButton,
            Button loadActionButton,
            Text statusText)
        {
            var requestedSlots =
                new List<AbilityHudSlot>();
            if (slots != null)
            {
                // 호출자가 소유한 임시 목록과 Inspector 직렬화 목록을 분리한다.
                foreach (AbilityHudSlot slot in slots)
                {
                    if (slot != null)
                    {
                        requestedSlots.Add(slot);
                    }
                }
            }

            bool changed = health != playerHealth
                || abilityState != playerAbilityState
                || saveController
                    != progressSaveController
                || healthFill != healthFillImage
                || healthLabel != healthText
                || saveButton != saveActionButton
                || loadButton != loadActionButton
                || saveStatusLabel != statusText
                || !SlotsMatch(
                    abilitySlots,
                    requestedSlots);

            Unsubscribe();
            health = playerHealth;
            abilityState = playerAbilityState;
            saveController = progressSaveController;
            healthFill = healthFillImage;
            healthLabel = healthText;
            abilitySlots.Clear();
            abilitySlots.AddRange(requestedSlots);
            saveButton = saveActionButton;
            loadButton = loadActionButton;
            saveStatusLabel = statusText;
            Subscribe();
            RefreshAll();
            return changed;
        }

        public void RefreshAll()
        {
            // 수동 새로고침도 이벤트 경로와 같은 두 표현 함수를 사용해 결과 차이를 막는다.
            RefreshHealth();
            RefreshAbilities();
        }

        public bool TrySave()
        {
            // 실제 파일 쓰기는 저장 제어기에 위임하고 HUD는 성공 여부만 사용자에게 표시한다.
            bool succeeded =
                saveController != null
                && saveController.SaveNow();
            SetSaveStatus(
                succeeded ? "SAVED" : "SAVE FAILED",
                succeeded);
            return succeeded;
        }

        public bool TryLoad()
        {
            // 완전히 검증된 JSON만 적용하는 저장 제어기의 결과를 받아 모든 HUD 상태를 다시 그린다.
            bool succeeded =
                saveController != null
                && saveController.LoadNow();
            SetSaveStatus(
                succeeded ? "LOADED" : "NO SAVE",
                succeeded);
            RefreshAll();
            return succeeded;
        }

        public bool IsAbilityUnlocked(
            string abilityId)
        {
            // 테스트와 보조 UI가 ID로 현재 슬롯 표현을 조회하도록 선형 탐색한다.
            foreach (AbilityHudSlot slot
                in abilitySlots)
            {
                if (slot != null
                    && slot.Matches(abilityId))
                {
                    return slot.IsUnlocked;
                }
            }

            return false;
        }

        private void RefreshHealth()
        {
            // 상태 참조가 없으면 0으로 표시해 오래된 체력 값을 화면에 남기지 않는다.
            int current = health != null
                ? health.CurrentHealth
                : 0;
            int maximum = health != null
                ? health.MaxHealth
                : 0;
            if (healthFill != null)
            {
                healthFill.fillAmount =
                    maximum > 0
                        ? Mathf.Clamp01(
                            (float)current / maximum)
                        : 0f;
            }

            if (healthLabel != null)
            {
                healthLabel.text =
                    $"HP {current} / {maximum}";
            }
        }

        private void RefreshAbilities()
        {
            // 각 슬롯은 자신의 정의만 조회해 능력 개수가 늘어도 HUD 본체에 조건문을 추가하지 않는다.
            foreach (AbilityHudSlot slot
                in abilitySlots)
            {
                if (slot == null)
                {
                    continue;
                }

                bool isUnlocked =
                    abilityState != null
                    && slot.Ability != null
                    && abilityState.HasAbility(
                        slot.Ability);
                slot.Apply(isUnlocked);
            }
        }

        private void HandleHealthChanged(
            int current,
            int maximum)
        {
            // 이벤트 인자는 상태와 같은 값이지만 단일 표시 함수로 다시 읽어 최대 체력 변경도 함께 반영한다.
            RefreshHealth();
        }

        private void HandleAbilityStateChanged()
        {
            // 개별 해금과 세이브 전체 복원 모두 같은 슬롯 갱신 경로를 사용한다.
            RefreshAbilities();
        }

        private void HandleSaveClicked()
        {
            // Button UnityEvent를 공개 저장 API로 변환해 파일 입출력 구현을 View에서 분리한다.
            TrySave();
        }

        private void HandleLoadClicked()
        {
            // Button UnityEvent를 검증된 불러오기 API로 전달한다.
            TryLoad();
        }

        private void SetSaveStatus(
            string message,
            bool succeeded)
        {
            // 성공과 실패를 문구뿐 아니라 색으로도 구분해 작은 HUD에서 빠르게 읽게 한다.
            if (saveStatusLabel == null)
            {
                return;
            }

            saveStatusLabel.text = message;
            saveStatusLabel.color = succeeded
                ? new Color(
                    0.2f,
                    0.95f,
                    0.72f,
                    1f)
                : new Color(
                    1f,
                    0.42f,
                    0.32f,
                    1f);
        }

        private void Subscribe()
        {
            // Configure와 OnEnable이 연속 호출돼도 상태·버튼 이벤트는 한 번만 연결한다.
            if (isSubscribed)
            {
                return;
            }

            if (health != null)
            {
                health.Damaged +=
                    HandleHealthChanged;
                health.Restored +=
                    HandleHealthChanged;
            }

            if (abilityState != null)
            {
                abilityState.AbilityStateChanged +=
                    HandleAbilityStateChanged;
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(
                    HandleSaveClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(
                    HandleLoadClicked);
            }

            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            // 구독 여부를 먼저 확인해 부분 구성된 HUD도 안전하게 재설정한다.
            if (!isSubscribed)
            {
                return;
            }

            if (health != null)
            {
                health.Damaged -=
                    HandleHealthChanged;
                health.Restored -=
                    HandleHealthChanged;
            }

            if (abilityState != null)
            {
                abilityState.AbilityStateChanged -=
                    HandleAbilityStateChanged;
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(
                    HandleSaveClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(
                    HandleLoadClicked);
            }

            isSubscribed = false;
        }

        private static bool SlotsMatch(
            IReadOnlyList<AbilityHudSlot> current,
            IReadOnlyList<AbilityHudSlot> requested)
        {
            // 슬롯 수가 다르면 같은 HUD 데이터 구성이 될 수 없으므로 즉시 실패한다.
            if (current.Count != requested.Count)
            {
                return false;
            }

            // 능력 정의·표시 문구·View 참조를 순서대로 비교해 불필요한 Scene Dirty를 막는다.
            for (int index = 0;
                 index < current.Count;
                 index++)
            {
                AbilityHudSlot currentSlot =
                    current[index];
                AbilityHudSlot requestedSlot =
                    requested[index];
                if (currentSlot == null
                    || requestedSlot == null
                    || currentSlot.Ability
                        != requestedSlot.Ability
                    || currentSlot.UnlockedLabel
                        != requestedSlot.UnlockedLabel
                    || currentSlot.Background
                        != requestedSlot.Background
                    || currentSlot.Label
                        != requestedSlot.Label)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
