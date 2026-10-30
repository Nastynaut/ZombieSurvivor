using System.Collections.Generic;
using UnityEngine;

namespace KnightSurvivors.Combat
{
    /// <summary>
    /// "Give me every enemy within this radius." Implemented by the EnemyManager using its spatial hash.
    ///
    /// WHY NOT Physics.OverlapSphere?
    /// Zombies have no colliders at all (PlaceholderSpec 0: crowd zombies use distance checks). 350
    /// colliders being moved every tick costs physics time for nothing, because the EnemyManager
    /// already knows where every zombie is. Weapons ask through this interface, so Combat doesn't
    /// need to reference the Enemies assembly.
    /// </summary>
    public interface IHitQuery
    {
        /// <summary>Clears <paramref name="results"/> and fills it with living enemies whose body overlaps the circle (XZ plane).</summary>
        void QueryEnemies(Vector3 center, float radius, List<Health> results);
    }
}
