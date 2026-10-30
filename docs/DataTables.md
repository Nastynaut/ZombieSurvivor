# Knight Survivors data tables (Coder fill sheet)

Fill sheet for the Unity Coder. Source of numbers and behavior: `docs/GDD.md` **v0.4** (§3 weapons and passives, §3.4 evolutions, §3.5 enemies, §4.3 level-up, §5 waves, §6 meta, §7.8 prewarm, §9 schema). Field names and types are the C# in `Assets/_Project/Scripts/Data/` as merged in PR #1. This sheet does not change code.

If a number here disagrees with the GDD, the GDD wins. Cells marked **†** are filled because the GDD is silent or says "n/a". They are listed again in §5. Do not retune them. Do not invent any other number.

`WeaponLevel`, `EnemyAttack`, `WaveEntry`, `WaveEvent`, and `EnemyWeight` are `[Serializable]` types embedded on their parent asset. They are not their own `.asset` files.

Percents are fractions (`0.10` = +10%). Units are metres, seconds, m/s, and HP unless a row says otherwise. Orbit `projectileSpeed` is deg/s (see the tooltip on `WeaponDefinition.projectileSpeed`).

Leave `Assets/_Project/Data/Config/SPN_Slice.asset` and the slice wiring in `CFG_RunConfig` alone. Those are the §8.1 ten-minute slice (minutes 0–9, Bloater and Spitter folded into Shambler, no boss). This sheet is the 15-minute run.

---

## 0. Paths

Existing menu names come from `[CreateAssetMenu]` on the class. Match the folders `GameDataFactory` already uses (`Assets/_Project/Data/...`).

| Class | Kind | Asset path |
|---|---|---|
| `WeaponDefinition` | ScriptableObject | `Assets/_Project/Data/Weapons/WPN_<Name>.asset` |
| `WeaponLevel` | struct on `levels` | (no file) |
| `PassiveDefinition` | ScriptableObject | `Assets/_Project/Data/Passives/PSV_<Name>.asset` |
| `EvolutionRecipe` | ScriptableObject | `Assets/_Project/Data/Evolutions/EVO_<Name>.asset` |
| `LevelUpRules` | ScriptableObject | `Assets/_Project/Data/Config/CFG_LevelUpRules.asset` (already created) |
| `MetaUpgrade` | ScriptableObject | `Assets/_Project/Data/Meta/META_<Name>.asset` |
| `EnemyDefinition` | ScriptableObject | `Assets/_Project/Data/Enemies/ENM_<Name>.asset` |
| `EnemyAttack` | class on `attackParams` | (no file) |
| `SpawnSchedule` | ScriptableObject | `Assets/_Project/Data/Config/SPN_FullRun.asset` (**new**; do not overwrite `SPN_Slice`) |
| `WaveEntry` / `WaveEvent` / `EnemyWeight` | serializable on the schedule | (no file) |

`icon`, `portrait`, and `hitSfx` stay empty. `prefab` on Longsword, Shambler, and Runner stays the prefab the graybox builder already assigns. Every other `prefab` stays empty until that behaviour prefab exists. Enemy prefab **names** (not created here) are the GDD §3.5.1 placeholders: `PF_PH_Zombie_Shambler`, `PF_PH_Zombie_Runner`, `PF_PH_Zombie_Bloater`, `PF_PH_Zombie_Spitter`, `PF_PH_Elite_GraveKnight`, `PF_PH_Boss_RottingKing`. Visual scale (Grave Knight 1.6×, Rotting King ~2.2×) lives on the prefab, not on these assets. Colors are PlaceholderSpec, not data.

File names:

| Asset file | `id` |
|---|---|
| `WPN_Longsword` | `longsword` |
| `WPN_Crossbow` | `crossbow` |
| `WPN_HolyFlail` | `holy_flail` |
| `WPN_ThrowingAxes` | `throwing_axes` |
| `WPN_FireFlask` | `fire_flask` |
| `WPN_ConsecratedGround` | `consecrated_ground` |
| `WPN_ExecutionersGreatsword` | `executioners_greatsword` (§9.5 example) |
| `WPN_BallistaRepeater` | `ballista_repeater` † id assigned in this sheet |
| `WPN_BloodstormCleavers` | `bloodstorm_cleavers` † |
| `WPN_MorningstarHalo` | `morningstar_halo` † |
| `PSV_Whetstone` | `whetstone` |
| `PSV_PlateArmor` | `plate_armor` |
| `PSV_PrayerBeads` | `prayer_beads` |
| `PSV_WarBanner` | `war_banner` |
| `PSV_Spurs` | `spurs` |
| `PSV_Lodestone` | `lodestone` |
| `EVO_Greatsword` | `evo_greatsword` |
| `EVO_Ballista` | `evo_ballista` |
| `EVO_Cleavers` | `evo_cleavers` |
| `EVO_Censer` | `evo_censer` |
| `ENM_Shambler` | `shambler` |
| `ENM_Runner` | `runner` |
| `ENM_Bloater` | `bloater` |
| `ENM_Spitter` | `spitter` |
| `ENM_GraveKnight` | `grave_knight` |
| `ENM_RottingKing` | `rotting_king` |
| `META_Might` | `meta_might` |
| `META_Armor` | `meta_armor` |
| `META_MaxHp` | `meta_maxhp` |
| `META_Recovery` | `meta_recovery` |
| `META_Cooldown` | `meta_cooldown` |
| `META_Area` | `meta_area` |
| `META_Speed` | `meta_speed` |
| `META_Magnet` | `meta_magnet` |
| `META_Growth` | `meta_growth` |
| `META_Greed` | `meta_greed` |
| `META_Luck` | `meta_luck` |
| `META_Revival` | `meta_revival` |
| `META_Reroll` | `meta_reroll` |
| `META_Banish` | `meta_banish` |
| `META_Amount` | `meta_amount` |
| `SPN_FullRun` | (no id field) |
| `CFG_LevelUpRules` | (no id field) |

---

## 0.1 Fields to add

These GDD numbers have no field today. Names are new in this sheet (flagged). Append any new enum value at the **end** of the enum (`GameEnums.cs` header). Do not add fields that are not in this list.

