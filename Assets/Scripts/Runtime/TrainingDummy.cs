// GOLDEN STANDARD
// Purpose: Provide a deterministic combat target for demos and regression checks.
// Responsibility: Subscribe to Health events, show a hit reaction, and respawn after death.
// Invariant: Event subscriptions are paired in OnEnable/OnDisable; no duplicate callbacks survive reactivation.
// Design choice: Coroutine-based presentation keeps Health independent from visual feedback policy.
using System.Collections;
using UnityEngine;

namespace GameSkill
{
    [RequireComponent(typeof(Health))]
    public sealed class TrainingDummy : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float respawnDelay = 1.5f;
        [SerializeField, Min(0f)] private float hitReactionDuration = 0.08f;

        private Health health;
        private Collider targetCollider;
        private Vector3 initialScale;

        private void Awake()
        {
            // Cache required components and remember the authored scale for reversible reactions.
            health = GetComponent<Health>();
            targetCollider = GetComponent<Collider>();
            initialScale = transform.localScale;
        }

        private void OnEnable()
        {
            // Subscribe only while active so disabled dummies cannot react to stale events.
            health ??= GetComponent<Health>();
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            // Always detach listeners to prevent duplicate reactions after re-enable.
            if (health == null)
            {
                return;
            }

            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }

        private void HandleDamaged(int currentHealth, int maxHealth)
        {
            // Stop an overlapping reaction before starting the newest one for deterministic visuals.
            StopAllCoroutines();
            StartCoroutine(PlayHitReaction());
        }

        private void HandleDied()
        {
            // Death owns the respawn sequence; Health remains unaware of scene presentation.
            StopAllCoroutines();
            StartCoroutine(Respawn());
        }

        private IEnumerator PlayHitReaction()
        {
            // Temporarily squash and stretch, then restore the exact original scale.
            transform.localScale = new Vector3(
                initialScale.x * 1.15f,
                initialScale.y * 0.85f,
                initialScale.z * 1.15f);
            yield return new WaitForSeconds(hitReactionDuration);
            transform.localScale = initialScale;
        }

        private IEnumerator Respawn()
        {
            // Disable collision while hidden so the dummy cannot receive invisible hits.
            targetCollider.enabled = false;
            transform.localScale = Vector3.zero;
            yield return new WaitForSeconds(respawnDelay);
            health.RestoreFullHealth();
            transform.localScale = initialScale;
            targetCollider.enabled = true;
        }
    }
}
