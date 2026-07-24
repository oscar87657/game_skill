using UnityEngine;

namespace GameSkill
{
    public static class MovementMath
    {
        public static Vector3 CameraRelativeDirection(
            Vector2 input,
            Vector3 cameraForward,
            Vector3 cameraRight)
        {
            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            return Vector3.ClampMagnitude(
                cameraForward * input.y + cameraRight * input.x,
                1f);
        }

        public static float JumpSpeed(float jumpHeight, float gravity)
        {
            if (jumpHeight <= 0f || gravity >= 0f)
            {
                return 0f;
            }

            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
