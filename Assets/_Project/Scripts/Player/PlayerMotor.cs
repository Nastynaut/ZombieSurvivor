using UnityEngine;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// Moves the knight on the 50 Hz gameplay tick, camera-relative (GDD 2.4):
    /// pressing W moves "up the screen", which in our 45-degree isometric view is world (-1, 0, 1).
    ///
    /// Uses a CharacterController (a capsule that slides along walls without a Rigidbody).
    /// Zombies have NO colliders (they are simulated by EnemyManager), so the controller only
    /// bumps into walls and gravestones.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Tooltip("Camera yaw in degrees used to turn stick input into world directions (GDD 2.2: 45).")]
        public float cameraYaw = 45f;

        [Tooltip("Small downward speed that keeps the controller glued to the floor.")]
        public float groundStickSpeed = 2f;

        [Tooltip("How fast the knight turns to face its aim direction, degrees per second.")]
        public float turnSpeed = 900f;

        /// <summary>Last non-zero movement direction (world, flat). Used by aim fallbacks and the spawner bias.</summary>
        public Vector3 LastMoveDirection { get; private set; } = new Vector3(-1f, 0f, 1f).normalized;

        /// <summary>Current movement velocity (world, m/s).</summary>
        public Vector3 Velocity { get; private set; }

        /// <summary>
        /// How fast the knight REALLY moved on the last tick (world, flat, m/s). Same as Velocity in the open,
        /// but smaller when a wall or gravestone blocks the move. The camera lookahead uses this, so the camera
        /// doesn't lean into a wall the knight is pushing against.
        /// </summary>
        public Vector3 GroundVelocity { get; private set; }

        private CharacterController controller;
        private PlayerController player;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            player = GetComponent<PlayerController>();
        }

        /// <summary>
        /// Turns a 2D input (x = right, y = up on screen) into a flat world direction using the camera yaw.
        /// With yaw 45: up (0,1) -> (-0.707, 0, 0.707); right (1,0) -> (0.707, 0, 0.707).
        /// </summary>
        public Vector3 ScreenToWorldDirection(Vector2 input)
        {
            // Camera sits at yaw 45 looking toward the arena centre, i.e. its forward points along -yaw.
            Quaternion yaw = Quaternion.Euler(0f, -cameraYaw, 0f);
            Vector3 world = yaw * new Vector3(input.x, 0f, input.y);
            return world;
        }

        private void FixedUpdate()
        {
            if (player == null || player.IsDead || player.Input == null)
            {
                Velocity = Vector3.zero;
                GroundVelocity = Vector3.zero;
                return;
            }

            Vector2 input = player.Input.State.Move;
            Vector3 dir = ScreenToWorldDirection(input);
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            if (dir.sqrMagnitude > 0.0001f) LastMoveDirection = dir.normalized;

            Velocity = dir * player.Stats.moveSpeed;
            controller.Move((Velocity + Vector3.down * groundStickSpeed) * Time.fixedDeltaTime);
            // CharacterController.velocity = what the Move above actually achieved. Drop the up/down part.
            Vector3 achieved = controller.velocity;
            achieved.y = 0f;
            GroundVelocity = achieved;

            // Face where we're attacking (the arrow on the ground ring shows this).
            Vector3 face = player.AimDirection;
            face.y = 0f;
            if (face.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(face, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.fixedDeltaTime);
            }
        }

        /// <summary>Move instantly (spawning, respawning). Temporarily disables the controller, as Unity requires.</summary>
        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            var interp = GetComponent<TickInterpolator>();
            if (interp != null) interp.Snap();
        }
    }
}
