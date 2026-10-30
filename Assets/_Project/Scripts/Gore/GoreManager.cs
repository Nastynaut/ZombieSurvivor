using System.Collections.Generic;
using KnightSurvivors.Combat;
using KnightSurvivors.Core;
using KnightSurvivors.Data;
using KnightSurvivors.Pooling;
using UnityEngine;

namespace KnightSurvivors.Gore
{
    /// <summary>
    /// All the blood. It is EVENT-DRIVEN: weapons and enemies never call Gore directly. They just deal
    /// damage; Health raises CombatEvents.AnyDamaged / AnyDied, and this class listens and reacts.
    /// That means you can delete Gore from the scene and the game still works (just cleaner).
    ///
    /// Two effects (GDD 7.4 - 7.6):
    ///   1. BLOOD BURSTS: pooled particle systems. We call ParticleSystem.Emit(n) with the GDD count,
    ///      so the prefab itself emits nothing on its own. Max 96 alive; when full, the oldest is reused.
    ///   2. GROUND DECALS: graybox version uses flat discs (a Quad lying on the ground) instead of URP
    ///      Decal Projectors, because discs need no renderer setup. 100 max, 20 s life (16 solid + 4 fade),
    ///      wet material for 6 s, then a swap to the shared dried material. At most 32 NEW decals per frame; extra requests wait in a queue.
    ///      If 3+ decals are already within 1 m, the nearest one grows 15% (max 2x) instead of adding another.
    ///
    /// Blood setting: Full (1.0), Low (0.5 particles, 10 s decals), Off (grey dust at 0.5 counts, no red decals;
    /// scorch marks from fire are still allowed - SpawnScorch - max 50).
    /// No dismemberment, by design.
    /// </summary>
    public class GoreManager : MonoBehaviour
    {
        [Header("Data")]
        public RunConfig runConfig;

        [Header("Blood burst prefabs (ParticleSystem, emission off, Play On Awake off)")]
        public ParticleSystem hitBloodPrefab;
        public ParticleSystem critBloodPrefab;
        public ParticleSystem killBloodPrefab;
        [Tooltip("Big burst (Max Particles 80) for Bloater, elite and boss kills.")]
        public ParticleSystem bigBloodPrefab;
        [Tooltip("Prewarm for the Big burst (GDD 7.8: 16, overriding the spec's 8 so a 15-Bloater herd fits).")]
        public int bigBloodPrewarm = 16;
        [Tooltip("Blood Off: grey dust puff used instead of blood.")]
        public ParticleSystem dustPrefab;

        [Header("Ground decal (a flat Quad lying on the floor, no collider) - PlaceholderSpec 6.2 fallback")]
        public GameObject decalPrefab;
        [Tooltip("Shared material for fresh, wet blood (#B3121B).")]
        public Material decalWetMaterial;
        [Tooltip("Shared material for dried blood (#7A0A10). Swapped in after decalWetTime.")]
        public Material decalDryMaterial;
        [Tooltip("Shared charcoal material for scorch marks (#26282B). Kept even with Blood Off.")]
        public Material scorchMaterial;

        [Tooltip("Seconds a burst system stays checked out before we return it to the pool.")]
        public float burstLifetime = 1.5f;

        // ---------------------------------------------------------------- bursts
        private struct ActiveBurst
        {
            public ParticleSystem system;
            public float endTime;
        }

        private readonly List<ActiveBurst> bursts = new List<ActiveBurst>();
        private PoolService pool;

        // ---------------------------------------------------------------- decals
        private class Decal
        {
            public Transform transform;
            public Renderer renderer;
            public float spawnTime;
            public float baseSize;
            public float growth = 1f;
            public bool active;
            public bool wet;
            public bool scorch;
        }

        private struct DecalRequest
        {
            public Vector3 position;
            public float size;
        }

        private Decal[] decals;
        private Decal[] scorches;
        private readonly Queue<DecalRequest> decalQueue = new Queue<DecalRequest>();
        private Transform decalRoot;
        private int decalSpawnIndex;

        private FeedbackSettings Settings => runConfig != null ? runConfig.feedbackSettings : null;

        /// <summary>Current blood setting. Change it at runtime (options menu later) without touching the asset.</summary>
        public BloodIntensity Intensity { get; set; } = BloodIntensity.Full;

        private float IntensityValue => Intensity == BloodIntensity.Off ? 0f : Intensity == BloodIntensity.Low ? 0.5f : 1f;

        private void Awake()
        {
            ServiceLocator.Register(this);
            if (Settings != null) Intensity = Settings.bloodIntensity;
        }

