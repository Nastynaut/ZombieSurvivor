using System.Collections.Generic;
using KnightSurvivors.Combat;
using KnightSurvivors.Core;
using KnightSurvivors.Data;
using KnightSurvivors.Pickups;
using KnightSurvivors.Player;
using KnightSurvivors.Pooling;
using UnityEngine;

namespace KnightSurvivors.Enemies
{
    /// <summary>
    /// Runs EVERY zombie, on the fixed 50 Hz gameplay tick:
    ///   1. Rebuild the spatial hash (which zombie is in which grid cell).
    ///   2. Every 0.5 s each zombie re-picks the nearest LIVING player (works for 1-4 players).
    ///   3. Seek: walk straight toward that player at moveSpeed.
    ///   4. Separation: push away from overlapping neighbours so the horde spreads into a crowd.
    ///   5. Knockback: weapon pushes, decaying to zero over 0.15 s.
    ///   6. Contact damage when touching the player, once per contactInterval.
    ///   7. Recycle zombies left more than 35 m behind (teleport ahead of a player, keep HP).
    /// Then every rendered frame (Update) it smooths the zombie transforms between ticks and
    /// handles hit flashes and corpses.
    ///
    /// SCALING NOTE (see docs/TechPlan.md): this simple C# loop is fine up to the 350 cap.
    /// If profiling ever shows > 6 ms for this, move steps 1-5 to a Burst-compiled Job.
    ///
    /// It also answers "which zombies are near here?" for weapons (IHitQuery) and
    /// "where's the nearest zombie?" for aim assist (ITargetProvider).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class EnemyManager : MonoBehaviour, IHitQuery, ITargetProvider
    {
        public RunConfig runConfig;

        [Tooltip("Shared white flash material (M_PH_HitFlash).")]
        public Material hitFlashMaterial;

        [Tooltip("Grid cell size for the spatial hash (m).")]
        public float cellSize = 2f;

        [Tooltip("How strongly overlapping zombies push apart (1 = fully resolve overlap in about a second).")]
        public float separationStrength = 6f;

        [Tooltip("Player body radius used for contact checks (m). PlaceholderSpec knight capsule: 0.35.")]
        public float playerRadius = 0.35f;

        [Tooltip("Seconds between 'who is the nearest living player?' checks (GDD 9.14: 0.5).")]
        public float retargetInterval = 0.5f;

        [Tooltip("Corpses over the cap sink this fast (s).")]
        public float fastSinkTime = 0.4f;

        private readonly List<Enemy> alive = new List<Enemy>(400);
        private readonly List<Enemy> corpses = new List<Enemy>(80);
        private readonly List<Enemy> scratch = new List<Enemy>(64);
        private SpatialHash hash;
        private PoolService pool;
        private PickupManager pickups;
        private int corpseWeightTotal;

        public int AliveCount => alive.Count;
        public IReadOnlyList<Enemy> Alive => alive;

        private FeedbackSettings Feedback => runConfig != null ? runConfig.feedbackSettings : null;

        private void Awake()
        {
            hash = new SpatialHash(cellSize);
            ServiceLocator.Register(this);
            ServiceLocator.Register<IHitQuery>(this);
            ServiceLocator.Register<ITargetProvider>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
            ServiceLocator.Unregister<IHitQuery>(this);
            ServiceLocator.Unregister<ITargetProvider>(this);
        }

        private void Start()
        {
            pool = ServiceLocator.Get<PoolService>();
            ServiceLocator.TryGet(out pickups);
        }

        // ------------------------------------------------------------------ spawning

        /// <summary>Take a zombie of this type from the pool and place it. Returns null if the definition has no prefab.</summary>
        public Enemy Spawn(EnemyDefinition definition, Vector3 position, float minute)
        {
            if (definition == null || definition.prefab == null || pool == null) return null;
            position.y = 0f;

            var go = pool.Spawn(definition.prefab, position, Quaternion.identity);
            var enemy = go != null ? go.GetComponent<Enemy>() : null;
            if (enemy == null)
            {
                Debug.LogError($"[EnemyManager] Prefab of '{definition.id}' has no Enemy component.");
                if (go != null) pool.Despawn(go);
                return null;
            }

            float coopHp = 1f;
            if (runConfig != null && runConfig.spawnSchedule != null && runConfig.spawnSchedule.coopScaling != null)
            {
                coopHp = CoopScaling.Pick(runConfig.spawnSchedule.coopScaling.enemyHpMult, PlayerRegistry.PlayerCount);
            }

            enemy.Activate(this, definition, definition.GetHealthAtMinute(minute, coopHp),
                definition.GetContactDamageAtMinute(minute), position);
            enemy.listIndex = alive.Count;
            alive.Add(enemy);
            return enemy;
        }

        /// <summary>Create zombies ahead of time (EnemyDefinition.prewarmCount, GDD 7.8).</summary>
        public void Prewarm(EnemyDefinition definition)
        {
            if (definition == null || definition.prefab == null || definition.prewarmCount <= 0) return;
            if (pool == null) pool = ServiceLocator.Get<PoolService>();
            pool.Prewarm(definition.prefab, definition.prewarmCount);
        }

        // ------------------------------------------------------------------ simulation (50 Hz)

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            hash.Clear();
            for (int i = 0; i < alive.Count; i++)
            {
                alive[i].previousPosition = alive[i].simPosition;
                hash.Insert(alive[i], alive[i].simPosition);
            }

            float knockDecay = Feedback != null ? Feedback.knockbackDecay : 0.15f;
            float recycleDistance = runConfig != null && runConfig.spawnSchedule != null ? runConfig.spawnSchedule.recycleDistance : 35f;

            for (int i = 0; i < alive.Count; i++)
            {
                var e = alive[i];
                var def = e.Definition;

                // 2. Re-target every 0.5 s (staggered by index so they don't all do it on the same tick).
                e.retargetTimer -= dt;
                if (e.retargetTimer <= 0f || e.target == null || e.target.IsDead)
                {
                    e.target = PlayerRegistry.GetNearestAlivePlayer(e.simPosition);
                    e.retargetTimer = retargetInterval + (i % 10) * 0.01f;
                }

                Vector3 move = Vector3.zero;

                // 3. Seek (or kite for ranged enemies with keepDistance set; not in the slice yet).
                if (e.target != null)
                {
                    Vector3 to = e.target.Position - e.simPosition;
                    to.y = 0f;
                    float dist = to.magnitude;
                    if (dist > 0.001f)
                    {
                        Vector3 dir = to / dist;
                        float sign = 1f;
                        if (def.keepDistanceMax > 0f)
                        {
                            if (dist < def.keepDistanceMin) sign = -1f;
                            else if (dist <= def.keepDistanceMax) sign = 0f;
                        }

                        move = dir * (def.moveSpeed * sign);

                        // 6. Contact damage.
                        e.contactTimer -= dt;
                        if (dist <= def.radius + playerRadius && e.contactTimer <= 0f)
                        {
                            var info = new DamageInfo(e.contactDamage, e.target.Position + Vector3.up, dir, 0f, e.gameObject);
                            e.target.Health.TakeDamage(info);
                            e.contactTimer = def.contactInterval;
                        }
                    }
                }

                // 4. Separation from neighbours.
                scratch.Clear();
                hash.Query(e.simPosition, def.radius + 1f, scratch);
                Vector3 push = Vector3.zero;
                for (int n = 0; n < scratch.Count; n++)
                {
                    var other = scratch[n];
                    if (other == e) continue;
                    Vector3 away = e.simPosition - other.simPosition;
                    away.y = 0f;
                    float minDist = def.radius + other.Radius;
                    float sqr = away.sqrMagnitude;
                    if (sqr >= minDist * minDist) continue;
                    if (sqr < 0.000001f)
                    {
                        // Exactly on top of each other: push in a fixed direction based on index.
                        away = new Vector3(Mathf.Cos(i), 0f, Mathf.Sin(i)) * 0.01f;
                        sqr = away.sqrMagnitude;
                    }

                    float d = Mathf.Sqrt(sqr);
                    // Heavier zombies get pushed less (mass), so Bloaters plough through Shamblers.
                    float massRatio = other.Definition.mass / (other.Definition.mass + def.mass);
                    push += away / d * ((minDist - d) * separationStrength * 2f * massRatio);
                }

                // 5. Knockback: a velocity that fades linearly to zero over knockbackDecay seconds.
                Vector3 knock = Vector3.zero;
                if (e.knockbackTimeLeft > 0f)
                {
                    knock = e.knockbackVelocity * (e.knockbackTimeLeft / knockDecay);
                    e.knockbackTimeLeft -= dt;
                }

                e.velocity = move;
                e.simPosition += (move + push + knock) * dt;
                e.simPosition.y = 0f;

                // 7. Recycle stragglers (GDD 5: teleport to ring A ahead of a player, keep HP; not a new spawn).
                if (PlayerRegistry.DistanceToClosestPlayer(e.simPosition) > recycleDistance)
                {
                    var anchor = PlayerRegistry.NextSpawnAnchor();
                    if (anchor != null && TryGetRingPoint(anchor, out Vector3 p))
                    {
                        e.simPosition = p;
                        e.previousPosition = p;
                    }
                }
            }

            UpdateCorpses(dt);
        }

