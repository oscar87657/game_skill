// GOLDEN STANDARD
// 목적: 능력과 체크포인트 진행 상태를 Unity 오브젝트 참조 없는 버전형 JSON 데이터로 표현한다.
// 책임: 현재 상태 캡처, JSON 왕복, 알려진 능력 정의를 통한 안전한 복원을 제공한다.
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
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public List<string> unlockedAbilityIds = new();
        public bool hasCheckpoint;
        public string checkpointId = string.Empty;
        public float respawnX;
        public float respawnY;
        public float respawnZ;
    }

    public static class GameProgressSaveCodec
    {
        [Serializable]
        private sealed class SaveVersionHeader
        {
            public int version;
        }

        public static GameProgressSaveData Capture(
            PlayerAbilityState abilityState,
            PlayerCheckpointState checkpointState)
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
                if (header == null
                    || header.version
                        != GameProgressSaveData.CurrentVersion)
                {
                    return false;
                }

                GameProgressSaveData parsed =
                    JsonUtility.FromJson<GameProgressSaveData>(
                        json);
                if (parsed == null
                    || parsed.version
                        != GameProgressSaveData.CurrentVersion)
                {
                    return false;
                }

                parsed.unlockedAbilityIds ??=
                    new List<string>();
                parsed.checkpointId ??= string.Empty;
                data = parsed;
                return true;
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
            IEnumerable<AbilityDefinition> knownAbilities)
        {
            // 현재 버전과 필수 상태 컴포넌트가 모두 확인된 뒤에만 복원을 시작한다.
            if (data == null
                || data.version
                    != GameProgressSaveData.CurrentVersion
                || abilityState == null
                || checkpointState == null
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
                return true;
            }

            return checkpointState.RestoreCheckpoint(
                data.checkpointId,
                new Vector3(
                    data.respawnX,
                    data.respawnY,
                    data.respawnZ));
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
