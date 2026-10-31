using System.Collections.Generic;
using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.2: everything about one weapon, as a data asset.
    /// Create one with: right-click in the Project window > Create > Knight Survivors > Weapon Definition.
    ///
    /// The <see cref="prefab"/> holds the weapon's BEHAVIOUR (for example a LongswordWeapon component) plus
    /// its visual. To make a new weapon type you write a new WeaponBase subclass once; to make a new
    /// weapon VARIANT (different numbers) you just duplicate this asset and change the numbers.
    ///
    /// Numbers come from GDD v0.1 (docs/GDD.md 3.2). Tune them here, not in code.
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Weapon Definition", fileName = "WPN_NewWeapon")]
    public class WeaponDefinition : ScriptableObject
    {
        [Tooltip("Unique id used in save files and lookups, e.g. \"longsword\". Never change it once shipped.")]
        public string id = "longsword";

        [Tooltip("Name shown to the player.")]
        public string displayName = "Longsword";

        [Tooltip("Card icon (optional for the graybox).")]
        public Sprite icon;

        [Tooltip("What kind of weapon behaviour this is. The prefab's component must match.")]
        public WeaponArchetype archetype = WeaponArchetype.MeleeArc;

        [Tooltip("How the weapon picks a direction or target.")]
        public TargetingMode targetingMode = TargetingMode.AimDirection;

        public DamageType damageType = DamageType.Physical;

        [Tooltip("Width of a melee arc in degrees (Longsword: 120).")]
        public float arcDegrees = 120f;

        [Tooltip("m/s for projectiles (deg/s for Orbit). 0 for melee.")]
        public float projectileSpeed;

        [Tooltip("Metres. For AimDirection weapons this is the assisted-aim range used to find the nearest enemy.")]
        public float range = 6f;

        [Tooltip("Seconds between hits on the same enemy for lingering attacks (orbits, zones). 0 = n/a (DataTables §1.1).")]
        public float hitInterval = 0.5f;

        [Tooltip("Half-angle of a lob spread in degrees (Throwing Axes: 20 = ±20°). 0 = no spread (DataTables §0.1).")]
        public float spreadHalfAngle;

        [Tooltip("HP healed per kill (Executioner's Greatsword: 1). 0 = none (DataTables §0.1).")]
        public float killHeal;

        [Tooltip("Max HP/s from killHeal (Executioner's Greatsword: 10). 0 = none (DataTables §0.1).")]
        public float killHealCapPerSec;

        [Tooltip("True if projectiles return to the player (Bloodstorm Cleavers). DataTables §0.1.")]
        public bool boomerang;

        [Tooltip("Highest level this weapon can reach.")]
        public int maxLevel = 8;

        [Tooltip("One row per level. Row 0 = level 1.")]
        public List<WeaponLevel> levels = new List<WeaponLevel>();

        [Tooltip("True for evolved weapons (they have a single level row).")]
        public bool isEvolution;

        [Tooltip("Prefab with the weapon behaviour (e.g. WPN_Longsword with a LongswordWeapon component) and its visual.")]
        public GameObject prefab;

        [Tooltip("Random hit sounds (not used in the graybox yet).")]
        public AudioClip[] hitSfx;

        /// <summary>
        /// Safely get the stats row for a level (1-based). Clamps to the rows that exist,
        /// so a missing row never crashes the game.
        /// </summary>
        public WeaponLevel GetLevel(int level)
        {
            if (levels == null || levels.Count == 0)
            {
                // Fallback so a half-filled asset still does something visible.
                return new WeaponLevel { level = 1, damage = 10f, cooldown = 1f, area = 2.5f, amount = 1, pierce = -1, duration = 0.2f, knockback = 5f, description = "(no level rows set)" };
            }

            int index = Mathf.Clamp(level - 1, 0, levels.Count - 1);
            return levels[index];
        }

        /// <summary>The real max level: the smaller of maxLevel and how many rows exist.</summary>
        public int EffectiveMaxLevel => Mathf.Max(1, Mathf.Min(maxLevel, levels != null && levels.Count > 0 ? levels.Count : 1));
    }
}
