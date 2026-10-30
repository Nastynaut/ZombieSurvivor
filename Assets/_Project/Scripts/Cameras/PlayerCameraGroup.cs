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
    ///
    /// And it owns the camera LOOKAHEAD (GDD 2.1: "1.2 m in the move direction, smoothing 0.4 s"):
    /// the camera aims a little ahead of where the knight is walking, so you see more of what's coming.
    ///
    /// Why not Cinemachine's own lookahead? Its predictor only smooths SPEEDING UP with the smoothing you
    /// set; when the target slows down it uses 1/30 of it (0.4 s -> 0.013 s, see PositionPredictor in the
    /// Cinemachine package). Our knight stops instantly (no deceleration in the GDD), so the whole 1.2 m
    /// lead vanished in about one frame and the camera snapped back toward the knight at ~3x walking speed.
    /// Dustin's snap2 recording measured exactly that: 32-37 px back in ~7 frames on every stop.
    /// Here we smooth the OFFSET itself, the same gentle way in both directions.
    /// </summary>
    [RequireComponent(typeof(CinemachineTargetGroup))]
    [DefaultExecutionOrder(-100)] // runs before the CinemachineBrain's LateUpdate, so the camera uses this frame's values
    public class PlayerCameraGroup : MonoBehaviour
    {
        /// <summary>The group in the scene (there is only one). The TEMP camera readout reads the lookahead from it.</summary>
        public static PlayerCameraGroup Current { get; private set; }

        [Tooltip("The PositionComposer on the CinemachineCamera (for co-op zoom-out).")]
        public CinemachinePositionComposer composer;

        public float baseDistance = 26f;
        public float maxZoomOut = 1.3f;
        [Tooltip("Player spread (m) that produces the full zoom-out.")]
        public float spreadForMaxZoom = 12f;

        [Tooltip("Radius each player takes up in the group (m).")]
        public float memberRadius = 1f;

        [Header("Lookahead (GDD 2.1: 1.2 m ahead, smoothing 0.4 s)")]
        [Tooltip("Turn the camera lookahead on/off.")]
        public bool lookaheadEnabled = true;

        [Tooltip("How far ahead to look, in SECONDS of walking: offset = velocity x this. " +
                 "0.24 s at the base 5 m/s = the GDD's 1.2 m (and a faster knight looks a bit further ahead).")]
        public float lookaheadTime = 0.24f;

        [Tooltip("Seconds to ease the offset OUT when the knight starts walking or turns (GDD: 0.4).")]
        public float lookaheadSmoothTime = 0.4f;

        [Tooltip("Seconds to ease the offset back IN when the knight slows down or stops. " +
                 "Bigger = the camera settles back more slowly. 0.5 feels calm; 0 would bring the snap back.")]
        public float lookaheadReleaseSmoothTime = 0.5f;

        /// <summary>The smoothed offset the camera uses right now (world, m). Shown in the F3 readout.</summary>
        public Vector3 LookaheadOffset => lookaheadOffset;

        /// <summary>Where the offset is heading: average player velocity x lookaheadTime (world, m).</summary>
        public Vector3 LookaheadTarget { get; private set; }

        private CinemachineTargetGroup group;
        private Vector3 lookaheadOffset;
        private Vector3 lookaheadVelocity; // SmoothDamp's memory of how fast the offset is changing

        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

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

            // We do the lookahead ourselves (see the summary above), so Cinemachine's must be OFF or the two
            // would add up. New scenes are built with it off; this fixes scenes built before the change.
            if (composer != null && composer.Lookahead.Enabled)
            {
                var look = composer.Lookahead;
                look.Enabled = false;
                composer.Lookahead = look;
                Debug.LogWarning("[Knight Survivors] Turned off Cinemachine's lookahead on '" + composer.name +
                                 "' (PlayerCameraGroup does a smoothed one instead). Rebuild the Graybox scene to make it permanent.");
            }
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
            Vector3 velocitySum = Vector3.zero;
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
                    if (p.Motor != null) velocitySum += p.Motor.GroundVelocity;
                    alive++;
                }
            }

            UpdateLookahead(alive > 0 ? velocitySum / alive : Vector3.zero);

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

        // Moves the lookahead offset smoothly toward "average velocity x lookaheadTime" and hands it to the
        // camera. The knight's velocity can jump from 5 m/s to 0 in one tick (it stops instantly); the
        // OFFSET never jumps, it glides there with SmoothDamp (a spring that never overshoots).
        private void UpdateLookahead(Vector3 averageVelocity)
        {
            if (composer == null) return;

            Vector3 target = Vector3.zero;
            if (lookaheadEnabled)
            {
                averageVelocity.y = 0f; // only look ahead across the floor, never up/down
                target = averageVelocity * lookaheadTime;
            }
            LookaheadTarget = target;

            // Growing (starting to walk, turning) uses the GDD's 0.4 s; shrinking (slowing, stopping) uses
            // the release time. Either way the offset moves smoothly, so the camera never snaps.
            bool shrinking = target.sqrMagnitude < lookaheadOffset.sqrMagnitude;
            float smoothTime = Mathf.Max(0.01f, shrinking ? lookaheadReleaseSmoothTime : lookaheadSmoothTime);
            lookaheadOffset = Vector3.SmoothDamp(lookaheadOffset, target, ref lookaheadVelocity, smoothTime,
                                                 Mathf.Infinity, Time.deltaTime);

            // The PositionComposer adds TargetOffset to the followed point, in the group's own rotation.
            // The group never rotates (it's at 0,0,0), but converting keeps this right if that ever changes.
            Vector3 local = Quaternion.Inverse(transform.rotation) * lookaheadOffset;
            if (composer.TargetOffset != local) composer.TargetOffset = local;
        }
    }
}