        /// <summary>A point on spawn ring A ahead of the player (used by recycling; the spawner has its own richer version).</summary>
        private bool TryGetRingPoint(PlayerController anchor, out Vector3 point)
        {
            var schedule = runConfig != null ? runConfig.spawnSchedule : null;
            float min = schedule != null ? schedule.ringMinDistance : 20f;
            float max = schedule != null ? schedule.ringMaxDistance : 24f;
            Vector3 forward = anchor.Motor.LastMoveDirection;
            float angle = Mathf.Atan2(forward.z, forward.x);
            float rand = 0.5f;
            float rand2 = 0.5f;
            if (ServiceLocator.TryGet(out RunState run) && run.Random != null)
            {
                rand = run.Random.Value();
                rand2 = run.Random.Value();
            }

            angle += (rand * 2f - 1f) * 60f * Mathf.Deg2Rad;
            float dist = Mathf.Lerp(min, max, rand2);
            point = anchor.Position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
            point.y = 0f;
            return true;
        }

        // ------------------------------------------------------------------ damage, death, corpses

        internal void OnEnemyDamaged(Enemy enemy, DamageInfo info)
        {
            var def = enemy.Definition;

            // Knockback (GDD 3.3): speed = knockback x (1 - resist) / mass, clamped to knockbackMax.
            if (info.Knockback > 0f && def != null)
            {
                float max = Feedback != null ? Feedback.knockbackMax : 12f;
                float speed = Mathf.Min(max, info.Knockback * (1f - def.knockbackResist) / Mathf.Max(0.1f, def.mass));
                Vector3 dir = info.Direction;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    enemy.knockbackVelocity = dir.normalized * speed;
                    enemy.knockbackTimeLeft = Feedback != null ? Feedback.knockbackDecay : 0.15f;
                }
            }

