using KnightSurvivors.Player;
using UnityEngine;

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup together with
    /// CameraFollowModeToggle, CameraJitterReadout, CameraRigSelfCheck and CameraDebugBootstrap).
    ///
    /// The simplest possible isometric follow camera, with NO Cinemachine involved. F2 switches between
    /// this and the Cinemachine rig. If walking looks smooth with this one but not with Cinemachine, the
    /// problem is in the Cinemachine set-up; if it jitters with both, the problem is in what we follow
    /// (the player's smoothing) or in rendering / frame pacing.
    ///
    /// HOW IT WORKS
    ///   - Focus point = average of the living players' CameraTarget (the SMOOTHED child, never the root).
    ///   - The camera keeps a fixed angle (50° down, 45° yaw) and sits "distance" metres back along its
    ///     own view direction, so the focus point is always in the middle of the screen.
    ///   - It runs in LateUpdate with a late execution order, so every Update (including the
    ///     TickInterpolator smoothing) has already happened this frame.
    ///   - SmoothDamp uses Time.deltaTime, i.e. once per rendered frame, matching the smoothed target.
    /// </summary>
    [DefaultExecutionOrder(200)] // after Cinemachine's brain (100) in case both are ever on
    [DisallowMultipleComponent]
    public class SimpleIsoFollow : MonoBehaviour
    {
        [Tooltip("Degrees the camera looks down (GDD: 50).")]
        public float pitch = 50f;

        [Tooltip("Iso yaw in degrees (GDD: 45). Must match PlayerMotor.cameraYaw so 'up' on the stick is 'up' on screen.")]
        public float yaw = 45f;

        [Tooltip("Distance from the focus point along the view direction (m).")]
        public float distance = 26f;

        [Tooltip("Vertical field of view (degrees).")]
        public float fieldOfView = 30f;

        [Tooltip("SmoothDamp time (s). 0 = locked hard to the target (the purest test: any jitter left is not the camera's).")]
        public float smoothTime = 0.15f;

        private Vector3 focus;
        private Vector3 velocity;
        private bool hasFocus;
        private Camera cam;

        /// <summary>Jump straight to the target (used when F2 switches to this mode).</summary>
        public void Snap()
        {
            hasFocus = TryGetTargetPoint(out focus);
            velocity = Vector3.zero;
            Apply();
        }

        private void OnEnable()
        {
            cam = GetComponent<Camera>();
            Snap();
        }

        private void LateUpdate()
        {
            if (!TryGetTargetPoint(out Vector3 target)) return;

            if (!hasFocus)
            {
                focus = target;
                hasFocus = true;
            }
            else if (smoothTime <= 0f)
            {
                focus = target;
            }
            else
            {
                focus = Vector3.SmoothDamp(focus, target, ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            }

            Apply();
        }

        private void Apply()
        {
            // Same sign convention as the Cinemachine rig (Euler(50, -45, 0)) and PlayerMotor.cameraYaw.
            Quaternion rotation = Quaternion.Euler(pitch, -yaw, 0f);
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
            if (cam != null) cam.fieldOfView = fieldOfView;
        }

        // Average of the living players' smoothed camera targets.
        private static bool TryGetTargetPoint(out Vector3 point)
        {
            point = Vector3.zero;
            int count = 0;
            var players = PlayerRegistry.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p.IsDead) continue;
                Transform t = p.cameraTarget != null ? p.cameraTarget : p.transform;
                point += t.position;
                count++;
            }

            if (count == 0) return false;
            point /= count;
            return true;
        }
    }
}
