# Knight Survivors: Tripo3D Asset Generation Plan

**Owner:** Asset Designer · **For:** Supervisor / Dustin · **Last updated:** 2026-10-30 (America/Chicago)
**Status:** Plan + **one Sir Aldric textured pilot** (approved via Supervisor; see §0.1 Test result). Further generates still need explicit OK. No rigging/retarget on the pilot.
**Sources:** `docs/ArtDirection.md`, `docs/PlaceholderSpec.md`, `docs/GDD.md` v0.3 (GDD wins on numbers; PlaceholderSpec wins on look). Tripo pricing from https://developers.tripo3d.ai/en/pricing (fetched 2026-10-30).
**Connector:** Composio MCP namespace `user-Composio`, toolkit `tripo3d`, account `tripo3d_strung-coring`.

---

## 0. Connection check (read-only, 2026-10-30)

| Item | Result |
|---|---|
| Connector visible | **Yes** (`user-Composio` ready) |
| Toolkit | `tripo3d` |
| Account | `tripo3d_strung-coring` |
| Account status | **ACTIVE** (default, PRIVATE) |
| Balance (initial check) | **0** available, **0** frozen |
| Balance (after top-up + pilot) | Started **1000**, ended **930** after two generates (35 each) |
| Generation run | **Sir Aldric textured pilot only** (see §0.1). Further GENERATE_*/rig/convert still need Dustin OK via Supervisor |

### 0.1 Test result — Sir Aldric textured pilot (2026-10-30)

Approved one textured `TRIPO3D_GENERATE_MODEL_FROM_TEXT` (H3.1, smart_low_poly + quad + standard texture). First result had floating weapon junk; one retry kept. Files left local only (not in this PR): `Assets/Art/Characters/SirAldric/SirAldric.fbx`, `previews/SirAldric_preview.webp`.

| Finding | Detail |
|---|---|
| `face_limit` | **15000 rejected** with `smart_low_poly` + `quad`. API max is **10000**. That run still produced **23,247 tris** (over the ArtDirection hero LOD0 ~15k target). |
| Export | Native output was **FBX** (not GLB). **Z-up**, **~100× scale** (cm), height **~1.0 m**, **centered pivot** (not feet-at-origin). Unity import needs axis/scale/pivot fix (or a paid convert with `pivot_to_center_bottom` + scale). |
| Credits | **~35 credits** per textured generate (20 + 10 smart_low_poly + 5 quad). Pilot used 70 (broken + retry). |
| Fix next | **Heater shield** (not a small buckler), **chest tabard** (recolorable panel, not a waist hang), and a clear **tint mask** (tabard / shield face / plume / trim UV islands for team G/B). |

Update plan defaults: knight `face_limit` → **10000** when smart_low_poly+quad; expect ~23k tris until a Blender decimate or convert `face_limit` pass.

### Exact tool names (Composio / Tripo)

| Tool slug | Spends credits? | Purpose |
|---|---|---|
| `TRIPO3D_GET_BALANCE` | No | Available + frozen credits |
| `TRIPO3D_LIST_USAGE` | No | Newest-first usage page (`limit`, `next_cursor`) |
| `TRIPO3D_GET_TASK` | No | One task status / outputs (`task_id`) |
| `TRIPO3D_GET_TASKS` | No | Up to 100 task snapshots (`task_ids`) |
| `TRIPO3D_CHECK_MODEL_RIGGABILITY` | Free (live-tested) | Recommend rig type (`input`) |
| `TRIPO3D_GENERATE_MODEL_FROM_TEXT` | **Yes** | Text → 3D |
| `TRIPO3D_GENERATE_MODEL_FROM_IMAGE` | **Yes** | Image → 3D |
| `TRIPO3D_CONVERT_MODEL` | **Yes** | Format / topology / pivot / scale |
| `TRIPO3D_UPLOAD_FILE` | Yes (upload) | Local file → Tripo token |
| `TRIPO3D_RIG_MODEL` | **Yes** (~25) | Auto-rig biped / creature |
| `TRIPO3D_RETARGET_ANIMATION` | **Yes** (~10 / clip) | Preset anims onto a rigged model |

### Key generation parameters (`TRIPO3D_GENERATE_MODEL_FROM_TEXT`)

