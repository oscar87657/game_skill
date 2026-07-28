// GOLDEN STANDARD
// 목적: 임시 효과음 합성이 에셋 없이도 안전하고 재현 가능한 파형을 만드는지 검증한다.
// 책임: 길이·샘플 범위·유효 숫자·시드 재현성과 최소 입력 보정을 확인한다.
// 불변식: 테스트는 오디오 출력 장치나 씬 재생 상태에 의존하지 않는다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class PrototypeAudioSynthTests
    {
        [Test]
        public void ToneSweep_ProducesBoundedAudibleSamples()
        {
            // 짧은 스윕도 지정 샘플레이트와 유효한 비영점 파형을 가져야 한다.
            AudioClip clip =
                PrototypeAudioSynth.CreateToneSweep(
                    "ToneTest",
                    0.05f,
                    220f,
                    440f,
                    0.4f);
            try
            {
                Assert.That(
                    clip.frequency,
                    Is.EqualTo(
                        PrototypeAudioSynth.SampleRate));
                Assert.That(
                    clip.samples,
                    Is.GreaterThan(1));
                float[] samples =
                    ReadSamples(clip);
                bool hasAudibleSample = false;
                // 모든 샘플을 검사해 NaN·무한대·클리핑이 오디오 파이프라인으로 들어가지 않게 한다.
                for (int index = 0;
                     index < samples.Length;
                     index++)
                {
                    Assert.That(
                        float.IsNaN(samples[index]),
                        Is.False);
                    Assert.That(
                        float.IsInfinity(samples[index]),
                        Is.False);
                    Assert.That(
                        samples[index],
                        Is.InRange(-1f, 1f));
                    hasAudibleSample |=
                        Mathf.Abs(samples[index])
                        > 0.0001f;
                }

                Assert.That(
                    hasAudibleSample,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void NoiseBurst_SameSeedProducesSameWaveform()
        {
            // Unity 전역 난수와 무관하게 같은 시드는 같은 임시 타격음을 만들어야 한다.
            AudioClip first =
                PrototypeAudioSynth.CreateNoiseBurst(
                    "NoiseA",
                    0.04f,
                    87657,
                    0.3f);
            AudioClip second =
                PrototypeAudioSynth.CreateNoiseBurst(
                    "NoiseB",
                    0.04f,
                    87657,
                    0.3f);
            try
            {
                float[] firstSamples =
                    ReadSamples(first);
                float[] secondSamples =
                    ReadSamples(second);
                Assert.That(
                    secondSamples.Length,
                    Is.EqualTo(firstSamples.Length));
                // 샘플 단위 비교로 LCG 시드 계약이 바뀌는 회귀를 즉시 찾는다.
                for (int index = 0;
                     index < firstSamples.Length;
                     index++)
                {
                    Assert.That(
                        secondSamples[index],
                        Is.EqualTo(firstSamples[index])
                            .Within(0.000001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        private static float[] ReadSamples(
            AudioClip clip)
        {
            // Mono Clip의 전체 샘플을 배열로 복사해 네이티브 오디오 객체를 결정적으로 검사한다.
            var samples =
                new float[clip.samples];
            Assert.That(
                clip.GetData(samples, 0),
                Is.True);
            return samples;
        }
    }
}
