using System.Collections.Generic;
using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.11: one playable character (launch has one: Sir Aldric, id "sir_aldric").
    /// Create with: Create > Knight Survivors > Character Definition. Field names match the GDD exactly.
    ///
    /// WHY A SEPARATE ASSET FROM PlayerStats?
    /// PlayerStats is just numbers. A character is numbers + a prefab (logic + look) + a starting
    /// weapon + a name for the menu + how it is unlocked. Adding Brother Cuthbert later (GDD 12) is
    /// "duplicate this asset, point it at other stats / weapon / prefab" with zero code changes.
    ///
    /// SWAPPING ART: <see cref="prefab"/> points at PF_PH_Knight (logic root + placeholder 'Visual'
    /// child). When real art arrives, make a Prefab Variant with the new Visual and point this field
    /// at it. That's a data change, not a code change (PlaceholderSpec 7.1).
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Character Definition", fileName = "CHR_NewCharacter")]
    public class CharacterDefinition : ScriptableObject
    {
        [Tooltip("Unique id, e.g. \"sir_aldric\". Used by save files and unlocks.")]
        public string id = "sir_aldric";

        [Tooltip("Name shown in menus.")]
        public string displayName = "Sir Aldric";

        [TextArea] public string description = "A knight of the fallen order.";

        [Tooltip("Menu portrait (optional).")]
        public Sprite portrait;

        [Tooltip("The player prefab that spawns: logic root (PlayerController etc.) + swappable 'Visual' child.")]
        public GameObject prefab;

        [Tooltip("Base stats (GDD 9.11 PlayerStats).")]
        public PlayerStats baseStats;

        [Tooltip("Weapon given at the start of a run.")]
        public WeaponDefinition startingWeapon;

        [Tooltip("Extra stat changes on top of baseStats (Sir Aldric has none).")]
        public List<StatModifier> bonusModifiers = new List<StatModifier>();

        [Tooltip("Available from the first launch? (Launch character: true.)")]
        public bool unlockedByDefault = true;

        [Tooltip("Optional: how this character gets unlocked if not unlocked by default.")]
        public UnlockDefinition unlock;
    }
}