        private void Start()
        {
            pool = ServiceLocator.Get<PoolService>();
            var s = Settings;
            if (pool != null && s != null)
            {
                if (hitBloodPrefab != null) pool.Prewarm(hitBloodPrefab.gameObject, s.bloodPrewarmHit);
                if (critBloodPrefab != null) pool.Prewarm(critBloodPrefab.gameObject, s.bloodPrewarmCrit);
                if (killBloodPrefab != null) pool.Prewarm(killBloodPrefab.gameObject, s.bloodPrewarmKill);
                if (bigBloodPrefab != null) pool.Prewarm(bigBloodPrefab.gameObject, bigBloodPrewarm);
                // Blood Off swaps every burst to dustPrefab. Prewarm it too so the first Off hit
                // does not grow the pool (and log a pool-growth warning) mid-fight. Same starting
                // size as hit blood — there is no separate dust count in the GDD.
                if (dustPrefab != null) pool.Prewarm(dustPrefab.gameObject, s.bloodPrewarmHit);
            }

            // Decals are a fixed ring of objects we own (never grows; oldest is recycled).
            decalRoot = new GameObject("Decals").transform;
            decalRoot.SetParent(transform, false);
            decals = CreateDecals(s != null ? s.maxDecals : 100, false);
            scorches = CreateDecals(s != null ? s.maxScorchMarks : 50, true);
        }

        private void OnEnable()
        {
            CombatEvents.AnyDamaged += OnDamaged;
            CombatEvents.AnyDied += OnDied;
        }

