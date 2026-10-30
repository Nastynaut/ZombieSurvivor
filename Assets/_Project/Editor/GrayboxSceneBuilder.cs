using KnightSurvivors.Cameras;
using KnightSurvivors.Enemies;
using KnightSurvivors.Gore;
using KnightSurvivors.Pickups;
using KnightSurvivors.Player;
using KnightSurvivors.Pooling;
using KnightSurvivors.Progression;
using KnightSurvivors.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace KnightSurvivors.EditorTools
{
    /// <summary>
    /// ONE-CLICK SETUP. Menu: Knight Survivors > Build Graybox Scene.
    ///
    /// It creates (or refreshes) everything the graybox needs, so you never have to wire things by hand:
    ///   1. Placeholder materials and prefabs (docs/PlaceholderSpec.md): knight, zombies, gems, blood, decals.
    ///   2. Data assets with the GDD numbers (weapons, enemies, spawn schedule, configs) + Input Actions.
    ///   3. A new scene "Assets/_Project/Scenes/Graybox.unity": arena, light, managers, Cinemachine camera, UI.
    ///   4. Adds the scene to Build Settings, so Play works straight away.
    ///
    /// Safe to run again: it overwrites the generated files (anything under Art/Placeholder, Data, the Graybox
    /// scene). Don't hand-edit those; make copies if you want to experiment.
    /// </summary>
    public static class GrayboxSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Graybox.unity";
        private const string SettingsDir = "Assets/_Project/Settings";

        [MenuItem("Knight Survivors/Build Graybox Scene", priority = 0)]
        public static void BuildGrayboxScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Knight Survivors", "Stop Play mode first, then run the builder again.", "OK");
                return;
            }

            // Ask to save the scene you have open, so the builder never throws away your work.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("Knight Survivors", "Render pipeline...", 0.05f);
                EnsureUrpAsset();

                EditorUtility.DisplayProgressBar("Knight Survivors", "Materials...", 0.15f);
                var m = PlaceholderFactory.CreateMaterials();

                EditorUtility.DisplayProgressBar("Knight Survivors", "Input actions...", 0.25f);
                var input = GameDataFactory.CreateInputActions();

                EditorUtility.DisplayProgressBar("Knight Survivors", "Prefabs...", 0.35f);
                var knight = PlaceholderFactory.BuildKnight(m, input);
                var shambler = PlaceholderFactory.BuildShambler(m);
                var runner = PlaceholderFactory.BuildRunner(m);
                var gems = new[] { PlaceholderFactory.BuildGem(0, m), PlaceholderFactory.BuildGem(1, m), PlaceholderFactory.BuildGem(2, m) };
                var gemObjects = new[] { gems[0].gameObject, gems[1].gameObject, gems[2].gameObject };
                var sword = PlaceholderFactory.BuildLongswordPrefab(m);
                var bloodHit = PlaceholderFactory.BuildBlood(PlaceholderFactory.BurstKind.Hit, m.bloodParticle, "PF_PH_Blood_Hit");
                var bloodCrit = PlaceholderFactory.BuildBlood(PlaceholderFactory.BurstKind.Crit, m.bloodParticle, "PF_PH_Blood_Crit");
                var bloodKill = PlaceholderFactory.BuildBlood(PlaceholderFactory.BurstKind.Kill, m.bloodParticle, "PF_PH_Blood_Kill");
                var bloodBig = PlaceholderFactory.BuildBlood(PlaceholderFactory.BurstKind.Big, m.bloodParticle, "PF_PH_Blood_Big");
                // "Blood: Off" setting shows grey dust instead (GDD 7.4).
                var dust = PlaceholderFactory.BuildBlood(PlaceholderFactory.BurstKind.Hit, m.bloodParticleOff, "PF_PH_Dust_Hit", dust: true);
                var decal = PlaceholderFactory.BuildDecalQuad(m);

                EditorUtility.DisplayProgressBar("Knight Survivors", "Data assets...", 0.55f);
                var data = GameDataFactory.CreateAll(knight, sword, shambler, runner, gemObjects);

                EditorUtility.DisplayProgressBar("Knight Survivors", "Scene...", 0.7f);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                BuildArena(m);
                BuildLighting();
                var systems = BuildSystems(data, m, gems, bloodHit, bloodCrit, bloodKill, bloodBig, dust, decal);
                BuildCamera(out var brain, out var vcam);
                AddCameraDiagnostics(systems, brain, vcam);
                BuildUI();

                PlaceholderFactory.EnsureFolder("Assets/_Project/Scenes");
                EditorSceneManager.SaveScene(scene, ScenePath);
                AddSceneToBuildSettings();
                AssetDatabase.SaveAssets();

                Selection.activeGameObject = systems;
                // TEMPORARY: a yellow warning (not a grey log) with the build stamp, so it shows even when the
                // Console's info filter is off and proves which version of the scripts built the scene.
                Debug.LogWarning("[Knight Survivors] Graybox scene built: " + ScenePath + " (scripts " +
                                 KnightSurvivors.Cameras.CameraJitterReadout.BuildStamp + "). Press Play!");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ------------------------------------------------------------------ render pipeline

        /// <summary>
        /// If the project has no URP asset assigned (for example it wasn't made from the URP template), make one.
        /// Without it the URP shaders render pink. If you already have one, it's left alone.
        /// </summary>
        private static void EnsureUrpAsset()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return;

            PlaceholderFactory.EnsureFolder(SettingsDir);
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, SettingsDir + "/PC_Renderer.asset");

            var urp = UniversalRenderPipelineAsset.Create(rendererData);
            // Shadow numbers from ArtDirection "URP lighting setup".
            urp.mainLightShadowmapResolution = 2048;
            urp.shadowCascadeCount = 2;
            urp.cascade2Split = 0.35f;
            urp.shadowDistance = 45f;
            AssetDatabase.CreateAsset(urp, SettingsDir + "/PC_RPAsset.asset");

            GraphicsSettings.defaultRenderPipeline = urp;
            AssetDatabase.SaveAssets();
            Debug.Log("[Knight Survivors] No URP asset was assigned, so one was created in " + SettingsDir +
                      ". If materials look pink, see README > Troubleshooting.");
        }

        // ------------------------------------------------------------------ arena (PlaceholderSpec 5.1 / 5.2)

        private static void BuildArena(PlaceholderFactory.Materials m)
        {
            var arena = new GameObject("Arena");

            // Mortar under-plate, its colour shows through the gaps between tiles as grout lines.
            Static(PlaceholderFactory.Prim(PrimitiveType.Cube, "UnderPlate", arena.transform,
                new Vector3(0f, -0.15f, 0f), Vector3.zero, new Vector3(150f, 0.2f, 150f), m.mortar, false));

            // 15 x 15 checker of 10 m tiles, top at y = 0.
            var tiles = new GameObject("Tiles").transform;
            tiles.SetParent(arena.transform, false);
            for (int ix = 0; ix < 15; ix++)
            {
                for (int iz = 0; iz < 15; iz++)
                {
                    float x = -70f + ix * 10f;
                    float z = -70f + iz * 10f;
                    var mat = ((ix + iz) % 2 == 0) ? m.floorA : m.floorB;
                    Static(PlaceholderFactory.Prim(PrimitiveType.Cube, $"Tile_{ix}_{iz}", tiles,
                        new Vector3(x, -0.05f, z), Vector3.zero, new Vector3(9.9f, 0.1f, 9.9f), mat, false));
                }
            }

            // ONE invisible collider for the whole floor (cheaper than 225 tile colliders).
            // The knight's CharacterController needs ground to stand on. Zombies don't use physics at all.
            var ground = new GameObject("GroundCollider");
            ground.transform.SetParent(arena.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            var box = ground.AddComponent<BoxCollider>();
            box.size = new Vector3(150f, 1f, 150f); // top surface at y = 0
            ground.isStatic = true;

            // Four walls, WITH colliders so the knight can't walk out.
            Wall(arena.transform, "Wall_N", new Vector3(0f, 1.5f, 75.5f), new Vector3(152f, 3f, 1f), m.wall);
            Wall(arena.transform, "Wall_S", new Vector3(0f, 1.5f, -75.5f), new Vector3(152f, 3f, 1f), m.wall);
            Wall(arena.transform, "Wall_E", new Vector3(75.5f, 1.5f, 0f), new Vector3(1f, 3f, 152f), m.wall);
            Wall(arena.transform, "Wall_W", new Vector3(-75.5f, 1.5f, 0f), new Vector3(1f, 3f, 152f), m.wall);
            // Torches, props, braziers (spec 5.2 / 5.3) are deferred; see README.
        }

        private static void Wall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var wall = PlaceholderFactory.Prim(PrimitiveType.Cube, name, parent, pos, Vector3.zero, scale, mat, true);
            wall.AddComponent<BoxCollider>(); // Prim() removes colliders; walls need one back
            Static(wall);
        }

        private static GameObject Static(GameObject go)
        {
            go.isStatic = true; // lets Unity batch the floor into a few draw calls
            return go;
        }

        // ------------------------------------------------------------------ light (ArtDirection 2)

        private static void BuildLighting()
        {
            var go = new GameObject("Moonlight");
            // Pitched 50, yawed so shadows fall diagonally (not straight along the camera's view axis).
            go.transform.rotation = Quaternion.Euler(50f, 20f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = PlaceholderFactory.Hex("#C9D6FF");
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.shadowBias = 1f;
            light.shadowNormalBias = 1f;

            // Cool gradient ambient so shadows aren't pitch black.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = PlaceholderFactory.Hex("#3A4A63");
            RenderSettings.ambientEquatorColor = PlaceholderFactory.Hex("#2A3445");
            RenderSettings.ambientGroundColor = PlaceholderFactory.Hex("#1C2433");
            RenderSettings.skybox = null;
            RenderSettings.sun = light;
        }

        // ------------------------------------------------------------------ managers

        private static GameObject BuildSystems(GameDataFactory.Assets data, PlaceholderFactory.Materials m, XpGem[] gems,
            ParticleSystem hit, ParticleSystem crit, ParticleSystem kill, ParticleSystem big, ParticleSystem dust, GameObject decal)
        {
            // Every "manager" lives on one object, so it's easy to find. Each registers itself with the
            // ServiceLocator in Awake (see docs/TechPlan.md "Why a service locator").
            var systems = new GameObject("Systems");

            var pool = systems.AddComponent<PoolService>();
            var so = new SerializedObject(pool); // prewarmConfig is private, so set it the "Inspector" way
            so.FindProperty("prewarmConfig").objectReferenceValue = data.poolConfig;
            so.ApplyModifiedPropertiesWithoutUndo();

            systems.AddComponent<RunManager>().runConfig = data.runConfig;
            systems.AddComponent<LevelUpService>().runConfig = data.runConfig;
            // TEMPORARY debug: F1 toggles camera shake (see CameraShakeDebugToggle). Remove with it.
            systems.AddComponent<CameraShakeDebugToggle>().runConfig = data.runConfig;

            var enemies = systems.AddComponent<EnemyManager>();
            enemies.runConfig = data.runConfig;
            enemies.hitFlashMaterial = m.hitFlash;

            systems.AddComponent<EnemySpawner>().runConfig = data.runConfig;

            var pickups = systems.AddComponent<PickupManager>();
            pickups.runConfig = data.runConfig;
            pickups.gemPrefabs = gems;

            var gore = systems.AddComponent<GoreManager>();
            gore.runConfig = data.runConfig;
            gore.hitBloodPrefab = hit;
            gore.critBloodPrefab = crit;
            gore.killBloodPrefab = kill;
            gore.bigBloodPrefab = big;
            gore.dustPrefab = dust;
            gore.decalPrefab = decal;
            gore.decalWetMaterial = m.bloodDecal;
            gore.decalDryMaterial = m.bloodDecalDry;
            gore.scorchMaterial = m.scorch;

            // Spawn points for up to 4 players, 1.5 m apart around the arena centre.
            var spawnRoot = new GameObject("PlayerSpawnPoints").transform;
            spawnRoot.SetParent(systems.transform, false);
            Vector3[] offsets = { new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 0f), new Vector3(-1.5f, 0f, 0f), new Vector3(0f, 0f, -1.5f) };
            var points = new Transform[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                var p = new GameObject($"Spawn_P{i + 1}").transform;
                p.SetParent(spawnRoot, false);
                p.localPosition = offsets[i];
                points[i] = p;
            }

            var spawner = systems.AddComponent<PlayerSpawner>();
            spawner.runConfig = data.runConfig;
            spawner.spawnPoints = points;
            spawner.useJoinHook = false; // single player by default; see README "Couch co-op test"
            spawner.maxPlayers = 4;

            return systems;
        }

        // ------------------------------------------------------------------ camera (GDD 2.2)

        private static void BuildCamera(out CinemachineBrain brainOut, out CinemachineCamera vcamOut)
        {
            // The real Unity camera. The CinemachineBrain moves it to wherever the active Cinemachine camera says.
            var main = new GameObject("Main Camera");
            main.tag = "MainCamera"; // PlayerAim uses Camera.main, which finds this tag
            var cam = main.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PlaceholderFactory.Hex("#141821");
            main.AddComponent<AudioListener>();
            var brain = main.AddComponent<CinemachineBrain>();
            // Move the camera in LateUpdate, every rendered frame. Our camera target is smoothed in Update
            // (TickInterpolator), so by LateUpdate it's already at this frame's position. We set this on
            // purpose instead of the default SmartUpdate: SmartUpdate guesses from how the target moves, and
            // a target that steps in FixedUpdate would make it update the camera at 50 Hz (= jitter).
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            var cameraData = main.AddComponent<UniversalAdditionalCameraData>();
            // Anti-aliasing. Without it, thin straight edges (the grout gaps between the floor tiles, the
            // tile borders) "crawl" and flicker whenever the camera moves by less than a pixel, which looks
            // like fast jitter across the whole screen even when the camera itself moves perfectly smoothly.
            // SMAA is a cheap post-process filter that works whatever MSAA setting the URP asset has.
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;

            // Target group: Cinemachine frames the middle of all living players (couch co-op ready).
            var groupGo = new GameObject("PlayerTargetGroup");
            var targetGroup = groupGo.AddComponent<CinemachineTargetGroup>();
            // The group recomputes its centre in LateUpdate, i.e. after the players' smoothed positions are
            // set (Update) and in step with the brain above. (This is Cinemachine's default; set to be explicit.)
            targetGroup.UpdateMethod = CinemachineTargetGroup.UpdateMethods.LateUpdate;

            // The Cinemachine camera: fixed rotation, so it never turns. Euler(50, -45, 0) makes the camera look
            // along world (-1, 0, 1). That's the "yaw 45" in the GDD (see TechPlan "Camera yaw convention").
            var vcamGo = new GameObject("CM_IsoCamera");
            vcamGo.transform.rotation = Quaternion.Euler(50f, -45f, 0f);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            var lens = LensSettings.Default;
            lens.FieldOfView = 30f;
            lens.NearClipPlane = 0.3f;
            lens.FarClipPlane = 200f;
            vcam.Lens = lens;
            vcam.Follow = groupGo.transform;

            // Position Composer: keeps the target at screen centre from a fixed distance, with no rotation.
            var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 26f;
            // Damping axes are in the CAMERA's own space, not the world's: x = screen left/right, y = screen
            // up/down, z = along the view direction. Because the camera looks 50° down, walking "up the screen"
            // moves the target in BOTH y and z. With y = 0 and z = 0.25 those two parts followed differently
            // (one instantly, one lagging), so the same walk felt different in each direction. Same value on
            // all three axes = the camera trails the knight the same way whichever way it walks.
            //
            // ROOT CAUSE of the "only W/S jitters" report (measured from Dustin's recording, see PR #1): on
            // W/S the floor moved in a repeating 7 / 2.5 / 3 px pattern (a ~10 Hz beat = 50 Hz ticks seen by
            // a 60 Hz screen) while A/D was smooth (5.5 +/- 0.4 px). That is exactly what damping (0.25, 0,
            // 0.25) does when the followed point still steps at 50 Hz: y = 0 passes every step straight to
            // the screen-vertical axis, x = 0.25 filters them out horizontally. So: same damping on every
            // axis here, a smoothed CameraTarget (TickInterpolator), and CameraRigSelfCheck repairs and
            // reports an old scene that still has y = 0.
            composer.Damping = new Vector3(0.25f, 0.25f, 0.25f);
            // GDD says "1.2 m lookahead". Cinemachine's lookahead is in SECONDS (max 1), so 0.24 s at 5 m/s = 1.2 m.
            var look = composer.Lookahead;
            look.Enabled = true;
            look.Time = 0.24f;
            // Lookahead estimates the target's velocity frame by frame. With a target that steps at 50 Hz, that
            // estimate swings between 0 and "huge" and the camera shakes; with the smoothed target it's steady.
            // The smoothing stays for gentle direction changes.
            look.Smoothing = 0.4f;
            look.IgnoreY = true;
            composer.Lookahead = look;

            var group = groupGo.AddComponent<PlayerCameraGroup>();
            group.composer = composer;
            group.baseDistance = 26f;
            group.maxZoomOut = 1.3f;

            brainOut = brain;
            vcamOut = vcam;
        }

        // TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup).
        // Puts the debug tools straight into the scene, on "Systems", so they don't depend on the
        // CameraDebugBootstrap running (in Dustin's run it didn't). The bootstrap stays as a backup for
        // scenes built before this change; it sees these components and does nothing.
        private static void AddCameraDiagnostics(GameObject systems, CinemachineBrain brain, CinemachineCamera vcam)
        {
            // The plain-code follow camera for F2, off by default (Cinemachine is the normal camera).
            var simple = brain.gameObject.AddComponent<SimpleIsoFollow>();
            simple.enabled = false;

            systems.AddComponent<CameraJitterReadout>(); // the on-screen numbers (visible by default, F3 hides)
            var toggle = systems.AddComponent<CameraFollowModeToggle>(); // F2
            toggle.brain = brain;
            toggle.virtualCamera = vcam;
            toggle.simpleFollow = simple;
            var check = systems.AddComponent<CameraRigSelfCheck>(); // wiring check + repair
            check.brain = brain;
            check.virtualCamera = vcam;
            systems.AddComponent<CameraDebugHost>(); // re-wires after reloads, stops the bootstrap duplicating
        }

        // ------------------------------------------------------------------ UI

        private static void BuildUI()
        {
            // Each screen is its own Canvas; higher sortingOrder draws on top.
            NewCanvas("HUD", 0).AddComponent<HudController>();
            NewCanvas("LevelUpScreen", 10).AddComponent<LevelUpScreen>();
            NewCanvas("GameOverScreen", 20).AddComponent<GameOverScreen>();

            // The EventSystem lets buttons receive clicks/gamepad. Must use the Input System module (not the old one).
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static GameObject NewCanvas(string name, int order)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        // ------------------------------------------------------------------ build settings

        private static void AddSceneToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
            {
                if (s.path == ScenePath) { s.enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); return; }
            }
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
