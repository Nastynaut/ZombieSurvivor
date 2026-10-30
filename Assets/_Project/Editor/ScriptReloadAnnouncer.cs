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
    }
}
