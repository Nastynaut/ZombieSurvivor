# Knight Survivors (working title): Game Design Document v0.3

## Changelog
| Version | Date | Changes |
|---|---|---|
| 0.3 | 2026-10-28 | Final cleanup. **Status: Frozen until Coder PR #1.** The dried decal color is `#7A0A10` everywhere (none of the old dried hex is left). Spitter colorblind cue is bluish green `#009E73` plus the sac shape. §3.5.1 calls the T3 tier Bloater. Adopted PlaceholderSpec numbers where the GDD was silent: telegraph sizes, scorch life, decals per frame, pool sizes (§3.5.2, §7.8), with overrides noted. Spitter spit range is explicit: 9 m, 8 m/s, 0.5 s telegraph. New `EnemyAttack` fields: `range`, `width`, `arcDegrees`, `angleOffsets`. New `EnemyDefinition` fields: `keepDistanceMin` / `keepDistanceMax`. New `FeedbackSettings` field: `maxNewDecalsPerFrame`. All §13 rows closed. |
| 0.2.2 | 2026-10-28 | The Supervisor ruled on §13 items 1-7 (DECIDED). Item 8, telegraph look, stays open with the Asset Designer. **Standing rule:** the GDD is canonical for numbers and behavior, and PlaceholderSpec is canonical for look. Changes: all pools can grow, with an enemy prewarm table (§7.8) and a new `prewarmCount` field. Hit flash is a material swap held for `flashMs`. Blood bursts use the GDD formula. Slice decals are 100 projectors with a 20 s life and a 4 s fade, using the spec's drying look, fresh `#B3121B` drying to `#7A0A10`. Blood Off keeps scorch marks (max 50). The perf test uses the minute-14 mix. Corpses sink and fade. The player-hurt flash is HP red `#E23A3A`. Per-enemy `radius` values are added (§3.5, §9.6). New fields are in §9.6 and §9.12. |
| 0.2.1 | 2026-10-28 | Aligned with `docs/PlaceholderSpec.md` (readability, per Supervisor request): XP gems are cyan / green / gold; Plague Spitter type color is pink `#FF4FB3`; all attack telegraphs are pulsing orange-red `#FF6A3D`, never blood red; play-area braziers are emissive only, with no real lights; the enemy color and silhouette table is adopted in §3.5.1 (naming reconciled). The gameplay numbers that differ from PlaceholderSpec are kept and listed in §13. |
| 0.2 | 2026-10-28 | Folded in Dustin's answers to the v0.1 open questions. **Art:** stylized low-poly, Synty POLYGON look, per `docs/ArtDirection.md`; placeholder art only, no asset purchases yet. **Working defaults:** Assisted aim, 4 weapon + 4 passive slots, 15:00 run + boss. **Roster:** launch with Sir Aldric only; `CharacterDefinition` SO added; Cuthbert and Maren moved to Future content (§12); character unlocks replaced with passive and Armory unlocks (meta total unchanged at 23,585 crowns). **Platform:** PC only; Steam Deck is not a target. **Business model:** one-time $4.99, no DLC, cosmetics, or microtransactions. **Co-op (later):** both couch and online; §11 rewritten. **§7 aligned with ArtDirection §4** (decal hybrid, pool sizes, damage-number pool). Open questions cleared. |
| 0.1 | 2026-10-28 | First draft |


| Field | Value |
|---|---|
| Version | 0.3 (2026-10-28) |
| Status | **Frozen until Coder PR #1.** Changes after that go through the Supervisor. |
| Owner | Game Designer (for Dustin Shelton) |
| Doc relationships | **Decided (Supervisor):** this GDD is **canonical for numbers and behavior**: stats, timings, counts, caps, pool sizes, colliders and rules. `docs/PlaceholderSpec.md` is **canonical for look**: shapes, colors, materials and silhouettes. `docs/ArtDirection.md` sets the final-art direction. When the docs disagree, numbers come from here and look comes from PlaceholderSpec. See §13. |
| Engine | **Decided:** Unity 6 LTS, URP |
| Platform | **Decided:** PC only (Windows, Steam). KB+M and gamepad. Steam Deck is **not** a target. |
| Business model | **Decided:** one-time purchase at **$4.99**. No DLC, no cosmetics, no microtransactions. All meta progression is earned in game. |
| Art | **Decided:** stylized low-poly, Synty POLYGON look. See `docs/ArtDirection.md` (Asset Designer), which owns palette, lighting, and blood rendering tech (numeric budgets live in §7.8). Placeholder or graybox art for now; **no asset purchases**. |
| Launch roster | **Decided:** one knight, **Sir Aldric**. The character system stays data-driven (`CharacterDefinition`, §9.11) so more characters can be added later (§12). |
| Working defaults | **Decided:** Assisted aim, 4 weapon + 4 passive slots, 15:00 run + boss |
| Players | **Decided:** single player first. Co-op comes later, as **both couch and online**. Every system is built to work with any number of players (see §11). |
| Camera | **Decided:** isometric (spec in §2) |
| Gore | **Decided:** blood and splatter only. No dismemberment, no gibs. |
| Testing | **Decided:** Dustin tests the builds by hand. Acceptance criteria are written as manual checks (§8). |

Units used everywhere: meters (m), seconds (s), m/s, HP. Percentages are stored as fractions (`0.10` = +10%). Column names in the content tables match the C# field names in the §9 schema, so they map 1:1 onto ScriptableObjects.

---

## 0. Pitch and pillars

**Pitch.** You are a lone knight in a cursed, plague-ridden countryside, and night is falling. The zombies don't stop coming. Your weapons swing and fire on their own. You steer, dodge, and choose how your build grows: one of three upgrades each level, until a longsword becomes an executioner's greatsword that clears the field in a red arc. Survive 15 minutes, kill the Rotting King, and spend the crowns you looted to come back stronger. It plays like Vampire Survivors, in 3D, under an isometric camera like Spell Brigade's, and every hit has weight.

**Business model.** $4.99 one-time purchase on Steam (PC). No DLC, cosmetics, or microtransactions. Every upgrade and unlock is earned by playing.

**Pillars**
1. **Every hit lands.** Hit-stop, knockback, a hit flash, and blood make each swing feel heavy, even when the screen holds 300 enemies.
2. **The horde is the threat.** Single zombies are weak. Numbers, crowding, and the pacing curve create the pressure.
3. **One more run.** Runs take 15 minutes, upgrade choices are readable, evolutions feel like breakthroughs, and the meta progression is always within reach.
4. **Simple input, deep build.** Attacks are automatic. You control movement, one dodge, and optionally the aim. The depth lives in your choices.

---

## 1. Core loop and meta loop

### 1.1 Core loop (inside a run, seconds to minutes)
Move → weapons auto-fire → zombies die and drop XP gems → collect gems (magnet) → level up → **pick 1 of 3** → the build gets stronger → more zombies spawn → elites drop chests → chests grant upgrades or **evolutions**.

### 1.2 Run structure (15:00 + boss)
| Time | Beat |
|---|---|
| 0:00 | Spawn with the starting weapon (character-specific; Longsword for Sir Aldric). Only Shamblers. |
| 2:00 to 4:00 | Runners, then Bloaters, join. First swarm event at 3:30. |
| 5:00 | First elite (Grave Knight), which drops a chest. |
| 7:00+ | Chests can now evolve weapons. |
| 8:00 / 10:30 / 13:00 | Elites with chests. |
| 15:00 | Normal spawns stop. Live enemies stay. **The Rotting King** spawns. Shamblers trickle in at 1.5/s (cap 40 live). |
| Boss dead | **Win.** The boss drops the Crown Chest (one guaranteed upgrade or evolution, +150 crowns). After a 5 s victory slow-mo, the Results screen shows. |
| 20:00 (boss still alive) | "Dawn Never Comes": the boss enrages (×2 damage, ×1.3 speed) and Shamblers spawn at 6/s. You can still win. |
| Player HP 0 | **Death** (unless a Revival charge is left: revive at 50% HP, 2 s of i-frames, and a 6 m knockback shockwave). Results screen. |

**Win or death rewards:** you keep **100% of the crowns earned** in both cases, and kills and time count toward unlocks. A win also adds the boss bonus and unlocks the next tier (see §6.3).

### 1.3 Meta loop (between runs)
Results → crowns banked → **Armory** (permanent upgrades) → **Unlocks** (weapons, passives, and Armory upgrades unlock from milestones) → next run as Sir Aldric. A character-select step exists in code but is skipped while the roster has one entry.

---

## 2. Controls and camera

### 2.1 Camera (decided: isometric)
| Field | Value | Notes |
|---|---|---|
| `projection` | Perspective | We use a perspective camera with a narrow FOV, not orthographic. The narrow FOV still reads as isometric, but it keeps depth cues on 3D models and gives cleaner URP decal and particle sorting. |
| `fieldOfView` | 30° (vertical) | Low distortion. |
| `pitch` | 50° | Looking down |
| `yaw` | 45° | Fixed. The player can't rotate the camera. |
| `distance` | 26 m | From the focus point, which gives a height of about 19.9 m |
| Visible ground at 16:9 | ≈ 26 m × 17 m | The view's half-diagonal is about 16 m, so spawn ring A starts at 20 m |
| Follow | Cinemachine 3 `CinemachinePositionComposer`, damping 0.25 s (x/z), 0 s (y) | |
| `lookAhead` | 1.2 m in the move direction, smoothing 0.4 s | |
| Dead zone | none | |
| Zoom | none at launch. Co-op target group zooms out to ×1.3 at most (§11). | |
| Occlusion | Props between the camera and the player use a dither-fade to 30% alpha | |

Movement is camera-relative: W moves up-screen, which is the world direction (−1, 0, 1) normalized at yaw 45°.

