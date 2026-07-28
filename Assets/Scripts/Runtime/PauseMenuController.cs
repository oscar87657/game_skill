// GOLDEN STANDARD
// 목적: 플레이 시간을 안전하게 멈추고 재개·저장·불러오기·마스터 음량 UI를 제공한다.
// 책임: Pause 입력 구독, TimeScale·AudioListener 정지, 버튼 전달과 옵션 영속화를 관리한다.
// 불변식: 메뉴가 닫히거나 컴포넌트가 비활성화되면 진입 전 시간 배율과 오디오 정지 상태를 복원한다.
// 선택 이유: 일시정지 수명 주기를 한 컴포넌트에 모으면 개별 이동·전투 시스템에 Pause 분기를 넣지 않아도 된다.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameSkill
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        public const string MasterVolumePreferenceKey =
            "game_skill.master_volume";
        private const float VolumeStep = 0.1f;

        [SerializeField] private GameObject menuRoot;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField]
        private GameProgressHud progressHud;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button volumeDownButton;
        [SerializeField] private Button volumeUpButton;
        [SerializeField] private Text volumeLabel;
        [SerializeField] private Text statusLabel;

        private InputAction pauseAction;
        private bool isSubscribed;
        private float timeScaleBeforePause = 1f;
        private bool audioPauseBeforePause;

        public bool IsPaused { get; private set; }
        public bool IsConfigured =>
            menuRoot != null
            && playerInput != null
            && progressHud != null
            && resumeButton != null
            && saveButton != null
            && loadButton != null
            && volumeDownButton != null
            && volumeUpButton != null
            && volumeLabel != null
            && statusLabel != null;
        public bool HasPauseAction =>
            pauseAction != null;
        public GameObject MenuRoot =>
            menuRoot;
        public float MasterVolume =>
            AudioListener.volume;
        public string VolumeLabelText =>
            volumeLabel != null
                ? volumeLabel.text
                : string.Empty;
        public string StatusText =>
            statusLabel != null
                ? statusLabel.text
                : string.Empty;

        private void Awake()
        {
            // 첫 프레임 전에 입력 참조와 저장된 음량을 적용하고 메뉴는 닫힌 상태로 시작한다.
            CachePauseAction();
            ApplyMasterVolume(
                PlayerPrefs.GetFloat(
                    MasterVolumePreferenceKey,
                    1f),
                false);
            SetMenuVisible(false);
        }

        private void OnEnable()
        {
            // 활성 메뉴 제어기만 입력과 Button 이벤트를 받아 중복 토글을 방지한다.
            CachePauseAction();
            Subscribe();
        }

        private void OnDisable()
        {
            // Scene 종료나 Canvas 비활성화 중에도 전역 시간과 오디오가 정지된 채 남지 않게 복원한다.
            Unsubscribe();
            RestoreRunningState();
        }

        public bool Configure(
            GameObject pauseMenuRoot,
            PlayerInput input,
            GameProgressHud gameProgressHud,
            Button resumeActionButton,
            Button saveActionButton,
            Button loadActionButton,
            Button volumeDecreaseButton,
            Button volumeIncreaseButton,
            Text masterVolumeText,
            Text resultText)
        {
            // 빌더 재실행이 같은 참조를 다시 기록할 때 Scene Dirty 여부를 정확히 반환한다.
            bool changed =
                menuRoot != pauseMenuRoot
                || playerInput != input
                || progressHud != gameProgressHud
                || resumeButton != resumeActionButton
                || saveButton != saveActionButton
                || loadButton != loadActionButton
                || volumeDownButton
                    != volumeDecreaseButton
                || volumeUpButton
                    != volumeIncreaseButton
                || volumeLabel != masterVolumeText
                || statusLabel != resultText;

            Unsubscribe();
            menuRoot = pauseMenuRoot;
            playerInput = input;
            progressHud = gameProgressHud;
            resumeButton = resumeActionButton;
            saveButton = saveActionButton;
            loadButton = loadActionButton;
            volumeDownButton = volumeDecreaseButton;
            volumeUpButton = volumeIncreaseButton;
            volumeLabel = masterVolumeText;
            statusLabel = resultText;
            CachePauseAction();
            ApplyMasterVolume(
                AudioListener.volume,
                false);
            SetMenuVisible(IsPaused);
            Subscribe();
            return changed;
        }

        public void TogglePause()
        {
            // 하나의 입력이 열기와 닫기를 모두 담당해 키보드와 게임패드 동작을 동일하게 유지한다.
            SetPaused(!IsPaused);
        }

        public void SetPaused(bool shouldPause)
        {
            // 이미 원하는 상태이면 TimeScale과 선택 오브젝트를 다시 덮어쓰지 않는다.
            if (IsPaused == shouldPause)
            {
                return;
            }

            if (shouldPause)
            {
                timeScaleBeforePause =
                    Time.timeScale;
                audioPauseBeforePause =
                    AudioListener.pause;
                IsPaused = true;
                Time.timeScale = 0f;
                AudioListener.pause = true;
                SetMenuVisible(true);
                SelectResumeButton();
                SetStatus("PAUSED", true);
                return;
            }

            RestoreRunningState();
        }

        public void Resume()
        {
            // UI Button과 외부 시스템이 토글 계산 없이 명시적으로 게임을 재개하게 한다.
            SetPaused(false);
        }

        public bool TrySave()
        {
            // 저장 규칙과 파일 처리는 기존 진행 HUD에 위임하고 Pause 메뉴는 결과만 복제해 표시한다.
            bool succeeded =
                progressHud != null
                && progressHud.TrySave();
            SetStatus(
                succeeded
                    ? "SAVED"
                    : "SAVE FAILED",
                succeeded);
            return succeeded;
        }

        public bool TryLoad()
        {
            // 검증된 진행 복원 API를 재사용해 Pause 메뉴가 별도 저장 경로를 만들지 않게 한다.
            bool succeeded =
                progressHud != null
                && progressHud.TryLoad();
            SetStatus(
                succeeded
                    ? "LOADED"
                    : "NO SAVE",
                succeeded);
            return succeeded;
        }

        public void SetMasterVolume(float normalizedVolume)
        {
            // 공개 옵션 변경은 0~1로 제한하고 다음 실행에도 유지되도록 PlayerPrefs에 기록한다.
            ApplyMasterVolume(
                normalizedVolume,
                true);
        }

        public void DecreaseMasterVolume()
        {
            // 고정 간격 버튼은 마우스와 게임패드 모두 같은 예측 가능한 음량 단계를 사용하게 한다.
            SetMasterVolume(
                MasterVolume - VolumeStep);
        }

        public void IncreaseMasterVolume()
        {
            // 부동소수점 누적값도 Apply 단계에서 다시 제한해 100%를 넘지 않게 한다.
            SetMasterVolume(
                MasterVolume + VolumeStep);
        }

        private void CachePauseAction()
        {
            // 입력 에셋이 아직 배치되지 않은 EditMode 구성에서도 예외 없이 Pause 액션을 찾는다.
            pauseAction =
                playerInput != null
                    ? playerInput.actions
                        ?.FindAction(
                            "Pause",
                            false)
                    : null;
        }

        private void Subscribe()
        {
            // Configure와 OnEnable이 연속 호출돼도 입력과 Button 리스너를 한 번만 연결한다.
            if (isSubscribed)
            {
                return;
            }

            if (pauseAction != null)
            {
                pauseAction.performed +=
                    HandlePausePerformed;
            }

            resumeButton?.onClick.AddListener(
                Resume);
            saveButton?.onClick.AddListener(
                HandleSaveClicked);
            loadButton?.onClick.AddListener(
                HandleLoadClicked);
            volumeDownButton?.onClick.AddListener(
                DecreaseMasterVolume);
            volumeUpButton?.onClick.AddListener(
                IncreaseMasterVolume);
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            // 부분 구성 상태에서도 등록된 리스너만 안전하게 제거한다.
            if (!isSubscribed)
            {
                return;
            }

            if (pauseAction != null)
            {
                pauseAction.performed -=
                    HandlePausePerformed;
            }

            resumeButton?.onClick.RemoveListener(
                Resume);
            saveButton?.onClick.RemoveListener(
                HandleSaveClicked);
            loadButton?.onClick.RemoveListener(
                HandleLoadClicked);
            volumeDownButton?.onClick.RemoveListener(
                DecreaseMasterVolume);
            volumeUpButton?.onClick.RemoveListener(
                IncreaseMasterVolume);
            isSubscribed = false;
        }

        private void HandlePausePerformed(
            InputAction.CallbackContext context)
        {
            // Input System의 키보드 Escape와 게임패드 Start를 같은 토글 경로로 합친다.
            TogglePause();
        }

        private void HandleSaveClicked()
        {
            // Button UnityEvent를 반환값이 있는 저장 API로 전달한다.
            TrySave();
        }

        private void HandleLoadClicked()
        {
            // Button UnityEvent를 반환값이 있는 불러오기 API로 전달한다.
            TryLoad();
        }

        private void ApplyMasterVolume(
            float normalizedVolume,
            bool persist)
        {
            // 모든 진입점에서 음량을 제한하고 실제 출력과 표시 문구를 한 번에 동기화한다.
            float safeVolume =
                Mathf.Clamp01(normalizedVolume);
            AudioListener.volume = safeVolume;
            if (volumeLabel != null)
            {
                volumeLabel.text =
                    $"VOLUME {Mathf.RoundToInt(safeVolume * 100f)}%";
            }

            if (persist)
            {
                PlayerPrefs.SetFloat(
                    MasterVolumePreferenceKey,
                    safeVolume);
            }
        }

        private void SetMenuVisible(bool isVisible)
        {
            // 메뉴 Root만 전환해 내부 Button 참조와 EventSystem 구성을 유지한다.
            if (menuRoot != null
                && menuRoot.activeSelf != isVisible)
            {
                menuRoot.SetActive(isVisible);
            }
        }

        private void SelectResumeButton()
        {
            // 메뉴가 열리면 게임패드 Submit이 즉시 재개 버튼에 작동하도록 초기 선택을 지정한다.
            if (resumeButton == null
                || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(
                resumeButton.gameObject);
        }

        private void SetStatus(
            string message,
            bool succeeded)
        {
            // Pause 메뉴 안에서도 저장 결과를 문구와 색으로 함께 구분한다.
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = message;
            statusLabel.color =
                succeeded
                    ? new Color(
                        0.2f,
                        0.95f,
                        0.72f,
                        1f)
                    : new Color(
                        1f,
                        0.42f,
                        0.32f,
                        1f);
        }

        private void RestoreRunningState()
        {
            // Pause를 소유한 경우에만 저장해 둔 전역 상태를 복구해 다른 시스템의 설정을 보존한다.
            if (!IsPaused)
            {
                SetMenuVisible(false);
                return;
            }

            IsPaused = false;
            Time.timeScale =
                timeScaleBeforePause;
            AudioListener.pause =
                audioPauseBeforePause;
            SetMenuVisible(false);
            SetStatus("READY", true);
        }
    }
}
