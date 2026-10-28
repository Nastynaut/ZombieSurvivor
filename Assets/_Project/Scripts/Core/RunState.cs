using System;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// Shared state for the whole run (not per player), as plain data.
    /// Like PlayerState, it is a plain [Serializable] class so it could be synced over the network later.
    ///
    /// The RunManager owns it and registers it with the ServiceLocator, so for example the
    /// EnemySpawner can read <c>elapsedTime</c> without knowing about the RunManager.
    /// </summary>
    [Serializable]
    public class RunState
    {
        /// <summary>Seed for this run's random numbers. Same seed = same spawns and cards.</summary>
        public int seed;

        /// <summary>Seconds survived so far (stops counting when the run is over).</summary>
        public float elapsedTime;

        /// <summary>Total zombies killed by all players.</summary>
        public int totalKills;

        /// <summary>Index of the SpawnSchedule entry in use (for UI / debugging).</summary>
        public int currentWaveIndex = -1;

        /// <summary>Shared XP pool (GDD 11.1): XP collected towards the next level.</summary>
        public float runXp;

        /// <summary>Shared level of the whole team.</summary>
        public int runLevel = 1;

        /// <summary>XP needed to reach the next level.</summary>
        public int xpToNextLevel = 5;

        /// <summary>True once every player is dead.</summary>
        public bool isRunOver;

        /// <summary>
        /// The gameplay random number generator. [NonSerialized] because System.Random can't be saved;
        /// it is rebuilt from <see cref="seed"/>.
        /// </summary>
        [NonSerialized] public RunRandom Random;

        /// <summary>Reset everything for a new run with the given seed.</summary>
        public void Begin(int newSeed)
        {
            seed = newSeed;
            elapsedTime = 0f;
            totalKills = 0;
            currentWaveIndex = -1;
            runXp = 0f;
            runLevel = 1;
            isRunOver = false;
            Random = new RunRandom(newSeed);
        }

        /// <summary>Minutes survived (used by HP/damage scaling formulas).</summary>
        public float ElapsedMinutes => elapsedTime / 60f;
    }
}
