using UnityEngine;
using UnityEngine.Pool;

namespace KnightSurvivors.Pooling
{
    /// <summary>
    /// Added automatically by PoolService to every object it creates.
    /// It remembers which pool the object belongs to, so anyone can send it back with
    /// <c>GetComponent&lt;PooledObject&gt;().Despawn()</c> or <c>PoolService.Despawn(gameObject)</c>
    /// without knowing which prefab it came from.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        internal ObjectPool<GameObject> Pool;
        internal IPoolable[] Poolables;   // cached so we don't call GetComponents every spawn
        internal bool IsInPool;

        /// <summary>The prefab this object was created from.</summary>
        public GameObject SourcePrefab { get; internal set; }

        /// <summary>Send this object back to its pool.</summary>
        public void Despawn()
        {
            // Guard against "double release", a classic pooling bug where the same object is
            // returned twice and then handed out to two different spawners at once.
            if (IsInPool) return;

            for (int i = 0; i < Poolables.Length; i++)
            {
                Poolables[i].OnDespawned();
            }

            IsInPool = true;
            gameObject.SetActive(false);
            Pool.Release(gameObject);
        }

        internal void NotifySpawned()
        {
            IsInPool = false;
            for (int i = 0; i < Poolables.Length; i++)
            {
                Poolables[i].OnSpawned();
            }
        }
    }
}
