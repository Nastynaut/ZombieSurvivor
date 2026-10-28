# Knight Survivors: Placeholder Art Spec

**Owner:** Asset Designer · **For:** Unity Coder · **Last updated:** 2026-10-28
**Matches:** `docs/ArtDirection.md` (palette, isometric readability §7, zombie color-coding §7.2, grey floor §7.3, blood §4, co-op tint slots §8) and `docs/GDD.md` (enemy list §3.5, XP gems §4.2, camera, 150 × 150 m map).
**Rule (Supervisor, 2026-10-28):** `docs/GDD.md` is canonical for numbers and behavior (counts, timings, sizes, caps); this spec is canonical for look (shapes, colors, materials). Where they disagree on a number, the GDD wins.
**Decision (Dustin):** no asset purchases for now. Everything below can be built from **Unity primitives + URP Lit/Unlit materials only**. Free packs are deferred (§8); nothing here depends on one.

---

## 0. Global conventions (read first)

| Rule | Value |
|---|---|
| Units | 1 Unity unit = 1 m. Y up, **+Z = forward** (facing). |
| Root pivot | **At the feet / ground contact, centered** (y = 0 is the floor). Root scale is always **(1, 1, 1)**; scale only the `Visual` children. |
| Primitive reference sizes | Cube 1 × 1 × 1 (pivot center). Sphere Ø1 (pivot center). Capsule Ø1 × 2 tall (pivot center). Cylinder Ø1 × 2 tall (pivot center). Quad 1 × 1, faces −Z (pivot center). |
| Colliders | **Delete the collider on every primitive child.** Gameplay colliders live only on the logic root (one CapsuleCollider or SphereCollider, or none for crowd zombies if the Coder uses distance checks). |
| Shadows | Knight, Grave Knight, Rotting King: Cast Shadows On. **All common zombies, gems, blood: Cast Shadows Off** (ArtDirection §2). Floor and walls: receive. |
| Materials | **Shared `.mat` assets only.** No per-instance `renderer.material` access (that clones materials and breaks the SRP Batcher). Per-instance variation is a material *swap* between shared assets, or a MaterialPropertyBlock on the knight only (≤ 4 objects, so acceptable). |
| Batching | SRP Batcher on. Enable **GPU Resident Drawer** (URP Asset → Rendering → GPU Resident Drawer: Instanced Drawing; Forward+ is already the path). Primitive zombies are MeshRenderers, so they get instanced automatically. |
| Shader | `Universal Render Pipeline/Lit` unless noted. Color values are **sRGB hex** (enter them in the color picker's Hex field). |
| Folders | `Assets/_Project/Art/Placeholder/Prefabs`, `.../Materials`, `.../VFX`. Names start with `PF_PH_` (prefab) and `M_PH_` (material) so a project search for `_PH_` finds everything to swap out later. |
| Pooling | Use the existing `KnightSurvivors.Pooling.PoolService` (`Spawn` / `Despawn` / `Prewarm`) and `IPoolable.OnSpawned()` for resets, `AutoDespawn` for timed VFX. |
| Camera reference | GDD: perspective, FOV 30°, pitch 50°, yaw 45°, distance 26 m (about 26 × 17 m of ground visible). **Judge every silhouette below from this camera**, not the Scene view front. A 1.8 m character is about 70–80 px tall at 1080p. |

### Readability rules carried over from ArtDirection §7
1. **Value hierarchy:** knight (light steel + team color) > enemy type rims/eyes > blood > floor. Zombie bodies are about 15% darker than the floor; the knight is lighter.
2. **The floor is a cool, desaturated mid grey.** No red, orange, brown, or maroon anywhere in the play area.
3. **Each zombie type is identified by shape first, color second.** Do the black-silhouette test: swap all materials for `M_PH_Debug_Black` (unlit `#000000`) at the gameplay camera. Every type must still be identifiable.
4. **Blood red `#B3121B` is reserved for blood** (and the HP bar is `#E23A3A`). No placeholder uses red for anything else.

---

## 1. Shared material list

| Material | Shader | Base color | Smooth | Metal | Emission | Used by |
|---|---|---|---|---|---|---|
| `M_PH_Knight_Steel` | Lit | `#A9B1BA` | 0.65 | 0.80 | — | Knight armor |
| `M_PH_Knight_Dark` | Lit | `#2E333A` | 0.30 | 0.20 | — | Visor slit, belt, grip |
| `M_PH_Team_Primary_P1..P4` | Lit | P1 `#2F80ED` · P2 `#FF7A1A` · P3 `#14C8B4` · P4 `#F2F2F2` | 0.25 | 0.00 | — | Tabard, shield face, plume (`TeamPrimary` slot) |
| `M_PH_Team_Secondary_P1..P4` | Lit | P1 `#F2F2F2` · P2 `#1A1A1A` · P3 `#F2C14E` · P4 `#C0283A` | 0.35 | 0.00 | — | Shield cross, pauldron bands (`TeamSecondary` slot) |
| `M_PH_Ring_P1..P4` | Unlit, Transparent, Alpha | team ring color at **alpha 0.70** (`#2F80ED` / `#FF7A1A` / `#14C8B4` / `#F2F2F2`) | — | — | — | Ground ring and facing arrow |
| `M_PH_Sword_Blade` | Lit | `#C9D0D8` | 0.80 | 0.90 | — | Blade (blood-on-weapon target, §2.3) |
| `M_PH_Zombie_Skin` | Lit | `#7D8574` | 0.20 | 0.00 | — | All zombie bodies and heads (shared!) |
| `M_PH_Zombie_Rags` | Lit | `#4B4F55` | 0.15 | 0.00 | — | Arms, clothing blocks (shared) |
| `M_PH_Eye_Shambler` | Unlit | `#8BD346` | — | — | (unlit = self-lit) | Shambler eyes + rim band |
| `M_PH_Eye_Runner` | Unlit | `#FFD23F` | — | — | | Runner |
| `M_PH_Eye_Bloater` | Unlit | `#9B5DE5` | — | — | | Bloater (ArtDirection T3 violet; ArtDirection's former "Brute") |
| `M_PH_Eye_Spitter` | Unlit | `#FF4FB3` | — | — | | Plague Spitter (**new color**, see note) |
| `M_PH_Eye_Elite` | Unlit | `#7FF3FF` | — | — | | Grave Knight, Rotting King |
| `M_PH_HitFlash` | Unlit | `#FFFFFF` | — | — | | Hit flash (material swap held for the GDD §7.1 `flashMs`, §3.7) |
| `M_PH_HitFlash_Warm` | Unlit | `#FFF1D6` (warm white) | — | — | | Elite / boss hit flash (GDD §7.3: elites tint warm white) |
| `M_PH_HurtFlash` | Unlit | `#E23A3A` (HP red, **not** blood red) | — | — | | Player-hurt flash, 120 ms (§2.5) |
| `M_PH_Floor_A` / `M_PH_Floor_B` | Lit | `#6B7178` / `#5C636B` | 0.15 | 0.00 | — | Floor tiles (checker) |
| `M_PH_Floor_Mortar` | Lit | `#474D54` | 0.05 | 0.00 | — | Under-plate showing through tile gaps |
| `M_PH_Wall` | Lit | `#2E333A` | 0.10 | 0.00 | — | Arena walls |
| `M_PH_Prop_Stone` | Lit | `#7A8087` | 0.10 | 0.00 | — | Gravestones, fence posts (cool grey only) |
| `M_PH_Torch` | Lit | `#2E333A` | 0.2 | 0.0 | `#FF9A3C` × 3 | Perimeter torches/braziers flame block |
| `M_PH_Gem_Small` / `_Medium` / `_Large` | Lit | see §4 | 0.90 | 0.00 | see §4 | XP gems |
| `M_PH_Blood_Particle` | URP `Particles/Unlit`, Transparent Alpha | `#B3121B` | — | — | — | Blood bursts |
| `M_PH_Blood_Decal` / `_Dry` | `Shader Graphs/Decal` (URP Decal Projector, §6.2) | fresh `#B3121B` / dried `#7A0A10` | 0.85 (wet) / 0.35 (dry) | 0.0 | — | Ground blood (slice pool 100, 20 s). Quad fallback: `M_PH_Blood_Decal_Quad(_Dry)`, same colors |
| `M_PH_Scorch` | `Shader Graphs/Decal` | `#26282B` (charcoal), base map `Default-Particle` | 0.10 | 0.0 | — | Blood **Off** scorch marks (§6.3), max 50 |
| `M_PH_Blood_Particle_Off` | URP `Particles/Unlit`, Transparent Alpha | `#4B4F55` grey dust | — | — | — | Blood **Off** bursts (§6.3) |
| `M_PH_Telegraph` | Unlit, Transparent, Additive | `#FF6A3D`, alpha pulsing 0.3 ↔ 0.7 at 4 Hz | — | — | `#FF6A3D` (additive = emissive look) | All attack telegraphs (§3.8) |

**Notes**
- **Spitter color** `#FF4FB3` (hot pink, rim and eyes) is now in ArtDirection §7.2 (approved). It avoids blood red, the 4 player colors, green/yellow/violet/cyan of the other types, and the grey floor. In colorblind mode the Spitter uses Okabe-Ito bluish green `#009E73` (ArtDirection §7.2), with its throat-sac shape as the primary cue.
- **Telegraph color (GDD §3.5 telegraph rule, adopted in v0.2.1):** every attack telegraph uses the shared **pulsing additive orange-red `#FF6A3D`** (alpha 0.3 ↔ 0.7 at 4 Hz), never blood red `#B3121B`. Blood never pulses or glows, which keeps them distinct. Pulse it with one `TelegraphPulse` manager that animates the shared material's color each frame (all telegraphs pulse in sync, no per-instance material). Shapes and timings: §3.8.
- **Rim light:** URP Lit has no rim term. Placeholders fake the ArtDirection "type-color rim" with a **thin unlit type-color band** (shoulder collar) plus **unlit eyes**. Optional upgrade (no pack needed): a 10-minute Shader Graph `SG_PH_ZombieRim` = Lit + `Fresnel Effect (power 3)` × `_RimColor` into Emission, with `_RimColor` exposed so one material per type works. That's the same property the final crowd shader will use.

---

## 2. Knight (player): `PF_PH_Knight`

**Size:** 1.8 m tall, about 0.7 m wide (shoulders 0.9 m with pauldrons). **Pivot:** feet, center. Faces +Z.
**Iso silhouette:** a light steel "pill" with a bright team-colored chest stripe and shield disc, broad pauldron "T" across the top, a long pale blade sticking out front-right, a tall plume, and a colored ring with an arrow on the ground. From above, it reads as a cross shape (shoulders + sword) that no zombie has.

### 2.1 Hierarchy (local position / rotation / scale, relative to parent)

```
PF_PH_Knight                         (logic root: PlayerController, CapsuleCollider r0.35 h1.8 center(0,0.9,0), scale 1)
├─ Visual                            (0,0,0)  scale 1        ← Animator lives here (AC_PH_Knight)
│  ├─ Body        Capsule            (0, 0.90, 0)     rot 0          scale (0.70, 0.90, 0.70)   M_PH_Knight_Steel
│  ├─ Tabard      Cube               (0, 0.85, 0.30)  rot 0          scale (0.50, 0.70, 0.12)   M_PH_Team_Primary_Px   [TeamPrimary]
│  ├─ Belt        Cube               (0, 0.75, 0)     rot 0          scale (0.74, 0.10, 0.74)   M_PH_Knight_Dark
│  ├─ Helmet      Sphere             (0, 1.62, 0)     rot 0          scale (0.42, 0.42, 0.42)   M_PH_Knight_Steel
│  │  ├─ Visor    Cube               (0, 0.02, 0.45)  rot 0          scale (0.70, 0.12, 0.15)   M_PH_Knight_Dark       ← direction cue (front)
│  │  └─ Plume    Cube               (0, 0.65, -0.15) rot (-20,0,0)  scale (0.15, 0.70, 0.45)   M_PH_Team_Primary_Px   [TeamPrimary]
│  ├─ Pauldron_L  Sphere             (-0.42, 1.40, 0) rot 0          scale (0.34, 0.24, 0.34)   M_PH_Knight_Steel
│  │  └─ Band     Cube               (0, -0.1, 0)     rot 0          scale (1.05, 0.25, 1.05)   M_PH_Team_Secondary_Px [TeamSecondary]
│  ├─ Pauldron_R  (mirror of L at +0.42)
│  ├─ Shield      Cylinder           (-0.48, 1.00, 0.18) rot (0,0,90) then (0,-20,0) scale (0.60, 0.04, 0.60)  M_PH_Team_Primary_Px
│  │  ├─ Cross_V  Cube               (0, 1.2, 0)      rot 0          scale (0.18, 0.5, 0.9)     M_PH_Team_Secondary_Px  ← heraldry slot
│  │  └─ Cross_H  Cube               (0, 1.2, 0)      rot 0          scale (0.9, 0.5, 0.18)     M_PH_Team_Secondary_Px
│  └─ Socket_WeaponR (empty)         (0.40, 1.00, 0.25) rot (75,0,0)  ← sword is parented here
│     └─ Sword
│        ├─ Grip   Cylinder          (0, 0, 0)        rot 0          scale (0.05, 0.10, 0.05)   M_PH_Knight_Dark
│        ├─ Guard  Cube              (0, 0.11, 0)     rot 0          scale (0.28, 0.04, 0.06)   M_PH_Knight_Steel
│        └─ Blade  Cube              (0, 0.66, 0)     rot 0          scale (0.09, 1.05, 0.025)  M_PH_Sword_Blade       [BloodWeapon target]
├─ GroundRing                        (0, 0.02, 0)     rot 0          (no Animator influence; stays flat)
│  ├─ Disc        Cylinder           (0, 0, 0)        scale (1.50, 0.005, 1.50)                M_PH_Ring_Px  (Cast Shadows Off)
│  └─ FacingArrow Cube               (0, 0, 0.95)     rot (0,45,0)   scale (0.30, 0.01, 0.30)   M_PH_Ring_Px  ← points along +Z
├─ Socket_Hit     (empty)            (0, 1.20, 0)     ← blood burst / damage number spawn
└─ Socket_Overhead(empty)            (0, 2.20, 0)     ← co-op name tag / mini HP bar
```

- The **direction cue** is triple-redundant: the dark visor slit on the front of the helmet, the sword projecting forward-right, and the ground-ring arrow. The arrow is the one that reads in a dense crowd.
- Ring renders **after** ground blood (Unlit transparent, Render Queue 3010 or sorting priority +1), so it's visible on a blood-soaked floor (ArtDirection §8.3).
- Sword length about 1.2 m (slightly oversized for iso reads, per ArtDirection §7.1).

### 2.2 Co-op tint slots
- Every team-colored renderer is tagged by **which slot** it uses: `TeamPrimary` (Tabard, Plume, Shield) and `TeamSecondary` (Pauldron bands, Shield cross).
- A `KnightTint` component on the root holds `int playerIndex` (0–3) and two arrays of renderers. On spawn it assigns the shared `M_PH_Team_Primary_P{n}` / `M_PH_Team_Secondary_P{n}` materials, and `M_PH_Ring_P{n}` to the ring. No runtime material creation.
- This maps 1:1 to the future mask shader: `TeamPrimary` → mask **G**, `TeamSecondary` → mask **B** (ArtDirection §8.1). Keep the property names `_TeamPrimary` and `_TeamSecondary` in code so the Synty swap only changes how they're applied.
- Heraldry: the shield cross is the placeholder for the heraldry decal slot. Optional variants per player: P1 cross, P2 single horizontal bar, P3 single vertical bar, P4 two diagonal bars (cubes rotated ±45°). That gives a shape cue in addition to color.
- Single player uses P1 (azure).

### 2.3 Blood on the weapon (placeholder of ArtDirection §4.3)
- **Material properties (names fixed now):** `_BloodWeapon` (float 0–1), `_BloodAmount` (float 0–1, armor), `_BloodColor` (`#7A0A10`).
- Placeholder behavior with URP Lit (which doesn't have these properties): a `KnightBloodState` component stores both floats and writes them through **one MaterialPropertyBlock** on the Blade renderer: `_BaseColor = lerp(#C9D0D8, #7A0A10, _BloodWeapon × 0.85)` and `_Smoothness = lerp(0.80, 0.90, _BloodWeapon)` (wet). It does the same for `_BloodAmount` on the Body renderer, capped at 0.5 so steel stays readable, and **never touches team-slot renderers** (team color must always read).
- Also call `SetFloat("_BloodWeapon", v)` / `SetFloat("_BloodAmount", v)` on the same MPB. URP Lit ignores them, but the future Shader Graph will pick them up with zero code changes.
- Rates from ArtDirection: +0.01 `_BloodWeapon` per kill (cap 1), +0.004 `_BloodAmount` per kill, decay −0.01/s, reset on level-up.

### 2.4 Animator placeholder: `AC_PH_Knight`
- Parameters (keep these names for Synty): `Speed` (float, m/s), `MoveX`/`MoveZ` (float, local), `Attack` (trigger), `AttackIndex` (int), `Dodge` (trigger), `Hit` (trigger), `Dead` (bool).
- States: `Locomotion` (empty clip), `Attack`, `Dodge`, `Hit`, `Death`. All have **empty placeholder clips** (`AN_PH_Empty`), or optional tiny clips made in Unity's Animation window: a 0.2 s Visual bob of ±0.05 m for walking, a Socket_WeaponR 120° swing for attacks, a 360° Y spin + 0.3 m dip for dodge.
- Apply Root Motion: **off** (movement is code-driven).

### 2.5 Player-hurt flash (GDD §7.1 `PlayerHurt`)
- On damage, swap the knight's body/helmet/pauldron renderers (not the team-slot or ring renderers) to `M_PH_HurtFlash` (`#E23A3A`, HP red) for **120 ms** (GDD `flashMs`), and flash the HUD HP bar the same color. Hit-stop 60 ms global and shake 0.25 m / 0.20 s come from the GDD table, not from art.
- `#E23A3A` is the UI HP red, deliberately lighter and more orange than blood `#B3121B`, so a hurt knight never reads as "covered in blood".

---

## 3. Zombies

All zombies share `M_PH_Zombie_Skin` and `M_PH_Zombie_Rags`. Only the **eye + collar band** material differs by type (one shared unlit material per type). Every zombie root has: logic components, an optional collider (CapsuleCollider on the root, or none if the Coder uses distance checks), `Visual` child with the Animator, `Socket_Hit` at chest height, `Cast Shadows Off` on all children (except elites).

Common to every type: **Eyes** = two Spheres scale (0.07, 0.07, 0.07) at head-local (±0.12, 0.05, 0.42), type eye material. **CollarBand** = Cylinder scale (1.05 × body width, 0.02, 1.05 × body width) at the neck, type eye material (fake rim, readable from above).

### 3.1 Shambler: `PF_PH_Zombie_Shambler` (GDD `shambler`, ArtDirection T1)
**Size:** 1.7 m tall, 0.55 m wide, arms reach 0.6 m forward. **Pivot:** feet. **Type color:** green `#8BD346`.
**Iso silhouette:** a narrow hunched "|" with two long arms sticking straight forward ("zombie arms"), small head tipped forward. The classic shape; the most common thing on screen, so the plainest.

```
PF_PH_Zombie_Shambler
└─ Visual          rot (12,0,0)   ← whole body leans forward 12°
   ├─ Body     Capsule   (0, 0.80, 0)      scale (0.55, 0.80, 0.50)   M_PH_Zombie_Rags
   ├─ Head     Sphere    (0, 1.45, 0.10)   scale (0.32, 0.32, 0.32)   M_PH_Zombie_Skin
   │  └─ Eyes ×2                                                         M_PH_Eye_Shambler
   ├─ Collar   Cylinder  (0, 1.25, 0.02)   scale (0.58, 0.02, 0.55)   M_PH_Eye_Shambler
   ├─ Arm_L    Cube      (-0.22, 1.15, 0.35) scale (0.12, 0.12, 0.65) M_PH_Zombie_Skin
   └─ Arm_R    Cube      ( 0.22, 1.10, 0.35) rot (8,0,0) scale (0.12, 0.12, 0.60) M_PH_Zombie_Skin  ← slight asymmetry
```

### 3.2 Runner: `PF_PH_Zombie_Runner` (GDD `runner`, ArtDirection T2)
**Size:** 1.6 m tall, **0.40 m wide** (thinnest), heavy 30° forward lean. **Pivot:** feet. **Type color:** yellow `#FFD23F`.
**Iso silhouette:** a thin diagonal "/" slash leaning hard toward the player with arms swept **back**. That's the opposite of the Shambler's forward arms, so they're distinguishable without color.

```
PF_PH_Zombie_Runner
└─ Visual          rot (30,0,0)
   ├─ Body     Capsule   (0, 0.78, 0)      scale (0.40, 0.78, 0.36)   M_PH_Zombie_Rags
   ├─ Head     Sphere    (0, 1.38, 0.08)   scale (0.28, 0.28, 0.28)   M_PH_Zombie_Skin
   │  └─ Eyes ×2 (scale 0.09; brighter, bigger)                          M_PH_Eye_Runner
   ├─ Collar   Cylinder  (0, 1.18, 0)      scale (0.42, 0.02, 0.38)   M_PH_Eye_Runner
   ├─ Arm_L    Cube      (-0.20, 0.95, -0.30) rot (-35,0,0) scale (0.10, 0.10, 0.55) M_PH_Zombie_Skin
   └─ Arm_R    Cube      ( 0.20, 0.95, -0.30) rot (-35,0,0) scale (0.10, 0.10, 0.55) M_PH_Zombie_Skin
```
Optional: a TrailRenderer on each eye (time 0.15 s, width 0.05 → 0, `M_PH_Eye_Runner`) for the "eye trail" from ArtDirection. Pool-safe: call `TrailRenderer.Clear()` in `OnSpawned()`.

### 3.3 Bloater: `PF_PH_Zombie_Bloater` (GDD `bloater`, ArtDirection T3; formerly called "Brute" there)
**Size:** 1.5 m tall, **1.4 m wide** (round). **Pivot:** feet. **Type color:** violet `#9B5DE5`.
**Iso silhouette:** a big round "O" blob on stubby legs, with a tiny head on top. It's the only round enemy.

```
PF_PH_Zombie_Bloater
└─ Visual
   ├─ Belly    Sphere    (0, 0.85, 0)       scale (1.40, 1.20, 1.30)  M_PH_Zombie_Skin
   ├─ Leg_L    Capsule   (-0.30, 0.20, 0)   scale (0.30, 0.20, 0.30)  M_PH_Zombie_Rags
   ├─ Leg_R    Capsule   ( 0.30, 0.20, 0)   scale (0.30, 0.20, 0.30)  M_PH_Zombie_Rags
   ├─ Head     Sphere    (0, 1.55, 0.15)    scale (0.30, 0.30, 0.30)  M_PH_Zombie_Skin
   │  └─ Eyes ×2                                                        M_PH_Eye_Bloater
   └─ Band     Cylinder  (0, 0.85, 0)       scale (1.44, 0.03, 1.34)  M_PH_Eye_Bloater   ← violet belt line around the widest part
```
**Swell telegraph** (GDD: 0.8 s before exploding): scale Belly 1.0 → 1.3 over 0.8 s and swap the Band to `M_PH_HitFlash` at 8 Hz. The 2.2 m blast radius shows as a `M_PH_Telegraph` disc (§3.8).

### 3.4 Plague Spitter: `PF_PH_Zombie_Spitter` (GDD `spitter`)
**Size:** 1.7 m tall, 0.5 m wide. **Pivot:** feet. **Type color:** pink `#FF4FB3` (new, see §1 note).
**Iso silhouette:** upright (no lean, unlike the others), with a **large glowing throat sac** sphere in front of the chest and **no forward arms**. It reads as a "lollipop/lantern" shape. The upright stance also signals "ranged, keeps distance".

```
PF_PH_Zombie_Spitter
└─ Visual
   ├─ Body     Capsule   (0, 0.82, 0)      scale (0.50, 0.82, 0.45)   M_PH_Zombie_Rags
   ├─ Head     Sphere    (0, 1.50, 0)      scale (0.30, 0.30, 0.30)   M_PH_Zombie_Skin
   │  └─ Eyes ×2                                                         M_PH_Eye_Spitter
   ├─ Sac      Sphere    (0, 1.20, 0.28)   scale (0.40, 0.40, 0.40)   M_PH_Eye_Spitter   ← the read
   └─ Arms     Cube      (0, 0.95, 0)      scale (0.70, 0.10, 0.10)   M_PH_Zombie_Skin   ← arms out sideways, not forward
```
**Bile glob projectile** `PF_PH_BileGlob`: Sphere scale 0.3, `M_PH_Eye_Spitter`, pooled (cap 32).

### 3.5 Grave Knight (elite): `PF_PH_Elite_GraveKnight` (GDD `grave_knight`, **1.6× scale**)
**Size:** 2.9 m tall (1.8 m × 1.6), 1.4 m wide with pauldrons. **Pivot:** feet. **Type color:** cyan-white `#7FF3FF`. **Casts shadows.**
**Iso silhouette:** a huge dark armored knight with a tall **helmet spike** and angular **cube pauldrons**. Think the player's knight shape, but dark, bigger, square-shouldered, with no team color. The long shadow from the spike reads from far away.

```
PF_PH_Elite_GraveKnight          (root scale 1; Visual scale 1.6)
└─ Visual          scale (1.6, 1.6, 1.6)
   ├─ Body       Capsule  (0, 0.90, 0)       scale (0.75, 0.90, 0.70)  M_PH_Knight_Dark
   ├─ Helmet     Cube     (0, 1.62, 0)       scale (0.40, 0.42, 0.42)  M_PH_Knight_Dark
   │  ├─ Spike   Cube     (0, 0.95, 0)       rot (0,45,0) scale (0.20, 1.20, 0.20)  M_PH_Zombie_Rags
   │  └─ Eyes ×2 (as a visor slit: Cube (0,0,0.51) scale (0.8,0.12,0.05))           M_PH_Eye_Elite
   ├─ Pauldron_L Cube     (-0.48, 1.38, 0)   rot (0,0,20)  scale (0.40, 0.20, 0.45) M_PH_Zombie_Rags
   ├─ Pauldron_R Cube     ( 0.48, 1.38, 0)   rot (0,0,-20) scale (0.40, 0.20, 0.45) M_PH_Zombie_Rags
   ├─ Collar     Cylinder (0, 1.30, 0)       scale (0.85, 0.03, 0.80)  M_PH_Eye_Elite
   └─ Greatsword Cube     (0.45, 0.90, 0.40) rot (20,0,0) scale (0.14, 1.60, 0.04) M_PH_Zombie_Rags
```
- Charge telegraph (GDD §3.5: 0.8 s windup, orange-red telegraph line, 8 m): see §3.8.
- Off-screen arrow in `#7FF3FF` (UI).

### 3.6 Rotting King (boss): `PF_PH_Boss_RottingKing` (GDD boss)
**Size:** about 4.0 m tall (Visual scale 2.2 on a Bloater-like base). **Pivot:** feet. **Type color:** cyan-white `#7FF3FF` with a pulse. **Casts shadows.**
**Iso silhouette:** an enormous bloated mass with a **crown ring of spikes** on top and long dragging arms. It's the biggest thing on screen.
- Build it as a variant of the Bloater: Belly Sphere scale (1.3, 1.1, 1.2), Head Sphere (0.35), **Crown** = Cylinder scale (0.40, 0.06, 0.40) at head top + 5 Cubes scale (0.06, 0.18, 0.06) around it, using `M_PH_Gem_Large` (gold). Arms = 2 Cubes scale (0.18, 0.18, 1.0) hanging down and forward. Eyes, Band: `M_PH_Eye_Elite` with an emission pulse via material swap or Shader Graph optional.
- Telegraphs for Ground Slam, Raise the Dead and Bile Wave: see §3.8.

### 3.7 Zombie hit flash, death, and corpses (placeholder of ArtDirection §4.4)
- **Hit flash:** swap all Visual renderers to the shared `M_PH_HitFlash` (elites and boss: `M_PH_HitFlash_Warm`) and **hold it for the GDD §7.1 `flashMs`**: Normal 80 ms, Crit 100 ms, EliteHit 80 ms, BossHit 60 ms (read `HitFeedbackEntry.flashMs`, don't hard-code). Then swap back. It's a shared-material swap (cache the `sharedMaterials` arrays once in `Awake`), so there's no clone. The final crowd shader drives the same timing through the GDD's `_FlashAmount` 0→1→0.
- **Death:** disable the collider and AI. Rotate `Visual` to lie down: tip 90° on local X over 0.25 s (or play `Death`).
- **Corpse (GDD §7.5):** lie for **3 s**, then **sink and dither-fade over 1.5 s**: sink `Visual` local Y by −0.6 m while a shared Shader Graph `SG_PH_DitherFade` (Lit + `Dither` node into Alpha Clip, `_Fade` 1 → 0) fades it out, then `PoolService.Despawn`. Corpses use a corpse-only material copy (`M_PH_Zombie_Skin_Corpse` / `_Rags_Corpse`) with `_Fade` set per corpse through a MaterialPropertyBlock (≤ 60 corpses, so the batching cost is fine). Until that Shader Graph exists, sink only: the floor is opaque, so the sink alone still hides it.
- **Cap:** **60 corpses** max (GDD §7.8). Bloaters and elites count as 3 (GDD §3.5.1). When over the cap, the oldest sinks and fades in 0.4 s.
- **Prewarm** (GDD §13 #1): 400 Shamblers, **200 Runners, 60 Bloaters, 60 Spitters**, 3 Grave Knights, 1 Rotting King. **Pools are growable**: past the prewarm, `PoolService` instantiates more (log a warning in development builds) instead of refusing a spawn. The spawner's hard cap (350 + 80 for events, GDD §5) is what limits the count, not the pool.
- **Wounded look (optional):** a second shared skin material `M_PH_Zombie_Skin_Bloody` (base `#7D8574` blended 30% toward `#7A0A10` = `#784A44`) swapped in below 50% HP. That's the placeholder for the instanced `_BloodAmount` on zombies.

### 3.8 Telegraphs (all `M_PH_Telegraph`: pulsing additive orange-red `#FF6A3D`)
Shapes and timings come from GDD §3.5. Every telegraph is a pooled flat mesh at y = **0.02** (above blood decals and the knight ring), Cast Shadows Off, rendered after ground blood (Render Queue 3020). It appears at the start of the windup, pulses alpha 0.3 ↔ 0.7 at 4 Hz for the whole windup, and despawns when the attack fires. Shapes are **locked when the windup starts** (they don't track the player), so the player can dodge.

| Attack (GDD) | Prefab | Shape and size | Timing | Pool |
|---|---|---|---|---|
| Plague Spitter spit | `PF_PH_Tele_AimLine` | Flat Quad (rot (90,0,0)), width **0.3 m** (the bile glob's diameter), pivot at the Spitter's sac, length = distance to the target at windup start, clamped to 9 m. Plus the sac pulse: Sac scale 1.0 ↔ 1.25 at 4 Hz. | 0.5 s, then the glob fires along the line (8 m/s) | 32 |
| Grave Knight charge | `PF_PH_Tele_ChargeLine` | Flat Quad, width 1.0 m (about the elite's body width), length **8 m**, pivot at the elite's feet pointing along the charge | 0.8 s windup | 4 |
| Bloater death blast | `PF_PH_Tele_Disc` | Cylinder disc scale (4.4, 0.005, 4.4) = **2.2 m radius**, centered on the Bloater | 0.8 s swell | 16 |
| Rotting King Ground Slam | `PF_PH_Tele_Disc` (scaled) | Cylinder disc scale (8, 0.005, 8) = **4 m radius**, centered on the boss | 1.2 s | shared with the disc pool |
| Rotting King Raise the Dead | `PF_PH_Tele_SpawnMarker` | **12 markers** evenly spaced (every 30°) on a **6 m ring** around the player's position at roar start. Each marker = Cylinder disc scale (1.0, 0.005, 1.0) (1 m across, a bit wider than a Shambler) + a thin upright Cube (0.08, 0.6, 0.08) "grave stake" in the center, both `M_PH_Telegraph`. Each Shambler spawns on its marker. | 1.0 s roar, then the 12 Shamblers spawn | 24 |
| Rotting King Bile Wave (phase 2) | `PF_PH_Tele_Cone` | **3 cones of 30°, 10 m long**, from the boss toward the player: centered at −40°, 0°, +40° from the aim direction (10° gaps you can stand in). Each cone is a flat fan mesh (8 segments, radius 10 m), generated once in code at startup and shared by all instances (a primitive can't make a sector). | 0.8 s | 6 |

---

---

## 4. XP gems: `PF_PH_Gem_Small` / `_Medium` / `_Large`

GDD §4.2 defines 3 tiers: Small (1 XP), Medium (5), Large (25), in cyan / green / gold (the GDD adopted these colors in v0.2.1).

| Tier | Value | Shape (primitives) | Size | Base | Emission | Smooth | Iso read |
|---|---|---|---|---|---|---|---|
| Small | 1 | Cube rotated (45, 0, 45) = a diamond | 0.22 m | `#3FE0FF` (cyan) | `#3FE0FF` × 1.5 | 0.9 | Tiny cyan sparkle |
| Medium | 5 | Cube rotated (45, 0, 45), scale (0.30, 0.45, 0.30): tall diamond | 0.45 m tall | `#4DFF7A` (green) | `#4DFF7A` × 2.0 | 0.9 | Green spike |
| Large | 25 | Two Cubes: one at (45,0,45) and one at (45,45,45), each 0.40 m (8-point star) | 0.55 m | `#F2C14E` (gold) | `#F2C14E` × 2.5 | 0.9 | Gold star, the only gold on the floor |

- **Pivot:** at the ground (root at y = 0). The visual child floats at y = 0.35 and **bobs** ±0.05 m at 1.5 Hz and **spins** 90°/s around Y. Do this in one `GemAnimator` manager over all gems (no per-gem Update) or a tiny vertex-offset shader later.
- Metallic 0, Cast Shadows Off, no collider on the visual (pickup uses the magnet radius check).
- Bloom (threshold 1.1) makes the emission glow, which is intended (ArtDirection: only emissive pickups, fire, and level-ups bloom).
- Ground cap 400 (GDD); pool 400 Small, 64 Medium, 32 Large.
- **Why not plain blue:** P1 azure `#2F80ED` is the knight's ring color, so cyan `#3FE0FF` is used for the small gem instead (lighter and greener). Gold for Large matches the UI rarity gold `#F2C14E`. Watch that Large gems stay distinguishable from the Runner's yellow `#FFD23F` eyes: gems are on the floor, spin, and bloom, so they should be.

---

## 5. Arena: floor, walls, props

**GDD map:** 150 × 150 m, bounded by walls. World origin at the arena center, floor top at **y = 0**.

### 5.1 Floor: `PF_PH_Arena_Floor`
- **Under-plate:** Cube at (0, −0.15, 0), scale (150, 0.2, 150), `M_PH_Floor_Mortar` `#474D54`. Static.
- **Tiles:** a 15 × 15 grid of 10 m tiles. Each is a Cube scale (9.9, 0.1, 9.9) at (x, −0.05, z) so its top is at y = 0, with 0.1 m gaps showing the mortar color as grout lines. Alternate `M_PH_Floor_A` `#6B7178` / `M_PH_Floor_B` `#5C636B` in a checker. Mark all **Static** (static batching). Generate it with a small editor script (`ArenaPlaceholderBuilder`) instead of by hand.
- **Iso read:** a calm, large cool-grey checker. The 10 m grid also helps judge speed and distance during playtests. There's no texture, so there's nothing for blood to hide in.
- Smoothness 0.15 (matte), so wet blood (smoothness 0.85) catches the moonlight specular and reads as wet (ArtDirection §7.3).
- Optional: a few `#4F6B5A` (cool moss) tiles, max 15% of tiles.

### 5.2 Walls: `PF_PH_Arena_Wall`
- 4 Cubes: north/south at (0, 1.5, ±75.5) scale (152, 3, 1); east/west at (±75.5, 1.5, 0) scale (1, 3, 152). `M_PH_Wall` `#2E333A`. Static, cast + receive shadows.
- **Iso read:** a dark frame darker than the floor, so the play space pops (ArtDirection §2). 3 m height is enough to read without blocking the camera much. Walls between the camera and the player use the dither-fade from the GDD (30% alpha). Placeholder: swap to `M_PH_Wall_Fade` (same color, Transparent, alpha 0.3) when a raycast from the camera to the knight hits a wall.
- Perimeter torches every 15 m along the walls: `PF_PH_Torch` = Cylinder post (scale (0.15, 1.0, 0.15), `M_PH_Wall`) + Cube flame (scale 0.25, `M_PH_Torch`, emissive `#FF9A3C` × 3) at y = 2.2, plus optionally 1 shadowless Point Light (range 6, intensity 2, `#FF9A3C`) on every second torch (keeps it ≤ 8 visible, per ArtDirection).

### 5.3 Props (GDD: gravestones, fences, 12 braziers)
| Prefab | Primitives | Size | Material | Iso read |
|---|---|---|---|---|
| `PF_PH_Gravestone` | Cube scale (0.6, 0.9, 0.18) at (0, 0.45, 0) + Cylinder cap rot (90,0,0) scale (0.6, 0.09, 0.6) at (0, 0.9, 0) | 0.6 × 1.05 × 0.18 m | `M_PH_Prop_Stone` `#7A8087` | Small light-grey tombstone with a rounded top |
| `PF_PH_Fence` (4 m segment) | 2 posts Cube (0.15, 1.0, 0.15) at x = ±2 + 2 rails Cube (4, 0.08, 0.08) at y = 0.4 / 0.8 | 4 × 1 m | `M_PH_Prop_Stone` (**not wood brown**) | Thin grey line |
| `PF_PH_Brazier` (destructible) | Cylinder bowl scale (0.7, 0.2, 0.7) at y = 0.8 + Cylinder stand (0.15, 0.4, 0.15) at y = 0.4 + Cube flame (0.35) at y = 1.05 | Ø0.7 × 1.2 m | Wall dark + `M_PH_Torch` flame | Small orange glow on a dark stand |

- Pivot at the base center for all props. Colliders: one BoxCollider on the root.
- **Braziers sit inside the play area** (GDD), while ArtDirection keeps fire at the perimeter. Placeholder braziers use the **emissive flame only, with no point light and no warm ground light**, so they don't warm the floor around the blood. Flag for the final art pass.

---

## 6. Blood

### 6.1 Blood bursts: `PF_PH_Blood_Hit`, `PF_PH_Blood_Crit`, `PF_PH_Blood_Kill`
Built-in **Particle System (Shuriken)**, pooled via `PoolService` with `AutoDespawn` (Lifetime = duration + max particle lifetime). One shared material `M_PH_Blood_Particle`.

**Primitive-only material:** `Universal Render Pipeline/Particles/Unlit`, Surface Transparent, Blending Alpha, Color `#B3121B`, base map = Unity's built-in **`Default-Particle`** texture (soft round dot, ships with the editor, so no pack is needed). Alternative: Renderer → Render Mode **Mesh** with the built-in **Sphere** mesh and no texture (opaque `#B3121B` Unlit), which looks chunkier and more stylized.

| Setting | `PF_PH_Blood_Hit` | `PF_PH_Blood_Crit` | `PF_PH_Blood_Kill` |
|---|---|---|---|
| Duration / Looping | 0.20 s / off | 0.25 s / off | 0.30 s / off |
| Play On Awake | **off** (pool calls `Play()` in `OnSpawned`) | off | off |
| Start Lifetime | 0.35–0.55 | 0.40–0.60 | 0.45–0.70 |
| Start Speed | 3–6 | 5–9 | 4–8 |
| Start Size | 0.06–0.14 | 0.08–0.16 | 0.10–0.22 |
| Start Color | Random between `#B3121B` and `#7A0A10` | same, plus 20% `#FF4A4A` | same |
| Gravity Modifier | 2.5 | 2.5 | 3.0 |
| Simulation Space | **World** | World | World |
| Max Particles | 24 | 36 | 32 |
| Emission (GDD §7.4, × `bloodIntensity`) | `ps.Emit(n)` with n = `clamp(4 + 20 × damage / maxHealth, 4, 24)` | the same n × 1.5 (max 36), plus a 0.25 s arterial spray (Emission Rate over Time 40 for 0.25 s, Cone 10°) | Kill: **12**. Overkill kill (≥ 2× remaining HP): **30**, radial |
| Shape | Cone, angle 25°, radius 0.05, aligned to hit direction (rotate the instance to face away from the attacker) | Cone 35° | Sphere, radius 0.15, hemisphere up |
| Size over Lifetime | 1 → 0.3 (ease out) | same | same |
| Color over Lifetime | alpha 1 → 1 (0–70%) → 0 | same | same |
| Collision | **off** | off | off |
| Sub Emitters / Lights / Trails | off | off | off |
| Renderer | Billboard (or Mesh: Sphere), Cast Shadows Off, Receive Off, Sort Mode None | | |
| Stop Action | **Callback** → `PoolService.Despawn` (or let `AutoDespawn` handle it) | | |
| Ground decal (GDD §7.4) | droplets: 10% chance of a small splat (subject to the decal budget) | same as Hit | Kill: 1 death splatter. Overkill: +1 large decal (1.5–2.5 m) |

**Big bursts:** `PF_PH_Blood_Big` = the Kill prefab with Max Particles 80, Sphere radius 0.3, Start Size 0.12–0.28. **Bloater death: 40, radial, green-red bile** (Start Color random between `#B3121B` and bile green `#7C8F2A`) + a 2.5 m bile decal (`M_PH_Blood_Decal` tinted toward `#5E5A1E`, same lifetime). **Elite or boss kill: 80** + 3 decals (large set).

Emit counts are **GDD §7.4 numbers** (driven by `HitFeedbackEntry.bloodParticles`), not art choices. The prefab only sets the look; the Max Particles values above are the GDD maximum for each prefab.

**Pool sizes and caps** (GDD §7.4 / §7.8, ArtDirection §4.1): prewarm Hit 64, Crit 24, Kill 48, Big 8. **Max 96 active blood systems** and **about 3,000 live particles**. When the 96 cap is hit, **steal the oldest** (this budget is the one pool that doesn't grow).
In `OnSpawned()`: `ps.Clear(true); ps.Play(true);`. In `OnDespawned()`: `ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)`.
Spawn at the zombie's `Socket_Hit`, rotated toward `−attackerDirection`.

**Blood setting:** see §6.3.

### 6.2 Ground blood decals: `PF_PH_BloodDecal`
**Slice (GDD §7.5 / §7.8): 100 URP Decal Projectors**, pooled (prewarm 100). **Lifetime 20 s: 16 s solid, then a 4 s fade.** When the pool is full, **recycle the oldest**. Max 32 new decals per frame (queue the rest). The full-game ArtDirection §4.2 hybrid (**48** fresh projectors + a baked render-texture splat map for unlimited persistence) **comes later** and replaces this; don't build the splat map for the slice.

**Default: URP Decal Projector (built into URP, no pack).** Add the **Decal** Renderer Feature to the URP Renderer (Technique: **Screen Space**, works with Forward+). `PF_PH_BloodDecal` = DecalProjector, size (S, S·0.8, 0.5) where S is random 0.6–1.4 m (overkill/elite: 1.5–2.5 m; Bloater bile: 2.5 m), Projection Depth 0.5, pointing down (rot (90, randomY, 0)), material `M_PH_Blood_Decal` (shader `Shader Graphs/Decal`, base map `Default-Particle`). Rendering layer mask = Ground only, so it doesn't land on zombies.

**Drying, inside the 20 s window** (fresh `#B3121B` → dried `#7A0A10`):

| Time | Look | How |
|---|---|---|
| 0–6 s | Fresh, wet: `#B3121B`, smoothness 0.85 | `M_PH_Blood_Decal` |
| 6 s (GDD `decalWetTime`) | Dried: `#7A0A10`, smoothness 0.35 | swap to the shared `M_PH_Blood_Decal_Dry` (a material swap, so no per-instance material) |
| 16–20 s | Fade out | `DecalProjector.fadeFactor` 1 → 0 (per projector, no material clone) |

Blood **Low** (GDD §7.5): lifetime 10 s, so dry at 4 s, solid until 6 s, then the same 4 s fade.

**Merge rule (GDD §7.5):** if 3 or more decals are already within 1.0 m, scale the nearest one up 15% (max 2.0×) and reset its lifetime (and its fresh color) instead of spawning a new one. This builds pools in high-traffic areas.

**Fallback (only if the Decal Renderer Feature can't be used): pooled flat Quads.** Same cap (100), lifetime, drying times and merge rule.
- `PF_PH_BloodDecal_Quad` = Quad lying flat (rot (90, randomY, 0)), scale (S, S·0.8, 1), at y = **0.003 + (spawnIndex % 8) × 0.0005** (above the floor, below the knight ring; the offset avoids z-fighting).
- Material `M_PH_Blood_Decal_Quad` = URP Unlit, Alpha Clipping on (threshold 0.5), base map `Default-Particle` (so the quad reads as a round splat), color `#B3121B`; dry swap at 6 s to `M_PH_Blood_Decal_Quad_Dry` (`#7A0A10`). Cast Shadows Off.
- Fade = scale the quad to 0 over the last 4 s (alpha-clipped, so no per-instance alpha).

**Both versions:** dark-red pools on the grey checker, separated by hue and value (grayscale test: darker spots). Clear all decals between runs (`PoolService.Despawn` all) or on the GDD's victory screen.

### 6.3 Blood setting (GDD §7.5: Off / Low / Full)
- **Full:** everything above.
- **Low:** half the particles (`bloodIntensity` 0.5), decal lifetime 10 s.
- **Off:** the same burst prefabs with `M_PH_Blood_Particle_Off` (`#4B4F55` grey dust/ash) at **0.5× the counts**. **No red decals.** Bloater bile turns grey. **Scorch marks** replace blood decals: `PF_PH_Scorch` = the decal projector with `M_PH_Scorch` (`#26282B` charcoal, smoothness 0.1), same sizes and spawn triggers as blood decals, its own pool capped at **50** (recycle the oldest), same 20 s lifetime and 4 s fade, no drying swap.

---

## 7. Swap-out plan (so Synty art drops in later)

### 7.1 Prefab structure (all characters)
```
PF_<Name>                    ← LOGIC ROOT. Never replaced. Scale (1,1,1). Pivot = feet, center. +Z forward.
│   Components: controller/AI, Health, Team, collider, pool hooks (IPoolable), KnightTint / KnightBloodState (knight),
│               VisualBinder (references below, filled in the Inspector)
├─ Visual                    ← SWAPPABLE. Holds the Animator. Placeholder primitives OR the Synty model go here.
│   └─ (model children)
├─ Socket_Hit                ← blood, damage numbers (chest height)
├─ Socket_Overhead           ← name tag / HP bar
├─ Socket_WeaponR / Socket_Shield (knight; *also* duplicated as bones inside the Synty rig later, see 7.4)
└─ GroundRing (knight only)  ← never inside Visual so it doesn't animate
```
- **Rule:** gameplay code may only reference the root, the sockets, and `VisualBinder` (which exposes `Animator`, `Renderer[] bodyRenderers`, `Renderer[] teamPrimaryRenderers`, `Renderer[] teamSecondaryRenderers`, `Renderer weaponRenderer`). **Never** `transform.Find("Visual/Head")` or similar, because those paths die at swap time.
- Make each character prefab a **Prefab Variant**-friendly base: `PF_Knight_Base` (logic) → `PF_PH_Knight` (variant with placeholder Visual) → later `PF_SY_Knight` (variant with the Synty Visual). Gameplay spawns whichever variant the `EnemyDefinition`/character data points to, so swapping is a data change.

### 7.2 Scale and pivot convention
- Character heights: Knight 1.8 m, Shambler 1.7 m, Runner 1.6 m (leaning), Bloater 1.5 m tall / 1.4 m wide, Spitter 1.7 m, Grave Knight 2.9 m (1.6×), Rotting King about 4.0 m (2.2×).
- When importing a Synty model: FBX Import Scale Factor 1 (Synty uses meters). Put it under `Visual`, then scale `Visual` (not the root) so its height matches the table above. Feet at y = 0, facing +Z (Synty characters face +Z by default; check on import).
- Elite and boss scale (1.6×, 2.2×) is applied on `Visual` only, and the collider on the root is sized by data (GDD `mass` / radius), not by the model.

### 7.3 Material slots
| Placeholder material(s) | Future Synty / final material | Notes |
|---|---|---|
| `M_PH_Knight_Steel`, `M_PH_Knight_Dark`, `M_PH_Sword_Blade` | Sidekick baked knight material (Synty color map) → later `M_Knight_Mask` (custom Shader Graph with R blood / G primary / B secondary / A emissive) | Keep `_BloodWeapon`, `_BloodAmount`, `_BloodColor`, `_TeamPrimary`, `_TeamSecondary` property names |
| `M_PH_Team_Primary_P1..4`, `M_PH_Team_Secondary_P1..4` | Values of `_TeamPrimary` / `_TeamSecondary` on the knight mask material (set via MPB per player) | `KnightTint` switches from material swap to MPB |
| `M_PH_Zombie_Skin`, `M_PH_Zombie_Rags` | Dark Fortress shared POLYGON atlas material | One material for all zombie types |
| `M_PH_Eye_<Type>` | `_RimColor` / `_EyeColor` per type on the crowd shader (`SG_ZombieCrowd`, VAT) | Hex values unchanged |
| `M_PH_HitFlash(_Warm)`, `M_PH_HurtFlash` | `_FlashAmount` instanced float on the crowd/knight shaders (GDD §7.3 name), same GDD `flashMs` timing | |
| `M_PH_Floor_*`, `M_PH_Wall` | Dark Fortress environment atlas (floor swatches recolored to grey, per ArtDirection) + ground shader with the blood splat map | |
| `M_PH_Blood_Particle` | Custom flipbook (ArtDirection §4.1) | Free blood packs deferred (§8) |
| `M_PH_Blood_Decal(_Dry)` | URP Decal Projector material (fresh) + splat-map stamp (persistent) | Cap changes from the slice's 100 projectors (20 s lifetime) → 48 fresh projectors + baked RT |

### 7.4 Animator placeholders
- `AC_PH_Knight` and `AC_PH_Zombie` are real Animator Controllers with the **final parameter names** and **empty or tiny clips**. At swap time, make an **Animator Override Controller** (`AOC_SY_Knight`, `AOC_SY_Shambler`, …) that replaces each placeholder clip with the real one. No code or transition changes.
- `AC_PH_Zombie` parameters: `Speed` (float), `Attack` (trigger), `Hit` (trigger), `Dead` (bool), `Variant` (int, for random death/idle). States: `Move`, `Attack`, `Hit`, `Death`. Clips: `AN_PH_Empty`. Optional placeholder motion: a ±5° Visual sway at walk frequency done in the shared zombie manager (cheaper than 300 Animators). **For the 300-zombie perf test, disable Animators on placeholder zombies entirely** and drive the sway from one manager; real animation goes to VAT later (ArtDirection §3).
- Placeholder clip names map to real clips via the override (examples): `AN_PH_Knight_Attack` → Sword Combat light attack; `AN_PH_Knight_Locomotion` → Mixamo/Base Locomotion blend tree; `AN_PH_Zombie_Move` → Undead Animations walk (paid pack, ArtDirection §9; not bought yet).
- When Synty arrives, Socket_WeaponR moves **into the rig** (the Synty hand prop bone). `VisualBinder` gets a `weaponSocket` field so code follows whichever socket is assigned.

### 7.5 Name mapping: placeholder → future asset
These follow the recommended packs in ArtDirection §9 (**not purchased; future only**). Exact Synty prefab names are **not verified** (I haven't seen the pack contents), so check them on import.

| Placeholder | Future asset (pack) |
|---|---|
| `PF_PH_Knight` | Baked Sidekick knight preset (Fantasy Knights – Sidekick) → `PF_SY_Knight`; budget option: a Dark Fortress knight character |
| `PF_PH_Zombie_Shambler` | Dark Fortress "Undead" character → `PF_SY_Zombie_Shambler` |
| `PF_PH_Zombie_Runner` | Dark Fortress "Corpse" character (lean variant) → `PF_SY_Zombie_Runner` |
| `PF_PH_Zombie_Bloater` | Dark Fortress "Behemoth" character → `PF_SY_Zombie_Bloater` |
| `PF_PH_Zombie_Spitter` | Dark Fortress "Plague" character → `PF_SY_Zombie_Spitter` |
| `PF_PH_Elite_GraveKnight` | Dark Fortress "Knights"/"Inquisitor" character with an undead material → `PF_SY_Elite_GraveKnight` |
| `PF_PH_Boss_RottingKing` | Dark Fortress "Plague Lord" + crown attachment → `PF_SY_Boss_RottingKing` |
| `PF_PH_Arena_Floor`, `PF_PH_Arena_Wall`, `PF_PH_Torch`, `PF_PH_Brazier`, `PF_PH_Gravestone`, `PF_PH_Fence` | Dark Fortress floor, wall, torch, brazier, grave and fence modules (recolor floor to grey) |
| `PF_PH_Gem_*` | Stays custom (simple low-poly crystal, ≤ 200 tris, per ArtDirection) |
| `PF_PH_Blood_Hit/Crit/Kill` | Custom flipbook (ArtDirection §4.1); free packs deferred (§8) |
| `PF_PH_BloodDecal` | `PF_BloodDecalProjector` (48 pool) + blood splat-map stamp (replaces the slice's 100-projector pool) |
| `AC_PH_Knight` | `AOC_SY_Knight` with Synty ANIMATION – Sword Combat + Mixamo/Base Locomotion clips |
| `AC_PH_Zombie` | `AOC_SY_<Type>` with Undead Animations clips, then VAT bake for Shamblers and Runners |

---

## 8. Free add-ons: deferred

**Deferred (Supervisor, 2026-10-28): don't use free packs for now.** The slice is primitive-only, and every placeholder in this spec is built without one. The two free Asset Store packs considered earlier (Vefects Free Blood VFX – URP, and Kevin Iglesias Zombie Monster Animations FREE) are parked until art sourcing reopens; their links and prices are in ArtDirection §9. Note that Asset Store packages, even free ones, need a Unity ID sign-in, so Dustin would have to do that step. Nothing has been downloaded and no one has signed in.

---

## 9. Primitive-only fallback summary (no pack needed for anything)

| Item | Fallback |
|---|---|
| Knight | §2: Capsule + Sphere + Cube + Cylinder, URP Lit, team-slot materials, ground ring disc + arrow |
| 5 enemy types + boss | §3: primitives + shared skin/rags + per-type unlit eye/collar materials, no Animator needed (manager-driven tilt/sway/sink) |
| XP gems | §4: rotated Cubes with emission |
| Arena | §5: Cube tiles in a grey checker, Cube walls, primitive props |
| Blood bursts | §6.1: Shuriken + built-in `Default-Particle` texture or Sphere mesh |
| Ground blood | §6.2: URP Decal Projector pool (built into URP, no pack), or pooled flat alpha-clipped Quads as the fallback; cap 100, 20 s lifetime (4 s fade), fresh `#B3121B` → dried `#7A0A10` swap at 6 s. Blood Off: scorch-mark projectors, cap 50 (§6.3) |
| Blood on weapon | §2.3: MaterialPropertyBlock color lerp + `_BloodWeapon` / `_BloodAmount` floats written for the future shader |
| Telegraphs | §3.8: flat Quad / Cylinder / generated fan mesh with `M_PH_Telegraph` (pulsing additive orange-red) |
| Animations | Placeholder Animator Controllers with empty clips + simple code-driven motion |

## 10. Acceptance checks (placeholder milestone)
- [ ] Black-silhouette test at the GDD camera: Knight, Shambler, Runner, Bloater, Spitter, Grave Knight, Rotting King each identifiable without color.
- [ ] Grayscale screenshot: blood decals, gems, and enemies separate from the floor checker.
- [ ] 4 knights (P1–P4 materials) distinguishable inside a crowd of 300 Shamblers.
- [ ] 350 placeholder zombies (GDD hard cap; also run the minute-14 mix with up to 20% Bloaters and 20% Spitters, GDD §13 #6) + 60 corpses + 96 blood systems (~3,000 particles) + 100 decal projectors at ≥ 60 fps (check the GPU Resident Drawer is on; Frame Debugger shows instanced draws, not 1,800 separate ones).
- [ ] Every telegraph (§3.8) pulses orange-red and is never mistaken for blood on a soaked floor; decals visibly go from fresh to dried and fade out by 20 s.
- [ ] Blood **Off** shows no red at all: grey dust, grey bile, charcoal scorch marks (≤ 50). The player-hurt flash is HP red `#E23A3A`.
- [ ] Project search for `_PH_` lists every placeholder asset; no gameplay script references a child path under `Visual`.
