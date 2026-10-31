using System.Collections.Generic;
using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Data
{
    /// <summary>
    /// GDD 9.9: rules for the level-up cards. The graybox uses cardsShown, ownedWeight, newWeight,
    /// skipHealPct and fallbackCards; the rest is stored for later PRs (4th card, evolutions, chests, crowns).
    /// </summary>
    [CreateAssetMenu(menuName = "Knight Survivors/Level Up Rules", fileName = "CFG_LevelUpRules")]
    public class LevelUpRules : ScriptableObject
    {
        [Tooltip("Cards shown per level-up.")]
        public int cardsShown = 3;
        [Tooltip("4th card chance = fourthCardLuckCoeff x (luck - 1) / 0.15. Not used yet.")]
        public float fourthCardLuckCoeff = 0.10f;
        public float fourthCardMax = 0.5f;
        [Tooltip("Weight of a card that upgrades something you already own.")]
        public float ownedWeight = 1.5f;
        [Tooltip("Weight of a card for a new weapon or passive.")]
        public float newWeight = 1.0f;
        [Tooltip("Weight multiplier when the card completes an evolution. Not used yet.")]
        public float evoBaseWeightMult = 1.5f;
        [Tooltip("Crowns given by Skip. Crowns are not implemented yet.")]
        public int skipCrowns = 3;
        [Tooltip("Skip heals this fraction of max HP (0.05 = 5%).")]
        public float skipHealPct = 0.05f;
        [Tooltip("Cards offered when nothing else can be upgraded. Order matches GDD: Gold Pouch then Roast Chicken.")]
        public List<PickupType> fallbackCards = new List<PickupType> { PickupType.CoinPouch, PickupType.RoastChicken };
        [Tooltip("Chest rolls 1 / 3 / 5 upgrades with these chances. Not used yet.")]
        public float[] chestRollChances = { 0.75f, 0.20f, 0.05f };
        [Tooltip("How long the chest auto-spin animation lasts, in seconds (GDD 4.3 / DataTables §0.1).")]
        public float chestSpinSeconds = 1.5f;
        [Tooltip("Rerolls granted when a chest is opened (+1). Does NOT grant skips (DataTables §0.1).")]
        public int rerollPerChest = 1;
        // NOTE (DataTables §0.1 / GDD 4.3): dismissing a chest's auto-spin costs NO skip.
        // When chest UI is built, its dismiss path must NOT call LevelUpService.Skip().
    }
}