| Class | Field to add | Type | Units | Who sets it |
|---|---|---|---|---|
| `WeaponDefinition` | `spreadHalfAngle` | float | deg | Throwing Axes **20** (the ±20° lob). Everyone else **0**. |
| `WeaponDefinition` | `killHeal` | float | HP | Executioner's Greatsword **1**. Everyone else **0**. |
| `WeaponDefinition` | `killHealCapPerSec` | float | HP/s | Executioner's Greatsword **10**. Everyone else **0**. |
| `WeaponDefinition` | `boomerang` | bool | — | Bloodstorm Cleavers **true**. Everyone else **false**. |
| `EnemyAttack` | `knockback` | float | m/s | Ground Slam **6**. Every other attack **0**. |
| `EnemyAttack` | `damageToEnemies` | float | HP | Bloater Death Blast **50** (other enemies). Player damage stays in `damage` (**15**). Every other attack **0**. |
| `EnemyAttack` | `phaseInterval` | float | s | Ground Slam **6**. **0** means "no override". Used only while the owner's HP fraction ≤ `EnemyDefinition.phase2HpFraction`. |
| `EnemyDefinition` | `phase2HpFraction` | float | 0–1 | Rotting King **0.5**. Everyone else **0** (no phase 2). |
| `EnemyDefinition` | `phase2MoveSpeed` | float | m/s | Rotting King **2.8**. Everyone else **0** (keep `moveSpeed`). Applied while HP fraction ≤ `phase2HpFraction`. |
| `EnemyDefinition` | `enrageDamageMult` | float | mult | Rotting King **2**. Everyone else **0** (do not read). Read only when `tier == Boss`, at `SpawnSchedule.enrageTimeSeconds`. Applies to contact damage and to `EnemyAttack.damage`. |
| `EnemyDefinition` | `enrageSpeedMult` | float | mult | Rotting King **1.3**. Everyone else **0** (do not read). Multiplies the speed the boss has at 20:00, which is `phase2MoveSpeed` if phase 2 has already started. Stacking is flagged in §5. |
| `SpawnSchedule` | `moveDirConeHalfAngle` | float | deg | **60** (±60° cone). `moveDirBias` is the separate 60% weight. |
| `SpawnSchedule` | `eventCapOverflow` | int | count | **80**. Swarm events may reach `hardCap + 80`. They do not ignore the cap. |
| `SpawnSchedule` | `enrageTimeSeconds` | float | s | **1200** (20:00). |
| `SpawnSchedule` | `enrageSpawnRatePerSec` | float | /s | **6**. Replaces the minute-15 rate. Does not add a new minute row. |
| `SpawnSchedule` | `enrageEnemy` | EnemyDefinition | ref | Shambler. |
| `WaveEvent` | `ringRadius` | float | m | Horde Ring **16**. **0** on every other event means "use `ringMinDistance`–`ringMaxDistance`". |
| `WaveEvent` | `sides` | int | count | 14:00 stampede **2**. **0** on every other event means one line / pattern default. |
| `LevelUpRules` | `chestSpinSeconds` | float | s | **1.5**. |
| `LevelUpRules` | `rerollPerChest` | int | count | **1** (§4.3: each chest grants +1 reroll). |

No new fields on `WeaponLevel`, `PassiveDefinition`, `EvolutionRecipe`, `MetaUpgrade`, or `WaveEntry`. Their GDD columns already exist.

---

## 1. Weapons

### 1.1 `WeaponDefinition`

**(a) Already in code:** `id` string, `displayName` string, `icon` Sprite, `archetype` WeaponArchetype, `targetingMode` TargetingMode, `damageType` DamageType, `arcDegrees` float deg, `projectileSpeed` float m/s (deg/s for Orbit), `range` float m, `hitInterval` float s, `maxLevel` int, `levels` List\<WeaponLevel\>, `isEvolution` bool, `prefab` GameObject, `hitSfx` AudioClip[].

**(b) Add:** `spreadHalfAngle`, `killHeal`, `killHealCapPerSec`, `boomerang` (§0.1).

**(c) Code defaults that differ from the GDD:**

| Field | Code initializer | What to store |
|---|---|---|
| `id` / `displayName` / `archetype` / `targetingMode` / `damageType` | Longsword values | Per row below. A new asset left on the initializer is a Longsword. |
| `arcDegrees` | `120` | **120** only on Longsword. **360** on the Greatsword. **0** on every other weapon. Leaving 120 on a Crossbow is wrong. |
| `range` | `6` | **6** on Longsword and Throwing Axes (assisted-aim range, §2.2). **18** on Crossbow. **10** on Fire Oil Flask. **0** where the GDD gives no range (†). |
| `hitInterval` | `0.5` | **0.5** on Holy Flail and Fire Oil Flask only. The initializer (and the factory Longsword, which never writes this field) leaves **0.5** on Longsword, where the GDD says n/a. Store **0** for n/a (†). |
| `maxLevel` | `8` | **8** on the six base weapons. **1** on evolutions. |
| `projectileSpeed` | `0` | Matches "n/a". Overwrite with the speeds below. |
| `GetLevel` fallback (not a serialized field) | damage 10, cooldown 1, area 2.5, knockback 5, pierce −1, duration 0.2 | Safety net when `levels` is empty. It is not Longsword L1 (12 / 1.2 / 2.8 / 6). Do not ship an empty list. |

`GameDataFactory.Lvl` hardcodes `pierce = -1` and `duration = 0.2`. That matches Longsword only. Do not reuse it for the other weapons.

Definition values. `icon` and `hitSfx` empty. `prefab` empty except the existing Longsword prefab.

| id | displayName | archetype | targetingMode | damageType | arcDegrees | projectileSpeed | range | hitInterval | maxLevel | isEvolution | spreadHalfAngle | killHeal | killHealCapPerSec | boomerang |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `longsword` | Longsword | MeleeArc | AimDirection | Physical | 120 | 0 | 6 | 0 † | 8 | false | 0 | 0 | 0 | false |
| `crossbow` | Crossbow | Projectile | NearestEnemy | Physical | 0 † | 25 | 18 | 0 † | 8 | false | 0 | 0 | 0 | false |
| `holy_flail` | Holy Flail | Orbit | PlayerCentered | Holy | 0 † | 180 | 0 † | 0.5 | 8 | false | 0 | 0 | 0 | false |
| `throwing_axes` | Throwing Axes | LobbedProjectile | AimDirection | Physical | 0 † | 12 | 6 | 0 † | 8 | false | 20 | 0 | 0 | false |
| `fire_flask` | Fire Oil Flask | GroundZone | RandomEnemyInRange | Fire | 0 † | 10 | 10 | 0.5 | 8 | false | 0 | 0 | 0 | false |
| `consecrated_ground` | Consecrated Ground | Aura | PlayerCentered | Holy | 0 † | 0 | 0 † | 0 † | 8 | false | 0 | 0 | 0 | false |
| `executioners_greatsword` | Executioner's Greatsword | MeleeArc | PlayerCentered † | Physical | 360 | 0 | 0 † | 0 † | 1 | true | 0 | 1 | 10 | false |
| `ballista_repeater` | Ballista Repeater | Projectile | NearestEnemy † | Physical | 0 † | 35 | 18 † | 0 † | 1 | true | 0 | 0 | 0 | false |
| `bloodstorm_cleavers` | Bloodstorm Cleavers | LobbedProjectile | AimDirection † | Physical | 0 † | 12 † | 6 † | 0 † | 1 | true | 0 † | 0 | 0 | true |
| `morningstar_halo` | Morningstar Halo | Orbit | PlayerCentered † | Holy | 0 † | 180 † | 0 † | 0.5 † | 1 | true | 0 | 0 | 0 | false |

Notes that are rules, not extra fields:

- Crossbow bolts: one bolt per distinct target, inside `range` 18. `area` on the level row is bolt size, not range.
- Holy Flail: maces orbit at radius `area` for `duration`, then wait. `cooldown` **includes** `duration`. `hitInterval` 0.5 is per mace per enemy.
- Throwing Axes: lob with `spreadHalfAngle` 20. Player `amount` adds projectiles.
- Fire Oil Flask: level `damage` is per tick. Tick length is `hitInterval` 0.5. Pool radius is level `area`, pool life is level `duration`.
- Consecrated Ground: the tick **is** `WeaponLevel.cooldown` (0.50 down to 0.40). Do not copy that into `hitInterval` (one float cannot hold every level). `duration` 0 means permanent. Player `amount` does **not** add rings.
- Evolutions are not in the level-up pool. A recipe swaps one into the slot. One `WeaponLevel` row, `maxLevel` 1, `isEvolution` true.
- Starting level-up pool (§6.3): `longsword`, `crossbow`, `holy_flail`, `throwing_axes`. `fire_flask` and `consecrated_ground` join only after their `UnlockDefinition` (`unl_flask` KillTotal 1500, `unl_consecrated` SurviveMinutes 10). `UnlockDefinition` already has the fields; this sheet does not add a class.

