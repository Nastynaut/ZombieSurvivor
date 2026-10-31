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
    /// Creates the ScriptableObject data assets (characters, weapons, enemies, spawn schedules, configs)
    /// with the numbers from docs/DataTables.md (GDD v0.4 fill sheet), plus the Input Actions asset.
    ///
    /// CreateAll builds the FULL 15-minute catalog (weapons, passives, recipes, meta, enemies, attacks,
    /// SPN_FullRun) AND keeps the ten-minute slice (SPN_Slice + CFG_RunConfig slice wiring) alone for
    /// graybox playtests. Running the builder again RESETS these files to the table numbers (keeping
    /// their file IDs, so references don't break).
    ///
    /// Beginner note: a ScriptableObject asset is a data file you can edit in the Inspector. Code reads
    /// numbers from these files instead of hard-coding them, so balancing never needs a code change.
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

        /// <summary>
        /// Creates/loads CHR_SirAldric and wires <see cref="CharacterDefinition.visualModel"/> + yaw
        /// BEFORE <see cref="PlaceholderFactory.BuildKnight"/> so the attach path has no hardcoded FBX string.
        /// Called from GrayboxSceneBuilder ahead of prefab build. CreateAll reasserts the same defaults later.
        /// </summary>
        public static CharacterDefinition EnsureSirAldricArtRefs()
        {
            // First import creates Unity's default .meta (we never hand-write FBX import settings).
            if (System.IO.File.Exists(System.IO.Path.Combine(
                    Application.dataPath, "Art/Characters/SirAldric/SirAldric.fbx")))
            {
                AssetDatabase.ImportAsset(PlaceholderFactory.SirAldricFbxPath,
                    ImportAssetOptions.ForceSynchronousImport);
            }

            var knight = Asset<CharacterDefinition>("Characters", "CHR_SirAldric");
            knight.id = "sir_aldric";
            knight.displayName = "Sir Aldric";
            knight.visualModel = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderFactory.SirAldricFbxPath);
            // Corrected default: +90° yaw after Rx(-90). File front -X → Unity +Z.
            // If Play shows 180° wrong, change this (or ModelOffsetYaw on the prefab) by ±180.
            knight.visualModelYawOffset = 90f;
            EditorUtility.SetDirty(knight);
            AssetDatabase.SaveAssets();
            return knight;
        }

        /// <summary>
        /// Build / refresh every data asset from docs/DataTables.md.
        /// Leaves SPN_Slice and CFG_RunConfig's slice wiring (10-minute schedule, Longsword + 3 passives)
        /// intact for the graybox. Also creates SPN_FullRun and the full weapon / passive / enemy / meta catalog.
        /// </summary>
        public static Assets CreateAll(GameObject knightPrefab, GameObject longswordPrefab,
            GameObject shamblerPrefab, GameObject runnerPrefab, GameObject[] gemPrefabs)
        {
            var a = new Assets();

            // ==================================================================== WEAPONS (DataTables §1)
            var longsword = BuildLongsword(longswordPrefab);
            var crossbow = BuildCrossbow();
            var holyFlail = BuildHolyFlail();
            var throwingAxes = BuildThrowingAxes();
            var fireFlask = BuildFireFlask();
            var consecrated = BuildConsecratedGround();
            var greatsword = BuildGreatsword();
            var ballista = BuildBallista();
            var cleavers = BuildCleavers();
            var halo = BuildHalo();
            a.longsword = longsword;

            // ==================================================================== PASSIVES (DataTables §2.1)
            var whetstone = Passive("PSV_Whetstone", "whetstone", "Whetstone", StatType.Might, ModifierOp.PercentAdd, 0.10f);
            var plate = Passive("PSV_PlateArmor", "plate_armor", "Plate Armor", StatType.Armor, ModifierOp.Add, 1f);
            var beads = Passive("PSV_PrayerBeads", "prayer_beads", "Prayer Beads", StatType.Cooldown, ModifierOp.PercentAdd, -0.06f);
            var banner = Passive("PSV_WarBanner", "war_banner", "War Banner", StatType.Area, ModifierOp.PercentAdd, 0.10f);
            var spurs = Passive("PSV_Spurs", "spurs", "Knight's Spurs", StatType.MoveSpeed, ModifierOp.PercentAdd, 0.07f);
            var lodestone = Passive("PSV_Lodestone", "lodestone", "Lodestone", StatType.PickupRadius, ModifierOp.PercentAdd, 0.25f);

            // ==================================================================== EVOLUTIONS (DataTables §2.2)
            Recipe("EVO_Greatsword", "evo_greatsword", longsword, whetstone, greatsword);
            Recipe("EVO_Ballista", "evo_ballista", crossbow, beads, ballista);
            Recipe("EVO_Cleavers", "evo_cleavers", throwingAxes, banner, cleavers);
            Recipe("EVO_Censer", "evo_censer", holyFlail, plate, halo);

            // ==================================================================== META (DataTables §2.4)
            Meta("META_Might", "meta_might", "Might", StatType.Might, ModifierOp.PercentAdd, 0.05f, 5, 100);
            Meta("META_Armor", "meta_armor", "Armor", StatType.Armor, ModifierOp.Add, 1f, 3, 150);
            Meta("META_MaxHp", "meta_maxhp", "Max Health", StatType.MaxHealth, ModifierOp.PercentAdd, 0.10f, 3, 120);
            Meta("META_Recovery", "meta_recovery", "Recovery", StatType.Recovery, ModifierOp.Add, 0.2f, 5, 100);
            Meta("META_Cooldown", "meta_cooldown", "Cooldown", StatType.Cooldown, ModifierOp.PercentAdd, -0.025f, 2, 300);
            Meta("META_Area", "meta_area", "Area", StatType.Area, ModifierOp.PercentAdd, 0.05f, 2, 200);
            Meta("META_Speed", "meta_speed", "Move Speed", StatType.MoveSpeed, ModifierOp.PercentAdd, 0.05f, 2, 150);
            Meta("META_Magnet", "meta_magnet", "Magnet", StatType.PickupRadius, ModifierOp.PercentAdd, 0.15f, 2, 100);
            Meta("META_Growth", "meta_growth", "Growth", StatType.Growth, ModifierOp.PercentAdd, 0.03f, 5, 120);
            Meta("META_Greed", "meta_greed", "Greed", StatType.Greed, ModifierOp.PercentAdd, 0.10f, 5, 100);
            Meta("META_Luck", "meta_luck", "Luck", StatType.Luck, ModifierOp.PercentAdd, 0.05f, 3, 150);
            var metaRevival = Meta("META_Revival", "meta_revival", "Revival", StatType.Revivals, ModifierOp.Add, 1f, 1, 1000);
            Meta("META_Reroll", "meta_reroll", "Reroll", StatType.Rerolls, ModifierOp.Add, 1f, 3, 250);
            Meta("META_Banish", "meta_banish", "Banish", StatType.Banishes, ModifierOp.Add, 1f, 3, 200);
            Meta("META_Skip", "meta_skip", "Skip", StatType.Skips, ModifierOp.Add, 1f, 2, 200);
            var metaAmount = Meta("META_Amount", "meta_amount", "Amount", StatType.Amount, ModifierOp.Add, 1f, 1, 2500);

            // ==================================================================== UNLOCKS (DataTables §1.1 / §2.1 / §2.4)
            Unlock("UNL_Flask", "unl_flask", "Unlock Fire Oil Flask", UnlockConditionType.KillTotal, 1500f, fireFlask, null, null);
            Unlock("UNL_Consecrated", "unl_consecrated", "Unlock Consecrated Ground", UnlockConditionType.SurviveMinutes, 10f, consecrated, null, null);
            Unlock("UNL_Spurs", "unl_spurs", "Unlock Knight's Spurs", UnlockConditionType.ReachLevel, 20f, null, spurs, null);
            Unlock("UNL_Lodestone", "unl_lodestone", "Unlock Lodestone", UnlockConditionType.KillTotal, 500f, null, lodestone, null);
            Unlock("UNL_MetaRevival", "unl_meta_revival", "Unlock Revival", UnlockConditionType.ReachLevel, 30f, null, null, metaRevival);
            Unlock("UNL_MetaAmount", "unl_meta_amount", "Unlock Amount", UnlockConditionType.EvolveWeapon, 1f, null, null, metaAmount);

            // ==================================================================== PLAYER (GDD 3.1)
            var stats = Asset<PlayerStats>("Characters", "STATS_SirAldric");
            // Class defaults already match Sir Aldric; write skips explicitly so a rebuilt asset is correct.
            stats.skips = 3;
            var knight = Asset<CharacterDefinition>("Characters", "CHR_SirAldric");
            knight.id = "sir_aldric";
            knight.displayName = "Sir Aldric";
            knight.prefab = knightPrefab;
            // Art reference lives on the ScriptableObject (project rule). Builder also loads this path
            // via AssetDatabase when wiring Visual/ModelOffset; null → primitive placeholder.
            // Builder may already have filled these via EnsureSirAldricArtRefs; CreateAll reasserts defaults.
            knight.visualModel = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderFactory.SirAldricFbxPath);
            // 90° after Z-up fix: Tripo file front is -X → Unity +Z. See ModelOffsetYaw / PlaceholderFactory comments.
            knight.visualModelYawOffset = 90f;
            knight.baseStats = stats;
            knight.startingWeapon = longsword;
            knight.unlockedByDefault = true;
            a.knight = knight;

            // ==================================================================== ENEMIES (DataTables §3)
            var shambler = Asset<EnemyDefinition>("Enemies", "ENM_Shambler");
            FillEnemy(shambler, "shambler", "Shambler", EnemyTier.Normal, EnemyBehavior.Chase,
                hp: 10, hpPerMin: 0.25f, speed: 1.6f, contact: 6, dmgPerMin: 0.05f,
                xp: 1, mass: 1.0f, kbResist: 0f, radius: 0.3f, prewarm: 400,
                keepMin: 0f, keepMax: 0f, firstMinute: 0f, crowns: 0.05f, dropsChest: false);
            shambler.prefab = shamblerPrefab;
            shambler.attackParams = new List<EnemyAttack>();

            var runner = Asset<EnemyDefinition>("Enemies", "ENM_Runner");
            FillEnemy(runner, "runner", "Runner", EnemyTier.Normal, EnemyBehavior.Chase,
                hp: 6, hpPerMin: 0.25f, speed: 3.8f, contact: 4, dmgPerMin: 0.05f,
                xp: 1, mass: 0.7f, kbResist: 0f, radius: 0.2f, prewarm: 200,
                keepMin: 0f, keepMax: 0f, firstMinute: 2f, crowns: 0.05f, dropsChest: false);
            runner.prefab = runnerPrefab;
            runner.attackParams = new List<EnemyAttack>();

            var bloater = Asset<EnemyDefinition>("Enemies", "ENM_Bloater");
            FillEnemy(bloater, "bloater", "Bloater", EnemyTier.Normal, EnemyBehavior.ExplodeOnDeath,
                hp: 40, hpPerMin: 0.25f, speed: 1.3f, contact: 8, dmgPerMin: 0.05f,
                xp: 3, mass: 2.5f, kbResist: 0.4f, radius: 0.7f, prewarm: 60,
                keepMin: 0f, keepMax: 0f, firstMinute: 4f, crowns: 0.05f, dropsChest: false);
            bloater.prefab = null; // prefab deferred until behaviour exists
            bloater.attackParams = new List<EnemyAttack>
            {
                Atk("Death Blast", telegraph: 0.8f, radius: 2.2f, range: 0f, width: 0f, arc: 0f,
                    angles: null, damage: 15f, dmgEnemies: 50f, knockback: 0f, interval: 0f,
                    phaseInterval: 0f, projSpeed: 0f, summon: null, summonCount: 0, hpThreshold: 1f),
            };

            var spitter = Asset<EnemyDefinition>("Enemies", "ENM_Spitter");
            FillEnemy(spitter, "spitter", "Plague Spitter", EnemyTier.Normal, EnemyBehavior.RangedKite,
                hp: 20, hpPerMin: 0.25f, speed: 2.0f, contact: 5, dmgPerMin: 0.05f,
                xp: 2, mass: 1.0f, kbResist: 0.1f, radius: 0.25f, prewarm: 60,
                keepMin: 7f, keepMax: 9f, firstMinute: 6f, crowns: 0.05f, dropsChest: false);
            spitter.prefab = null;
            spitter.attackParams = new List<EnemyAttack>
            {
                Atk("Spit", telegraph: 0.5f, radius: 0f, range: 9f, width: 0.3f, arc: 0f,
                    angles: null, damage: 8f, dmgEnemies: 0f, knockback: 0f, interval: 3.5f,
                    phaseInterval: 0f, projSpeed: 8f, summon: null, summonCount: 0, hpThreshold: 1f),
            };

            var graveKnight = Asset<EnemyDefinition>("Enemies", "ENM_GraveKnight");
            FillEnemy(graveKnight, "grave_knight", "Grave Knight", EnemyTier.Elite, EnemyBehavior.ChargeElite,
                hp: 300, hpPerMin: 0.25f, speed: 2.6f, contact: 20, dmgPerMin: 0.05f,
                xp: 30, mass: 6.0f, kbResist: 0.9f, radius: 0.7f, prewarm: 3,
                keepMin: 0f, keepMax: 0f, firstMinute: 5f, crowns: 15f, dropsChest: true);
            graveKnight.prefab = null;
            graveKnight.attackParams = new List<EnemyAttack>
            {
                // damage 0 †: GDD gives no separate charge hit; contact damage still applies.
                Atk("Charge", telegraph: 0.8f, radius: 0f, range: 8f, width: 1.4f, arc: 0f,
                    angles: null, damage: 0f, dmgEnemies: 0f, knockback: 0f, interval: 6f,
                    phaseInterval: 0f, projSpeed: 10f, summon: null, summonCount: 0, hpThreshold: 1f),
            };

            var king = Asset<EnemyDefinition>("Enemies", "ENM_RottingKing");
            FillEnemy(king, "rotting_king", "The Rotting King", EnemyTier.Boss, EnemyBehavior.Boss,
                hp: 18000, hpPerMin: 0f, speed: 2.2f, contact: 30, dmgPerMin: 0f,
                xp: 0, mass: 50f, kbResist: 1.0f, radius: 1.5f, prewarm: 1,
                keepMin: 0f, keepMax: 0f, firstMinute: 15f, crowns: 0f, dropsChest: true);
            king.prefab = null;
            king.phase2HpFraction = 0.5f;
            king.phase2MoveSpeed = 2.8f;
            king.enrageDamageMult = 2f;
            king.enrageSpeedMult = 1.3f;
            king.attackParams = new List<EnemyAttack>
            {
                Atk("Ground Slam", telegraph: 1.2f, radius: 4f, range: 0f, width: 0f, arc: 0f,
                    angles: null, damage: 35f, dmgEnemies: 0f, knockback: 6f, interval: 8f,
                    phaseInterval: 6f, projSpeed: 0f, summon: null, summonCount: 0, hpThreshold: 1f),
                Atk("Raise the Dead", telegraph: 1.0f, radius: 6f, range: 0f, width: 0f, arc: 0f,
                    angles: null, damage: 0f, dmgEnemies: 0f, knockback: 0f, interval: 15f,
                    phaseInterval: 0f, projSpeed: 0f, summon: shambler, summonCount: 12, hpThreshold: 1f),
                Atk("Bile Wave", telegraph: 0.8f, radius: 0f, range: 10f, width: 0f, arc: 30f,
                    angles: new[] { -40f, 0f, 40f }, damage: 20f, dmgEnemies: 0f, knockback: 0f, interval: 10f,
                    phaseInterval: 0f, projSpeed: 0f, summon: null, summonCount: 0, hpThreshold: 0.5f),
            };

            a.shambler = shambler;
            a.runner = runner;

            // ==================================================================== CONFIGS
            var coop = Asset<CoopScaling>("Config", "CFG_CoopScaling");
            var xp = Asset<XpCurve>("Config", "CFG_XpCurve");
            var rules = Asset<LevelUpRules>("Config", "CFG_LevelUpRules");
            // Class defaults already match DataTables §2.3; write the new / fixed fields explicitly.
            rules.chestSpinSeconds = 1.5f;
            rules.rerollPerChest = 1;
            rules.fallbackCards = new List<PickupType> { PickupType.CoinPouch, PickupType.RoastChicken };
            var feedback = Asset<FeedbackSettings>("Config", "CFG_FeedbackSettings");
            var hitProfile = Asset<HitFeedbackProfile>("Config", "CFG_HitFeedback");
            FillHitFeedback(hitProfile);

            // ==================================================================== SPN_Slice (leave alone — §8.1 ten-minute slice)
            // Bloater + Spitter shares are folded into Shambler; no boss; events empty.
            var slice = Asset<SpawnSchedule>("Config", "SPN_Slice");
            slice.runLengthMinutes = 10f;
            slice.bossMinute = 10f;
            slice.boss = null;
            slice.hardCap = 350;
            slice.ringMinDistance = 20f;
            slice.ringMaxDistance = 24f;
            slice.recycleDistance = 35f;
            slice.moveDirBias = 0.6f;
            slice.moveDirConeHalfAngle = 60f;
            slice.eventCapOverflow = 80;
            slice.enrageTimeSeconds = 0f; // slice has no enrage
            slice.enrageSpawnRatePerSec = 0f;
            slice.enrageEnemy = null;
            slice.coopScaling = coop;
            slice.events = new List<WaveEvent>();
            slice.entries = new List<WaveEntry>
            {
                SliceRow(0, 15, 1.0f, 0, shambler, runner),
                SliceRow(1, 25, 1.5f, 0, shambler, runner),
                SliceRow(2, 35, 2.0f, 20, shambler, runner),
                SliceRow(3, 50, 2.5f, 25, shambler, runner),
                SliceRow(4, 60, 3.0f, 25, shambler, runner),
                SliceRow(5, 70, 3.5f, 25, shambler, runner),
                SliceRow(6, 85, 4.0f, 25, shambler, runner),
                SliceRow(7, 100, 4.5f, 25, shambler, runner),
                SliceRow(8, 120, 5.0f, 25, shambler, runner),
                SliceRow(9, 140, 5.5f, 30, shambler, runner),
            };

            // ==================================================================== SPN_FullRun (DataTables §4) — NEW
            var full = Asset<SpawnSchedule>("Config", "SPN_FullRun");
            full.runLengthMinutes = 15f;
            full.bossMinute = 15f;
            full.boss = king;
            full.hardCap = 350;
            full.ringMinDistance = 20f;
            full.ringMaxDistance = 24f;
            full.recycleDistance = 35f;
            full.moveDirBias = 0.6f;
            full.moveDirConeHalfAngle = 60f;
            full.eventCapOverflow = 80;
            full.enrageTimeSeconds = 1200f;
            full.enrageSpawnRatePerSec = 6f;
            full.enrageEnemy = shambler;
            full.coopScaling = coop;
            full.entries = new List<WaveEntry>
            {
                FullRow(0, 15, 1.0f, shambler, 100, null, 0, null, 0, null, 0),
                FullRow(1, 25, 1.5f, shambler, 100, null, 0, null, 0, null, 0),
                FullRow(2, 35, 2.0f, shambler, 80, runner, 20, null, 0, null, 0),
                FullRow(3, 50, 2.5f, shambler, 75, runner, 25, null, 0, null, 0),
                FullRow(4, 60, 3.0f, shambler, 65, runner, 25, bloater, 10, null, 0),
                FullRow(5, 70, 3.5f, shambler, 60, runner, 25, bloater, 15, null, 0),
                FullRow(6, 85, 4.0f, shambler, 50, runner, 25, bloater, 15, spitter, 10),
                FullRow(7, 100, 4.5f, shambler, 45, runner, 25, bloater, 15, spitter, 15),
                FullRow(8, 120, 5.0f, shambler, 45, runner, 25, bloater, 15, spitter, 15),
                FullRow(9, 140, 5.5f, shambler, 40, runner, 30, bloater, 15, spitter, 15),
                FullRow(10, 160, 6.0f, shambler, 40, runner, 25, bloater, 20, spitter, 15),
                FullRow(11, 180, 6.5f, shambler, 35, runner, 30, bloater, 20, spitter, 15),
                FullRow(12, 200, 7.0f, shambler, 35, runner, 30, bloater, 20, spitter, 15),
                FullRow(13, 230, 7.5f, shambler, 30, runner, 30, bloater, 20, spitter, 20),
                FullRow(14, 260, 8.0f, shambler, 30, runner, 30, bloater, 20, spitter, 20),
                // Minute 15 = boss trickle: Shamblers only, targetAlive 40, rate 1.5.
                FullRow(15, 40, 1.5f, shambler, 100, null, 0, null, 0, null, 0),
            };
            // Events: swarms ignoreCap false; elite/boss ignoreCap true (DataTables §4.3 / §5).
            full.events = new List<WaveEvent>
            {
                Evt(210f, WaveEventType.Swarm, runner, 40, SpawnPattern.LineStampede, ignoreCap: false, ringRadius: 0f, sides: 0),
                Evt(300f, WaveEventType.Elite, graveKnight, 1, SpawnPattern.RingRandom, ignoreCap: true, ringRadius: 0f, sides: 0),
                Evt(420f, WaveEventType.EnableEvolutions, null, 0, SpawnPattern.RingRandom, ignoreCap: false, ringRadius: 0f, sides: 0),
                Evt(480f, WaveEventType.Elite, graveKnight, 1, SpawnPattern.RingRandom, ignoreCap: true, ringRadius: 0f, sides: 0),
                Evt(510f, WaveEventType.Swarm, bloater, 15, SpawnPattern.Cluster, ignoreCap: false, ringRadius: 0f, sides: 0),
                Evt(630f, WaveEventType.Elite, graveKnight, 1, SpawnPattern.RingRandom, ignoreCap: true, ringRadius: 0f, sides: 0),
                Evt(720f, WaveEventType.Swarm, shambler, 80, SpawnPattern.RingFull, ignoreCap: false, ringRadius: 16f, sides: 0),
                Evt(780f, WaveEventType.Elite, graveKnight, 1, SpawnPattern.RingRandom, ignoreCap: true, ringRadius: 0f, sides: 0),
                Evt(840f, WaveEventType.Swarm, runner, 80, SpawnPattern.LineStampede, ignoreCap: false, ringRadius: 0f, sides: 2),
                Evt(900f, WaveEventType.Boss, king, 1, SpawnPattern.RingRandom, ignoreCap: true, ringRadius: 0f, sides: 0),
            };

            // ==================================================================== CFG_RunConfig — slice wiring left alone
            var run = Asset<RunConfig>("Config", "CFG_RunConfig");
            run.defaultCharacter = knight;
            run.defaultAimMode = AimMode.Assisted;
            run.spawnSchedule = slice; // NOT full — graybox stays on the 10-minute slice
            run.xpCurve = xp;
            run.levelUpRules = rules;
            run.weaponPool = new List<WeaponDefinition> { longsword }; // slice pool
            run.passivePool = new List<PassiveDefinition> { whetstone, beads, plate }; // slice pool
            run.feedbackSettings = feedback;
            run.hitFeedbackProfile = hitProfile;
            a.runConfig = run;

            // ==================================================================== Pool prewarm (GDD 7.8)
            var pools = Asset<PoolPrewarmConfig>("Config", "CFG_PoolPrewarm");
            int[] gemCounts = { 400, 64, 32 };
            pools.entries = new List<PoolPrewarmEntry>();
            for (int i = 0; i < gemPrefabs.Length; i++)
                pools.entries.Add(new PoolPrewarmEntry { prefab = gemPrefabs[i], prewarmCount = gemCounts[i] });
            a.poolConfig = pools;

            AssetDatabase.SaveAssets();
            return a;
        }

        // ------------------------------------------------------------------ weapons

        private static WeaponDefinition BuildLongsword(GameObject prefab)
        {
            var w = Asset<WeaponDefinition>("Weapons", "WPN_Longsword");
            FillWeapon(w, "longsword", "Longsword", WeaponArchetype.MeleeArc, TargetingMode.AimDirection,
                DamageType.Physical, arc: 120f, projSpeed: 0f, range: 6f, hitInterval: 0f, maxLevel: 8,
                isEvo: false, spread: 0f, killHeal: 0f, killCap: 0f, boomerang: false, prefab: prefab);
            w.levels = new List<WeaponLevel>
            {
                Lvl(1, 12, 1.20f, 2.8f, 1, -1, 0.20f, 6, "Slash in front"),
                Lvl(2, 15, 1.20f, 2.8f, 1, -1, 0.20f, 6, "+3 damage"),
                Lvl(3, 15, 1.10f, 3.1f, 1, -1, 0.20f, 6, "Faster, wider"),
                Lvl(4, 19, 1.10f, 3.1f, 2, -1, 0.20f, 6, "Rear slash"),
                Lvl(5, 19, 1.00f, 3.4f, 2, -1, 0.20f, 7, "Faster, wider"),
                Lvl(6, 24, 1.00f, 3.4f, 2, -1, 0.20f, 7, "+5 damage"),
                Lvl(7, 24, 0.90f, 3.7f, 2, -1, 0.20f, 7, "Faster, wider"),
                Lvl(8, 30, 0.90f, 3.7f, 3, -1, 0.20f, 8, "Side slashes (max, can evolve)"),
            };
            return w;
        }

        private static WeaponDefinition BuildCrossbow()
        {
            var w = Asset<WeaponDefinition>("Weapons", "WPN_Crossbow");
            FillWeapon(w, "crossbow", "Crossbow", WeaponArchetype.Projectile, TargetingMode.NearestEnemy,
                DamageType.Physical, arc: 0f, projSpeed: 25f, range: 18f, hitInterval: 0f, maxLevel: 8,
                isEvo: false, spread: 0f, killHeal: 0f, killCap: 0f, boomerang: false, prefab: null);
            w.levels = new List<WeaponLevel>
            {
                Lvl(1, 10, 1.00f, 0.3f, 1, 1, 0f, 3, "One bolt"),
                Lvl(2, 10, 1.00f, 0.3f, 2, 1, 0f, 3, "+1 bolt"),
                Lvl(3, 13, 1.00f, 0.3f, 2, 1, 0f, 3, "+3 damage"),
                Lvl(4, 13, 0.90f, 0.3f, 2, 2, 0f, 3, "+1 pierce, faster"),
                Lvl(5, 16, 0.90f, 0.3f, 3, 2, 0f, 3, "+1 bolt, +3 damage"),
                Lvl(6, 16, 0.80f, 0.3f, 3, 2, 0f, 4, "Faster"),
                Lvl(7, 18, 0.80f, 0.3f, 3, 3, 0f, 4, "+1 pierce"),
                Lvl(8, 18, 0.75f, 0.3f, 4, 3, 0f, 4, "+1 bolt (max, can evolve)"),
            };
            return w;
        }

        private static WeaponDefinition BuildHolyFlail()
        {
            var w = Asset<WeaponDefinition>("Weapons", "WPN_HolyFlail");
            FillWeapon(w, "holy_flail", "Holy Flail", WeaponArchetype.Orbit, TargetingMode.PlayerCentered,
                DamageType.Holy, arc: 0f, projSpeed: 180f, range: 0f, hitInterval: 0.5f, maxLevel: 8,
                isEvo: false, spread: 0f, killHeal: 0f, killCap: 0f, boomerang: false, prefab: null);
            w.levels = new List<WeaponLevel>
            {
                Lvl(1, 8, 4.0f, 2.5f, 2, -1, 2.5f, 5, "Two maces"),
                Lvl(2, 10, 4.0f, 2.5f, 2, -1, 2.5f, 5, "+2 damage"),
                Lvl(3, 10, 4.0f, 2.5f, 3, -1, 2.5f, 5, "+1 mace"),
                Lvl(4, 10, 4.0f, 2.5f, 3, -1, 3.0f, 5, "Longer"),
                Lvl(5, 13, 4.0f, 2.5f, 3, -1, 3.0f, 6, "+3 damage"),
                Lvl(6, 13, 4.0f, 2.5f, 4, -1, 3.0f, 6, "+1 mace"),
                Lvl(7, 13, 4.0f, 3.0f, 4, -1, 3.0f, 6, "Wider orbit"),
                Lvl(8, 16, 4.0f, 3.0f, 4, -1, 3.5f, 7, "+3 damage, longer (max)"),
            };
            return w;
        }

        private static WeaponDefinition BuildThrowingAxes()
        {
            var w = Asset<WeaponDefinition>("Weapons", "WPN_ThrowingAxes");
            FillWeapon(w, "throwing_axes", "Throwing Axes", WeaponArchetype.LobbedProjectile, TargetingMode.AimDirection,
                DamageType.Physical, arc: 0f, projSpeed: 12f, range: 6f, hitInterval: 0f, maxLevel: 8,
                isEvo: false, spread: 20f, killHeal: 0f, killCap: 0f, boomerang: false, prefab: null);
            w.levels = new List<WeaponLevel>
            {
                Lvl(1, 20, 1.50f, 0.6f, 1, 3, 0f, 4, "One axe"),
                Lvl(2, 20, 1.50f, 0.6f, 2, 3, 0f, 4, "+1 axe"),
                Lvl(3, 25, 1.50f, 0.6f, 2, 3, 0f, 4, "+5 damage"),
                Lvl(4, 25, 1.50f, 0.7f, 2, 5, 0f, 4, "+2 pierce, bigger"),
                Lvl(5, 25, 1.50f, 0.7f, 3, 5, 0f, 5, "+1 axe"),
                Lvl(6, 30, 1.50f, 0.7f, 3, 5, 0f, 5, "+5 damage"),
                Lvl(7, 30, 1.30f, 0.8f, 3, 5, 0f, 5, "Faster, bigger"),
                Lvl(8, 35, 1.30f, 0.8f, 4, 6, 0f, 5, "+1 axe (max, can evolve)"),
            };
            return w;
        }

        private static WeaponDefinition BuildFireFlask()
        {
            var w = Asset<WeaponDefinition>("Weapons", "WPN_FireFlask");
            FillWeapon(w, "fire_flask", "Fire Oil Flask", WeaponArchetype.GroundZone, TargetingMode.RandomEnemyInRange,
                DamageType.Fire, arc: 0f, projSpeed: 10f, range: 10f, hitInterval: 0.5f, maxLevel: 8,
                isEvo: false, spread: 0f, killHeal: 0f, killCap: 0f, boomerang: false, prefab: null);
            w.levels = new List<WeaponLevel>
            {
                Lvl(1, 6, 4.0f, 2.0f, 1, -1, 3.0f, 0, "One pool"),
                Lvl(2, 6, 4.0f, 2.0f, 2, -1, 3.0f, 0, "+1 flask"),
                Lvl(3, 8, 4.0f, 2.0f, 2, -1, 3.0f, 0, "+2 damage"),
                Lvl(4, 8, 4.0f, 2.4f, 2, -1, 3.5f, 0, "Bigger, longer"),
                Lvl(5, 8, 4.0f, 2.4f, 3, -1, 3.5f, 0, "+1 flask"),
                Lvl(6, 10, 4.0f, 2.4f, 3, -1, 3.5f, 0, "+2 damage"),
                Lvl(7, 10, 3.5f, 2.4f, 3, -1, 3.5f, 0, "Faster"),
                Lvl(8, 12, 3.5f, 2.8f, 3, -1, 4.0f, 0, "+2 damage, bigger (max)"),
            };
            return w;
        }

        private static WeaponDefinition BuildConsecratedGround()
        {
            var w = Asset<WeaponDefinition>("Weapons", "WPN_ConsecratedGround");
            FillWeapon(w, "consecrated_ground", "Consecrated Ground", WeaponArchetype.Aura, TargetingMode.PlayerCentered,
                DamageType.Holy, arc: 0f, projSpeed: 0f, range: 0f, hitInterval: 0f, maxLevel: 8,
                isEvo: false, spread: 0f, killHeal: 0f, killCap: 0f, boomerang: false, prefab: null);
            w.levels = new List<WeaponLevel>
            {
                Lvl(1, 3, 0.50f, 2.0f, 1, -1, 0f, 1, "Holy ring"),
                Lvl(2, 3, 0.50f, 2.4f, 1, -1, 0f, 1, "Wider"),
                Lvl(3, 4, 0.50f, 2.4f, 1, -1, 0f, 1, "+1 damage"),
                Lvl(4, 4, 0.45f, 2.4f, 1, -1, 0f, 1, "Faster tick"),
                Lvl(5, 4, 0.45f, 2.8f, 1, -1, 0f, 1, "Wider"),
                Lvl(6, 5, 0.45f, 2.8f, 1, -1, 0f, 2, "+1 damage"),
                Lvl(7, 5, 0.45f, 3.2f, 1, -1, 0f, 2, "Wider"),
                Lvl(8, 6, 0.40f, 3.2f, 1, -1, 0f, 2, "+1 damage, faster (max)"),
            };
            return w;
        }

        private static WeaponDefinition BuildGreatsword()
        {
            var w = Asset<WeaponDefinition>("Weapons", "WPN_ExecutionersGreatsword");
            FillWeapon(w, "executioners_greatsword", "Executioner's Greatsword", WeaponArchetype.MeleeArc,
                TargetingMode.PlayerCentered, DamageType.Physical, arc: 360f, projSpeed: 0f, range: 0f,
                hitInterval: 0f, maxLevel: 1, isEvo: true, spread: 0f, killHeal: 1f, killCap: 10f,
                boomerang: false, prefab: null);
            w.levels = new List<WeaponLevel>
            {
                Lvl(1, 40, 0.90f, 4.2f, 1, -1, 0.20f, 10, "360° whirl. Kills heal 1 HP (max 10/s)."),
            };
            return w;
        }

        private static WeaponDefinition BuildBallista()
        {
            var w = Asset<WeaponDefinition>("Weapons", "WPN_BallistaRepeater");
            FillWeapon(w, "ballista_repeater", "Ballista Repeater", WeaponArchetype.Projectile,
                TargetingMode.NearestEnemy, DamageType.Physical, arc: 0f, projSpeed: 35f, range: 18f,
                hitInterval: 0f, maxLevel: 1, isEvo: true, spread: 0f, killHeal: 0f, killCap: 0f,
                boomerang: false, prefab: null);
            w.levels = new List<WeaponLevel>
            {
                Lvl(1, 22, 0.50f, 0.3f, 5, 5, 0f, 8, "Five bolts, knockback 8."),
            };
            return w;
        }

        private static WeaponDefinition BuildCleavers()
        {
            var w = Asset<WeaponDefinition>("Weapons", "WPN_BloodstormCleavers");
            FillWeapon(w, "bloodstorm_cleavers", "Bloodstorm Cleavers", WeaponArchetype.LobbedProjectile,
                TargetingMode.AimDirection, DamageType.Physical, arc: 0f, projSpeed: 12f, range: 6f,
                hitInterval: 0f, maxLevel: 1, isEvo: true, spread: 0f, killHeal: 0f, killCap: 0f,
                boomerang: true, prefab: null);
            w.levels = new List<WeaponLevel>
            {
                Lvl(1, 45, 1.2f, 1.2f, 6, -1, 0f, 5, "Six axes, boomerang."),
            };
            return w;
        }

        private static WeaponDefinition BuildHalo()
        {
            var w = Asset<WeaponDefinition>("Weapons", "WPN_MorningstarHalo");
            FillWeapon(w, "morningstar_halo", "Morningstar Halo", WeaponArchetype.Orbit,
                TargetingMode.PlayerCentered, DamageType.Holy, arc: 0f, projSpeed: 180f, range: 0f,
                hitInterval: 0.5f, maxLevel: 1, isEvo: true, spread: 0f, killHeal: 0f, killCap: 0f,
                boomerang: false, prefab: null);
            w.levels = new List<WeaponLevel>
            {
                Lvl(1, 22, 0f, 3.2f, 5, -1, 0f, 8, "Permanent orbit."),
            };
            return w;
        }

        private static void FillWeapon(WeaponDefinition w, string id, string displayName, WeaponArchetype archetype,
            TargetingMode targeting, DamageType damageType, float arc, float projSpeed, float range,
            float hitInterval, int maxLevel, bool isEvo, float spread, float killHeal, float killCap,
            bool boomerang, GameObject prefab)
        {
            w.id = id;
            w.displayName = displayName;
            w.archetype = archetype;
            w.targetingMode = targeting;
            w.damageType = damageType;
            w.arcDegrees = arc;
            w.projectileSpeed = projSpeed;
            w.range = range;
            w.hitInterval = hitInterval;
            w.maxLevel = maxLevel;
            w.isEvolution = isEvo;
            w.spreadHalfAngle = spread;
            w.killHeal = killHeal;
            w.killHealCapPerSec = killCap;
            w.boomerang = boomerang;
            w.prefab = prefab;
        }

        /// <summary>One WeaponLevel row. pierce/duration are explicit so non-Longsword weapons are not forced to -1 / 0.2.</summary>
        private static WeaponLevel Lvl(int level, float damage, float cooldown, float area, int amount,
            int pierce, float duration, float knockback, string description)
        {
            return new WeaponLevel
            {
                level = level, damage = damage, cooldown = cooldown, area = area, amount = amount,
                pierce = pierce, duration = duration, knockback = knockback, description = description
            };
        }

        // ------------------------------------------------------------------ passives / recipes / meta / unlocks

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

        private static EvolutionRecipe Recipe(string file, string id, WeaponDefinition baseWeapon,
            PassiveDefinition requiredPassive, WeaponDefinition result)
        {
            var r = Asset<EvolutionRecipe>("Evolutions", file);
            r.id = id;
            r.baseWeapon = baseWeapon;
            r.requiredWeaponLevel = 8;
            r.requiredPassive = requiredPassive;
            r.conditionType = EvolutionConditionType.ChestAfterMinute;
            r.minMinute = 7f;
            r.resultWeapon = result;
            return r;
        }

        private static MetaUpgrade Meta(string file, string id, string displayName, StatType stat,
            ModifierOp op, float valuePerRank, int maxRank, int baseCost)
        {
            var m = Asset<MetaUpgrade>("Meta", file);
            m.id = id;
            m.displayName = displayName;
            m.stat = stat;
            m.op = op;
            m.valuePerRank = valuePerRank;
            m.maxRank = maxRank;
            m.baseCost = baseCost;
            m.costGrowth = 0.25f;
            return m;
        }

        private static UnlockDefinition Unlock(string file, string id, string displayName,
            UnlockConditionType type, float threshold,
            WeaponDefinition weapon, PassiveDefinition passive, MetaUpgrade meta)
        {
            var u = Asset<UnlockDefinition>("Unlocks", file);
            u.id = id;
            u.displayName = displayName;
            u.conditionType = type;
            u.threshold = threshold;
            u.unlocksWeapon = weapon;
            u.unlocksPassive = passive;
            u.unlocksMetaUpgrade = meta;
            u.unlocksCharacter = null;
            return u;
        }

        // ------------------------------------------------------------------ enemies

        private static void FillEnemy(EnemyDefinition e, string id, string displayName, EnemyTier tier,
            EnemyBehavior behavior, float hp, float hpPerMin, float speed, float contact, float dmgPerMin,
            int xp, float mass, float kbResist, float radius, int prewarm,
            float keepMin, float keepMax, float firstMinute, float crowns, bool dropsChest)
        {
            e.id = id;
            e.displayName = displayName;
            e.tier = tier;
            e.behavior = behavior;
            e.maxHealth = hp;
            e.hpPerMinute = hpPerMin;
            e.moveSpeed = speed;
            e.contactDamage = contact;
            e.damagePerMinute = dmgPerMin;
            e.contactInterval = 0.5f;
            e.xpValue = xp;
            e.mass = mass;
            e.knockbackResist = kbResist;
            e.radius = radius;
            e.prewarmCount = prewarm;
            e.keepDistanceMin = keepMin;
            e.keepDistanceMax = keepMax;
            e.firstMinute = firstMinute;
            e.crownValue = crowns;
            e.dropsChest = dropsChest;
            // Boss-only fields default to 0; Rotting King overwrites after FillEnemy.
            e.phase2HpFraction = 0f;
            e.phase2MoveSpeed = 0f;
            e.enrageDamageMult = 0f;
            e.enrageSpeedMult = 0f;
        }

        private static EnemyAttack Atk(string name, float telegraph, float radius, float range, float width,
            float arc, float[] angles, float damage, float dmgEnemies, float knockback, float interval,
            float phaseInterval, float projSpeed, EnemyDefinition summon, int summonCount, float hpThreshold)
        {
            return new EnemyAttack
            {
                name = name,
                telegraph = telegraph,
                radius = radius,
                range = range,
                width = width,
                arcDegrees = arc,
                angleOffsets = angles ?? new float[0],
                damage = damage,
                damageToEnemies = dmgEnemies,
                knockback = knockback,
                interval = interval,
                phaseInterval = phaseInterval,
                projectileSpeed = projSpeed,
                summonEnemy = summon,
                summonCount = summonCount,
                hpThreshold = hpThreshold,
            };
        }

        // ------------------------------------------------------------------ schedules

        /// <summary>Slice row: Shambler + optional Runner only (Bloater/Spitter folded into Shambler).</summary>
        private static WaveEntry SliceRow(int minute, int targetAlive, float rate, float runnerPct,
            EnemyDefinition shambler, EnemyDefinition runner)
        {
            var row = new WaveEntry { minute = minute, targetAlive = targetAlive, spawnRatePerSec = rate, mix = new List<EnemyWeight>() };
            row.mix.Add(new EnemyWeight { enemy = shambler, weightPct = 100f - runnerPct });
            if (runnerPct > 0f) row.mix.Add(new EnemyWeight { enemy = runner, weightPct = runnerPct });
            return row;
        }

        /// <summary>Full-run row. Omit a type by passing null / 0 weight (do not store a 0-weight row).</summary>
        private static WaveEntry FullRow(int minute, int targetAlive, float rate,
            EnemyDefinition a, float wa, EnemyDefinition b, float wb, EnemyDefinition c, float wc, EnemyDefinition d, float wd)
        {
            var row = new WaveEntry { minute = minute, targetAlive = targetAlive, spawnRatePerSec = rate, mix = new List<EnemyWeight>() };
            if (a != null && wa > 0f) row.mix.Add(new EnemyWeight { enemy = a, weightPct = wa });
            if (b != null && wb > 0f) row.mix.Add(new EnemyWeight { enemy = b, weightPct = wb });
            if (c != null && wc > 0f) row.mix.Add(new EnemyWeight { enemy = c, weightPct = wc });
            if (d != null && wd > 0f) row.mix.Add(new EnemyWeight { enemy = d, weightPct = wd });
            return row;
        }

        private static WaveEvent Evt(float time, WaveEventType type, EnemyDefinition enemy, int count,
            SpawnPattern pattern, bool ignoreCap, float ringRadius, int sides)
        {
            return new WaveEvent
            {
                timeSeconds = time,
                type = type,
                enemy = enemy,
                count = count,
                pattern = pattern,
                ignoreCap = ignoreCap,
                ringRadius = ringRadius,
                sides = sides,
            };
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
