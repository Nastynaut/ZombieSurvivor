using KnightSurvivors.Combat;
using KnightSurvivors.Core;
using KnightSurvivors.Data;
using KnightSurvivors.Player;
using KnightSurvivors.Pooling;
using UnityEngine;

namespace KnightSurvivors.Enemies
{
    /// <summary>
    /// One zombie. Like gems, a zombie only HOLDS data: the EnemyManager moves all zombies in one loop.
    ///
    /// WHY NO COLLIDER, RIGIDBODY OR NAVMESHAGENT?
    /// 300+ physics bodies or NavMesh agents shoving each other is the #1 way horde games get slow.
    /// Instead, the manager keeps every zombie's position in a list, uses a spatial hash (a grid of
    /// buckets) to find neighbours quickly, and pushes overlapping zombies apart with simple maths.
    /// Weapons find zombies through the same grid (IHitQuery), so no colliders are needed at all.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour, IPoolable, IEnemyInfo
    {
        public enum LifeState { Alive, Corpse }

        public EnemyDefinition Definition { get; private set; }
        public Health Health { get; private set; }
        public VisualBinder Binder { get; private set; }

        // ---- Simulation data (written by EnemyManager on the 50 Hz tick) ----
        [HideInInspector] public Vector3 simPosition;
        [HideInInspector] public Vector3 previousPosition;
        [HideInInspector] public Vector3 velocity;
        [HideInInspector] public Vector3 knockbackVelocity;
        [HideInInspector] public float knockbackTimeLeft;
        [HideInInspector] public float contactTimer;
        [HideInInspector] public float retargetTimer;
        [HideInInspector] public float contactDamage;
        [HideInInspector] public PlayerController target;
        [HideInInspector] public float flashTimer;
        [HideInInspector] public LifeState state;
        [HideInInspector] public float corpseTimer;
        [HideInInspector] public float corpseSinkDuration;
        [HideInInspector] public int listIndex = -1;

        /// <summary>Corpses of Bloaters and elites count as 3 toward the corpse cap (GDD 7.5).</summary>
        public int CorpseWeight => Definition != null && (Definition.tier != EnemyTier.Normal || Definition.behavior == EnemyBehavior.ExplodeOnDeath) ? 3 : 1;

        public float Radius => Definition != null ? Definition.radius : 0f;

        // IEnemyInfo: lets Gore (and later UI) ask what kind of enemy this is without referencing this assembly.
        public EnemyTier Tier => Definition != null ? Definition.tier : EnemyTier.Normal;
        public bool ExplodesOnDeath => Definition != null && Definition.behavior == EnemyBehavior.ExplodeOnDeath;

        private EnemyManager manager;
        private Quaternion visualBaseRotation = Quaternion.identity;
        private Vector3 visualBaseLocalPosition;

        private void Awake()
        {
            Health = GetComponent<Health>();
            Binder = GetComponent<VisualBinder>();
            Health.team = Team.Enemy;
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
            if (Binder != null && Binder.visualRoot != null)
            {
                visualBaseRotation = Binder.visualRoot.localRotation;
                visualBaseLocalPosition = Binder.visualRoot.localPosition;
            }
        }

        private void OnDestroy()
        {
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
        }

        /// <summary>Called by EnemyManager right after taking the zombie from the pool.</summary>
        public void Activate(EnemyManager owner, EnemyDefinition definition, float maxHealth, float contact, Vector3 position)
        {
            manager = owner;
            Definition = definition;
            contactDamage = contact;
            Health.ResetHealth(maxHealth);
            simPosition = position;
            previousPosition = position;
            velocity = Vector3.zero;
            knockbackVelocity = Vector3.zero;
            knockbackTimeLeft = 0f;
            contactTimer = 0f;
            retargetTimer = 0f;
            target = null;
            flashTimer = 0f;
            state = LifeState.Alive;
            corpseTimer = 0f;
            transform.position = position;
        }

        public void OnSpawned()
        {
            ResetVisual();
        }

        public void OnDespawned()
        {
            if (Binder != null) Binder.SetFlash(false, null);
            ResetVisual();
            manager = null;
        }

        /// <summary>Tip the visual over (graybox death pose). PlaceholderSpec: corpses lie flat.</summary>
        public void SetCorpsePose()
        {
            if (Binder == null || Binder.visualRoot == null) return;
            Binder.visualRoot.localRotation = Quaternion.Euler(-90f, 0f, 0f) * visualBaseRotation;
            Binder.visualRoot.localPosition = visualBaseLocalPosition + Vector3.up * Mathf.Max(0.1f, Radius);
        }

        private void ResetVisual()
        {
            if (Binder == null || Binder.visualRoot == null) return;
            Binder.visualRoot.localRotation = visualBaseRotation;
            Binder.visualRoot.localPosition = visualBaseLocalPosition;
        }

        private void OnDamaged(Health health, DamageInfo info)
        {
            if (manager != null) manager.OnEnemyDamaged(this, info);
        }

        private void OnDied(Health health, DamageInfo info)
        {
            if (manager != null) manager.OnEnemyDied(this, info);
        }
    }
}
