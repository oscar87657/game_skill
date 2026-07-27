// GOLDEN STANDARD
// Purpose: Keep deterministic movement formulas independent from MonoBehaviour state.
// Responsibility: Normalize input, calculate facing, dash direction, and ballistic jump speed.
// Invariant: Invalid physical parameters return a safe value instead of NaN or Infinity.
// Design choice: Pure static functions are easy to unit-test and reusable by alternate movement controllers.
using UnityEngine;

namespace GameSkill
{
    public static class MovementMath
    {
        public static float HorizontalInput(Vector2 input, float deadZone)
        {
            // Clamp first, then apply the dead zone so keyboard and analog input share one contract.
            float clampedInput = Mathf.Clamp(input.x, -1f, 1f);
            return Mathf.Abs(clampedInput) < Mathf.Clamp01(deadZone)
                ? 0f
                : clampedInput;
        }

        public static float SideScrollerFacingYaw(float horizontalDirection)
        {
            // A side-scroller only needs two visual orientations, so map sign directly to yaw.
            return horizontalDirection < 0f ? -90f : 90f;
        }

        public static float DodgeDirection(
            float horizontalInput,
            float facingDirection)
        {
            // Player intent wins; when there is no input, preserve the character's last facing direction.
            if (Mathf.Abs(horizontalInput) > Mathf.Epsilon)
            {
                return Mathf.Sign(horizontalInput);
            }

            return facingDirection < 0f ? -1f : 1f;
        }

        public static float JumpSpeed(float jumpHeight, float gravity)
        {
            // Derive the initial velocity from v² = u² + 2as; gravity must point downward.
            if (jumpHeight <= 0f || gravity >= 0f)
            {
                return 0f;
            }

            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