### 1.2 `WeaponLevel` (absolute values, row 0 = level 1)

**(a) Already in code:** `level` int, `damage` float HP, `cooldown` float s, `area` float m, `amount` int, `pierce` int (−1 = unlimited), `duration` float s, `knockback` float m/s, `description` string.

**(b) Add:** none.

**(c) Defaults:** a struct row is all zeros / null. That matches no GDD row. The `GetLevel` fallback in §1.1(c) is the other trap.

`description` strings below are the GDD card lines. Evolution descriptions are labels from the GDD blurb (†, not a separate design pass).

**Longsword** (`arcDegrees` 120 lives on the definition)

| level | damage | cooldown | area | amount | pierce | duration | knockback | description |
|---|---|---|---|---|---|---|---|---|
| 1 | 12 | 1.20 | 2.8 | 1 | -1 | 0.20 | 6 | Slash in front |
| 2 | 15 | 1.20 | 2.8 | 1 | -1 | 0.20 | 6 | +3 damage |
| 3 | 15 | 1.10 | 3.1 | 1 | -1 | 0.20 | 6 | Faster, wider |
| 4 | 19 | 1.10 | 3.1 | 2 | -1 | 0.20 | 6 | Rear slash |
| 5 | 19 | 1.00 | 3.4 | 2 | -1 | 0.20 | 7 | Faster, wider |
| 6 | 24 | 1.00 | 3.4 | 2 | -1 | 0.20 | 7 | +5 damage |
| 7 | 24 | 0.90 | 3.7 | 2 | -1 | 0.20 | 7 | Faster, wider |
| 8 | 30 | 0.90 | 3.7 | 3 | -1 | 0.20 | 8 | Side slashes (max, can evolve) |

**Crossbow**

| level | damage | cooldown | area | amount | pierce | duration | knockback | description |
|---|---|---|---|---|---|---|---|---|
| 1 | 10 | 1.00 | 0.3 | 1 | 1 | 0 | 3 | One bolt |
| 2 | 10 | 1.00 | 0.3 | 2 | 1 | 0 | 3 | +1 bolt |
| 3 | 13 | 1.00 | 0.3 | 2 | 1 | 0 | 3 | +3 damage |
| 4 | 13 | 0.90 | 0.3 | 2 | 2 | 0 | 3 | +1 pierce, faster |
| 5 | 16 | 0.90 | 0.3 | 3 | 2 | 0 | 3 | +1 bolt, +3 damage |
| 6 | 16 | 0.80 | 0.3 | 3 | 2 | 0 | 4 | Faster |
| 7 | 18 | 0.80 | 0.3 | 3 | 3 | 0 | 4 | +1 pierce |
| 8 | 18 | 0.75 | 0.3 | 4 | 3 | 0 | 4 | +1 bolt (max, can evolve) |

**Holy Flail**

| level | damage | cooldown | area | amount | pierce | duration | knockback | description |
|---|---|---|---|---|---|---|---|---|
| 1 | 8 | 4.0 | 2.5 | 2 | -1 | 2.5 | 5 | Two maces |
| 2 | 10 | 4.0 | 2.5 | 2 | -1 | 2.5 | 5 | +2 damage |
| 3 | 10 | 4.0 | 2.5 | 3 | -1 | 2.5 | 5 | +1 mace |
| 4 | 10 | 4.0 | 2.5 | 3 | -1 | 3.0 | 5 | Longer |
| 5 | 13 | 4.0 | 2.5 | 3 | -1 | 3.0 | 6 | +3 damage |
| 6 | 13 | 4.0 | 2.5 | 4 | -1 | 3.0 | 6 | +1 mace |
| 7 | 13 | 4.0 | 3.0 | 4 | -1 | 3.0 | 6 | Wider orbit |
| 8 | 16 | 4.0 | 3.0 | 4 | -1 | 3.5 | 7 | +3 damage, longer (max) |

**Throwing Axes**

| level | damage | cooldown | area | amount | pierce | duration | knockback | description |
|---|---|---|---|---|---|---|---|---|
| 1 | 20 | 1.50 | 0.6 | 1 | 3 | 0 | 4 | One axe |
| 2 | 20 | 1.50 | 0.6 | 2 | 3 | 0 | 4 | +1 axe |
| 3 | 25 | 1.50 | 0.6 | 2 | 3 | 0 | 4 | +5 damage |
| 4 | 25 | 1.50 | 0.7 | 2 | 5 | 0 | 4 | +2 pierce, bigger |
| 5 | 25 | 1.50 | 0.7 | 3 | 5 | 0 | 5 | +1 axe |
| 6 | 30 | 1.50 | 0.7 | 3 | 5 | 0 | 5 | +5 damage |
| 7 | 30 | 1.30 | 0.8 | 3 | 5 | 0 | 5 | Faster, bigger |
| 8 | 35 | 1.30 | 0.8 | 4 | 6 | 0 | 5 | +1 axe (max, can evolve) |

**Fire Oil Flask** (damage per 0.5 s tick)

| level | damage | cooldown | area | amount | pierce | duration | knockback | description |
|---|---|---|---|---|---|---|---|---|
| 1 | 6 | 4.0 | 2.0 | 1 | -1 | 3.0 | 0 | One pool |
| 2 | 6 | 4.0 | 2.0 | 2 | -1 | 3.0 | 0 | +1 flask |
| 3 | 8 | 4.0 | 2.0 | 2 | -1 | 3.0 | 0 | +2 damage |
| 4 | 8 | 4.0 | 2.4 | 2 | -1 | 3.5 | 0 | Bigger, longer |
| 5 | 8 | 4.0 | 2.4 | 3 | -1 | 3.5 | 0 | +1 flask |
| 6 | 10 | 4.0 | 2.4 | 3 | -1 | 3.5 | 0 | +2 damage |
| 7 | 10 | 3.5 | 2.4 | 3 | -1 | 3.5 | 0 | Faster |
| 8 | 12 | 3.5 | 2.8 | 3 | -1 | 4.0 | 0 | +2 damage, bigger (max) |

**Consecrated Ground** (`cooldown` is the tick; `duration` 0 = permanent)

| level | damage | cooldown | area | amount | pierce | duration | knockback | description |
|---|---|---|---|---|---|---|---|---|
| 1 | 3 | 0.50 | 2.0 | 1 | -1 | 0 | 1 | Holy ring |
| 2 | 3 | 0.50 | 2.4 | 1 | -1 | 0 | 1 | Wider |
| 3 | 4 | 0.50 | 2.4 | 1 | -1 | 0 | 1 | +1 damage |
| 4 | 4 | 0.45 | 2.4 | 1 | -1 | 0 | 1 | Faster tick |
| 5 | 4 | 0.45 | 2.8 | 1 | -1 | 0 | 1 | Wider |
| 6 | 5 | 0.45 | 2.8 | 1 | -1 | 0 | 2 | +1 damage |
| 7 | 5 | 0.45 | 3.2 | 1 | -1 | 0 | 2 | Wider |
| 8 | 6 | 0.40 | 3.2 | 1 | -1 | 0 | 2 | +1 damage, faster (max) |

**Evolutions** (one row each)