| Param | Type / values | Notes for this project |
|---|---|---|
| `prompt` | string ≤ 1024 | Required. Style + silhouette + flat color |
| `negative_prompt` | string ≤ 255 | No gore meshes, no photoreal, no brown floor, etc. |
| `model` | `v3.1-20260211` (H3.1), `v3.0-20250812`, `v2.5-20250123`, `P1-20260311`, `P2-20260801` | **Default plan: H3.1** for smart low-poly |
| `smart_low_poly` | bool | H3 only. **On** for crowd zombies |
| `face_limit` | int > 0 | Cap to ArtDirection LOD0 budgets |
| `texture` | bool (default true) | On for base color; keep flat |
| `pbr` | bool (default true) | **Off** preferred for flat POLYGON look (albedo only); on only if we need a roughness map for steel |
| `quad` | bool | On for knight / elite / boss (rig-friendly); off or on for crowd (VAT can use tris) |
| `texture_quality` | `standard` / `detailed` / `extreme` | **standard** (flat atlas feel) |
| `geometry_quality` | `standard` / `detailed` | **standard** for crowd; detailed only if a hero fails silhouette |
| `export_orientation` | `+x` / `-x` / `-y` / `+y` | Leave unset at generate; fix in convert / Unity |
| `auto_size` | bool | Optional normalize; we still force meters in Unity |
| `export_uv` | bool (default true) | Keep true |

### Key convert / rig parameters

| Tool | Key params |
|---|---|
| `TRIPO3D_CONVERT_MODEL` | `input`, `format` (`GLTF` / `FBX` / …), `quad`, `face_limit`, `texture_size`, `texture_format`, `pivot_to_center_bottom`, `scale_factor`, `export_orientation`, `fbx_preset` (`blender` / `mixamo` / …), `flatten_bottom` |
| `TRIPO3D_RIG_MODEL` | `input`, `rig_type` (`biped` …), `spec` (`tripo` / `mixamo`), `model` (`v1.0-20240301` biped / `v2.5-20260210` creature), `out_format` (`glb` / `fbx`) |
| `TRIPO3D_RETARGET_ANIMATION` | `input` (rig task UUID), `animation` or `animations[]` (e.g. `preset:biped:idle`), `out_format`, `bake_animation`, `animate_in_place` |

---

## 1. Style and constraints (from project docs)

- **Look:** stylized low-poly, Synty POLYGON-style: bold silhouettes, flat color atlas, low-frequency detail. Readable at iso ~18–25 m (characters ~60–120 px tall at 1080p).
- **Gore:** blood and splatter **only**. No dismemberment, no gibs, no exposed organs, no gore baked into the mesh or texture. Blood is runtime (decals, particles, `_BloodAmount` / `_BloodWeapon` shaders).
- **Floor / props:** play-area floor stays cool grey (`#6B7178` / `#5C636B` / mortar `#474D54`). No red / orange / brown / maroon in the fight floor. Warm fire only as emissive on braziers / perimeter torches (`#FF9A3C`), no point lights under the play area.
- **Type colors (eyes + rim / collar; not baked as full-body paint):** Shambler green `#8BD346`, Runner yellow `#FFD23F`, Spitter pink `#FF4FB3`, Bloater violet `#9B5DE5`, Elite/Boss cyan-white `#7FF3FF`. Skin/rags stay desaturated (`#7D8574` / `#4B4F55`).
- **Knight team tint:** mask channels R = blood, G = primary (tabard, shield face, plume), B = secondary (trim, pauldron bands, heraldry), A = emissive. Heraldry on shield + tabard.
- **Heights (PlaceholderSpec; 1 unit = 1 m; pivot at feet; +Z forward, Y up):** Knight 1.8 m, Shambler 1.7 m, Runner 1.6 m, Bloater 1.5 × 1.4 m, Spitter 1.7 m, Grave Knight 2.9 m (1.6×), Rotting King ~4.0 m (2.2×).
- **Triangle budgets (ArtDirection §3):** Knight ≤ 15k, Shambler ≤ 2.5k, Runner / Spitter ≤ 3k, Bloater ≤ 6k, Elite/Boss ≤ 12k, props 100–1.5k each.

### Shared negative prompt (append to every character / prop)

```
photorealistic, realistic skin pores, high poly, subdivision surface, noise texture, dirty brown floor, wooden floorboards, red carpet, rust, mud, gore, exposed organs, entrails, severed limbs, gibs, blood splatters painted on mesh, dripping blood texture, wounds, mutilation, horror movie realism, T-pose with floating detached parts, modern clothing, sci-fi armor, guns, neon cyberpunk, text, watermark, logo
```

