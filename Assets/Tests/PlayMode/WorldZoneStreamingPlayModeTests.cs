// GOLDEN STANDARD
// 목적: Main Scene과 네 구역 콘텐츠 Scene의 실제 Additive 로드·언로드 흐름을 검증한다.
// 책임: 시작 구역 자동 로드, 다음 구역 전환, 이전 Scene 정리와 콘텐츠 루트를 확인한다.
// 불변식: 테스트 종료 시 자신이 로드한 Additive Scene을 언로드해 다음 테스트에 상태를 남기지 않는다.
// 선택 이유: 비동기 Scene API는 순수 상태 테스트만으로 검증할 수 없어 최소 통합 테스트를 별도로 둔다.
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameSkill.Tests
{
    public sealed class WorldZoneStreamingPlayModeTests
    {
        private const string StartScenePath =
            "Assets/Scenes/Zones/Zone_StartHall.unity";
        private const string TraversalScenePath =
            "Assets/Scenes/Zones/Zone_TraversalLab.unity";
        private const string BossScenePath =
            "Assets/Scenes/Zones/Zone_BossRoom.unity";

        [UnityTest]
        public IEnumerator MainScene_StreamsOneZoneContentSceneAtATime()
        {
            // 지속 오브젝트가 있는 Main을 단독 로드해 이전 테스트의 Additive Scene을 먼저 정리한다.
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            WorldZoneStreamController controller =
                Object.FindAnyObjectByType<WorldZoneStreamController>();
            Assert.That(controller, Is.Not.Null);

            // 비동기 시작 구역 로드가 완료될 때까지 제한된 프레임만 기다려 무한 대기를 막는다.
            for (int frame = 0;
                 frame < 180
                 && controller.LoadedZoneId != "start_hall";
                 frame++)
            {
                yield return null;
            }

            Assert.That(
                controller.LoadedZoneId,
                Is.EqualTo("start_hall"));
            Scene startScene =
                SceneManager.GetSceneByPath(StartScenePath);
            Assert.That(startScene.isLoaded, Is.True);
            Assert.That(
                FindRoot(startScene, "ZoneContent_StartHall"),
                Is.Not.Null);

            WorldZoneVolume traversalVolume =
                GameObject.Find("Zone_TraversalLab")
                    .GetComponent<WorldZoneVolume>();
            Assert.That(traversalVolume, Is.Not.Null);
            Assert.That(
                controller.RequestZone(traversalVolume.Zone),
                Is.True);

            // 새 Scene 로드와 이전 Scene 언로드가 모두 끝날 때까지 전환 상태를 관찰한다.
            for (int frame = 0;
                 frame < 180
                 && (controller.IsTransitioning
                    || controller.LoadedZoneId
                        != "traversal_lab");
                 frame++)
            {
                yield return null;
            }

            Assert.That(
                controller.LoadedZoneId,
                Is.EqualTo("traversal_lab"));
            Assert.That(
                SceneManager.GetSceneByPath(StartScenePath).isLoaded,
                Is.False);
            Scene traversalScene =
                SceneManager.GetSceneByPath(TraversalScenePath);
            Assert.That(traversalScene.isLoaded, Is.True);
            Assert.That(
                FindRoot(
                    traversalScene,
                    "ZoneContent_TraversalLab"),
                Is.Not.Null);

            WorldZoneVolume bossVolume =
                GameObject.Find("Zone_BossRoom")
                    .GetComponent<WorldZoneVolume>();
            Assert.That(bossVolume, Is.Not.Null);
            Assert.That(
                controller.RequestZone(bossVolume.Zone),
                Is.True);

            // 보스 전용 Scene까지 전환해 새 네 번째 바인딩이 실제 빌드 목록에서 로드되는지 확인한다.
            for (int frame = 0;
                 frame < 180
                 && (controller.IsTransitioning
                    || controller.LoadedZoneId
                        != "boss_room");
                 frame++)
            {
                yield return null;
            }

            Assert.That(
                controller.LoadedZoneId,
                Is.EqualTo("boss_room"));
            Assert.That(traversalScene.isLoaded, Is.False);
            Scene bossScene =
                SceneManager.GetSceneByPath(BossScenePath);
            Assert.That(bossScene.isLoaded, Is.True);
            Assert.That(
                FindRoot(
                    bossScene,
                    "ZoneContent_BossRoom"),
                Is.Not.Null);

            AsyncOperation cleanup =
                SceneManager.UnloadSceneAsync(bossScene);
            // 테스트가 로드한 콘텐츠를 완전히 제거한 뒤 다음 테스트에 제어권을 넘긴다.
            while (cleanup != null && !cleanup.isDone)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator BoundaryOverlap_KeepsPlayerAndCameraInTraversal()
        {
            // 실제 CharacterController가 맞닿은 두 Trigger에 동시에 걸리는 사용자 재현 위치를 검증한다.
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameObject player = GameObject.Find("Player");
            SideScrollerMotor motor =
                player.GetComponent<SideScrollerMotor>();
            PlayerWorldState worldState =
                player.GetComponent<PlayerWorldState>();
            SideScrollerCamera sideScrollerCamera =
                Camera.main.GetComponent<SideScrollerCamera>();
            WorldZoneVolume startVolume =
                GameObject.Find("Zone_StartHall")
                    .GetComponent<WorldZoneVolume>();
            WorldZoneVolume traversalVolume =
                GameObject.Find("Zone_TraversalLab")
                    .GetComponent<WorldZoneVolume>();

            Assert.That(
                traversalVolume.Enter(worldState),
                Is.True);
            Assert.That(
                sideScrollerCamera.ActiveZoneId,
                Is.EqualTo("traversal_lab"));

            motor.Teleport(
                new Vector3(6.4f, 0.05f, 0f));
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(
                worldState.CurrentZone.Id,
                Is.EqualTo("traversal_lab"));
            Assert.That(
                sideScrollerCamera.ActiveZoneId,
                Is.EqualTo("traversal_lab"));

            // 시작 홀 안쪽 임계점까지 이동한 뒤에만 구역·카메라가 함께 전환되어야 한다.
            motor.Teleport(
                new Vector3(6f, 0.05f, 0f));
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(
                worldState.CurrentZone.Id,
                Is.EqualTo("start_hall"));
            Assert.That(
                sideScrollerCamera.ActiveZoneId,
                Is.EqualTo("start_hall"));
            Assert.That(startVolume.HorizontalEntryInset, Is.EqualTo(0.45f));
        }

        private static GameObject FindRoot(
            Scene scene,
            string rootName)
        {
            // Additive Scene의 루트만 검색해 Main에 같은 이름이 있어도 잘못 통과하지 않게 한다.
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
