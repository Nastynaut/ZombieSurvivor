# Knight Survivors

A 3D isometric horde-survival roguelite (think Vampire Survivors): one knight, endless zombies,
automatic attacks, and plenty of blood.

- **Engine:** Unity **6000.3.27f1** (Unity 6.3 LTS), Universal Render Pipeline (URP), PC.
- **Design docs:** `docs/GDD.md` (rules and numbers; v0.3 is frozen), `docs/ArtDirection.md`, `docs/PlaceholderSpec.md`.
- **Tech docs:** `docs/TechPlan.md`: how the code is organised and *why* (start here when reading code).
- **Art:** placeholder primitives only (cubes, spheres, capsules). No paid assets.

---

## 1. Git LFS is required

Large binary files (textures, models, audio) are stored with **Git LFS**. `.gitattributes` already routes them.
1. Install Git LFS once. It ships with Git for Windows and GitHub Desktop; otherwise see https://git-lfs.com.
2. Run `git lfs install` once on your machine, **before** cloning.
3. Clone: `git clone https://github.com/Nastynaut/ZombieSurvivor.git`

Without LFS, art files arrive as tiny text "pointer" files and Unity shows them as broken.
(Right now the repo has no LFS files yet; everything is generated from primitives.)

## 2. Open and play (first time, about 5 minutes)

