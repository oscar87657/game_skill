using UnityEngine;
using UnityEngine.InputSystem;

namespace GameSkill
{
    public sealed class ThirdPersonOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField, Min(0f)] private float pivotHeight = 1.4f;
        [SerializeField, Min(0.1f)] private float distance = 5.5f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float gamepadDegreesPerSecond = 150f;
        [SerializeField] private Vector2 pitchLimits = new(-30f, 65f);
        [SerializeField, Min(0f)] private float collisionRadius = 0.2f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private InputAction lookAction;
        private float yaw;
        private float pitch = 15f;

        private void Awake()
        {
            if (playerInput != null)
            {
                lookAction = playerInput.actions.FindAction("Look", true);
            }
        }

        private void Start()
        {
            yaw = target != null ? target.eulerAngles.y : transform.eulerAngles.y;
            LockCursor();
            UpdateCameraPosition();
        }

        private void LateUpdate()
        {
            if (target == null || lookAction == null)
            {
                return;
            }

            HandleCursor();

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = lookAction.ReadValue<Vector2>();
                float scale = lookAction.activeControl?.device is Mouse
                    ? mouseSensitivity
                    : gamepadDegreesPerSecond * Time.deltaTime;

                yaw += look.x * scale;
                pitch = Mathf.Clamp(pitch - look.y * scale, pitchLimits.x, pitchLimits.y);
            }

            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            if (target == null)
            {
                return;
            }

            Vector3 pivot = target.position + Vector3.up * pivotHeight;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 backwards = rotation * Vector3.back;
            float resolvedDistance = distance;

            if (Physics.SphereCast(
                    pivot,
                    collisionRadius,
                    backwards,
                    out RaycastHit hit,
                    distance,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                resolvedDistance = Mathf.Max(hit.distance - collisionRadius, 0.1f);
            }

            transform.SetPositionAndRotation(
                pivot + backwards * resolvedDistance,
                rotation);
        }

        private static void HandleCursor()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                LockCursor();
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
