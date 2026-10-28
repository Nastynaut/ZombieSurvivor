using UnityEngine;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// The fixed gameplay tick (GDD 9.14 / 11.3): all gameplay logic runs at 50 steps per second.
    ///
    /// HOW IT WORKS IN UNITY
    /// Unity already has a fixed-rate loop: FixedUpdate(). We set Time.fixedDeltaTime = 0.02 (50 Hz)
    /// and put gameplay logic (movement, spawning, weapons, damage, pickups) in FixedUpdate.
    /// Presentation (moving the visible model smoothly, UI, particles) stays in Update/LateUpdate.
    ///
    /// WHY?
    ///   * Same results at 60 fps or 240 fps: no "faster PC = different game" bugs.
    ///   * Online co-op later needs a fixed step to keep machines in sync.
    ///   * Pausing is free: Time.timeScale = 0 stops FixedUpdate.
    ///
    /// Because the logic only moves 50 times a second, the visible models would look choppy on a
    /// 144 Hz monitor. So presentation code blends between the last two tick positions using
    /// <see cref="InterpolationAlpha"/> (0 = previous tick, 1 = latest tick).
    /// </summary>
    public static class GameTick
    {
        public const int TickRate = 50;
        public const float TickDelta = 1f / TickRate;

        /// <summary>Call once at startup (RunManager does this).</summary>
        public static void Apply()
        {
            Time.fixedDeltaTime = TickDelta;
        }

        /// <summary>How far we are between the last tick and the next one, 0..1. Use in Update/LateUpdate only.</summary>
        public static float InterpolationAlpha
        {
            get
            {
                if (Time.fixedDeltaTime <= 0f) return 1f;
                return Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            }
        }
    }
}
