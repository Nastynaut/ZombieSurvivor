namespace KnightSurvivors.Core
{
    /// <summary>
    /// Which side something is on. Weapons use this so they never hurt their own side
    /// (the knight's sword skips the knight, and later co-op players skip each other).
    /// </summary>
    public enum Team
    {
        Player,
        Enemy
    }
}
