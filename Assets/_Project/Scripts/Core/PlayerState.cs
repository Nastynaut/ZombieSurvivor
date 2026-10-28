using System;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// Everything that describes ONE player's progress in the current run, as plain data.
    ///
    /// WHY KEEP THIS SEPARATE FROM THE PlayerController MonoBehaviour?
    /// Co-op later. Networking libraries sync plain data much more easily than whole GameObjects.
    /// If the "truth" (health, kills, stats) lives in a simple [Serializable] class, a future network
    /// layer can copy it between machines, and the MonoBehaviours just display / act on it.
    /// It also makes debugging easy: the Inspector shows the whole state in one foldout.
    ///
    /// XP and level are NOT here: GDD 11.1 uses one shared XP pool, stored in RunState.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        /// <summary>0 for player one, 1 for player two, ...</summary>
        public int playerIndex;

        /// <summary>Id of the CharacterDefinition this player spawned as.</summary>
        public string characterId;

        public float currentHealth;
        public int kills;
        public bool isDead;

        /// <summary>Level-up options left this run (GDD 3.1). Reroll/banish UI comes later.</summary>
        public int rerollsLeft;
        public int banishesLeft;

        /// <summary>The current, upgraded stats (base + character bonus + passives).</summary>
        public RuntimeStats stats = new RuntimeStats();
    }
}
