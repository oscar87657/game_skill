// GOLDEN STANDARD
// 목적: 플레이어 진행 스냅샷과 로컬 JSON 파일 사이의 명시적인 저장·불러오기 경계를 제공한다.
// 책임: 저장 경로 계산, 능력 카탈로그 구성, 파일 입출력과 Codec 적용 결과를 관리한다.
// 불변식: 자동 저장이나 자동 불러오기를 수행하지 않으며 성공한 전체 JSON만 런타임 상태에 적용한다.
// 선택 이유: 명시적 API부터 제공하면 테스트와 UI가 저장 시점을 통제하고 개발 중 세이브 덮어쓰기를 피할 수 있다.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerAbilityState))]
    [RequireComponent(typeof(PlayerCheckpointState))]
    public sealed class GameProgressSaveController : MonoBehaviour
    {
        [SerializeField]
        private List<AbilityDefinition> abilityCatalog = new();
        [SerializeField]
        private string saveFileName = "game_skill_save_v1.json";

        private PlayerAbilityState abilityState;
        private PlayerCheckpointState checkpointState;

        public string SavePath =>
            Path.Combine(
                Application.persistentDataPath,
                saveFileName);

        private void Awake()
        {
            // 저장 요청마다 컴포넌트를 검색하지 않도록 같은 플레이어의 상태 소유자를 캐시한다.
            abilityState =
                GetComponent<PlayerAbilityState>();
            checkpointState =
                GetComponent<PlayerCheckpointState>();
        }

        public bool Configure(
            IEnumerable<AbilityDefinition> knownAbilities,
            string fileName = "game_skill_save_v1.json")
        {
            // 파일명은 디렉터리 탈출을 막기 위해 마지막 경로 요소만 허용한다.
            string requestedFileName =
                string.IsNullOrWhiteSpace(fileName)
                    ? "game_skill_save_v1.json"
                    : Path.GetFileName(fileName.Trim());
            string normalizedFileName =
                string.IsNullOrWhiteSpace(requestedFileName)
                    ? "game_skill_save_v1.json"
                    : requestedFileName;
            var requestedCatalog =
                new List<AbilityDefinition>();
            if (knownAbilities != null)
            {
                // null 정의는 저장 복원에 쓰일 수 없으므로 직렬화 카탈로그에서 제외한다.
                foreach (AbilityDefinition ability in knownAbilities)
                {
                    if (ability != null
                        && ability.IsConfigured
                        && !requestedCatalog.Contains(ability))
                    {
                        requestedCatalog.Add(ability);
                    }
                }
            }

            bool changed =
                saveFileName != normalizedFileName
                || !CatalogMatches(
                    abilityCatalog,
                    requestedCatalog);
            saveFileName = normalizedFileName;
            abilityCatalog.Clear();
            abilityCatalog.AddRange(requestedCatalog);
            return changed;
        }

        public string CaptureJson(
            bool prettyPrint = true)
        {
            // 테스트와 향후 클라우드 저장도 파일 API 없이 같은 스냅샷 계약을 사용할 수 있게 JSON을 반환한다.
            EnsureStateReferences();
            return GameProgressSaveCodec.ToJson(
                GameProgressSaveCodec.Capture(
                    abilityState,
                    checkpointState),
                prettyPrint);
        }

        public bool ApplyJson(string json)
        {
            // 파싱이 완전히 성공한 데이터만 상태에 전달해 손상 파일의 부분 복원을 막는다.
            EnsureStateReferences();
            return GameProgressSaveCodec.TryFromJson(
                    json,
                    out GameProgressSaveData data)
                && GameProgressSaveCodec.Apply(
                    data,
                    abilityState,
                    checkpointState,
                    abilityCatalog);
        }

        public bool SaveNow()
        {
            // 호출자가 명시적으로 요청한 시점에만 현재 진행을 UTF-8 JSON 파일로 덮어쓴다.
            try
            {
                File.WriteAllText(
                    SavePath,
                    CaptureJson());
                return true;
            }
            catch (Exception exception)
                when (exception is IOException
                    || exception is UnauthorizedAccessException
                    || exception is ArgumentException)
            {
                Debug.LogError(
                    $"진행 저장 실패: {exception.Message}",
                    this);
                return false;
            }
        }

        public bool LoadNow()
        {
            // 파일이 없거나 읽기 실패한 경우 현재 런타임 진행 상태를 그대로 보존한다.
            try
            {
                return File.Exists(SavePath)
                    && ApplyJson(
                        File.ReadAllText(SavePath));
            }
            catch (Exception exception)
                when (exception is IOException
                    || exception is UnauthorizedAccessException
                    || exception is ArgumentException)
            {
                Debug.LogError(
                    $"진행 불러오기 실패: {exception.Message}",
                    this);
                return false;
            }
        }

        private void EnsureStateReferences()
        {
            // EditMode 테스트처럼 Awake 전에 호출돼도 같은 오브젝트의 필수 상태를 안전하게 찾는다.
            abilityState ??=
                GetComponent<PlayerAbilityState>();
            checkpointState ??=
                GetComponent<PlayerCheckpointState>();
        }

        private static bool CatalogMatches(
            IReadOnlyList<AbilityDefinition> current,
            IReadOnlyList<AbilityDefinition> requested)
        {
            // 개수가 다르면 동일한 복원 카탈로그가 될 수 없으므로 항목 비교를 생략한다.
            if (current.Count != requested.Count)
            {
                return false;
            }

            // 에셋 참조와 순서를 모두 비교해 빌더 재실행 시 불필요한 Scene 변경을 막는다.
            for (int index = 0;
                 index < current.Count;
                 index++)
            {
                if (current[index] != requested[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
