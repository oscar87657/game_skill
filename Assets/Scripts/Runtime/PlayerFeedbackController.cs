// GOLDEN STANDARD
// 목적: 플레이어 이동·전투·진행 이벤트를 짧은 VFX와 임시 SFX로 번역한다.
// 책임: 이벤트 구독, 대시 Trail, 방향성 파티클, 코드 생성 효과음 재생과 수명 정리를 담당한다.
// 불변식: 피드백은 게임 판정과 상태를 변경하지 않고 확정된 이벤트를 표현하기만 한다.
// 선택 이유: 판정과 표현을 분리하면 최종 모델·애니메이션·음원 교체가 전투 로직에 영향을 주지 않는다.
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    public sealed class PlayerFeedbackController : MonoBehaviour
    {
        [SerializeField]
        private SideScrollerMotor motor;
        [SerializeField]
        private PlayerCombat combat;
        [SerializeField] private Health health;
        [SerializeField]
        private PlayerAbilityState abilityState;
        [SerializeField]
        private TrailRenderer dashTrail;
        [SerializeField]
        private ParticleSystem feedbackParticles;
        [SerializeField]
        private AudioSource audioSource;

        private AudioClip dashClip;
        private AudioClip attackClip;
        private AudioClip hitClip;
        private AudioClip hurtClip;
        private AudioClip abilityClip;
        private bool isSubscribed;

        public bool IsConfigured =>
            motor != null
            && combat != null
            && health != null
            && abilityState != null
            && dashTrail != null
            && feedbackParticles != null
            && audioSource != null;
        public int PlayedCueCount { get; private set; }

        private void Awake()
        {
            // 첫 이벤트 전에 같은 플레이어의 상태 참조와 런타임 합성 Clip을 준비한다.
            CacheComponents();
            EnsureAudioClips();
            SetTrailEmission(false);
        }

        private void OnEnable()
        {
            // 활성 플레이어만 판정 이벤트를 표현하도록 필요한 참조를 확인하고 구독한다.
            CacheComponents();
            EnsureAudioClips();
            Subscribe();
        }

        private void Update()
        {
            // Trail의 생성 여부만 현재 대시 상태에 맞추고 위치·이동 계산은 Motor에 맡긴다.
            SetTrailEmission(
                motor != null
                && motor.IsDashing);
        }

        private void OnDisable()
        {
            // 비활성 플레이어가 이전 이벤트를 계속 받거나 Trail을 남기지 않도록 정리한다.
            Unsubscribe();
            SetTrailEmission(false);
        }

        private void OnDestroy()
        {
            // 코드로 만든 AudioClip은 에셋이 아니므로 플레이어 수명 종료 때 명시적으로 해제한다.
            DestroyRuntimeClip(
                ref dashClip);
            DestroyRuntimeClip(
                ref attackClip);
            DestroyRuntimeClip(
                ref hitClip);
            DestroyRuntimeClip(
                ref hurtClip);
            DestroyRuntimeClip(
                ref abilityClip);
        }

        public bool Configure(
            SideScrollerMotor playerMotor,
            PlayerCombat playerCombat,
            Health playerHealth,
            PlayerAbilityState playerAbilityState,
            TrailRenderer playerDashTrail,
            ParticleSystem playerFeedbackParticles,
            AudioSource playerAudioSource)
        {
            // 빌더 재실행이 같은 표현 참조를 다시 전달할 때 Scene Dirty 여부를 정확히 반환한다.
            bool changed =
                motor != playerMotor
                || combat != playerCombat
                || health != playerHealth
                || abilityState
                    != playerAbilityState
                || dashTrail
                    != playerDashTrail
                || feedbackParticles
                    != playerFeedbackParticles
                || audioSource
                    != playerAudioSource;

            Unsubscribe();
            motor = playerMotor;
            combat = playerCombat;
            health = playerHealth;
            abilityState =
                playerAbilityState;
            dashTrail =
                playerDashTrail;
            feedbackParticles =
                playerFeedbackParticles;
            audioSource =
                playerAudioSource;
            SetTrailEmission(false);
            Subscribe();
            return changed;
        }

        private void CacheComponents()
        {
            // EditMode 구성과 런타임 역직렬화 양쪽에서 누락된 동일 루트 참조만 보완한다.
            motor ??=
                GetComponent<SideScrollerMotor>();
            combat ??=
                GetComponent<PlayerCombat>();
            health ??=
                GetComponent<Health>();
            abilityState ??=
                GetComponent<PlayerAbilityState>();
            audioSource ??=
                GetComponent<AudioSource>();
        }

        private void Subscribe()
        {
            // Configure와 OnEnable이 연속 호출돼도 같은 피드백이 두 번 재생되지 않게 한다.
            if (isSubscribed)
            {
                return;
            }

            if (motor != null)
            {
                motor.DashStarted +=
                    HandleDashStarted;
            }

            if (combat != null)
            {
                combat.AttackStarted +=
                    HandleAttackStarted;
                combat.HitConfirmed +=
                    HandleHitConfirmed;
            }

            if (health != null)
            {
                health.Damaged +=
                    HandleDamaged;
            }

            if (abilityState != null)
            {
                abilityState.AbilityUnlocked +=
                    HandleAbilityUnlocked;
            }

            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            // 부분 구성된 Presenter도 등록된 이벤트만 안전하게 해제한다.
            if (!isSubscribed)
            {
                return;
            }

            if (motor != null)
            {
                motor.DashStarted -=
                    HandleDashStarted;
            }

            if (combat != null)
            {
                combat.AttackStarted -=
                    HandleAttackStarted;
                combat.HitConfirmed -=
                    HandleHitConfirmed;
            }

            if (health != null)
            {
                health.Damaged -=
                    HandleDamaged;
            }

            if (abilityState != null)
            {
                abilityState.AbilityUnlocked -=
                    HandleAbilityUnlocked;
            }

            isSubscribed = false;
        }

        private void HandleDashStarted(
            float direction)
        {
            // 대시 반대편으로 짧은 파편을 뿌려 속도 방향을 명확하게 보여 준다.
            EmitDirectionalBurst(
                feedbackParticles,
                transform.position
                    + Vector3.up * 0.75f,
                new Vector3(
                    -Mathf.Sign(direction),
                    0.15f,
                    0f),
                8,
                2.8f,
                new Color(
                    0.25f,
                    0.9f,
                    1f,
                    0.9f),
                0.13f,
                0.22f);
            PlayCue(dashClip, 0.7f);
        }

        private void HandleAttackStarted(
            int comboStep)
        {
            // 콤보 단계가 높아질수록 파티클 수와 속도를 조금 늘려 판정 변경 없이 강도를 표현한다.
            float facing =
                motor != null
                    ? motor.FacingDirection
                    : 1f;
            EmitDirectionalBurst(
                feedbackParticles,
                transform.position
                    + Vector3.up * 0.9f
                    + Vector3.right
                        * facing
                        * 0.55f,
                new Vector3(
                    facing,
                    0.08f,
                    0f),
                5 + Mathf.Max(1, comboStep),
                2.6f
                    + comboStep * 0.25f,
                new Color(
                    1f,
                    0.72f,
                    0.18f,
                    0.95f),
                0.16f,
                0.2f);
            PlayCue(
                attackClip,
                0.5f
                    + comboStep * 0.06f);
        }

        private void HandleHitConfirmed(
            Vector3 hitPosition)
        {
            // Health가 승인한 타격 위치에만 방사형 파편과 타격음을 재생한다.
            EmitRadialBurst(
                feedbackParticles,
                hitPosition,
                10,
                3.6f,
                new Color(
                    1f,
                    0.32f,
                    0.12f,
                    1f),
                0.14f,
                0.18f);
            PlayCue(hitClip, 0.85f);
        }

        private void HandleDamaged(
            int current,
            int maximum)
        {
            // 플레이어 중심의 붉은 파편으로 피격을 표시하고 체력 계산에는 관여하지 않는다.
            EmitRadialBurst(
                feedbackParticles,
                transform.position
                    + Vector3.up * 0.9f,
                12,
                2.8f,
                new Color(
                    1f,
                    0.08f,
                    0.18f,
                    0.95f),
                0.15f,
                0.24f);
            PlayCue(hurtClip, 0.8f);
        }

        private void HandleAbilityUnlocked(
            AbilityDefinition ability)
        {
            // 능력 종류와 무관하게 공통 상승 파편과 확인음을 사용해 획득 이벤트를 강조한다.
            EmitDirectionalBurst(
                feedbackParticles,
                transform.position
                    + Vector3.up * 0.75f,
                Vector3.up,
                18,
                3.2f,
                new Color(
                    0.2f,
                    1f,
                    0.62f,
                    0.95f),
                0.17f,
                0.42f);
            PlayCue(abilityClip, 0.9f);
        }

        private static void EmitDirectionalBurst(
            ParticleSystem particles,
            Vector3 position,
            Vector3 direction,
            int count,
            float speed,
            Color color,
            float size,
            float lifetime)
        {
            // 참조가 없는 선택 VFX는 게임 판정을 방해하지 않고 조용히 생략한다.
            if (particles == null
                || count <= 0)
            {
                return;
            }

            Vector3 normalizedDirection =
                direction.sqrMagnitude
                    > 0.0001f
                    ? direction.normalized
                    : Vector3.right;
            // 가운데 방향을 기준으로 Y축 속도를 분산해 짧은 부채꼴 버스트를 만든다.
            for (int index = 0;
                 index < count;
                 index++)
            {
                float offset =
                    index - (count - 1) * 0.5f;
                var emit =
                    new ParticleSystem.EmitParams
                    {
                        position = position,
                        velocity =
                            normalizedDirection
                                * speed
                            + Vector3.up
                                * offset
                                * 0.12f,
                        startColor = color,
                        startSize = size,
                        startLifetime = lifetime
                    };
                particles.Emit(emit, 1);
            }
        }

        private static void EmitRadialBurst(
            ParticleSystem particles,
            Vector3 position,
            int count,
            float speed,
            Color color,
            float size,
            float lifetime)
        {
            // 참조가 없거나 개수가 0인 경우 파티클 모듈 호출을 생략한다.
            if (particles == null
                || count <= 0)
            {
                return;
            }

            // 동일한 각도 간격으로 X/Y 평면에 배치해 카메라에서 읽기 쉬운 방사형 피드백을 만든다.
            for (int index = 0;
                 index < count;
                 index++)
            {
                float angle =
                    Mathf.PI
                    * 2f
                    * index
                    / count;
                var emit =
                    new ParticleSystem.EmitParams
                    {
                        position = position,
                        velocity =
                            new Vector3(
                                Mathf.Cos(angle),
                                Mathf.Sin(angle),
                                0f)
                            * speed,
                        startColor = color,
                        startSize = size,
                        startLifetime = lifetime
                    };
                particles.Emit(emit, 1);
            }
        }

        private void PlayCue(
            AudioClip clip,
            float volumeScale)
        {
            // 음원이 준비된 경우에만 OneShot으로 재생해 연속 이벤트가 서로의 Clip을 끊지 않게 한다.
            if (audioSource == null
                || clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(
                clip,
                Mathf.Clamp01(volumeScale));
            PlayedCueCount++;
        }

        private void EnsureAudioClips()
        {
            // 에디터 씬에는 런타임 생성 객체를 저장하지 않고 실제 Play 수명에서 한 번만 합성한다.
            if (!Application.isPlaying
                || dashClip != null)
            {
                return;
            }

            dashClip =
                PrototypeAudioSynth.CreateNoiseBurst(
                    "Dash_Cue",
                    0.11f,
                    104729,
                    0.24f);
            attackClip =
                PrototypeAudioSynth.CreateToneSweep(
                    "Attack_Cue",
                    0.09f,
                    210f,
                    105f,
                    0.3f);
            hitClip =
                PrototypeAudioSynth.CreateNoiseBurst(
                    "Hit_Cue",
                    0.07f,
                    161803,
                    0.34f);
            hurtClip =
                PrototypeAudioSynth.CreateToneSweep(
                    "Hurt_Cue",
                    0.16f,
                    120f,
                    55f,
                    0.34f);
            abilityClip =
                PrototypeAudioSynth.CreateToneSweep(
                    "Ability_Cue",
                    0.28f,
                    520f,
                    1040f,
                    0.28f);
        }

        private void SetTrailEmission(
            bool shouldEmit)
        {
            // 상태가 바뀔 때만 TrailRenderer를 갱신해 불필요한 네이티브 프로퍼티 쓰기를 줄인다.
            if (dashTrail != null
                && dashTrail.emitting
                    != shouldEmit)
            {
                dashTrail.emitting =
                    shouldEmit;
            }
        }

        private static void DestroyRuntimeClip(
            ref AudioClip clip)
        {
            // 생성되지 않았거나 이미 정리된 Clip은 다시 파괴하지 않는다.
            if (clip == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(clip);
            }
            else
            {
                Object.DestroyImmediate(clip);
            }

            clip = null;
        }
    }
}
