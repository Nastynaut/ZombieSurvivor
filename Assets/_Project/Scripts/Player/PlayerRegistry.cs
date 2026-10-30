using System;
using System.Collections.Generic;
using UnityEngine;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// The list of all players in the game (GDD 9.14). There is NO "Player.Instance" singleton.
    ///
    /// WHY?
    /// Code like "Player.Instance.transform.position" works for one player and breaks the day a second
    /// player joins. Asking the registry ("nearest living player to this zombie", "are ALL players
    /// dead?") works the same for 1 or 4 players, so co-op later doesn't need a rewrite.
    ///
    /// Players add themselves when they spawn and remove themselves when destroyed.
    /// </summary>
    public static class PlayerRegistry
    {
        private static readonly List<PlayerController> players = new List<PlayerController>(4);
        private static int spawnAnchorCursor;

        /// <summary>Every player (alive or dead), in join order.</summary>
        public static IReadOnlyList<PlayerController> Players => players;

        /// <summary>How many players are in the game (used for co-op scaling).</summary>
        public static int PlayerCount => players.Count;

        public static event Action<PlayerController> PlayerAdded;
        public static event Action<PlayerController> PlayerRemoved;

        /// <summary>Fired when any player dies.</summary>
        public static event Action<PlayerController> PlayerDied;

        public static void Register(PlayerController player)
        {
            if (player == null || players.Contains(player)) return;
            players.Add(player);
            PlayerAdded?.Invoke(player);
        }

        public static void Unregister(PlayerController player)
        {
            if (players.Remove(player)) PlayerRemoved?.Invoke(player);
        }

        internal static void NotifyDied(PlayerController player)
        {
            PlayerDied?.Invoke(player);
        }

        /// <summary>True if at least one player has joined and every one of them is dead.</summary>
        public static bool AllDead
        {
            get
            {
                if (players.Count == 0) return false;
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i] != null && !players[i].IsDead) return false;
                }

                return true;
            }
        }

        /// <summary>The closest living player to a point (flat XZ distance), or null if none.</summary>
        public static PlayerController GetNearestAlivePlayer(Vector3 from)
        {
            PlayerController best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p.IsDead) continue;
                Vector3 d = p.Position - from;
                float sqr = d.x * d.x + d.z * d.z;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = p;
                }
            }

            return best;
        }

        /// <summary>Flat distance from a point to the closest player (alive or dead), or +infinity.</summary>
        public static float DistanceToClosestPlayer(Vector3 from)
        {
            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null) continue;
                Vector3 d = p.Position - from;
                float sqr = d.x * d.x + d.z * d.z;
                if (sqr < bestSqr) bestSqr = sqr;
            }

            return Mathf.Sqrt(bestSqr);
        }

        /// <summary>
        /// GDD 9.14: "each spawn picks a living player (round-robin) for ring centering".
        /// Returns the next living player in turn, or null if nobody is alive.
        /// </summary>
        public static PlayerController NextSpawnAnchor()
        {
            for (int tries = 0; tries < players.Count; tries++)
            {
                spawnAnchorCursor = (spawnAnchorCursor + 1) % players.Count;
                var p = players[spawnAnchorCursor];
                if (p != null && !p.IsDead) return p;
            }

            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            players.Clear();
            spawnAnchorCursor = 0;
            PlayerAdded = null;
            PlayerRemoved = null;
            PlayerDied = null;
        }
    }
}