        private void OnDisable()
        {
            CombatEvents.AnyDamaged -= OnDamaged;
            CombatEvents.AnyDied -= OnDied;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        // ================================================================ reacting to combat

        private void OnDamaged(Health health, DamageInfo info, float healthBefore)
        {
            if (health == null || health.team != Team.Enemy) return;

            // A killing blow is handled by OnDied (bigger effect), so skip the normal hit spray.
            // (Damaged is raised just BEFORE IsDead is set, so we check the HP instead.)
            if (health.CurrentHealth <= 0f) return;

            // GDD 7.4: clamp(4 + 20 * damage / maxHealth, 4, 24), x1.5 on crit, x bloodIntensity.
            float maxHp = Mathf.Max(1f, health.maxHealth);
            float count = Mathf.Clamp(4f + 20f * info.Amount / maxHp, 4f, 24f);
            if (info.IsCrit) count *= 1.5f;

            var prefab = info.IsCrit && critBloodPrefab != null ? critBloodPrefab : hitBloodPrefab;
            Burst(prefab, info.Point, info.Direction, count, false);
        }

        private void OnDied(Health health, DamageInfo info, float healthBefore)
        {
            if (health == null || health.team != Team.Enemy) return;

            var enemyInfo = health.GetComponent<IEnemyInfo>();
            EnemyTier tier = enemyInfo != null ? enemyInfo.Tier : EnemyTier.Normal;
            bool bloater = enemyInfo != null && enemyInfo.ExplodesOnDeath;
            bool overkill = info.Amount >= 2f * healthBefore;

            // Pick the GDD row: elite/boss 80 + 3 decals, bloater 40 + big decal, overkill 30 + 1 extra large, else 12 + 1.
            float count = 12f;
            int smallDecals = 1;
            int largeDecals = 0;
            bool radial = false;

            if (tier != EnemyTier.Normal)
            {
                count = 80f; radial = true; smallDecals = 0; largeDecals = 3;
            }
            else if (bloater)
            {
                count = 40f; radial = true; smallDecals = 0; largeDecals = 1; // ~2.5 m bile splat
            }
            else if (overkill)
            {
                count = 30f; radial = true; largeDecals = 1;
            }

            Vector3 ground = health.transform.position;
            Vector3 point = info.Point == Vector3.zero ? ground + Vector3.up * 0.8f : info.Point;
            var prefab = count > 32f && bigBloodPrefab != null ? bigBloodPrefab : killBloodPrefab != null ? killBloodPrefab : hitBloodPrefab;
            Burst(prefab, point, info.Direction, count, radial);

            // No red decals with Blood Off.
            if (Intensity == BloodIntensity.Off) return;

            // INTENTIONAL: UnityEngine.Random (not the seeded RunRandom) for these cosmetic sizes and offsets.
            // Gore is looks only. If it drew from the gameplay random stream, turning Blood Off (or a
            // different particle budget) would change which upgrade cards and spawns a seed produces.
            // Please don't "fix" this into RunRandom.
            for (int i = 0; i < smallDecals; i++) QueueDecal(ground, Random.Range(0.6f, 1.4f));
            for (int i = 0; i < largeDecals; i++)
            {
                Vector2 jitter = Random.insideUnitCircle * (i == 0 ? 0f : 0.8f);
                float size = bloater && i == 0 ? 2.5f : Random.Range(1.5f, 2.5f);
                QueueDecal(ground + new Vector3(jitter.x, 0f, jitter.y), size);
            }
        }

        // ================================================================ particles

        private void Burst(ParticleSystem prefab, Vector3 point, Vector3 direction, float count, bool radial)
        {
            if (pool == null) return;

            // Blood Off: grey dust at the same counts x 0.5 instead of blood.
            if (Intensity == BloodIntensity.Off)
            {
                prefab = dustPrefab;
                count *= 0.5f;
            }
            else
            {
                count *= IntensityValue;
            }

            if (prefab == null) return;

            // Global particle budget: shrink new bursts as we approach maxBloodParticles.
            var s = Settings;
            int maxSystems = s != null ? s.maxBloodSystems : 96;
            int maxParticles = s != null ? s.maxBloodParticles : 3000;
            int live = LiveParticlesThisFrame();
            int n = Mathf.Min(Mathf.RoundToInt(count), Mathf.Max(0, maxParticles - live));
            if (n <= 0) return;
            liveParticlesCached += n; // keep this frame's estimate up to date without recounting

            // Too many systems alive? Steal the oldest (it is first in the list).
            if (bursts.Count >= maxSystems) ReturnBurst(0);

            // Face the spray along the hit direction (the cone shape points along local +Z), tilted up a bit.
            Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            dir = (dir + Vector3.up * 0.35f).normalized;
            var go = pool.Spawn(prefab.gameObject, point, Quaternion.LookRotation(dir));
            if (go == null) return;

            var ps = go.GetComponent<ParticleSystem>();
            var shape = ps.shape;
            if (radial)
            {
                shape.shapeType = ParticleSystemShapeType.Hemisphere;
                go.transform.rotation = Quaternion.LookRotation(Vector3.up);
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 35f;
            }

            ps.Clear(true);
            ps.Play(true); // emission module is off, so Play itself spawns nothing...
            ps.Emit(n);    // ...and we emit exactly the GDD count.

            bursts.Add(new ActiveBurst { system = ps, endTime = Time.time + burstLifetime });
        }

        // A sword swing into 30 zombies calls Burst 30 times in one frame. Counting every system's
        // particles each time would be ~3000 reads, so we count once per frame and then just add
        // what we emit. (Particles that expired this frame are counted until next frame: a tiny,
        // safe over-estimate.)
        private int liveParticlesFrame = -1;
        private int liveParticlesCached;

        private int LiveParticlesThisFrame()
        {
            if (liveParticlesFrame != Time.frameCount)
            {
                liveParticlesFrame = Time.frameCount;
                liveParticlesCached = CountLiveParticles();
            }

            return liveParticlesCached;
        }

        private int CountLiveParticles()
        {
            int total = 0;
            for (int i = 0; i < bursts.Count; i++)
            {
                if (bursts[i].system != null) total += bursts[i].system.particleCount;
            }

            return total;
        }

        private void ReturnBurst(int index)
        {
            var b = bursts[index];
            bursts.RemoveAt(index);
            if (b.system == null) return;
            b.system.Clear(true);
            pool.Despawn(b.system.gameObject);
        }

        // ================================================================ decals

        private Decal[] CreateDecals(int count, bool scorch)
        {
            var list = new Decal[Mathf.Max(0, count)];
            for (int i = 0; i < list.Length; i++)
            {
                GameObject go = decalPrefab != null ? Instantiate(decalPrefab, decalRoot) : CreateFallbackDecal();
                go.name = scorch ? "Scorch" : "BloodDecal";
                go.SetActive(false);
                list[i] = new Decal
                {
                    transform = go.transform,
                    renderer = go.GetComponentInChildren<Renderer>(),
                    scorch = scorch,
                };
            }

            return list;
        }

        private GameObject CreateFallbackDecal()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(decalRoot, false);
            return go;
        }

        /// <summary>Ask for a blood splat. It appears this frame, or a later frame if more than 32 were asked for.</summary>
        public void QueueDecal(Vector3 position, float size)
        {
            if (Intensity == BloodIntensity.Off || decals == null) return;
            decalQueue.Enqueue(new DecalRequest { position = position, size = size });
        }

        /// <summary>Scorch mark for fire weapons (later). Kept even with Blood Off. Max 50; oldest recycled.</summary>
        public void SpawnScorch(Vector3 position, float size)
        {
            if (scorches == null) return;
            var d = TakeOldest(scorches);
            Place(d, position, size, scorchMaterial);
        }

