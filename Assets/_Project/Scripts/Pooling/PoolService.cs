using System.Collections.Generic;
using KnightSurvivors.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace KnightSurvivors.Pooling
{
    /// <summary>
    /// One place that pools EVERYTHING that spawns a lot: zombies, XP gems, blood bursts, decals,
    /// later projectiles and VFX.
    ///
    /// WHY POOL?
    /// Instantiate() and Destroy() are slow-ish and create garbage that makes the garbage collector
    /// pause the game (visible stutters). In a horde game we spawn and kill hundreds of things per
    /// minute, so instead we keep finished objects switched off in a pool and switch them back on
    /// when needed. After the first few seconds, the game creates almost nothing new.
    ///
    /// HOW TO USE
    ///   var zombie = ServiceLocator.Get&lt;PoolService&gt;().Spawn(zombiePrefab, position, rotation);
    ///   ... later ...
    ///   ServiceLocator.Get&lt;PoolService&gt;().Despawn(zombie);
    ///
    /// Under the hood it uses Unity's built-in UnityEngine.Pool.ObjectPool, one pool per prefab.
    ///
    /// POOLS ALWAYS GROW (GDD 7.8): Spawn() never fails. If a pool is empty it creates a new object
    /// and, in the Editor / development builds only, logs a warning so we know the prewarm count
    /// is too small. Budgets (max blood systems, max decals, max corpses...) are enforced by the
    /// systems that use the pools (by recycling their oldest object), NOT by the pool size.
    /// </summary>
    public class PoolService : MonoBehaviour
    {
        [Tooltip("Max objects kept switched-off per prefab. Extra ones beyond this are destroyed when released. (The pool can still GROW past this; this only limits how many idle objects are kept.)")]
        [SerializeField] private int maxPooledPerPrefab = 2000;

        [Tooltip("Optional list of extra prefabs to create ahead of time (XP gems, bile globs...). Enemies and blood prewarm themselves from their own data.")]
        [SerializeField] private PoolPrewarmConfig prewarmConfig;

        // How many objects each pool has created, and the prewarm size we expected to be enough.
        private readonly Dictionary<GameObject, int> createdCounts = new Dictionary<GameObject, int>();
        private readonly Dictionary<GameObject, int> prewarmSizes = new Dictionary<GameObject, int>();

        // One ObjectPool per prefab. The prefab GameObject itself is the dictionary key.
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> pools =
            new Dictionary<GameObject, ObjectPool<GameObject>>();

        // Empty parent objects so the Hierarchy window stays tidy ("Pool_Zombie", "Pool_XPGem"...).
        private readonly Dictionary<GameObject, Transform> containers = new Dictionary<GameObject, Transform>();

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            if (prewarmConfig == null || prewarmConfig.entries == null) return;
            for (int i = 0; i < prewarmConfig.entries.Count; i++)
            {
                var entry = prewarmConfig.entries[i];
                if (entry.prefab != null && entry.prewarmCount > 0) Prewarm(entry.prefab, entry.prewarmCount);
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        /// <summary>
        /// Take an object from the pool (or create one if the pool is empty), place it, and switch it on.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolService] Tried to spawn a null prefab.");
                return null;
            }

            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();

            // Position BEFORE activating, so it never appears for a frame at its old position.
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            instance.GetComponent<PooledObject>().NotifySpawned();
            return instance;
        }

        /// <summary>Convenience version that returns a component, e.g. Spawn(enemyPrefabComponent, ...).</summary>
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null) return null;
            var instance = Spawn(prefab.gameObject, position, rotation);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <summary>
        /// Return an object to its pool. Objects that did not come from a pool are simply destroyed,
        /// so calling this is always safe.
        /// </summary>
        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            if (instance.TryGetComponent(out PooledObject pooled) && pooled.Pool != null)
            {
                pooled.Despawn();
            }
            else
            {
                Destroy(instance);
            }
        }

        /// <summary>
        /// Create objects ahead of time (for example during a loading screen) so the first wave
        /// does not stutter while the pool fills up.
        /// </summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            var pool = GetOrCreatePool(prefab);
            prewarmSizes.TryGetValue(prefab, out int previous);
            prewarmSizes[prefab] = Mathf.Max(previous, count);
            var temp = new List<GameObject>(count);
            for (int i = 0; i < count; i++) temp.Add(pool.Get());
            for (int i = 0; i < temp.Count; i++) pool.Release(temp[i]);
        }

        private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (pools.TryGetValue(prefab, out var existing)) return existing;

            var container = new GameObject($"Pool_{prefab.name}").transform;
            container.SetParent(transform, false);
            containers[prefab] = container;

            ObjectPool<GameObject> pool = null;
            pool = new ObjectPool<GameObject>(
                createFunc: () => CreateTracked(prefab, container, pool),
                actionOnGet: null,      // Spawn() handles activation so it can position first
                actionOnRelease: null,  // PooledObject.Despawn() handles deactivation
                actionOnDestroy: obj => { if (obj != null) Destroy(obj); },
                // collectionCheck = true would catch double-releases but costs time on every release.
                // PooledObject already guards against double-release, so we keep it off for speed.
                collectionCheck: false,
                defaultCapacity: 32,
                maxSize: maxPooledPerPrefab);

            pools[prefab] = pool;
            return pool;
        }

        private GameObject CreateTracked(GameObject prefab, Transform parent, ObjectPool<GameObject> pool)
        {
            createdCounts.TryGetValue(prefab, out int created);
            created++;
            createdCounts[prefab] = created;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev-only warning when a pool grows past its prewarm size. To avoid spamming the
            // Console we only warn at the first overflow and then each time the size doubles.
            if (prewarmSizes.TryGetValue(prefab, out int prewarm) && prewarm > 0 && created > prewarm)
            {
                int over = created - prewarm;
                if ((over & (over - 1)) == 0) // 1, 2, 4, 8, 16 ... extra objects
                {
                    Debug.LogWarning($"[PoolService] Pool '{prefab.name}' grew to {created} (prewarm was {prewarm}). " +
                                     "Not an error: consider raising its prewarm count.");
                }
            }
#endif

            return CreateInstance(prefab, parent, pool);
        }

        private static GameObject CreateInstance(GameObject prefab, Transform parent, ObjectPool<GameObject> pool)
        {
            var obj = Instantiate(prefab, parent);
            obj.name = prefab.name; // drop the "(Clone)" suffix
            obj.SetActive(false);

            var pooled = obj.GetComponent<PooledObject>();
            if (pooled == null) pooled = obj.AddComponent<PooledObject>();
            pooled.Pool = pool;
            pooled.SourcePrefab = prefab;
            pooled.Poolables = obj.GetComponentsInChildren<IPoolable>(true);
            pooled.IsInPool = true;
            return obj;
        }
    }
}
