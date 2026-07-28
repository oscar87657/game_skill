// GOLDEN STANDARD
// 목적: Scene 배치와 무관하게 Pause 메뉴의 전역 정지와 음량 옵션 계약을 검증한다.
// 책임: 시간·오디오 정지 및 복원, 메뉴 표시, Pause 액션과 음량 제한을 확인한다.
// 불변식: 테스트가 변경한 TimeScale, AudioListener와 PlayerPrefs 값은 종료 전에 원상 복구한다.
// 선택 이유: 전역 상태를 다루는 기능은 작은 회귀 테스트로 종료 경로의 복원 누락을 방지해야 한다.
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameSkill.Tests
{
    public sealed class PauseMenuControllerTests
    {
        [Test]
        public void Controller_PausesRestoresAndPersistsClampedVolume()
        {
            // 전역 상태와 기존 사용자 옵션을 먼저 보관해 성공·실패와 관계없이 테스트 뒤 복원한다.
            float originalTimeScale =
                Time.timeScale;
            bool originalAudioPause =
                AudioListener.pause;
            float originalVolume =
                AudioListener.volume;
            bool hadStoredVolume =
                PlayerPrefs.HasKey(
                    PauseMenuController
                        .MasterVolumePreferenceKey);
            float storedVolume =
                PlayerPrefs.GetFloat(
                    PauseMenuController
                        .MasterVolumePreferenceKey,
                    1f);
            var controllerObject =
                new GameObject(
                    "PauseMenuControllerTest");
            var inputObject =
                new GameObject(
                    "PauseMenuInputTest");
            var menuRoot =
                new GameObject(
                    "PauseMenuRootTest");
            InputActionAsset inputActions =
                ScriptableObject
                    .CreateInstance<InputActionAsset>();
            try
            {
                InputActionMap playerMap =
                    inputActions.AddActionMap("Player");
                playerMap
                    .AddAction(
                        "Pause",
                        InputActionType.Button)
                    .AddBinding(
                        "<Keyboard>/escape");
                PlayerInput playerInput =
                    inputObject
                        .AddComponent<PlayerInput>();
                playerInput.actions =
                    inputActions;
                playerInput.defaultActionMap =
                    "Player";

                GameProgressHud progressHud =
                    controllerObject
                        .AddComponent<GameProgressHud>();
                Button resumeButton =
                    CreateButton(
                        "Resume",
                        controllerObject.transform);
                Button saveButton =
                    CreateButton(
                        "Save",
                        controllerObject.transform);
                Button loadButton =
                    CreateButton(
                        "Load",
                        controllerObject.transform);
                Button volumeDownButton =
                    CreateButton(
                        "VolumeDown",
                        controllerObject.transform);
                Button volumeUpButton =
                    CreateButton(
                        "VolumeUp",
                        controllerObject.transform);
                Text volumeLabel =
                    CreateText(
                        "VolumeLabel",
                        controllerObject.transform);
                Text statusLabel =
                    CreateText(
                        "StatusLabel",
                        controllerObject.transform);
                PauseMenuController controller =
                    controllerObject
                        .AddComponent<PauseMenuController>();
                controller.Configure(
                    menuRoot,
                    playerInput,
                    progressHud,
                    resumeButton,
                    saveButton,
                    loadButton,
                    volumeDownButton,
                    volumeUpButton,
                    volumeLabel,
                    statusLabel);

                Assert.That(
                    controller.IsConfigured,
                    Is.True);
                Assert.That(
                    controller.HasPauseAction,
                    Is.True);
                Assert.That(
                    menuRoot.activeSelf,
                    Is.False);

                Time.timeScale = 0.65f;
                AudioListener.pause = false;
                controller.SetPaused(true);

                Assert.That(
                    controller.IsPaused,
                    Is.True);
                Assert.That(
                    Time.timeScale,
                    Is.Zero);
                Assert.That(
                    AudioListener.pause,
                    Is.True);
                Assert.That(
                    menuRoot.activeSelf,
                    Is.True);
                Assert.That(
                    controller.StatusText,
                    Is.EqualTo("PAUSED"));

                controller.Resume();

                Assert.That(
                    controller.IsPaused,
                    Is.False);
                Assert.That(
                    Time.timeScale,
                    Is.EqualTo(0.65f)
                        .Within(0.001f));
                Assert.That(
                    AudioListener.pause,
                    Is.False);
                Assert.That(
                    menuRoot.activeSelf,
                    Is.False);

                controller.SetMasterVolume(0.34f);
                Assert.That(
                    controller.MasterVolume,
                    Is.EqualTo(0.34f)
                        .Within(0.001f));
                Assert.That(
                    controller.VolumeLabelText,
                    Is.EqualTo("VOLUME 34%"));
                controller.SetMasterVolume(-2f);
                Assert.That(
                    controller.MasterVolume,
                    Is.Zero);
                controller.SetMasterVolume(2f);
                Assert.That(
                    controller.MasterVolume,
                    Is.EqualTo(1f));
                Assert.That(
                    PlayerPrefs.GetFloat(
                        PauseMenuController
                            .MasterVolumePreferenceKey),
                    Is.EqualTo(1f));
            }
            finally
            {
                // 테스트가 소유한 객체를 제거한 뒤 기존 전역 설정과 사용자 옵션을 정확히 되돌린다.
                Object.DestroyImmediate(
                    controllerObject);
                Object.DestroyImmediate(
                    inputObject);
                Object.DestroyImmediate(
                    menuRoot);
                Object.DestroyImmediate(
                    inputActions);
                Time.timeScale =
                    originalTimeScale;
                AudioListener.pause =
                    originalAudioPause;
                AudioListener.volume =
                    originalVolume;
                if (hadStoredVolume)
                {
                    PlayerPrefs.SetFloat(
                        PauseMenuController
                            .MasterVolumePreferenceKey,
                        storedVolume);
                }
                else
                {
                    PlayerPrefs.DeleteKey(
                        PauseMenuController
                            .MasterVolumePreferenceKey);
                }
            }
        }

        private static Button CreateButton(
            string objectName,
            Transform parent)
        {
            // Button과 같은 오브젝트의 Image를 Target Graphic으로 사용해 실제 메뉴 구조를 재현한다.
            var buttonObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
            buttonObject.transform.SetParent(
                parent,
                false);
            Button button =
                buttonObject.GetComponent<Button>();
            button.targetGraphic =
                buttonObject.GetComponent<Image>();
            return button;
        }

        private static Text CreateText(
            string objectName,
            Transform parent)
        {
            // 옵션 문구를 직접 조회할 수 있도록 최소 Text View를 만든다.
            var textObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
            textObject.transform.SetParent(
                parent,
                false);
            return textObject.GetComponent<Text>();
        }
    }
}
