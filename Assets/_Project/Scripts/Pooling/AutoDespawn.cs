using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Pooling
{
    /// <summary>
    /// Sends a pooled object back to its pool after a delay. Handy for one-shot effects
    /// (a blood burst particle effect, a hit spark) that just play and disappear.
    /// </summary>
    public class AutoDespawn : MonoBehaviour, IPoolable
    {
        [Tooltip("Seconds before this object returns to the pool.")]
        [SerializeField] private float lifetime = 1.5f;

        private float timer;

        public float Lifetime
        {
            get => lifetime;
            set => lifetime = value;
        }

        public void OnSpawned()
        {
            timer = lifetime;
        }

        public void OnDespawned() { }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            if (ServiceLocator.TryGet<PoolService>(out var pools))
                pools.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
