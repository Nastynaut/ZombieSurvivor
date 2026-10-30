using System;
using KnightSurvivors.Combat;
using KnightSurvivors.Core;
using KnightSurvivors.Data;
using UnityEngine;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// The "brain" of one knight. It owns this player's state (health, stats, kills) and connects
    /// the parts that live on the same object:
    ///   PlayerInputHandler (reads the device) -> PlayerMotor (moves) + PlayerAim (aims)
    ///   PlayerLoadout (weapons + passives)     -> weapons ask this class for stats and aim (IWeaponOwner)
    ///   Health (takes damage)                  -> death -> PlayerRegistry tells the RunManager
    ///
    /// There can be 1 to 4 of these. Nothing in the game assumes there is only one.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerAim))]
    [RequireComponent(typeof(PlayerLoadout))]
    public class PlayerController : MonoBehaviour, IWeaponOwner
    {
        // IMPORTANT: this must be a SMOOTHED child (listed in TickInterpolator), never the root itself.
        // The root only moves at 50 Hz, so a camera following it jitters at high frame rates.
        [Tooltip("The transform the camera follows: the smoothed 'CameraTarget' child. Never the root (it moves at 50 Hz and makes the camera jitter). Leave empty: a smoothed child is created at runtime.")]
        public Transform cameraTarget;

        [Tooltip("How long the red 'you got hurt' tint shows, in seconds (HitFeedbackProfile PlayerHurt flashMs overrides this).")]
        public float hurtFlashSeconds = 0.12f;

        /// <summary>All the saveable / networkable data for this player. A plain C# class (GDD 9.14).</summary>
        public PlayerState State { get; private set; } = new PlayerState();

        public CharacterDefinition Character { get; private set; }
        public RunConfig Config { get; private set; }
        public Health Health { get; private set; }
        public PlayerMotor Motor { get; private set; }
        public PlayerAim Aim { get; private set; }
        public PlayerLoadout Loadout { get; private set; }
        public IPlayerInputSource Input { get; private set; }

        public int PlayerIndex => State.playerIndex;
        public bool IsDead => State.isDead;

        /// <summary>Fired once when this player dies.</summary>
        public event Action<PlayerController> Died;

        // ---- IWeaponOwner (what weapons need to know) ----
        public RuntimeStats Stats => State.stats;
        public Team Team => Team.Player;
        public Vector3 Position => transform.position;
        // Smoothed feet point (moves every rendered frame). Visuals that should stick to the knight
        // (the sword slash) parent to this instead of the 50 Hz root, or they would stutter while walking.
        public Transform VisualAnchor => cameraTarget != null ? cameraTarget : transform;
        public Vector3 AimDirection => Aim != null ? Aim.AimDirection : transform.forward;
        public GameObject OwnerObject => gameObject;
        public bool CanAttack => !State.isDead;

        private VisualBinder binder;
        private MaterialPropertyBlock hurtBlock;
        private float hurtTimer;
        private Color hurtColor = new Color32(0xE2, 0x3A, 0x3A, 0xFF);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            Health = GetComponent<Health>();
            Motor = GetComponent<PlayerMotor>();
            Aim = GetComponent<PlayerAim>();
            Loadout = GetComponent<PlayerLoadout>();
            binder = GetComponent<VisualBinder>();
            Input = GetComponent<IPlayerInputSource>();

            Health.team = Team.Player;
            Health.Damaged += OnDamaged;
            Health.Died += OnHealthDied;

            EnsureSmoothedCameraTarget();
        }

        /// <summary>
        /// Makes sure the camera (and the slash visuals) follow a SMOOTHED child, whatever the prefab says.
        /// Done at runtime on purpose: an old prefab, a missing reference or a scene that wasn't rebuilt
        /// must never be able to make the camera read the 50 Hz root again.
        ///   - no TickInterpolator?            add one
        ///   - no cameraTarget, or it IS the root? create a "CameraTarget" child at the feet
        ///   - cameraTarget not smoothed?       register it with the TickInterpolator
        /// Returns true if it had to repair something (missing CameraTarget / not on the interpolator).
        /// </summary>
        public bool EnsureSmoothedCameraTarget()
        {
            bool repaired = false;

            var interpolator = GetComponent<TickInterpolator>();
            if (interpolator == null)
            {
                interpolator = gameObject.AddComponent<TickInterpolator>();
                repaired = true;
            }

            // The model should be smoothed too (otherwise it steps at 50 Hz while the camera glides).
            if (binder != null && binder.visualRoot != null && binder.visualRoot.parent == transform &&
                !interpolator.IsSmoothed(binder.visualRoot))
            {
                interpolator.AddSmoothedChild(binder.visualRoot);
                repaired = true;
            }

            // The camera target must be a DIRECT child (so TickInterpolator can smooth it) and not the root.
            if (cameraTarget == null || cameraTarget == transform || cameraTarget.parent != transform)
            {
                var existing = transform.Find("CameraTarget");
                if (existing == null)
                {
                    existing = new GameObject("CameraTarget").transform;
                    existing.SetParent(transform, false);
                    existing.localPosition = Vector3.zero;
                    existing.localRotation = Quaternion.identity;
                }
                cameraTarget = existing;
                repaired = true;
            }

            if (!interpolator.IsSmoothed(cameraTarget))
            {
                interpolator.AddSmoothedChild(cameraTarget);
                repaired = true;
            }

            // The camera target follows the smoothed position but NEVER rotates (only the model turns).
            // Not counted as a "repair": it's how the target is always meant to behave.
            interpolator.SetPositionOnly(cameraTarget);

            if (repaired)
            {
                Debug.LogWarning($"[PlayerController] '{name}': camera target was not a smoothed child, repaired at runtime " +
                                 "(re-run Tools > Knight Survivors > Build Graybox Scene to fix the prefab).", this);
            }
            return repaired;
        }

        private void OnDestroy()
        {
            Health.Damaged -= OnDamaged;
            Health.Died -= OnHealthDied;
            PlayerRegistry.Unregister(this);
        }

        /// <summary>
        /// Called once by the PlayerSpawner right after the player is created.
        /// Builds the stats from the CharacterDefinition, gives the starting weapon, picks team colors,
        /// and adds the player to the PlayerRegistry.
        /// </summary>
        public void Setup(CharacterDefinition character, int playerIndex, RunConfig config)
        {
            Character = character;
            Config = config;

            State = new PlayerState
            {
                playerIndex = playerIndex,
                characterId = character != null ? character.id : "unknown",
            };

            if (config != null && config.feedbackSettings != null) hurtColor = config.feedbackSettings.playerHurtFlashColor;
            if (config != null && config.hitFeedbackProfile != null &&
                config.hitFeedbackProfile.TryGet(HitType.PlayerHurt, out var hurtEntry) && hurtEntry.flashMs > 0)
            {
                hurtFlashSeconds = hurtEntry.flashMs / 1000f;
            }

            Loadout.Initialize(this, character);
            State.rerollsLeft = State.stats.rerolls;
            State.banishesLeft = State.stats.banishes;
            Health.ResetHealth(State.stats.maxHealth);
            Health.armor = State.stats.armor;
            State.currentHealth = Health.CurrentHealth;

            Aim.Initialize(this, config != null ? config.defaultAimMode : AimMode.Assisted);

            var tint = GetComponent<KnightTint>();
            if (tint != null) tint.Apply(playerIndex);
            if (binder != null) binder.RefreshMaterialCache(); // remember the team colors as "normal"

            gameObject.name = $"Player{playerIndex + 1}_{State.characterId}";
            PlayerRegistry.Register(this);
        }

        /// <summary>Called by PlayerLoadout after stats change (level-ups), so health and armor follow.</summary>
        public void OnStatsRecalculated()
        {
            Health.SetMaxHealth(State.stats.maxHealth);
            Health.armor = State.stats.armor;
        }

        private void FixedUpdate()
        {
            if (State.isDead) return;

            // Recovery = HP regenerated per second (GDD 3.1).
            if (State.stats.recovery > 0f) Health.Heal(State.stats.recovery * Time.fixedDeltaTime);
            State.currentHealth = Health.CurrentHealth;

            // Dodge roll and pause menu are deferred; consume presses so they don't pile up.
            Input?.ConsumeButtons();
        }

        private void Update()
        {
            // Red "hurt" tint (the knight is the one object allowed a MaterialPropertyBlock; PlaceholderSpec 3.7).
            if (hurtTimer > 0f)
            {
                hurtTimer -= Time.deltaTime;
                if (hurtTimer <= 0f) SetHurtTint(false);
            }
        }

        private void OnDamaged(Health health, DamageInfo info)
        {
            State.currentHealth = health.CurrentHealth;
            if (hurtTimer <= 0f) SetHurtTint(true);
            hurtTimer = hurtFlashSeconds;
        }

        private void SetHurtTint(bool on)
        {
            if (binder == null || binder.bodyRenderers == null) return;
            if (hurtBlock == null) hurtBlock = new MaterialPropertyBlock();
            hurtBlock.Clear();
            if (on) hurtBlock.SetColor(BaseColorId, hurtColor);
            for (int i = 0; i < binder.bodyRenderers.Length; i++)
            {
                var r = binder.bodyRenderers[i];
                if (r == null) continue;
                if (on) r.SetPropertyBlock(hurtBlock);
                else r.SetPropertyBlock(null);
            }
        }

        private void OnHealthDied(Health health, DamageInfo info)
        {
            if (State.isDead) return;
            State.isDead = true;
            State.currentHealth = 0f;
            SetHurtTint(false);

            // Graybox "death": tip the knight over. A real death animation comes with the art pass.
            if (binder != null && binder.visualRoot != null)
            {
                binder.visualRoot.localRotation = Quaternion.Euler(90f, 0f, 0f) * binder.visualRoot.localRotation;
            }

            Died?.Invoke(this);
            PlayerRegistry.NotifyDied(this);
        }
    }
}
