// GOLDEN STANDARD
// 목적: 플레이어 진행 상태를 Unity 오브젝트 참조 없는 버전형 JSON 데이터로 표현한다.
// 책임: 능력·체크포인트·월드·보스 ID의 캡처, JSON 왕복, 버전 이전과 안전한 복원을 제공한다.
// 불변식: 저장 데이터는 영구 ID와 유한한 좌표만 포함하며 ScriptableObject·GameObject 참조를 포함하지 않는다.
// 선택 이유: 순수 DTO와 Codec을 분리하면 파일 저장·클라우드 저장·테스트가 같은 직렬화 계약을 재사용한다.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSkill
{
    [Serializable]
    public sealed class GameProgressSaveData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public List<string> unlockedAbilityIds = new();
        public bool hasCheckpoint;
        public string checkpointId = string.Empty;
        public float respawnX;
        public float respawnY;
        public float respawnZ;
        public List<string> visitedZoneIds = new();
        public List<string> unlockedShortcutIds = new();
        public List<string> collectedRewardIds = new();
        public List<string> defeatedBossIds = new();
    }

    public static class GameProgressSaveCodec
    {
        [Serializable]
        private sealed class SaveVersionHeader
        {
            public int version;
        }

        [Serializable]
        private sealed class GameProgressSaveDataVersionOne
        {
            public int version;
            public List<string> unlockedAbilityIds = new();
            public bool hasCheckpoint;
            public string checkpointId = string.Empty;
            public float respawnX;
            public float respawnY;
            public float respawnZ;
            public List<string> visitedZoneIds = new();
            public List<string> unlockedShortcutIds = new();
            public List<string> collectedRewardIds = new();
        }

        public static GameProgressSaveData Capture(
            PlayerAbilityState abilityState,
            PlayerCheckpointState checkpointState,
            PlayerWorldState worldState = null)
        {
            // 누락된 컴포넌트는 예외 대신 해당 진행 항목이 비어 있는 유효한 기본 데이터로 기록한다.
            var data = new GameProgressSaveData();
            if (abilityState != null)
            {
                data.unlockedAbilityIds =
                    abilityState.CopyUnlockedAbilityIds();
            }

            if (checkpointState != null
                && checkpointState.HasCheckpoint)
            {
                // Vector3를 세 실수로 풀어 JsonUtility가 엔진 내부 필드 구조에 의존하지 않게 한다.
                Vector3 position =
                    checkpointState.LastRespawnPosition;
                data.hasCheckpoint = true;
                data.checkpointId =
                    checkpointState.LastCheckpointId;
                data.respawnX = position.x;
                data.respawnY = position.y;
                data.respawnZ = position.z;
            }

            if (worldState != null)
            {
                // 월드 진행도 내부 HashSet이 아닌 정렬된 영구 ID 복사본만 DTO에 기록한다.
                data.visitedZoneIds =
                    worldState.CopyVisitedZoneIds();
                data.unlockedShortcutIds =
                    worldState.CopyUnlockedShortcutIds();
                data.collectedRewardIds =
                    worldState.CopyCollectedRewardIds();
                data.defeatedBossIds =
                    worldState.CopyDefeatedBossIds();
            }

            return data;
        }

        public static string ToJson(
            GameProgressSaveData data,
            bool prettyPrint = true)
        {
            // null 요청도 현재 버전의 빈 저장 데이터로 바꿔 호출자가 항상 유효한 JSON을 받게 한다.
            return JsonUtility.ToJson(
                data ?? new GameProgressSaveData(),
                prettyPrint);
        }

        public static bool TryFromJson(
            string json,
            out GameProgressSaveData data)
        {
            // 빈 파일과 JSON 문법 오류는 기본값을 반환하고 명시적인 실패로 보고한다.
            data = new GameProgressSaveData();
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                // 별도 헤더는 필드 초기값이 없어 version이 빠진 JSON을 현재 버전으로 오인하지 않는다.
                SaveVersionHeader header =
                    JsonUtility.FromJson<SaveVersionHeader>(
                        json);
                if (header == null)
                {
                    return false;
                }

                switch (header.version)
                {
                    case GameProgressSaveData
                        .CurrentVersion:
                        return TryParseCurrentVersion(
                            json,
                            out data);
                    case 1:
                        return TryMigrateVersionOne(
                            json,
                            out data);
                    default:
                        return false;
                }
            }
            catch (ArgumentException)
            {
                // JsonUtility가 거부한 손상 데이터는 런타임 진행 상태에 일부만 적용하지 않는다.
                return false;
            }
        }

        public static bool Apply(
            GameProgressSaveData data,
            PlayerAbilityState abilityState,
            PlayerCheckpointState checkpointState,
            IEnumerable<AbilityDefinition> knownAbilities,
            PlayerWorldState worldState = null,
            IEnumerable<WorldZoneDefinition> knownZones = null)
        {
            // 현재 버전과 필수 상태 컴포넌트가 모두 확인된 뒤에만 복원을 시작한다.
            if (data == null
                || data.version
                    != GameProgressSaveData.CurrentVersion
                || abilityState == null
                || checkpointState == null
                || (worldState != null
                    && knownZones == null)
                || (data.hasCheckpoint
                    && (string.IsNullOrWhiteSpace(
                            data.checkpointId)
                        || !IsFinite(
                            data.respawnX,
                            data.respawnY,
                            data.respawnZ))))
            {
                return false;
            }

            abilityState.RestoreUnlockedAbilities(
                knownAbilities,
                data.unlockedAbilityIds);
            if (!data.hasCheckpoint)
            {
                checkpointState.ClearCheckpoint();
            }
            else if (!checkpointState.RestoreCheckpoint(
                data.checkpointId,
                new Vector3(
                    data.respawnX,
                    data.respawnY,
                    data.respawnZ)))
            {
                return false;
            }

            // 월드 상태가 제공된 통합 경로에서만 방문·지름길·보상·보스 ID를 한 번에 교체한다.
            return worldState == null
                || worldState.RestoreProgress(
                    knownZones,
                    data.visitedZoneIds,
                    data.unlockedShortcutIds,
                    data.collectedRewardIds,
                    data.defeatedBossIds);
        }

        private static bool TryParseCurrentVersion(
            string json,
            out GameProgressSaveData data)
        {
            // 현재 스키마는 추가 변환 없이 역직렬화하되 모든 선택 목록을 빈 컬렉션으로 정규화한다.
            data = new GameProgressSaveData();
            GameProgressSaveData parsed =
                JsonUtility.FromJson<GameProgressSaveData>(
                    json);
            if (parsed == null
                || parsed.version
                    != GameProgressSaveData
                        .CurrentVersion)
            {
                return false;
            }

            Normalize(parsed);
            data = parsed;
            return true;
        }

        private static bool TryMigrateVersionOne(
            string json,
            out GameProgressSaveData data)
        {
            // v1에는 보스 처치 필드가 없으므로 기존 진행을 그대로 복사하고 빈 보스 목록을 추가한다.
            data = new GameProgressSaveData();
            GameProgressSaveDataVersionOne legacy =
                JsonUtility
                    .FromJson<GameProgressSaveDataVersionOne>(
                        json);
            if (legacy == null
                || legacy.version != 1)
            {
                return false;
            }

            var migrated =
                new GameProgressSaveData
                {
                    version =
                        GameProgressSaveData
                            .CurrentVersion,
                    unlockedAbilityIds =
                        legacy.unlockedAbilityIds,
                    hasCheckpoint =
                        legacy.hasCheckpoint,
                    checkpointId =
                        legacy.checkpointId,
                    respawnX = legacy.respawnX,
                    respawnY = legacy.respawnY,
                    respawnZ = legacy.respawnZ,
                    visitedZoneIds =
                        legacy.visitedZoneIds,
                    unlockedShortcutIds =
                        legacy.unlockedShortcutIds,
                    collectedRewardIds =
                        legacy.collectedRewardIds,
                    defeatedBossIds =
                        new List<string>()
                };
            Normalize(migrated);
            data = migrated;
            return true;
        }

        private static void Normalize(
            GameProgressSaveData data)
        {
            // 누락 가능한 참조 필드를 한곳에서 초기화해 현재 파싱과 구버전 이전의 결과 계약을 같게 만든다.
            data.unlockedAbilityIds ??=
                new List<string>();
            data.checkpointId ??=
                string.Empty;
            data.visitedZoneIds ??=
                new List<string>();
            data.unlockedShortcutIds ??=
                new List<string>();
            data.collectedRewardIds ??=
                new List<string>();
            data.defeatedBossIds ??=
                new List<string>();
        }

        private static bool IsFinite(
            float x,
            float y,
            float z)
        {
            // 상태를 바꾸기 전에 체크포인트의 세 좌표를 모두 검증해 부분 복원을 막는다.
            return !float.IsNaN(x)
                && !float.IsNaN(y)
                && !float.IsNaN(z)
                && !float.IsInfinity(x)
                && !float.IsInfinity(y)
                && !float.IsInfinity(z);
        }
    }
}
