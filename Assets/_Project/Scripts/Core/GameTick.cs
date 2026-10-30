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

        /// <summary>
        /// How far we are between the last tick and the next one, 0..1. Use in Update/LateUpdate only.
        ///
        /// Unity runs its fixed steps at the start of a frame until Time.fixedTime has caught up with Time.time,
        /// so in Update (Time.time - Time.fixedTime) is normally between 0 and one tick. We divide by the SAME
        /// Time.fixedDeltaTime that drives FixedUpdate (GameTick.Apply sets it to TickDelta), so there is only
        /// one 50 Hz clock: no "two clocks drifting" beat.
        ///
        /// Defensive: if fixedTime is ever slightly AHEAD of Time.time (float rounding, odd platforms), the
        /// value is negative; we wrap it to "1 + value" instead of clamping to 0. Clamping would freeze the
        /// smoothed position on the previous tick for that frame and cause exactly the stutter we're avoiding.
        /// </summary>
        public static float InterpolationAlpha
        {
            get
            {
                float dt = Time.fixedDeltaTime;
                if (dt <= 0f) return 1f;
                float a = (Time.time - Time.fixedTime) / dt;
                if (a < 0f) a += 1f;
                return Mathf.Clamp01(a);
            }
        }
    }
}
