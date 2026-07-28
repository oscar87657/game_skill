// GOLDEN STANDARD
// 목적: 하나의 월드 구역과 허용되는 카메라 중심점 범위를 직렬화 가능한 데이터로 연결한다.
// 책임: 구역 정의·최소·최대 중심점을 보관하고 ID 일치와 위치 제한을 제공한다.
// 불변식: 유효한 바인딩은 영구 ID가 있는 구역을 가지며 위치 계산에서 Z 깊이를 변경하지 않는다.
// 선택 이유: 구역별 값을 카메라 코드의 조건문 대신 데이터 목록으로 두면 새 방과 튜닝값을 코드 수정 없이 추가할 수 있다.
using System;
using UnityEngine;

namespace GameSkill
{
    [Serializable]
    public sealed class CameraZoneBounds
    {
        [SerializeField] private WorldZoneDefinition zone;
        [SerializeField] private Vector2 minimumCenter;
        [SerializeField] private Vector2 maximumCenter;

        public WorldZoneDefinition Zone => zone;
        public Vector2 MinimumCenter => minimumCenter;
        public Vector2 MaximumCenter => maximumCenter;
        public bool IsConfigured =>
            zone != null && zone.IsConfigured;

        public CameraZoneBounds(
            WorldZoneDefinition zoneDefinition,
            Vector2 minimum,
            Vector2 maximum)
        {
            // 런타임 테스트와 에디터 빌더가 같은 설정 경로를 사용하도록 생성자에서 구성한다.
            Configure(zoneDefinition, minimum, maximum);
        }

        public bool Configure(
            WorldZoneDefinition zoneDefinition,
            Vector2 minimum,
            Vector2 maximum)
        {
            bool changed = zone != zoneDefinition
                || minimumCenter != minimum
                || maximumCenter != maximum;
            zone = zoneDefinition;
            minimumCenter = minimum;
            maximumCenter = maximum;
            return changed;
        }

        public bool Matches(WorldZoneDefinition candidate)
        {
            // 서로 다른 ScriptableObject라도 영구 ID가 같으면 같은 구역 경계로 처리한다.
            if (!IsConfigured
                || candidate == null
                || !candidate.IsConfigured)
            {
                return false;
            }

            return string.Equals(
                zone.Id,
                candidate.Id,
                StringComparison.Ordinal);
        }

        public Vector3 Constrain(Vector3 desiredPosition)
        {
            // 실제 제한 계산은 순수 함수에 위임해 모든 호출자가 같은 최소·최대 정규화를 사용한다.
            return CameraBoundsMath.ClampCenter(
                desiredPosition,
                minimumCenter,
                maximumCenter);
        }
    }
}
