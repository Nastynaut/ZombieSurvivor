using System.Collections.Generic;
using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// The "settings for one run" asset that ties the others together: which character, which spawn
    /// schedule, which weapons and passives can show up on level-up cards, and the default aim mode.
    /// Create with: Create > Knight Survivors > Run Config.
    ///
    /// Everything that a designer might want to change for a run lives in data like this, never
    /// hard-coded. Weapon and passive slot limits live in PlayerStats (weaponSlots / passiveSlots, GDD 9.11).
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Run Config", fileName = "CFG_RunConfig")]
    public class RunConfig : ScriptableObject
    {
        [Header("Who plays")]
        [Tooltip("Character every player spawns as (character select comes later).")]
        public CharacterDefinition defaultCharacter;

        [Tooltip("Default aim mode for directional weapons. GDD 2.2: Assisted.")]
        public AimMode defaultAimMode = AimMode.Assisted;

        [Header("Pacing and progression")]
        public SpawnSchedule spawnSchedule;
        public XpCurve xpCurve;
        public LevelUpRules levelUpRules;

        [Header("Level-up card pool")]
        [Tooltip("Weapons that can appear as NEW weapon cards (slice: Longsword, Crossbow, Holy Flail).")]
        public List<WeaponDefinition> weaponPool = new List<WeaponDefinition>();
        [Tooltip("Passives that can appear on cards (slice: Whetstone, Prayer Beads, Plate Armor).")]
        public List<PassiveDefinition> passivePool = new List<PassiveDefinition>();

        [Header("Feel")]
        public FeedbackSettings feedbackSettings;
        public HitFeedbackProfile hitFeedbackProfile;
    }
}
