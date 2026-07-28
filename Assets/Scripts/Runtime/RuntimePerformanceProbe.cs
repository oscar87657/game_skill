// GOLDEN STANDARD
// 목적: 실제 Main 씬에서 짧은 저부하 성능 기준선을 개발 환경 Console에 기록한다.
// 책임: 워밍업, 고정 버퍼 샘플링, ProfilerRecorder 수명과 기준선 보고서 생성을 담당한다.
// 불변식: Release 빌드에서는 자동 측정하지 않으며 측정 중에도 게임 상태를 변경하지 않는다.
// 선택 이유: Unity Profiler의 상세 분석 전에 반복 가능한 숫자 기준선을 남기면 최적화 전후를 비교할 수 있다.
using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameSkill
{
    [DisallowMultipleComponent]
    public sealed class RuntimePerformanceProbe :
        MonoBehaviour
    {
        private const int BufferCapacity = 600;
        private const float BytesPerMegabyte =
            1024f * 1024f;

        [SerializeField, Min(0f)]
        private float warmupSeconds = 1.5f;
        [SerializeField, Min(0.1f)]
        private float sampleSeconds = 5f;
        [SerializeField]
        private bool captureOnEnable = true;
        [SerializeField]
        private bool logCompletedReport = true;

        private readonly float[] frameMilliseconds =
            new float[BufferCapacity];
        private readonly float[] gcBytes =
            new float[BufferCapacity];
        private readonly float[] drawCalls =
            new float[BufferCapacity];

        private ProfilerRecorder gcRecorder;
        private ProfilerRecorder drawCallRecorder;
        private float warmupElapsed;
        private float sampleElapsed;
        private float peakAllocatedMemoryMegabytes;
        private int sampleCount;
        private int totalSampleCount;
        private bool ownsRunInBackgroundOverride;
        private bool previousRunInBackground;

        public bool IsConfigured =>
            warmupSeconds >= 0f
            && sampleSeconds >= 0.1f;
        public bool IsCapturing { get; private set; }
        public bool IsComplete { get; private set; }
        public int SampleCount => sampleCount;
        public PerformanceBaselineReport LastReport
        {
            get;
            private set;
        }

        private void OnEnable()
        {
            // 자동 측정은 Editor와 Development Build에만 허용해 Release 플레이 비용을 만들지 않는다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (captureOnEnable)
            {
                StartCapture();
            }
#endif
        }

        private void Update()
        {
            // 개발 중 F8을 누르면 원하는 전투·구역 상황에서 같은 길이의 측정을 다시 시작한다.
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null
                && Keyboard.current.f8Key
                    .wasPressedThisFrame)
            {
                StartCapture();
            }
#endif

            if (!IsCapturing)
            {
                return;
            }

            float deltaTime =
                Time.unscaledDeltaTime;
            if (warmupElapsed
                < warmupSeconds)
            {
                warmupElapsed +=
                    deltaTime;
                return;
            }

            RecordSample(deltaTime);
            sampleElapsed += deltaTime;
            if (sampleElapsed >= sampleSeconds)
            {
                FinishCapture();
            }
        }

        private void OnDisable()
        {
            // Scene 종료나 컴포넌트 비활성화 시 네이티브 Recorder 핸들을 항상 해제한다.
            bool wasCapturing =
                IsCapturing;
            DisposeRecorders();
            RestoreRunInBackground();
            IsCapturing = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (wasCapturing)
            {
                // 측정 도중 Play를 종료한 경우 완료 보고서와 혼동하지 않게 수집 개수를 명시한다.
                Debug.Log(
                    "[Performance Capture] aborted before completion, samples="
                    + sampleCount,
                    this);
            }
#endif
        }

        public bool Configure(
            float warmupDuration,
            float sampleDuration,
            bool shouldCaptureOnEnable,
            bool shouldLogReport)
        {
            // 빌더가 같은 측정 설정을 다시 전달할 때 Scene Dirty 여부를 값으로 정확히 반환한다.
            float safeWarmup =
                Mathf.Max(
                    0f,
                    warmupDuration);
            float safeSample =
                Mathf.Max(
                    0.1f,
                    sampleDuration);
            bool changed =
                !Mathf.Approximately(
                    warmupSeconds,
                    safeWarmup)
                || !Mathf.Approximately(
                    sampleSeconds,
                    safeSample)
                || captureOnEnable
                    != shouldCaptureOnEnable
                || logCompletedReport
                    != shouldLogReport;
            warmupSeconds = safeWarmup;
            sampleSeconds = safeSample;
            captureOnEnable =
                shouldCaptureOnEnable;
            logCompletedReport =
                shouldLogReport;
            return changed;
        }

        [ContextMenu("Start Performance Capture")]
        public void StartCapture()
        {
            // 재측정은 이전 Recorder와 카운터를 먼저 초기화해 두 구간의 샘플이 섞이지 않게 한다.
            DisposeRecorders();
            Array.Clear(
                frameMilliseconds,
                0,
                frameMilliseconds.Length);
            Array.Clear(
                gcBytes,
                0,
                gcBytes.Length);
            Array.Clear(
                drawCalls,
                0,
                drawCalls.Length);
            warmupElapsed = 0f;
            sampleElapsed = 0f;
            peakAllocatedMemoryMegabytes =
                0f;
            sampleCount = 0;
            totalSampleCount = 0;
            IsComplete = false;
            IsCapturing = true;
            EnableBackgroundSampling();

            ProfilerRecorderOptions commonOptions =
                ProfilerRecorderOptions.Default;
            ProfilerRecorderOptions gcOptions =
                commonOptions
                | ProfilerRecorderOptions
                    .CollectOnlyOnCurrentThread;
            gcRecorder =
                ProfilerRecorder.StartNew(
                    ProfilerCategory.Internal,
                    "GC.Alloc",
                    1,
                    gcOptions);
            drawCallRecorder =
                ProfilerRecorder.StartNew(
                    ProfilerCategory.Render,
                    "Draw Calls Count",
                    1,
                    commonOptions);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 측정 시작을 Console에 남겨 자동 Probe 구성과 F8 재측정 입력이 실제로 승인됐는지 확인한다.
            Debug.Log(
                "[Performance Capture] started, warmup="
                + $"{warmupSeconds:F1}s, sample={sampleSeconds:F1}s",
                this);
#endif
        }

        private void RecordSample(
            float deltaTime)
        {
            // 순환 인덱스는 최근 600프레임만 보존하면서도 배열 확장이나 새 관리 객체 할당을 만들지 않는다.
            int writeIndex =
                totalSampleCount
                % BufferCapacity;

            frameMilliseconds[writeIndex] =
                Mathf.Max(
                    0f,
                    deltaTime * 1000f);
            gcBytes[writeIndex] =
                gcRecorder.Valid
                    ? Mathf.Max(
                        0f,
                        gcRecorder.LastValue)
                    : 0f;
            drawCalls[writeIndex] =
                drawCallRecorder.Valid
                    ? Mathf.Max(
                        0f,
                        drawCallRecorder.LastValue)
                    : 0f;
            float allocatedMegabytes =
                Profiler
                    .GetTotalAllocatedMemoryLong()
                / BytesPerMegabyte;
            peakAllocatedMemoryMegabytes =
                Mathf.Max(
                    peakAllocatedMemoryMegabytes,
                    allocatedMegabytes);
            totalSampleCount++;
            sampleCount =
                Mathf.Min(
                    totalSampleCount,
                    BufferCapacity);
        }

        private void FinishCapture()
        {
            // Recorder 지원 여부를 결과에 포함한 뒤 통계 계산과 Console 문자열 할당은 측정 종료 후에만 수행한다.
            bool gcAvailable =
                gcRecorder.Valid;
            bool drawCallsAvailable =
                drawCallRecorder.Valid;
            IsCapturing = false;
            IsComplete = true;
            LastReport =
                new PerformanceBaselineReport(
                    PerformanceStatisticsMath
                        .Calculate(
                            frameMilliseconds,
                            sampleCount),
                    PerformanceStatisticsMath
                        .Calculate(
                            gcBytes,
                            sampleCount),
                    PerformanceStatisticsMath
                        .Calculate(
                            drawCalls,
                            sampleCount),
                    peakAllocatedMemoryMegabytes,
                    gcAvailable,
                    drawCallsAvailable);
            DisposeRecorders();
            RestoreRunInBackground();
            if (logCompletedReport)
            {
                Debug.Log(
                    "[Performance Baseline] "
                    + LastReport.ToSummary()
                    + ", observed frames="
                    + totalSampleCount,
                    this);
            }
        }

        private void DisposeRecorders()
        {
            // 유효하지 않은 Recorder도 Dispose 가능한 값 형식이므로 생성 여부와 무관하게 수명을 닫는다.
            gcRecorder.Dispose();
            drawCallRecorder.Dispose();
            gcRecorder = default;
            drawCallRecorder = default;
        }

        private void EnableBackgroundSampling()
        {
            // Editor가 Terminal 같은 다른 창으로 포커스를 넘겨도 같은 실시간 구간을 측정하도록 일시 설정한다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ownsRunInBackgroundOverride)
            {
                return;
            }

            previousRunInBackground =
                Application.runInBackground;
            Application.runInBackground = true;
            ownsRunInBackgroundOverride = true;
#endif
        }

        private void RestoreRunInBackground()
        {
            // 측정 종료 뒤에는 프로젝트가 원래 사용하던 백그라운드 실행 정책을 정확히 복원한다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!ownsRunInBackgroundOverride)
            {
                return;
            }

            Application.runInBackground =
                previousRunInBackground;
            ownsRunInBackgroundOverride = false;
#endif
        }
    }
}
