// GOLDEN STANDARD
// 목적: 체크포인트의 활성화·저장 복원·초기화 계약을 월드 씬 없이 검증한다.
// 책임: ID·좌표 검증, 활성화 회복과 이벤트, 부작용 없는 저장 복원과 초기화를 확인한다.
// 불변식: 각 테스트는 독립 GameObject를 사용하고 영구 파일이나 Scene 상태를 변경하지 않는다.
// 선택 이유: 실패 복구의 기준점을 이동 수식 테스트와 분리해 상태 수명 차이를 드러낸다.
using NUnit.Framework;
using UnityEngine;

namespace GameSkill.Tests
{
    public sealed class CheckpointSystemTests
    {
        [Test]
        public void ActivateCheckpoint_RecordsPositionAndRestoresHealth()
        {
            // 플레이 중 실제 접촉은 위치 기록·회복·활성 이벤트를 하나의 원자적 결과로 만들어야 한다.
            var player = new GameObject("CheckpointStateTestPlayer");
            try
            {
                Health health = player.AddComponent<Health>();
                health.Configure(5);
                Assert.That(health.TakeDamage(3), Is.True);
                PlayerCheckpointState checkpointState =
                    player.AddComponent<PlayerCheckpointState>();
                int activationEvents = 0;
                int restorationEvents = 0;
                checkpointState.CheckpointActivated += (_, _) =>
                    activationEvents++;
                health.Restored += (_, _) => restorationEvents++;
                Vector3 respawnPosition = new(4f, 1.05f, 0f);

                bool activated = checkpointState.ActivateCheckpoint(
                    "test_hall",
                    respawnPosition);

                Assert.That(activated, Is.True);
                Assert.That(checkpointState.HasCheckpoint, Is.True);
                Assert.That(
                    checkpointState.LastCheckpointId,
                    Is.EqualTo("test_hall"));
                Assert.That(
                    checkpointState.LastRespawnPosition,
                    Is.EqualTo(respawnPosition));
                Assert.That(health.CurrentHealth, Is.EqualTo(5));
                Assert.That(activationEvents, Is.EqualTo(1));
                Assert.That(restorationEvents, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        public void ActivateCheckpoint_RejectsEmptyId(string checkpointId)
        {
            // 영구 저장 키로 사용할 수 없는 빈 ID는 상태와 체력을 부분 변경하면 안 된다.
            var player = new GameObject("InvalidCheckpointTestPlayer");
            try
            {
                Health health = player.AddComponent<Health>();
                health.Configure(5);
                health.TakeDamage(2);
                PlayerCheckpointState checkpointState =
                    player.AddComponent<PlayerCheckpointState>();

                bool activated = checkpointState.ActivateCheckpoint(
                    checkpointId,
                    Vector3.zero);

                Assert.That(activated, Is.False);
                Assert.That(checkpointState.HasCheckpoint, Is.False);
                Assert.That(health.CurrentHealth, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ActivateCheckpoint_RejectsInvalidPosition()
        {
            // NaN 좌표가 들어오면 다음 부활과 저장이 깨지므로 활성화 경계에서 거부한다.
            var player = new GameObject("InvalidCheckpointPositionTestPlayer");
            try
            {
                Health health = player.AddComponent<Health>();
                health.Configure(3);
                PlayerCheckpointState checkpointState =
                    player.AddComponent<PlayerCheckpointState>();

                bool activated = checkpointState.ActivateCheckpoint(
                    "invalid_position",
                    new Vector3(float.NaN, 0f, 0f));

                Assert.That(activated, Is.False);
                Assert.That(checkpointState.HasCheckpoint, Is.False);
                Assert.That(
                    health.CurrentHealth,
                    Is.EqualTo(health.MaxHealth));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void RestoreCheckpoint_DoesNotHealOrPublishActivation()
        {
            // 저장 불러오기는 과거 접촉을 재현하는 작업이 아니므로 회복과 활성화 이벤트를 반복하면 안 된다.
            var player = new GameObject("CheckpointRestoreTestPlayer");
            try
            {
                Health health = player.AddComponent<Health>();
                health.Configure(5);
                health.TakeDamage(2);
                PlayerCheckpointState checkpointState =
                    player.AddComponent<PlayerCheckpointState>();
                int activationEvents = 0;
                checkpointState.CheckpointActivated += (_, _) =>
                    activationEvents++;

                bool restored = checkpointState.RestoreCheckpoint(
                    "saved_hall",
                    new Vector3(8f, 2f, 0f));

                Assert.That(restored, Is.True);
                Assert.That(health.CurrentHealth, Is.EqualTo(3));
                Assert.That(activationEvents, Is.Zero);
                Assert.That(
                    checkpointState.LastCheckpointId,
                    Is.EqualTo("saved_hall"));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ClearCheckpoint_RemovesAllRespawnState()
        {
            // 새 게임 데이터를 적용할 때 과거 ID나 좌표가 남으면 이전 세션으로 부활할 수 있다.
            var player = new GameObject("CheckpointClearTestPlayer");
            try
            {
                player.AddComponent<Health>();
                PlayerCheckpointState checkpointState =
                    player.AddComponent<PlayerCheckpointState>();
                checkpointState.RestoreCheckpoint(
                    "old_hall",
                    new Vector3(3f, 1f, 0f));

                checkpointState.ClearCheckpoint();

                Assert.That(checkpointState.HasCheckpoint, Is.False);
                Assert.That(checkpointState.LastCheckpointId, Is.Empty);
                Assert.That(
                    checkpointState.LastRespawnPosition,
                    Is.EqualTo(Vector3.zero));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