        private void LateUpdate()
        {
            var s = Settings;
            UpdateBursts();
            if (decals == null) return;

            // 1) Spawn up to maxNewDecalsPerFrame from the queue.
            int budget = s != null ? s.maxNewDecalsPerFrame : 32;
            while (budget > 0 && decalQueue.Count > 0)
            {
                var req = decalQueue.Dequeue();
                if (!TryMerge(req.position)) Place(TakeOldest(decals), req.position, req.size, decalWetMaterial);
                budget--;
            }

            // 2) Age the decals: wet -> dry swap, then shrink away during the fade.
            float lifetime = s != null ? s.decalLifetime : 20f;
            if (Intensity == BloodIntensity.Low) lifetime = 10f;
            Age(decals, lifetime, true);
            Age(scorches, s != null ? s.decalLifetime : 20f, false);
        }

        private void UpdateBursts()
        {
            float now = Time.time;
            for (int i = bursts.Count - 1; i >= 0; i--)
            {
                if (bursts[i].system == null || now >= bursts[i].endTime) ReturnBurst(i);
            }
        }

        /// <summary>GDD merge rule: if 3+ decals are within 1 m, grow the nearest by 15% (max 2x) and reset its life.</summary>
        private bool TryMerge(Vector3 position)
        {
            var s = Settings;
            float radius = s != null ? s.decalMergeRadius : 1f;
            int threshold = s != null ? s.decalMergeThreshold : 3;
            float maxScale = s != null ? s.decalMaxScale : 2f;
            float r2 = radius * radius;

            int near = 0;
            Decal nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < decals.Length; i++)
            {
                var d = decals[i];
                if (!d.active) continue;
                Vector3 delta = d.transform.position - position;
                delta.y = 0f;
                float dist = delta.sqrMagnitude;
                if (dist > r2) continue;
                near++;
                if (dist < best)
                {
                    best = dist;
                    nearest = d;
                }
            }

            if (near < threshold || nearest == null) return false;

            nearest.growth = Mathf.Min(nearest.growth * 1.15f, maxScale);
            nearest.spawnTime = Time.time;
            SetMaterial(nearest, decalWetMaterial);
            nearest.wet = true;
            ApplyScale(nearest, 1f);
            return true;
        }

        private static Decal TakeOldest(Decal[] list)
        {
            Decal oldest = list[0];
            for (int i = 0; i < list.Length; i++)
            {
                if (!list[i].active) return list[i];
                if (list[i].spawnTime < oldest.spawnTime) oldest = list[i];
            }

            return oldest;
        }

        private void Place(Decal d, Vector3 position, float size, Material material)
        {
            // Lie flat just above the floor. Each new decal is a hair higher than the last (8 steps, PlaceholderSpec 6.2)
            // so overlapping splats don't flicker against each other ("z-fighting"). The knight's ring is above all of them.
            float y = 0.003f + (decalSpawnIndex++ % 8) * 0.0005f;
            d.transform.SetPositionAndRotation(
                new Vector3(position.x, y, position.z),
                Quaternion.Euler(90f, Random.Range(0f, 360f), 0f)); // cosmetic-only Random (see the INTENTIONAL note above)
            d.baseSize = size;
            d.growth = 1f;
            d.spawnTime = Time.time;
            d.active = true;
            d.wet = !d.scorch;
            SetMaterial(d, material);
            ApplyScale(d, 1f);
            d.transform.gameObject.SetActive(true);
        }

        private void Age(Decal[] list, float lifetime, bool dries)
        {
            if (list == null) return;
            float fade = Settings != null ? Settings.decalFade : 4f;
            float wetTime = Settings != null ? Settings.decalWetTime : 6f;
            float now = Time.time;

            for (int i = 0; i < list.Length; i++)
            {
                var d = list[i];
                if (!d.active) continue;
                float age = now - d.spawnTime;

                if (age >= lifetime)
                {
                    d.active = false;
                    d.transform.gameObject.SetActive(false);
                    continue;
                }

                if (dries && d.wet && age >= wetTime)
                {
                    d.wet = false;
                    SetMaterial(d, decalDryMaterial);
                }

                // Opaque graybox material: we "fade" by shrinking during the last `fade` seconds.
                float fadeStart = lifetime - fade;
                if (age > fadeStart) ApplyScale(d, 1f - (age - fadeStart) / Mathf.Max(0.01f, fade));
            }
        }

        private static void ApplyScale(Decal d, float fade01)
        {
            float s = d.baseSize * d.growth * Mathf.Clamp01(fade01);
            // A Quad rotated 90 degrees on X lies flat; its local X/Y become world X/Z. Slightly oval (S x 0.8S).
            d.transform.localScale = new Vector3(s, s * 0.8f, 1f);
        }

        /// <summary>Swap between SHARED materials (wet / dry / scorch). No per-instance material copies.</summary>
        private static void SetMaterial(Decal d, Material material)
        {
            if (d.renderer != null && material != null) d.renderer.sharedMaterial = material;
        }
    }
}