1. Install **Unity 6000.3.27f1** with Unity Hub (Installs → Install Editor → Archive, if it isn't listed).
2. Unity Hub → **Add → Add project from disk** → pick the cloned folder. Open it. The first import takes a few minutes
   while Unity downloads the packages listed in `Packages/manifest.json` (URP, Input System, Cinemachine, uGUI).
3. If Unity asks **"enable the new Input System backends?"**, click **Yes** (Unity restarts).
   Otherwise check *Edit → Project Settings → Player → Other Settings → Active Input Handling* = **Input System Package (New)** (or **Both**).
4. Menu bar: **Knight Survivors → Build Graybox Scene**. Wait for the progress bar. It creates materials, prefabs,
   data assets, input actions and the scene `Assets/_Project/Scenes/Graybox.unity`, and opens it.
5. Press **Play** ▶.

**Controls:** WASD / arrows or left stick = move. Mouse or right stick = aim (Assisted aim: the sword swings toward your aim).
The sword attacks by itself. Walk near gems to collect XP. On level-up, click a card (or press 1/2/3/4; X = skip, R = reroll. Skip is on X, not S, because S is move-down).
Esc / Start is bound but the pause menu isn't built yet. When you die: **Restart** (or R / Enter) reloads the scene.

### Troubleshooting
| Problem | Fix |
|---|---|
| Everything is **pink** | No URP asset is active. The builder creates one if none is assigned, but if it still happens: *Assets → Create → Rendering → URP Asset (with Universal Renderer)*, then *Project Settings → Graphics → Default Render Pipeline* = that asset. Re-run the builder. |
| Menu **Knight Survivors** missing | There are compile errors. Open *Window → General → Console*, fix (or send) the first red error. |
| `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class` | Active Input Handling (step 3). |
| Console shows only `This project uses Input Manager, which is marked for deprecation` | That warning appears when Active Input Handling is **Input Manager (Old)** or **Both**. Set *Edit → Project Settings → Player → Other Settings → Configuration → Active Input Handling* = **Input System Package (New)**, click **Apply**, let the Editor restart. |
| Console shows only warnings, none of the grey `[Knight Survivors] ...` lines | The Console's **info filter** (grey speech-bubble button, top-right of the Console) is off. Click it so all three buttons (info / warning / error) are on. |
| Knight falls through the floor | Re-run the builder (it adds the invisible `GroundCollider`). |
| Builder dialog "Stop Play mode first" | Exit Play mode, then run it again. |
| Want the GDD **lookahead** (1.2 m ahead) | Lookahead defaults to **off** (Dustin's preference). Select `PlayerTargetGroup` → `PlayerCameraGroup` → tick **Lookahead Enabled** (time 0.24, ease out 0.4, ease in 0.5). Re-run the builder after changing the default in code. |

Re-running the builder is safe and **overwrites** generated files (`Art/Placeholder`, `Data`, `Input`, `Scenes/Graybox.unity`).
To experiment with numbers, **duplicate** an asset in `Assets/_Project/Data` and point `CFG_RunConfig` at your copy.

### Couch co-op test (optional)
Select `Systems` → `PlayerSpawner` → tick **Use Join Hook**, then Play and press a button on each controller
(keyboard counts as one device). Up to 4 players. This is groundwork only; co-op isn't a slice feature.

## 3. What's in the graybox

| System | Where | Notes |
|---|---|---|
| Movement + iso camera | `Player/PlayerMotor`, Cinemachine `CM_IsoCamera` | CharacterController, 5 m/s, camera-relative. FOV 30, pitch 50, yaw 45, 26 m. Optional lookahead (off by default) by `Cameras/PlayerCameraGroup`. |
| Spawner | `Enemies/EnemySpawner` | Time-based rows from `SPN_Slice` (min 0–9), ring 20–24 m, recycle > 35 m, cap 350. |
| Longsword | `Combat/LongswordWeapon` | 120° arc, 8 levels from the GDD, aimed (Assisted). |
| Enemies | `Enemies/EnemyManager`, `Enemy` | Shambler + Runner; seek + separation via spatial hash; health, hit flash, death sink. |
| XP gems | `Pickups/PickupManager` | Pooled, 3 tiers, magnet pickup, merge when far away. |
| Level-up | `Progression/LevelUpService`, `UI/LevelUpScreen` | Pick 1 of 3 (4 with luck), game paused via `Time.timeScale = 0`. |
| Game over | `Progression/RunManager`, `UI/GameOverScreen` | When **all** players are dead. Restart = reload scene. |
| Gore | `Gore/GoreManager` | Pooled bursts (max 96), decal quads (max 100, 32 new/frame). |
| Save | `Save/SaveSystem` | JSON in `Application.persistentDataPath/meta.json`. |

## 4. Deferred (not in this branch)

Dodge roll (input is wired, no movement yet) · Crossbow and Holy Flail · Bloater / Spitter behaviours · elites,
crowns from elites, chests · evolutions · Rotting King boss · wave events (swarms, stampedes) · reroll/banish UI polish ·
hit-stop and screen shake (data is in `CFG_HitFeedback`, not applied yet) · damage numbers · Armory (meta shop) ·
pause and options menus (blood-intensity setting is saved but not yet wired to Gore) · Animators (primitives don't animate) ·
blood droplet splats · torches, props and braziers · wall dither-fade · URP Decal Projectors.
Known graybox quirk: zombies can spawn outside the walls near the arena edge (they have no physics); they walk in.

## 5. ✅ Dustin: verify in Editor

Nothing here could be compiled or run on the build machine (Unity isn't installed there), so please check:

- [ ] Project opens in **6000.3.27f1** with **no red errors** in the Console after packages import.
- [ ] Active Input Handling is **Input System Package (New)** (Both also works, but it causes the yellow "Input Manager ... marked for deprecation" warning).
- [ ] **Knight Survivors → Build Graybox Scene** runs without errors and opens `Graybox.unity`.
- [ ] Materials are **not pink**; floor is a grey checker; knight is light steel with a blue tabard and ring.
- [ ] Transparent materials look right: ground ring and sword slash are see-through (set by code; check the Inspector shows *Surface Type: Transparent*).
- [ ] Press Play: knight moves with WASD and left stick; **W moves up the screen** (toward the top-left of the grid).
- [ ] Camera looks down at about 50°, diagonal across the tiles, follows smoothly with a slight lookahead.
- [ ] **Camera smoothness:** rebuild the scene first (**Knight Survivors → Build Graybox Scene**; the prefab and camera changed). Walk in circles and zig-zag with no zombies near you: the knight, its ground ring and the floor grid should glide with **no fast jitter**, also while turning. Then press **F1** (Console: `Camera shake: OFF`) and walk again; it should look exactly the same, since no shake code exists yet. If it still jitters, use **F2** and **F3** (see the TEMPORARY diagnostics note below) and send a screenshot of the readout.
- [ ] Zombies appear off-screen around you and walk in; Runners show up after minute 2.
- [ ] Sword swings automatically in a 120° fan toward the mouse / right stick; zombies flash, bleed, die and sink.
- [ ] Blood bursts and floor splats appear; splats darken after about 6 s; no frame hitch when many die.
- [ ] Gems drop, fly to you when close, XP bar fills; **level-up pauses** the game and shows 3 cards; picking one resumes. **X** skips, **R** rerolls, and holding **S** while it opens does NOT skip.
- [ ] Let zombies kill you: **Game Over** screen appears; **Restart** reloads and plays again.
- [ ] Console shows pool **growth warnings** only rarely (fine in a graybox; tell me which pool if spammy).
- [ ] Stats (Profiler, optional): 300+ enemies at 60 fps.

> **TEMPORARY debug toggle (remove after the jitter check):** `cameraShakeEnabled` on `CFG_FeedbackSettings` (Inspector tick box, default on) and **F1** in Play mode flip a global camera-shake switch (`CameraShakeGate`) and log `Camera shake: ON/OFF`. There is no shake code yet (that comes with the juice PR), so it only proves that the jitter isn't shake. Files to delete afterwards: `Scripts/Cameras/CameraShakeDebugToggle.cs`, `Scripts/Core/CameraShakeGate.cs`, plus that field and the line in `GrayboxSceneBuilder` that adds the toggle.

> **TEMPORARY camera jitter diagnostics (remove in the PR #1 cleanup):** the scene builder puts them on **Systems** (`CameraJitterReadout`, `CameraFollowModeToggle`, `CameraRigSelfCheck`, `CameraDebugHost`) and a disabled `SimpleIsoFollow` on the Main Camera; `CameraDebugBootstrap` adds them at Play for older scenes. The top-left panel is **visible by default** and starts with a build stamp (`diag-2026-10-30b (input fix)`); **F3** hides/shows it, **F2** switches Cinemachine / `SimpleIsoFollow`; both log `[TEMP DEBUG] ...` as yellow warnings. F1/F2/F3 are read through `DebugKeys`, which works with every Active Input Handling setting. After every script reload the Console shows `[Knight Survivors] Scripts loaded: <stamp>. Active Input Handling: ...` (`ScriptReloadAnnouncer`), and a red error if the Input System is off. **No panel and no `[TEMP DEBUG] CameraDebugBootstrap running` log = Unity is running old scripts:** press **Ctrl+R** (Assets → Refresh) and check **Preferences → Asset Pipeline → Auto Refresh**. Files to delete: `Scripts/Cameras/SimpleIsoFollow.cs`, `DebugKeys.cs`, `Editor/ScriptReloadAnnouncer.cs`, `CameraFollowModeToggle.cs`, `CameraJitterReadout.cs`, `CameraRigSelfCheck.cs`, `CameraDebugBootstrap.cs`, `GrayboxSceneBuilder.AddCameraDiagnostics`, `UnityEngine.UI` in the Cameras asmdef (if nothing else needs it), and `TickInterpolator.LastAlpha`. (Keep `PlayerController.EnsureSmoothedCameraTarget` and `TickInterpolator.SetPositionOnly`: they are the real fix.)
