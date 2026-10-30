namespace KnightSurvivors.Core
{
    /// <summary>
    /// "Someone picked up XP." Implemented by the RunManager (Progression), used by Pickups.
    /// The interface lives in Core so Pickups doesn't have to reference Progression.
    ///
    /// GDD 9.14 / 11.1: XP goes into ONE shared pool for all players; when the pool levels up,
    /// every living player picks their own card.
    /// </summary>
    public interface IExperienceReceiver
    {
        /// <param name="playerIndex">Who collected it (for stats).</param>
        /// <param name="amount">XP after the collector's Growth multiplier.</param>
        void AddExperience(int playerIndex, float amount);
    }
}
