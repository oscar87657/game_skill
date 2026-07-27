// GOLDEN STANDARD
// 목적: 월드 구역의 영구 ID와 표시 이름을 재사용 가능한 데이터로 정의한다.
// 책임: 구역 ID·표시 이름·설명을 보관하고 유효한 설정만 허용한다.
// 불변식: 유효한 구역은 공백이 아닌 영구 ID를 가지며 런타임 오브젝트를 직접 참조하지 않는다.
// 선택 이유: ScriptableObject 정의를 지도·세이브·씬 로딩이 공유하면 문자열 중복과 참조 결합을 줄일 수 있다.
using UnityEngine;

namespace GameSkill
{
    [CreateAssetMenu(
        fileName = "WorldZone_",
        menuName = "Game Skill/World Zone Definition")]
    public sealed class WorldZoneDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(id);

        public bool Configure(
            string zoneId,
            string zoneDisplayName,
            string zoneDescription)
        {
            // 영구 저장 키로 사용할 수 없는 빈 ID는 에셋을 유효 상태로 만들지 않는다.
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                return false;
            }

            id = zoneId.Trim();
            displayName = string.IsNullOrWhiteSpace(zoneDisplayName)
                ? id
                : zoneDisplayName.Trim();
            description = zoneDescription?.Trim() ?? string.Empty;
            return true;
        }
    }
}