---

## 2. Recommended pipeline settings (before any spend)

| Setting | Crowd zombies (T1/T2/T2b) | Bloater / Elite / Boss | Knight | Static props |
|---|---|---|---|---|
| Model version | `v3.1-20260211` (H3.1) | H3.1 | H3.1 | H3.1 |
| `smart_low_poly` | **true** (+10 cr) | true | true | true |
| `face_limit` | 2500 / 3000 | 6000 / 12000 | **10000** (API max w/ smart_low_poly+quad; was 15000) | 500–1500 |
| `texture` | true | true | true | true |
| `texture_quality` | `standard` | `standard` | `standard` | `standard` |
| `pbr` | **false** (flat) | false (Bloater); true optional on steel (elite/knight) | true for steel + mask paint later | false |
| `quad` | false (VAT-friendly tris OK) or true if we want FBX quads | **true** (+5) | **true** (+5) | false |
| `geometry_quality` | `standard` | `standard` | `standard` | `standard` |
| Export for Unity | **GLB** via convert (`GLTF`) + glTFast, **or FBX** | Prefer **FBX** (`fbx_preset: mixamo` or `blender`) if Tripo-rigged | FBX preferred for Humanoid | GLB or FBX |
| Pivot | convert `pivot_to_center_bottom: true` | same | same | same |
| Scale | Unity Import Scale 1; Visual child scaled to target height | same | same | same |