### 2.2 Auto vs manual
| Action | Mode | Why |
|---|---|---|
| Weapons fire | **Auto** (on cooldown) | This is the core of the genre. The player watches the horde, not a fire button. |
| Aim of directional weapons (Longsword, Throwing Axes) | **Assisted** (decided working default): mouse cursor direction / right stick if held, otherwise the nearest enemy within 6 m, otherwise the move direction. Option: **Full Auto** (always nearest, then move direction). | Lets skilled players control where the big slash goes without making aim mandatory. |
| Crossbow, Fire Flask | Always auto-targeted (nearest / random enemy in range) | Readability |
| Dodge roll | **Manual** | The one active skill. It gives skill expression and an escape from encirclement. |
| Level-up choice | Manual (the game pauses) | |

### 2.3 Dodge roll (in PlayerStats)
`dodgeDistance` 4.0 m over `dodgeDuration` 0.30 s, `dodgeIFrames` 0.30 s, `dodgeCooldown` 3.0 s, 1 charge. You can pass through enemies while rolling.

### 2.4 Input map (Unity Input System, action map `Gameplay` + `UI`)
| Action | KB+M | Gamepad |
|---|---|---|
| Move | WASD / arrows | Left stick |
| Aim (assisted) | Mouse position (raycast to the ground plane) | Right stick (deadzone 0.25) |
| Dodge | Space or Left Shift | A / South, or RB |
| Pause | Esc | Start |
| Stats overlay (hold) | Tab | Select / View |
| Level-up: pick option 1/2/3 | Mouse click or 1/2/3 | D-pad/stick + A |
| Level-up: Reroll | R | X / West |
| Level-up: Banish (then pick a card) | B | Y / North |
| Level-up: Skip | Esc / S (in UI) | B / East |

---

## 3. Starter content

### 3.1 Player base stats (`PlayerStats`, referenced by `CharacterDefinition` `sir_aldric`)
| Field | Value | Notes |
|---|---|---|
| `maxHealth` | 120 | |
| `recovery` | 0 HP/s | |
| `armor` | 0 | Flat reduction per hit, minimum 1 damage taken |
| `moveSpeed` | 5.0 m/s | Runners move at 3.8, so you can outrun them |
| `might` | 1.00 | Damage multiplier |
| `area` | 1.00 | Multiplies weapon area/radius |
| `cooldown` | 1.00 | Multiplies weapon cooldown (lower is faster). Floor 0.40. |
| `amount` | 0 | Extra projectiles/instances added to every weapon |
| `duration` | 1.00 | |
| `projectileSpeed` | 1.00 | |
| `critChance` | 0.05 | |
| `critMultiplier` | 2.0 | |
| `pickupRadius` | 2.5 m | Magnet |
| `luck` | 1.00 | Affects the 4th-card chance and chest roll |
| `growth` | 1.00 | XP multiplier |
| `greed` | 1.00 | Crown multiplier |
| `revivals` | 0 | |
| `rerolls` | 2 | Per run |
| `banishes` | 1 | Per run |
| `skips` | unlimited (-1) | |
| `dodgeDistance` / `dodgeDuration` / `dodgeIFrames` / `dodgeCooldown` | 4.0 / 0.30 / 0.30 / 3.0 | |
| `weaponSlots` / `passiveSlots` | 4 / 4 | **Decided working default.** The pool is 6+6, so slots are limited to force build choices. Revisit 6/6 when the pool reaches about 12+12. |

Damage formula: `hit = weaponLevel.damage × might × (crit ? critMultiplier : 1)`. Enemy takes `hit`. Player takes `max(1, enemyContactDamage − armor)`. Each enemy can deal contact damage once every `contactInterval` 0.5 s.

### 3.2 Weapons (`WeaponDefinition`)
| id | displayName | archetype | targetingMode | damageType | knockback (m/s) | projectileSpeed (m/s) | hitInterval (s) | maxLevel | Behavior |
|---|---|---|---|---|---|---|---|---|---|
| `longsword` | Longsword | MeleeArc | AimDirection | Physical | 6 | n/a | n/a | 8 | A 120° arc slash with radius `area`. Amount 2 adds a rear slash, amount 3 adds side slashes. |
| `crossbow` | Crossbow | Projectile | NearestEnemy | Physical | 3 | 25 | n/a | 8 | Bolts go to the nearest enemies, one bolt per distinct target. Range 18 m. |
| `holy_flail` | Holy Flail | Orbit | PlayerCentered | Holy | 5 | 180°/s (orbit) | 0.5 per mace per enemy | 8 | Maces orbit at radius `area` for `duration`, then go dormant until the cooldown ends (cooldown includes duration). |
| `throwing_axes` | Throwing Axes | LobbedProjectile | AimDirection | Physical | 4 | 12 | n/a | 8 | Axes arc upward, spread ±20° toward the aim, then fall through enemies. Size scales with `area`. |
| `fire_flask` | Fire Oil Flask | GroundZone | RandomEnemyInRange (10 m) | Fire | 0 | 10 (lob) | 0.5 tick | 8 | A burning pool of radius `area` for `duration`. Damages every tick. |
| `consecrated_ground` | Consecrated Ground | Aura | PlayerCentered | Holy | 1 | n/a | = cooldown (tick) | 8 | A permanent ring around the player that ticks damage. |

Rules: `pierce` = the most enemies one projectile or instance can hit (-1 = unlimited). `area` = range/radius in m before the `area` multiplier. `duration` in s (0 = instant). `cooldown` in s, multiplied by the player's `cooldown`.

#### 3.2.1 Per-level tables (`WeaponLevel`, absolute values, not deltas)

**Longsword** (arc 120°)
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

**Crossbow** (range 18 m)
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

**Holy Flail** (orbit 180°/s; the cooldown includes the active duration)
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

**Fire Oil Flask** (damage is per 0.5 s tick)
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

**Consecrated Ground** (here `cooldown` is the tick interval and `duration` is 0, meaning permanent)
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

The `amount` stat adds to `amount` for every archetype except Aura, where it is ignored.

### 3.3 Passives (`PassiveDefinition`, maxLevel 5)
| id | displayName | stat (StatType) | op | valuePerLevel | Max total | Notes |
|---|---|---|---|---|---|---|
| `whetstone` | Whetstone | Might | PercentAdd | 0.10 | +50% | Evolves Longsword |
| `plate_armor` | Plate Armor | Armor | Add | 1 | +5 | Evolves Holy Flail |
| `prayer_beads` | Prayer Beads | Cooldown | PercentAdd | -0.06 | -30% | Evolves Crossbow |
| `war_banner` | War Banner | Area | PercentAdd | 0.10 | +50% | Evolves Throwing Axes |
| `spurs` | Knight's Spurs | MoveSpeed | PercentAdd | 0.07 | +35% | |
| `lodestone` | Lodestone | PickupRadius | PercentAdd | 0.25 | +125% (2.5 → 5.6 m) | |

### 3.4 Evolutions (`EvolutionRecipe`)
Condition for all of them: the base weapon is at **level 8**, you **own** the required passive (any level), and you open a **chest at or after 7:00**. The evolved weapon replaces the base weapon in its slot. The passive stays.

| id | baseWeapon | requiredPassive | conditionType | minMinute | resultWeapon | Result stats |
|---|---|---|---|---|---|---|
| `evo_greatsword` | longsword | whetstone | ChestAfterMinute | 7 | Executioner's Greatsword | 360° whirl, damage 40, cooldown 0.90, area 4.2, knockback 10, pierce -1. Kills heal 1 HP (at most 10 HP/s). |
| `evo_ballista` | crossbow | prayer_beads | ChestAfterMinute | 7 | Ballista Repeater | damage 22, cooldown 0.50, amount 5, pierce 5, speed 35. Bolts knock enemies back 8. |
| `evo_cleavers` | throwing_axes | war_banner | ChestAfterMinute | 7 | Bloodstorm Cleavers | damage 45, cooldown 1.2, amount 6, pierce -1, area 1.2. Axes boomerang back. |
| `evo_censer` | holy_flail | plate_armor | ChestAfterMinute | 7 | Morningstar Halo | damage 22, permanent orbit (duration 0, cooldown n/a), amount 5, area 3.2, knockback 8 |

Each evolved weapon is a `WeaponDefinition` with a single `WeaponLevel` row (maxLevel 1, `isEvolution = true`).

### 3.5 Enemies (`EnemyDefinition`)
`radius` is the body radius used for separation, contact and hit checks. It is set by data, never by the model. Runtime HP = `maxHealth × (1 + hpPerMinute × minute) × coopHpMult`. Runtime contact damage = `contactDamage × (1 + damagePerMinute × minute)`.

