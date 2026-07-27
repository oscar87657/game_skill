// GOLDEN STANDARD
// 목적: 플레이어가 발견한 월드 구역의 방문 상태를 단일 런타임 원본으로 관리한다.
// 책임: 구역 ID 방문·조회·초기화와 최초 방문 이벤트를 제공한다.
// 불변식: 같은 구역 ID는 한 번만 기록하며 중복 진입은 상태와 이벤트를 변경하지 않는다.
// 선택 이유: HashSet 기반 ID 상태는 지도 표시와 저장 데이터 직렬화에 동일한 계약을 제공한다.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    public sealed class PlayerWorldState : MonoBehaviour
    {
        [SerializeField]
        private List<WorldZoneDefinition> initiallyVisitedZones = new();

        private readonly HashSet<string> visitedZoneIds =
            new(StringComparer.Ordinal);

        public event Action<WorldZoneDefinition> ZoneVisited;

        public int VisitedCount => visitedZoneIds.Count;

        private void Awake()
        {
            // 씬에서 지정한 초기 방문 목록을 빠른 조회용 집합으로 한 번 변환한다.
            RebuildInitialState();
        }

        public bool HasVisited(WorldZoneDefinition zone)
        {
            // 유효하지 않은 정의는 발견한 구역으로 취급하지 않는다.
            if (zone == null || !zone.IsConfigured)
            {
                return false;
            }

            return visitedZoneIds.Contains(zone.Id);
        }

        public bool HasVisitedId(string zoneId)
        {
            // 저장 데이터나 UI에서 전달한 빈 ID는 조회하지 않고 즉시 실패한다.
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                return false;
            }

            return visitedZoneIds.Contains(zoneId.Trim());
        }

        public bool TryVisit(WorldZoneDefinition zone)
        {
            // 영구 ID가 없는 구역은 지도와 저장에서 복원할 수 없으므로 기록하지 않는다.
            if (zone == null || !zone.IsConfigured)
            {
                return false;
            }

            // HashSet.Add의 결과로 최초 진입과 중복 진입을 한곳에서 구분한다.
            if (!visitedZoneIds.Add(zone.Id))
            {
                return false;
            }

            ZoneVisited?.Invoke(zone);
            return true;
        }

        public void RebuildInitialState()
        {
            // 씬 재시작과 테스트 초기화가 이전 런타임 방문 기록을 남기지 않게 먼저 비운다.
            visitedZoneIds.Clear();

            // 직렬화 목록의 null과 중복을 검증하며 유효한 영구 ID만 집합에 넣는다.
            foreach (WorldZoneDefinition zone in initiallyVisitedZones)
            {
                if (zone != null && zone.IsConfigured)
                {
                    visitedZoneIds.Add(zone.Id);
                }
            }
        }

        public void ConfigureInitialZones(
            IEnumerable<WorldZoneDefinition> zones)
        {
            // 호출자가 전달한 목록과 런타임 상태가 같은 컬렉션을 공유하지 않게 복사한다.
            initiallyVisitedZones.Clear();
            if (zones != null)
            {
                // 각 정의는 RebuildInitialState에서 다시 유효성을 검사한다.
                foreach (WorldZoneDefinition zone in zones)
                {
                    initiallyVisitedZones.Add(zone);
                }
            }

            RebuildInitialState();
        }
    }
}