| id | level | damage | cooldown | area | amount | pierce | duration | knockback | description |
|---|---|---|---|---|---|---|---|---|---|
| `executioners_greatsword` | 1 | 40 | 0.90 | 4.2 | 1 † | -1 | 0.20 † | 10 | 360° whirl. Kills heal 1 HP (max 10/s). † |
| `ballista_repeater` | 1 | 22 | 0.50 | 0.3 † | 5 | 5 | 0 † | 8 | Five bolts, knockback 8. † |
| `bloodstorm_cleavers` | 1 | 45 | 1.2 | 1.2 | 6 | -1 | 0 † | 5 † | Six axes, boomerang. † |
| `morningstar_halo` | 1 | 22 | 0 † | 3.2 | 5 | -1 † | 0 | 8 | Permanent orbit. † |

Morningstar Halo `cooldown` 0 means no cycle (GDD: cooldown n/a, duration 0 = permanent). Greatsword heal uses the new fields, not the description string.

---

## 2. Upgrades

### 2.1 `PassiveDefinition`

There is no per-level row type. Level n applies `valuePerLevel × n` (see `PassiveDefinition.DescribeLevel`). Max level is 5 on all six. Do not add a `PassiveLevel` class.

**(a) Already in code:** `id` string, `displayName` string, `icon` Sprite, `stat` StatType, `op` ModifierOp, `valuePerLevel` float, `maxLevel` int.

**(b) Add:** none.

**(c) Code defaults:** initializer is Whetstone (`might`, `PercentAdd`, `0.10`, `maxLevel` 5). Correct for that one asset. Every other passive must overwrite `stat`, `op`, and `valuePerLevel`.

| id | displayName | stat | op | valuePerLevel | maxLevel | L1 | L2 | L3 | L4 | L5 total | Starting pool |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `whetstone` | Whetstone | Might | PercentAdd | 0.10 | 5 | +0.10 | +0.20 | +0.30 | +0.40 | +0.50 | yes |
| `plate_armor` | Plate Armor | Armor | Add | 1 | 5 | +1 | +2 | +3 | +4 | +5 | yes |
| `prayer_beads` | Prayer Beads | Cooldown | PercentAdd | -0.06 | 5 | −0.06 | −0.12 | −0.18 | −0.24 | −0.30 | yes |
| `war_banner` | War Banner | Area | PercentAdd | 0.10 | 5 | +0.10 | +0.20 | +0.30 | +0.40 | +0.50 | yes |
| `spurs` | Knight's Spurs | MoveSpeed | PercentAdd | 0.07 | 5 | +0.07 | +0.14 | +0.21 | +0.28 | +0.35 | no, `unl_spurs` ReachLevel 20 |
| `lodestone` | Lodestone | PickupRadius | PercentAdd | 0.25 | 5 | +0.25 | +0.50 | +0.75 | +1.00 | +1.25 | no, `unl_lodestone` KillTotal 500 |

L1–L5 columns are `valuePerLevel × level`, not stored. Lodestone L5 on a 2.5 m base is `2.5 × (1 + 1.25) = 5.625`. The GDD writes that as 5.6 m. Store **0.25**, not 5.6. Cooldown from passives still respects `RuntimeStats.CooldownFloor` **0.40** (already a code constant; not a new field). `icon` empty.

### 2.2 `EvolutionRecipe`

**(a) Already in code:** `id` string, `baseWeapon` WeaponDefinition, `requiredWeaponLevel` int, `requiredPassive` PassiveDefinition, `conditionType` EvolutionConditionType, `minMinute` float minutes, `resultWeapon` WeaponDefinition.

The GDD §9.5 column header has a space (`required passive`). The C# field is `requiredPassive` with no space.

**(b) Add:** none.

**(c) Code defaults:** `id` `evo_greatsword`, `requiredWeaponLevel` 8, `conditionType` ChestAfterMinute, `minMinute` 7. Those match every recipe. `baseWeapon`, `requiredPassive`, and `resultWeapon` start null and must be assigned. `GameDataFactory` creates no recipe assets yet.

All four: `requiredWeaponLevel` **8**, `conditionType` **ChestAfterMinute**, `minMinute` **7**. The evolved weapon replaces the base weapon in its slot. The passive stays. Evolutions also require the 7:00 `EnableEvolutions` event (§4.3); `minMinute` is the same gate on the recipe.

| id | baseWeapon | requiredPassive | resultWeapon |
|---|---|---|---|
| `evo_greatsword` | `longsword` | `whetstone` | `executioners_greatsword` |
| `evo_ballista` | `crossbow` | `prayer_beads` | `ballista_repeater` |
| `evo_cleavers` | `throwing_axes` | `war_banner` | `bloodstorm_cleavers` |
| `evo_censer` | `holy_flail` | `plate_armor` | `morningstar_halo` |

Fire Oil Flask and Consecrated Ground have no recipe.

### 2.3 `LevelUpRules`

One asset: `CFG_LevelUpRules`.

**(a) Already in code:** `cardsShown` int, `fourthCardLuckCoeff` float, `fourthCardMax` float, `ownedWeight` float, `newWeight` float, `evoBaseWeightMult` float, `skipCrowns` int, `skipHealPct` float, `fallbackCards` List\<PickupType\>, `chestRollChances` float[].

**(b) Add:** `chestSpinSeconds`, `rerollPerChest` (§0.1).

**(c) Code defaults:**

| Field | Code default | GDD | Action |
|---|---|---|---|
| `cardsShown` | 3 | 3 | keep |
| `fourthCardLuckCoeff` | 0.10 | 0.10 | keep. Chance = `0.10 × (luck − 1) / 0.15`, cap `fourthCardMax`. |
| `fourthCardMax` | 0.5 | 0.5 | keep |
| `ownedWeight` | 1.5 | 1.5 | keep |
| `newWeight` | 1.0 | 1.0 | keep |
| `evoBaseWeightMult` | 1.5 | ×1.5 on the base-weapon card when you own the recipe's passive | keep |
| `skipCrowns` | 3 | 3 | keep |
| `skipHealPct` | 0.05 | 0.05 of max HP | keep |
| `chestRollChances` | 0.75, 0.20, 0.05 | 1 / 3 / 5 upgrades | keep |
| `fallbackCards` | RoastChicken, CoinPouch | "Gold Pouch" then "Roast Chicken" | Store **CoinPouch, RoastChicken** so the list follows the GDD sentence. The initializer order is the reverse. |

Set `chestSpinSeconds` **1.5**, `rerollPerChest` **1**.

Rules that stay in code, with no new number:

- Card pool: owned weapons and passives under max level, plus a new weapon or passive if that slot type has room. Banished items stay out. The same item never appears twice in one roll.
- Skip is unlimited (§3.1 `skips` −1, §4.3). §8.4 says the skip counter goes down. That sentence disagrees with §3.1 and §4.3. Implement unlimited. Do not add `PlayerStats.skips`: §9.11 and `PlayerStats.cs` both omit it, and a −1 counter is not read by anything.
- Gold Pouch **+25** crowns and Roast Chicken **30** HP already match `LevelUpService.GoldPouchCrowns` and `RoastChickenHeal`. Do not add fields and do not point the fallback card at the brazier Coin Pouch, which is **+10**. `PickupType.CoinPouch` is the code's token for the Gold Pouch card title.
- "Luck multiplies the chance of 3 and 5" (§4.3) has **no coefficient**. Do not add one.
- Same item never twice. Owned weight 1.5, new weight 1.0. 4th card only from the luck formula above. Base luck 1.0 gives 0.
- Armory ranks add rerolls and banishes through `meta_reroll` / `meta_banish` (`valuePerRank` 1), not through this asset.

