using System.Collections.Generic;
using KnightSurvivors.Core;
using KnightSurvivors.Data;
using KnightSurvivors.Player;
using UnityEngine;

namespace KnightSurvivors.Enemies
{
    /// <summary>
    /// Time-based spawner that follows the SpawnSchedule asset (GDD 5), one WaveEntry per minute:
    ///   * If alive &lt; targetAlive, spawn at spawnRatePerSec x 2 (catch up quickly).
    ///   * Otherwise spawn at spawnRatePerSec, unless the hard cap (350 for 1 player) is reached.
    ///     After bossMinute the trickle stops at targetAlive (cap 40), not hardCap.
    ///   * Each zombie appears 20-24 m from a player (round-robin over living players), just off
    ///     screen. 60% of spawns are pushed into a +-60 degree cone in front of where that player is walking,
    ///     so you run INTO the horde instead of leaving it behind.
    ///   * Which zombie type is picked by the minute's mix percentages (weightPct).
    /// All random rolls use the seeded RunRandom, so a seed replays the same run.
    ///
    /// Not in the graybox yet (deferred): WaveEvents (Stampede, Elite, Boss). The data is already in the asset.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public RunConfig runConfig;

        [Tooltip("Turn off to stop spawning (handy while testing movement).")]
        public bool spawningEnabled = true;

        private EnemyManager enemies;
        private RunState run;
        private float spawnBudget;

        private SpawnSchedule Schedule => runConfig != null ? runConfig.spawnSchedule : null;

        private void Start()
        {
            enemies = ServiceLocator.Get<EnemyManager>();
            ServiceLocator.TryGet(out run);
            PrewarmAll();
        }

        /// <summary>Create pooled zombies up front (EnemyDefinition.prewarmCount) so the first minute doesn't hitch.</summary>
        private void PrewarmAll()
        {
            if (Schedule == null || enemies == null) return;
            var done = new HashSet<EnemyDefinition>();
            foreach (var entry in Schedule.entries)
            {
                if (entry == null || entry.mix == null) continue;
                foreach (var w in entry.mix)
                {
                    if (w != null && w.enemy != null && done.Add(w.enemy)) enemies.Prewarm(w.enemy);
                }
            }

            if (Schedule.boss != null && done.Add(Schedule.boss)) enemies.Prewarm(Schedule.boss);
        }

        private void FixedUpdate()
        {
            if (!spawningEnabled || Schedule == null || enemies == null) return;
            if (run == null && !ServiceLocator.TryGet(out run)) return;
            if (run.isRunOver || PlayerRegistry.AllDead) return;

            int index = Schedule.GetEntryIndexForTime(run.elapsedTime);
            if (index < 0) return;
            run.currentWaveIndex = index;
            var entry = Schedule.entries[index];

            int players = Mathf.Max(1, PlayerRegistry.PlayerCount);
            float countMult = Schedule.coopScaling != null ? CoopScaling.Pick(Schedule.coopScaling.enemyCountMult, players) : 1f;
            int hardCap = Schedule.coopScaling != null ? CoopScaling.Pick(Schedule.coopScaling.hardCap, players, Schedule.hardCap) : Schedule.hardCap;
            int targetAlive = Mathf.RoundToInt(entry.targetAlive * countMult);
            int alive = enemies.AliveCount;

            // Boss trickle (minute >= bossMinute): GDD "cap 40 live" — stop at targetAlive, do not
            // keep filling toward hardCap 350. Earlier minutes still use hardCap as the upper bound.
            bool bossTrickle = Schedule.bossMinute > 0f && run.ElapsedMinutes >= Schedule.bossMinute;
            int spawnCap = bossTrickle ? targetAlive : hardCap;

            // Enrage (20:00): replace the minute-15 rate with enrageSpawnRatePerSec when the boss is set.
            // The minute-15 row's targetAlive (40) and Shambler mix stay as written in the schedule.
            float baseRate = entry.spawnRatePerSec;
            if (Schedule.enrageTimeSeconds > 0f
                && run.elapsedTime >= Schedule.enrageTimeSeconds
                && Schedule.enrageSpawnRatePerSec > 0f)
            {
                baseRate = Schedule.enrageSpawnRatePerSec;
            }

            float rate;
            if (alive < targetAlive) rate = baseRate * 2f;
            else if (alive < spawnCap) rate = baseRate;
            else rate = 0f;

            rate *= countMult;

            // "Budget" adds up fractions: at 1.5 per second we spawn one zombie on some ticks and none on others.
            spawnBudget += rate * Time.fixedDeltaTime;
            if (rate <= 0f) spawnBudget = 0f;

            float minute = run.ElapsedMinutes;
            while (spawnBudget >= 1f && enemies.AliveCount < spawnCap)
            {
                spawnBudget -= 1f;
                var def = PickEnemy(entry, minute);
                if (def == null) break;
                if (!TryGetSpawnPoint(out Vector3 point)) break;
                enemies.Spawn(def, point, minute);
            }
        }

        /// <summary>Weighted pick from the minute's mix, skipping types that aren't allowed yet (firstMinute).</summary>
        private EnemyDefinition PickEnemy(WaveEntry entry, float minute)
        {
            if (entry.mix == null || entry.mix.Count == 0) return null;

            float total = 0f;
            foreach (var w in entry.mix)
            {
                if (w != null && w.enemy != null && w.enemy.firstMinute <= minute) total += Mathf.Max(0f, w.weightPct);
            }

            if (total <= 0f) return null;

            float roll = run.Random.Value() * total;
            foreach (var w in entry.mix)
            {
                if (w == null || w.enemy == null || w.enemy.firstMinute > minute) continue;
                roll -= Mathf.Max(0f, w.weightPct);
                if (roll <= 0f) return w.enemy;
            }

            return entry.mix[entry.mix.Count - 1].enemy;
        }

        /// <summary>A point on spawn ring A around the next living player (round-robin), biased toward their move direction.</summary>
        private bool TryGetSpawnPoint(out Vector3 point)
        {
            point = Vector3.zero;
            var anchor = PlayerRegistry.NextSpawnAnchor();
            if (anchor == null) return false;

            var rng = run.Random;
            float angle;
            Vector3 moveDir = anchor.Motor.LastMoveDirection;
            if (moveDir.sqrMagnitude > 0.01f && rng.Chance(Schedule.moveDirBias))
            {
                float cone = Schedule.moveDirConeHalfAngle > 0f ? Schedule.moveDirConeHalfAngle : 60f;
                angle = Mathf.Atan2(moveDir.z, moveDir.x) + rng.Range(-cone, cone) * Mathf.Deg2Rad;
            }
            else
            {
                angle = rng.Range(0f, Mathf.PI * 2f);
            }

            float dist = rng.Range(Schedule.ringMinDistance, Schedule.ringMaxDistance);
            point = anchor.Position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
            point.y = 0f;
            return true;
        }
    }
}
