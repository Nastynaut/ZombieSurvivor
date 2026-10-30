using Unity.Cinemachine;
using UnityEngine;

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup).
    ///
    /// BACKUP way to get the camera debug tools (F2 follow toggle, F3 readout, wiring self-check). The main
    /// way is that the scene builder now puts them straight on the "Systems" object. This bootstrap only
    /// adds them when the scene doesn't already have them (an old scene that wasn't rebuilt).
    ///
    /// WHY IT IS BUILT LIKE THIS (the a3146a4 version sometimes showed nothing at all):
    /// The old version ran ONCE right after the scene loaded and silently gave up if Camera.main or the
    /// CinemachineBrain wasn't there at that exact moment, so no readout and no F2/F3 ever existed and
    /// there was no message saying why. Now:
    ///   1. It logs the moment it runs (so the Console proves the bootstrap happened), as a yellow WARNING
    ///      so the Console's info filter can't hide it, and says if the Input System is switched off.
    ///   2. It ALWAYS creates the debug object, straight away, and keeps it alive across scene reloads
    ///      (DontDestroyOnLoad), whatever the scene contains.
    ///   3. The debug object itself (CameraDebugHost) looks for the camera every frame until it finds it,
    ///      and again after a restart reloads the scene. The readout says "camera not found" meanwhile.
    /// </summary>
    public static class CameraDebugBootstrap
    {
        private static GameObject installed;

        // With "Enter Play Mode Options" (no domain reload) static fields survive between Play sessions.
        // SubsystemRegistration runs first thing in every Play session, so we reset them there.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            installed = null;
            CameraDebugHost.Instance = null;
        }

        // Runs at the start of EVERY Play session, before the first scene loads, whatever the scene
        // contains and whatever the input settings are. It only writes to the Console, so nothing can
        // stop it. If you press Play and don't see this line, Unity is not running these scripts.
        //
        // LogWarning (yellow) instead of Log (grey) ON PURPOSE: the Console has three filter buttons in
        // its top-right corner (grey speech bubble = info, yellow triangle = warnings, red = errors).
        // If the grey one is switched off, every Debug.Log line is hidden while warnings still show.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AnnounceAndCheckInput()
        {
            Debug.LogWarning("[TEMP DEBUG] CameraDebugBootstrap running, build " + CameraJitterReadout.BuildStamp +
                             ". Active Input Handling: " + DebugKeys.ActiveInputHandling +
                             ". (Camera diagnostics: readout ON by default, F3 hides it, F2 switches follow mode.)");

            // All gameplay input (PlayerInput / PlayerInputHandler, level-up keys) uses the Input System
            // package. If it's switched off, the game gets no keyboard/gamepad at all, so shout about it.
            if (!DebugKeys.InputSystemEnabled)
            {
                Debug.LogError("[Knight Survivors] The Input System is switched OFF for this project, so the knight " +
                               "can't be controlled. Fix: " + DebugKeys.FixHint);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (installed != null) return; // Unity's == null is also true for a destroyed object

            // AfterSceneLoad runs after the scene objects' Awake/OnEnable, so if the scene builder already
            // put a CameraDebugHost on "Systems", it has registered itself by now and we're not needed.
            if (CameraDebugHost.Instance != null)
            {
                Debug.LogWarning("[TEMP DEBUG] Camera diagnostics found in the scene on '" + CameraDebugHost.Instance.name + "'.");
                return;
            }

            Debug.LogWarning("[TEMP DEBUG] No camera diagnostics in this scene (old scene?), adding TEMP_CameraDebug.");
            installed = new GameObject("TEMP_CameraDebug");
            Object.DontDestroyOnLoad(installed);
            installed.AddComponent<CameraDebugHost>();
        }
    }

    /// <summary>
    /// TEMPORARY. Lives on "Systems" (added by the scene builder) or on TEMP_CameraDebug (added by the
    /// bootstrap above for old scenes). Makes sure the readout, F2 toggle and self-check exist next to it
    /// (re-using ones the builder already added), then finds the main camera / brain / virtual camera whenever they
    /// are missing (first frames, after a scene reload) and hands them to the toggle and the self-check.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // wire things up before the other debug scripts run this frame
    public class CameraDebugHost : MonoBehaviour
    {
        private CameraFollowModeToggle toggle;
        private CameraRigSelfCheck check;
        private bool loggedWired;

        /// <summary>The host that is running right now (null if none). Lets the bootstrap avoid a duplicate.</summary>
        public static CameraDebugHost Instance { get; set; }

        private void Awake()
        {
            // The readout first: it must show even if nothing else can be wired.
            GetOrAdd<CameraJitterReadout>();
            toggle = GetOrAdd<CameraFollowModeToggle>();
            check = GetOrAdd<CameraRigSelfCheck>();
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        // Use the component if the scene builder already added it, otherwise add one.
        private T GetOrAdd<T>() where T : Component
        {
            var existing = GetComponent<T>();
            if (existing != null) return existing;
            return gameObject.AddComponent<T>();
        }

        private void Update()
        {
            if (toggle.brain != null && toggle.simpleFollow != null) return; // all wired

            var cam = Camera.main;
            if (cam == null) { CameraJitterReadout.WiringStatus = "camera not found (no enabled camera tagged MainCamera)"; loggedWired = false; return; }
            var brain = cam.GetComponent<CinemachineBrain>();
            if (brain == null) { CameraJitterReadout.WiringStatus = "main camera has no CinemachineBrain"; loggedWired = false; return; }

            CinemachineCamera vcam = null;
            for (int i = 0; i < CinemachineCore.VirtualCameraCount; i++)
            {
                vcam = CinemachineCore.GetVirtualCamera(i) as CinemachineCamera;
                if (vcam != null) break;
            }

            var simple = cam.GetComponent<SimpleIsoFollow>();
            if (simple == null)
            {
                simple = cam.gameObject.AddComponent<SimpleIsoFollow>();
                simple.enabled = false; // Cinemachine is the default; F2 switches
            }

            toggle.brain = brain;
            toggle.virtualCamera = vcam;
            toggle.simpleFollow = simple;
            toggle.SetMode(CameraFollowModeToggle.Mode.Cinemachine, log: false); // a reload always starts on Cinemachine
            check.brain = brain;
            check.virtualCamera = vcam;

            if (!loggedWired)
            {
                loggedWired = true;
                Debug.LogWarning($"[TEMP DEBUG] Camera diagnostics wired to '{cam.name}' (vcam: {(vcam != null ? vcam.name : "none")}).");
            }
        }
    }
}
