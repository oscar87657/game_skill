// GOLDEN STANDARD
// 목적: 플레이어의 구역 방문·현재 위치·영구 지름길·수집 보상을 단일 런타임 원본으로 관리한다.
// 책임: 구역 방문, 지름길 해금과 보상 ID의 조회·초기화·최초 변경 이벤트를 제공한다.
// 불변식: 같은 진행 ID는 한 번만 기록하며 중복 변경은 상태와 이벤트를 바꾸지 않는다.
// 선택 이유: HashSet 기반 ID 상태는 지도·월드 게이트·저장 데이터에 동일한 계약을 제공한다.
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
        [SerializeField]
        private List<string> initiallyUnlockedShortcutIds = new();
        [SerializeField]
        private List<string> initiallyCollectedRewardIds = new();

        private readonly HashSet<string> visitedZoneIds =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> unlockedShortcutIds =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> collectedRewardIds =
            new(StringComparer.Ordinal);

        public event Action<WorldZoneDefinition> ZoneVisited;
        public event Action<WorldZoneDefinition> ZoneEntered;
        public event Action<string> ShortcutUnlocked;
        public event Action<string> RewardCollected;

        public int VisitedCount => visitedZoneIds.Count;
        public int UnlockedShortcutCount => unlockedShortcutIds.Count;
        public int CollectedRewardCount => collectedRewardIds.Count;
        public WorldZoneDefinition CurrentZone { get; private set; }

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

        public bool EnterZone(WorldZoneDefinition zone)
        {
            // 유효하지 않거나 현재 구역과 같은 ID의 중복 Trigger 진입은 전환 이벤트를 만들지 않는다.
            if (zone == null
                || !zone.IsConfigured
                || (CurrentZone != null
                    && string.Equals(
                        CurrentZone.Id,
                        zone.Id,
                        StringComparison.Ordinal)))
            {
                return false;
            }

            // 방문 기록은 최초 한 번만 바뀌지만 구역 진입은 재방문 때도 스트리밍에 알려야 한다.
            TryVisit(zone);
            CurrentZone = zone;
            ZoneEntered?.Invoke(zone);
            return true;
        }

        public bool IsShortcutUnlocked(string shortcutId)
        {
            // 저장 데이터나 게이트에서 전달한 빈 ID는 해금 상태로 판단하지 않는다.
            if (string.IsNullOrWhiteSpace(shortcutId))
            {
                return false;
            }

            return unlockedShortcutIds.Contains(shortcutId.Trim());
        }

        public bool TryUnlockShortcut(string shortcutId)
        {
            // 영구 저장 키가 없는 지름길은 재시작 뒤 복원할 수 없으므로 등록하지 않는다.
            if (string.IsNullOrWhiteSpace(shortcutId))
            {
                return false;
            }

            string normalizedId = shortcutId.Trim();
            // HashSet.Add 반환값으로 최초 해금만 이벤트를 발생시키고 중복 Trigger 진입을 무시한다.
            if (!unlockedShortcutIds.Add(normalizedId))
            {
                return false;
            }

            ShortcutUnlocked?.Invoke(normalizedId);
            return true;
        }

        public bool IsRewardCollected(string rewardId)
        {
            // 저장 키로 사용할 수 없는 빈 보상 ID는 획득 상태로 판단하지 않는다.
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return false;
            }

            return collectedRewardIds.Contains(rewardId.Trim());
        }

        public bool TryCollectReward(string rewardId)
        {
            // 영구 ID가 없는 보상은 재시작과 저장 복원에서 구분할 수 없으므로 거부한다.
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return false;
            }

            string normalizedId = rewardId.Trim();
            // HashSet.Add 결과로 최초 획득만 기록하고 효과와 연출의 중복 적용을 막는다.
            if (!collectedRewardIds.Add(normalizedId))
            {
                return false;
            }

            RewardCollected?.Invoke(normalizedId);
            return true;
        }

        public void RebuildInitialState()
        {
            // 씬 재시작과 테스트 초기화가 이전 런타임 진행 기록을 남기지 않게 먼저 비운다.
            visitedZoneIds.Clear();
            unlockedShortcutIds.Clear();
            collectedRewardIds.Clear();
            CurrentZone = null;

            // 직렬화 목록의 null과 중복을 검증하며 유효한 영구 ID만 집합에 넣는다.
            foreach (WorldZoneDefinition zone in initiallyVisitedZones)
            {
                if (zone != null && zone.IsConfigured)
                {
                    visitedZoneIds.Add(zone.Id);
                }
            }

            // 지름길 문자열 목록도 공백을 제거하고 중복 없이 저장 가능한 ID만 등록한다.
            foreach (string shortcutId in initiallyUnlockedShortcutIds)
            {
                if (!string.IsNullOrWhiteSpace(shortcutId))
                {
                    unlockedShortcutIds.Add(shortcutId.Trim());
                }
            }

            // 수집 보상도 영구 ID만 복원해 월드 오브젝트 참조 없이 획득 상태를 재구성한다.
            foreach (string rewardId in initiallyCollectedRewardIds)
            {
                if (!string.IsNullOrWhiteSpace(rewardId))
                {
                    collectedRewardIds.Add(rewardId.Trim());
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

        public void ConfigureInitialShortcutIds(
            IEnumerable<string> shortcutIds)
        {
            // 테스트와 저장 복원 준비가 호출자 컬렉션을 직접 소유하지 않도록 문자열을 복사한다.
            initiallyUnlockedShortcutIds.Clear();
            if (shortcutIds != null)
            {
                // 유효성 검사는 RebuildInitialState에 모아 초기화 경로마다 같은 규칙을 사용한다.
                foreach (string shortcutId in shortcutIds)
                {
                    initiallyUnlockedShortcutIds.Add(shortcutId);
                }
            }

            RebuildInitialState();
        }

        public void ConfigureInitialRewardIds(
            IEnumerable<string> rewardIds)
        {
            // 저장 로더와 테스트가 전달한 목록을 복사해 런타임 상태가 외부 컬렉션에 의존하지 않게 한다.
            initiallyCollectedRewardIds.Clear();
            if (rewardIds != null)
            {
                // 공백과 중복 검사는 공통 초기화 경로에서 처리해 모든 복원 방식에 같은 규칙을 적용한다.
                foreach (string rewardId in rewardIds)
                {
                    initiallyCollectedRewardIds.Add(rewardId);
                }
            }

            RebuildInitialState();
        }
    }
}
