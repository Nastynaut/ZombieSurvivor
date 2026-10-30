using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.10: one permanent Armory upgrade bought with crowns between runs.
    /// DEFINED NOW, NOT USED YET: the Armory comes in a later PR. The save file already has room for ranks.
    /// cost(rank r) = baseCost x r x (1 + costGrowth x (r - 1)), rounded to 5.
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Meta Upgrade", fileName = "META_NewUpgrade")]
    public class MetaUpgrade : ScriptableObject
    {
        public string id = "meta_might";
        public string displayName = "Might";
        public StatType stat = StatType.Might;
        public ModifierOp op = ModifierOp.PercentAdd;
        public float valuePerRank = 0.05f;
        public int maxRank = 5;
        public int baseCost = 100;
        public float costGrowth = 0.25f;

        public int GetCost(int rank)
        {
            float raw = baseCost * rank * (1f + costGrowth * (rank - 1));
            return Mathf.RoundToInt(raw / 5f) * 5;
        }
    }
}
