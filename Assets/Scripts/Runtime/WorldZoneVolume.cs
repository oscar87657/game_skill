// GOLDEN STANDARD
// 목적: 플레이어의 물리적 구역 진입을 영구 ID 기반 방문 상태로 변환한다.
// 책임: 구역 정의를 보관하고 Trigger 진입 시 플레이어의 월드 상태를 갱신한다.
// 불변식: 구역 볼륨은 이동을 막지 않으며 같은 현재 구역의 중복 접촉은 전환을 만들지 않는다.
// 선택 이유: 물리 볼륨과 방문 상태를 분리하면 구역 크기나 씬 분할 방식이 바뀌어도 진행 데이터를 유지할 수 있다.
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class WorldZoneVolume : MonoBehaviour
    {
        [SerializeField] private WorldZoneDefinition zone;

        private Collider zoneCollider;

        public WorldZoneDefinition Zone => zone;

        private void Awake()
        {
            // 잘못 저장된 Collider도 플레이어를 막지 않는 Trigger 계약으로 복구한다.
            zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
        }

        public bool Configure(WorldZoneDefinition zoneDefinition)
        {
            // 정의가 같아도 잘못 저장된 Collider는 아래에서 Trigger 상태로 복구한다.
            zoneCollider ??= GetComponent<Collider>();
            if (zoneCollider != null)
            {
                // Awake 이전에 빌더나 테스트가 호출해도 물리 통과 규칙을 보장한다.
                zoneCollider.isTrigger = true;
            }

            // 같은 정의를 다시 넣을 때 에디터 씬을 불필요하게 변경하지 않는다.
            if (zone == zoneDefinition)
            {
                return false;
            }

            zone = zoneDefinition;
            return true;
        }

        public bool Enter(PlayerWorldState worldState)
        {
            // 필수 참조가 없거나 현재 구역 Trigger가 중복 호출되면 상태와 로그를 유지한다.
            if (worldState == null
                || zone == null
                || !zone.IsConfigured
                || !worldState.EnterZone(zone))
            {
                return false;
            }

            Debug.Log($"구역 진입: {zone.DisplayName} ({zone.Id})", this);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // CharacterController 또는 자식 콜라이더가 닿아도 부모의 단일 월드 상태를 찾는다.
            PlayerWorldState worldState =
                other.GetComponentInParent<PlayerWorldState>();
            if (worldState != null)
            {
                Enter(worldState);
            }
        }
    }
}
