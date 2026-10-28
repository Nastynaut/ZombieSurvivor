using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.5: "weapon at max level + this passive + a chest after minute X = evolved weapon".
    /// DEFINED NOW, NOT USED YET: chests and evolutions come in a later PR.
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Evolution Recipe", fileName = "EVO_NewEvolution")]
    public class EvolutionRecipe : ScriptableObject
    {
        public string id = "evo_greatsword";
        public WeaponDefinition baseWeapon;
        public int requiredWeaponLevel = 8;
        public PassiveDefinition requiredPassive;
        public EvolutionConditionType conditionType = EvolutionConditionType.ChestAfterMinute;
        [Tooltip("Minutes into the run before a chest can evolve this weapon.")]
        public float minMinute = 7f;
        public WeaponDefinition resultWeapon;
    }
}
