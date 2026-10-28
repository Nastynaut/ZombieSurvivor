using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.8: how much XP each level needs, plus XP gem settings.
    /// xpToNext(L) = baseXp + round(linearPerLevel x (L-1)) + floor(quadraticPerLevel x (L-1)^2)
    /// With the defaults: L1 = 5, L2 = 11, L3 = 17, L10 = 67 (see the table in GDD 4.1).
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/XP Curve", fileName = "CFG_XpCurve")]
    public class XpCurve : ScriptableObject
    {
        public int baseXp = 5;
        public float linearPerLevel = 6f;
        public float quadraticPerLevel = 0.1f;
        [Tooltip("Optional: if this curve has keys, it REPLACES the formula (x = level, y = XP to next level).")]
        public AnimationCurve overrideCurve = new AnimationCurve();
        [Tooltip("XP value of each gem tier: small, medium, large.")]
        public int[] gemValues = { 1, 5, 25 };
        [Tooltip("Max gems lying on the ground. Past this, new XP merges into an old far-away gem.")]
        public int maxGemsOnGround = 400;
        [Tooltip("Gem fly-to-player start speed (m/s).")]
        public float magnetSpeed = 12f;
        [Tooltip("Gem fly-to-player acceleration (m/s^2).")]
        public float magnetAccel = 20f;

        /// <summary>XP needed to go from <paramref name="level"/> to level + 1 (GDD 9.8 formula).</summary>
        public int XpToNext(int level)
        {
            if (overrideCurve != null && overrideCurve.length > 0)
            {
                return Mathf.Max(1, Mathf.RoundToInt(overrideCurve.Evaluate(level)));
            }

            return baseXp + Mathf.RoundToInt(linearPerLevel * (level - 1)) + Mathf.FloorToInt(quadraticPerLevel * (level - 1) * (level - 1));
        }
    }
}
