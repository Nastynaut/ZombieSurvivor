using UnityEngine;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// "Where is the nearest enemy?" without needing to know about the Enemies code.
    ///
    /// The EnemyManager implements this and registers itself with the ServiceLocator as an
    /// ITargetProvider. Player aiming and auto-targeting weapons ask the interface. This keeps the
    /// Player assembly from depending on the Enemies assembly (which already depends on Player),
    /// so there is no circular reference.
    /// </summary>
    public interface ITargetProvider
    {
        /// <summary>Finds the closest living enemy within <paramref name="range"/> metres of <paramref name="from"/>.</summary>
        bool TryGetNearestEnemy(Vector3 from, float range, out Vector3 enemyPosition);
    }
}
