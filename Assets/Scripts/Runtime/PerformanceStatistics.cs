// GOLDEN STANDARD
// 목적: 런타임 성능 샘플을 평균·95백분위·최댓값과 명시적 예산 판정으로 변환한다.
// 책임: 샘플 통계 계산, 프로토타입 예산 비교와 사람이 읽을 수 있는 기준선 요약을 제공한다.
// 불변식: 입력 배열을 수정하지 않으며 유효 샘플이 없으면 모든 통계를 0으로 반환한다.
// 선택 이유: 측정 수집과 통계 계산을 분리하면 Profiler API 없이도 경계값과 예산을 테스트할 수 있다.
using System;
using UnityEngine;

namespace GameSkill
{
    public readonly struct PerformanceStatistics
    {
        public PerformanceStatistics(
            int sampleCount,
            float average,
            float percentile95,
            float maximum)
        {
            // 계산 완료된 불변 통계만 전달해 UI·로그 계층이 원본 배열을 소유하지 않게 한다.
            SampleCount = sampleCount;
            Average = average;
            Percentile95 = percentile95;
            Maximum = maximum;
        }

        public int SampleCount { get; }
        public float Average { get; }
        public float Percentile95 { get; }
        public float Maximum { get; }
    }

    public readonly struct PerformanceBaselineReport
    {
        public const float TargetFrameMilliseconds =
            1000f / 60f;
        public const float TargetGcBytesPerFrame =
            1024f;
        public const float TargetDrawCalls =
            150f;

        public PerformanceBaselineReport(
            PerformanceStatistics frameMilliseconds,
            PerformanceStatistics gcBytes,
            PerformanceStatistics drawCalls,
            float peakAllocatedMemoryMegabytes,
            bool gcRecorderAvailable,
            bool drawCallRecorderAvailable)
        {
            // 측정값과 Recorder 지원 여부를 함께 보존해 지원되지 않는 0을 실제 성능으로 오해하지 않게 한다.
            FrameMilliseconds =
                frameMilliseconds;
            GcBytes = gcBytes;
            DrawCalls = drawCalls;
            PeakAllocatedMemoryMegabytes =
                Mathf.Max(
                    0f,
                    peakAllocatedMemoryMegabytes);
            GcRecorderAvailable =
                gcRecorderAvailable;
            DrawCallRecorderAvailable =
                drawCallRecorderAvailable;
        }

        public PerformanceStatistics FrameMilliseconds
        {
            get;
        }
        public PerformanceStatistics GcBytes
        {
            get;
        }
        public PerformanceStatistics DrawCalls
        {
            get;
        }
        public float PeakAllocatedMemoryMegabytes
        {
            get;
        }
        public bool GcRecorderAvailable { get; }
        public bool DrawCallRecorderAvailable
        {
            get;
        }
        public bool MeetsFrameBudget =>
            FrameMilliseconds.Percentile95
            <= TargetFrameMilliseconds;
        public bool MeetsGcBudget =>
            !GcRecorderAvailable
            || GcBytes.Percentile95
                <= TargetGcBytesPerFrame;
        public bool MeetsDrawCallBudget =>
            !DrawCallRecorderAvailable
            || DrawCalls.Maximum
                <= TargetDrawCalls;
        public bool MeetsPrototypeBudget =>
            MeetsFrameBudget
            && MeetsGcBudget
            && MeetsDrawCallBudget;

        public string ToSummary()
        {
            // 소수점 자릿수와 단위를 고정해 서로 다른 측정 로그를 Git 문서에서 쉽게 비교하게 한다.
            string gcSummary =
                GcRecorderAvailable
                    ? $"{GcBytes.Average:F0} B avg / "
                        + $"{GcBytes.Percentile95:F0} B p95"
                    : "unsupported";
            string drawSummary =
                DrawCallRecorderAvailable
                    ? $"{DrawCalls.Average:F1} avg / "
                        + $"{DrawCalls.Maximum:F0} max"
                    : "unsupported";
            return "samples="
                + FrameMilliseconds.SampleCount
                + $", frame={FrameMilliseconds.Average:F2} ms avg"
                + $" / {FrameMilliseconds.Percentile95:F2} ms p95"
                + $" / {FrameMilliseconds.Maximum:F2} ms max"
                + $", GC={gcSummary}"
                + $", draw calls={drawSummary}"
                + $", allocated memory peak={PeakAllocatedMemoryMegabytes:F1} MB"
                + $", budget={(MeetsPrototypeBudget ? "PASS" : "CHECK")}";
        }
    }

    public static class PerformanceStatisticsMath
    {
        public static PerformanceStatistics Calculate(
            float[] samples,
            int requestedCount)
        {
            // null 배열과 범위를 벗어난 개수는 빈 통계 또는 실제 배열 길이로 안전하게 제한한다.
            if (samples == null
                || samples.Length == 0
                || requestedCount <= 0)
            {
                return new PerformanceStatistics(
                    0,
                    0f,
                    0f,
                    0f);
            }

            int sampleCount =
                Mathf.Min(
                    requestedCount,
                    samples.Length);
            var sortedSamples =
                new float[sampleCount];
            float sum = 0f;
            float maximum =
                float.MinValue;
            // 원본 측정 버퍼를 보존하면서 유효 범위만 복사하고 평균·최댓값을 한 번에 계산한다.
            for (int index = 0;
                 index < sampleCount;
                 index++)
            {
                float sample =
                    Mathf.Max(
                        0f,
                        samples[index]);
                sortedSamples[index] =
                    sample;
                sum += sample;
                maximum =
                    Mathf.Max(
                        maximum,
                        sample);
            }

            Array.Sort(sortedSamples);
            int percentileIndex =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        sampleCount * 0.95f)
                    - 1,
                    0,
                    sampleCount - 1);
            return new PerformanceStatistics(
                sampleCount,
                sum / sampleCount,
                sortedSamples[
                    percentileIndex],
                maximum);
        }
    }
}
