using KnightSurvivors.Cameras;
using UnityEditor;
using UnityEngine;

namespace KnightSurvivors.EditorTools
{
    /// <summary>
    /// TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup).
    ///
    /// [InitializeOnLoad] makes Unity run the static constructor below every time it finishes compiling
    /// and reloading our scripts (after a git pull + Ctrl+R, when the Editor opens, when you press Play).
    /// So right after Ctrl+R the Console tells you, WITHOUT pressing Play:
    ///   * which version of the scripts is loaded (the build stamp), and
    ///   * whether the Input System is switched on (all gameplay input needs it).
    /// No line after Ctrl+R = Unity did not reload the new scripts (look for red compile errors).
    ///
    /// It's a yellow warning, not a grey log, so the Console's info filter can't hide it.
    /// </summary>
    [InitializeOnLoad]
    internal static class ScriptReloadAnnouncer
    {
        static ScriptReloadAnnouncer()
        {
            // Also report what the scene really contains every time Play starts (see OnPlayModeChanged).
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            // Don't spam on the extra reload that happens when entering Play mode.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Debug.LogWarning("[Knight Survivors] Scripts loaded: " + CameraJitterReadout.BuildStamp +
                             ". Active Input Handling: " + DebugKeys.ActiveInputHandling + ".");

            if (!DebugKeys.InputSystemEnabled)
            {
                Debug.LogError("[Knight Survivors] The Input System is switched OFF, so the knight can't be controlled. " +
                               "Fix: " + DebugKeys.FixHint);
            }
        }

        // Runs in the EDITOR's code (not the game's), right after Play starts. It doesn't depend on any
        // game script running, so it tells us what Play mode really contains even if nothing else logs.
        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode) return;

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            int readouts = CountInScenes<CameraJitterReadout>();
            int hosts = CountInScenes<CameraDebugHost>();
            Debug.LogWarning("[Knight Survivors] Play started in scene '" + scene.name + "' (" + scene.path + "). " +
                             "Readouts: " + readouts + ", debug hosts: " + hosts + ", build " + CameraJitterReadout.BuildStamp +
                             ". Active Input Handling: " + DebugKeys.ActiveInputHandling + ".");
            if (readouts == 0)
            {
                Debug.LogError("[Knight Survivors] No CameraJitterReadout in Play mode. Is Graybox.unity the open scene? " +
                               "Run Knight Survivors > Build Graybox Scene, then press Play.");
            }
        }

        // How many T components exist in the loaded scenes, including ones on inactive objects.
        // (FindObjectsOfTypeAll also returns prefab assets, so we keep only objects that live in a scene.)
        private static int CountInScenes<T>() where T : Component
        {
            int count = 0;
            foreach (var c in Resources.FindObjectsOfTypeAll<T>())
            {
                if (c.gameObject.scene.IsValid()) count++;
            }
            return count;
        }
    }
}
