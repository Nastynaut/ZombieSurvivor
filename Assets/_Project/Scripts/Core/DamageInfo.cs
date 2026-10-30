using UnityEngine;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// Everything we know about one hit, bundled together.
    ///
    /// WHY A STRUCT INSTEAD OF JUST A float AMOUNT?
    /// Other systems want more than the number: the gore system wants the hit POINT and DIRECTION
    /// to spray blood the right way, enemies want KNOCKBACK, and later a damage-number popup or a
    /// "who killed whom" stat wants the SOURCE. Passing one bundle keeps every method signature short
    /// and lets us add fields later without changing every caller.
    /// </summary>
    public struct DamageInfo
    {
        /// <summary>How much health to remove.</summary>
        public float Amount;

        /// <summary>World position where the hit landed (used to spawn blood).</summary>
        public Vector3 Point;

        /// <summary>Direction the hit travelled, flattened to the ground (used for blood spray and knockback).</summary>
        public Vector3 Direction;

        /// <summary>How hard the target is pushed back (0 = no push).</summary>
        public float Knockback;

        /// <summary>Who caused the hit (may be null).</summary>
        public GameObject Source;

        /// <summary>True if this was a critical hit (bigger blood burst, later a yellow damage number).</summary>
        public bool IsCrit;

        /// <summary>Physical, Holy, Fire... (for future resistances, sounds and VFX).</summary>
        public DamageType DamageType;

        public DamageInfo(float amount, Vector3 point, Vector3 direction, float knockback, GameObject source,
            bool isCrit = false, DamageType damageType = DamageType.Physical)
        {
            IsCrit = isCrit;
            DamageType = damageType;
            Amount = amount;
            Point = point;
            Direction = direction;
            Knockback = knockback;
            Source = source;
        }
    }
}
