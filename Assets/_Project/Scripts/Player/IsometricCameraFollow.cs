using UnityEngine;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// FALLBACK camera, used only if Cinemachine isn't set up. The one-click builder sets up the real
    /// Cinemachine 3 camera (CinemachineCamera + PositionComposer + TargetGroup) and disables this.
    ///
    /// Same numbers as GDD 2.2: perspective, 30 degree vertical FOV, 50 degree pitch, 45 degree yaw,
    /// 26 m away, following the average position of all living players, zooming out up to x1.3
    /// when co-op players spread apart.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class IsometricCameraFollow : MonoBehaviour
    {
        public float fieldOfView = 30f;
        public float pitch = 50f;
        public float yaw = 45f;
        public float distance = 26f;
        [Tooltip("Max zoom-out factor for co-op spread (34 m at 26 m base).")]
        public float maxZoomOut = 1.3f;
        [Tooltip("Seconds to catch up with the target (like Cinemachine damping 0.25).")]
        public float damping = 0.25f;

        private Vector3 focus;
        private Vector3 focusVelocity;
        private bool hasFocus;
        private Camera cam; // cached once: GetComponent every frame is a needless lookup

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            var players = PlayerRegistry.Players;
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == null || players[i].IsDead) continue;
                sum += players[i].cameraTarget.position;
                count++;
            }

            if (count == 0 && !hasFocus) return;
            Vector3 target = count > 0 ? sum / count : focus;

            // Spread = farthest living player from the centre; zoom out a little when players separate.
            float spread = 0f;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == null || players[i].IsDead) continue;
                spread = Mathf.Max(spread, Vector3.Distance(players[i].cameraTarget.position, target));
            }

            float zoom = Mathf.Clamp(1f + spread / 20f, 1f, maxZoomOut);

            if (!hasFocus)
            {
                focus = target;
                hasFocus = true;
            }

            focus = Vector3.SmoothDamp(focus, target, ref focusVelocity, damping);

            // Rotation Y = -yaw so the camera looks along world (-1, 0, 1): "up the screen" = W (GDD 2.4).
            Quaternion rot = Quaternion.Euler(pitch, -yaw, 0f);
            transform.rotation = rot;
            transform.position = focus - rot * Vector3.forward * (distance * zoom);
            if (cam != null) cam.fieldOfView = fieldOfView;
        }
    }
}
