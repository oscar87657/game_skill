// GOLDEN STANDARD
// 목적: 플레이어가 획득한 능력의 보유 상태를 단일 런타임 원본으로 관리한다.
// 책임: 초기 능력을 등록하고 고유 ID 기준 조회·해금·전체 복원·변경 이벤트를 제공한다.
// 불변식: 같은 능력 ID는 한 번만 보유하며 중복 해금은 상태와 이벤트를 변경하지 않는다.
// 선택 이유: HashSet 기반 상태는 빠른 조회를 제공하고 이후 저장 시스템이 ID 목록만 직렬화하게 한다.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    public sealed class PlayerAbilityState : MonoBehaviour
    {
        [SerializeField]
        private List<AbilityDefinition> initiallyUnlockedAbilities = new();

        private readonly HashSet<string> unlockedAbilityIds =
            new(StringComparer.Ordinal);

        public event Action<AbilityDefinition> AbilityUnlocked;
        public event Action AbilityStateChanged;

        public int UnlockedCount => unlockedAbilityIds.Count;

        private void Awake()
        {
            // 씬에서 시작 능력으로 지정한 에셋을 런타임 조회 집합으로 한 번 변환한다.
            RebuildInitialState();
        }

        public bool HasAbility(AbilityDefinition ability)
        {
            // 설정되지 않은 정의는 어떤 플레이어도 보유한 것으로 판단하지 않는다.
            if (ability == null || !ability.IsConfigured)
            {
                return false;
            }

            return unlockedAbilityIds.Contains(ability.Id);
        }

        public bool HasAbilityId(string abilityId)
        {
            // 저장 데이터나 테스트에서 들어온 빈 ID는 조회하지 않고 즉시 실패한다.
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            return unlockedAbilityIds.Contains(abilityId.Trim());
        }

        public bool TryUnlock(AbilityDefinition ability)
        {
            // 유효하지 않은 ScriptableObject는 저장 가능한 진행 상태를 만들 수 없으므로 거부한다.
            if (ability == null || !ability.IsConfigured)
            {
                return false;
            }

            // HashSet.Add의 반환값으로 중복 해금을 한곳에서 판별해 이벤트 중복도 막는다.
            if (!unlockedAbilityIds.Add(ability.Id))
            {
                return false;
            }

            AbilityUnlocked?.Invoke(ability);
            AbilityStateChanged?.Invoke();
            return true;
        }

        public void RebuildInitialState()
        {
            // 씬 재시작이나 테스트 재초기화가 이전 런타임 상태를 남기지 않도록 먼저 비운다.
            unlockedAbilityIds.Clear();

            // 직렬화 목록은 중복과 null을 포함할 수 있으므로 각 항목을 검증하며 집합에 넣는다.
            foreach (AbilityDefinition ability in initiallyUnlockedAbilities)
            {
                if (ability != null && ability.IsConfigured)
                {
                    unlockedAbilityIds.Add(ability.Id);
                }
            }

            AbilityStateChanged?.Invoke();
        }

        public void ConfigureInitialAbilities(
            IEnumerable<AbilityDefinition> abilities)
        {
            // 초기 능력 구성은 현재 런타임 상태도 함께 교체하므로 명시적인 설정 시점에만 호출한다.
            initiallyUnlockedAbilities.Clear();

            // 에디터 빌더가 전달한 목록을 복사해 호출자 컬렉션의 이후 변경과 상태를 분리한다.
            if (abilities != null)
            {
                foreach (AbilityDefinition ability in abilities)
                {
                    initiallyUnlockedAbilities.Add(ability);
                }
            }

            RebuildInitialState();
        }

        public List<string> CopyUnlockedAbilityIds()
        {
            // 저장 계층이 내부 HashSet을 수정하지 못하도록 정렬된 새 목록을 반환한다.
            var copiedIds =
                new List<string>(unlockedAbilityIds);
            copiedIds.Sort(StringComparer.Ordinal);
            return copiedIds;
        }

        public int RestoreUnlockedAbilities(
            IEnumerable<AbilityDefinition> knownAbilities,
            IEnumerable<string> savedAbilityIds)
        {
            // 저장 데이터 적용은 현재 보유 상태를 완전히 대체해 삭제된 세이브 슬롯도 정확히 반영한다.
            unlockedAbilityIds.Clear();
            var requestedIds =
                new HashSet<string>(StringComparer.Ordinal);
            if (savedAbilityIds != null)
            {
                // 공백과 중복 ID를 먼저 정규화해 에셋 탐색 결과가 입력 순서에 좌우되지 않게 한다.
                foreach (string savedId in savedAbilityIds)
                {
                    if (!string.IsNullOrWhiteSpace(savedId))
                    {
                        requestedIds.Add(savedId.Trim());
                    }
                }
            }

            if (knownAbilities != null)
            {
                // 현재 빌드에 존재하는 정의만 복원해 제거되거나 오타가 난 ID를 런타임 상태에 넣지 않는다.
                foreach (AbilityDefinition ability in knownAbilities)
                {
                    if (ability != null
                        && ability.IsConfigured
                        && requestedIds.Contains(ability.Id))
                    {
                        unlockedAbilityIds.Add(ability.Id);
                    }
                }
            }

            AbilityStateChanged?.Invoke();
            return unlockedAbilityIds.Count;
        }
    }
}