| id | displayName | maxHealth | hpPerMinute | moveSpeed (m/s) | contactDamage | damagePerMinute | xpValue | mass | knockbackResist (0-1) | radius (m) | behavior | firstMinute | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `shambler` | Shambler | 10 | 0.25 | 1.6 | 6 | 0.05 | 1 | 1.0 | 0.0 | 0.3 | Chase | 0 | Baseline fodder |
| `runner` | Runner | 6 | 0.25 | 3.8 | 4 | 0.05 | 1 | 0.7 | 0.0 | 0.2 | Chase | 2 | Fast and fragile |
| `bloater` | Bloater | 40 | 0.25 | 1.3 | 8 | 0.05 | 3 | 2.5 | 0.4 | 0.7 | ExplodeOnDeath | 4 | On death: 0.8 s swell telegraph (belly scales up, band flashes, orange-red 2.2 m disc), then a 2.2 m blast. 15 damage to the player, 50 damage to other enemies. |
| `spitter` | Plague Spitter | 20 | 0.25 | 2.0 | 5 | 0.05 | 2 | 1.0 | 0.1 | 0.25 | RangedKite | 6 | Keeps 7-9 m away (`keepDistanceMin/Max`). **Spit:** every 3.5 s, if a player is within **9 m** (`range`): a 0.5 s telegraph (sac pulse + orange-red 9 m × 0.3 m aim line, locked at windup start), then a bile glob (Ø0.3 m) at 8 m/s along the line. 8 damage. It flies up to 9 m (≈1.1 s) or until it hits. |
| `grave_knight` (Elite) | Grave Knight | 300 | 0.25 | 2.6 | 20 | 0.05 | 30 | 6.0 | 0.9 | 0.7 | ChargeElite | 5 | Every 6 s: a 0.8 s windup (orange-red telegraph line, 8 m × 1.4 m), then an 8 m charge at 10 m/s. Drops a chest. Model at 1.6× scale. |
| `rotting_king` (Boss) | The Rotting King | 18000 | 0 | 2.2 | 30 | 0 | 0 | 50 | 1.0 | 1.5 | Boss | 15 | See below |

**Telegraph rule (all attacks):** telegraphs use the shared pulsing additive orange-red `#FF6A3D` (alpha 0.3 ↔ 0.7 at 4 Hz). They are never blood red `#B3121B`, because blood never pulses or glows, and that's how players tell the two apart.

#### 3.5.1 Enemy color and silhouette table (adopted from `docs/PlaceholderSpec.md` §3)
| id (GDD / data) | displayName | ArtDirection tier name | Placeholder prefab | Type color (eyes + collar band) | Height × width | Iso silhouette (shape first, color second) |
|---|---|---|---|---|---|---|
| `shambler` | Shambler | T1 | `PF_PH_Zombie_Shambler` | Green `#8BD346` | 1.7 × 0.55 m | Narrow, hunched "\|" with two long arms straight forward, head tipped forward |
| `runner` | Runner | T2 | `PF_PH_Zombie_Runner` | Yellow `#FFD23F` | 1.6 × 0.40 m, 30° lean | Thin diagonal "/" leaning hard forward, arms swept **back** (optional eye trail) |
| `bloater` | Bloater | T3 Bloater | `PF_PH_Zombie_Bloater` | Violet `#9B5DE5` | 1.5 × 1.4 m | Big round "O" blob on stubby legs, tiny head, violet belt band. The only round enemy. |
| `spitter` | Plague Spitter | T2b Plague Spitter | `PF_PH_Zombie_Spitter` | Pink `#FF4FB3` (colorblind mode: bluish green `#009E73` + the throat-sac shape; white stays the elite color) | 1.7 × 0.5 m | Upright with no lean, a large glowing throat sac in front of the chest, arms out sideways. "Lollipop/lantern" shape. |
| `grave_knight` | Grave Knight (elite) | Elite | `PF_PH_Elite_GraveKnight` | Cyan-white `#7FF3FF` | 2.9 × 1.4 m (1.6× scale) | Huge dark armored knight, tall helmet spike, angular cube pauldrons, no team color. Casts a shadow. |
| `rotting_king` | The Rotting King (boss) | Boss | `PF_PH_Boss_RottingKing` | Cyan-white `#7FF3FF`, pulsing | ~4.0 m (2.2× scale) | Enormous bloated mass, gold crown ring of spikes, long dragging arms. Casts a shadow. |

**Naming:** ArtDirection and PlaceholderSpec now use the GDD names: T3 = **Bloater** (Bloater budget, violet, counts as 3 toward the corpse cap) and T2b = **Plague Spitter**. "Elite / boss" covers the Grave Knight and the Rotting King. The data `id`s are canonical. For the colorblind palette of the other types, see ArtDirection §7.2.

