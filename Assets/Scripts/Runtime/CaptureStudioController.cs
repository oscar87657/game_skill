// GOLDEN STANDARD
// 목적: 포트폴리오 촬영 중 기능별 무대로 즉시 이동하고 같은 장면을 빠르게 다시 준비한다.
// 책임: 촬영용 능력을 해금하고 숫자 키 이동·씬 초기화 입력을 플레이어 이동 시스템에 전달한다.
// 불변식: 1~8번은 직렬화된 같은 순서의 앵커로 이동하며 0번은 현재 촬영 씬만 다시 불러온다.
// 선택 이유: 실제 게임 로직은 재사용하고 촬영 편의 기능만 별도 컴포넌트에 격리해 본편 동작을 오염시키지 않는다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GameSkill
{
    [DisallowMultipleComponent]
    public sealed class CaptureStudioController : MonoBehaviour
    {
        [SerializeField] private SideScrollerMotor motor;
        [SerializeField] private PlayerAbilityState abilityState;
        [SerializeField]
        private List<AbilityDefinition> captureAbilities = new();
        [SerializeField]
        private List<Transform> zoneAnchors = new();

        public int ZoneCount => zoneAnchors.Count;
        public bool IsConfigured =>
            motor != null
            && abilityState != null
            && zoneAnchors.Count == 8;

        private void Start()
        {
            // 촬영 씬에서는 진행 저장과 무관하게 모든 이동 기능을 즉시 사용할 수 있게 한다.
            UnlockCaptureAbilities();
            TeleportToZone(0);
            Debug.Log(
                "촬영 스튜디오: 1~8 구역 이동 / 0 전체 초기화",
                this);
        }

        private void Update()
        {
            // Input System이 준비되지 않은 프레임은 입력을 건너뛰어 장치 연결 상태와 무관하게 실행한다.
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            int requestedZone = ReadRequestedZone(keyboard);
            if (requestedZone >= 0)
            {
                TeleportToZone(requestedZone);
            }

            // 적 처치·체력·투사체까지 처음 상태로 되돌려 같은 영상을 반복 촬영하게 한다.
            if (keyboard.digit0Key.wasPressedThisFrame)
            {
                ReloadStudio();
            }
        }

        public void Configure(
            SideScrollerMotor playerMotor,
            PlayerAbilityState playerAbilityState,
            IEnumerable<AbilityDefinition> abilities,
            IEnumerable<Transform> anchors)
        {
            // 에디터 빌더가 만든 참조를 복사해 원본 컬렉션 변경이 씬 설정에 영향을 주지 않게 한다.
            motor = playerMotor;
            abilityState = playerAbilityState;
            captureAbilities.Clear();
            zoneAnchors.Clear();

            if (abilities != null)
            {
                // 촬영에 필요한 능력 정의를 직렬화 목록에 순서대로 보관한다.
                foreach (AbilityDefinition ability in abilities)
                {
                    if (ability != null)
                    {
                        captureAbilities.Add(ability);
                    }
                }
            }

            if (anchors != null)
            {
                // 숫자 키 번호와 촬영 무대 순서가 항상 같도록 앵커를 전달 순서대로 저장한다.
                foreach (Transform anchor in anchors)
                {
                    if (anchor != null)
                    {
                        zoneAnchors.Add(anchor);
                    }
                }
            }
        }

        public bool TeleportToZone(int zoneIndex)
        {
            // 잘못된 키 번호나 누락된 플레이어 참조는 현재 촬영 상태를 바꾸지 않는다.
            if (motor == null
                || zoneIndex < 0
                || zoneIndex >= zoneAnchors.Count
                || zoneAnchors[zoneIndex] == null)
            {
                return false;
            }

            motor.SetControlLocked(false);
            motor.Teleport(zoneAnchors[zoneIndex].position);
            return true;
        }

        private void UnlockCaptureAbilities()
        {
            // 본편의 능력 획득 순서는 그대로 두고 이 전용 씬에서만 등록된 기능을 모두 연다.
            if (abilityState == null)
            {
                return;
            }

            foreach (AbilityDefinition ability in captureAbilities)
            {
                abilityState.TryUnlock(ability);
            }
        }

        private static int ReadRequestedZone(Keyboard keyboard)
        {
            // 숫자 행을 기능별 촬영 무대와 직접 대응시켜 별도 UI 없이 빠르게 이동한다.
            if (keyboard.digit1Key.wasPressedThisFrame) return 0;
            if (keyboard.digit2Key.wasPressedThisFrame) return 1;
            if (keyboard.digit3Key.wasPressedThisFrame) return 2;
            if (keyboard.digit4Key.wasPressedThisFrame) return 3;
            if (keyboard.digit5Key.wasPressedThisFrame) return 4;
            if (keyboard.digit6Key.wasPressedThisFrame) return 5;
            if (keyboard.digit7Key.wasPressedThisFrame) return 6;
            if (keyboard.digit8Key.wasPressedThisFrame) return 7;
            return -1;
        }

        private static void ReloadStudio()
        {
            // 현재 활성 씬의 경로만 다시 불러와 본편이나 다른 열린 씬을 건드리지 않는다.
            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                SceneManager.LoadScene(activeScene.path);
            }
        }
    }
}