### 2.4 `MetaUpgrade`

**(a) Already in code:** `id` string, `displayName` string, `stat` StatType, `op` ModifierOp, `valuePerRank` float, `maxRank` int, `baseCost` int crowns, `costGrowth` float.

`GetCost(rank)` is already `round-to-5 of baseCost × rank × (1 + costGrowth × (rank − 1))`. Do not store the per-rank costs. `costGrowth` is **0.25** on every row. Refunds are free (§6.2); that is a rule, not a field.

**(b) Add:** none.

**(c) Code defaults:** initializer is `meta_might` (Might, PercentAdd, 0.05, maxRank 5, baseCost 100, costGrowth 0.25). Correct for `META_Might` only.

`displayName` is a UI label (†). The GDD names the `id` and the `stat`, and only exemplifies displayName "Might".

| id | displayName † | stat | op | valuePerRank | maxRank | baseCost | costGrowth | Costs (check GetCost) | Total | At start |
|---|---|---|---|---|---|---|---|---|---|---|
| `meta_might` | Might | Might | PercentAdd | 0.05 | 5 | 100 | 0.25 | 100/250/450/700/1000 | 2500 | yes |
| `meta_armor` | Armor | Armor | Add | 1 | 3 | 150 | 0.25 | 150/375/675 | 1200 | yes |
| `meta_maxhp` | Max Health | MaxHealth | PercentAdd | 0.10 | 3 | 120 | 0.25 | 120/300/540 | 960 | yes |
| `meta_recovery` | Recovery | Recovery | Add | 0.2 | 5 | 100 | 0.25 | 100/250/450/700/1000 | 2500 | yes |
| `meta_cooldown` | Cooldown | Cooldown | PercentAdd | -0.025 | 2 | 300 | 0.25 | 300/750 | 1050 | yes |
| `meta_area` | Area | Area | PercentAdd | 0.05 | 2 | 200 | 0.25 | 200/500 | 700 | yes |
| `meta_speed` | Move Speed | MoveSpeed | PercentAdd | 0.05 | 2 | 150 | 0.25 | 150/375 | 525 | yes |
| `meta_magnet` | Magnet | PickupRadius | PercentAdd | 0.15 | 2 | 100 | 0.25 | 100/250 | 350 | yes |
| `meta_growth` | Growth | Growth | PercentAdd | 0.03 | 5 | 120 | 0.25 | 120/300/540/840/1200 | 3000 | yes |
| `meta_greed` | Greed | Greed | PercentAdd | 0.10 | 5 | 100 | 0.25 | 100/250/450/700/1000 | 2500 | yes |
| `meta_luck` | Luck | Luck | PercentAdd | 0.05 | 3 | 150 | 0.25 | 150/375/675 | 1200 | yes |
| `meta_revival` | Revival | Revivals | Add | 1 | 1 | 1000 | 0.25 | 1000 | 1000 | no, `unl_meta_revival` ReachLevel 30 |
| `meta_reroll` | Reroll | Rerolls | Add | 1 | 3 | 250 | 0.25 | 250/625/1125 | 2000 | yes |
| `meta_banish` | Banish | Banishes | Add | 1 | 3 | 200 | 0.25 | 200/500/900 | 1600 | yes |
| `meta_amount` | Amount | Amount | Add | 1 | 1 | 2500 | 0.25 | 2500 | 2500 | no, `unl_meta_amount` EvolveWeapon 1 |

Grand total **23585**. Gate the two locked rows with `UnlockDefinition` (`unlocksMetaUpgrade`). There is no `unlockedByDefault` field on `MetaUpgrade`.

---

## 3. Enemies

### 3.1 `EnemyDefinition`

**(a) Already in code:** `id` string, `displayName` string, `tier` EnemyTier, `behavior` EnemyBehavior, `maxHealth` float HP, `hpPerMinute` float fraction/min, `moveSpeed` float m/s, `contactDamage` float HP, `damagePerMinute` float fraction/min, `contactInterval` float s, `xpValue` int, `mass` float, `knockbackResist` float 0–1, `radius` float m, `prewarmCount` int, `keepDistanceMin` float m, `keepDistanceMax` float m, `firstMinute` float min, `crownValue` float crowns, `dropsChest` bool, `attackParams` List\<EnemyAttack\>, `prefab` GameObject.

**(b) Add:** `phase2HpFraction`, `phase2MoveSpeed`, `enrageDamageMult`, `enrageSpeedMult` (§0.1). Only the Rotting King sets them. Other enemies stay **0**.

**(c) Code defaults that differ:**

| Field | Code initializer | GDD |
|---|---|---|
| `radius` | `0` | Per enemy below. Shambler is **0.3**, not 0. The factory does set Shambler and Runner. A menu-created asset does not. |
| `prewarmCount` | `0` | §7.8: 400 / 200 / 60 / 60 / 3 / 1. Factory sets Shambler 400 and Runner 200 only. |
| `id`, `displayName`, `maxHealth`, `moveSpeed`, `contactDamage`, `xpValue`, `mass` | Shambler | Per row. |
| `hpPerMinute` | 0.25 | Matches the four zombies and the Grave Knight. Rotting King is **0**. |
| `damagePerMinute` | 0.05 | Same split. King is **0**. |
| `contactInterval` | 0.5 | Matches §3.1 for every enemy. Store 0.5 on all six, including the King. |
| `crownValue` | 0.05 | Matches normal kills (§6.1). Grave Knight must be **15**. King must be **0** (†, §5): the **150** boss crowns are the win bonus, not this field. Leaving 0.05 on the King pays a normal kill on top of the 150. |
| `tier` / `behavior` | Normal / Chase | Elite/ChargeElite and Boss/Boss as below. |
| `keepDistanceMin` / `Max` | 0 / 0 | Correct except Spitter **7 / 9**. |
| `dropsChest` | false | Grave Knight and Rotting King **true**. |

`contactInterval` is not a column in the §3.5 table. It is the global 0.5 s from §3.1 and the §9.6 example. Store it.

Runtime HP = `maxHealth × (1 + hpPerMinute × minute) × coopHpMult` (`GetHealthAtMinute`). Runtime contact damage = `contactDamage × (1 + damagePerMinute × minute)`. One player: `coopHpMult` 1. Coop arrays already live on `CFG_CoopScaling` and are not retuned here.

| id | displayName | tier | behavior | maxHealth | hpPerMinute | moveSpeed | contactDamage | damagePerMinute | contactInterval | xpValue | mass | knockbackResist | radius | prewarmCount | keepDistanceMin | keepDistanceMax | firstMinute | crownValue | dropsChest |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `shambler` | Shambler | Normal | Chase | 10 | 0.25 | 1.6 | 6 | 0.05 | 0.5 | 1 | 1.0 | 0.0 | 0.3 | 400 | 0 | 0 | 0 | 0.05 | false |
| `runner` | Runner | Normal | Chase | 6 | 0.25 | 3.8 | 4 | 0.05 | 0.5 | 1 | 0.7 | 0.0 | 0.2 | 200 | 0 | 0 | 2 | 0.05 | false |
| `bloater` | Bloater | Normal | ExplodeOnDeath | 40 | 0.25 | 1.3 | 8 | 0.05 | 0.5 | 3 | 2.5 | 0.4 | 0.7 | 60 | 0 | 0 | 4 | 0.05 | false |
| `spitter` | Plague Spitter | Normal | RangedKite | 20 | 0.25 | 2.0 | 5 | 0.05 | 0.5 | 2 | 1.0 | 0.1 | 0.25 | 60 | 7 | 9 | 6 | 0.05 | false |
| `grave_knight` | Grave Knight | Elite | ChargeElite | 300 | 0.25 | 2.6 | 20 | 0.05 | 0.5 | 30 | 6.0 | 0.9 | 0.7 | 3 | 0 | 0 | 5 | 15 | true |
| `rotting_king` | The Rotting King | Boss | Boss | 18000 | 0 | 2.2 | 30 | 0 | 0.5 | 0 | 50 | 1.0 | 1.5 | 1 | 0 | 0 | 15 | 0 † | true |

