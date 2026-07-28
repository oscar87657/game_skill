// GOLDEN STANDARD
// 목적: 플레이어의 물리적 구역 진입을 영구 ID 기반 방문 상태로 변환한다.
// 책임: 구역 정의와 경계 여유 폭을 보관하고 플레이어 중심이 내부로 진입하면 월드 상태를 갱신한다.
// 불변식: 구역 볼륨은 이동을 막지 않으며 경계 완충 구간과 같은 현재 구역 접촉은 전환을 만들지 않는다.
// 선택 이유: 중심점 히스테리시스는 캡슐이 두 Trigger에 동시에 걸쳐도 구역과 카메라 판정이 흔들리지 않게 한다.
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class WorldZoneVolume : MonoBehaviour
    {
        [SerializeField] private WorldZoneDefinition zone;
        [SerializeField, Min(0f)]
        private float horizontalEntryInset = 0.45f;

        private Collider zoneCollider;

        public WorldZoneDefinition Zone => zone;
        public float HorizontalEntryInset =>
            horizontalEntryInset;

        private void Awake()
        {
            // 잘못 저장된 Collider도 플레이어를 막지 않는 Trigger 계약으로 복구한다.
            zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
        }

        public bool Configure(
            WorldZoneDefinition zoneDefinition,
            float entryInset = 0.45f)
        {
            // 정의가 같아도 잘못 저장된 Collider는 아래에서 Trigger 상태로 복구한다.
            zoneCollider ??= GetComponent<Collider>();
            if (zoneCollider != null)
            {
                // Awake 이전에 빌더나 테스트가 호출해도 물리 통과 규칙을 보장한다.
                zoneCollider.isTrigger = true;
            }

            float normalizedInset = Mathf.Max(0f, entryInset);
            // 같은 정의와 여유 폭을 다시 넣을 때 에디터 씬을 불필요하게 변경하지 않는다.
            if (zone == zoneDefinition
                && Mathf.Approximately(
                    horizontalEntryInset,
                    normalizedInset))
            {
                return false;
            }

            zone = zoneDefinition;
            horizontalEntryInset = normalizedInset;
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

        public bool IsInsideEntryInterior(
            Vector3 worldPosition)
        {
            // Collider의 실제 월드 Bounds를 사용해 부모 크기와 Transform 스케일을 함께 반영한다.
            zoneCollider ??= GetComponent<Collider>();
            if (zoneCollider == null)
            {
                return false;
            }

            Bounds bounds = zoneCollider.bounds;
            return WorldZoneBoundaryMath
                .IsInsideHorizontalInterior(
                    worldPosition.x,
                    bounds.min.x,
                    bounds.max.x,
                    horizontalEntryInset);
        }

        private void OnTriggerEnter(Collider other)
        {
            // 최초 접촉에서도 캐릭터 중심이 완충 구간을 넘은 경우에만 새 구역을 확정한다.
            TryEnterFromCollider(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // 캡슐이 두 Trigger에 걸친 채 이동해도 중심이 내부 임계점을 넘는 프레임에 전환한다.
            TryEnterFromCollider(other);
        }

        private bool TryEnterFromCollider(Collider other)
        {
            // CharacterController 또는 자식 콜라이더가 닿아도 부모의 단일 월드 상태를 찾는다.
            PlayerWorldState worldState =
                other.GetComponentInParent<PlayerWorldState>();
            if (worldState == null
                || !IsInsideEntryInterior(
                    worldState.transform.position))
            {
                return false;
            }

            return Enter(worldState);
        }
    }
}
