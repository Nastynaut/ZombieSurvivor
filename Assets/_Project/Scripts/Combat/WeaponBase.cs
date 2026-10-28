using KnightSurvivors.Core;
using KnightSurvivors.Data;
using UnityEngine;

namespace KnightSurvivors.Combat
{
    /// <summary>
    /// Shared logic for every auto-firing weapon: level, cooldown timer, damage + crit roll.
    /// A concrete weapon (LongswordWeapon) only has to say what happens in <see cref="Fire"/>.
    ///
    /// The cooldown counts down in FixedUpdate (the 50 Hz gameplay tick), so attack timing is the
    /// same on every PC and stops automatically when the game is paused (timeScale 0).
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        public WeaponDefinition Definition { get; private set; }
        public IWeaponOwner Owner { get; private set; }

        /// <summary>Current level, starting at 1.</summary>
        public int Level { get; private set; } = 1;

        public bool IsMaxLevel => Definition == null || Level >= Definition.EffectiveMaxLevel;

        /// <summary>The level row from the GDD table (damage, cooldown, area...) for the current level.</summary>
        public WeaponLevel CurrentLevelData => Definition.GetLevel(Level);

        private float cooldownTimer;

        /// <summary>Called once by the PlayerLoadout right after the weapon is created.</summary>
        public virtual void Initialize(IWeaponOwner owner, WeaponDefinition definition)
        {
            Owner = owner;
            Definition = definition;
            Level = 1;
            // Fire the first swing shortly after spawning so the player sees the weapon right away.
            cooldownTimer = 0.25f;
        }

        public void LevelUp()
        {
            if (!IsMaxLevel) Level++;
        }

        /// <summary>Cooldown after the owner's Cooldown stat (which has a 0.40 floor, GDD 3.1).</summary>
        public float CurrentCooldown
        {
            get
            {
                float mult = Owner != null ? Owner.Stats.cooldown : 1f;
                return Mathf.Max(0.05f, CurrentLevelData.cooldown * mult);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (Owner == null || Definition == null || !Owner.CanAttack) return;

            cooldownTimer -= Time.fixedDeltaTime;
            if (cooldownTimer <= 0f)
            {
                Fire();
                cooldownTimer += CurrentCooldown;
                if (cooldownTimer < 0f) cooldownTimer = CurrentCooldown;
            }
        }

        /// <summary>Do the attack. Called once per cooldown.</summary>
        protected abstract void Fire();

        /// <summary>
        /// GDD 3.1 damage formula: hit = weaponLevel.damage x might x (crit ? critMultiplier : 1).
        /// The crit roll uses the seeded run RNG (GDD 9.14), so the same seed gives the same crits.
        /// </summary>
        protected float RollDamage(out bool isCrit)
        {
            var stats = Owner.Stats;
            float damage = CurrentLevelData.damage * stats.might;

            isCrit = false;
            if (ServiceLocator.TryGet(out RunState run) && run.Random != null)
            {
                isCrit = run.Random.Chance(stats.critChance);
            }

            if (isCrit) damage *= stats.critMultiplier;
            return damage;
        }
    }
}
