// ---------------------------------------------------------------------------------------------
// All the shared enums from the GDD (docs/GDD.md, section 9.1), in one file.
//
// WHY ARE THESE IN "Core" AND NOT "Data"?
// Both the data assets (ScriptableObjects) and the runtime code (RuntimeStats, weapons, gore)
// need them. Core is referenced by every other assembly, so putting them here means everyone
// can use them without creating circular references between assemblies.
//
// Names and values match GDD 9.1 exactly. Add new values at the END of an enum: Unity saves
// enums as numbers, so inserting in the middle would silently change existing assets.
// ---------------------------------------------------------------------------------------------
namespace KnightSurvivors.Core
{
    public enum DamageType { Physical, Holy, Fire, Poison }

    public enum WeaponArchetype { MeleeArc, Projectile, Orbit, LobbedProjectile, GroundZone, Aura }

    public enum TargetingMode { AimDirection, NearestEnemy, RandomEnemyInRange, PlayerCentered, MoveDirection }

    /// <summary>
    /// How directional weapons (Longsword, Throwing Axes) choose where to swing.
    /// Assisted (default): mouse cursor / right stick if in use, else nearest enemy in range, else move direction.
    /// FullAuto: always nearest enemy in range, else move direction.
    /// </summary>
    public enum AimMode { Assisted, FullAuto }

    public enum StatType
    {
        MaxHealth, Recovery, Armor, MoveSpeed, Might, Area, Cooldown, Amount, Duration,
        ProjectileSpeed, CritChance, CritMultiplier, PickupRadius, Luck, Growth, Greed,
        Revivals, Rerolls, Banishes,
        // Appended for limited level-up Skips (docs/DataTables.md §0.1). Never insert in the middle.
        Skips
    }

    /// <summary>
    /// How a modifier combines with a stat. Final value = (base + all Add) x (1 + all PercentAdd) x (all Multiply).
    /// </summary>
    public enum ModifierOp { Add, PercentAdd, Multiply }

    public enum EnemyBehavior { Chase, ExplodeOnDeath, RangedKite, ChargeElite, Boss }

    public enum EnemyTier { Normal, Elite, Boss }

    public enum SpawnPattern { RingRandom, RingFull, LineStampede, Cluster }

    public enum WaveEventType { Swarm, Elite, Boss, EnableEvolutions, Chest }

    public enum EvolutionConditionType { ChestAfterMinute }

    public enum UnlockConditionType { KillTotal, SurviveMinutes, ReachLevel, EvolveWeapon, DefeatBoss }

    public enum HitType { Normal, Crit, Overkill, EliteHit, EliteKill, BossHit, BossPhaseChange, BossKill, PlayerHurt, Explosion }

    /// <summary>Gore option. Off = grey dust instead of blood, Low = half, Full = everything (default).</summary>
    public enum BloodIntensity { Off, Low, Full }

    public enum PickupType { XpGem, RoastChicken, CoinPouch, HolyReliquary, Chest }
}
