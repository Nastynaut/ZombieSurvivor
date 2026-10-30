namespace KnightSurvivors.Pooling
{
    /// <summary>
    /// Put this interface on any component that needs to reset itself when it is re-used from a pool.
    ///
    /// With pooling, Awake() and Start() only run ONCE (the first time the object is created),
    /// so they are the wrong place to reset health, timers, etc. Use OnSpawned() instead.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called right after the object is taken from the pool, positioned, and activated.</summary>
        void OnSpawned();

        /// <summary>Called right before the object goes back into the pool and is deactivated.</summary>
        void OnDespawned();
    }
}
