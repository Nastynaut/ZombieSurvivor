using System.Collections.Generic;
using KnightSurvivors.Combat;
using KnightSurvivors.Core;
using KnightSurvivors.Data;
using UnityEngine;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// What a player is carrying: up to <c>weaponSlots</c> weapons and <c>passiveSlots</c> passives
    /// (4 / 4 by default, from the PlayerStats asset). Also owns the stat recalculation:
    ///   final = (base + all Add) x (1 + all PercentAdd) x (all Multiply)      (GDD 3.1)
    /// where "all" = the character's bonusModifiers + every passive x its level.
    /// </summary>
    public class PlayerLoadout : MonoBehaviour
    {
        /// <summary>One owned passive and its level (1..maxLevel).</summary>
        public class OwnedPassive
        {
            public PassiveDefinition definition;
            public int level;
        }

        private readonly List<WeaponBase> weapons = new List<WeaponBase>(4);
        private readonly List<OwnedPassive> passives = new List<OwnedPassive>(4);

        private PlayerController player;
        private CharacterDefinition character;
        private RuntimeStats baseStats = new RuntimeStats();

        public IReadOnlyList<WeaponBase> Weapons => weapons;
        public IReadOnlyList<OwnedPassive> Passives => passives;
        public int WeaponSlots => character != null && character.baseStats != null ? character.baseStats.weaponSlots : 4;
        public int PassiveSlots => character != null && character.baseStats != null ? character.baseStats.passiveSlots : 4;
        public bool HasFreeWeaponSlot => weapons.Count < WeaponSlots;
        public bool HasFreePassiveSlot => passives.Count < PassiveSlots;

        public void Initialize(PlayerController owner, CharacterDefinition characterDefinition)
        {
            player = owner;
            character = characterDefinition;

            if (character != null && character.baseStats != null) baseStats = character.baseStats.ToRuntimeStats();
            else Debug.LogWarning("[PlayerLoadout] CharacterDefinition or its baseStats is missing; using code defaults.");

            RecalculateStats();

            if (character != null && character.startingWeapon != null) AddWeapon(character.startingWeapon);
        }

        public WeaponBase GetWeapon(WeaponDefinition definition)
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i].Definition == definition) return weapons[i];
            }

            return null;
        }

        public OwnedPassive GetPassive(PassiveDefinition definition)
        {
            for (int i = 0; i < passives.Count; i++)
            {
                if (passives[i].definition == definition) return passives[i];
            }

            return null;
        }

        /// <summary>Add a new weapon (or level it up if already owned). Returns false if no slot is free.</summary>
        public bool AddWeapon(WeaponDefinition definition)
        {
            if (definition == null) return false;
            var owned = GetWeapon(definition);
            if (owned != null)
            {
                owned.LevelUp();
                return true;
            }

            if (!HasFreeWeaponSlot) return false;

            WeaponBase weapon = null;
            if (definition.prefab != null)
            {
                var go = Instantiate(definition.prefab, transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                weapon = go.GetComponent<WeaponBase>();
                if (weapon == null) Destroy(go);
            }

            if (weapon == null)
            {
                // No prefab yet: the Longsword works without one (it just won't show a slash).
                if (definition.archetype != WeaponArchetype.MeleeArc)
                {
                    Debug.LogWarning($"[PlayerLoadout] Weapon '{definition.id}' has no prefab with a weapon script. Skipped.");
                    return false;
                }

                var go = new GameObject($"WPN_{definition.id}");
                go.transform.SetParent(transform, false);
                weapon = go.AddComponent<LongswordWeapon>();
            }

            weapon.Initialize(player, definition);
            weapons.Add(weapon);
            return true;
        }

        /// <summary>Add a passive or raise its level. Returns false if no slot is free or it's maxed.</summary>
        public bool AddPassive(PassiveDefinition definition)
        {
            if (definition == null) return false;
            var owned = GetPassive(definition);
            if (owned != null)
            {
                if (owned.level >= definition.maxLevel) return false;
                owned.level++;
            }
            else
            {
                if (!HasFreePassiveSlot) return false;
                passives.Add(new OwnedPassive { definition = definition, level = 1 });
            }

            RecalculateStats();
            return true;
        }

        /// <summary>Rebuild the final stats from base + modifiers. Cheap, only runs when something changes.</summary>
        public void RecalculateStats()
        {
            var stats = player.State.stats;
            stats.BeginModifiers();

            if (character != null && character.bonusModifiers != null)
            {
                for (int i = 0; i < character.bonusModifiers.Count; i++)
                {
                    var m = character.bonusModifiers[i];
                    stats.AddModifier(m.stat, m.op, m.value);
                }
            }

            for (int i = 0; i < passives.Count; i++)
            {
                var p = passives[i];
                stats.AddModifier(p.definition.stat, p.definition.op, p.definition.valuePerLevel * p.level);
            }

            stats.ApplyModifiers(baseStats);
            player.OnStatsRecalculated();
        }
    }
}
