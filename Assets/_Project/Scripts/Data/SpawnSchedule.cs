using System;
using System.Collections.Generic;
using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.7: the whole run's spawn pacing. One WaveEntry per minute (row "minute 5" is used from 5:00 to 5:59).
    /// Create with: Create > Knight Survivors > Spawn Schedule.
    /// Numbers come from GDD v0.1 (docs/GDD.md section 5 / vertical slice 8.1).
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Spawn Schedule", fileName = "SPN_NewSchedule")]
    public class SpawnSchedule : ScriptableObject
    {
        [Tooltip("Run length in minutes (slice: 10, full game: 15).")]
        public float runLengthMinutes = 10f;
        [Tooltip("Minute the boss appears. Boss NOT implemented in the graybox.")]
        public float bossMinute = 10f;
        public EnemyDefinition boss;
        [Tooltip("Max live enemies (1 player). GDD: 350.")]
        public int hardCap = 350;
        [Tooltip("Spawn ring inner radius in metres from a player (outside the camera view).")]
        public float ringMinDistance = 20f;
        [Tooltip("Spawn ring outer radius in metres.")]
        public float ringMaxDistance = 24f;
        [Tooltip("Enemies further than this from EVERY player are teleported back onto the ring.")]
        public float recycleDistance = 35f;
        [Tooltip("0-1: share of spawns biased into a +/-60 degree cone in the player's move direction.")]
        [Range(0f, 1f)] public float moveDirBias = 0.6f;
        public List<WaveEntry> entries = new List<WaveEntry>();
        [Tooltip("Timed events (swarms, elites, boss). NOT implemented in the graybox yet.")]
        public List<WaveEvent> events = new List<WaveEvent>();
        [Tooltip("Co-op multipliers. Optional; the 1-player row is used when empty.")]
        public CoopScaling coopScaling;

        /// <summary>
        /// The entry to use at a given time: the last entry whose minute is &lt;= the current minute.
        /// Returns -1 if there are no entries.
        /// </summary>
        public int GetEntryIndexForTime(float elapsedSeconds)
        {
            if (entries == null || entries.Count == 0) return -1;

            int minute = Mathf.FloorToInt(elapsedSeconds / 60f);
            int best = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].minute <= minute) best = i;
            }
            return best;
        }
    }

    /// <summary>GDD 9.7 WaveEntry: how many enemies and which mix during one minute.</summary>
    [Serializable]
    public class WaveEntry
    {
        [Tooltip("Minute this row starts (0 = 0:00-0:59).")]
        public int minute;
        [Tooltip("If fewer enemies than this are alive, spawning runs at double speed to catch up.")]
        public int targetAlive = 15;
        [Tooltip("Normal spawns per second.")]
        public float spawnRatePerSec = 1f;
        [Tooltip("Which enemies spawn and how often (weights in %).")]
        public List<EnemyWeight> mix = new List<EnemyWeight>();
    }

    /// <summary>One enemy type in a wave mix.</summary>
    [Serializable]
    public class EnemyWeight
    {
        public EnemyDefinition enemy;
        [Tooltip("Relative chance in percent (the mix does not have to add up to exactly 100).")]
        public float weightPct = 100f;
    }

    /// <summary>GDD 9.7 WaveEvent: a one-off scripted moment (swarm, elite, boss). Data only for now.</summary>
    [Serializable]
    public class WaveEvent
    {
        public float timeSeconds;
        public WaveEventType type;
        public EnemyDefinition enemy;
        public int count;
        public SpawnPattern pattern;
        public bool ignoreCap = true;
    }
}
