# Knight Survivors: Art Direction

**Owner:** Asset Designer · **Last updated:** 2026-10-28
**Target:** Unity 6 LTS · URP (Forward+) · PC first · 300+ zombies on screen at 60 fps · **isometric camera**
**Confirmed decisions (Dustin):** isometric camera with readability as the top priority. Gore is **blood and splatter only** (no dismemberment, no gibs). Co-op comes later, so player variants must be easy to tell apart.

---

## 1. Style pick

**Recommendation: stylized low-poly (Synty POLYGON-style flat-shaded atlas art) with heavy, glossy, high-contrast blood.**

Why:

| Factor | Stylized low-poly | Semi-realistic |
|---|---|---|
| 300+ skinned or animated enemies | 1.5–3k tris per zombie and 1 shared atlas material, so batching is easy | 8–20k tris, PBR texture sets, many materials, and the vertex and bandwidth budget is gone by about 150 enemies |
| Iso camera at roughly 18–25 m | Bold shapes and flat color read at 60–120 px character height | Fine surface detail is lost at that distance, so you pay for detail nobody sees |
| Readability | Hue-coded zombie types, clean silhouettes | Realistic grime makes everything mid-brown, and blood disappears into it |
| Asset sourcing | Synty has knights, undead, castles and animation packs that share one style | Mixed realistic packs rarely match each other |
| Visceral feel | Comes from the blood (saturated red, gloss, persistent pooling), hit-stop, and sound, not from mesh detail | Same, at 5× the cost |

Visceral comes from **contrast and motion**: bright red blood on a cool grey floor, splats that stay, a knight whose armor visibly gets soaked, hit-stop, and punchy audio. Fidelity isn't what sells it.

---

## 2. Palette, mood, and lighting

### Mood
"Cursed moonlit courtyard." The world is cold, desaturated, and blue-grey. The only warm, saturated things on screen are **blood, fire, the player's team color, and the UI**. The player's eye should always land on the knight, the threat types, and the gore.

### Palette (hex)

| Role | Hex | Notes |
|---|---|---|
| Ground: flagstone base (mid-value) | `#6B7178` | Desaturated cool grey. **The value range of the floor is the most important art rule.** |
| Ground: flagstone variation | `#5C636B` / `#7A8087` | Value variation within ±10%, no hue shifts toward red or brown |
| Ground: mortar / cracks | `#474D54` | |
| Moss / grass accents (sparse) | `#4F6B5A` | Cool, desaturated green. Keep it below 15% of floor area. |
| Walls and arena edge (dark frame) | `#2E333A` | Darker than the floor, so the play space pops |
| Sky / fog / ambient | `#1C2433` → `#3A4A63` | Night-blue gradient |
| Moonlight (key) | `#C9D6FF` | |
| Torch / fire (accent) | `#FF9A3C` | Only at the arena perimeter, never under the play area |
| Blood: fresh | `#B3121B` | Highest-saturation red in the game, reserved for blood |
| Blood: core / dark | `#7A0A10` | |
| Blood: gloss highlight | `#FF4A4A` | Specular/rim on wet blood |
| Blood: dried decal (aged) | `#5A0B0E` | Still darker and redder than any floor color |
| Knight steel base | `#A9B1BA` | Neutral, so team tint and blood read on it |
| Knight team color slot | per player (see §8) | |
| UI gold (XP, rarity) | `#F2C14E` | |
| UI health red | `#E23A3A` | Slightly orange-shifted from blood so the HP bar isn't confused with gore |
| UI XP blue | `#3FA9F5` | |
| Damage numbers: normal / crit | `#FFFFFF` / `#FFD23F` with a `#1A1A1A` outline | |

**Forbidden in the play-area floor:** reds, oranges, browns, and dark maroons (anything with hue 340°–40° and saturation > 20%). Brown dirt, wooden floors, red carpets, or rust-colored stone would hide the blood. If a pack's arena tiles are brown, recolor the atlas swatch.

### URP lighting setup
- **Rendering path:** Forward+ (needed for many small lights and for GPU Resident Drawer). Depth priming: Auto.
- **Main light:** 1 directional "moonlight", intensity about 1.2, color `#C9D6FF`, pitched about 50° and yawed so shadows fall diagonally (not straight down the iso axis, which would hide them).
- **Additional lights:** per-pixel, max about 8 visible, **no shadows** on additional lights. Torches sit at the perimeter only. Ability and VFX flashes use short-lived pooled point lights (cap 4).
- **Ambient:** gradient (Sky `#3A4A63`, Equator `#2A3445`, Ground `#1C2433`). No realtime GI. Bake a simple light probe and AO pass for the static arena (APV not required).
- **Rim/fresnel:** a character shader rim term (see §7) does most of the readability work. Don't add rim lights.

### Shadows (URP Asset)
- Main light shadows on. Resolution **2048**. **2 cascades** (split at about 35%). Max distance **40–50 m** (just past the iso frustum; tune to the camera). Soft shadows: **Medium**. Depth bias 1, normal bias 1.
- **Knight(s), Bloaters and elites cast real shadows. Common zombies use cast shadows Off and a pooled blob shadow** (a cheap instanced quad or part of the VAT shader). That halves crowd draw cost. If profiling allows, enable crowd shadows on LOD0 only.
- Static arena: cast and receive. Small props (<0.5 m): cast shadows off.

