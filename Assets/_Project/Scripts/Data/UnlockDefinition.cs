using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.15: "do X to unlock Y", e.g. "reach level 20 to unlock Knight's Spurs".
    /// Create with: Create > Knight Survivors > Unlock Definition. Field names match the GDD exactly.
    ///
    /// Only ONE of the unlocks... fields is normally set. The unlock CHECK (watching kills, minutes,
    /// levels during a run and saving the result) comes in a later PR; this PR only defines the data
    /// so CharacterDefinition.unlock can reference it.
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Unlock Definition", fileName = "UNL_NewUnlock")]
    public class UnlockDefinition : ScriptableObject
    {
        [Tooltip("Unique id, e.g. \"unl_spurs\". Stored in the save file once unlocked.")]
        public string id = "unl_new";

        public string displayName = "New Unlock";

        [Tooltip("What the player has to do.")]
        public UnlockConditionType conditionType = UnlockConditionType.ReachLevel;

        [Tooltip("How much: a count (kills), minutes (survive) or a level, depending on conditionType.")]
        public float threshold = 20f;

        [Tooltip("Optional: weapon added to the level-up pool when unlocked.")]
        public WeaponDefinition unlocksWeapon;

        [Tooltip("Optional: passive added to the level-up pool when unlocked.")]
        public PassiveDefinition unlocksPassive;

        [Tooltip("Optional: meta upgrade made available in the Armory.")]
        public MetaUpgrade unlocksMetaUpgrade;

        [Tooltip("Optional: playable character (unused at launch).")]
        public CharacterDefinition unlocksCharacter;
    }
}
