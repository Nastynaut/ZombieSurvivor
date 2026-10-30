using System.Collections.Generic;
using KnightSurvivors.Core;
using KnightSurvivors.Data;
using KnightSurvivors.Player;
using KnightSurvivors.Pooling;
using UnityEngine;

namespace KnightSurvivors.Pickups
{
    /// <summary>
    /// Spawns, moves and collects XP gems (GDD 4.1).
    ///   * A gem within a living player's pickupRadius is "magnetized" to that player.
    ///   * Magnetized gems fly to the player starting at 12 m/s, speeding up by 20 m/s every second.
    ///   * On touch, XP (gem value x the player's growth) goes to the run's shared XP pool via IExperienceReceiver.
    ///   * At most 400 gems lie on the ground. Over the cap, a new gem's value is added to the oldest
    ///     gem that's more than 15 m from every player, instead of spawning a new object.
    /// Gems are pooled; their prewarm (400 / 64 / 32) is in the PoolPrewarmConfig asset.
    /// </summary>
    public class PickupManager : MonoBehaviour
    {
        public RunConfig runConfig;

        [Tooltip("PF_PH_XPGem_Small / Medium / Large, in tier order.")]
        public XpGem[] gemPrefabs = new XpGem[3];

        [Tooltip("Gems closer than this to the player are collected (m).")]
        public float collectDistance = 0.5f;

        [Tooltip("Over the cap, merge into the oldest gem at least this far from every player (m).")]
        public float mergeMinDistance = 15f;

        private readonly List<XpGem> gems = new List<XpGem>(512);
        private PoolService pool;
        private IExperienceReceiver xpReceiver;

        public int GemCount => gems.Count;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        private void Start()
        {
            pool = ServiceLocator.Get<PoolService>();
        }

        /// <summary>Drop gems worth <paramref name="xp"/> at a position (one gem of the best-fitting tier).</summary>
        public void SpawnGem(Vector3 position, int xp)
        {
            if (xp <= 0 || pool == null) return;

            int[] values = runConfig != null && runConfig.xpCurve != null ? runConfig.xpCurve.gemValues : new[] { 1, 5, 25 };
            int maxGems = runConfig != null && runConfig.xpCurve != null ? runConfig.xpCurve.maxGemsOnGround : 400;

            if (gems.Count >= maxGems)
            {
                var target = FindMergeTarget();
                if (target != null)
                {
                    target.value += xp;
                    return;
                }
            }

            int tier = 0;
            for (int i = values.Length - 1; i >= 0; i--)
            {
                if (xp >= values[i])
                {
                    tier = i;
                    break;
                }
            }

            tier = Mathf.Clamp(tier, 0, gemPrefabs.Length - 1);
            var prefab = gemPrefabs[tier];
            if (prefab == null) return;

            position.y = 0f;
            var gem = pool.Spawn(prefab, position, Quaternion.identity);
            if (gem == null) return;
            gem.value = xp;
            gems.Add(gem);
        }

        private XpGem FindMergeTarget()
        {
            // gems[] is in spawn order, so the first match is the oldest.
            for (int i = 0; i < gems.Count; i++)
            {
                if (!gems[i].magnetized && PlayerRegistry.DistanceToClosestPlayer(gems[i].transform.position) > mergeMinDistance)
                {
                    return gems[i];
                }
            }

            return gems.Count > 0 ? gems[0] : null;
        }

        private void FixedUpdate()
        {
            if (xpReceiver == null) ServiceLocator.TryGet(out xpReceiver);
            float accel = runConfig != null && runConfig.xpCurve != null ? runConfig.xpCurve.magnetAccel : 20f;
            float startSpeed = runConfig != null && runConfig.xpCurve != null ? runConfig.xpCurve.magnetSpeed : 12f;
            float dt = Time.fixedDeltaTime;
            var players = PlayerRegistry.Players;

            for (int i = gems.Count - 1; i >= 0; i--)
            {
                var gem = gems[i];
                Vector3 gemPos = gem.transform.position;

                if (!gem.magnetized)
                {
                    // Look for a living player whose pickup radius reaches this gem.
                    for (int p = 0; p < players.Count; p++)
                    {
                        var player = players[p];
                        if (player == null || player.IsDead) continue;
                        float r = player.Stats.pickupRadius;
                        Vector3 d = player.Position - gemPos;
                        if (d.x * d.x + d.z * d.z <= r * r)
                        {
                            gem.magnetized = true;
                            gem.targetPlayer = player;
                            gem.speed = startSpeed;
                            break;
                        }
                    }

                    continue;
                }

                var target = gem.targetPlayer;
                if (target == null || target.IsDead)
                {
                    // Owner died or left the game (a destroyed player compares equal to null in Unity):
                    // drop back to the ground so another player can pick it up.
                    gem.magnetized = false;
                    gem.targetPlayer = null;
                    continue;
                }

                gem.speed += accel * dt;
                Vector3 to = target.Position - gemPos;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist <= collectDistance + gem.speed * dt)
                {
                    xpReceiver?.AddExperience(target.PlayerIndex, gem.value * target.Stats.growth);
                    gems.RemoveAt(i);
                    pool.Despawn(gem.gameObject);
                    continue;
                }

                gem.transform.position = gemPos + to / dist * (gem.speed * dt);
            }
        }

        private void Update()
        {
            float t = Time.time;
            for (int i = 0; i < gems.Count; i++) gems[i].Animate(t);
        }

        /// <summary>Pull every gem to its nearest living player (for a future Holy Reliquary pickup).</summary>
        public void MagnetizeAll()
        {
            for (int i = 0; i < gems.Count; i++)
            {
                var nearest = PlayerRegistry.GetNearestAlivePlayer(gems[i].transform.position);
                if (nearest == null) return;
                gems[i].magnetized = true;
                gems[i].targetPlayer = nearest;
                gems[i].speed = runConfig != null && runConfig.xpCurve != null ? runConfig.xpCurve.magnetSpeed : 12f;
            }
        }
    }
}
