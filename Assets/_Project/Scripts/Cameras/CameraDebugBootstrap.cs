using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup).
    ///
    /// Adds the camera debug tools (F2 follow toggle, F3 readout, wiring self-check) to every scene that
    /// has a Cinemachine main camera, from code. So nobody has to rebuild the scene to get them, and
    /// deleting this folder's TEMPORARY files later removes them completely.
    /// </summary>
    public static class CameraDebugBootstrap
    {
        private static GameObject installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnFirstSceneLoaded()
        {
            // Also run after later scene loads (restarting a run reloads the scene).
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Install();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Install();
        }

        private static void Install()
        {
            if (installed != null) return; // already there (Unity's == null also catches a destroyed object)

            var cam = Camera.main;
            if (cam == null) return;
            var brain = cam.GetComponent<CinemachineBrain>();
            if (brain == null) return;

            CinemachineCamera vcam = null;
            for (int i = 0; i < CinemachineCore.VirtualCameraCount; i++)
            {
                vcam = CinemachineCore.GetVirtualCamera(i) as CinemachineCamera;
                if (vcam != null) break;
            }

            var simple = cam.GetComponent<SimpleIsoFollow>();
            if (simple == null) simple = cam.gameObject.AddComponent<SimpleIsoFollow>();
            simple.enabled = false; // Cinemachine is the default; F2 switches

            installed = new GameObject("TEMP_CameraDebug");

            var toggle = installed.AddComponent<CameraFollowModeToggle>();
            toggle.brain = brain;
            toggle.virtualCamera = vcam;
            toggle.simpleFollow = simple;

            var check = installed.AddComponent<CameraRigSelfCheck>();
            check.brain = brain;
            check.virtualCamera = vcam;

            installed.AddComponent<CameraJitterReadout>();

            Debug.Log("[TEMP CameraDebugBootstrap] Camera diagnostics added: F2 = switch Cinemachine / SimpleFollow, " +
                      "F3 = hide/show the jitter readout. (Remove in the PR #1 cleanup.)");
        }
    }
}
