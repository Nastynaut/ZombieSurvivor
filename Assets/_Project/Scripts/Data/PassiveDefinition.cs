using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.4: a passive item that boosts one stat per level (e.g. Whetstone: Might +10% per level).
    /// Create with: Create > Knight Survivors > Passive Definition.
    /// Numbers come from GDD v0.1 (docs/GDD.md 3.3).
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Passive Definition", fileName = "PSV_NewPassive")]
    public class PassiveDefinition : ScriptableObject
    {
        [Tooltip("Unique id, e.g. \"whetstone\".")]
        public string id = "whetstone";

        public string displayName = "Whetstone";

        public Sprite icon;

        [Tooltip("Which stat this passive changes.")]
        public StatType stat = StatType.Might;

        [Tooltip("Add = flat amount, PercentAdd = +x% of base (0.10 = +10%), Multiply = multiply.")]
        public ModifierOp op = ModifierOp.PercentAdd;

        [Tooltip("Applied once per level: level 3 Whetstone = 3 x 0.10 = +30% Might.")]
        public float valuePerLevel = 0.10f;

        public int maxLevel = 5;

        /// <summary>Text for the level-up card, e.g. "Might +10%".</summary>
        public string DescribeLevel()
        {
            switch (op)
            {
                case ModifierOp.PercentAdd:
                    return $"{stat} {(valuePerLevel >= 0 ? "+" : "")}{valuePerLevel * 100f:0}%";
                case ModifierOp.Multiply:
                    return $"{stat} x{valuePerLevel:0.##}";
                default:
                    return $"{stat} {(valuePerLevel >= 0 ? "+" : "")}{valuePerLevel:0.##}";
            }
        }
    }
}
