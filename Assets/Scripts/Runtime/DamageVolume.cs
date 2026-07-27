// GOLDEN STANDARD
// 목적: 월드 Trigger 접촉을 Health 데미지로 변환해 사망·재시작 흐름을 시연한다.
// 책임: 유효한 Health 대상을 찾고 대시 무적을 존중한 뒤 설정된 데미지를 한 번 적용한다.
// 불변식: 무적 상태와 Health가 없는 객체에는 데미지를 주지 않으며 체력 상태를 직접 수정하지 않는다.
// 선택 이유: 환경 위험과 Health를 작은 계약으로 연결해 적 공격이나 낙사 판정으로 쉽게 교체한다.
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DamageVolume : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 99;

        private void Awake()
        {
            // 위험 지대가 물리 벽으로 동작하지 않도록 런타임에도 Trigger 설정을 보장한다.
            GetComponent<Collider>().isTrigger = true;
        }

        public void Configure(int damageAmount)
        {
            // 0 이하 데미지는 시연 흐름을 만들 수 없으므로 최소 1로 제한한다.
            damage = Mathf.Max(1, damageAmount);

            Collider damageCollider = GetComponent<Collider>();
            if (damageCollider != null)
            {
                damageCollider.isTrigger = true;
            }
        }

        public bool TryApply(GameObject targetObject)
        {
            // 테스트와 실제 Trigger가 같은 데미지 경로를 사용하도록 공개 함수로 분리한다.
            if (targetObject == null)
            {
                return false;
            }

            Health targetHealth =
                targetObject.GetComponentInParent<Health>();
            if (targetHealth == null || targetHealth.IsDead)
            {
                return false;
            }

            SideScrollerMotor targetMotor =
                targetObject.GetComponentInParent<SideScrollerMotor>();
            if (targetMotor != null && targetMotor.IsInvulnerable)
            {
                // 대시 무적은 환경 위험에도 같은 규칙으로 적용해 전투 규칙의 일관성을 지킨다.
                return false;
            }

            return targetHealth.TakeDamage(damage);
        }

        private void OnTriggerEnter(Collider other)
        {
            // 물리 콜백은 대상 판별을 직접 구현하지 않고 검증 가능한 공개 경로에 위임한다.
            TryApply(other.gameObject);
        }
    }
}
