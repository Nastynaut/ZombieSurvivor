using System.Collections.Generic;
using System.IO;
using KnightSurvivors.Core;
using KnightSurvivors.Data;
using KnightSurvivors.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KnightSurvivors.EditorTools
{
    /// <summary>
    /// Creates the ScriptableObject data assets (characters, weapons, enemies, spawn schedule, configs) with the
    /// numbers from docs/GDD.md v0.3, plus the Input Actions asset.
    ///
    /// Beginner note: a ScriptableObject asset is a data file you can edit in the Inspector. Code reads numbers
    /// from these files instead of hard-coding them, so balancing never needs a code change. Running the builder
    /// again RESETS these files to the GDD numbers (keeping their file IDs, so references don't break).
    /// </summary>
    internal static class GameDataFactory
    {
        public const string DataDir = "Assets/_Project/Data";
        public const string InputPath = "Assets/_Project/Input/KnightSurvivors.inputactions";

        public class Assets
        {
            public RunConfig runConfig;
            public CharacterDefinition knight;
            public WeaponDefinition longsword;
            public EnemyDefinition shambler, runner;
            public PoolPrewarmConfig poolConfig;
        }

        /// <summary>Load the asset at the path, or create it. Existing assets are reused so other files' links survive.</summary>
        private static T Asset<T>(string folder, string name) where T : ScriptableObject
        {
            string dir = $"{DataDir}/{folder}";
            PlaceholderFactory.EnsureFolder(dir);
            string path = $"{dir}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            EditorUtility.SetDirty(asset);
            return asset;
        }

        public static Assets CreateAll(GameObject knightPrefab, GameObject longswordPrefab,
            GameObject shamblerPrefab, GameObject runnerPrefab, GameObject[] gemPrefabs)
        {
            var a = new Assets();

            // ---------------- Weapon: Longsword (GDD 3.2.1). 8 levels: damage / cooldown / area / amount / knockback.
            var sword = Asset<WeaponDefinition>("Weapons", "WPN_Longsword");
            sword.id = "longsword";
            sword.displayName = "Longsword";
            sword.archetype = WeaponArchetype.MeleeArc;
            sword.targetingMode = TargetingMode.AimDirection;
            sword.damageType = DamageType.Physical;
            sword.arcDegrees = 120f;
            sword.maxLevel = 8;
            sword.isEvolution = false;
            sword.prefab = longswordPrefab;
            sword.levels = new List<WeaponLevel>
            {
                Lvl(1, 12, 1.2f, 2.8f, 1, 6, "Slash in front"),
                Lvl(2, 15, 1.2f, 2.8f, 1, 6, "+3 damage"),
                Lvl(3, 15, 1.1f, 3.1f, 1, 6, "Faster, wider"),
                Lvl(4, 19, 1.1f, 3.1f, 2, 6, "Rear slash"),
                Lvl(5, 19, 1.0f, 3.4f, 2, 7, "Faster, wider"),
                Lvl(6, 24, 1.0f, 3.4f, 2, 7, "+5 damage"),
                Lvl(7, 24, 0.9f, 3.7f, 2, 7, "Faster, wider"),
                Lvl(8, 30, 0.9f, 3.7f, 3, 8, "Side slashes (max, can evolve)"),
            };
            a.longsword = sword;
            // Crossbow and Holy Flail (GDD 3.2.1) are NOT created yet: they have no weapon script, and a card for
            // a weapon that does nothing would confuse playtests. They're on the deferred list in the README.

            // ---------------- Passives in the slice (GDD 3.3), 5 levels each.
            var whetstone = Passive("PSV_Whetstone", "whetstone", "Whetstone", StatType.Might, ModifierOp.PercentAdd, 0.10f);
            var beads = Passive("PSV_PrayerBeads", "prayer_beads", "Prayer Beads", StatType.Cooldown, ModifierOp.PercentAdd, -0.06f);
            var plate = Passive("PSV_PlateArmor", "plate_armor", "Plate Armor", StatType.Armor, ModifierOp.Add, 1f);

            // ---------------- Player: Sir Aldric (GDD 3.1). PlayerStats' default values ARE the GDD numbers.
            var stats = Asset<PlayerStats>("Characters", "STATS_SirAldric");
            var knight = Asset<CharacterDefinition>("Characters", "CHR_SirAldric");
            knight.id = "sir_aldric";
            knight.displayName = "Sir Aldric";
            knight.prefab = knightPrefab;
            // Art reference lives on the ScriptableObject (project rule). Builder also loads this path
            // via AssetDatabase when wiring Visual/ModelOffset; null → primitive placeholder.
            knight.visualModel = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderFactory.SirAldricFbxPath);
            knight.baseStats = stats;
            knight.startingWeapon = sword;
            knight.unlockedByDefault = true;
            a.knight = knight;

            // ---------------- Enemies (GDD 3.5). Bloater / Spitter / Grave Knight / Rotting King come later.
            var shambler = Asset<EnemyDefinition>("Enemies", "ENM_Shambler");
            Enemy(shambler, "shambler", "Shambler", hp: 10, speed: 1.6f, contact: 6, xp: 1, mass: 1.0f, radius: 0.3f, prewarm: 400, firstMinute: 0);
            shambler.prefab = shamblerPrefab;
            var runner = Asset<EnemyDefinition>("Enemies", "ENM_Runner");
            Enemy(runner, "runner", "Runner", hp: 6, speed: 3.8f, contact: 4, xp: 1, mass: 0.7f, radius: 0.2f, prewarm: 200, firstMinute: 2);
            runner.prefab = runnerPrefab;
            a.shambler = shambler;
            a.runner = runner;

            // ---------------- Configs. Their default field values are already the GDD numbers.
            var coop = Asset<CoopScaling>("Config", "CFG_CoopScaling");
            var xp = Asset<XpCurve>("Config", "CFG_XpCurve");
            var rules = Asset<LevelUpRules>("Config", "CFG_LevelUpRules");
            var feedback = Asset<FeedbackSettings>("Config", "CFG_FeedbackSettings");
            var hitProfile = Asset<HitFeedbackProfile>("Config", "CFG_HitFeedback");
            FillHitFeedback(hitProfile);

            // ---------------- Spawn schedule (GDD 5). Bloater + Spitter shares are folded into Shambler for now.
            var schedule = Asset<SpawnSchedule>("Config", "SPN_Slice");
            schedule.runLengthMinutes = 10f;
            schedule.bossMinute = 10f;
            schedule.boss = null; // Rotting King deferred
            schedule.hardCap = 350;
            schedule.ringMinDistance = 20f;
            schedule.ringMaxDistance = 24f;
            schedule.recycleDistance = 35f;
            schedule.coopScaling = coop;
            schedule.events = new List<WaveEvent>(); // swarms / elites / chests deferred
            //              minute, targetAlive, spawns per sec, runner %
            schedule.entries = new List<WaveEntry>
            {
                Row(0, 15, 1.0f, 0, shambler, runner),
                Row(1, 25, 1.5f, 0, shambler, runner),
                Row(2, 35, 2.0f, 20, shambler, runner),
                Row(3, 50, 2.5f, 25, shambler, runner),
                Row(4, 60, 3.0f, 25, shambler, runner),
                Row(5, 70, 3.5f, 25, shambler, runner),
                Row(6, 85, 4.0f, 25, shambler, runner),
                Row(7, 100, 4.5f, 25, shambler, runner),
                Row(8, 120, 5.0f, 25, shambler, runner),
                Row(9, 140, 5.5f, 30, shambler, runner),
            };

            // ---------------- Run config: the one asset the scene's managers point at.
            var run = Asset<RunConfig>("Config", "CFG_RunConfig");
            run.defaultCharacter = knight;
            run.defaultAimMode = AimMode.Assisted;
            run.spawnSchedule = schedule;
            run.xpCurve = xp;
            run.levelUpRules = rules;
            run.weaponPool = new List<WeaponDefinition> { sword };
            run.passivePool = new List<PassiveDefinition> { whetstone, beads, plate };
            run.feedbackSettings = feedback;
            run.hitFeedbackProfile = hitProfile;
            a.runConfig = run;

            // ---------------- Pool prewarm for things no manager prewarms itself: XP gems 400 / 64 / 32 (GDD 7.8).
            var pools = Asset<PoolPrewarmConfig>("Config", "CFG_PoolPrewarm");
            int[] gemCounts = { 400, 64, 32 };
            pools.entries = new List<PoolPrewarmEntry>();
            for (int i = 0; i < gemPrefabs.Length; i++)
                pools.entries.Add(new PoolPrewarmEntry { prefab = gemPrefabs[i], prewarmCount = gemCounts[i] });
            a.poolConfig = pools;

            AssetDatabase.SaveAssets();
            return a;
        }

        private static WeaponLevel Lvl(int level, float damage, float cooldown, float area, int amount, float knockback, string description)
        {
            return new WeaponLevel
            {
                level = level, damage = damage, cooldown = cooldown, area = area, amount = amount,
                pierce = -1, // -1 = hits everything in the arc
                duration = 0.2f, knockback = knockback, description = description
            };
        }

        private static PassiveDefinition Passive(string file, string id, string displayName, StatType stat, ModifierOp op, float value)
        {
            var p = Asset<PassiveDefinition>("Passives", file);
            p.id = id;
            p.displayName = displayName;
            p.stat = stat;
            p.op = op;
            p.valuePerLevel = value;
            p.maxLevel = 5;
            return p;
        }

        private static void Enemy(EnemyDefinition e, string id, string displayName, float hp, float speed, float contact,
            int xp, float mass, float radius, int prewarm, float firstMinute)
        {
            e.id = id;
            e.displayName = displayName;
            e.tier = EnemyTier.Normal;
            e.behavior = EnemyBehavior.Chase;
            e.maxHealth = hp;
            e.hpPerMinute = 0.25f;     // +25% HP per minute
            e.moveSpeed = speed;
            e.contactDamage = contact;
            e.damagePerMinute = 0.05f; // +5% damage per minute
            e.xpValue = xp;
            e.mass = mass;
            e.knockbackResist = 0f;
            e.radius = radius;
            e.prewarmCount = prewarm;
            e.firstMinute = firstMinute;
        }

        private static WaveEntry Row(int minute, int targetAlive, float rate, float runnerPct, EnemyDefinition shambler, EnemyDefinition runner)
        {
            var row = new WaveEntry { minute = minute, targetAlive = targetAlive, spawnRatePerSec = rate, mix = new List<EnemyWeight>() };
            row.mix.Add(new EnemyWeight { enemy = shambler, weightPct = 100f - runnerPct });
            if (runnerPct > 0f) row.mix.Add(new EnemyWeight { enemy = runner, weightPct = runnerPct });
            return row;
        }

        /// <summary>GDD 7.1 hit-stop / shake / flash table, plus the 7.4 blood counts for the death types.</summary>
        private static void FillHitFeedback(HitFeedbackProfile p)
        {
            p.entries = new List<HitFeedbackEntry>
            {
                Hit(HitType.Normal, 40, 0, 0f, 0f, 80, 0, 0),
                Hit(HitType.Crit, 60, 0, 0.05f, 0.10f, 100, 0, 0),
                Hit(HitType.Overkill, 0, 20, 0.03f, 0.08f, 0, 30, 1),
                Hit(HitType.EliteHit, 50, 0, 0.08f, 0.10f, 80, 0, 0),
                Hit(HitType.EliteKill, 0, 120, 0.40f, 0.35f, 0, 80, 3),
                Hit(HitType.BossHit, 30, 0, 0f, 0f, 60, 0, 0),
                Hit(HitType.BossPhaseChange, 0, 200, 0.50f, 0.50f, 0, 0, 0),
                Hit(HitType.BossKill, 0, 300, 0.60f, 0.60f, 0, 80, 3),
                Hit(HitType.PlayerHurt, 0, 60, 0.25f, 0.20f, 120, 0, 0),
                Hit(HitType.Explosion, 0, 40, 0.30f, 0.25f, 0, 40, 1),
            };
        }

        private static HitFeedbackEntry Hit(HitType type, int localMs, int globalMs, float shake, float shakeTime, int flashMs, int blood, int decals)
        {
            return new HitFeedbackEntry
            {
                hitType = type, localStopMs = localMs, globalStopMs = globalMs, shakeAmplitude = shake,
                shakeDuration = shakeTime, flashMs = flashMs, bloodParticles = blood, decalCount = decals
            };
        }

        // ------------------------------------------------------------------ input actions (GDD 2.4)

        /// <summary>
        /// Build the "KnightSurvivors.inputactions" file in code and import it. You can open it afterwards in the
        /// Input Actions editor (double-click) to rebind keys. If this file is ever missing, PlayerInputHandler
        /// builds the same actions in code as a fallback, so the game still plays.
        /// </summary>
        public static InputActionAsset CreateInputActions()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "KnightSurvivors";

            // Control schemes let PlayerInput tell keyboard players from gamepad players (needed for couch co-op).
            asset.AddControlScheme("Keyboard&Mouse").WithRequiredDevice("<Keyboard>").WithOptionalDevice("<Mouse>");
            asset.AddControlScheme("Gamepad").WithRequiredDevice("<Gamepad>");
            const string kbm = "Keyboard&Mouse";
            const string pad = "Gamepad";

            var map = asset.AddActionMap("Gameplay");

            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", kbm).With("Down", "<Keyboard>/s", kbm)
                .With("Left", "<Keyboard>/a", kbm).With("Right", "<Keyboard>/d", kbm);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", kbm).With("Down", "<Keyboard>/downArrow", kbm)
                .With("Left", "<Keyboard>/leftArrow", kbm).With("Right", "<Keyboard>/rightArrow", kbm);
            move.AddBinding("<Gamepad>/leftStick", groups: pad);

            // Right stick aim, with the GDD's 0.25 deadzone.
            var aim = map.AddAction("Aim", InputActionType.Value, expectedControlLayout: "Vector2");
            aim.AddBinding("<Gamepad>/rightStick", processors: "StickDeadzone(min=0.25)", groups: pad);

            // Mouse position (PlayerAim raycasts it onto the ground plane).
            var point = map.AddAction("Point", InputActionType.Value, expectedControlLayout: "Vector2");
            point.AddBinding("<Mouse>/position", groups: kbm);

            var dodge = map.AddAction("Dodge", InputActionType.Button);
            dodge.AddBinding("<Keyboard>/space", groups: kbm);
            dodge.AddBinding("<Keyboard>/leftShift", groups: kbm);
            dodge.AddBinding("<Gamepad>/buttonSouth", groups: pad);
            dodge.AddBinding("<Gamepad>/rightShoulder", groups: pad);

            var pause = map.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape", groups: kbm);
            pause.AddBinding("<Gamepad>/start", groups: pad);

            var stats = map.AddAction("Stats", InputActionType.Button);
            stats.AddBinding("<Keyboard>/tab", groups: kbm);
            stats.AddBinding("<Gamepad>/select", groups: pad);

            // The UI map is left to InputSystemUIInputModule's built-in defaults for now (menus are code-built).

            PlaceholderFactory.EnsureFolder(Path.GetDirectoryName(InputPath).Replace('\\', '/'));
            File.WriteAllText(InputPath, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        }
    }
}
