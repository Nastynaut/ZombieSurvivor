using System;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// GDD 9.11: one "change this stat" entry, e.g. { Might, PercentAdd, 0.10 } = +10% damage.
    /// Used by CharacterDefinition.bonusModifiers (and later meta upgrades).
    /// [Serializable] makes it editable in the Inspector.
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModifierOp op;
        public float value;

        public StatModifier(StatType stat, ModifierOp op, float value)
        {
            this.stat = stat;
            this.op = op;
            this.value = value;
        }
    }
}
