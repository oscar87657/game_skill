// GOLDEN STANDARD
// 목적: 월드 구역 정의와 Additive Scene 경로의 데이터 연결을 표현한다.
// 책임: 구역·Scene 경로를 보관하고 유효성 검사와 ID 일치 조회를 제공한다.
// 불변식: 유효한 바인딩은 설정된 구역 영구 ID와 비어 있지 않은 Scene 경로를 함께 가진다.
// 선택 이유: 직렬화 가능한 작은 데이터 객체로 분리하면 스트리밍 제어와 에디터 배치를 독립적으로 테스트할 수 있다.
using System;
using UnityEngine;

namespace GameSkill
{
    [Serializable]
    public sealed class WorldZoneSceneBinding
    {
        [SerializeField] private WorldZoneDefinition zone;
        [SerializeField] private string scenePath;

        public WorldZoneDefinition Zone => zone;
        public string ScenePath => scenePath;
        public bool IsConfigured =>
            zone != null
            && zone.IsConfigured
            && !string.IsNullOrWhiteSpace(scenePath);

        public WorldZoneSceneBinding(
            WorldZoneDefinition zoneDefinition,
            string additiveScenePath)
        {
            // 생성 시 Configure와 같은 정규화 규칙을 적용해 런타임·에디터 경로를 통일한다.
            Configure(zoneDefinition, additiveScenePath);
        }

        public bool Configure(
            WorldZoneDefinition zoneDefinition,
            string additiveScenePath)
        {
            string normalizedPath =
                additiveScenePath?.Trim() ?? string.Empty;
            bool changed = zone != zoneDefinition
                || scenePath != normalizedPath;
            zone = zoneDefinition;
            scenePath = normalizedPath;
            return changed;
        }

        public bool Matches(string zoneId)
        {
            // 빈 조회 ID와 설정되지 않은 바인딩은 어떤 구역에도 일치시키지 않는다.
            if (!IsConfigured || string.IsNullOrWhiteSpace(zoneId))
            {
                return false;
            }

            return string.Equals(
                zone.Id,
                zoneId.Trim(),
                StringComparison.Ordinal);
        }
    }
}
