// GOLDEN STANDARD
// 목적: 현재 플레이어 구역에 대응하는 콘텐츠 Scene을 Additive 방식으로 비동기 전환한다.
// 책임: 구역 진입 이벤트를 구독하고 목표 Scene 로드 후 이전 Scene을 언로드하며 전환 상태를 공개한다.
// 불변식: Main Scene은 유지하고 동시에 활성 상태로 관리하는 구역 콘텐츠 Scene은 최대 하나다.
// 선택 이유: 코루틴 직렬화는 빠른 연속 진입도 마지막 요청까지 처리하며 Unity Scene API 생명주기를 명확히 보여 준다.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameSkill
{
    [DisallowMultipleComponent]
    public sealed class WorldZoneStreamController : MonoBehaviour
    {
        [SerializeField] private PlayerWorldState playerWorldState;
        [SerializeField] private WorldZoneDefinition initialZone;
        [SerializeField]
        private List<WorldZoneSceneBinding> sceneBindings = new();

        private Coroutine transitionRoutine;
        private string requestedZoneId = string.Empty;
        private bool isSubscribed;

        public string LoadedZoneId { get; private set; } = string.Empty;
        public bool IsTransitioning { get; private set; }
        public int BindingCount => sceneBindings.Count;

        private void OnEnable()
        {
            // 활성 컨트롤러만 플레이어 진입 이벤트를 받아 중복 Scene 전환 요청을 막는다.
            Subscribe();
        }

        private void Start()
        {
            // 첫 물리 이동 전에 시작 구역 콘텐츠를 요청해 빈 배경 프레임을 최소화한다.
            if (initialZone != null)
            {
                RequestZone(initialZone);
            }
        }

        private void OnDisable()
        {
            // 비활성화된 컨트롤러가 이후 구역 진입 이벤트를 계속 받지 않도록 구독을 해제한다.
            Unsubscribe();
        }

        public bool Configure(
            PlayerWorldState worldState,
            WorldZoneDefinition firstZone,
            IEnumerable<WorldZoneSceneBinding> bindings)
        {
            var requestedBindings =
                new List<WorldZoneSceneBinding>();

            // 호출자가 소유한 컬렉션과 Inspector 직렬화 목록을 분리해 이후 변경의 영향을 막는다.
            if (bindings != null)
            {
                foreach (WorldZoneSceneBinding binding in bindings)
                {
                    if (binding != null)
                    {
                        requestedBindings.Add(binding);
                    }
                }
            }

            bool changed = playerWorldState != worldState
                || initialZone != firstZone
                || !BindingsMatch(
                    sceneBindings,
                    requestedBindings);
            if (!changed)
            {
                // 같은 구성은 직렬화 목록을 다시 쓰지 않아 에디터 씬의 불필요한 Dirty 상태를 막는다.
                Subscribe();
                return false;
            }

            Unsubscribe();
            playerWorldState = worldState;
            initialZone = firstZone;
            sceneBindings.Clear();
            sceneBindings.AddRange(requestedBindings);
            Subscribe();
            return true;
        }

        public bool TryGetScenePath(
            string zoneId,
            out string scenePath)
        {
            // 소수의 구역 목록은 선형 탐색해 별도 Dictionary 직렬화 동기화 비용을 피한다.
            foreach (WorldZoneSceneBinding binding in sceneBindings)
            {
                if (binding != null && binding.Matches(zoneId))
                {
                    scenePath = binding.ScenePath;
                    return true;
                }
            }

            scenePath = string.Empty;
            return false;
        }

        public bool RequestZone(WorldZoneDefinition zone)
        {
            // 정의와 Scene 바인딩이 모두 유효한 요청만 비동기 전환 대상으로 받는다.
            if (zone == null
                || !zone.IsConfigured
                || !TryGetScenePath(zone.Id, out _))
            {
                return false;
            }

            requestedZoneId = zone.Id;
            if (Application.isPlaying
                && isActiveAndEnabled
                && transitionRoutine == null)
            {
                // 실행 중인 전환이 없을 때만 코루틴을 시작하고 연속 요청은 같은 루프가 소비한다.
                transitionRoutine =
                    StartCoroutine(ProcessRequests());
            }

            return true;
        }

        private IEnumerator ProcessRequests()
        {
            IsTransitioning = true;

            // 로드 도중 요청이 바뀌어도 마지막 목표와 현재 로드 구역이 같아질 때까지 순서대로 처리한다.
            while (!string.IsNullOrWhiteSpace(requestedZoneId)
                && !string.Equals(
                    LoadedZoneId,
                    requestedZoneId,
                    StringComparison.Ordinal))
            {
                string targetZoneId = requestedZoneId;
                if (!TryGetScenePath(
                    targetZoneId,
                    out string targetScenePath))
                {
                    // 빌드 설정이 훼손된 요청은 무한 재시도하지 않고 현재 상태를 유지한다.
                    break;
                }

                Scene targetScene =
                    SceneManager.GetSceneByPath(targetScenePath);
                if (!targetScene.isLoaded)
                {
                    AsyncOperation loadOperation =
                        SceneManager.LoadSceneAsync(
                            targetScenePath,
                            LoadSceneMode.Additive);
                    if (loadOperation == null)
                    {
                        // Unity가 로드 작업을 만들지 못하면 다음 프레임 반복 대신 전환을 중단한다.
                        break;
                    }

                    // 비동기 Scene 작업이 끝날 때까지 프레임을 양보해 게임 루프를 막지 않는다.
                    while (!loadOperation.isDone)
                    {
                        yield return null;
                    }
                }

                string previousZoneId = LoadedZoneId;
                if (!string.IsNullOrWhiteSpace(previousZoneId)
                    && !string.Equals(
                        previousZoneId,
                        targetZoneId,
                        StringComparison.Ordinal)
                    && TryGetScenePath(
                        previousZoneId,
                        out string previousScenePath))
                {
                    Scene previousScene =
                        SceneManager.GetSceneByPath(previousScenePath);
                    if (previousScene.isLoaded)
                    {
                        AsyncOperation unloadOperation =
                            SceneManager.UnloadSceneAsync(previousScene);
                        // 이전 콘텐츠가 완전히 정리될 때까지 기다려 활성 구역 수 불변식을 회복한다.
                        while (unloadOperation != null
                            && !unloadOperation.isDone)
                        {
                            yield return null;
                        }
                    }
                }

                LoadedZoneId = targetZoneId;
                Debug.Log(
                    $"구역 Scene 전환 완료: {LoadedZoneId}",
                    this);
            }

            IsTransitioning = false;
            transitionRoutine = null;
        }

        private void HandleZoneEntered(
            WorldZoneDefinition enteredZone)
        {
            // 물리 구역 진입을 Scene 경로가 아닌 데이터 정의로 전달해 런타임 결합을 낮춘다.
            RequestZone(enteredZone);
        }

        private void Subscribe()
        {
            // Configure와 OnEnable이 연속 호출돼도 플레이어 이벤트는 한 번만 구독한다.
            if (isSubscribed || playerWorldState == null)
            {
                return;
            }

            playerWorldState.ZoneEntered += HandleZoneEntered;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            // 아직 구독하지 않았거나 상태가 사라진 경우에도 생명주기 종료를 안전하게 처리한다.
            if (!isSubscribed || playerWorldState == null)
            {
                isSubscribed = false;
                return;
            }

            playerWorldState.ZoneEntered -= HandleZoneEntered;
            isSubscribed = false;
        }

        private static bool BindingsMatch(
            IReadOnlyList<WorldZoneSceneBinding> current,
            IReadOnlyList<WorldZoneSceneBinding> requested)
        {
            // 개수가 다르면 같은 인덱스 기반 구역·Scene 매핑일 수 없으므로 즉시 실패한다.
            if (current.Count != requested.Count)
            {
                return false;
            }

            // 빌더가 정의한 순서까지 비교해 Inspector와 테스트 결과를 결정적으로 유지한다.
            for (int index = 0; index < current.Count; index++)
            {
                WorldZoneSceneBinding currentBinding =
                    current[index];
                WorldZoneSceneBinding requestedBinding =
                    requested[index];
                if (currentBinding == null
                    || requestedBinding == null
                    || currentBinding.Zone != requestedBinding.Zone
                    || currentBinding.ScenePath
                        != requestedBinding.ScenePath)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
