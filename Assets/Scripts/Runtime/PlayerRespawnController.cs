// GOLDEN STANDARD
// 목적: 플레이어 사망을 마지막 체크포인트에서의 일관된 재시작 흐름으로 변환한다.
// 책임: Health 사망을 구독하고 이동·전투를 잠근 뒤 재배치·회복·조작 복귀 순서를 실행한다.
// 불변식: 한 번에 하나의 재시작만 실행하며 완료 시 플레이어는 살아 있고 조작 가능한 상태다.
// 선택 이유: 재시작 순서를 한 컴포넌트가 소유하면 이동·전투·UI가 사망 이벤트에 각각 반응하는 경쟁을 막는다.
using System;
using System.Collections;
using UnityEngine;

namespace GameSkill
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerCheckpointState))]
    [RequireComponent(typeof(SideScrollerMotor))]
    [RequireComponent(typeof(PlayerCombat))]
    public sealed class PlayerRespawnController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float respawnDelay = 0.65f;

        private Health health;
        private PlayerCheckpointState checkpointState;
        private SideScrollerMotor motor;
        private PlayerCombat combat;
        private Coroutine respawnRoutine;
        private Vector3 initialSpawnPosition;

        public event Action<Vector3> Respawned;

        public bool IsRespawning { get; private set; }
        public int RespawnCount { get; private set; }

        private void Awake()
        {
            // 최초 위치와 협력 컴포넌트를 사망 전에 캐시해 씬 상태가 변해도 기준점을 보존한다.
            health = GetComponent<Health>();
            checkpointState = GetComponent<PlayerCheckpointState>();
            motor = GetComponent<SideScrollerMotor>();
            combat = GetComponent<PlayerCombat>();
            initialSpawnPosition = transform.position;
        }

        private void OnEnable()
        {
            // 활성화된 생존 컨트롤러만 사망 이벤트를 처리하도록 구독 수명을 맞춘다.
            health ??= GetComponent<Health>();
            health.Died += HandleDied;
        }

        private void OnDisable()
        {
            // 씬 종료나 컴포넌트 비활성화 후 오래된 사망 콜백이 남지 않도록 해제한다.
            if (health != null)
            {
                health.Died -= HandleDied;
            }

            if (respawnRoutine != null)
            {
                // 비활성 컴포넌트의 코루틴이 나중에 플레이어를 이동시키지 않게 중단한다.
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }

            IsRespawning = false;
        }

        public void Configure(float delay)
        {
            // 테스트와 프리팹이 음수 대기 시간을 만들지 않도록 공개 설정 경계에서 제한한다.
            respawnDelay = Mathf.Max(0f, delay);
        }

        private void HandleDied()
        {
            // 다중 데미지나 중복 이벤트가 들어와도 이미 진행 중인 재시작은 다시 시작하지 않는다.
            if (IsRespawning)
            {
                return;
            }

            respawnRoutine = StartCoroutine(RespawnSequence());
        }

        private IEnumerator RespawnSequence()
        {
            // 입력 잠금이 재배치보다 먼저 일어나야 사망 직후 한 프레임 이동하는 현상을 막는다.
            IsRespawning = true;
            motor.SetControlLocked(true);
            combat.enabled = false;

            // 0초 설정도 한 프레임은 양보해 사망 이벤트 구독자들이 상태를 관찰하게 한다.
            yield return respawnDelay > 0f
                ? new WaitForSeconds(respawnDelay)
                : null;

            Vector3 destination = RespawnMath.ResolveDestination(
                checkpointState.HasCheckpoint,
                checkpointState.LastRespawnPosition,
                initialSpawnPosition);
            motor.Teleport(destination);
            health.RestoreFullHealth();
            combat.enabled = true;
            motor.SetControlLocked(false);

            RespawnCount++;
            IsRespawning = false;
            respawnRoutine = null;
            Respawned?.Invoke(destination);
        }
    }
}
