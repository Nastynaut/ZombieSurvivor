using Unity.Cinemachine;
using UnityEngine;

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup).
    ///
    /// Press F2 to switch the main camera between:
    ///   Cinemachine  - the real rig (CinemachineBrain + CM_IsoCamera + PlayerTargetGroup)
    ///   SimpleFollow - SimpleIsoFollow, a few lines of plain code (no Cinemachine at all)
    /// Every press prints "[TEMP DEBUG] F2 follow: ..." to the Console; the readout shows the mode too.
    ///
    /// The key is read through DebugKeys (not the player's PlayerInput), so it works no matter which
    /// control scheme the player uses AND whatever the project's Active Input Handling setting is.
    /// (Click inside the Game view first: Unity only sends keys to the game when the Game view has focus.)
    /// </summary>
    public class CameraFollowModeToggle : MonoBehaviour
    {
        public enum Mode { Cinemachine, SimpleFollow }

        /// <summary>What the readout shows. Static so the readout doesn't need a reference.</summary>
        public static Mode Current { get; private set; } = Mode.Cinemachine;

        [Tooltip("The main camera's CinemachineBrain.")]
        public CinemachineBrain brain;

        [Tooltip("The iso virtual camera (CM_IsoCamera).")]
        public CinemachineCamera virtualCamera;

        [Tooltip("The plain-code follow camera on the main camera.")]
        public SimpleIsoFollow simpleFollow;

        private void Update()
        {
            if (!DebugKeys.WasPressed(DebugKeys.Key.F2)) return;
            SetMode(Current == Mode.Cinemachine ? Mode.SimpleFollow : Mode.Cinemachine, log: true);
        }

        public void SetMode(Mode mode, bool log)
        {
            bool useCinemachine = mode == Mode.Cinemachine || simpleFollow == null;
            if (useCinemachine) mode = Mode.Cinemachine;

            if (simpleFollow != null) simpleFollow.enabled = !useCinemachine;
            if (virtualCamera != null) virtualCamera.enabled = useCinemachine;
            if (brain != null) brain.enabled = useCinemachine;
            if (!useCinemachine) simpleFollow.Snap(); // start exactly on the player, no swoop

            Current = mode;
            if (log)
            {
                string name = mode == Mode.Cinemachine ? "Cinemachine" : "SimpleFollow";
                string note = simpleFollow == null ? " (SimpleFollow not available yet: camera not wired)" : "";
                Debug.LogWarning($"[TEMP DEBUG] F2 follow: {name}{note}");
            }
        }
    }
}