**Unity import note:** Tripo GLB needs the **glTFast** package (or Unity's glTTF importer). FBX imports natively and is better for Humanoid Avatar + Mixamo. Prefer FBX for anything that will be Mecanim-Humanoid (knight, Bloater, elite, boss). Prefer low-poly GLB/FBX for VAT zombies after a Blender bake.

**Orientation:** Unity = +Z forward, Y up, left-handed. After import: feet at y = 0 under `Visual`, face +Z. Do not rely on Tripo `export_orientation` alone; verify in the Scene view against `PF_PH_*` placeholders.

---

## 3. Credit estimates (official pricing)

**Source:** https://developers.tripo3d.ai/en/pricing (fetched 2026-10-30). 1 credit = $0.01 USD.

| Operation | Credits |
|---|---|
| Text → 3D, no texture (H) | 10 |
| Text → 3D, standard texture (H) | 20 |
| Smart low-poly add-on | +10 |
| Quad mesh add-on | +5 |
| Detailed / HD texture add-ons | +10 / +20 (avoid) |
| Format convert basic | 5 |
| Format convert advanced (any of: `quad`, `face_limit`, `pivot_to_center_bottom`, `scale_factor`, `texture_size`, …) | 10 |
| Auto Rig | 25 |
| Rig check | Free |
| Animation retarget | 10 / clip |
| Texture-only pass | 10–30 |

### Per-asset planned spend (text-to-3D path, H3.1)

Assumes **one successful generate** each: standard texture + smart_low_poly, plus **one advanced convert** (pivot + face_limit). Rig costs listed separately (optional).

| # | Asset | Generate | Convert | Subtotal (mesh) | Optional Auto Rig | Notes |
|---|---|---|---|---|---|---|
| 1 | Sir Aldric (knight) | 20 + 10 smart + 5 quad = **35** | 10 | **45** | +25 | Hero; need clean biped |
| 2 | Shambler | 20 + 10 = **30** | 10 | **40** | +25 or Mixamo / Blender | VAT later |
| 3 | Runner | 20 + 10 = **30** | 10 | **40** | +25 or Mixamo / Blender | VAT later |
| 4 | Bloater | 20 + 10 + 5 quad = **35** | 10 | **45** | +25 | SMR OK |
| 5 | Plague Spitter | 20 + 10 = **30** | 10 | **40** | +25 | Sac must read |
| 6 | Grave Knight | 20 + 10 + 5 = **35** | 10 | **45** | +25 | Elite |
| 7 | Rotting King | 20 + 10 + 5 = **35** | 10 | **45** | +25 (creature rig v2.5?) | Boss |
| 8 | Gravestone | 20 + 10 = **30** | 10 | **40** | — | Static |
| 9 | Fence segment | 20 + 10 = **30** | 10 | **40** | — | Static |
| 10 | Brazier | 20 + 10 = **30** | 10 | **40** | — | Flame as separate emissive mesh or VFX |
| 11 | Wall segment | 20 + 10 = **30** | 10 | **40** | — | Modular |
| 12 | Floor tile | 20 + 10 = **30** | 10 | **40** | — | Cool grey only |

| Roll-up | Credits | USD (@ $0.01) |
|---|---|---|
| All 12 meshes (generate + convert, no retries) | **500** | **$5.00** |
| + Auto Rig on 7 characters | +175 | +$1.75 |
| + 3 retarget clips × 7 characters (idle/walk/attack) | +210 | +$2.10 |
| **Total if full Tripo rig+anim path** | **~885** | **~$8.85** |
| Contingency (1 failed regen per hero/zombie) | +~200 | +$2.00 |
| **Budget ask (recommended)** | **~1,100 credits** | **~$11** |

Retries are likely on silhouette failures (especially Spitter sac, Bloater roundness, knight shield/heraldry areas). Do **not** buy credits or generate until the Supervisor / Dustin approve a batch and a credit top-up.

**Cheaper variant (untextured generate + our own flat materials):** text no-texture 10 + smart_low_poly 10 = 20 per mesh, then paint URP Lit colors in Unity. Subtotal for 12 ≈ 240 + converts ≈ **360 credits (~$3.60)** before rigs. Recommended if the flat POLYGON look is more important than Tripo's baked albedo.

---

## 4. Assets in priority order

### 4.1 Sir Aldric — `PF_PH_Knight` → `PF_TR_Knight`

- **Role:** Launch hero (GDD: Sir Aldric only). Co-op tint later.
- **Target:** height 1.8 m, shoulders ~0.9 m, ≤ 15k tris LOD0, ≤ 60 bones.
- **Prompt:**
  ```
  Stylized low-poly medieval knight character for a top-down isometric indie game, Synty POLYGON style, flat shaded color regions, clean hard edges. Light steel plate armor in cool grey, oversized pauldrons forming a clear T silhouette from above, round heater shield on the left arm with a blank flat face for heraldry, cloth tabard over the chest for team color, helmet with a tall plume, longsword held in the right hand. Readable silhouette from a 45-degree isometric camera: cross shape of shoulders plus sword. Separate clear regions for tabard, shield face, plume, and pauldron trim so they can be recolored. No blood, no dirt, no damage. Single mesh, T-pose or A-pose, feet together on the ground, facing forward.
  ```
- **Negative:** shared negative + `cloak covering silhouette, tiny shield, dual wield, axe, bow, female proportions, child, fat armor, asymmetric broken armor`
- **Settings:** H3.1, `smart_low_poly=true`, `face_limit=10000` (API max with smart_low_poly+quad; 15000 was rejected — see §0.1), `quad=true`, `texture=true`, `texture_quality=standard`, `pbr=true` (steel), then convert FBX + `pivot_to_center_bottom` + scale/axis so feet sit at y=0.
- **Mask / heraldry (post-Tripo, custom):** paint mask 1024² — G = tabard + shield face + plume, B = pauldron bands + shield cross, R = blood accumulation zones, A = visor slit. Heraldry atlas (cross, lion, …) as secondary on shield/tabard.
- **Credit estimate:** 45 mesh (+25 rig) — see §3.
- **Rig:** Tripo Auto Rig biped (`spec=mixamo` if going to Mixamo clips) **or** Mixamo in browser (needs Dustin Adobe ID) **or** Blender Rigify. Then Unity Humanoid Avatar; Animator Override on `AC_PH_Knight`.

### 4.2 Shambler — `PF_PH_Zombie_Shambler`

- **Target:** 1.7 × 0.55 m, ≤ 2.5k tris, green rim/eyes `#8BD346` (instanced later, not full-body paint).
- **Prompt:**
  ```
  Stylized low-poly undead shambling zombie for an isometric horde game, Synty POLYGON style, flat colors. Narrow hunched male figure, small head tipped forward, two long thin arms stretched straight forward (classic zombie arms), tattered grey-green rags, desaturated greenish skin, no weapons. Clear thin vertical "|" silhouette from a top-down isometric view. Simple geometry, game-ready low poly. No blood textures, no open wounds, no exposed guts. T-pose or slight forward reach, feet on ground.
  ```
- **Negative:** shared + `muscular bodybuilder, fat bloated belly, running pose, sword, armor, glowing veins, hyper detail`
- **Settings:** H3.1, `smart_low_poly=true`, `face_limit=2500`, `quad=false`, `texture=true`, `pbr=false`, convert with pivot bottom. LOD1 ~1.2k / LOD2 ~500 in Blender.
- **Crowd:** bake VAT clips (idle/walk/attack/death) for GPU Resident Drawer. Eyes + collar band remain separate unlit type-color meshes or instanced `_RimColor`.
- **Credits:** 40 mesh.

### 4.3 Runner — `PF_PH_Zombie_Runner`

- **Target:** 1.6 × 0.40 m, 30° forward lean, ≤ 3k tris, yellow `#FFD23F`.
- **Prompt:**
  ```
  Stylized low-poly undead runner zombie, Synty POLYGON style, flat colors. Very thin lean body, heavy forward lean about 30 degrees, arms swept BACK behind the torso (opposite of shambler), long stride pose frozen, tattered dark rags, desaturated skin. Clear diagonal "/" slash silhouette from isometric top-down view. Game-ready low poly. No blood paint, no gore meshes. Feet on ground.
  ```
- **Negative:** shared + `hunched forward arms, fat, armored, crawling on all fours`
- **Settings:** same as Shambler with `face_limit=3000`.
- **Credits:** 40 mesh.

### 4.4 Bloater — `PF_PH_Zombie_Bloater` (GDD `bloater`; ArtDirection T3)

- **Target:** 1.5 m tall × 1.4 m wide round "O", ≤ 6k tris, violet `#9B5DE5`. Counts as 3 corpses.
- **Prompt:**
  ```
  Stylized low-poly bloated plague zombie, Synty POLYGON style, flat colors. Short round body as wide as it is tall, huge spherical belly, stubby legs, tiny head on top, short arms, desaturated skin and dark rags, violet cloth belt band around the waist. Clear round "O" blob silhouette from isometric top-down. The only round enemy. No exploding gore, no exposed intestines, no blood textures. Standing, feet on ground.
  ```
- **Negative:** shared + `tall thin, muscular, knight armor, long arms forward, spikes`
- **Settings:** H3.1, smart_low_poly, `face_limit=6000`, `quad=true`, pbr=false. SMR OK (≤ 10 on screen); optional ragdoll later.
- **Credits:** 45 mesh (+25 rig).

### 4.5 Plague Spitter — `PF_PH_Zombie_Spitter`

- **Target:** 1.7 × 0.5 m, upright, large throat sac, pink `#FF4FB3`, ≤ 3k tris.
- **Prompt:**
  ```
  Stylized low-poly plague spitter zombie, Synty POLYGON style, flat colors. Upright stance with NO forward lean, thin body, arms out sideways, large glowing spherical throat sac on the front of the chest (lollipop / lantern silhouette), desaturated skin, dark rags, hot-pink sac and eyes. Readable from isometric camera as upright lantern shape. No bile spray mesh, no blood, no open mouth gore. Standing, feet on ground.
  ```
- **Negative:** shared + `hunched, forward zombie arms, bloated round belly, quadruped`
- **Settings:** `face_limit=3000`, smart_low_poly, pbr=false. Sac material swapped to emissive pink at import (or vertex color region).
- **Credits:** 40 mesh.

### 4.6 Grave Knight (elite) — `PF_PH_Elite_GraveKnight`

- **In GDD:** yes (`grave_knight`). 1.6× scale → 2.9 m, cyan-white `#7FF3FF`, ≤ 12k tris, casts shadows.
- **Prompt:**
  ```
  Stylized low-poly elite undead grave knight, Synty POLYGON style, flat colors. Huge dark steel armored knight, square angular cube pauldrons, tall helmet spike, greatsword, no colorful tabard, cyan-white eye slit glow. Imposing angular silhouette with a long vertical spike readable from isometric distance. No team colors, no blood textures, no gore. T-pose, feet on ground.
  ```
- **Settings:** `face_limit=12000`, quad=true, pbr=true optional for steel. Scale Visual × 1.6 in Unity (collider from GDD radius 0.7).
- **Credits:** 45 mesh (+25 rig).

### 4.7 Rotting King (boss) — `PF_PH_Boss_RottingKing`

- **In GDD:** yes. ~4.0 m (Visual × 2.2), cyan-white pulse, ≤ 12k tris, casts shadows.
- **Prompt:**
  ```
  Stylized low-poly undead boss, the Rotting King, Synty POLYGON style, flat colors. Enormous bloated royal zombie, massive body, long dragging arms, gold crown ring of spikes on the head, tiny relative head, dark rags and desaturated flesh, cyan-white glowing eyes. Biggest silhouette on screen, readable from isometric camera. No exposed organs, no blood paint, no severed parts. Standing, feet on ground.
  ```
- **Settings:** `face_limit=12000`, quad=true. Rig: check with `TRIPO3D_CHECK_MODEL_RIGGABILITY`; may need creature rig `v2.5-20260210` rather than biped.
- **Credits:** 45 mesh (+25 rig).

### 4.8 Arena props

| Asset | Prefab | Prompt focus | face_limit | Credits (mesh) |
|---|---|---|---|---|
| Gravestone | `PF_PH_Gravestone` | Low-poly cool grey stone tombstone, rounded top, ~1 m tall, flat POLYGON, no moss brown, no writing | 800 | 40 |
| Fence segment | `PF_PH_Fence` | 4 m grey stone fence, two posts + two rails, cool grey only (no wood brown) | 600 | 40 |
| Brazier | `PF_PH_Brazier` | Dark metal bowl on a stand, empty bowl (flame is a separate emissive VFX cube), cool dark metal, no warm ground glow baked in | 1000 | 40 |
| Wall segment | `PF_PH_Arena_Wall` | Modular 4–8 m dark grey castle wall segment `#2E333A`, flat stones, low detail | 1500 | 40 |
| Floor tile | `PF_PH_Arena_Floor` tile | Single 10 m-ish cool grey flagstone tile, flat, mid grey `#6B7178`, no brown, no red veins, low-frequency cracks only | 500 | 40 |

Props: no rig. Convert with `pivot_to_center_bottom`, mark Static in Unity, share one environment atlas / URP Lit material family.

---

## 5. Import, rigging, and Unity integration

### 5.1 Export format

| Path | When | Package |
|---|---|---|
| **FBX** | Knight, Bloater, elite, boss (Humanoid / Mecanim) | Native Unity FBX importer; `fbx_preset: mixamo` or `blender` |
| **GLB / GLTF** | Props, or zombies headed to Blender VAT bake | Needs **glTFast** (or Unity glTTF). Tripo convert `format=GLTF` |

Always run convert with `pivot_to_center_bottom=true` and verify feet at origin. Scale: 1 unit = 1 m; adjust `Visual` local scale to PlaceholderSpec heights; **never** scale the logic root.

### 5.2 Rigging options (all cost or account gates)

1. **Tripo Auto Rig** (`TRIPO3D_RIG_MODEL`, ~25 cr): run `TRIPO3D_CHECK_MODEL_RIGGABILITY` first (free). Use `rig_type=biped`, `spec=mixamo` for Mixamo-compatible naming, `out_format=fbx`. Then `TRIPO3D_RETARGET_ANIMATION` (~10 / clip) for presets. **Needs credits + Dustin OK.**
2. **Mixamo auto-rig** (browser): upload FBX/OBJ, download with skin. **Needs Dustin's Adobe ID.** Free for personal use; don't automate login from the agent.
3. **Blender manual / Rigify:** full control, best for non-humanoid Spitter sac / Rotting King. Export FBX → Unity Humanoid or Generic.

### 5.3 Unity Humanoid + crowd plan

- **Knight / Bloater / Grave Knight / Rotting King:** SkinnedMeshRenderer + Animator. Configure Avatar as Humanoid; map bones; Animator Override Controllers (`AOC_TR_*`) replace `AC_PH_*` clips.
- **Shambler / Runner / Spitter (bulk):** prefer **VAT** (vertex animation textures) after baking clips in Houdini/Blender/Unity VAT tools. MeshRenderers + GPU Resident Drawer / BRG. Bone cap ≤ 24 if temporary SMR fallback.
- **LODs:** generate LOD1/LOD2 in Blender Instant Meshes / Unity mesh simplifier to ArtDirection budgets. Crowd LOD via crowd renderer distance buckets, not 300× LODGroup.
- **Decimation:** if Tripo returns over budget, re-convert with tighter `face_limit` (advanced convert 10 cr) or decimate offline (preferred, $0).

### 5.4 Materials → URP

1. Import textures as RGBA; turn off unsupported Tripo PBR maps if `pbr=false`.
2. Create URP Lit (or custom Shader Graph) materials:
   - Zombies: shared atlas / base color; instanced `_RimColor` / eye emissive per type; `_BloodAmount` for wounded optional.
   - Knight: Shader Graph with mask — `_TeamPrimary`, `_TeamSecondary`, `_BloodAmount`, `_BloodWeapon`, `_BloodColor`, `_FlashAmount`.
3. Strip baked blood / brown dirt from albedo in an image editor if Tripo adds any.
4. Braziers: flame = separate unlit emissive mesh (`#FF9A3C`), **no** real Light component in the play area (GDD).

### 5.5 Prefab swap (PlaceholderSpec §7.1)

```
PF_<Name>          ← LOGIC ROOT kept forever (controller, collider, pool, sockets)
└─ Visual          ← REPLACE contents with Tripo mesh (or Prefab Variant PF_TR_*)
```

- Gameplay only references root + sockets + `VisualBinder`.
- Prefer Prefab Variants: `PF_Knight_Base` → `PF_PH_Knight` → `PF_TR_Knight`.
- Copy socket positions from the placeholder; rebind `VisualBinder` renderers (body, team primary/secondary, weapon).
- Ground ring stays **outside** `Visual`.

---

## 6. Review checklist (each generated model)

- [ ] **Silhouette:** black-unlit material at gameplay camera — type identifiable with no color (Shambler `|`, Runner `/`, Bloater `O`, Spitter lantern, knight cross).
- [ ] **Tris:** Mesh Inspector ≤ ArtDirection LOD0 budget (measure after import).
- [ ] **Pivot / facing:** feet at y = 0, +Z forward, upright on grey floor tile.
- [ ] **Height:** matches PlaceholderSpec within ~5% after Visual scale.
- [ ] **No gore:** no wound meshes, no blood in albedo; grayscale still separates from floor.
- [ ] **Colors:** base skin/rags desaturated; type color only in eyes/rim/sac regions (or left neutral for runtime tint).
- [ ] **Knight mask areas:** clear UV islands for tabard, shield face, plume, trim (paintable G/B).
- [ ] **Batching:** one material family where possible; SRP Batcher / GPU Resident Drawer friendly.
- [ ] **Rig (if any):** Humanoid map complete; no floating geometry; clips play without sliding feet.
- [ ] **Swap test:** drop under existing `Visual`, play mode: pooling, hit flash, blood decals, telegraphs still work.
- [ ] **Perf:** with placeholders replaced, 300+ crowd / 60 corpses / 96 blood systems / 100 decals still ≥ 60 fps on min-spec target.

---

## 7. Open questions (need Supervisor / Dustin)

1. **Credit top-up:** balance is **0**. Approve buying ~1,100 credits (~$11) for the full path, or the cheaper untextured ~360-credit mesh-only path (~$3.60)?
2. **Batch order:** generate knight-only first as a style pilot, or zombies first for the crowd perf gate?
3. **Rig path:** Tripo Auto Rig (credits), Mixamo (Dustin Adobe ID), or Blender-only?
4. **Textured vs untextured Tripo:** keep Tripo standard albedo, or untextured + our flat URP colors for a stricter POLYGON match?
5. **P-series:** skip for now (P2 is 100+ credits/text and no `smart_low_poly`). Confirm H3.1 only.
6. **Retries policy:** max N regenerations per asset before manual Blender fix?
7. **Rotting King rig type:** biped vs creature — run free `CHECK_MODEL_RIGGABILITY` after first mesh, before paying for Auto Rig?
8. **Synty still on the table?** ArtDirection recommended paid Synty packs; Dustin previously said no purchases. Is Tripo the temporary / permanent mesh source instead?

---

## 8. Hard stops

- **Do not** call `TRIPO3D_GENERATE_*`, texture, `TRIPO3D_CONVERT_MODEL`, `TRIPO3D_RIG_MODEL`, `TRIPO3D_RETARGET_ANIMATION`, or `TRIPO3D_UPLOAD_FILE` until Dustin explicitly OK's spend through the Supervisor.
- Read-only tools only until then: `GET_BALANCE`, `LIST_USAGE`, `GET_TASK`, `GET_TASKS`, and (if a mesh already existed) `CHECK_MODEL_RIGGABILITY`.
- Further paid generates still need Dustin OK via Supervisor. Pilot artifacts (FBX / previews) stay local and out of git unless a later task says otherwise.