### Post-processing (one global Volume)
- **Tonemapping:** Neutral (keeps the blood red from shifting orange the way ACES does). Switch to ACES only if the art reads better after testing.
- **Color Adjustments:** saturation −10 globally (blood and UI stay saturated because they're authored hot). Contrast +10.
- **Bloom:** threshold 1.1, intensity 0.3, so only emissive pickups, fire, and level-up flashes bloom. Blood must **not** bloom.
- **Vignette:** 0.25, color `#000000`. On low HP, a pulsing red vignette (`#7A0A10`) via script.
- **Film grain / chromatic aberration:** off (or a very small hit flash only).
- **SSAO (Renderer Feature):** optional, low quality, half-res, radius 0.3. Disable first if the GPU budget is tight.
- **Off:** motion blur, depth of field, lens distortion, SSR. All of them hurt readability.

---

## 3. Poly and texture budgets, plus performance architecture

Budgets assume an iso camera about 20 m away, where characters are about 60–120 px tall at 1080p.

| Asset | LOD0 tris | LOD1 | LOD2 / impostor | Bones | Textures |
|---|---|---|---|---|---|
| Knight (player; Sidekick-baked) | ≤ 15k (up to 4 in co-op) | 7k | — (always near) | ≤ 60 (incl. prop bone) | Sidekick color map + 1 shared atlas 2048², plus a **mask texture 1024²** (team/blood, see §4/§8) |
| Zombie Tier 1 "Shambler" (bulk 70%) | **≤ 2.5k** | 1.2k | 500 | ≤ 24 (SMR fallback) or VAT | Shared POLYGON atlas 2048² (1 material for all T1 variants) |
| Zombie Tier 2 "Runner" (bulk 25%) | ≤ 3k | 1.5k | 600 | ≤ 24 or VAT | Same shared atlas |
| Zombie Tier 2b "Plague Spitter" (GDD `spitter`; T2 budget) | ≤ 3k | 1.5k | 600 | ≤ 24 or VAT | Same shared atlas |
| Zombie Tier 3 "Bloater" (GDD `bloater`; formerly "Brute"; ≤ 10 on screen) | ≤ 6k | 3k | 1.2k | ≤ 40 (SMR OK) | Same atlas + optional 512² detail |
| Elite / boss (1–2) | ≤ 12k | 6k | — | ≤ 60 | Own 1024²–2048² |
| Props (barrels, carts, weapons racks) | 100–1.5k each | 50% | cull at 60 m | — | Shared environment atlas 2048² |
| Arena total (static, on screen) | ≤ 250k | — | — | — | 1–2 environment atlases 2048², 1 ground material |
| Pickups (XP gems, chests) | ≤ 200 | — | — | — | Shared FX/UI atlas, emissive |

**Frame triangle budget (on screen):** about 1.3–1.8M tris total (300 × ~2.5k = 750k for the crowd, plus arena, plus VFX). That's comfortable for mid-range PC GPUs if draw calls stay low.

**Note on Synty source meshes:** Synty POLYGON characters are usually a few thousand tris, but **measure each prefab on import** (Mesh Inspector or Rendering Debugger) and decimate if needed. Bloaters and elites can come straight from the pack. Common zombies need LOD1/LOD2 (Unity 6 has no built-in auto-LOD, so use a mesh simplifier or a DCC tool).

### LODs
- LODGroup on every character and prop larger than 1 m. With the iso camera, LOD bias is driven by the zoom level. Fade mode: none or crossfade-dither off (dither breaks batching on crowds).
- Crowd LOD is done **in the crowd renderer** (distance bucket → mesh index), not with LODGroup components on 300 GameObjects.

### Shared materials and atlases
- **One material per family:** all zombies share 1 material (atlas UV + per-instance color/rim via MaterialPropertyBlock-free instanced properties). All arena modules share 1–2 materials. All blood decals use 1 material with a flipbook atlas.
- Synty's palette atlas makes recoloring trivial. To recolor floor tiles away from brown, edit the atlas swatch, not the meshes.
- Max about 8 unique materials in the crowd plus character pass.

### Batching: SRP Batcher vs GPU instancing vs GPU Resident Drawer
- **SRP Batcher:** on (default). It covers the static arena, props and the knight. All custom shaders must be SRP Batcher-compatible (all material properties inside `UnityPerMaterial` CBUFFER; Shader Graph does this automatically).
- **GPU Resident Drawer (Unity 6):** enable in the URP Asset (Instanced Drawing; requires Forward+ and BatchRendererGroup variants kept). It automatically instances MeshRenderers, so **VAT zombies (MeshRenderers) get it for free**. It does **not** apply to SkinnedMeshRenderers.
- **Manual instancing:** for the crowd, prefer a single `Graphics.RenderMeshInstanced` / `BatchRendererGroup` crowd renderer driven from a NativeArray of transforms and anim states (Jobs + Burst). That means no per-zombie GameObject for rendering. This is the scalable path to 300 and then 1000.
- GPU instancing and the SRP Batcher are mutually exclusive per draw. Keep crowd materials on the instancing path and everything else on the SRP Batcher.

### Animating the crowd

| Option | Cost at 300 | Pros | Cons | Use for |
|---|---|---|---|---|
| **Vertex Animation Textures (VAT) / GPU baked animation** | Almost free on the CPU, 1–3 instanced draws | Scales to 1000+, works with GPU Resident Drawer and BRG | No blending (crossfade in shader between 2 clips), no IK, bake step, more texture memory (about 0.5–2 MB per clip set) | **T1 Shamblers and T2 Runners (recommended)** |
| SkinnedMeshRenderer with a bone cap | CPU/GPU skinning about 0.02–0.05 ms each → 300 = 6–15 ms. Too expensive. | Mecanim blending, ragdoll-ready | Draw call per SMR, Animator update cost | **Knight(s), T3 Bloaters, elites (≤ 15 total)** |
| SMR + Animator culling + "Update When Offscreen" off + GPU skinning (compute) + bone cap 24 | Mid | Easy to start | Still hits a wall around 100–150 | **Prototype only**, until the VAT baker exists |

**Plan:** prototype with SMRs (Quality → Skin Weights: 2 bones; Animator Culling Mode: Cull Completely; Animator update via `Animator.keepAnimatorStateOnDisable`), then switch T1/T2 to VAT before the 300-enemy perf gate. Bake walk, run, attack, hit-react, and 2 death clips per zombie into an RGBAHalf position (+ normal) texture. The VAT baker and shader are **custom** (an in-house editor script plus a Shader Graph custom function; about 1–2 days for the Unity Coder).

---

## 4. Gore toolkit: blood and splatter only

Scope (confirmed): **blood bursts, persistent blood decals on the ground, blood build-up on the knight's weapon and armor, and corpse handling.** **No dismemberment, no gibs, no severed parts.** The tone is stylized M-rated action: bright, glossy, exaggerated red, nothing anatomical.

### 4.1 Blood bursts (hit and kill VFX)
- **Tech:** pooled **Particle System (Shuriken)** prefabs, not VFX Graph, for hits. This works well with pooling, needs no compute or GPU readback, and suits URP on any PC. Use **VFX Graph** for one or two "mass" effects only (for example a big AoE ultimate that sprays 2–5k droplets) with a single persistent graph instance that you fire events into (`VisualEffect.SendEvent` with position attributes). Don't instantiate a graph per hit.
- **Variants:** `Blood_Hit_S` (normal hit), `Blood_Hit_Crit` (plus a short arterial spray), `Blood_Kill_Burst` (kill / overkill, a mesh-flipbook "splash" sheet), `Blood_Bloater_Kill` (big radial green-red bile burst, also used for elite/boss kills, with a ground-splat spawn). **Particle counts are GDD §7.4's** (`clamp(4 + 20 × damage / maxHealth, 4, 24)` per hit, crit ×1.5, kill 12, overkill 30, Bloater 40, elite/boss 80, all × `bloodIntensity`); this doc only sets the look.
- **Pooling:** an `ObjectPool<ParticleSystem>` per variant, prewarmed (S: 64, Crit: 24, Kill: 48, Bloater: 8). Use `Play()`/`Stop(StopEmittingAndClear)`, never Instantiate or Destroy. Stop Action → Callback returns the instance to the pool.
- **Budget:** cap simultaneous active blood systems at **96**. If the pool is empty, **steal the oldest** rather than spawning. Max about 3,000 live blood particles. Particle material: 1 unlit/simple-lit Shader Graph with a flipbook atlas (1024², 4×4), soft particles **off**, sorting by distance off.
- **Look:** hot red `#B3121B` core with a `#FF4A4A` glossy rim. Fast outward burst (0.15 s), gravity 2–3, and droplets that "land" (collision off; instead spawn a decal at predicted landing points, see 4.2).
- **Hit feel (non-gore but critical):** hit-stop, a white hit flash on the zombie (instanced `_FlashAmount` property, warm white on elites), and a camera shake capped by a global trauma budget. **Durations and amplitudes are GDD §7.1's** (for example flash 80 ms normal / 100 ms crit / 60 ms boss; the player-hurt flash is 120 ms in HP red `#E23A3A`, never blood red).

### 4.2 Persistent ground blood (decals)

| Approach | Persist count | Cost | Verdict |
|---|---|---|---|
| URP **Decal Projector** (Decal Renderer Feature) | Each projector is a draw. 500+ hurts. | Screen-space technique is cheap per decal, DBuffer is pricier | Use for a **small pool of "fresh, hero" splats only** |
| Pooled quads (flat mesh slightly above the floor, instanced) | 1–2k fine if instanced | Cheap, simple | Fine fallback, but z-fighting on uneven ground and no wrap over curbs |
| **Render-texture splat map** (blood accumulation RT sampled by the ground shader) | **Unlimited, fixed cost** | One 2048² RT for the arena (16 MB RGBA8, or 4 MB R8+ flow) plus a stamp pass | **Recommended for persistence** |

**Recommended hybrid:**
1. **Fresh splat:** when a zombie dies or a heavy hit lands, take a **URP Decal Projector** from a pool of **48** (Screen Space technique, Forward+ compatible; 1 material with an 8-variant atlas, random rotation and scale 0.6–1.4). It lives 4–6 s, glossy and wet.
2. **Bake into the splat map:** as a fresh decal expires (or immediately, for small splats), **stamp** it into the arena's **blood RT** with a `CommandBuffer.DrawMesh` blit (arena-space orthographic UVs). The ground Shader Graph lerps floor albedo toward dried blood `#5A0B0E` (and lowers roughness slightly) by the RT value. Result: **unlimited persistent blood at a fixed cost**, accumulating trails and pools around hot zones, which sells the "visceral" brief.
3. **Caps:** live projectors ≤ 48 (steal oldest). Splat-map stamps per frame ≤ 64 (queue the rest). Optional slow "drying" (RT darkens over about 60 s) and a "rain/wash" reset between waves or runs.
4. **Vertical slice (interim, before the splat map exists; matches GDD §7.5):** projectors only, a pool of **100** URP Decal Projectors. Lifetime **20 s** (16 s solid, then a 4 s fade via `DecalProjector.fadeFactor`, so there's no per-instance material). Drying fits inside that window: fresh `#B3121B` until 6 s (GDD `decalWetTime` 6), then a shared-material swap to dried `#7A0A10` (PlaceholderSpec §6.2). Blood **Off** uses charcoal scorch-mark projectors instead, max 50. When the pool is full, recycle the oldest. Merge rule: if 3+ decals are already within 1.0 m, scale the nearest up 15% (max 2.0×) and reset its lifetime instead of spawning. The 48-projector + splat-map hybrid above replaces this later.
5. Props and walls do not receive the RT. Only the ground does (set the Decal Projector layer mask to Ground only so fresh splats don't land on zombies).

### 4.3 Blood build-up on the knight (weapon and armor)
This is **custom** (Shader Graph). It's the main "visceral" signal on the player character.
- **Material parameters:** `_BloodAmount` (0–1, global for the knight), `_BloodWeapon` (0–1, separate for the sword), `_BloodColor` (`#7A0A10`), `_BloodGloss` (wet → dry over time).
- **Mask texture (1024², channel-packed):** **R = blood accumulation mask** (where blood pools: sword blade edge and fuller, gauntlets, greaves, the lower tabard, and the chest plate's lower half). Authored as a gradient, so `_BloodAmount` grows blood "from the blade outward" with `step(1 - _BloodAmount, mask.R)` plus a noise edge. **G/B channels are reserved for team color (§8)**, and A for emissive trim.
- **Driving it:** every kill adds +0.01 to `_BloodWeapon` (cap 1) and +0.004 to `_BloodAmount`. Values decay slowly (−0.01/s) or reset on level-up, dodge-roll through water, or a "cleanse" upgrade. That gives a visible "kill streak" read.
- **Optional:** a world-space hit-direction splatter. Project a triplanar blood noise only where `dot(normal, hitDir) > 0.3` on heavy hits. Nice to have, not required for the slice.
- Zombies get a simpler version: per-instance `_BloodAmount` on the shared material (instanced property), so wounded zombies look bloodier. It costs no extra draws.

### 4.4 Corpse handling
- **On death:** the zombie plays one of 2–3 baked death clips (VAT for T1/T2, Animator for T3). There's **no ragdoll** for crowds (ragdolls are allowed for Bloaters only, max 3 active).
- **Lifetime:** lie for **3 s**, then **sink** 0.6 m into the ground over 1.5 s (vertex offset in shader, no physics) while dither-fading (Bayer dither in the VAT shader keeps it opaque-pass and batch-friendly; avoid transparent fades). A small blood pool decal stamps into the splat map at the moment of death.
- **Caps:** max **60 corpses** visible at once. When the cap is hit, the oldest corpse sinks immediately (0.4 s). Bloater and elite corpses count as 3.
- **Pooling:** each zombie is a pooled entity (crowd array slot plus optional GameObject for colliders). "Death" switches the slot to a `Corpse` state (no collider, no AI), then back to `Pooled` after the sink ends. **No Instantiate or Destroy in gameplay.** Prewarm 400 Shamblers, 200 Runners, 60 Bloaters, 60 Spitters (GDD §13 #1), and let those pools grow past the prewarm; the spawner's hard cap limits the count.
- Corpses don't block movement or pathing (colliders disabled on death).

---

## 5. UI and HUD direction

**Principle:** with 300 zombies and blood everywhere, the HUD lives at the **screen edges**. The center of the screen is for gameplay and floating combat feedback only. All HUD elements sit on dark translucent plates (`#0E1116` at 70% alpha) so they never fight the floor.

- **Look:** "gothic-medieval minimal": dark iron plates, thin gold (`#F2C14E`) trims, no heavy parchment textures. Built in **uGUI** for the slice (mature, easy world-space damage numbers). UI Toolkit is acceptable for menus later.
- **Health:** a large bar at bottom center (`#E23A3A` fill, `#5A1A1A` background, white chip-damage trail). It shakes and flashes on hit. Low HP triggers a red vignette pulse. In co-op, each player gets a compact bar tinted with their **team color** and also a **world-space mini bar plus ring** under the knight (§8).
- **XP bar:** full width along the top edge, blue `#3FA9F5`, with level number and timer at top center. It flashes gold when full.
- **Level-up card picker:** the game pauses (or slows to 10%) and dims the world to 40%, then shows **3 (or 4 with a reroll upgrade) cards**. Each card has a big icon, a name in the display font, a short description in the body font, a rarity frame (Common steel `#A9B1BA`, Rare blue `#3FA9F5`, Epic violet `#A06CFF`, Legendary gold `#F2C14E`), and keyboard, mouse, and gamepad shortcuts 1/2/3. Cards are readable at 1080p from couch distance (title ≥ 28 px).
- **Damage numbers:** pooled world-to-screen TextMeshPro labels (pool 128; **steal oldest**). Normal hits are white with a dark outline. Crits are yellow `#FFD23F`, 1.4× size, with a pop scale. Incoming damage to the knight is red. **Aggregation:** hits on the same target within 0.15 s merge into one rising number. An option toggle offers *All / Crits only / Off*, because hordes produce hundreds per second. Use TMP SDF with a shared material (one draw for all numbers via TMP's mesh batching on a single canvas).
- **Fonts (free, SIL Open Font License, from Google Fonts):** **Cinzel** (display: titles, card names, level-up), **Barlow Condensed SemiBold** (numbers, timers, damage numbers, since condensed numerals read fast), **Inter** (body text). Build TMP SDF atlases with ASCII plus the needed glyphs only.
- **Readability with hordes:** an off-screen indicator arrow for elites and bosses (tinted with the zombie's type color). Pickups (XP gems) are emissive cyan/green, the only non-red, non-team emissives on the floor. Nothing in the HUD uses blood red except HP. A UI scale slider (80–130%) and a colorblind palette swap (see §7) are available.

## 6. Audio direction

- **Music:** dark orchestral-meets-metal. Low strings, war drums, choir hits, and a distorted guitar or cello layer that **intensifies with horde density and the run timer** (3–4 stems, vertically layered; FMOD/Wwise or Unity Audio Mixer snapshots). Boss waves get a dedicated cue. The menu is a sparse choir and wind.
- **SFX categories (mixer groups):** `Player_Weapon` (swings, impacts), `Hits_Flesh` (wet impacts, splats), `Zombie_Vox` (groans, attacks, deaths), `Player_Vox` (grunts, hurt), `Pickups` (XP chimes, ascending pitch with combo), `UI` (card open/select, level-up), `Ambience` (wind, distant bells, torches), `Abilities`.
- **Voice limits and pooling (critical for hundreds of hits per second):**
  - A pooled `AudioSource` system (64 sources total, prewarmed, 2D for UI, 3D with a steep falloff for the world). Never `AudioSource.PlayClipAtPoint` (it allocates).
  - **Per-category voice caps:** Hits_Flesh 12, Zombie_Vox 10 (prioritize the closest and elites), Player_Weapon 6, Pickups 6, Abilities 8. When over the cap, **steal the oldest or quietest**, or skip.
  - **Rate limiting:** max 1 instance of the same clip per 40–60 ms. Beyond that, **escalate to a "multi-hit" layered sample** (a single "crowd shred" sound) instead of stacking 30 identical hits.
  - **Variation:** 6–10 variants per hit sound, ±8% pitch and ±2 dB random. Round-robin to avoid repeats.
  - **Ducking:** a sidechain snapshot ducks the horde and ambience by 3 dB on level-up and boss roar. The UI is never ducked.
  - Zombie groans are a **horde bed** (1–2 looped crowd-groan beds scaled by nearby enemy count) plus a few positional one-shots, not 300 individual groan emitters.
- **Sourcing (not in the slice list):** use free or royalty-free packs (for example Sonniss GDC bundles) or a paid SFX pack later. To be picked in a separate audio pass.

---

## 7. Isometric readability (top priority)

The camera is isometric (orthographic or narrow-FOV perspective, about 45° yaw and 35–50° pitch, about 18–25 m from the knight). Everything below serves "can I instantly tell the knight, each threat type, and the blood apart at that distance?"

### 7.1 Silhouettes at iso distance
- **Shape language per tier (from above):** Shamblers are narrow, hunched, with arms forward (a "|" shape). Runners are lean, leaning far forward, with a long stride (a "/" shape). Bloaters (formerly "Brutes") are about as wide as they are tall (1.4 m wide), round, with a clear top-down mass (an "O" shape). Plague Spitters stand upright with no lean and carry a large glowing throat sac in front of the chest (a "lollipop/lantern" shape). Elites have a tall helmet, crown or banner spike that sticks up and casts a long shadow. **Judge silhouettes from the gameplay camera, not the front view.** Do a black-silhouette test in-engine (unlit black material) at the gameplay zoom: each tier must be identifiable with no color.
- **Heads, shoulders and weapons read best from above.** Push scale on them: knight pauldrons and helmet about 15% oversized, sword about 1.2× length. Zombie heads stay normal-sized, but Bloaters get an exaggerated belly and Spitters an oversized throat sac.
- **Animation reads:** big, exaggerated poses (wide windups, a clear attack telegraph 0.3–0.5 s before the hit). Avoid subtle motion.
- **Knight priority:** the knight is never occluded. Walls and tall props near the camera use a dither cut-out sphere around the player(s) (Shader Graph screen-space dither by distance to the player). The knight also gets a **ground ring** (§8) so he's findable inside a dense crowd.

### 7.2 Color coding per zombie type
> **Naming alias:** this doc's former "T3 Brute" tier **is the GDD's Bloater** (`bloater`), and is called Bloater throughout. The GDD's Plague Spitter (`spitter`) uses the T2 budget with its own rim color (below). Grave Knight = elite, Rotting King = boss.

The base zombie skin and cloth stay **desaturated** (`#7D8574` skin, `#4B4F55` rags) so the **type color appears only as a rim/fresnel glow plus eye glow**. That's a cheap, instanced per-type property on the shared material, so it costs no extra draws.

| Type | Rim / eye color | Hex | Shape | Notes |
|---|---|---|---|---|
| T1 Shambler | Sickly green | `#8BD346` | Narrow, hunched | Bulk enemy, lowest-intensity rim (0.5) |
| T2 Runner | Toxic yellow | `#FFD23F` | Lean, forward | Fast, so a brighter rim (0.8) and an eye trail |
| T2b Plague Spitter | Hot pink | `#FF4FB3` (rim and eyes; the throat sac glows the same color) | Upright, throat sac | Rim 0.8. Ranged, so the sac glow brightens during its 0.5 s spit telegraph |
| T3 Bloater (formerly "Brute") | Violet | `#9B5DE5` | Round, wide | Rim 1.0 plus a ground disc telegraph for its 2.2 m death blast (pulsing orange-red `#FF6A3D`, like all telegraphs) |
| Elite / boss | Cyan-white | `#7FF3FF` | Spiked, tall | Pulse, off-screen arrow |

- These hues avoid red (blood), blue/orange/white (players, §8), and the cool grey floor. Spitter pink `#FF4FB3` sits about 30° of hue away from blood red `#B3121B` and is far lighter and more saturated, so it never reads as blood. It's also kept clearly apart from the Bloater's violet by value (pink is much lighter).
- **Colorblind mode:** a swap to a deuteranopia-safe (Okabe-Ito) set: Shambler `#56B4E9` sky, Runner `#F0E442` yellow, Spitter `#009E73` bluish green, Bloater `#CC79A7` reddish purple, Elite `#FFFFFF`. Shape differences stay the primary cue, especially for the Spitter, whose throat sac and upright stance identify it even if its rim color is confused with the floor or a player ring.

### 7.3 Ground and arena contrast (so blood reads on the floor)
- **Floor = cool, desaturated mid-value grey** (`#5C636B`–`#7A8087`, roughly 40–50% luminance). Fresh blood `#B3121B` and dried blood `#5A0B0E` are both clearly distinct from it, in hue and in value.
- **No red, orange, brown, or maroon in the play-area floor** (no dirt, wood decks, red rugs, or rusty metal). Brown or warm elements can exist only on the **arena perimeter walls and props**, outside the fight space.
- **Low floor detail frequency:** big flagstones and a subtle normal map, with no busy high-contrast patterns that would compete with blood, pickups, and enemies.
- **Zombies are slightly darker than the floor** (value −15%), and the knight's steel is lighter (`#A9B1BA`), giving a clear figure/ground hierarchy: **knight (light + team color) > enemy rims > blood > floor**.
- Blood gets **lower roughness (glossier)** than the floor in the ground shader, so the moonlight specular catches it even when it's dried. It's visible as wet, not just as color.
- **Validation:** a grayscale screenshot test at every milestone (blood pools and enemies must still separate from the floor in grayscale), plus the black-silhouette test above.

---

## 8. Co-op player variants (planned now, shipped later)

Goal: up to 4 knights that are instantly distinguishable at iso distance, in a crowd, while covered in blood.

### 8.1 Team-color mask channels
- The knight material uses the **same channel-packed mask texture as §4.3**: **R = blood accumulation**, **G = primary team color region** (tabard, shield face, helmet plume, cape), **B = secondary team color region** (trim and pauldron bands), **A = emissive trim** (visor slit or crest glow, low intensity).
- Shader: `albedo = lerp(lerp(base, _TeamPrimary, mask.G), _TeamSecondary, mask.B)`, then the blood layer on top (blood darkens team colors but **never fully covers the G region**: clamp blood on G to 60%, so team color always reads).
- With Sidekick (recommended knight), parts already use editable color slots via the Sidekick color map. Bake the chosen knight to one mesh, then author the G/B mask on the baked UVs (or use Sidekick's color-map regions as the mask). **Making the mask texture is custom work** (about half a day).

### 8.2 Heraldry, shield and tabard decals
- 4–8 simple, bold **heraldic charges** (lion, cross, tower, eagle, boar, sun) as a 1024² single-channel atlas, projected via a UV-mapped decal slot on the **shield face and tabard chest** (a second UV set or a mesh-decal quad, not URP Decal Projectors, so it moves with the skinned mesh).
- Heraldry uses the secondary color on the primary field (for example a white lion on a blue field). Big shapes only: readable at 60 px character height.
- **Custom:** heraldry icon set (vector, about 1 day) and the shield/tabard UV decal slot (part of the mask pass).

### 8.3 Per-player rings and outlines

| Player | Primary | Secondary | Ring / outline |
|---|---|---|---|
| P1 | Azure `#2F80ED` | White `#F2F2F2` | `#2F80ED` |
| P2 | Orange `#FF7A1A` | Black `#1A1A1A` | `#FF7A1A` |
| P3 | Teal `#14C8B4` | Gold `#F2C14E` | `#14C8B4` |
| P4 | White `#F2F2F2` | Crimson trim `#C0283A`* | `#F2F2F2` |

\* P4's crimson is a small trim only. Its identifying color is white.
- **Ground ring:** a pooled, instanced unlit ring quad (or projected decal) under each knight, in the player color at 70% alpha, with the player number "P1–P4" in the ring at low zoom. It always renders above blood (render order after the splat-map ground) so it's visible in a gore-soaked arena.
- **Occlusion outline:** a URP Renderer Feature (Render Objects pass with a stencil, or a simple inverted-hull outline) draws the knight's **silhouette in the player color when occluded** by walls or by a Bloater or elite. Cap 4 outlined objects, so it's cheap.
- **HUD:** each player's HP/XP plate is framed in their color. Damage numbers *dealt by* a player are white with a thin team-color outline (optional toggle).
- **Single-player default:** P1 azure. The ring and outline are still used, because they help find the knight in the horde.

---

## 9. Asset sourcing list: vertical slice

**Prices were checked live on 2026-10-28** by fetching each Asset Store page (the `finalPrice`/`originalPrice` fields in the page data) with curl or WebFetch. All prices are USD before tax/VAT. **Asset Store prices change often, and many Synty items below are currently on a 50% sale.** Re-check before buying. Nothing has been purchased.

**Style consistency:** the recommended set is all **Synty** (POLYGON + Sidekick, both designed to match), with Synty's own animation packs. The only non-Synty items are animation clips and blood VFX, which don't affect mesh style.

**Compatibility notes:** the Synty packs list URP support with an original Unity version of 2021.3/2022.3. They generally import into Unity 6 URP, but **verify on import** (run the Render Pipeline Converter if any material is pink). Free Blood VFX – URP and Undead Animations list Unity 6 (6000.0.x) explicitly. Zombie Monster Animations FREE lists 6000.3.

**Licenses:** "Standard Unity Asset Store EULA" (https://unity.com/legal/as-terms). Synty Asset Store items are **Restricted Single Entity** (only for the licensing entity; check seat rules if more people join), others are Single Entity. **Mixamo:** free with an Adobe ID (Dustin would have to sign in himself; I didn't). Characters and animations are royalty-free for commercial video games per Adobe's FAQ (https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html), but the raw files can't be redistributed. **Quaternius:** CC0 (public domain).

| # | Need | Item / pack | URL | Price (checked 2026-10-28) | License | Status |
|---|---|---|---|---|---|---|
| 1 | Knight (player, co-op variants) | **Fantasy Knights – Sidekick Modular Characters** (Synty). 119 knight parts, Sidekick Character Creator with editor color customization, runtime API, bake-to-single-mesh, Mecanim humanoid, works with Synty ANIMATION packs | https://assetstore.unity.com/packages/3d/characters/fantasy-knights-sidekick-modular-characters-by-synty-325898 | **$99.99** (sale; list $199.99) | Asset Store EULA, Restricted Single Entity | **Recommended** |
| 2 | Zombies (3 types) + arena environment | **POLYGON – Dark Fortress Pack** (Synty). 850+ prefabs, a modular dark castle/keep/crypt, and 13 rigged characters incl. **corpse, plague, plague lord, undeads, behemoth** (→ Shambler = undead, Runner = corpse, Plague Spitter = plague, Bloater = behemoth, Rotting King = plague lord; matches PlaceholderSpec §7.5) plus knights. URP + Built-in. | https://assetstore.unity.com/packages/3d/environments/dungeons/polygon-dark-fortress-pack-art-by-synty-334214 | **$124.99** (sale; list $249.99) | Asset Store EULA, Restricted Single Entity | **Recommended** |
| 3 | Knight combat animations | **Synty ANIMATION – Sword Combat**. 105 clips (light/heavy attacks, deaths, leaps), works with POLYGON and Sidekick | https://assetstore.unity.com/packages/3d/animations/synty-animation-sword-combat-291806 | **$29.99** (sale; list $59.99) | Asset Store EULA, Restricted Single Entity | **Recommended** |
| 4 | Zombie animations | **Undead Animations** (Polygonmaker). 35 clips, root motion + in-place, Mecanim; a reviewer confirms it works on Synty zombies; lists Unity 6000.0 | https://assetstore.unity.com/packages/3d/animations/undead-animations-189573 | **$9.99** | Asset Store EULA, Single Entity | **Recommended** |
| 5 | Extra zombie clips (variety) | **Zombie Monster Animations FREE** (Kevin Iglesias). Idle, walk, attack, damage, death | https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/zombie-monster-animations-free-259680 | **$0.00** | Asset Store EULA | **Recommended** (free) |
| 6 | Knight locomotion (run, strafe, dodge) | **Mixamo** sword-and-shield and locomotion clips, retargeted via Humanoid | https://www.mixamo.com/ | **Free** (Adobe ID sign-in required, done by Dustin) | Adobe Mixamo terms, royalty-free in games, no raw redistribution | **Recommended** (free) |
| 7 | Blood VFX (bursts + ground blood) | **Free Blood VFX – URP** (Vefects). Directional and omni splashes, lit/unlit shaders, decal-style ground blood; lists Unity 6000.0 URP | https://assetstore.unity.com/packages/vfx/free-blood-vfx-urp-375130 | **$0.00** | Asset Store EULA | **Recommended** (free) |
| 8 | Knight locomotion (Synty-matched) | Synty ANIMATION – Base Locomotion. Pairs with Sword Combat (includes transition clips and an example controller) | https://assetstore.unity.com/packages/3d/animations/synty-animation-base-locomotion-character-animset-280764 | $34.99 (sale; list $69.99) | Asset Store EULA, Restricted Single Entity | Alternative (upgrade over Mixamo) |
| 9 | Knight + medieval arena (budget) | POLYGON – Knights Pack (Synty). 5 knight and soldier characters (alt army colors), modular castle, church and cobble paths | https://assetstore.unity.com/packages/3d/environments/fantasy/polygon-knights-pack-art-by-synty-83694 | $14.99 (sale; list $29.99) | Asset Store EULA, Restricted Single Entity | Alternative (budget knight/arena; tint by atlas swap, no modular parts) |
| 10 | Zombie variety (bosses) | POLYGON – Boss Zombies Pack (Synty). 4 boss zombies with a blood-color shader; **modern** clothing, so it needs a medieval reskin | https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/polygon-boss-zombies-pack-art-by-synty-173373 | $39.99 (sale; list $79.99) | Asset Store EULA, Restricted Single Entity | Alternative (only for Bloater/boss variety) |
| 11 | Undead variety (skeleton elites) | Fantasy Skeletons – Sidekick Modular Characters (Synty) | https://assetstore.unity.com/packages/3d/characters/fantasy-skeletons-sidekick-modular-characters-by-synty-327648 | $99.99 (sale; list $199.99) | Asset Store EULA, Restricted Single Entity | Alternative (post-slice) |
| 12 | Zombie animations (full set) | Zombie Monster Animations (Kevin Iglesias), full version | https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/zombie-monster-animations-259560 | $18.00 | Asset Store EULA | Alternative |
| 13 | Blood VFX (premium) | Blood VFX – URP (Vefects), the premium version of #7; lists 6000.0 | https://assetstore.unity.com/packages/vfx/blood-vfx-urp-265906 | $44.99 | Asset Store EULA | Alternative (if #7 lacks variety) |
| 14 | Blood VFX + decals | Blood Effects Pack (RVFX). Splashes, 12 decal + 12 Decal Projector prefabs, screen blood (it also has "gut" prefabs, which **we won't use**) | https://assetstore.unity.com/packages/vfx/particles/blood-effects-pack-295450 | $15.00 | Asset Store EULA, Single Entity | Alternative |
| 15 | Stylized hit splatter | Hit Splatter Effects Pack 1 (Effectcore). Low-poly/stylized splats; last updated 2021 (older, so test in Unity 6) | https://assetstore.unity.com/packages/vfx/particles/hit-splatter-effects-pack-1-109052 | $19.95 | Asset Store EULA, Single Entity | Alternative |
| 16 | Free zombie (prototype only) | Quaternius Animated Zombie Pack (6 anims) | https://quaternius.com/packs/animatedzombie.html | Free | CC0 | Alternative (greybox only; style differs from Synty) |
| 17 | Free zombies (prototype only) | Quaternius Zombie Apocalypse Kit (4 enemies; modern setting) | https://quaternius.com/packs/zombieapocalypsekit.html | Free | CC0 | Alternative (greybox only) |
| 18 | Modular realistic knights | Modular Knights (The Tales Factory). 10.5k–42.9k tris, 4K PBR | https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/modular-knights-331098 | $180.00 | Asset Store EULA | **Not recommended** (semi-realistic, over budget, style mismatch). Listed for comparison only. |

### Recommended total
| Pack | Price |
|---|---|
| Fantasy Knights – Sidekick | $99.99 |
| POLYGON – Dark Fortress | $124.99 |
| ANIMATION – Sword Combat | $29.99 |
| Undead Animations | $9.99 |
| Zombie Monster Animations FREE | $0.00 |
| Mixamo | $0.00 |
| Free Blood VFX – URP | $0.00 |
| **Total (recommended)** | **$264.96 USD** (before tax/VAT, at current sale prices; at list prices it would be about $519.96) |

- **Budget path (−$99.99 → $164.97):** skip Sidekick and use one of Dark Fortress's knight characters as the player, with a custom team-color mask (§8.1). It's less modular, so co-op variants rely more on the tint mask and heraldry.
- **Subscription option:** Synty also sells SyntyPass (whole library from about $30/mo according to syntystore.com). It's worth comparing if more Synty packs are likely later.
- Prices change and sales come and go (the Synty items above are currently at −50%). Re-check every URL before purchase.

### Custom work (not purchasable; Unity Coder and art)
| Item | Owner | Est. |
|---|---|---|
| VAT baker (editor script) + VAT crowd shader (Shader Graph, instanced, per-type rim, `_BloodAmount`, `_FlashAmount`, dither sink) | Coder + Art | 2–3 d |
| Knight shader: blood accumulation (R) + team color (G/B) + emissive (A) mask, and the mask texture painted on the baked Sidekick knight | Art | 1–1.5 d |
| Ground shader + blood splat-map RT system (stamp pass, drying, reset) | Coder | 1–2 d |
| Heraldry icon atlas (6–8 charges) + shield/tabard UV decal slot | Art | 1 d |
| Player ground ring + occlusion outline Renderer Feature | Coder | 0.5–1 d |
| Floor atlas recolor (remove any brown/red swatches from the Dark Fortress ground tiles) | Art | 0.5 d |
| LOD1/LOD2 for crowd zombies (decimate) | Art | 0.5–1 d |
| UI kit (plates, card frames, icons) in the gothic-minimal style; fonts are free (OFL) | Art | 2–3 d |

*(Per the confirmed scope, there's no dismemberment and there are no gib assets anywhere in this plan.)*

---

## 10. Validation checklist (each milestone)
- [ ] 300 zombies (70/25/5 tier mix) + 60 corpses + 100 decal projectors (slice; 48 fresh projectors + splat map in the full game) + 96 blood systems (~3,000 particles) at **≥ 60 fps** on the min-spec PC (profile the GPU and CPU frame, crowd draws ≤ 10).
- [ ] Black-silhouette test: every tier identifiable at gameplay zoom.
- [ ] Grayscale test: blood, enemies, and pickups separate from the floor.
- [ ] Colorblind mode check (deuteranopia simulation).
- [ ] 4 knights (co-op mock) distinguishable in a dense crowd while fully blood-soaked.
- [ ] No GC allocations per frame during combat (pools prewarmed).
