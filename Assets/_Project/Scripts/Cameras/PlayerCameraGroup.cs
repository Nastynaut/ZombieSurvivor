using KnightSurvivors.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// Keeps a CinemachineTargetGroup filled with every player's camera target (GDD 2.2 / 11).
    /// The CinemachineCamera follows the GROUP, never one player, so co-op needs no camera change:
    /// players join -> they're added here -> the shared camera frames them all.
    ///
    /// Also zooms the camera out (up to x1.3, i.e. 26 m -> 34 m) as living players spread apart.
    /// Dead players are given weight 0 so the camera follows the survivors.
    /// </summary>
    [RequireComponent(typeof(CinemachineTargetGroup))]
    public class PlayerCameraGroup : MonoBehaviour
    {
        [Tooltip("The PositionComposer on the CinemachineCamera (for co-op zoom-out).")]
        public CinemachinePositionComposer composer;

        public float baseDistance = 26f;
        public float maxZoomOut = 1.3f;
        [Tooltip("Player spread (m) that produces the full zoom-out.")]
        public float spreadForMaxZoom = 12f;

        [Tooltip("Radius each player takes up in the group (m).")]
        public float memberRadius = 1f;

        private CinemachineTargetGroup group;

        private void Awake()
        {
            group = GetComponent<CinemachineTargetGroup>();
            PlayerRegistry.PlayerAdded += OnPlayerAdded;
            PlayerRegistry.PlayerRemoved += OnPlayerRemoved;
        }

        private void OnDestroy()
        {
            PlayerRegistry.PlayerAdded -= OnPlayerAdded;
            PlayerRegistry.PlayerRemoved -= OnPlayerRemoved;
        }

        private void Start()
        {
            // Players that spawned before this object woke up.
            var players = PlayerRegistry.Players;
            for (int i = 0; i < players.Count; i++) OnPlayerAdded(players[i]);
        }

        private void OnPlayerAdded(PlayerController player)
        {
            if (player == null || player.cameraTarget == null) return;
            if (group.FindMember(player.cameraTarget) >= 0) return;
            group.AddMember(player.cameraTarget, 1f, memberRadius);
        }

        private void OnPlayerRemoved(PlayerController player)
        {
            if (player != null && player.cameraTarget != null) group.RemoveMember(player.cameraTarget);
        }

        private void LateUpdate()
        {
            var players = PlayerRegistry.Players;
            Vector3 centre = Vector3.zero;
            int alive = 0;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p.cameraTarget == null) continue;
                int index = group.FindMember(p.cameraTarget);
                if (index >= 0) group.Targets[index].Weight = p.IsDead ? 0f : 1f;
                if (!p.IsDead)
                {
                    centre += p.cameraTarget.position;
                    alive++;
                }
            }

            if (composer == null || alive == 0) return;
            centre /= alive;

            float spread = 0f;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p.IsDead || p.cameraTarget == null) continue;
                spread = Mathf.Max(spread, Vector3.Distance(centre, p.cameraTarget.position));
            }

            // One living player: the distance is EXACTLY baseDistance (26 m), not "26 times a tiny spread
            // factor". Cinemachine applies distance changes instantly (no damping), so even a flickering
            // 0.001 m would move the camera along its view axis, i.e. on screen-vertical + depth.
            float distance = baseDistance;
            if (alive > 1)
            {
                float t = Mathf.Clamp01(spread / spreadForMaxZoom);
                distance = baseDistance * Mathf.Lerp(1f, maxZoomOut, t);
            }
            // Only write when it really changes, so nothing touches the composer while walking alone.
            if (composer.CameraDistance != distance) composer.CameraDistance = distance;
        }
    }
}
