using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.13: difficulty multipliers by player count. Index = playerCount - 1.
    /// Only the 1-player row (index 0) matters until co-op ships, but the spawner already reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Coop Scaling", fileName = "CFG_CoopScaling")]
    public class CoopScaling : ScriptableObject
    {
        public float[] enemyCountMult = { 1.0f, 1.6f, 2.1f, 2.5f };
        public float[] enemyHpMult = { 1.0f, 1.25f, 1.45f, 1.6f };
        public float[] bossHpMult = { 1.0f, 1.7f, 2.3f, 2.8f };
        public int[] hardCap = { 350, 450, 500, 550 };
        public float[] xpMult = { 1.0f, 1.4f, 1.7f, 2.0f };

        /// <summary>Safely read a value for a player count (clamped to the array).</summary>
        public static float Pick(float[] values, int playerCount, float fallback = 1f)
        {
            if (values == null || values.Length == 0) return fallback;
            return values[Mathf.Clamp(playerCount - 1, 0, values.Length - 1)];
        }

        public static int Pick(int[] values, int playerCount, int fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            return values[Mathf.Clamp(playerCount - 1, 0, values.Length - 1)];
        }
    }
}