#### 3.5.2 Telegraph shapes (numbers here; look per PlaceholderSpec §3.8)
All telegraphs are locked at windup start (they don't track the player) and despawn when the attack fires.

| Attack | Shape | Size | Timing | Source |
|---|---|---|---|---|
| Spitter spit | Aim line | **9 m long** × 0.3 m wide (glob diameter), from the sac | 0.5 s | Spec width. **Override:** fixed 9 m = spit `range`, not "distance to target, clamped 9", because the glob flies the full 9 m, so the line must show its whole danger path. |
| Grave Knight charge | Charge line | **8 m** × **1.4 m** | 0.8 s | Length kept at 8 m (= charge distance). **Override** of the spec's 1.0 m width: 1.4 m = the elite's body width (2 × `radius` 0.7), so the line matches the swath it hits. |
| Bloater death blast | Disc | 2.2 m radius | 0.8 s swell | Spec (matches the GDD blast) |
| Ground Slam | Disc | 4 m radius | 1.2 s | Spec (matches the GDD) |
| Raise the Dead | 12 markers on a 6 m ring (every 30°) | Each: 1 m disc + 0.6 m upright stake | 1.0 s | Spec. Each Shambler spawns on its marker. |
| Bile Wave | 3 cones | 30° each, 10 m long, centered at **−40° / 0° / +40°** from the aim direction (10° safe gaps) | 0.8 s | Spec |

At minute 1, a Shambler has 12.5 HP. Longsword L1 does 12, or 24 on a crit, so it takes 1-2 hits. At minute 0 it's 1 hit, and a Runner is always 1 hit early on. Crossbow L1 (10 damage) also kills in 1-2 hits. ✔

**The Rotting King (boss) attacks**
| Attack | Telegraph | Effect | Interval |
|---|---|---|---|
| Ground Slam | 1.2 s orange-red circle (4 m radius) | 4 m radius, 35 damage, knocks the player back 6 m/s | 8 s |
| Raise the Dead | 1.0 s roar + 12 orange-red spawn markers (1 m disc + 0.6 m stake) | Spawns 12 Shamblers in a 6 m ring around the player, one on each marker | 15 s |
| Bile Wave (phase 2, ≤50% HP) | 0.8 s orange-red cones | 3 cones of 30°, 10 m long, centered at −40° / 0° / +40° (10° safe gaps), 20 damage | 10 s |
| Phase 2 | at 50% HP | Speed 2.8 m/s, slam interval 6 s | n/a |

**Boss time-to-kill check** (typical build at 15:00, level ~37, 4 weapons, 1-2 evolutions, might ≈ 1.4):

| Weapon | Single-target DPS on the boss |
|---|---|
| Executioner's Greatsword | 40 / 0.9 ≈ 44 |
| Crossbow L8 (all 4 bolts hit) | 4 × 18 / 0.75 ≈ 96 |
| Holy Flail L6 | ≈ 19 |
| Throwing Axes L5 (60% hit rate) | ≈ 30 |
| **Total** | 189 × 1.4 ≈ 265, plus crits (+5%) ≈ 280 DPS |

At 280 DPS, 18,000 HP dies in **≈ 64 s**. A weaker build (200 DPS) takes **90 s**. ✔

---

## 4. XP and level curve

### 4.1 Formula (`XpCurve`)
`xpToNext(L) = 5 + 6·(L−1) + floor(0.1·(L−1)²)`. XP needed for level L = Σ of `xpToNext` for levels 1 through L−1. Beyond level 40 the formula keeps going (there's no cap). XP gained = gem value × `growth`.

| L | xpToNext | cumXP | L | xpToNext | cumXP | L | xpToNext | cumXP | L | xpToNext | cumXP |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 5 | 0 | 11 | 75 | 344 | 21 | 165 | 1478 | 31 | 275 | 3602 |
| 2 | 11 | 5 | 12 | 83 | 419 | 22 | 175 | 1643 | 32 | 287 | 3877 |
| 3 | 17 | 16 | 13 | 91 | 502 | 23 | 185 | 1818 | 33 | 299 | 4164 |
| 4 | 23 | 33 | 14 | 99 | 593 | 24 | 195 | 2003 | 34 | 311 | 4463 |
| 5 | 30 | 56 | 15 | 108 | 692 | 25 | 206 | 2198 | 35 | 324 | 4774 |
| 6 | 37 | 86 | 16 | 117 | 800 | 26 | 217 | 2404 | 36 | 337 | 5098 |
| 7 | 44 | 123 | 17 | 126 | 917 | 27 | 228 | 2621 | 37 | 350 | 5435 |
| 8 | 51 | 167 | 18 | 135 | 1043 | 28 | 239 | 2849 | 38 | 363 | 5785 |
| 9 | 59 | 218 | 19 | 145 | 1178 | 29 | 251 | 3088 | 39 | 377 | 6148 |
| 10 | 67 | 277 | 20 | 155 | 1323 | 30 | 263 | 3339 | 40 | 391 | 6525 |

**Expected pace** (assumes the §5 spawn rates, average XP 1.0 to 1.75 per kill, 85% of gems collected):

| Minute | 1 | 3 | 5 | 8 | 10 | 12 | 15 |
|---|---|---|---|---|---|---|---|
| Level | ~4 | ~9 | ~14 | ~21 | ~26 | ~30 | ~37 |

Max build = 4 weapons × 8 + 4 passives × 5 = 52 levels. Players won't usually max everything, so the choices matter.

### 4.2 XP gems and pickups
| Gem | value | Color | Source |
|---|---|---|---|
| Small | 1 | Cyan `#3FE0FF` (small diamond, 0.22 m) | Shambler, Runner |
| Medium | 5 | Green `#4DFF7A` (tall diamond, 0.45 m) | Merge result. Bloaters drop 3 small. |
| Large | 25 | Gold `#F2C14E` (8-point star, 0.55 m) | Merge result. Elites drop 1 Large + 1 Medium (30). |

Red is reserved for blood and blue for Player 1 (azure `#2F80ED`), so neither is used for gems. Gems are emissive, bob, and spin (PlaceholderSpec §4).

- Magnet: gems inside `pickupRadius` (2.5 m base) fly to the player at 12 m/s, accelerating 20 m/s².
- Ground cap is 400 gems. Past that, a new gem's value is added to the oldest gem farther than 15 m away (which upgrades its tier visually).
- Destructible braziers (12 per map inside the play area, respawn 60 s; **emissive flame only, no real lights**, so they don't warm the floor around the blood) drop: Roast Chicken (heal 30 HP) 50%, Coin Pouch (+10 crowns) 35%, Holy Reliquary (pulls every gem on the map) 15%.

### 4.3 Level-up rules
- The game pauses and shows **3 cards**. You get a **4th card** with chance `0.10 × (luck − 1) / 0.15`, capped at 50%. So base luck 1.0 gives 0%. With meta Luck +15%, the 4th card shows 10% of the time.
- Card pool: upgrades to owned weapons and passives below max level, plus new weapons and passives if there's a free slot of that type. Banished items are excluded.
- Weights: owned upgrade **1.5**, new item **1.0**. If you own a passive that completes an evolution recipe, the recipe's base weapon card gets **×1.5**. The same item never appears twice in one roll.
- **Reroll:** redraws all cards. 2 per run base, +1 per Armory rank (max +3). Each chest also grants +1 reroll.
- **Banish:** removes one item from the pool for the rest of the run. 1 base, +1 per Armory rank (max +3).
- **Skip:** unlimited. Gives +3 crowns and heals 5% of max HP.
- **Everything maxed or pool empty:** cards show "Gold Pouch" (+25 crowns) and "Roast Chicken" (heal 30 HP).
- **Chests:** roll 1 / 3 / 5 upgrades at 75% / 20% / 5%. Luck multiplies the chance of 3 and 5. If you're eligible for an evolution, the first upgrade is always the evolution (the first eligible recipe in list order). The chest UI is an auto-spin of 1.5 s that you can skip.

---

## 5. Spawn and wave pacing (`SpawnSchedule`, one `WaveEntry` per minute)

| minute | targetAlive | spawnRatePerSec | shamblerPct | runnerPct | bloaterPct | spitterPct | events |
|---|---|---|---|---|---|---|---|
| 0 | 15 | 1.0 | 100 | 0 | 0 | 0 | none |
| 1 | 25 | 1.5 | 100 | 0 | 0 | 0 | none |
| 2 | 35 | 2.0 | 80 | 20 | 0 | 0 | none |
| 3 | 50 | 2.5 | 75 | 25 | 0 | 0 | 3:30 Runner Stampede (40, LineStampede) |
| 4 | 60 | 3.0 | 65 | 25 | 10 | 0 | none |
| 5 | 70 | 3.5 | 60 | 25 | 15 | 0 | 5:00 Elite + chest |
| 6 | 85 | 4.0 | 50 | 25 | 15 | 10 | none |
| 7 | 100 | 4.5 | 45 | 25 | 15 | 15 | 7:00 evolutions enabled |
| 8 | 120 | 5.0 | 45 | 25 | 15 | 15 | 8:00 Elite + chest. 8:30 Bloater Herd (15, Cluster). |
| 9 | 140 | 5.5 | 40 | 30 | 15 | 15 | none |
| 10 | 160 | 6.0 | 40 | 25 | 20 | 15 | 10:30 Elite + chest |
| 11 | 180 | 6.5 | 35 | 30 | 20 | 15 | none |
| 12 | 200 | 7.0 | 35 | 30 | 20 | 15 | 12:00 Horde Ring (80 Shamblers, RingFull at 16 m, closing) |
| 13 | 230 | 7.5 | 30 | 30 | 20 | 20 | 13:00 Elite + chest |
| 14 | 260 | 8.0 | 30 | 30 | 20 | 20 | 14:00 Runner Stampede (80, from 2 sides) |
| 15 | 40 | 1.5 | 100 | 0 | 0 | 0 | 15:00 Boss. Normal waves stop. |

**Spawner rules**
- Spawn ring A: **20 to 24 m** from a chosen player, outside the camera view (the view's half-diagonal is about 16 m). 60% of spawns are biased into a ±60° cone in that player's move direction.
- If `alive < targetAlive`, the spawner tops up at `spawnRatePerSec × 2` until it reaches the target. Otherwise it spawns at `spawnRatePerSec`, unless the hard cap is reached.
- **Hard cap: 350 live enemies** (1 player). Events can go over the cap by up to +80 temporarily. The boss and elites ignore the cap.
- Recycling: enemies more than **35 m** from every player teleport to ring A in a player's move direction. They keep their HP. This doesn't count as a spawn.
- Expected kills in a full run: about 4,000.

---

## 6. Meta progression

### 6.1 Currency: **Crowns**
| Source | Amount |
|---|---|
| Per kill (normal) | 0.05 (fractions add up, displayed as an integer) |
| Per elite kill | 15 |
| Per minute survived | 10 |
| Boss kill bonus | 150 |
| Coin Pouch (brazier) | 10 |
| Skip a level-up | 3 |
| Multiplier | All of the above × `greed` |

Typical income: a win ≈ 200 (kills) + 150 (time) + 150 (boss) + 60 (elites) + ~30 (pouches) ≈ **590**. A death at 8:00 ≈ **150**. The average across a learning curve is ≈ **360 per run**, rising to about 450 once Greed is maxed.

### 6.2 Permanent upgrades (`MetaUpgrade`, the Armory)
`cost(rank r) = round5(baseCost × r × (1 + 0.25 × (r − 1)))`

| id | stat | op | valuePerRank | maxRank | baseCost | Costs per rank | Total |
|---|---|---|---|---|---|---|---|
| `meta_might` | Might | PercentAdd | 0.05 | 5 | 100 | 100/250/450/700/1000 | 2500 |
| `meta_armor` | Armor | Add | 1 | 3 | 150 | 150/375/675 | 1200 |
| `meta_maxhp` | MaxHealth | PercentAdd | 0.10 | 3 | 120 | 120/300/540 | 960 |
| `meta_recovery` | Recovery | Add | 0.2 | 5 | 100 | 100/250/450/700/1000 | 2500 |
| `meta_cooldown` | Cooldown | PercentAdd | -0.025 | 2 | 300 | 300/750 | 1050 |
| `meta_area` | Area | PercentAdd | 0.05 | 2 | 200 | 200/500 | 700 |
| `meta_speed` | MoveSpeed | PercentAdd | 0.05 | 2 | 150 | 150/375 | 525 |
| `meta_magnet` | PickupRadius | PercentAdd | 0.15 | 2 | 100 | 100/250 | 350 |
| `meta_growth` | Growth | PercentAdd | 0.03 | 5 | 120 | 120/300/540/840/1200 | 3000 |
| `meta_greed` | Greed | PercentAdd | 0.10 | 5 | 100 | 100/250/450/700/1000 | 2500 |
| `meta_luck` | Luck | PercentAdd | 0.05 | 3 | 150 | 150/375/675 | 1200 |
| `meta_revival` | Revivals | Add | 1 | 1 | 1000 | 1000 | 1000 |
| `meta_reroll` | Rerolls | Add | 1 | 3 | 250 | 250/625/1125 | 2000 |
| `meta_banish` | Banishes | Add | 1 | 3 | 200 | 200/500/900 | 1600 |
| `meta_amount` | Amount | Add | 1 | 1 | 2500 | 2500 | 2500 |
| **Total** | | | | | | | **23,585** |

Refunds are free and unlimited, so players can experiment.

### 6.3 Unlocks (`UnlockDefinition`)
| id | Unlocks | conditionType | threshold |
|---|---|---|---|
| Start | Sir Aldric; Longsword, Crossbow, Holy Flail, Throwing Axes; Whetstone, Plate Armor, Prayer Beads, War Banner; the Armory minus `meta_amount` and `meta_revival` | n/a | n/a |
| `unl_flask` | Fire Oil Flask | KillTotal (lifetime) | 1,500 |
| `unl_consecrated` | Consecrated Ground | SurviveMinutes (one run) | 10 |
| `unl_spurs` | Knight's Spurs (passive) | ReachLevel (one run) | 20 |
| `unl_lodestone` | Lodestone (passive) | KillTotal (lifetime) | 500 |
| `unl_meta_revival` | Armory: Revival upgrade becomes purchasable | ReachLevel (one run) | 30 |
| `unl_meta_amount` | Armory: Amount upgrade becomes purchasable | EvolveWeapon (any) | 1 |
| `unl_plague_night` | Plague Night mode: enemy HP ×1.5, crowns ×1.5 | DefeatBoss | 1 |
| `unl_codex` | Evolution codex entry (shows the recipe) | EvolveWeapon (each) | 1 |

No launch unlock depends on a second character. The unlocks only use the existing `UnlockConditionType` values, so no enum changes are needed. `UnlockDefinition` schema: §9.15.

**Grind target:** full Armory (23,585 crowns, unchanged from v0.1) at about 400 crowns per run is **~55 runs × ~13 min ≈ 12 hours**. All unlocks fall inside that window, so total meta completion is **≈ 12 hours**, all earnable in game.

---

## 7. Visceral feel, in gameplay terms (blood and splatter only)

> **Ownership (decided, standing rule):** this section is **canonical for numbers and behavior**: triggers, timings, counts, caps, pool sizes and lifetimes. **Look** (colors, shapes, materials, particle textures) comes from `docs/PlaceholderSpec.md` for placeholders and `docs/ArtDirection.md` for final art. Where those docs give different numbers, these win (§13).

**Decided:** no dismemberment, no gibs. The visceral feel comes from hit-stop, knockback, hit flash, blood bursts, ground splatter decals, screen shake, damage numbers, and audio.

### 7.1 Hit-stop (`HitFeedbackProfile`)
"Local" freezes only the hit enemy's animation and movement. "Global" sets `Time.timeScale = 0` for real-time ms. Global stops are rate-limited to **one per 150 ms**, and the largest pending one wins.

| hitType | localStopMs | globalStopMs | shakeAmplitude (m) | shakeDuration (s) | flashMs |
|---|---|---|---|---|---|
| Normal | 40 | 0 | 0 | 0 | 80 |
| Crit | 60 | 0 | 0.05 | 0.10 | 100 |
| Overkill (damage ≥ 2× remaining HP) | 0 (dies) | 20 | 0.03 | 0.08 | n/a |
| EliteHit | 50 | 0 | 0.08 | 0.10 | 80 |
| EliteKill | n/a | 120 | 0.40 | 0.35 | n/a |
| BossHit | 30 | 0 | 0 | 0 | 60 |
| BossPhaseChange | n/a | 200 | 0.50 | 0.50 | n/a |
| BossKill | n/a | 300, then 1.5 s at timeScale 0.3 | 0.60 | 0.60 | n/a |
| PlayerHurt | n/a | 60 | 0.25 | 0.20 | 120 (knight flashes HP red `#E23A3A`) |
| Explosion (Bloater) | n/a | 40 | 0.30 | 0.25 | n/a |

Shake uses a trauma model: `trauma += amplitude`, clamped to 1, decaying at 1.5/s. Offset = trauma² × 0.6 m on Perlin noise. Use a Cinemachine Impulse Source. The **Screen Shake** option slider (0-100%, default 100) scales it.

### 7.2 Knockback
- Knockback velocity = `weaponKnockback × (1 − knockbackResist) / mass` m/s. It decays linearly to 0 over **0.15 s**, and is clamped to 12 m/s.
- Enemies use kinematic movement with spatial-hash separation (no Rigidbody per enemy). Knockback is a velocity added in the same step.
- Boss and elite: knockback resist 1.0 / 0.9.

### 7.3 Hit flash and damage numbers
- Hit flash: a **material swap** to the shared flash material (look per PlaceholderSpec: `M_PH_HitFlash`, white). Hold it for the hit type's `flashMs` (80 normal / 100 crit / 80 elite / 60 boss), then swap back. Cache `sharedMaterials` once, so nothing is cloned. A new hit during a flash restarts the timer.
- Player-hurt flash: the same swap on the knight's renderers, to an HP-red `#E23A3A` flash material, for 120 ms. It is never blood red `#B3121B`.
- Later, the final crowd shader (`_HitFlash` / `_FlashAmount`) must keep the same durations.
- Damage numbers: white for normal, yellow ×1.3 scale with a "!" for crits, HP red `#E23A3A` for damage to the player. Lifetime 0.6 s, rising 1 m with an ease-out. Hits on the same target within 0.15 s merge into one number. **Pool of 128** (per ArtDirection §5). When the pool is full, the oldest number is recycled. Option toggle: On / Crits only / Off.

### 7.4 Blood bursts (particles)
| Trigger | Particle count (× bloodIntensity) | Notes |
|---|---|---|
| Any damaging hit | `clamp(4 + 20 × damage / maxHealth, 4, 24)` | Spray in the hit direction, cone 35° |
| Crit | ×1.5 | Plus a short arterial spray (0.25 s) |
| Kill | 12 + a death splatter decal | |
| Overkill kill (≥ 2× remaining HP) | 30, radial | Plus 1 extra large decal |
| Bloater death | 40, radial, green-red bile | Plus a 2.5 m bile decal |
| Elite or boss kill | 80 | Plus 3 decals |

**Decided:** these counts are canonical. Code sets the burst count per spawn (`ParticleSystem.Emit(n)` or by overriding the burst) instead of using the prefab's fixed burst. Each prefab's Max Particles must be at least the largest count it can emit: Hit 24, Crit 36, Kill 80. All counts are × `bloodIntensity`.

- Global budget: **96 active blood systems, about 3,000 live particles** (pooled Shuriken prefabs per ArtDirection §4.1). When the pool is empty, the oldest system is stolen. Blood droplets that hit the ground can spawn a small splat (10% chance, subject to the decal budget).

### 7.5 Ground splatter decals
- **Full game (ArtDirection §4.2 hybrid):** fresh splats use a pool of **48 URP Decal Projectors** that live 4-6 s and look wet. When each one expires, it is stamped into a **blood splat-map render texture** on the ground shader. That gives unlimited persistent blood at a fixed cost, with drying over about 60 s and a reset between runs. Max 64 stamps per frame.
- **Vertical slice (interim, before the splat map exists), decided:** URP Decal Projectors only, pool of **100**. Lifetime **20 s**: 16 s solid, then a 4 s alpha fade. When the pool is full, the oldest decal is recycled. At most **32 new decals per frame** (`maxNewDecalsPerFrame`); the rest are queued. The look follows PlaceholderSpec's drying: **wet** (fresh `#B3121B`, glossy) for the first `decalWetTime` **6 s**, then it swaps to **dry** (`#7A0A10`, matte) for the rest of its life.
- Pooling and merge (slice projectors): if 3 or more decals are already within 1.0 m, the nearest one scales up by 15% (max 2.0×) and its lifetime resets, instead of spawning a new decal. This creates pools in high-traffic areas.
- 6 decal variants with random rotation and scale 0.6-1.4 m. Overkill and elite kills use the large set (1.5-2.5 m).
- Wet sheen: decals write to smoothness for a glossy blood look. They go from fresh `#B3121B` to dried `#7A0A10` (decided).
- Corpses (decided): lie for 3 s, then **sink 0.6 m and dither-fade together** over 1.5 s. Max 60 visible (ArtDirection §4.4). Blood build-up on the knight's sword and armor is per ArtDirection §4.3.

### 7.6 Gore option
**Blood** setting: `Off / Low / Full` (`bloodIntensity` 0 / 0.5 / 1.0, default Full).
- Off: blood bursts become grey dust/ash puffs at the same counts × 0.5. No red decals. Scorch marks are kept (decided): charcoal, max 50 (`maxScorchMarks`), **20 s life** with a 4 s fade like decals, and no drying swap. Bloater bile turns grey.
- Low: half the particles, decal lifetime 10 s.
This is a separate setting from the Screen Shake and Damage Numbers sliders.

### 7.7 Audio layering
| Layer | Content | Rules |
|---|---|---|
| Impact | Metal or blunt transient per damage type | Pitch ±8%, max 8 concurrent voices |
| Flesh | Wet squelch | Played on 50% of hits and every kill. Max 6 voices. |
| Death | Short zombie groan or burst | Max 4 per 100 ms |
| Crit / overkill | Bass thump + extra splash | Ducks the impact layer by −3 dB |
| Player hurt | Armor clank + grunt | Always plays. Duck SFX −6 dB for 0.2 s. |
| Level-up | Choir sting | Music low-pass while paused |

### 7.8 Performance budgets (PC target: 1080p at 60 fps on a GTX 1060 / RX 580 class GPU)
| Item | Budget |
|---|---|
| Live enemies | 350 (hard cap) |
| Enemy rendering | GPU-instanced meshes, vertex-animation-texture or 3-clip LOD animator. 2 LODs. |
| Projectiles (pooled) | 300 |
| Blood systems / particles | 96 / ~3,000 |
| Decals | 48 fresh projectors + splat-map RT (slice: 100 projectors) |
| Corpses | 60 |
| Damage numbers | 128 |
| XP gems on ground | 400 |
| Gameplay CPU (sim + spawn + collision) | ≤ 6 ms/frame at 350 enemies |

**Perf test (decided):** use the **GDD minute-14 mix**, not a fixed tier split. That's 260 alive at Shambler 30 / Runner 30 / Bloater 20 / Spitter 20 %, plus the 80-Runner stampede, pushed to the 350 cap. It also runs with 60 corpses, 96 blood systems and 100 decals. Pass = 60 fps on the PC target.

**Pools (decided):** **every object pool can grow**. The 96-system blood budget is a cap, not a pool limit. A pool never fails to spawn: when it's empty, it creates a new instance and logs a dev warning. Prewarm counts are only a starting size. The budget caps above (blood systems, decals, corpses, damage numbers, gems) are enforced by recycling the oldest when a cap is reached, not by the pool size.

| Pool | Prewarm | Source / note |
|---|---|---|
| Shambler | 400 | `EnemyDefinition.prewarmCount` |
| Runner | 200 | `prewarmCount` (decided) |
| Bloater | 60 | `prewarmCount` (decided) |
| Plague Spitter | 60 | `prewarmCount` (decided) |
| Grave Knight | 3 | `prewarmCount` |
| Rotting King | 1 | `prewarmCount` |
| Bile glob | 32 | Spec size. **Override:** grows past 32 (spec said "cap 32"), because every pool can grow. |
| Telegraph: aim line / charge line / spawn marker / cone | 32 / 4 / 24 / 6 | Spec |
| Telegraph: disc (Bloater + slam, shared) | 32 | **Override** of the spec's 16: a herd of 15 plus area-of-effect kills can start more than 16 swells within 0.8 s. |
| Blood bursts Hit / Crit / Kill | 64 / 24 / 48 | Spec. Capped at 96 active systems (steal the oldest). |
| Blood Big (Bloater, elite, boss kills) | 16 | **Override** of the spec's 8: a Bloater Herd (15) killed at once needs 15. Still inside the 96-system cap. |
| Decal projectors (slice) | 100 | `maxDecals` |
| Scorch marks (Blood Off) | 50 | `maxScorchMarks` |
| XP gems Small / Medium / Large | 400 / 64 / 32 | Spec |
| Damage numbers | 128 | `maxDamageNumbers` |

---

## 8. Vertical slice (about 10 working days, one coder)

### 8.1 In
| Area | Content |
|---|---|
| Map | 1 graybox field, 150 × 150 m, bounded by walls. Scattered props (gravestones, fences), 12 braziers (chicken only; emissive flame, no point lights). Placeholder primitives or free CC0 meshes in the ArtDirection palette (cool grey floor, type-colored enemy rims). **No paid assets.** |
| Character | Sir Aldric (base stats in §3.1). Starts with the Longsword. |
| Camera / input | Isometric camera as in §2.1. KB+M + gamepad, assisted aim, dodge roll. |
| Weapons | Longsword, Crossbow, Holy Flail (all levels 1-8) |
| Passives | Whetstone, Prayer Beads, Plate Armor (levels 1-5) |
| Evolution | Longsword L8 + Whetstone + chest ≥ 7:00 → Executioner's Greatsword |
| Enemies | Shambler, Runner, Grave Knight elite (at 5:00 and 8:00) |
| Boss | **10:00 mini-boss:** the Rotting King with 8,000 HP, Ground Slam + Raise the Dead only, no phase 2. Expected TTK ~50-60 s at ~150 DPS. |
| Run | 10 minutes. Uses §5 rows 0-9, with Bloater/Spitter percentages folded into Shambler. Events: 3:30 Stampede only. |
| Level-up | Pick 1 of 3, 2 rerolls, 1 banish, skip. Slots 3 + 3. |
| Chest | Elite drop, 1 upgrade, or the evolution if eligible |
| Juice | Hit flash, local and global hit-stop, knockback, shake, damage numbers, blood bursts, 100 pooled decals, Blood Off/Low/Full toggle |
| Meta | Crowns earned and saved to disk (JSON). Armory with 3 upgrades: Might, MaxHealth, PickupRadius. |
| UI | HUD (HP, XP bar, level, timer, kills, crowns), level-up cards, pause/options (Blood, Shake, Damage numbers, aim mode), results screen |
| Audio | Placeholder impact, death, and level-up SFX |

### 8.2 Cut from the slice
Throwing Axes, Fire Flask, Consecrated Ground; Spurs and Lodestone; 3 of the 4 evolutions; Bloater, Spitter; boss phase 2 and Bile Wave; Horde Ring and Bloater Herd events; Synty or other paid assets; character select; unlocks; codex; Plague Night; revival; luck / 4th card; chest 3/5 rolls; rebinding; co-op; music; final art.

### 8.3 Effort estimate
| Day | Feature |
|---|---|
| 1 | Player controller, iso camera rig, Input System map (KB+M and gamepad), `PlayerStats` SO, `PlayerRegistry` (list) |
| 2 | `EnemyDefinition` SO, pooled enemies, chase + spatial-hash separation, contact damage, `SpawnSchedule`/`WaveEntry` spawner with ring + hard cap + recycle |
| 3 | Weapon framework (`WeaponDefinition` / `WeaponLevel`, archetype runners), Longsword (MeleeArc), Crossbow (Projectile), damage pipeline (might, crit, armor) |
| 4 | Holy Flail (Orbit), knockback, hit flash, damage-number pool |
| 5 | XP gems + magnet + merge cap, `XpCurve`, level-up UI (pick 1 of 3, weights, reroll, banish, skip) |
| 6 | Passives + stat aggregation + slots. Elite (charge), chest, `EvolutionRecipe` → Greatsword |
| 7 | Mini-boss (slam, summon), win/death flow, results screen, crowns |
| 8 | Juice pass: hit-stop manager, trauma shake, blood burst pool, decal pool + merge, Blood/Shake/Numbers options |
| 9 | Armory menu + save/load, pause menu, perf pass with the minute-14 mix at the 350 cap (§7.8; profiling, instancing) |
| 10 | Tuning against the §3-5 numbers, bug fixes, Windows build handed to Dustin |
| **Total** | **10 days** |

### 8.4 Done when (Dustin can check each of these by hand in a Windows build)
1. The build launches from an .exe to the main menu. Pressing **Play** starts a run within 5 s.
2. KB+M and a gamepad both move the knight, roll (the knight passes through zombies during the roll), and aim the Longsword (the slash follows the cursor or right stick). Switching aim mode to Full Auto in Options works.
3. The camera is isometric (45° yaw, looking down at about 50°) and follows smoothly. Zombies never pop into existence inside the visible screen.
4. At minute 0-1, one or two Longsword hits kill a Shambler.
5. The timer reaches 10:00 and the mini-boss appears. The enemy count visibly ramps up, and Runners show up from about 2:00.
6. Level-ups pause the game and show 3 cards. Mouse, number keys, and gamepad all work. Reroll (2 per run), Banish (1), and Skip work, and their counters go down.
7. Elites appear at 5:00 and 8:00 and charge after a visible pulsing orange-red line (clearly different from blood). Each drops a chest.
8. With Longsword L8 and Whetstone owned, opening the 8:00 chest turns the Longsword into the Executioner's Greatsword with a 360° swing.
9. Hits flash white and knock zombies back. Crits show yellow numbers. Elite kills freeze the screen briefly and shake it. Blood bursts on hits, and splatter decals pool on the ground and fade after about 20 s.
10. Setting Blood to **Off** removes all red blood (grey dust instead), and **Low** visibly reduces it. Screen Shake 0% removes shake.
11. Killing the mini-boss shows the **Victory** results screen. Dying shows **Defeat**. Both show time, kills, level, and crowns earned.
12. Crowns persist after quitting and relaunching. Buying Might in the Armory raises the damage numbers in the next run.
13. With 300+ enemies on screen, the game stays smooth (the F1 debug overlay shows FPS ≥ 60 on Dustin's PC and the live enemy count). The full perf gate (the minute-14 mix at the 350 cap, §7.8) runs once Bloaters and Spitters exist. The slice runs this check with its own mix.

**Manual test checklist** (about 15 minutes, once per build): fresh launch → Options: set Blood Off, then back to Full → Play → roll through a crowd → reach L5 by about 1:30 → use a reroll, a banish, and a skip → kill the 5:00 elite and open the chest → aim for Longsword + Whetstone → check the evolution at 8:00 → fight the mini-boss → results → buy an Armory upgrade → quit, relaunch, and check that crowns and the upgrade are kept. Note FPS at 9:00.

---

## 9. Data schema appendix (ScriptableObjects)

### 9.1 Enums
| Enum | Values |
|---|---|
| `DamageType` | Physical, Holy, Fire, Poison |
| `WeaponArchetype` | MeleeArc, Projectile, Orbit, LobbedProjectile, GroundZone, Aura |
| `TargetingMode` | AimDirection, NearestEnemy, RandomEnemyInRange, PlayerCentered, MoveDirection |
| `AimMode` (option) | Assisted, FullAuto |
| `StatType` | MaxHealth, Recovery, Armor, MoveSpeed, Might, Area, Cooldown, Amount, Duration, ProjectileSpeed, CritChance, CritMultiplier, PickupRadius, Luck, Growth, Greed, Revivals, Rerolls, Banishes |
| `ModifierOp` | Add, PercentAdd, Multiply |
| `EnemyBehavior` | Chase, ExplodeOnDeath, RangedKite, ChargeElite, Boss |
| `EnemyTier` | Normal, Elite, Boss |
| `SpawnPattern` | RingRandom, RingFull, LineStampede, Cluster |
| `WaveEventType` | Swarm, Elite, Boss, EnableEvolutions, Chest |
| `EvolutionConditionType` | ChestAfterMinute |
| `UnlockConditionType` | KillTotal, SurviveMinutes, ReachLevel, EvolveWeapon, DefeatBoss |
| `HitType` | Normal, Crit, Overkill, EliteHit, EliteKill, BossHit, BossPhaseChange, BossKill, PlayerHurt, Explosion |
| `BloodIntensity` | Off, Low, Full |
| `PickupType` | XpGem, RoastChicken, CoinPouch, HolyReliquary, Chest |

### 9.2 `WeaponDefinition`
| field | type | units | example |
|---|---|---|---|
| `id` | string | n/a | `longsword` |
| `displayName` | string | n/a | Longsword |
| `icon` | Sprite (ref) | n/a | icon_longsword |
| `archetype` | WeaponArchetype | n/a | MeleeArc |
| `targetingMode` | TargetingMode | n/a | AimDirection |
| `damageType` | DamageType | n/a | Physical |
| `arcDegrees` | float | deg | 120 |
| `projectileSpeed` | float | m/s (deg/s for Orbit) | 0 |
| `range` | float | m | 6 (assist range) |
| `hitInterval` | float | s | 0.5 |
| `maxLevel` | int | n/a | 8 |
| `levels` | List<WeaponLevel> | n/a | 8 entries |
| `isEvolution` | bool | n/a | false |
| `prefab` | GameObject (ref) | n/a | WPN_Longsword |
| `hitSfx` | AudioClip[] (ref) | n/a | n/a |

### 9.3 `WeaponLevel` (serializable struct)
| field | type | units | example (Longsword L1) |
|---|---|---|---|
| `level` | int | n/a | 1 |
| `damage` | float | HP | 12 |
| `cooldown` | float | s | 1.2 |
| `area` | float | m | 2.8 |
| `amount` | int | count | 1 |
| `pierce` | int | enemies (-1 = ∞) | -1 |
| `duration` | float | s (0 = instant/perm) | 0.2 |
| `knockback` | float | m/s | 6 |
| `description` | string | n/a | "Slash in front" |

### 9.4 `PassiveDefinition`
| field | type | units | example |
|---|---|---|---|
| `id` | string | n/a | `whetstone` |
| `displayName` | string | n/a | Whetstone |
| `icon` | Sprite | n/a | n/a |
| `stat` | StatType | n/a | Might |
| `op` | ModifierOp | n/a | PercentAdd |
| `valuePerLevel` | float | fraction or flat | 0.10 |
| `maxLevel` | int | n/a | 5 |

### 9.5 `EvolutionRecipe`
| field | type | units | example |
|---|---|---|---|
| `id` | string | n/a | `evo_greatsword` |
| `baseWeapon` | WeaponDefinition (ref) | n/a | longsword |
| `requiredWeaponLevel` | int | n/a | 8 |
| `requiredPassive` | PassiveDefinition (ref) | n/a | whetstone |
| `conditionType` | EvolutionConditionType | n/a | ChestAfterMinute |
| `minMinute` | float | min | 7 |
| `resultWeapon` | WeaponDefinition (ref) | n/a | executioners_greatsword |

### 9.6 `EnemyDefinition`
| field | type | units | example |
|---|---|---|---|
| `id` | string | n/a | `shambler` |
| `displayName` | string | n/a | Shambler |
| `tier` | EnemyTier | n/a | Normal |
| `behavior` | EnemyBehavior | n/a | Chase |
| `maxHealth` | float | HP | 10 |
| `hpPerMinute` | float | fraction/min | 0.25 |
| `moveSpeed` | float | m/s | 1.6 |
| `contactDamage` | float | HP | 6 |
| `damagePerMinute` | float | fraction/min | 0.05 |
| `contactInterval` | float | s | 0.5 |
| `xpValue` | int | XP | 1 |
| `mass` | float | n/a | 1.0 |
| `knockbackResist` | float | 0-1 | 0 |
| `radius` | float | m | 0.3 (approved: Shambler 0.3, Runner 0.2, Bloater 0.7, Spitter 0.25, Grave Knight 0.7, Rotting King 1.5) |
| `prewarmCount` | int | count | 400 (Shambler; see §7.8 pool table) |
| `keepDistanceMin` / `keepDistanceMax` | float | m | 7 / 9 (Spitter; 0 / 0 = no kiting) |
| `firstMinute` | float | min | 0 |
| `crownValue` | float | crowns | 0.05 |
| `dropsChest` | bool | n/a | false |
| `attackParams` | List<EnemyAttack> (ref) | n/a | (Bloater: blast 2.2 m / 15 dmg / 0.8 s fuse) |
| `prefab` | GameObject (ref) | n/a | ENM_Shambler |

`EnemyAttack` fields: `name` string, `telegraph` float s (telegraph time), `radius` float m, **`range`** float m (max attack/projectile distance), **`width`** float m (line or projectile width), **`arcDegrees`** float (cone width), **`angleOffsets`** float[] deg (cone/volley directions), `damage` float, `interval` float s, `projectileSpeed` float m/s, `summonEnemy` EnemyDefinition ref, `summonCount` int, `hpThreshold` float (0-1, phase gate).

### 9.7 `SpawnSchedule` / `WaveEntry` / `WaveEvent`
`SpawnSchedule` fields: `runLengthMinutes` float (15), `bossMinute` float (15), `boss` EnemyDefinition ref, `hardCap` int (350), `ringMinDistance` float m (20), `ringMaxDistance` float m (24), `recycleDistance` float m (35), `moveDirBias` float (0.6), `entries` List<WaveEntry>, `events` List<WaveEvent>, `coopScaling` CoopScaling ref.

**WaveEntry**
| field | type | units | example (minute 5) |
|---|---|---|---|
| `minute` | int | min | 5 |
| `targetAlive` | int | count | 70 |
| `spawnRatePerSec` | float | /s | 3.5 |
| `mix` | List<EnemyWeight> {`enemy` ref, `weightPct` float} | % | Shambler 60, Runner 25, Bloater 15 |

**WaveEvent**
| field | type | units | example |
|---|---|---|---|
| `timeSeconds` | float | s | 210 (3:30) |
| `type` | WaveEventType | n/a | Swarm |
| `enemy` | EnemyDefinition ref | n/a | runner |
| `count` | int | n/a | 40 |
| `pattern` | SpawnPattern | n/a | LineStampede |
| `ignoreCap` | bool | n/a | true |

### 9.8 `XpCurve`
| field | type | units | example |
|---|---|---|---|
| `baseXp` | int | XP | 5 |
| `linearPerLevel` | float | XP | 6 |
| `quadraticPerLevel` | float | XP | 0.1 |
| `overrideCurve` | AnimationCurve (optional) | level→XP | empty |
| `gemValues` | int[] | XP | [1, 5, 25] |
| `maxGemsOnGround` | int | n/a | 400 |
| `magnetSpeed` / `magnetAccel` | float | m/s, m/s² | 12 / 20 |

Method: `int XpToNext(int level) => baseXp + Mathf.RoundToInt(linearPerLevel*(level-1)) + Mathf.FloorToInt(quadraticPerLevel*(level-1)*(level-1));`

### 9.9 `LevelUpRules`
`cardsShown` int 3, `fourthCardLuckCoeff` float 0.10/0.15 luck, `fourthCardMax` 0.5, `ownedWeight` 1.5, `newWeight` 1.0, `evoBaseWeightMult` 1.5, `skipCrowns` 3, `skipHealPct` 0.05, `fallbackCards` List<PickupType>, `chestRollChances` float[] [0.75, 0.20, 0.05].

### 9.10 `MetaUpgrade`
| field | type | units | example |
|---|---|---|---|
| `id` | string | n/a | `meta_might` |
| `displayName` | string | n/a | Might |
| `stat` | StatType | n/a | Might |
| `op` | ModifierOp | n/a | PercentAdd |
| `valuePerRank` | float | n/a | 0.05 |
| `maxRank` | int | n/a | 5 |
| `baseCost` | int | crowns | 100 |
| `costGrowth` | float | n/a | 0.25 (`cost = baseCost·r·(1+costGrowth·(r−1))`, rounded to 5) |

### 9.11 `CharacterDefinition` and `PlayerStats`
**`CharacterDefinition`** (one asset per character; launch has one, `sir_aldric`)
| field | type | units | example |
|---|---|---|---|
| `id` | string | n/a | `sir_aldric` |
| `displayName` | string | n/a | Sir Aldric |
| `description` | string | n/a | "A knight of the fallen order." |
| `portrait` | Sprite (ref) | n/a | portrait_aldric |
| `prefab` | GameObject (ref) | n/a | PLR_Knight_Placeholder |
| `baseStats` | PlayerStats (ref) | n/a | stats_sir_aldric |
| `startingWeapon` | WeaponDefinition (ref) | n/a | longsword |
| `bonusModifiers` | List<StatModifier> {`stat` StatType, `op` ModifierOp, `value` float} | n/a | empty (Aldric has none) |
| `unlockedByDefault` | bool | n/a | true |
| `unlock` | UnlockDefinition (ref, optional) | n/a | none |

**`PlayerStats`** (base values from §3.1; referenced by `CharacterDefinition.baseStats`)
| field | type | units | example |
|---|---|---|---|
| `maxHealth` | float | HP | 120 |
| `recovery` | float | HP/s | 0 |
| `armor` | float | HP | 0 |
| `moveSpeed` | float | m/s | 5.0 |
| `might`, `area`, `cooldown`, `duration`, `projectileSpeed` | float | mult | 1.0 |
| `amount` | int | n/a | 0 |
| `critChance` / `critMultiplier` | float | n/a | 0.05 / 2.0 |
| `pickupRadius` | float | m | 2.5 |
| `luck`, `growth`, `greed` | float | mult | 1.0 |
| `revivals`, `rerolls`, `banishes` | int | n/a | 0, 2, 1 |
| `dodgeDistance`, `dodgeDuration`, `dodgeIFrames`, `dodgeCooldown` | float | m, s, s, s | 4, 0.3, 0.3, 3 |
| `weaponSlots`, `passiveSlots` | int | n/a | 4, 4 |

### 9.12 `HitFeedbackProfile` and `FeedbackSettings`
`HitFeedbackEntry`: `hitType` HitType, `localStopMs` int, `globalStopMs` int, `shakeAmplitude` float m, `shakeDuration` float s, `flashMs` int, `bloodParticles` int, `decalCount` int.
`FeedbackSettings`: `globalStopMinIntervalMs` 150, `maxNewDecalsPerFrame` 32, `maxBloodSystems` 96, `maxBloodParticles` 3000, `maxDecals` 48 (slice 100), `decalLifetime` 20, `decalFade` 4, `decalWetTime` 6, `decalFreshColor` `#B3121B`, `decalDryColor` `#7A0A10`, `maxScorchMarks` 50, `bloodPrewarmHit` 64, `bloodPrewarmCrit` 24, `bloodPrewarmKill` 48, `corpseLieTime` 3, `corpseSinkTime` 1.5, `corpseSinkDepth` 0.6, `playerHurtFlashColor` `#E23A3A`, `poolsGrowable` true, `decalMergeRadius` 1.0, `decalMergeThreshold` 3, `decalMaxScale` 2.0, `maxDamageNumbers` 128, `damageNumberMergeWindow` 0.15, `maxCorpses` 60, `knockbackDecay` 0.15, `knockbackMax` 12, `bloodIntensity` BloodIntensity.

### 9.13 `CoopScaling` (future; the 1-player row is used now)
| field | type | example (index = playerCount−1) |
|---|---|---|
| `enemyCountMult` | float[4] | [1.0, 1.6, 2.1, 2.5] |
| `enemyHpMult` | float[4] | [1.0, 1.25, 1.45, 1.6] |
| `bossHpMult` | float[4] | [1.0, 1.7, 2.3, 2.8] |
| `hardCap` | int[4] | [350, 450, 500, 550] |
| `xpMult` | float[4] | [1.0, 1.4, 1.7, 2.0] |

### 9.14 System rules for supporting any player count (apply these now)
- No `Player.Instance` singleton. Use a `PlayerRegistry` with `IReadOnlyList<PlayerController> Players` and a `PlayerCount` property.
- Targeting: "nearest enemy" is computed relative to the **firing player**. Enemy AI chases the **nearest living player**, re-evaluated every 0.5 s.
- Spawning: each spawn picks a random living player (round-robin) for ring centering. Recycling checks the distance to **every** player.
- XP: gems are attracted by each player's own `pickupRadius`, and collected XP goes to the shared `RunXp` pool (§11).
- Difficulty: `targetAlive`, `spawnRatePerSec`, HP, and `hardCap` are multiplied by `CoopScaling[playerCount−1]`.
- Stats: each player has its own `RuntimeStats` (base + passives + meta). Meta is per profile.
- Camera: the follow target is a `CinemachineTargetGroup` of all players, even with 1 player.
- Input: each `PlayerController` owns its own `PlayerInput` (Input System). Gameplay code never reads `Keyboard.current` or `Gamepad.current` directly, so couch co-op can assign devices per player.
- Randomness: all gameplay rolls (spawns, level-up cards, chests, crits) use a seeded per-run `System.Random` held by `RunState`, not `UnityEngine.Random`. Cosmetic randomness (blood, pitch) may use anything.

### 9.15 `UnlockDefinition`
| field | type | units | example |
|---|---|---|---|
| `id` | string | n/a | `unl_spurs` |
| `displayName` | string | n/a | Knight's Spurs |
| `conditionType` | UnlockConditionType | n/a | ReachLevel |
| `threshold` | float | count / min / level | 20 |
| `unlocksWeapon` | WeaponDefinition (ref, optional) | n/a | none |
| `unlocksPassive` | PassiveDefinition (ref, optional) | n/a | spurs |
| `unlocksMetaUpgrade` | MetaUpgrade (ref, optional) | n/a | none |
| `unlocksCharacter` | CharacterDefinition (ref, optional; unused at launch) | n/a | none |

---

## 10. Open questions for Dustin
None right now. All v0.1 questions are resolved (see the header table and the changelog). All PlaceholderSpec alignment items are closed (§13).

---

## 11. Co-op (future: couch and online; single player ships first)

**Decided:** co-op will come later and will support **both couch (local) and online**, for 1 to 4 players. Nothing co-op ships in the slice. Every system follows the player-count rules in §9.14 from day one, so co-op doesn't need a rewrite.

### 11.1 Gameplay rules (both modes)
| Topic | Decision / recommendation |
|---|---|
| XP model | **Shared pool (recommended).** All players share one XP bar and level up together. Each player picks their own card from their own roll. Total XP is scaled by `xpMult` (1.0 / 1.4 / 1.7 / 2.0), because the XP thresholds don't change. Why: no one falls behind, and nobody fights over gems. |
| Level-up handling | The game pauses for everyone. Each player sees their own 3 cards at the same time and picks independently. A 20 s timer then auto-picks the highest-weight card. Rerolls and banishes are per player. |
| Chests | The player who opens the chest gets its rolls, and evolution eligibility is checked for that player. Other players get +1 reroll each. Elites drop **1 chest per 2 players** (rounded up). |
| Downed and revive | At 0 HP a player is **downed**: crawls at 1.0 m/s, takes no damage, and bleeds out after **30 s**. A teammate stands within **1.5 m** for **3.0 s** to revive (progress decays at 2× when they leave). The revived player comes back at **40% HP** with **2 s** of i-frames. A Revival charge self-revives instantly when downed. The run ends when all players are downed or dead. |
| Enemy count | `targetAlive` and `spawnRatePerSec` × **1.0 / 1.6 / 2.1 / 2.5** for 1-4 players |
| Enemy HP | × **1.0 / 1.25 / 1.45 / 1.6** (normal and elite) |
| Boss HP | × **1.0 / 1.7 / 2.3 / 2.8** (boss TTK target stays 60-90 s) |
| Hard cap | **350 / 450 / 500 / 550** |
| Characters | Every player can pick Sir Aldric. Players are told apart by team color, heraldry, and ground rings (ArtDirection §8). |
| Crowns | Each player earns from their own kills. Time and boss bonuses go to everyone in full. |
| Friendly fire | None. Blood decals and the hit-stop rate limit are shared globally. |

### 11.2 Couch co-op (local)
- **Input:** Unity `PlayerInputManager`, join by pressing a button on a new device. One device per player; KB+M counts as one device. Each player gets their own `PlayerInput` and UI cursor on the level-up screen (MultiplayerEventSystem).
- **Camera:** one **shared** camera, no split-screen. A `CinemachineTargetGroup` of all living players, zooming out from 26 m to at most **34 m** (×1.3) as players spread. A soft tether at **22 m** between any two players (they slow down past 18 m and can't go past 22 m). Downed players stay in the group so others can find them.
- **Aim:** Assisted aim with the mouse only works for the KB+M player; gamepad players use the right stick or fall back to nearest-enemy.
- **Performance:** the same machine runs every player, so the higher hard caps above must hold 60 fps on the PC target. Profile at 4 players before shipping couch co-op.

### 11.3 Online co-op (later)
- **Netcode:** a netcode solution is chosen when online work starts (for example Unity Netcode for GameObjects or Netcode for Entities with a Steam transport). The design doesn't depend on which.
- **Authority:** **host-authoritative.** The host simulates enemies, spawns, damage, XP, chests, and level-up rolls. Clients send inputs (move, aim, dodge, card picks) and render snapshots. Clients predict their own knight's movement and dodge only.
- **Deterministic-friendly habits to keep now (cheap, single-player safe):** a seeded per-run RNG for all gameplay rolls (§9.14); gameplay logic on a fixed tick (`FixedUpdate`, 50 Hz) and kept separate from presentation; enemy state kept in compact arrays (position, HP, state) that are easy to snapshot and quantize; no gameplay decisions inside VFX, audio, or animation callbacks.
- **What's replicated vs local:** replicated: player transforms, enemy snapshots (quantized positions at about 20 Hz, interpolated), HP, deaths, pickups, level-up offers and picks, chest results. **Local only:** blood bursts, decals, splat map, damage numbers, hit-stop, screen shake, audio. Each client derives these from replicated hit and death events, so they never need syncing.
- **Pausing:** level-up and chest screens pause the shared simulation for everyone (same 20 s auto-pick). The pause menu does not pause an online session; it only opens the menu for that player.
- **Join rules:** join from the lobby only, not mid-run, at least at first.

---

## 12. Future content (not at launch)
| Item | Notes |
|---|---|
| **Brother Cuthbert** (character) | Starts with Holy Flail, cooldown −10%, moveSpeed −10%. Would use `CharacterDefinition` + `UnlockDefinition` (for example ReachLevel 25). |
| **Maren the Crossbow** (character) | Starts with Crossbow, amount +1, maxHealth −20%. Would be unlocked by, for example, EvolveWeapon 1. |
| Character select screen | In code from the start but hidden while the roster has one entry |
| Co-op (couch + online) | §11 |
| More weapons and passives (toward 12 + 12) | Then revisit 6 + 6 slots |

---

## 13. Doc alignment notes (PlaceholderSpec vs GDD): all closed
**Standing rule (decided, Supervisor):** the **GDD is canonical for numbers and behavior**, and **PlaceholderSpec is canonical for look** (shapes, colors). Where the GDD was silent, the spec's numbers were adopted in v0.3, with gameplay overrides noted in §3.5.2 and §7.8. Any new conflict after the freeze goes to the Supervisor.

Rows use the original v0.2.1 numbering.

| # | Topic | Status | Final ruling | In |
|---|---|---|---|---|
| 1 | Prewarm and pools | **DECIDED, closed** | All pools can grow. Prewarm 400 / 200 / 60 / 60 / 3 / 1 (Shambler / Runner / Bloater / Spitter / Grave Knight / King). Spec pool sizes adopted, with 3 overrides. | §7.8 |
| 2 | Hit flash | **DECIDED, closed** | Material swap held for 80 / 100 / 80 / 60 / 120 ms | §7.1, §7.3 |
| 3 | Blood burst counts | **DECIDED, closed** | GDD formula | §7.4 |
| 4 | Slice decals | **DECIDED, closed** | 100 projectors, 20 s life, 4 s fade. Wet for `decalWetTime` **6 s**, then dry. Max 32 new per frame. | §7.5 |
| 5 | Blood Off / perf mix | **DECIDED, closed** | Scorch marks max 50 with a 20 s life. Perf test uses the minute-14 mix at the 350 cap. | §7.6, §7.8 |
| 6 | (merged into 5) | **closed** | n/a | n/a |
| 7 | Enemy collider radius | **DECIDED, closed (approved)** | 0.3 / 0.2 / 0.7 / 0.25 / 0.7 / 1.5 m | §3.5, §9.6 |
| 8 | Player-hurt flash | **DECIDED, closed** | HP red `#E23A3A` | §7.1, §7.3 |
| 9 | Corpse removal | **DECIDED, closed** | Lie 3 s, then sink 0.6 m + dither-fade over 1.5 s | §7.5 |
| 10 | Decal colors | **DECIDED, closed** | Fresh `#B3121B` drying to `#7A0A10` (the only dried color) | §7.5, §9.12 |
| 11 | Telegraph visuals | **DECIDED, closed** | Sizes in §3.5.2 (spec numbers, 2 overrides). Look per PlaceholderSpec §3.8. | §3.5.2 |
