using KnightSurvivors.Core;
using KnightSurvivors.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// TEMPORARY debug tool (camera-jitter investigation). Delete this script, CameraShakeGate and
    /// FeedbackSettings.cameraShakeEnabled once the camera is confirmed smooth.
    ///
    /// * At start, copies FeedbackSettings.cameraShakeEnabled (Inspector tick box) into CameraShakeGate.
    /// * Press F1 in Play mode to flip it. The Console logs "Camera shake: ON" or "OFF".
    ///
    /// F1 is not used by any gameplay or UI action. We read the keyboard directly here (instead of the
    /// PlayerInput actions) on purpose: it's a developer key, not a player control, and it's going away.
    /// </summary>
    public class CameraShakeDebugToggle : MonoBehaviour
    {
        public RunConfig runConfig;

        private void Start()
        {
            var settings = runConfig != null ? runConfig.feedbackSettings : null;
            if (settings != null) CameraShakeGate.Enabled = settings.cameraShakeEnabled;
            Debug.Log("[TEMP DEBUG] Camera shake: " + (CameraShakeGate.Enabled ? "ON" : "OFF") + " (press F1 to toggle)");
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f1Key.wasPressedThisFrame) return;

            CameraShakeGate.Enabled = !CameraShakeGate.Enabled;
            Debug.Log("[TEMP DEBUG] Camera shake: " + (CameraShakeGate.Enabled ? "ON" : "OFF"));
        }
    }
}
