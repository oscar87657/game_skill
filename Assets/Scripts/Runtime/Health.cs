using System;
using UnityEngine;

namespace GameSkill
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 3;

        public event Action<int, int> Damaged;
        public event Action Died;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        private void Awake()
        {
            RestoreFullHealth();
        }

        public void Configure(int maximumHealth)
        {
            maxHealth = Mathf.Max(1, maximumHealth);
            RestoreFullHealth();
        }

        public bool TakeDamage(int amount)
        {
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
            CurrentHealth = MaxHealth;
        }
    }
}