Boss-only fields on `rotting_king`: `phase2HpFraction` **0.5**, `phase2MoveSpeed` **2.8**, `enrageDamageMult` **2**, `enrageSpeedMult` **1.3**. All four are **0** on the other five enemies.

Checks computed from the formula (not new stats). The minute-1 Shambler is the GDD's own check:

| id | minute | HP |
|---|---|---|
| shambler | 0 | 10 |
| shambler | 1 | 12.5 |
| runner | 2 | 9 |
| bloater | 4 | 80 |
| spitter | 6 | 50 |
| grave_knight | 5 | 675 |
| rotting_king | 15 | 18000 |

§8.1's slice mini-boss (**8000** HP, slam + raise only, no phase 2, at 10:00) is **not** this asset. `ENM_RottingKing` is the 15-minute boss. Do not write 8000 into it. The slice schedule keeps `boss` empty until a slice-specific copy exists.

King `dropsChest` true is the Crown Chest: one guaranteed upgrade or evolution, plus the §6.1 boss bonus of **150** crowns (results reward, not `crownValue`). An elite chest is the normal chest roll. Branch on `tier == Boss`. Do not add a chest-type enum.

### 3.2 `EnemyAttack`

**(a) Already in code:** `name` string, `telegraph` float s, `radius` float m, `range` float m, `width` float m, `arcDegrees` float deg, `angleOffsets` float[] deg, `damage` float HP, `interval` float s, `projectileSpeed` float m/s, `summonEnemy` EnemyDefinition, `summonCount` int, `hpThreshold` float 0–1.

