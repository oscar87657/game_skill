// GOLDEN STANDARD
// 목적: 같은 지형·입력·충돌 조건에서 세 점프 후보와 애니메이션 연결을 비교한다.
// 책임: 실험 실행·초기화·화면 계측만 담당하며 본편의 능력·저장·전투와 연결하지 않는다.
// 불변식: 후보 변경은 출발점으로 복귀하며 천장 접촉은 상승을 중단한다.
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameSkill.Experiments
{
    public enum JumpVariant { Gravity, ReleaseCut, TimedCurve }

    [RequireComponent(typeof(CharacterController))]
    public sealed class JumpExperiment : MonoBehaviour
    {
        [SerializeField] private JumpVariant variant;
        [SerializeField, Min(0.1f), Tooltip("공통 가로 이동 속도 (m/s)")]
        private float moveSpeed = 5f;
        [SerializeField, Min(0.1f), Tooltip("홀드 점프의 목표 높이 (m)")]
        private float jumpHeight = 3f;
        [SerializeField, Min(0.1f), Tooltip("중력 크기 (m/s²). A/B 및 곡선 종료 후 낙하에 사용")]
        private float gravity = 24f;
        [SerializeField, Min(0f), Tooltip("B에서 버튼을 놓았을 때 남길 최대 상승 속도")]
        private float releaseSpeed = 2f;
        [SerializeField, Min(0.1f), Tooltip("C의 출발 높이 복귀 시간. 기본값은 A/B 홀드와 1초로 일치")]
        private float curveDuration = 1f;
        [SerializeField, Tooltip("C의 시간-높이 곡선. 시작·끝 0, 최고 높이 1을 기준으로 조정")]
        private AnimationCurve heightCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 5f, 5f),
            new Keyframe(0.4f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -3.333333f, -3.333333f));
        [SerializeField] private Animator animator;
        [SerializeField] private bool timedAnimation;
        [SerializeField, Min(0.01f), Tooltip("고정 시간 모드에서 상승→낙하 클립을 전환할 시간")]
        private float animationApexTime = 0.5f;

        private CharacterController body;
        private Vector3 spawn;
        private Vector3 takeoff;
        private float velocity;
        private float elapsed;
        private bool curveActive;
        private bool recording;
        private float peak;
        private float inputHold;
        private bool holdingMeasuredJump;
        private float lastHeight;
        private float lastAirTime;
        private float lastDistance;
        private float lastHold;
        private bool hasResult;
        private bool ceilingHit;
        private bool lastCeilingHit;

        public JumpVariant Variant => variant;
        public float VerticalSpeed => velocity;
        public bool IsGrounded => body != null && body.isGrounded;
        public bool HasResult => hasResult;
        public float LastHeight => lastHeight;
        public bool LastCeilingHit => lastCeilingHit;

        private void Awake()
        {
            // 씬에 저장된 위치를 모든 시행의 공통 복귀 지점으로 고정한다.
            body = GetComponent<CharacterController>();
            spawn = transform.position;
        }

        private void OnValidate()
        {
            // Inspector에서 계산 불가능한 시간·중력이 들어오지 않게 설정 경계를 지킨다.
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            jumpHeight = Mathf.Max(0.1f, jumpHeight);
            gravity = Mathf.Max(0.1f, gravity);
            releaseSpeed = Mathf.Max(0f, releaseSpeed);
            curveDuration = Mathf.Max(0.1f, curveDuration);
            animationApexTime = Mathf.Max(0.01f, animationApexTime);
        }

        public void ConfigureVisual(Animator target)
        {
            // 실험 전용 시각 인스턴스만 연결하고 원본 Importer는 바꾸지 않는다.
            animator = target;
        }

        public void SelectVariant(JumpVariant next)
        {
            // 출발 조건을 맞추기 위해 이전 후보의 속도와 계측을 이어받지 않는다.
            variant = next;
            ResetTrial();
        }

        public void ResetTrial()
        {
            // CharacterController를 잠시 끄고 출발점에 안전하게 재배치한다.
            body ??= GetComponent<CharacterController>();
            body.enabled = false;
            transform.position = spawn;
            body.enabled = true;
            velocity = 0f;
            elapsed = 0f;
            peak = 0f;
            inputHold = 0f;
            curveActive = recording = holdingMeasuredJump = hasResult = ceilingHit = false;
            Physics.SyncTransforms();
            body.Move(Vector3.down * 0.1f);
            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private void Update()
        {
            // 키보드와 게임패드 입력을 같은 실험 입력으로 변환한다.
            Keyboard keys = Keyboard.current;
            Gamepad pad = Gamepad.current;
            if (keys != null)
            {
                if (keys.digit1Key.wasPressedThisFrame) SelectVariant(JumpVariant.Gravity);
                if (keys.digit2Key.wasPressedThisFrame) SelectVariant(JumpVariant.ReleaseCut);
                if (keys.digit3Key.wasPressedThisFrame) SelectVariant(JumpVariant.TimedCurve);
                if (keys.rKey.wasPressedThisFrame) ResetTrial();
                if (keys.fKey.wasPressedThisFrame) timedAnimation = !timedAnimation;
            }
            float horizontal = pad != null ? pad.leftStick.x.ReadValue() : 0f;
            if (keys != null)
            {
                if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) horizontal = -1f;
                if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) horizontal = 1f;
            }
            bool pressed = (keys?.spaceKey.wasPressedThisFrame ?? false)
                || (pad?.buttonSouth.wasPressedThisFrame ?? false);
            bool held = (keys?.spaceKey.isPressed ?? false) || (pad?.buttonSouth.isPressed ?? false);
            Simulate(horizontal, pressed, held, Time.deltaTime);
        }

        public void Simulate(float horizontal, bool pressed, bool held, float deltaTime)
        {
            // 외부 재현 입력에도 비유한 값이나 정지 시간을 통과시키지 않는다.
            if (!float.IsFinite(deltaTime) || deltaTime <= 0f || !float.IsFinite(horizontal)) return;
            horizontal = Mathf.Clamp(horizontal, -1f, 1f);
            bool grounded = body.isGrounded;
            if (grounded && velocity <= 0f)
            {
                velocity = -2f;
                curveActive = false;
            }
            if (pressed && grounded)
            {
                // 점프 시작 조건은 모든 후보가 같다. 버퍼와 코요테 타임은 후속 비교 변수다.
                velocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                elapsed = peak = inputHold = 0f;
                takeoff = transform.position;
                recording = holdingMeasuredJump = true;
                ceilingHit = false;
                curveActive = variant == JumpVariant.TimedCurve;
            }

            float dy;
            if (curveActive)
            {
                dy = TimedCurveJump.Step(heightCurve, jumpHeight, curveDuration, elapsed, deltaTime);
                velocity = dy / deltaTime;
                // 곡선 끝점보다 낮은 지면까지는 일반 중력 낙하로 이어 간다.
                if (elapsed + deltaTime >= curveDuration) curveActive = false;
            }
            else if (variant == JumpVariant.ReleaseCut && recording)
                dy = ReleaseCutJump.Step(ref velocity, gravity, releaseSpeed, held, deltaTime);
            else
                dy = GravityJump.Step(ref velocity, gravity, deltaTime);

            CollisionFlags collision = body.Move(new Vector3(horizontal * moveSpeed * deltaTime, dy, 0f));
            if ((collision & CollisionFlags.Above) != 0 && dy > 0f)
            {
                // 천장에 닿으면 시간 곡선도 중단해 보이지 않는 상승을 계속하지 않는다.
                velocity = 0f;
                curveActive = false;
                ceilingHit = true;
            }
            if (recording)
            {
                elapsed += deltaTime;
                peak = Mathf.Max(peak, transform.position.y - takeoff.y);
                holdingMeasuredJump &= held;
                if (holdingMeasuredJump) inputHold += deltaTime;
                if ((collision & CollisionFlags.Below) != 0 && dy <= 0f)
                {
                    // 실제 접지 프레임에서 기록을 확정하며 예상 착지로 완료 처리하지 않는다.
                    lastHeight = peak;
                    lastAirTime = elapsed;
                    lastDistance = Mathf.Abs(transform.position.x - takeoff.x);
                    lastHold = inputHold;
                    lastCeilingHit = ceilingHit;
                    hasResult = true;
                    recording = curveActive = false;
                    velocity = -2f;
                }
            }
            UpdateAnimation(horizontal);
            if (transform.position.y < -6f) ResetTrial();
        }

        private void UpdateAnimation(float horizontal)
        {
            if (animator == null) return;
            // 같은 클립의 전환 근거만 바꾼다. 접지는 두 방식 모두 실제 충돌을 우선한다.
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            animator.SetBool("Grounded", body.isGrounded);
            animator.SetFloat("VerticalSpeed", timedAnimation && recording
                ? (elapsed < animationApexTime ? 1f : -1f) : velocity);
            if (Mathf.Abs(horizontal) > 0.05f)
                animator.transform.localRotation = Quaternion.Euler(0f, horizontal > 0f ? 90f : -90f, 0f);
        }

        private void OnGUI()
        {
            // 별도 UI 의존성 없이 후보와 재현 조건을 계속 보여 주는 실험 전용 화면이다.
            GUILayout.BeginArea(new Rect(12, 12, 430, 275), GUI.skin.box);
            GUILayout.Label("J01 | JUMP CONTROL LAB");
            GUILayout.Label("A/D or arrows: move | Space: jump | R: reset | F: animation");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1  Gravity")) SelectVariant(JumpVariant.Gravity);
            if (GUILayout.Button("2  Release cut")) SelectVariant(JumpVariant.ReleaseCut);
            if (GUILayout.Button("3  Time curve")) SelectVariant(JumpVariant.TimedCurve);
            GUILayout.EndHorizontal();
            GUILayout.Label($"Mode: {variant} | Animation: {(timedAnimation ? "fixed time" : "vertical speed")}");
            if (GUILayout.Button("Toggle animation transition")) timedAnimation = !timedAnimation;
            if (GUILayout.Button("Reset trial")) ResetTrial();
            GUILayout.Label($"Height {jumpHeight:F2} m | Gravity {gravity:F1} | Curve {curveDuration:F2} s");
            GUILayout.Label($"Y {transform.position.y:F2} | VY {velocity:F2} | Grounded {body.isGrounded}");
            GUILayout.Label(hasResult
                ? $"Last: height {lastHeight:F2} m | air {lastAirTime:F2} s | distance {lastDistance:F2} m\nHold {lastHold:F2} s | ceiling {lastCeilingHit}"
                : "Last: no completed jump (reset clears results)");
            GUILayout.Label("LEFT: low ceiling | CENTER: flat trial | RIGHT: raised platforms");
            GUILayout.EndArea();
        }
    }
}
