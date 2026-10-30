namespace KnightSurvivors.Core
{
    /// <summary>
    /// A tiny read-only view of "what kind of enemy is this?".
    /// It lives in Core so systems like Gore can ask about an enemy without referencing the Enemies
    /// assembly (that keeps assemblies independent: Gore only knows Core, Data, Pooling and Combat).
    /// </summary>
    public interface IEnemyInfo
    {
        EnemyTier Tier { get; }

        /// <summary>True for the Bloater (bursts on death, bigger bile splat).</summary>
        bool ExplodesOnDeath { get; }
    }
}
