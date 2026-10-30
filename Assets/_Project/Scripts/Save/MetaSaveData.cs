using System;
using System.Collections.Generic;
using KnightSurvivors.Core;

namespace KnightSurvivors.Save
{
    /// <summary>
    /// Everything that survives between runs, stored as JSON (see SaveSystem).
    /// Plain C# class + [Serializable] so Unity's JsonUtility can read and write it.
    /// Add new fields at the END and give them defaults; old save files then still load.
    /// </summary>
    [Serializable]
    public class MetaSaveData
    {
        /// <summary>Bump this when the layout changes in a way that needs converting old saves.</summary>
        public int version = 1;

        /// <summary>Crowns in the bank. Stored as a float so 0.05-per-kill fractions add up (GDD 6.1).</summary>
        public float crowns;

        /// <summary>Armory ranks bought, by MetaUpgrade id (Armory UI is deferred).</summary>
        public List<MetaRank> upgradeRanks = new List<MetaRank>();

        /// <summary>Unlock ids already earned (UnlockDefinition.id).</summary>
        public List<string> unlocked = new List<string>();

        // ---- Stats ----
        public int runsPlayed;
        public int totalKills;
        public float bestTimeSeconds;

        // ---- Options (GDD 7.6 / 2.2) ----
        public BloodIntensity bloodIntensity = BloodIntensity.Full;
        public AimMode aimMode = AimMode.Assisted;
        public float screenShake = 1f;
        public bool damageNumbers = true;

        public int GetRank(string upgradeId)
        {
            foreach (var r in upgradeRanks)
            {
                if (r.id == upgradeId) return r.rank;
            }

            return 0;
        }

        public void SetRank(string upgradeId, int rank)
        {
            foreach (var r in upgradeRanks)
            {
                if (r.id == upgradeId)
                {
                    r.rank = rank;
                    return;
                }
            }

            upgradeRanks.Add(new MetaRank { id = upgradeId, rank = rank });
        }
    }

    [Serializable]
    public class MetaRank
    {
        public string id;
        public int rank;
    }
}
