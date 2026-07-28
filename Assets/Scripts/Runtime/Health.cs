// GOLDEN STANDARD
// 목적: 한 객체의 체력 상태를 소유하고 데미지·회복·사망 이벤트를 제공한다.
// 책임: 값을 제한하고 데미지·회복·최대 체력 증가 결과를 구독자에게 알린다.
// 불변식: CurrentHealth는 항상 0 이상 MaxHealth 이하이며, UI와 전투 규칙은 알지 않는다.
// 선택 이유: 작은 이벤트 기반 컴포넌트로 적·플레이어·UI를 서로 교체 가능하게 한다.
using System;
using UnityEngine;

namespace GameSkill
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 3;

        public event Action<int, int> Damaged;
        public event Action<int, int> Restored;
        public event Action Died;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        private void Awake()
        {
            // 프리팹 기본값에 의존하지 않고 직렬화된 최대 체력으로 런타임 상태를 초기화한다.
            RestoreFullHealth();
        }

        public void Configure(int maximumHealth)
        {
            // 외부 설정으로 사용할 수 없는 0 체력 대상이 생기지 않도록 값을 제한한다.
            maxHealth = Mathf.Max(1, maximumHealth);
            RestoreFullHealth();
        }

        public bool TryIncreaseMaximum(int amount)
        {
            // 양수가 아니거나 사망한 대상의 최대 체력은 보상 획득으로 변경하지 않는다.
            if (amount <= 0
                || IsDead
                || maxHealth > int.MaxValue - amount)
            {
                return false;
            }

            maxHealth += amount;
            CurrentHealth = Mathf.Min(
                MaxHealth,
                CurrentHealth + amount);
            // 최대치와 현재치가 함께 변했으므로 기존 회복 이벤트로 UI에 두 값을 다시 전달한다.
            Restored?.Invoke(CurrentHealth, MaxHealth);
            return true;
        }

        public bool TakeDamage(int amount)
        {
            // 잘못된 데미지나 사망 후 데미지는 무시하여 중복 피격 콜백에도 안전하게 만든다.
            if (amount <= 0 || IsDead)
            {
                return false;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            Damaged?.Invoke(CurrentHealth, MaxHealth);

            if (IsDead)
            {
                Died?.Invoke();
            }

            return true;
        }

        public void RestoreFullHealth()
        {
            // 회복과 부활이 서로 다른 로직으로 갈라지지 않도록 하나의 초기화 경로를 사용한다.
            CurrentHealth = MaxHealth;
            Restored?.Invoke(CurrentHealth, MaxHealth);
        }
    }
}
