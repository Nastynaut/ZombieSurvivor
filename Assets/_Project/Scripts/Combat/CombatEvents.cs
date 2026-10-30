using System;
using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Combat
{
    /// <summary>
    /// One global "something got hurt / something died" broadcast.
    ///
    /// WHY GLOBAL EVENTS?
    /// The gore system wants to know about EVERY hit on EVERY zombie. Subscribing to 350 Health
    /// components one by one (and unsubscribing when they are pooled) is fiddly. Instead each Health
    /// also reports here, and the GoreManager subscribes once.
    ///
    /// These are "hooks": gameplay never depends on who is listening. Blood, sound, damage numbers,
    /// stats tracking can all plug in without touching combat code.
    /// </summary>
    public static class CombatEvents
    {
        /// <summary>(victim, hit info with the final amount, HP before the hit)</summary>
        public static event Action<Health, DamageInfo, float> AnyDamaged;

        /// <summary>(victim, killing blow, HP before the killing blow)</summary>
        public static event Action<Health, DamageInfo, float> AnyDied;

        internal static void RaiseDamaged(Health victim, DamageInfo info, float healthBefore)
        {
            AnyDamaged?.Invoke(victim, info, healthBefore);
        }

        internal static void RaiseDied(Health victim, DamageInfo info, float healthBefore)
        {
            AnyDied?.Invoke(victim, info, healthBefore);
        }

        // Static events survive between Play sessions when "Enter Play Mode Options" skips the
        // domain reload. Clear them at startup so old listeners from a previous session don't fire.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AnyDamaged = null;
            AnyDied = null;
        }
    }
}
