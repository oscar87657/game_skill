// GOLDEN STANDARD
// Purpose: Follow the player on the fixed 2.5D plane without changing gameplay physics.
// Responsibility: Compute a look-ahead target and smooth only camera position.
// Invariant: Camera rotation and target depth remain fixed for side-scroller readability.
// Design choice: SmoothDamp avoids frame-rate-dependent lerps and exposes tuning values to designers.
using UnityEngine;

namespace GameSkill
{
    public sealed class SideScrollerCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 2.4f, -9f);
        [SerializeField, Min(0f)] private float horizontalLookAhead = 1.35f;
        [SerializeField, Min(0.01f)] private float horizontalSmoothTime = 0.16f;
        [SerializeField, Min(0.01f)] private float verticalSmoothTime = 0.24f;

        private SideScrollerMotor motor;
        private float horizontalVelocity;
        private float verticalVelocity;

        public void Configure(Transform followTarget)
        {
            // Resolve the motor from the target once so look-ahead follows facing, not raw input.
            target = followTarget;
            motor = target != null ? target.GetComponent<SideScrollerMotor>() : null;
        }

        private void Awake()
        {
            // Support both scene-authored references and editor-generated configuration.
            Configure(target);
        }

        private void Start()
        {
            // Avoid a visible camera lerp from the origin on the first frame.
            SnapToTarget();
        }

        private void LateUpdate()
        {
            // LateUpdate runs after player movement, preventing one-frame camera lag.
            if (target == null)
            {
                return;
            }

            float facingDirection = motor != null ? motor.FacingDirection : 0f;
            Vector3 desiredPosition = TargetPosition(facingDirection);
            float x = Mathf.SmoothDamp(
                transform.position.x,
                desiredPosition.x,
                ref horizontalVelocity,
                horizontalSmoothTime);
            float y = Mathf.SmoothDamp(
                transform.position.y,
                desiredPosition.y,
                ref verticalVelocity,
                verticalSmoothTime);

            transform.SetPositionAndRotation(
                new Vector3(x, y, desiredPosition.z),
                Quaternion.identity);
        }

        private void SnapToTarget()
        {
            // Place the camera immediately when entering a scene or respawning.
            if (target == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                TargetPosition(motor != null ? motor.FacingDirection : 0f),
                Quaternion.identity);
        }

        private Vector3 TargetPosition(float facingDirection)
        {
            // Look-ahead uses facing rather than velocity so the camera anticipates deliberate direction changes.
            return new Vector3(
                target.position.x + offset.x + facingDirection * horizontalLookAhead,
                target.position.y + offset.y,
                target.position.z + offset.z);
        }
    }
}
