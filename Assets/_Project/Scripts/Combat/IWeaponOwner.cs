using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Combat
{
    /// <summary>
    /// What a weapon needs to know about whoever is holding it.
    /// The PlayerController implements this. Weapons never reference PlayerController directly,
    /// so a turret or an ally could hold the same weapons later.
    /// </summary>
    public interface IWeaponOwner
    {
        /// <summary>The owner's current stats (might, area, cooldown, crit, ...).</summary>
        RuntimeStats Stats { get; }

        /// <summary>Which team the owner is on (weapons only hurt the other team).</summary>
        Team Team { get; }

        /// <summary>Position on the ground (feet).</summary>
        Vector3 Position { get; }

        /// <summary>Flat, normalized direction directional weapons should fire in (decided by the aim rules).</summary>
        Vector3 AimDirection { get; }

        /// <summary>The GameObject to credit kills to.</summary>
        GameObject OwnerObject { get; }

        /// <summary>True while the owner can attack (alive, run not over).</summary>
        bool CanAttack { get; }
    }
}
