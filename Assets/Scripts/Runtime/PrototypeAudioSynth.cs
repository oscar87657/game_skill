// GOLDEN STANDARD
// 목적: 외부 오디오 에셋 전에도 플레이 감각을 검증할 수 있는 결정적 임시 효과음을 생성한다.
// 책임: 톤 스윕과 노이즈 버스트의 샘플 계산, 엔벌로프 적용과 AudioClip 생성을 제공한다.
// 불변식: 생성 샘플은 항상 -1~1 범위이며 같은 인자는 같은 파형을 만든다.
// 선택 이유: 코드 생성 임시음은 판정 이벤트와 믹싱 구조를 먼저 검증하고 최종 음원으로 쉽게 교체할 수 있다.
using UnityEngine;

namespace GameSkill
{
    public static class PrototypeAudioSynth
    {
        public const int SampleRate = 22050;

        public static AudioClip CreateToneSweep(
            string clipName,
            float duration,
            float startFrequency,
            float endFrequency,
            float amplitude = 0.35f)
        {
            // 잘못된 길이·주파수·음량을 가청 범위의 안전한 값으로 제한한다.
            float safeDuration =
                Mathf.Max(0.01f, duration);
            float safeStartFrequency =
                Mathf.Max(20f, startFrequency);
            float safeEndFrequency =
                Mathf.Max(20f, endFrequency);
            float safeAmplitude =
                Mathf.Clamp01(amplitude);
            int sampleCount =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        safeDuration * SampleRate));
            var samples =
                new float[sampleCount];
            float phase = 0f;
            // 샘플마다 주파수를 보간하고 위상을 누적해 클릭 없는 짧은 톤 스윕을 만든다.
            for (int index = 0;
                 index < sampleCount;
                 index++)
            {
                float progress =
                    sampleCount <= 1
                        ? 1f
                        : (float)index
                            / (sampleCount - 1);
                float frequency =
                    Mathf.Lerp(
                        safeStartFrequency,
                        safeEndFrequency,
                        progress);
                phase +=
                    Mathf.PI
                    * 2f
                    * frequency
                    / SampleRate;
                float envelope =
                    Mathf.Sin(
                        Mathf.PI * progress);
                samples[index] =
                    Mathf.Clamp(
                        Mathf.Sin(phase)
                        * envelope
                        * safeAmplitude,
                        -1f,
                        1f);
            }

            return CreateClip(
                clipName,
                samples);
        }

        public static AudioClip CreateNoiseBurst(
            string clipName,
            float duration,
            int seed,
            float amplitude = 0.28f)
        {
            // 노이즈도 최소 길이와 안전한 음량을 적용해 빈 Clip이나 클리핑을 막는다.
            float safeDuration =
                Mathf.Max(0.01f, duration);
            float safeAmplitude =
                Mathf.Clamp01(amplitude);
            int sampleCount =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        safeDuration * SampleRate));
            var samples =
                new float[sampleCount];
            uint state =
                seed == 0
                    ? 1u
                    : unchecked((uint)seed);
            // 간단한 LCG를 사용해 UnityEngine.Random 전역 상태를 바꾸지 않는 재현 가능한 노이즈를 만든다.
            for (int index = 0;
                 index < sampleCount;
                 index++)
            {
                state =
                    state * 1664525u
                    + 1013904223u;
                float normalizedNoise =
                    ((state >> 8)
                        / 16777215f)
                    * 2f
                    - 1f;
                float progress =
                    sampleCount <= 1
                        ? 1f
                        : (float)index
                            / (sampleCount - 1);
                float envelope =
                    1f - progress;
                envelope *= envelope;
                samples[index] =
                    Mathf.Clamp(
                        normalizedNoise
                        * envelope
                        * safeAmplitude,
                        -1f,
                        1f);
            }

            return CreateClip(
                clipName,
                samples);
        }

        private static AudioClip CreateClip(
            string clipName,
            float[] samples)
        {
            // 계산과 Unity 객체 생성을 분리해 모든 합성 방식이 같은 Mono Clip 계약을 사용하게 한다.
            string safeName =
                string.IsNullOrWhiteSpace(clipName)
                    ? "PrototypeCue"
                    : clipName.Trim();
            AudioClip clip =
                AudioClip.Create(
                    safeName,
                    samples.Length,
                    1,
                    SampleRate,
                    false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
