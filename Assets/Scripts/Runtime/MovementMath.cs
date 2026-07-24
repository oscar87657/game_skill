using UnityEngine;

namespace GameSkill
{
    public static class MovementMath
    {
        public static float HorizontalInput(Vector2 input, float deadZone)
        {
            float clampedInput = Mathf.Clamp(input.x, -1f, 1f);
            return Mathf.Abs(clampedInput) < Mathf.Clamp01(deadZone)
                ? 0f
                : clampedInput;
        }

        public static float SideScrollerFacingYaw(float horizontalDirection)
        {
            return horizontalDirection < 0f ? -90f : 90f;
        }

        public static float DodgeDirection(
            float horizontalInput,
            float facingDirection)
        {
            if (Mathf.Abs(horizontalInput) > Mathf.Epsilon)
            {
                return Mathf.Sign(horizontalInput);
            }

            return facingDirection < 0f ? -1f : 1f;
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
