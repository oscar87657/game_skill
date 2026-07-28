// GOLDEN STANDARD
// 목적: 성능 Probe가 수집한 숫자를 안정적인 통계와 예산 판정으로 변환하는지 검증한다.
// 책임: 평균·95백분위·최댓값, 유효 개수 제한과 지원 여부별 예산 처리를 확인한다.
// 불변식: 테스트는 Unity Profiler 활성 상태와 실행 장치 성능에 의존하지 않는다.
// 선택 이유: 환경 변동이 큰 실제 프레임 대신 순수 통계 계약을 자동 회귀 테스트로 고정한다.
using NUnit.Framework;

namespace GameSkill.Tests
{
    public sealed class PerformanceStatisticsTests
    {
        [Test]
        public void Calculate_ReturnsAveragePercentileAndMaximum()
        {
            // 정렬되지 않은 다섯 샘플에서 평균과 nearest-rank 95백분위를 검증한다.
            float[] samples =
            {
                8f,
                4f,
                20f,
                16f,
                12f
            };

            PerformanceStatistics statistics =
                PerformanceStatisticsMath.Calculate(
                    samples,
                    samples.Length);

            Assert.That(
                statistics.SampleCount,
                Is.EqualTo(5));
            Assert.That(
                statistics.Average,
                Is.EqualTo(12f).Within(0.001f));
            Assert.That(
                statistics.Percentile95,
                Is.EqualTo(20f).Within(0.001f));
            Assert.That(
                statistics.Maximum,
                Is.EqualTo(20f).Within(0.001f));
            Assert.That(
                samples[0],
                Is.EqualTo(8f));
        }

        [Test]
        public void Calculate_ClampsCountAndNegativeSamples()
        {
            // Recorder 초기화 구간의 음수 방어값은 0으로 제한하고 배열 길이 밖을 읽지 않아야 한다.
            PerformanceStatistics statistics =
                PerformanceStatisticsMath.Calculate(
                    new[]
                    {
                        -4f,
                        2f
                    },
                    10);

            Assert.That(
                statistics.SampleCount,
                Is.EqualTo(2));
            Assert.That(
                statistics.Average,
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                statistics.Maximum,
                Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void BaselineReport_EvaluatesPrototypeBudgets()
        {
            // 60 FPS·1 KB GC·150 draw call 기준 안팎의 값을 한 보고서에서 판정한다.
            var frame =
                new PerformanceStatistics(
                    120,
                    8f,
                    12f,
                    15f);
            var gc =
                new PerformanceStatistics(
                    120,
                    32f,
                    128f,
                    2048f);
            var draw =
                new PerformanceStatistics(
                    120,
                    70f,
                    80f,
                    90f);
            var passing =
                new PerformanceBaselineReport(
                    frame,
                    gc,
                    draw,
                    180f,
                    true,
                    true);
            var failing =
                new PerformanceBaselineReport(
                    new PerformanceStatistics(
                        120,
                        12f,
                        20f,
                        28f),
                    gc,
                    draw,
                    180f,
                    true,
                    true);

            Assert.That(
                passing.MeetsPrototypeBudget,
                Is.True);
            Assert.That(
                failing.MeetsFrameBudget,
                Is.False);
            Assert.That(
                failing.MeetsPrototypeBudget,
                Is.False);
        }

        [Test]
        public void BaselineReport_DoesNotFailUnsupportedRecorders()
        {
            // 플랫폼에서 GC·Render 통계를 지원하지 않으면 0을 좋은 측정값으로 취급하지 않고 예산에서 제외한다.
            var frame =
                new PerformanceStatistics(
                    60,
                    8f,
                    10f,
                    14f);
            var unavailable =
                new PerformanceStatistics(
                    60,
                    9999f,
                    9999f,
                    9999f);
            var report =
                new PerformanceBaselineReport(
                    frame,
                    unavailable,
                    unavailable,
                    100f,
                    false,
                    false);

            Assert.That(
                report.MeetsPrototypeBudget,
                Is.True);
            Assert.That(
                report.ToSummary(),
                Does.Contain("unsupported"));
        }
    }
}