`hpThreshold` default **1** means the attack is allowed at any HP. Below 1 means "only at or under this HP fraction" (the tooltip's phase gate).

**(b) Add:** `knockback`, `damageToEnemies`, `phaseInterval` (§0.1).

**(c) Defaults:** new attack is zeros, except `hpThreshold` 1 and `angleOffsets` empty. `damage` 0 is not a designed hit. Empty `attackParams` is correct for Shambler and Runner.

Unused numeric fields stay **0**. `angleOffsets` empty unless listed. `hpThreshold` **1** unless listed.

| Owner | name | telegraph | radius | range | width | arcDegrees | angleOffsets | damage | damageToEnemies | knockback | interval | phaseInterval | projectileSpeed | summonEnemy | summonCount | hpThreshold |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| bloater | Death Blast | 0.8 | 2.2 | 0 | 0 | 0 | — | 15 | 50 | 0 | 0 | 0 | 0 | — | 0 | 1 |
| spitter | Spit | 0.5 | 0 | 9 | 0.3 | 0 | — | 8 | 0 | 0 | 3.5 | 0 | 8 | — | 0 | 1 |
| grave_knight | Charge | 0.8 | 0 | 8 | 1.4 | 0 | — | 0 † | 0 | 0 | 6 | 0 | 10 | — | 0 | 1 |
| rotting_king | Ground Slam | 1.2 | 4 | 0 | 0 | 0 | — | 35 | 0 | 6 | 8 | 6 | 0 | — | 0 | 1 |
| rotting_king | Raise the Dead | 1.0 | 6 | 0 | 0 | 0 | — | 0 | 0 | 0 | 15 | 0 | 0 | shambler | 12 | 1 |
| rotting_king | Bile Wave | 0.8 | 0 | 10 | 0 | 30 | -40, 0, 40 | 20 | 0 | 0 | 10 | 0 | 0 | — | 0 | 0.5 |

Attack notes:

- Death Blast fires from `ExplodeOnDeath`, once. `interval` 0 is not a cooldown. Telegraph is the 0.8 s swell. Disc radius 2.2.
- Spit only if a player is inside `range` 9. Glob uses `width` 0.3 as its diameter. `projectileSpeed` 8. Line is locked at windup start.
- Charge: windup `telegraph` 0.8, then move `range` 8 at `projectileSpeed` 10. `width` 1.4 is the swath (GDD override of the spec's 1.0 m line). `damage` 0 †: the GDD gives no separate charge hit. Contact damage still applies. Do not copy 20 into `damage`.
- Ground Slam: disc `radius` 4, `interval` 8, `phaseInterval` 6 once HP fraction ≤ 0.5. Do not add a second slam row (it would double-fire). `knockback` 6 is the new field.
- Raise the Dead: `radius` 6 is the ring. `summonCount` 12. Marker size is look (PlaceholderSpec), not a field.
- Bile Wave: three cones via `angleOffsets`. `hpThreshold` 0.5 so it exists only in phase 2. `range` 10, `arcDegrees` 30.

### 3.3 Phase 2 and enrage (Rotting King)

- At HP fraction ≤ **0.5**: `moveSpeed` becomes **2.8** (`phase2MoveSpeed`). Ground Slam `interval` becomes **6**. Bile Wave becomes available.
- At **1200 s**, if the King is alive (§1.2 "Dawn Never Comes"): damage ×**2**, speed ×**1.3**, Shambler spawn rate **6**/s (§4.1). The run does not end. The player can still win.
- Speed stacking is flagged in §5. Store 2.8 and 1.3 separately. Do not store a combined 3.64.

---

## 4. Waves

### 4.1 `SpawnSchedule` (`SPN_FullRun`)

**(a) Already in code:** `runLengthMinutes` float, `bossMinute` float, `boss` EnemyDefinition, `hardCap` int, `ringMinDistance` float m, `ringMaxDistance` float m, `recycleDistance` float m, `moveDirBias` float 0–1, `entries` List\<WaveEntry\>, `events` List\<WaveEvent\>, `coopScaling` CoopScaling.

**(b) Add:** `moveDirConeHalfAngle`, `eventCapOverflow`, `enrageTimeSeconds`, `enrageSpawnRatePerSec`, `enrageEnemy` (§0.1).

**(c) Code defaults that differ from this run:**

| Field | Code initializer | Full run |
|---|---|---|
| `runLengthMinutes` | **10** (comment: slice 10, full game 15) | **15** |
| `bossMinute` | **10** | **15** |
| `boss` | null | `ENM_RottingKing` |
| `hardCap` | 350 | 350 |
| `ringMinDistance` / `ringMaxDistance` | 20 / 24 | 20 / 24 |
| `recycleDistance` | 35 | 35 |
| `moveDirBias` | 0.6 | 0.6 |
| `entries` | empty until the factory writes minutes 0–9 | minutes **0–15** below |
| `events` | empty | §4.3 |
| `coopScaling` | null | `CFG_CoopScaling` |

`SPN_Slice` (factory) is minutes 0–9 with Runner weight only and Bloater/Spitter percent added back onto Shambler, `runLengthMinutes` 10, `bossMinute` 10, `boss` null, `events` empty. That matches §8.1. It is not a bug in the slice. It is the wrong asset for this table.

**WaveEntry (a)** `minute` int, `targetAlive` int, `spawnRatePerSec` float /s, `mix` List\<EnemyWeight\>. **(b)** none. **(c)** `targetAlive` initializer 15 and `spawnRatePerSec` 1 match minute 0 only.

**EnemyWeight (a)** `enemy` EnemyDefinition, `weightPct` float. Weights below are the GDD percents. They sum to 100. Omit a type whose percent is 0 (do not store a 0-weight row).

**WaveEvent (a)** `timeSeconds` float s, `type` WaveEventType, `enemy` EnemyDefinition, `count` int, `pattern` SpawnPattern, `ignoreCap` bool. **(b)** `ringRadius`, `sides`. **(c)** `ignoreCap` initializer is **true**. §5 says only the boss and elites ignore the cap, and swarms may go over by at most 80. The §9.7 sample sets `ignoreCap` true on the 3:30 stampede. Follow §5: swarms **false**, elite and boss **true**. See §6.

Schedule scalars: `runLengthMinutes` 15, `bossMinute` 15, `boss` = Rotting King, `hardCap` 350, `ringMinDistance` 20, `ringMaxDistance` 24, `recycleDistance` 35, `moveDirBias` 0.6, `moveDirConeHalfAngle` 60, `eventCapOverflow` 80, `enrageTimeSeconds` 1200, `enrageSpawnRatePerSec` 6, `enrageEnemy` = Shambler, `coopScaling` = `CFG_CoopScaling`.

### 4.2 Minute rows (`entries`)

`GetEntryIndexForTime` uses the last row whose `minute` ≤ current minute. One row per minute, 0 through 15. Elites and the boss are events, not mix weights.

| minute | targetAlive | spawnRatePerSec | shambler | runner | bloater | spitter |
|---|---|---|---|---|---|---|
| 0 | 15 | 1.0 | 100 | — | — | — |
| 1 | 25 | 1.5 | 100 | — | — | — |
| 2 | 35 | 2.0 | 80 | 20 | — | — |
| 3 | 50 | 2.5 | 75 | 25 | — | — |
| 4 | 60 | 3.0 | 65 | 25 | 10 | — |
| 5 | 70 | 3.5 | 60 | 25 | 15 | — |
| 6 | 85 | 4.0 | 50 | 25 | 15 | 10 |
| 7 | 100 | 4.5 | 45 | 25 | 15 | 15 |
| 8 | 120 | 5.0 | 45 | 25 | 15 | 15 |
| 9 | 140 | 5.5 | 40 | 30 | 15 | 15 |
| 10 | 160 | 6.0 | 40 | 25 | 20 | 15 |
| 11 | 180 | 6.5 | 35 | 30 | 20 | 15 |
| 12 | 200 | 7.0 | 35 | 30 | 20 | 15 |
| 13 | 230 | 7.5 | 30 | 30 | 20 | 20 |
| 14 | 260 | 8.0 | 30 | 30 | 20 | 20 |
| 15 | 40 | 1.5 | 100 | — | — | — |

Minute 15 is the boss trickle: normal mix stops, live enemies stay, Shamblers at 1.5/s with `targetAlive` 40. `firstMinute` still blocks a type from the **mix** before its minute. Events may spawn a type at the time below even when it is not in the mix (elites, boss, stampedes).

Spawner rules (already described in the GDD, no new numbers):

- If `alive < targetAlive`, spawn at `spawnRatePerSec × 2` until the target. Otherwise spawn at `spawnRatePerSec` until `hardCap`.
- Ring 20–24 m. `moveDirBias` 0.6 of spawns go in the ±60° cone (`moveDirConeHalfAngle`).
- Recycle enemies farther than 35 m from every player back onto the ring, in a player's move direction. They keep HP. Recycle is not a spawn.
- Events: swarms may exceed `hardCap` by `eventCapOverflow` (80) and then stop. Elite and Boss use `ignoreCap` true (no cap).
- After `enrageTimeSeconds` 1200, if the boss is alive, Shambler rate becomes 6/s. Keep the minute-15 row's `targetAlive` 40 and 100% Shambler mix. The GDD does not change the 40. Do not add a minute-20 row.

§1.2 "cap 40 live" vs the rule above (keep spawning at `spawnRatePerSec` when already above `targetAlive`, until the hard cap) is an open conflict. §6. Data values stay `targetAlive` 40 and rate 1.5.

### 4.3 Events

Times are seconds from run start. Spawn the boss **once**. `bossMinute` 15 and the Boss event at 900 s are the same moment, not two kings.

| timeSeconds | clock | type | enemy | count | pattern | ignoreCap | ringRadius | sides |
|---|---|---|---|---|---|---|---|---|
| 210 | 3:30 | Swarm | runner | 40 | LineStampede | false | 0 | 0 |
| 300 | 5:00 | Elite | grave_knight | 1 | RingRandom † | true | 0 | 0 |
| 420 | 7:00 | EnableEvolutions | — | 0 | RingRandom † | false | 0 | 0 |
| 480 | 8:00 | Elite | grave_knight | 1 | RingRandom † | true | 0 | 0 |
| 510 | 8:30 | Swarm | bloater | 15 | Cluster | false | 0 | 0 |
| 630 | 10:30 | Elite | grave_knight | 1 | RingRandom † | true | 0 | 0 |
| 720 | 12:00 | Swarm | shambler | 80 | RingFull | false | 16 | 0 |
| 780 | 13:00 | Elite | grave_knight | 1 | RingRandom † | true | 0 | 0 |
| 840 | 14:00 | Swarm | runner | 80 | LineStampede | false | 0 | 2 |
| 900 | 15:00 | Boss | rotting_king | 1 | RingRandom † | true | 0 | 0 |

Elite pattern, the EnableEvolutions pattern, and the boss pattern are †: the GDD names LineStampede, Cluster, and RingFull only for the swarms. `RingRandom` reuses the normal ring and adds no distance.

Horde Ring "closing" has no speed. Those Shamblers use Shambler `moveSpeed` 1.6 and chase. Do not add a close-speed field.

The GDD does not say where the King appears. Do not invent a radius. Use the same "not inside the camera" rule as other spawns (ring 20–24 m). That uses the existing ring, and it is flagged in §5.

---

## 5. Flagged fills (GDD silent or "n/a")

Every † above. Short list so none of them get treated as tuned numbers:

| Item | Value stored | Why it is flagged |
|---|---|---|
| `hitInterval` 0 on Longsword, Crossbow, Throwing Axes, Greatsword, Ballista, Cleavers | 0 | GDD says n/a. Class default 0.5 would otherwise stick. |
| `hitInterval` 0 on Consecrated Ground | 0 | Tick is per-level `cooldown`, not this field. |
| `range` / `arcDegrees` 0 on weapons with no GDD range or arc | 0 | Class default arc 120 and range 6 would be wrong. |
| Greatsword `targetingMode` | PlayerCentered | 360° whirl. GDD does not name the enum. |
| Greatsword `amount` | 1 | Not stated. One whirl. |
| Greatsword `duration` | 0.20 | Not stated. Carried from the Longsword slash. |
| Ballista `area` | 0.3 | Not stated. Carried from Crossbow bolt size. |
| Ballista `duration` | 0 | Not stated. Same as Crossbow projectiles. |
| Ballista `range` | 18 | Not stated. Carried from Crossbow. |
| Ballista / Cleavers / Halo targeting, and Halo damage type | inherited from the base archetype | GDD does not restate them. |
| Cleavers `projectileSpeed` | 12 | Not stated. Carried from Throwing Axes. |
| Cleavers `range` | 6 | Not stated. Assisted-aim default. |
| Cleavers `spreadHalfAngle` | 0 | GDD states ±20 only on Throwing Axes. Not copied. |
| Cleavers `duration` | 0 | Not stated. Same as axes. |
| Cleavers `knockback` | 5 | Not stated. Carried from Axes L8. |
| Halo `cooldown` | 0 | GDD says n/a. 0 means no cycle. |
| Halo `pierce` | -1 | Not stated. Carried from the Flail. |
| Halo `hitInterval` | 0.5 | Not stated. Carried from the Flail's per-mace interval. |
| Halo `projectileSpeed` | 180 | Not stated. Carried from the Flail's orbit. |
| Evolution `description` strings and result-weapon ids other than `executioners_greatsword` | see tables | Labels / ids so the assets have stable keys. Display names are the GDD's. |
| Meta `displayName` column | see §2.4 | UI labels. Balance fields are the other columns. |
| King `crownValue` | 0 | Enemy table is silent. 150 stays the §6.1 win bonus so it is not paid twice. |
| Charge `damage` | 0 | No charge-hit number. Do not copy contact 20. |
| Elite / boss / EnableEvolutions `pattern` | RingRandom | Pattern not stated. |
| Boss spawn | ring 20–24 | Spawn point not stated. Existing ring only. |
| Enrage ×1.3 | multiply the speed in effect at 20:00 | GDD does not say "× base 2.2" vs "× phase-2 2.8". Literal reading: multiply current speed. Do not store 3.64. |
| Enrage ×2 damage | contact and `EnemyAttack.damage` | GDD says "×2 damage" without listing sources. |
| Horde Ring closing | no extra speed | Not stated. |
| Chest luck on the 3- and 5-upgrade rolls | no field | No coefficient in the GDD. Do not add one. |

New **field names** in §0.1 are also flagged. The numbers those fields hold are from the GDD (20°, 1 HP, 10 HP/s, 6 m/s knockback, 50 HP, 6 s, 0.5, 2.8 m/s, ×2, ×1.3, ±60°, +80, 1200 s, 6/s, 16 m, 2 sides, 1.5 s, +1 reroll).

---

## 6. Conflicts between code and the GDD

Already called out in the (c) tables. Collected here:

1. **Skip key.** GDD v0.3 bound Skip to Esc and S. Esc is Pause. S is move-down. v0.4 binds keyboard Skip to X. Graybox `LevelUpScreen` already uses X. Gamepad West is Reroll, so keyboard X must not be copied onto gamepad West. Gamepad Skip stays East.
2. **`SpawnSchedule.runLengthMinutes` / `bossMinute`.** Code defaults and `SPN_Slice` are 10. Full run is 15. Slice is intentional (§8.1). Full run is `SPN_FullRun`.
3. **`EnemyDefinition.radius` and `prewarmCount`.** Initializers are 0. GDD Shambler is 0.3 and 400.
4. **`WeaponDefinition.arcDegrees` 120, `range` 6, `hitInterval` 0.5, `maxLevel` 8.** Longsword-shaped defaults. Wrong on the other weapons and on evolutions. Factory Longsword never writes `hitInterval`, so that asset stays 0.5 where the GDD says n/a.
5. **`WeaponDefinition.GetLevel` fallback** (10 damage, area 2.5, knockback 5) is not a GDD row.
6. **`GameDataFactory.Lvl`** forces pierce −1 and duration 0.2.
7. **Factory content** is the slice subset: Longsword only, three passives, Shambler and Runner, no attacks, no recipes, no meta, schedule minutes 0–9 with Bloater/Spitter folded into Shambler. Those assets match §8.1, not §3–§5.
8. **`ignoreCap`.** Initializer and the §9.7 sample are true. §5 prose: swarms overflow by at most +80; only elites and the boss ignore the cap. This sheet follows §5.
9. **Minute-15 cap.** §1.2 says "cap 40 live". The §5 spawner still emits `spawnRatePerSec` when `alive > targetAlive`, until `hardCap` 350. Data stays target 40 and rate 1.5. Do not add a third number. Supervisor call if the trickle must hard-stop at 40.
10. **Skip counter.** §8.4 says the skip counter goes down. §3.1 and §4.3 say unlimited. Implement unlimited. `PlayerStats` has no `skips` field (§9.11 doesn't either).
11. **Fallback card token.** `PickupType` has `CoinPouch`, not Gold Pouch. Card pays 25 via `GoldPouchCrowns`. Brazier pouch pays 10. Do not share one payout.
12. **`fallbackCards` order.** Initializer is RoastChicken then CoinPouch. GDD names Gold Pouch then Roast Chicken.
13. **Slice boss vs full boss.** §8.1 is 8000 HP at 10:00 with no phase 2. §3.5 is 18000 HP at 15:00 with phase 2 and Bile Wave. One asset (`ENM_RottingKing`) holds the full boss only.
14. **Passive field name.** GDD §9.5 writes the column as `required passive` (with a space). The C# field is `requiredPassive`. Same slot. Use the code name.
15. **Consecrated Ground tick** cannot live in both `hitInterval` and per-level `cooldown`. Use `cooldown`.
16. **Camera lookahead** (not a data table, do not "fix" it from this sheet): GDD §2.1 `lookAhead` 1.2 m. `PlayerCameraGroup.lookaheadEnabled` defaults **false** (Dustin's preference, recorded in code). Leave it.

`LevelUpRules` numeric defaults, `MetaUpgrade` cost formula, `XpCurve` defaults, `CoopScaling` arrays, and `FeedbackSettings` juice numbers match the GDD and are out of scope for this fill except `chestSpinSeconds` and `rerollPerChest`. `FeedbackSettings.maxDecals` defaults to **100** (slice). The GDD's later full-game cap is 48. Not part of these four tables. Do not change it here.

---

## 7. Done when

The Coder can check these by hand. No new balance number appears outside §5.

1. Every asset in §0 exists at that path. `SPN_Slice` and the slice `CFG_RunConfig` still describe the 10-minute slice.
2. Six base weapons have 8 `WeaponLevel` rows each. Four evolutions have 1 row, `maxLevel` 1, `isEvolution` true. Numbers match §1, including † cells.
3. Six passives, four recipes, `CFG_LevelUpRules`, and sixteen meta upgrades match §2. `GetCost` matches the cost column. Locked meta rows are referenced by their unlock assets, not granted at start.
4. Six enemies match §3, including `radius`, `keepDistanceMin` / `keepDistanceMax`, `prewarmCount`, and the attack rows. Shambler and Runner have empty `attackParams`. King HP is 18000, not the slice's 8000.
5. `SPN_FullRun` has minutes 0–15, the §4.3 events, `hardCap` 350, `bossMinute` 15, and `boss` pointing at the King.
6. A run that uses `SPN_FullRun` spawns only Shamblers at 0:00, Runners from 2:00, Bloaters from 4:00, Spitters from 6:00, Grave Knights at 5:00, 8:00, 10:30, and 13:00, and **one** Rotting King at 15:00. Normal mix stops at 15:00. Shamblers continue at 1.5/s. The fields in §0.1 exist on the classes and are set on the assets.
7. Keyboard Skip is X. Esc still pauses. S still moves down. Gamepad Skip is B / East, and gamepad West still rerolls.
