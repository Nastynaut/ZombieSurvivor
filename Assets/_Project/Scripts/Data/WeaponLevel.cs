using System;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.3: the stats of a weapon at ONE level. Values are ABSOLUTE (not "+3 damage"),
    /// so level 4 simply reads row 4. That is easier to balance in a spreadsheet.
    /// </summary>
    [Serializable]
    public struct WeaponLevel
    {
        [Tooltip("Which level this row is for (1 = first).")]
        public int level;

        [Tooltip("HP removed per hit, before the player's Might multiplier and crits.")]
        public float damage;

        [Tooltip("Seconds between attacks, before the player's Cooldown multiplier.")]
        public float cooldown;

        [Tooltip("Range / radius in metres, before the player's Area multiplier.")]
        public float area;

        [Tooltip("How many instances (slashes, bolts, maces). Longsword: 1 = front, 2 = + rear, 3 = + sides.")]
        public int amount;

        [Tooltip("Max enemies one instance can hit. -1 = unlimited.")]
        public int pierce;

        [Tooltip("Seconds the attack stays active (0 = instant / permanent). Longsword: how long the slash is visible.")]
        public float duration;

        [Tooltip("Knockback speed in m/s given to hit enemies.")]
        public float knockback;

        [Tooltip("Short text shown on the level-up card, e.g. \"+3 damage\".")]
        public string description;
    }
}
