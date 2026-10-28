using System;
using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Combat
{
    /// <summary>
    /// Hit points for anything that can be hurt: players and zombies.
    ///
    /// Health only does bookkeeping (subtract HP, tell everyone). It does NOT spawn blood, play
    /// sounds or drop XP. Those systems LISTEN to the events below. That way adding "screen shake
    /// on hit" later means writing a new listener, not editing Health.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Tooltip("Which side this belongs to. Weapons only hurt the other team.")]
        public Team team = Team.Enemy;

        [Tooltip("Starting / maximum HP. Players and enemies overwrite this from their data at spawn.")]
        public float maxHealth = 10f;

        [Tooltip("Flat damage reduction per hit. A hit always does at least 1 damage (GDD 3.1).")]
        public float armor;

        [Tooltip("How wide the body is (m), seen from above. Melee range checks add this so a big zombie is hit when its edge, not its centre, is in reach. Enemies set it from their data at spawn.")]
        public float bodyRadius;

        [Tooltip("Optional: where blood spawns. If empty, uses the VisualBinder hit socket or 1 m above the feet.")]
        public Transform hitPoint;

        /// <summary>Fired after damage is applied. (this, damage info with the FINAL amount)</summary>
        public event Action<Health, DamageInfo> Damaged;

        /// <summary>Fired once when HP reaches 0. (this, the killing blow)</summary>
        public event Action<Health, DamageInfo> Died;

        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        /// <summary>HP as 0..1 for health bars.</summary>
        public float Normalized => maxHealth > 0f ? CurrentHealth / maxHealth : 0f;

        // Dodge i-frames (dodge roll comes later): no damage until this time.
        private float invulnerableUntil;

        private VisualBinder binder;

        private void Awake()
        {
            binder = GetComponent<VisualBinder>();
            if (CurrentHealth <= 0f && !IsDead) CurrentHealth = maxHealth;
        }

        /// <summary>Where blood / damage numbers should appear.</summary>
        public Vector3 HitPosition
        {
            get
            {
                if (hitPoint != null) return hitPoint.position;
                if (binder != null) return binder.HitPosition;
                return transform.position + Vector3.up;
            }
        }

        /// <summary>Refill to full and clear the dead flag (pooled zombies call this on respawn).</summary>
        public void ResetHealth(float newMax)
        {
            maxHealth = Mathf.Max(1f, newMax);
            CurrentHealth = maxHealth;
            IsDead = false;
            invulnerableUntil = 0f;
        }

        /// <summary>Change max HP (e.g. a MaxHealth upgrade). Keeps the same amount of MISSING HP, so +20 max HP also heals 20.</summary>
        public void SetMaxHealth(float newMax)
        {
            float missing = maxHealth - CurrentHealth;
            maxHealth = Mathf.Max(1f, newMax);
            CurrentHealth = Mathf.Clamp(maxHealth - missing, 1f, maxHealth);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        /// <summary>Ignore all damage for the given seconds (dodge roll, revive).</summary>
        public void SetInvulnerable(float seconds)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
        }

        /// <summary>
        /// Apply damage. Returns the damage actually dealt (0 if ignored).
        /// </summary>
        public float TakeDamage(DamageInfo info)
        {
            if (IsDead || Time.time < invulnerableUntil) return 0f;

            // GDD 3.1: armor is a flat reduction, but every hit does at least 1 damage.
            float amount = armor > 0f ? Mathf.Max(1f, info.Amount - armor) : info.Amount;
            if (amount <= 0f) return 0f;

            // Remember HP before the hit, so listeners can detect an overkill (damage >= 2x remaining HP).
            float before = CurrentHealth;
            CurrentHealth -= amount;
            info.Amount = amount;

            Damaged?.Invoke(this, info);
            CombatEvents.RaiseDamaged(this, info, before);

            if (CurrentHealth <= 0f)
            {
                CurrentHealth = 0f;
                IsDead = true;
                Died?.Invoke(this, info);
                CombatEvents.RaiseDied(this, info, before);
            }

            return amount;
        }
    }
}
