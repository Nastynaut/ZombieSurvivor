using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.11: a character's BASE stats (Sir Aldric's values from GDD 3.1).
    /// Referenced by CharacterDefinition.baseStats. Field names match the GDD exactly.
    /// Create with: Create > Knight Survivors > Player Stats.
    ///
    /// This asset is never changed during play. At the start of a run the player copies these values
    /// into a RuntimeStats object (see <see cref="ToRuntimeStats"/>), and upgrades change that copy.
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Player Stats", fileName = "STATS_NewCharacter")]
    public class PlayerStats : ScriptableObject
    {
        public float maxHealth = 120f;
        [Tooltip("HP regenerated per second.")]
        public float recovery = 0f;
        [Tooltip("Flat damage reduction per hit (minimum 1 damage is always taken).")]
        public float armor = 0f;
        [Tooltip("Metres per second.")]
        public float moveSpeed = 5f;
        [Tooltip("Damage multiplier.")]
        public float might = 1f;
        [Tooltip("Weapon area / radius multiplier.")]
        public float area = 1f;
        [Tooltip("Weapon cooldown multiplier (lower = faster). Floor 0.40.")]
        public float cooldown = 1f;
        public float duration = 1f;
        public float projectileSpeed = 1f;
        [Tooltip("Extra projectiles / instances for every weapon.")]
        public int amount = 0;
        public float critChance = 0.05f;
        public float critMultiplier = 2f;
        [Tooltip("XP gem magnet radius in metres.")]
        public float pickupRadius = 2.5f;
        public float luck = 1f;
        [Tooltip("XP multiplier.")]
        public float growth = 1f;
        [Tooltip("Crown multiplier.")]
        public float greed = 1f;
        public int revivals = 0;
        public int rerolls = 2;
        public int banishes = 1;
        [Tooltip("Dodge roll (NOT implemented in the graybox yet).")]
        public float dodgeDistance = 4f;
        public float dodgeDuration = 0.3f;
        public float dodgeIFrames = 0.3f;
        public float dodgeCooldown = 3f;
        // Slot counts are config values: 4 weapons and 4 passives (confirmed).
        // The GDD slice's "3 + 3" is how many weapons and passives the slice CONTAINS, not the slot count.
        [Tooltip("Max different weapons a player can hold (4).")]
        public int weaponSlots = 4;
        [Tooltip("Max different passives a player can hold.")]
        public int passiveSlots = 4;

        /// <summary>Copy the base values into a fresh RuntimeStats for one run.</summary>
        public RuntimeStats ToRuntimeStats()
        {
            return new RuntimeStats
            {
                maxHealth = maxHealth,
                recovery = recovery,
                armor = armor,
                moveSpeed = moveSpeed,
                might = might,
                area = area,
                cooldown = cooldown,
                amount = amount,
                duration = duration,
                projectileSpeed = projectileSpeed,
                critChance = critChance,
                critMultiplier = critMultiplier,
                pickupRadius = pickupRadius,
                luck = luck,
                growth = growth,
                greed = greed,
                revivals = revivals,
                rerolls = rerolls,
                banishes = banishes
            };
        }
    }
}
