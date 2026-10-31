using System;
using System.Collections.Generic;
using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.6: one enemy type (Shambler, Runner, ...).
    /// Create with: Create > Knight Survivors > Enemy Definition.
    ///
    /// Runtime HP     = maxHealth x (1 + hpPerMinute x minute) x coop HP multiplier
    /// Runtime damage = contactDamage x (1 + damagePerMinute x minute)
    /// Numbers come from GDD v0.1 (docs/GDD.md 3.5).
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Enemy Definition", fileName = "ENM_NewEnemy")]
    public class EnemyDefinition : ScriptableObject
    {
        public string id = "shambler";
        public string displayName = "Shambler";
        public EnemyTier tier = EnemyTier.Normal;
        [Tooltip("Only Chase is implemented in the graybox.")]
        public EnemyBehavior behavior = EnemyBehavior.Chase;

        [Tooltip("HP at minute 0.")]
        public float maxHealth = 10f;
        [Tooltip("HP grows by this fraction per minute (0.25 = +25% per minute).")]
        public float hpPerMinute = 0.25f;
        [Tooltip("Metres per second.")]
        public float moveSpeed = 1.6f;
        [Tooltip("Damage dealt to a player on touch.")]
        public float contactDamage = 6f;
        [Tooltip("Contact damage grows by this fraction per minute.")]
        public float damagePerMinute = 0.05f;
        [Tooltip("Seconds between touches that can hurt the player.")]
        public float contactInterval = 0.5f;
        [Tooltip("XP dropped on death (split into gems).")]
        public int xpValue = 1;
        [Tooltip("Heavier enemies get knocked back less: knockback / mass.")]
        public float mass = 1f;
        [Tooltip("0 = full knockback, 1 = immune.")]
        [Range(0f, 1f)] public float knockbackResist;
        [Tooltip("Body radius in metres, used for separation, contact and hit checks. Set by data, never by the model (GDD 3.5). Shambler 0.3, Runner 0.2, Bloater 0.7, Spitter 0.25, Grave Knight 0.7, Rotting King 1.5. Default matches Shambler so a menu-created asset is not radius 0.")]
        public float radius = 0.3f;
        [Tooltip("How many of this enemy the pool creates at startup (GDD 7.8). Only a starting size: the pool grows if needed. Default matches Shambler (400) so a menu-created asset is not 0.")]
        public int prewarmCount = 400;
        [Tooltip("Ranged kiters stay between these distances (m) from their target. 0 / 0 = no kiting (GDD 9.6; Spitter 7 / 9).")]
        public float keepDistanceMin;
        public float keepDistanceMax;
        [Tooltip("First minute this enemy may appear in the wave mix.")]
        public float firstMinute;
        [Tooltip("Crowns (meta currency) dropped. Not used until the meta PR.")]
        public float crownValue = 0.05f;
        [Tooltip("Elites drop a chest. Not used until the chest PR.")]
        public bool dropsChest;
        [Tooltip("HP fraction that unlocks phase 2 (Rotting King: 0.5). 0 = no phase 2 (DataTables §0.1).")]
        [Range(0f, 1f)] public float phase2HpFraction;

        [Tooltip("Move speed while in phase 2 (Rotting King: 2.8). 0 = keep moveSpeed (DataTables §0.1).")]
        public float phase2MoveSpeed;

        [Tooltip("Contact + attack damage multiplier at enrage (Rotting King: 2). 0 = do not read. Boss only (DataTables §0.1).")]
        public float enrageDamageMult;

        [Tooltip("Speed multiplier at enrage (Rotting King: 1.3). 0 = do not read. Boss only (DataTables §0.1).")]
        public float enrageSpeedMult;

        [Tooltip("Special attacks (Bloater blast, boss slam). Not used in the graybox.")]
        public List<EnemyAttack> attackParams = new List<EnemyAttack>();

        [Tooltip("The enemy's logic prefab (root has Enemy + Health; the model is a child VisualSlot).")]
        public GameObject prefab;

        /// <summary>HP for an enemy spawned at the given minute.</summary>
        public float GetHealthAtMinute(float minute, float coopHpMult = 1f)
        {
            return maxHealth * (1f + hpPerMinute * minute) * coopHpMult;
        }

        /// <summary>Contact damage for an enemy spawned at the given minute.</summary>
        public float GetContactDamageAtMinute(float minute)
        {
            return contactDamage * (1f + damagePerMinute * minute);
        }
    }

    /// <summary>
    /// GDD 9.6 EnemyAttack. The GDD lists it as "(ref)"; here it is an inline [Serializable] class
    /// so it can be edited directly inside the enemy asset. Not used in the graybox.
    /// </summary>
    [Serializable]
    public class EnemyAttack
    {
        public string name;
        [Tooltip("Seconds of warning before the attack lands.")]
        public float telegraph;
        public float radius;
        [Tooltip("Max attack / projectile distance in metres (Spitter spit: 9).")]
        public float range;
        [Tooltip("Width in metres of a line attack or projectile (Spitter aim line / glob: 0.3, Grave Knight charge line: 1.4).")]
        public float width;
        [Tooltip("Cone width in degrees (cone attacks, e.g. 30).")]
        public float arcDegrees;
        [Tooltip("Directions in degrees for cones / volleys, relative to the aim (e.g. -40, 0, +40).")]
        public float[] angleOffsets = new float[0];
        public float damage;
        [Tooltip("Damage dealt to OTHER enemies (Bloater Death Blast: 50). Player damage stays in damage. 0 = none (DataTables §0.1).")]
        public float damageToEnemies;
        [Tooltip("Knockback speed in m/s (Ground Slam: 6). 0 = none (DataTables §0.1).")]
        public float knockback;
        public float interval;
        [Tooltip("Interval override while owner HP fraction ≤ phase2HpFraction (Ground Slam: 6). 0 = no override (DataTables §0.1).")]
        public float phaseInterval;
        public float projectileSpeed;
        public EnemyDefinition summonEnemy;
        public int summonCount;
        [Tooltip("0-1: only used below this HP fraction (phase gate).")]
        [Range(0f, 1f)] public float hpThreshold = 1f;
    }
}
