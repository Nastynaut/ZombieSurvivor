using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// Decides which way the knight attacks (GDD 2.4, AimMode).
    ///
    /// Assisted (default), checked in this order:
    ///   1. Right stick held past the deadzone -> stick direction.
    ///   2. Mouse moved within the last <see cref="mouseAimTimeout"/> seconds -> toward the cursor on the ground.
    ///   3. Nearest enemy within <see cref="assistRange"/> (the Longsword's 6 m range) -> toward it.
    ///   4. Otherwise -> the last movement direction.
    /// FullAuto: skip 1 and 2 (always nearest enemy, else move direction).
    ///
    /// NOTE (open question for Dustin): "mouse moved recently" is our reading of the GDD so that a
    /// keyboard+mouse player who stops touching the mouse still gets auto-aim. Set the timeout very
    /// high to make the mouse always win.
    /// </summary>
    public class PlayerAim : MonoBehaviour
    {
        public AimMode mode = AimMode.Assisted;

        [Tooltip("Seconds after the mouse last moved during which the cursor decides the aim.")]
        public float mouseAimTimeout = 1.5f;

        [Tooltip("Assisted / auto aim looks for enemies this close (m). GDD: weapon range, 6 m for the Longsword.")]
        public float assistRange = 6f;

        /// <summary>Flat, normalized world direction the weapons should use.</summary>
        public Vector3 AimDirection { get; private set; } = new Vector3(-1f, 0f, 1f).normalized;

        private PlayerController player;
        private ITargetProvider targets;
        private Camera cachedCamera;

        public void Initialize(PlayerController owner, AimMode aimMode)
        {
            player = owner;
            mode = aimMode;
        }

        private void FixedUpdate()
        {
            if (player == null || player.IsDead || player.Input == null) return;
            var input = player.Input.State;
            Vector3 dir;

            if (mode == AimMode.Assisted && input.AimStick.sqrMagnitude > 0f)
            {
                dir = player.Motor.ScreenToWorldDirection(input.AimStick);
            }
            else if (mode == AimMode.Assisted && input.HasPointer &&
                     Time.time - input.PointerLastMovedTime <= mouseAimTimeout &&
                     TryPointerDirection(input.PointerScreenPosition, out dir))
            {
                // dir set by TryPointerDirection
            }
            else if (TryNearestEnemy(out dir))
            {
                // dir set by TryNearestEnemy
            }
            else
            {
                dir = player.Motor.LastMoveDirection;
            }

            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) AimDirection = dir.normalized;
        }

        private bool TryNearestEnemy(out Vector3 dir)
        {
            dir = Vector3.zero;
            if (targets == null && !ServiceLocator.TryGet(out targets)) return false;
            if (!targets.TryGetNearestEnemy(transform.position, assistRange, out Vector3 enemyPos)) return false;
            dir = enemyPos - transform.position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f;
        }

        /// <summary>Cast a ray from the camera through the cursor onto the ground (y = 0 plane).</summary>
        private bool TryPointerDirection(Vector2 screenPos, out Vector3 dir)
        {
            dir = Vector3.zero;
            if (cachedCamera == null) cachedCamera = Camera.main;
            if (cachedCamera == null) return false;

            Ray ray = cachedCamera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (!ground.Raycast(ray, out float enter)) return false;

            dir = ray.GetPoint(enter) - transform.position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.01f;
        }
    }
}
