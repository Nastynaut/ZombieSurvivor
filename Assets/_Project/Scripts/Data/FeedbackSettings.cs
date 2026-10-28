using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.12: global "juice" and gore budgets. Field names match the GDD exactly.
    ///
    /// These caps are what keep the game at 60 fps when 300 zombies explode at once: every pool and
    /// effect manager reads its limit from here instead of a hard-coded number.
    ///   * GoreManager uses maxBloodSystems, maxBloodParticles, maxDecals, decal*, bloodIntensity.
    ///   * EnemyManager uses maxCorpses, knockbackDecay, knockbackMax.
    ///   * Hit-stop and damage numbers come in a later PR (fields are ready).
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Feedback Settings", fileName = "CFG_FeedbackSettings")]
    public class FeedbackSettings : ScriptableObject
    {
        [Tooltip("Global hit-stops are rate-limited to one per this many ms. Not used yet.")]
        public int globalStopMinIntervalMs = 150;
        [Tooltip("At most this many new ground decals per frame; the rest wait in a queue (GDD 7.5).")]
        public int maxNewDecalsPerFrame = 32;

        [Header("Blood")]
        [Tooltip("Max blood burst effects alive at once. When full, the OLDEST is reused (never grows).")]
        public int maxBloodSystems = 96;
        [Tooltip("Max live blood particles across all bursts. New bursts get fewer particles near the limit.")]
        public int maxBloodParticles = 3000;
        [Tooltip("Blood burst prefabs created at startup (GDD 7.8). Pools still grow if needed.")]
        public int bloodPrewarmHit = 64;
        public int bloodPrewarmCrit = 24;
        public int bloodPrewarmKill = 48;

        [Header("Ground decals")]
        [Tooltip("Max ground blood decals; the oldest is recycled when full. 100 for the slice (48 later, when we switch to URP Decal Projectors + a splat map).")]
        public int maxDecals = 100; // 48 later
        [Tooltip("Seconds a decal stays (including the fade).")]
        public float decalLifetime = 20f;
        [Tooltip("Seconds of fade at the end of a decal's life.")]
        public float decalFade = 4f;
        [Tooltip("Seconds a decal looks wet/fresh before it swaps to the dried look.")]
        public float decalWetTime = 6f;
        [Tooltip("Fresh blood color on the ground (sRGB #B3121B).")]
        public Color decalFreshColor = new Color32(0xB3, 0x12, 0x1B, 0xFF);
        [Tooltip("Dried blood color on the ground (sRGB #7A0A10).")]
        public Color decalDryColor = new Color32(0x7A, 0x0A, 0x10, 0xFF);
        [Tooltip("Blood Off: grey scorch marks allowed instead of red decals, at most this many.")]
        public int maxScorchMarks = 50;
        [Tooltip("If decalMergeThreshold decals are already within this radius (m), grow the nearest instead of adding one.")]
        public float decalMergeRadius = 1.0f;
        public int decalMergeThreshold = 3;
        [Tooltip("A merged decal never grows past this scale.")]
        public float decalMaxScale = 2.0f;

        [Header("Damage numbers (later PR)")]
        public int maxDamageNumbers = 128;
        [Tooltip("Hits on the same enemy within this many seconds merge into one number.")]
        public float damageNumberMergeWindow = 0.15f;

        [Header("Corpses and knockback")]
        [Tooltip("Max dead zombies lying on the ground. Oldest sinks away quickly when over the cap.")]
        public int maxCorpses = 60;
        [Tooltip("Seconds a corpse lies on the ground before sinking.")]
        public float corpseLieTime = 3f;
        [Tooltip("Seconds the sink takes.")]
        public float corpseSinkTime = 1.5f;
        [Tooltip("How far (m) the corpse sinks below the floor before it's returned to the pool.")]
        public float corpseSinkDepth = 0.6f;
        [Tooltip("Seconds for knockback to decay to zero.")]
        public float knockbackDecay = 0.15f;
        [Tooltip("Max knockback speed (m/s).")]
        public float knockbackMax = 12f;

        [Header("Player and pools")]
        [Tooltip("Color the knight flashes when hurt (HP red #E23A3A).")]
        public Color playerHurtFlashColor = new Color32(0xE2, 0x3A, 0x3A, 0xFF);
        [Tooltip("GDD 7.8: every pool can grow (always true; kept as data so it's documented). Caps above are enforced by recycling the oldest.")]
        public bool poolsGrowable = true;

        [Header("Options")]
        [Tooltip("Gore option: Off (grey dust, no decals) / Low (half) / Full (default).")]
        public BloodIntensity bloodIntensity = BloodIntensity.Full;

        /// <summary>GDD 7.6: Off = 0, Low = 0.5, Full = 1.</summary>
        public float BloodIntensityValue
        {
            get
            {
                switch (bloodIntensity)
                {
                    case BloodIntensity.Off: return 0f;
                    case BloodIntensity.Low: return 0.5f;
                    default: return 1f;
                }
            }
        }
    }
}