            // Hit flash: hold the white material for the hit type's flashMs (GDD 7.3). A new hit restarts the timer.
            int flashMs = 80;
            if (runConfig != null && runConfig.hitFeedbackProfile != null &&
                runConfig.hitFeedbackProfile.TryGet(info.IsCrit ? HitType.Crit : HitType.Normal, out var entry) && entry.flashMs > 0)
            {
                flashMs = entry.flashMs;
            }

            enemy.flashTimer = flashMs / 1000f;
            if (enemy.Binder != null) enemy.Binder.SetFlash(true, hitFlashMaterial);
        }

        internal void OnEnemyDied(Enemy enemy, DamageInfo info)
        {
            RemoveAlive(enemy);

            // XP gem drop (kill credit and crowns are handled by the RunManager via CombatEvents).
            if (pickups != null && enemy.Definition != null) pickups.SpawnGem(enemy.simPosition, enemy.Definition.xpValue);

            // Become a corpse: lie for corpseLieTime, then sink (GDD 7.5).
            enemy.state = Enemy.LifeState.Corpse;
            enemy.corpseTimer = 0f;
            enemy.corpseSinkDuration = Feedback != null ? Feedback.corpseSinkTime : 1.5f;
            enemy.flashTimer = 0f;
            if (enemy.Binder != null) enemy.Binder.SetFlash(false, hitFlashMaterial);
            enemy.SetCorpsePose();
            enemy.transform.position = enemy.simPosition;
            corpses.Add(enemy);
            corpseWeightTotal += enemy.CorpseWeight;

            // Over the cap: the oldest corpses that are still lying down start a fast sink.
            int cap = Feedback != null ? Feedback.maxCorpses : 60;
            float lie = Feedback != null ? Feedback.corpseLieTime : 3f;
            int over = corpseWeightTotal - cap;
            for (int i = 0; i < corpses.Count && over > 0; i++)
            {
                var c = corpses[i];
                if (c.corpseTimer < lie)
                {
                    c.corpseTimer = lie;
                    c.corpseSinkDuration = fastSinkTime;
                }

                over -= c.CorpseWeight;
            }
        }

        private void RemoveAlive(Enemy enemy)
        {
            int index = enemy.listIndex;
            if (index < 0 || index >= alive.Count || alive[index] != enemy) index = alive.IndexOf(enemy);
            if (index < 0) return;

            // Swap-remove: move the last zombie into this slot (O(1) instead of shifting the list).
            int last = alive.Count - 1;
            alive[index] = alive[last];
            alive[index].listIndex = index;
            alive.RemoveAt(last);
            enemy.listIndex = -1;
        }

        private void UpdateCorpses(float dt)
        {
            float lie = Feedback != null ? Feedback.corpseLieTime : 3f;
            float depth = Feedback != null ? Feedback.corpseSinkDepth : 0.6f;

            for (int i = corpses.Count - 1; i >= 0; i--)
            {
                var c = corpses[i];
                c.corpseTimer += dt;
                if (c.corpseTimer <= lie) continue;

                float t = (c.corpseTimer - lie) / Mathf.Max(0.01f, c.corpseSinkDuration);
                if (t >= 1f)
                {
                    corpses.RemoveAt(i);
                    corpseWeightTotal -= c.CorpseWeight;
                    pool.Despawn(c.gameObject);
                    continue;
                }

                c.transform.position = c.simPosition + Vector3.down * (depth * t);
            }
        }

        /// <summary>Remove every zombie and corpse (used when a run restarts without reloading).</summary>
        public void DespawnAll()
        {
            for (int i = alive.Count - 1; i >= 0; i--) pool.Despawn(alive[i].gameObject);
            for (int i = corpses.Count - 1; i >= 0; i--) pool.Despawn(corpses[i].gameObject);
            alive.Clear();
            corpses.Clear();
            corpseWeightTotal = 0;
        }

        // ------------------------------------------------------------------ presentation (every frame)

        private void Update()
        {
            float alpha = GameTick.InterpolationAlpha;
            float dt = Time.deltaTime;

            for (int i = 0; i < alive.Count; i++)
            {
                var e = alive[i];
                var t = e.transform;
                t.position = Vector3.Lerp(e.previousPosition, e.simPosition, alpha);

                // Face where it's walking.
                if (e.velocity.sqrMagnitude > 0.01f)
                {
                    var look = Quaternion.LookRotation(e.velocity, Vector3.up);
                    t.rotation = Quaternion.RotateTowards(t.rotation, look, 540f * dt);
                }

                if (e.flashTimer > 0f)
                {
                    // Unscaled so the flash still ends during hit-stop / level-up pause.
                    e.flashTimer -= Time.unscaledDeltaTime;
                    if (e.flashTimer <= 0f && e.Binder != null) e.Binder.SetFlash(false, hitFlashMaterial);
                }
            }
        }

        // ------------------------------------------------------------------ queries

        /// <summary>IHitQuery: all living zombies whose body overlaps the circle.</summary>
        public void QueryEnemies(Vector3 center, float radius, List<Health> results)
        {
            // Empty the caller's list first. Weapons reuse one list for every swing (no garbage), so
            // without this it would still hold last swing's enemies, some already dead or respawned elsewhere.
            results.Clear();
            scratch.Clear();
            hash.Query(center, radius + 1f, scratch);
            for (int i = 0; i < scratch.Count; i++)
            {
                var e = scratch[i];
                if (e.state != Enemy.LifeState.Alive || e.Health.IsDead) continue;
                Vector3 d = e.simPosition - center;
                float r = radius + e.Radius;
                if (d.x * d.x + d.z * d.z <= r * r) results.Add(e.Health);
            }
        }

        /// <summary>ITargetProvider: the nearest living zombie within range (for aim assist).</summary>
        public bool TryGetNearestEnemy(Vector3 from, float range, out Vector3 enemyPosition)
        {
            enemyPosition = Vector3.zero;
            float bestSqr = range * range;
            bool found = false;
            scratch.Clear();
            hash.Query(from, range, scratch);
            for (int i = 0; i < scratch.Count; i++)
            {
                var e = scratch[i];
                if (e.state != Enemy.LifeState.Alive) continue;
                Vector3 d = e.simPosition - from;
                float sqr = d.x * d.x + d.z * d.z;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    enemyPosition = e.simPosition;
                    found = true;
                }
            }

            return found;
        }
    }
}
