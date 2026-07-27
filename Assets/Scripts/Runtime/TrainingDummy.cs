// GOLDEN STANDARD
// 목적: 시연과 회귀 테스트에 사용할 결정적인 전투 대상을 제공한다.
// 책임: Health 이벤트를 구독하고 피격 반응과 사망 후 부활을 보여준다.
// 불변식: OnEnable과 OnDisable의 이벤트 구독 쌍을 지켜 재활성화 중 중복 콜백을 막는다.
// 선택 이유: Coroutine 기반 표현으로 Health가 시각 피드백 정책을 알지 않게 한다.
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
            // 필요한 컴포넌트를 캐시하고 반응 후 복원할 원래 크기를 저장한다.
            health = GetComponent<Health>();
            targetCollider = GetComponent<Collider>();
            initialScale = transform.localScale;
        }

        private void OnEnable()
        {
            // 활성화된 동안만 구독하여 비활성 더미가 오래된 이벤트에 반응하지 않게 한다.
            health ??= GetComponent<Health>();
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            // 재활성화 후 중복 반응을 막기 위해 항상 리스너를 해제한다.
            if (health == null)
            {
                return;
            }

            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }

        private void HandleDamaged(int currentHealth, int maxHealth)
        {
            // 시각 결과를 결정적으로 만들기 위해 기존 반응을 멈추고 최신 반응을 시작한다.
            StopAllCoroutines();
            StartCoroutine(PlayHitReaction());
        }

        private void HandleDied()
        {
            // 부활 순서는 더미가 소유하며 Health는 씬 표현을 알지 못한다.
            StopAllCoroutines();
            StartCoroutine(Respawn());
        }

        private IEnumerator PlayHitReaction()
        {
            // 잠시 스쿼시·스트레치를 적용한 뒤 정확히 원래 크기로 복원한다.
            transform.localScale = new Vector3(
                initialScale.x * 1.15f,
                initialScale.y * 0.85f,
                initialScale.z * 1.15f);
            yield return new WaitForSeconds(hitReactionDuration);
            transform.localScale = initialScale;
        }

        private IEnumerator Respawn()
        {
            // 숨겨진 동안 충돌을 끄어 보이지 않는 대상이 피격되지 않게 한다.
            targetCollider.enabled = false;
            transform.localScale = Vector3.zero;
            yield return new WaitForSeconds(respawnDelay);
            health.RestoreFullHealth();
            transform.localScale = initialScale;
            targetCollider.enabled = true;
        }
    }
}
