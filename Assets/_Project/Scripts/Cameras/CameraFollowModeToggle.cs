using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup).
    ///
    /// Press F2 to switch the main camera between:
    ///   Cinemachine  - the real rig (CinemachineBrain + CM_IsoCamera + PlayerTargetGroup)
    ///   SimpleFollow - SimpleIsoFollow, a few lines of plain code (no Cinemachine at all)
    /// The active mode is printed to the Console and shown in the F3 readout.
    ///
    /// Switching OFF Cinemachine means disabling the CinemachineBrain (so it stops moving the camera)
    /// AND the virtual camera (so nothing else drives it). Switching back re-enables both.
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

        private void Start()
        {
            // Start in Cinemachine mode (the real game), whatever was saved in the scene.
            SetMode(Mode.Cinemachine, log: false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f2Key.wasPressedThisFrame) return;
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
            if (log) Debug.Log($"[TEMP CameraFollowModeToggle] F2: camera follow mode is now {mode}.");
        }
    }
}
