using System;
using UnityEngine;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// The live stats of ONE player during a run (GDD 9.14 calls this "RuntimeStats").
    ///
    /// HOW STATS ARE CALCULATED
    ///   1. Start from the character's base values (the PlayerStats ScriptableObject).
    ///   2. Collect every modifier from passives (and later meta upgrades).
    ///   3. For each stat: final = (base + sum of Add) x (1 + sum of PercentAdd) x (product of Multiply).
    /// We recalculate from scratch whenever a passive changes, instead of adding on top of the
    /// old values. That way the numbers can never "drift" from rounding or from applying something twice.
    ///
    /// WHY A PLAIN [Serializable] CLASS?
    /// It can be shown in the Inspector, saved, or (later, for co-op) sent over the network.
    /// It never modifies the ScriptableObject asset on disk.
    /// </summary>
    [Serializable]
    public class RuntimeStats
    {
        // Field names match the GDD PlayerStats fields (section 9.11), so they are easy to look up.
        public float maxHealth = 120f;
        public float recovery;
        public float armor;
        public float moveSpeed = 5f;
        public float might = 1f;
        public float area = 1f;
        public float cooldown = 1f;
        public int amount;
        public float duration = 1f;
        public float projectileSpeed = 1f;
        public float critChance = 0.05f;
        public float critMultiplier = 2f;
        public float pickupRadius = 2.5f;
        public float luck = 1f;
        public float growth = 1f;
        public float greed = 1f;
        public int revivals;
        public int rerolls = 2;
        public int banishes = 1;

        /// <summary>GDD 3.1: the cooldown multiplier can never go below 0.40 (attacks at most 2.5x faster).</summary>
        public const float CooldownFloor = 0.40f;

        // Number of values in StatType, used to size the temporary arrays below.
        private static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;

        // Reused scratch arrays so recalculating does not create garbage.
        [NonSerialized] private float[] addSums;
        [NonSerialized] private float[] percentSums;
        [NonSerialized] private float[] multiplyProducts;

        /// <summary>Read a stat by its enum value.</summary>
        public float Get(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxHealth: return maxHealth;
                case StatType.Recovery: return recovery;
                case StatType.Armor: return armor;
                case StatType.MoveSpeed: return moveSpeed;
                case StatType.Might: return might;
                case StatType.Area: return area;
                case StatType.Cooldown: return cooldown;
                case StatType.Amount: return amount;
                case StatType.Duration: return duration;
                case StatType.ProjectileSpeed: return projectileSpeed;
                case StatType.CritChance: return critChance;
                case StatType.CritMultiplier: return critMultiplier;
                case StatType.PickupRadius: return pickupRadius;
                case StatType.Luck: return luck;
                case StatType.Growth: return growth;
                case StatType.Greed: return greed;
                case StatType.Revivals: return revivals;
                case StatType.Rerolls: return rerolls;
                case StatType.Banishes: return banishes;
                default: return 0f;
            }
        }

        /// <summary>Write a stat by its enum value (ints are rounded).</summary>
        public void Set(StatType stat, float value)
        {
            switch (stat)
            {
                case StatType.MaxHealth: maxHealth = Mathf.Max(1f, value); break;
                case StatType.Recovery: recovery = value; break;
                case StatType.Armor: armor = value; break;
                case StatType.MoveSpeed: moveSpeed = Mathf.Max(0.5f, value); break;
                case StatType.Might: might = Mathf.Max(0f, value); break;
                case StatType.Area: area = Mathf.Max(0.1f, value); break;
                case StatType.Cooldown: cooldown = Mathf.Max(CooldownFloor, value); break;
                case StatType.Amount: amount = Mathf.RoundToInt(value); break;
                case StatType.Duration: duration = Mathf.Max(0f, value); break;
                case StatType.ProjectileSpeed: projectileSpeed = Mathf.Max(0f, value); break;
                case StatType.CritChance: critChance = Mathf.Clamp01(value); break;
                case StatType.CritMultiplier: critMultiplier = Mathf.Max(1f, value); break;
                case StatType.PickupRadius: pickupRadius = Mathf.Max(0f, value); break;
                case StatType.Luck: luck = value; break;
                case StatType.Growth: growth = value; break;
                case StatType.Greed: greed = value; break;
                case StatType.Revivals: revivals = Mathf.RoundToInt(value); break;
                case StatType.Rerolls: rerolls = Mathf.RoundToInt(value); break;
                case StatType.Banishes: banishes = Mathf.RoundToInt(value); break;
            }
        }

        /// <summary>Copy every value from another RuntimeStats (used to reset to base values).</summary>
        public void CopyFrom(RuntimeStats other)
        {
            for (int i = 0; i < StatCount; i++)
            {
                var stat = (StatType)i;
                Set(stat, other.Get(stat));
            }
        }

        /// <summary>Step 1 of a recalculation: forget all modifiers collected so far.</summary>
        public void BeginModifiers()
        {
            if (addSums == null)
            {
                addSums = new float[StatCount];
                percentSums = new float[StatCount];
                multiplyProducts = new float[StatCount];
            }

            for (int i = 0; i < StatCount; i++)
            {
                addSums[i] = 0f;
                percentSums[i] = 0f;
                multiplyProducts[i] = 1f;
            }
        }

        /// <summary>Step 2: collect one modifier (call once per passive, per meta upgrade, ...).</summary>
        public void AddModifier(StatType stat, ModifierOp op, float value)
        {
            if (addSums == null) BeginModifiers();
            int i = (int)stat;
            switch (op)
            {
                case ModifierOp.Add: addSums[i] += value; break;
                case ModifierOp.PercentAdd: percentSums[i] += value; break;
                case ModifierOp.Multiply: multiplyProducts[i] *= value; break;
            }
        }

        /// <summary>Step 3: final = (base + adds) x (1 + percents) x multiplies, for every stat.</summary>
        public void ApplyModifiers(RuntimeStats baseStats)
        {
            if (addSums == null) BeginModifiers();
            for (int i = 0; i < StatCount; i++)
            {
                var stat = (StatType)i;
                float value = (baseStats.Get(stat) + addSums[i]) * (1f + percentSums[i]) * multiplyProducts[i];
                Set(stat, value);
            }
        }
    }
}
