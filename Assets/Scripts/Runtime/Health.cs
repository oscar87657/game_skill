// GOLDEN STANDARD
// Purpose: Own one object's health state and expose stable damage/death events.
// Responsibility: Clamp values, reject invalid damage, and notify observers.
// Invariant: CurrentHealth is always in [0, MaxHealth]; this class does not know UI or combat rules.
// Design choice: A small event-driven component keeps enemies, players, and UI replaceable.
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
            // Initialize runtime state from the serialized maximum rather than relying on prefab defaults.
            RestoreFullHealth();
        }

        public void Configure(int maximumHealth)
        {
            // Configuration is clamped so external designers cannot create an unusable zero-health target.
            maxHealth = Mathf.Max(1, maximumHealth);
            RestoreFullHealth();
        }

        public bool TakeDamage(int amount)
        {
            // Invalid or post-mortem damage is ignored, making repeated hit callbacks safe.
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
            // A single reset path prevents respawn and checkpoint code from drifting apart.
            CurrentHealth = MaxHealth;
        }
    }
}
