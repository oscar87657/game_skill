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
            health = GetComponent<Health>();
            targetCollider = GetComponent<Collider>();
            initialScale = transform.localScale;
        }

        private void OnEnable()
        {
            health ??= GetComponent<Health>();
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }

        private void HandleDamaged(int currentHealth, int maxHealth)
        {
            StopAllCoroutines();
            StartCoroutine(PlayHitReaction());
        }

        private void HandleDied()
        {
            StopAllCoroutines();
            StartCoroutine(Respawn());
        }

        private IEnumerator PlayHitReaction()
        {
            transform.localScale = new Vector3(
                initialScale.x * 1.15f,
                initialScale.y * 0.85f,
                initialScale.z * 1.15f);
            yield return new WaitForSeconds(hitReactionDuration);
            transform.localScale = initialScale;
        }

        private IEnumerator Respawn()
        {
            targetCollider.enabled = false;
            transform.localScale = Vector3.zero;
            yield return new WaitForSeconds(respawnDelay);
            health.RestoreFullHealth();
            transform.localScale = initialScale;
            targetCollider.enabled = true;
        }
    }
}
