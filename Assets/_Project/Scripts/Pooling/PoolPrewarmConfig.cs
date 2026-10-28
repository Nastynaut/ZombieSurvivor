using System;
using System.Collections.Generic;
using UnityEngine;

namespace KnightSurvivors.Pooling
{
    /// <summary>
    /// A small list of "create N of this prefab at startup" entries for pools that don't have their
    /// own data asset: XP gems (400 / 64 / 32) and the Plague Spitter bile glob (32), GDD 7.8.
    /// Create with: Create > Knight Survivors > Pool Prewarm Config. Assign it on the PoolService.
    ///
    /// Enemies prewarm from EnemyDefinition.prewarmCount; blood bursts from FeedbackSettings.
    /// Prewarm is only a starting size: pools still grow if they run out.
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Pool Prewarm Config", fileName = "CFG_PoolPrewarm")]
    public class PoolPrewarmConfig : ScriptableObject
    {
        public List<PoolPrewarmEntry> entries = new List<PoolPrewarmEntry>();
    }

    [Serializable]
    public struct PoolPrewarmEntry
    {
        [Tooltip("Prefab to pool.")]
        public GameObject prefab;
        [Tooltip("How many to create at startup.")]
        public int prewarmCount;
    }
}
