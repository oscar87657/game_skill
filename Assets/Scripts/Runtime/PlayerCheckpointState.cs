// GOLDEN STANDARD
// 목적: 플레이어가 마지막으로 활성화한 체크포인트와 재시작 위치를 소유한다.
// 책임: 체크포인트 ID·위치를 검증해 기록하고 활성화 순간 Health를 완전히 회복한다.
// 불변식: HasCheckpoint가 참이면 ID는 비어 있지 않으며 위치는 마지막 활성화 지점을 나타낸다.
// 선택 이유: 진행 상태를 체크포인트 오브젝트가 아닌 플레이어에 두어 사망·저장 시스템이 재사용하게 한다.
using System;
using UnityEngine;

namespace GameSkill
{
    [RequireComponent(typeof(Health))]
    public sealed class PlayerCheckpointState : MonoBehaviour
    {
        private Health health;

        public event Action<string, Vector3> CheckpointActivated;

        public bool HasCheckpoint { get; private set; }
        public string LastCheckpointId { get; private set; } = string.Empty;
        public Vector3 LastRespawnPosition { get; private set; }

        private void Awake()
        {
            // 활성화 때마다 검색하지 않도록 필수 체력 컴포넌트를 한 번 캐시한다.
            health = GetComponent<Health>();
        }

        public bool ActivateCheckpoint(
            string checkpointId,
            Vector3 respawnPosition)
        {
            // 빈 ID를 허용하면 저장 데이터에서 서로 다른 지점을 구분할 수 없으므로 거부한다.
            if (string.IsNullOrWhiteSpace(checkpointId)
                || !IsFinite(respawnPosition))
            {
                return false;
            }

            health ??= GetComponent<Health>();
            if (health == null)
            {
                // RequireComponent가 훼손된 씬에서도 NullReference 대신 명시적으로 실패한다.
                return false;
            }

            HasCheckpoint = true;
            LastCheckpointId = checkpointId.Trim();
            LastRespawnPosition = respawnPosition;
            health.RestoreFullHealth();
            CheckpointActivated?.Invoke(
                LastCheckpointId,
                LastRespawnPosition);
            return true;
        }

        public bool RestoreCheckpoint(
            string checkpointId,
            Vector3 respawnPosition)
        {
            // 저장 복원은 체크포인트 접촉 효과인 체력 회복과 활성화 이벤트를 다시 발생시키지 않는다.
            if (string.IsNullOrWhiteSpace(checkpointId)
                || !IsFinite(respawnPosition))
            {
                return false;
            }

            HasCheckpoint = true;
            LastCheckpointId = checkpointId.Trim();
            LastRespawnPosition = respawnPosition;
            return true;
        }

        public void ClearCheckpoint()
        {
            // 체크포인트가 없는 새 저장 데이터를 적용할 때 이전 런타임 값을 남기지 않는다.
            HasCheckpoint = false;
            LastCheckpointId = string.Empty;
            LastRespawnPosition = Vector3.zero;
        }

        private static bool IsFinite(Vector3 position)
        {
            // NaN이나 무한대 좌표를 저장하면 다음 단계의 재시작 이동이 영구적으로 깨질 수 있다.
            return !float.IsNaN(position.x)
                && !float.IsNaN(position.y)
                && !float.IsNaN(position.z)
                && !float.IsInfinity(position.x)
                && !float.IsInfinity(position.y)
                && !float.IsInfinity(position.z);
        }
    }
}
