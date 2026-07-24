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
            target = followTarget;
            motor = target != null ? target.GetComponent<SideScrollerMotor>() : null;
        }

        private void Awake()
        {
            Configure(target);
        }

        private void Start()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
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
            return new Vector3(
                target.position.x + offset.x + facingDirection * horizontalLookAhead,
                target.position.y + offset.y,
                target.position.z + offset.z);
        }
    }
}
