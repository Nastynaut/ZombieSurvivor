using UnityEngine;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// The ONE random number generator for gameplay rolls (GDD 9.14).
    ///
    /// WHY NOT UnityEngine.Random?
    /// UnityEngine.Random is global: anything (a particle effect, a plugin) can pull numbers from it,
    /// so two runs with the same start never play out the same. A seeded System.Random that only
    /// gameplay uses means:
    ///   * The same seed gives the same spawns, crits and level-up cards: great for reproducing bugs
    ///     ("it happened on seed 12345").
    ///   * Later, online co-op can send just the seed instead of every random result.
    ///
    /// RULE: gameplay (spawns, cards, crits, chests) uses RunState.Random. Purely cosmetic things
    /// (blood spray angles, sound pitch) may use UnityEngine.Random.
    /// </summary>
    public class RunRandom
    {
        private readonly System.Random random;

        public int Seed { get; }

        public RunRandom(int seed)
        {
            Seed = seed;
            random = new System.Random(seed);
        }

        /// <summary>A float from 0 (inclusive) to 1 (exclusive).</summary>
        public float Value() => (float)random.NextDouble();

        /// <summary>A float between min and max.</summary>
        public float Range(float min, float max) => min + (max - min) * Value();

        /// <summary>An int from minInclusive up to (but NOT including) maxExclusive, like UnityEngine.Random.Range for ints.</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return random.Next(minInclusive, maxExclusive);
        }

        /// <summary>True with the given probability (0.25 = 25% of the time).</summary>
        public bool Chance(float probability) => Value() < probability;

        /// <summary>A flat direction on the ground (y = 0) with length 1.</summary>
        public Vector3 OnUnitCircleXZ()
        {
            float angle = Range(0f, Mathf.PI * 2f);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }
}
