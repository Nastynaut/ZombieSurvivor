using System;
using System.Collections.Generic;
using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.12 / 7.1: per hit type, how hard the game "freezes", shakes and flashes.
    /// The graybox only reads flashMs (enemy hit flash). Hit-stop and shake come in the juice PR.
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Hit Feedback Profile", fileName = "CFG_HitFeedback")]
    public class HitFeedbackProfile : ScriptableObject
    {
        public List<HitFeedbackEntry> entries = new List<HitFeedbackEntry>();

        /// <summary>Find the entry for a hit type (returns false if the profile has none).</summary>
        public bool TryGet(HitType hitType, out HitFeedbackEntry entry)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].hitType == hitType)
                    {
                        entry = entries[i];
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }
    }

    [Serializable]
    public struct HitFeedbackEntry
    {
        public HitType hitType;
        public int localStopMs;
        public int globalStopMs;
        [Tooltip("Metres.")]
        public float shakeAmplitude;
        [Tooltip("Seconds.")]
        public float shakeDuration;
        public int flashMs;
        public int bloodParticles;
        public int decalCount;
    }
}
