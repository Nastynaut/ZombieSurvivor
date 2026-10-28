using System.Collections.Generic;
using UnityEngine;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// Pauses the game by setting Time.timeScale to 0.
    ///
    /// WHY NOT JUST SET Time.timeScale DIRECTLY?
    /// Several things can pause at the same time (a level-up screen AND the game-over screen,
    /// later a pause menu). If each one sets timeScale = 1 when it closes, it could accidentally
    /// un-pause the game while another screen is still open. So every pauser adds a "reason";
    /// the game only runs again when no reasons are left.
    ///
    /// Note: UI buttons keep working while paused, because uGUI uses unscaled time.
    /// </summary>
    public static class PauseService
    {
        private static readonly HashSet<object> Reasons = new HashSet<object>();

        public static bool IsPaused => Reasons.Count > 0;

        public static void AddPause(object reason)
        {
            Reasons.Add(reason);
            Apply();
        }

        public static void RemovePause(object reason)
        {
            Reasons.Remove(reason);
            Apply();
        }

        /// <summary>Remove every pause (used when restarting a run).</summary>
        public static void ClearAll()
        {
            Reasons.Clear();
            Apply();
        }

        private static void Apply()
        {
            Time.timeScale = IsPaused ? 0f : 1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Reasons.Clear();
            Time.timeScale = 1f;
        }
    }
}
