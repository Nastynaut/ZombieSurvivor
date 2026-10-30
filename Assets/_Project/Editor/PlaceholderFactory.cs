using System.IO;
using KnightSurvivors.Combat;
using KnightSurvivors.Core;
using KnightSurvivors.Enemies;
using KnightSurvivors.Pickups;
using KnightSurvivors.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace KnightSurvivors.EditorTools
{
    /// <summary>
    /// Builds the placeholder ("_PH_") materials and prefabs from docs/PlaceholderSpec.md out of Unity primitives.
    /// Everything is created as real asset files under Assets/_Project/Art/Placeholder, so you can open and tweak
    /// them by hand afterwards. Running the builder again overwrites them with the spec values.
    ///
    /// Beginner notes:
    /// - "Shared material" = one .mat file used by many objects. We never create materials at runtime, because
    ///   per-object copies break Unity's SRP Batcher (the thing that makes 300 zombies cheap to draw).
    /// - Primitives come with a collider. The spec says: delete it on every child. Gameplay uses distance
    ///   checks (enemies) or one collider on the root (the knight's CharacterController).
    /// </summary>
    internal static class PlaceholderFactory
    {
        public const string Root = "Assets/_Project/Art/Placeholder";
        public const string MaterialsDir = Root + "/Materials";
        public const string PrefabsDir = Root + "/Prefabs";
        public const string VfxDir = Root + "/VFX";

        // Tripo pilot FBX for Sir Aldric (Dustin-approved). Do NOT point at SirAldric_attempt1.fbx.
        // No hand-made .meta: Unity generates import settings on Dustin's machine.
        public const string SirAldricFbxPath = "Assets/Art/Characters/SirAldric/SirAldric.fbx";

        // Target knight height in meters (matches CharacterController height / PlaceholderSpec).
        public const float KnightTargetHeightM = 1.8f;

        // URP shader names. Shader.Find works in the Editor even before the pipeline is switched on.
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";
        private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";

        // ------------------------------------------------------------------ materials

        /// <summary>All the shared materials, created once per build and handed to the prefab builders.</summary>
        public class Materials
        {
            public Material knightSteel, knightDark, swordBlade;
            public Material[] teamPrimary = new Material[4];
            public Material[] teamSecondary = new Material[4];
            public Material[] ring = new Material[4];
            public Material zombieSkin, zombieRags, eyeShambler, eyeRunner, eyeElite;
            public Material hitFlash, hurtFlash;
            public Material floorA, floorB, mortar, wall, propStone;
            public Material gemSmall, gemMedium, gemLarge;
            public Material bloodParticle, bloodParticleOff, bloodDecal, bloodDecalDry, scorch, slash;
        }

        public static Materials CreateMaterials()
        {
            EnsureFolder(MaterialsDir);
            var m = new Materials();

            m.knightSteel = Lit("M_PH_Knight_Steel", "#A9B1BA", 0.65f, 0.80f);
            m.knightDark = Lit("M_PH_Knight_Dark", "#2E333A", 0.30f, 0.20f);
            m.swordBlade = Lit("M_PH_Sword_Blade", "#C9D0D8", 0.80f, 0.90f);

            string[] primary = { "#2F80ED", "#FF7A1A", "#14C8B4", "#F2F2F2" };
            string[] secondary = { "#F2F2F2", "#1A1A1A", "#F2C14E", "#C0283A" };
            for (int i = 0; i < 4; i++)
            {
                m.teamPrimary[i] = Lit($"M_PH_Team_Primary_P{i + 1}", primary[i], 0.25f, 0f);
                m.teamSecondary[i] = Lit($"M_PH_Team_Secondary_P{i + 1}", secondary[i], 0.35f, 0f);
                // Ring: unlit, see-through (alpha 0.7), queue 3010. Blood decals use the AlphaTest queue (2450), so they are
                // drawn first and the ring always ends up on top of them, staying visible.
                m.ring[i] = Unlit($"M_PH_Ring_P{i + 1}", primary[i], 0.70f);
                m.ring[i].renderQueue = 3010;
            }

            m.zombieSkin = Lit("M_PH_Zombie_Skin", "#7D8574", 0.20f, 0f);
            m.zombieRags = Lit("M_PH_Zombie_Rags", "#4B4F55", 0.15f, 0f);
            m.eyeShambler = Unlit("M_PH_Eye_Shambler", "#8BD346");
            m.eyeRunner = Unlit("M_PH_Eye_Runner", "#FFD23F");
            m.eyeElite = Unlit("M_PH_Eye_Elite", "#7FF3FF");
            m.hitFlash = Unlit("M_PH_HitFlash", "#FFFFFF");
            m.hurtFlash = Unlit("M_PH_HurtFlash", "#E23A3A");

            m.floorA = Lit("M_PH_Floor_A", "#6B7178", 0.15f, 0f);
            m.floorB = Lit("M_PH_Floor_B", "#5C636B", 0.15f, 0f);
            m.mortar = Lit("M_PH_Floor_Mortar", "#474D54", 0.05f, 0f);
            m.wall = Lit("M_PH_Wall", "#2E333A", 0.10f, 0f);
            m.propStone = Lit("M_PH_Prop_Stone", "#7A8087", 0.10f, 0f);

            m.gemSmall = Lit("M_PH_Gem_Small", "#3FE0FF", 0.90f, 0f, 1.5f);
            m.gemMedium = Lit("M_PH_Gem_Medium", "#4DFF7A", 0.90f, 0f, 2.0f);
            m.gemLarge = Lit("M_PH_Gem_Large", "#F2C14E", 0.90f, 0f, 2.5f);

            // Built-in soft round dot texture. Ships with the Editor, so no art pack is needed.
            var dot = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");

            m.bloodParticle = Particle("M_PH_Blood_Particle", "#B3121B", dot);
            m.bloodParticleOff = Particle("M_PH_Blood_Particle_Off", "#4B4F55", dot);

            // Ground decals use the flat-Quad fallback (PlaceholderSpec 6.2): Unlit + alpha clipping, so the round
            // dot texture cuts the square quad into a splat shape without needing transparency sorting.
            m.bloodDecal = DecalQuad("M_PH_Blood_Decal_Quad", "#B3121B", dot);
            m.bloodDecalDry = DecalQuad("M_PH_Blood_Decal_Quad_Dry", "#7A0A10", dot);
            m.scorch = DecalQuad("M_PH_Scorch_Quad", "#26282B", dot);

            // Longsword slash fan: pale, see-through, unlit. Not in the spec's table; a graybox readability aid.
            m.slash = Unlit("M_PH_Slash", "#F2F2F2", 0.35f);
            m.slash.renderQueue = 3020;

            AssetDatabase.SaveAssets();
            return m;
        }

        private static Material Lit(string name, string hex, float smoothness, float metallic, float emission = 0f)
        {
            var mat = LoadOrCreate(name, LitShader);
            Color c = Hex(hex);
            SetColor(mat, "_BaseColor", c);
            SetFloat(mat, "_Smoothness", smoothness);
            SetFloat(mat, "_Metallic", metallic);
            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                SetColor(mat, "_EmissionColor", c * emission); // HDR color above 1 = glows with Bloom
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material Unlit(string name, string hex, float alpha = 1f)
        {
            var mat = LoadOrCreate(name, UnlitShader);
            Color c = Hex(hex);
            c.a = alpha;
            SetColor(mat, "_BaseColor", c);
            if (alpha < 1f) MakeTransparent(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material Particle(string name, string hex, Texture2D baseMap)
        {
            var mat = LoadOrCreate(name, ParticleShader);
            SetColor(mat, "_BaseColor", Hex(hex));
            if (baseMap != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseMap);
            MakeTransparent(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material DecalQuad(string name, string hex, Texture2D baseMap)
        {
            var mat = LoadOrCreate(name, UnlitShader);
            SetColor(mat, "_BaseColor", Hex(hex));
            if (baseMap != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseMap);
            // Alpha clipping: pixels with alpha below 0.5 are simply not drawn.
            SetFloat(mat, "_AlphaClip", 1f);
            SetFloat(mat, "_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Sets the same properties the URP material Inspector sets when you pick "Surface Type: Transparent,
        /// Blending Mode: Alpha". Done by hand here because that Inspector code lives in an editor-only URP assembly.
        /// If a material still looks opaque, select it and toggle Surface Type once in the Inspector (see README).
        /// </summary>
        private static void MakeTransparent(Material mat)
        {
            SetFloat(mat, "_Surface", 1f);
            SetFloat(mat, "_Blend", 0f);
            SetFloat(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(mat, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(mat, "_SrcBlendAlpha", (float)BlendMode.One);
            SetFloat(mat, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(mat, "_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Material LoadOrCreate(string name, string shaderName)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[Graybox] Shader '{shaderName}' not found. Is the URP package installed? Using a fallback.");
                shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            return mat;
        }

        private static void SetColor(Material m, string prop, Color c) { if (m.HasProperty(prop)) m.SetColor(prop, c); }
        private static void SetFloat(Material m, string prop, float v) { if (m.HasProperty(prop)) m.SetFloat(prop, v); }

        /// <summary>"#RRGGBB" (sRGB, as typed in the color picker) to a Unity Color.</summary>
        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        // ------------------------------------------------------------------ primitive helper

        /// <summary>
        /// Make one primitive child: name, local position / rotation / scale, shared material, shadows.
        /// Its collider is removed straight away (spec rule: no colliders on primitive children).
        /// </summary>
        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 euler,
            Vector3 scale, Material mat, bool castShadows)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        private static Transform Empty(string name, Transform parent, Vector3 pos, Vector3 euler = default)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(euler);
            return t;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------------ knight (PlaceholderSpec 2.1)

        public static GameObject BuildKnight(Materials m, InputActionAsset actions)
        {
            var root = new GameObject("PF_PH_Knight");
            var p1Primary = m.teamPrimary[0];
            var p1Secondary = m.teamSecondary[0];

            // Logic root. The CharacterController IS the spec's capsule collider (r 0.35, h 1.8, centre y 0.9).
            var cc = root.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.03f;

            // Visual: swappable art root. Real FBX goes under Visual/ModelOffset; primitives are the fallback.
            var visual = Empty("Visual", root.transform, Vector3.zero);

            // Try the Tripo Sir Aldric FBX first. If the file is missing (or not imported yet), fall back
            // to the old capsule knight so the graybox still runs.
            Renderer[] bodyRenderers;
            Renderer[] teamPrimaryRenderers;
            Renderer[] teamSecondaryRenderers;
            Renderer weaponRenderer;
            Transform socketWeapon;

            bool usedFbx = TryAttachSirAldricModel(visual, out bodyRenderers);
            if (usedFbx)
            {
                // Tripo pilot has no separate tabard / plume / shield meshes for team tint yet.
                // KnightTint still colours the ground ring. Hit flash uses every mesh renderer.
                teamPrimaryRenderers = new Renderer[0];
                teamSecondaryRenderers = new Renderer[0];
                weaponRenderer = null;
                // Keep a weapon socket at roughly hand height so VisualBinder stays filled.
                socketWeapon = Empty("Socket_WeaponR", visual, new Vector3(0.40f, 1.00f, 0.25f), new Vector3(75f, 0, 0));
            }
            else
            {
                BuildKnightPrimitiveVisual(visual, m, p1Primary, p1Secondary,
                    out bodyRenderers, out teamPrimaryRenderers, out teamSecondaryRenderers,
                    out weaponRenderer, out socketWeapon);
            }

            // Ground ring: outside Visual so death tip / future anims never tilt it.
            var ring = Empty("GroundRing", root.transform, new Vector3(0, 0.02f, 0));
            var disc = Prim(PrimitiveType.Cylinder, "Disc", ring, Vector3.zero, Vector3.zero, new Vector3(1.50f, 0.005f, 1.50f), m.ring[0], false);
            var arrow = Prim(PrimitiveType.Cube, "FacingArrow", ring, new Vector3(0, 0, 0.95f), new Vector3(0, 45f, 0), new Vector3(0.30f, 0.01f, 0.30f), m.ring[0], false);

            var socketHit = Empty("Socket_Hit", root.transform, new Vector3(0, 1.20f, 0));
            var socketOverhead = Empty("Socket_Overhead", root.transform, new Vector3(0, 2.20f, 0));

            // Gameplay components. Adding PlayerController also adds Health, PlayerMotor, PlayerAim and
            // PlayerLoadout automatically ([RequireComponent]). PlayerInputHandler adds PlayerInput.
            root.AddComponent<PlayerController>();
            // (Never use "??" with Unity objects: a missing component can be a "fake null" in the Editor.)
            root.AddComponent<PlayerInputHandler>();
            var input = root.GetComponent<PlayerInput>();
            if (actions != null)
            {
                input.actions = actions;
                input.defaultActionMap = "Gameplay";
            }
            input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            var binder = root.AddComponent<VisualBinder>();
            binder.visualRoot = visual;
            binder.bodyRenderers = bodyRenderers;
            binder.teamPrimaryRenderers = teamPrimaryRenderers;
            binder.teamSecondaryRenderers = teamSecondaryRenderers;
            binder.weaponRenderer = weaponRenderer;
            binder.weaponSocket = socketWeapon;
            binder.hitSocket = socketHit;
            binder.overheadSocket = socketOverhead;

            var tint = root.AddComponent<KnightTint>();
            tint.teamPrimaryMaterials = (Material[])m.teamPrimary.Clone();
            tint.teamSecondaryMaterials = (Material[])m.teamSecondary.Clone();
            tint.ringMaterials = (Material[])m.ring.Clone();
            tint.ringRenderers = new Renderer[] { R(disc), R(arrow) };

            // What the camera follows. An empty child at the feet, smoothed like the model (below).
            // NOT the root: the root only moves at 50 Hz, so a camera following it stutters (the "jitter" bug).
            var cameraTarget = Empty("CameraTarget", root.transform, Vector3.zero);

            // Smooths everything you SEE between the 50 Hz physics ticks: the model, the ground ring and the
            // camera target. All three must be listed, or that one piece would visibly stutter at high frame rates.
            var interp = root.AddComponent<TickInterpolator>();
            interp.smoothedChildren = new[] { visual, ring, cameraTarget };

            var player = root.GetComponent<PlayerController>();
            player.cameraTarget = cameraTarget;

            return SavePrefab(root, PrefabsDir + "/PF_PH_Knight.prefab");
        }

        /// <summary>
        /// Old capsule / box knight (PlaceholderSpec 2.1). Used when SirAldric.fbx is not in the project yet.
        /// </summary>
        private static void BuildKnightPrimitiveVisual(
            Transform visual, Materials m, Material p1Primary, Material p1Secondary,
            out Renderer[] bodyRenderers, out Renderer[] teamPrimaryRenderers,
            out Renderer[] teamSecondaryRenderers, out Renderer weaponRenderer, out Transform socketWeapon)
        {
            var body = Prim(PrimitiveType.Capsule, "Body", visual, new Vector3(0, 0.90f, 0), Vector3.zero, new Vector3(0.70f, 0.90f, 0.70f), m.knightSteel, true);
            var tabard = Prim(PrimitiveType.Cube, "Tabard", visual, new Vector3(0, 0.85f, 0.30f), Vector3.zero, new Vector3(0.50f, 0.70f, 0.12f), p1Primary, true);
            var belt = Prim(PrimitiveType.Cube, "Belt", visual, new Vector3(0, 0.75f, 0), Vector3.zero, new Vector3(0.74f, 0.10f, 0.74f), m.knightDark, true);
            var helmet = Prim(PrimitiveType.Sphere, "Helmet", visual, new Vector3(0, 1.62f, 0), Vector3.zero, Vector3.one * 0.42f, m.knightSteel, true);
            var visor = Prim(PrimitiveType.Cube, "Visor", helmet.transform, new Vector3(0, 0.02f, 0.45f), Vector3.zero, new Vector3(0.70f, 0.12f, 0.15f), m.knightDark, true);
            var plume = Prim(PrimitiveType.Cube, "Plume", helmet.transform, new Vector3(0, 0.65f, -0.15f), new Vector3(-20f, 0, 0), new Vector3(0.15f, 0.70f, 0.45f), p1Primary, true);
            var pauldronL = Prim(PrimitiveType.Sphere, "Pauldron_L", visual, new Vector3(-0.42f, 1.40f, 0), Vector3.zero, new Vector3(0.34f, 0.24f, 0.34f), m.knightSteel, true);
            var bandL = Prim(PrimitiveType.Cube, "Band", pauldronL.transform, new Vector3(0, -0.1f, 0), Vector3.zero, new Vector3(1.05f, 0.25f, 1.05f), p1Secondary, true);
            var pauldronR = Prim(PrimitiveType.Sphere, "Pauldron_R", visual, new Vector3(0.42f, 1.40f, 0), Vector3.zero, new Vector3(0.34f, 0.24f, 0.34f), m.knightSteel, true);
            var bandR = Prim(PrimitiveType.Cube, "Band", pauldronR.transform, new Vector3(0, -0.1f, 0), Vector3.zero, new Vector3(1.05f, 0.25f, 1.05f), p1Secondary, true);

            // Shield: "rot (0,0,90) then (0,-20,0)" = first stand the disc up, then turn it 20 degrees.
            var shield = Prim(PrimitiveType.Cylinder, "Shield", visual, new Vector3(-0.48f, 1.00f, 0.18f), Vector3.zero, new Vector3(0.60f, 0.04f, 0.60f), p1Primary, true);
            shield.transform.localRotation = Quaternion.Euler(0f, -20f, 0f) * Quaternion.Euler(0f, 0f, 90f);
            var crossV = Prim(PrimitiveType.Cube, "Cross_V", shield.transform, new Vector3(0, 1.2f, 0), Vector3.zero, new Vector3(0.18f, 0.5f, 0.9f), p1Secondary, true);
            var crossH = Prim(PrimitiveType.Cube, "Cross_H", shield.transform, new Vector3(0, 1.2f, 0), Vector3.zero, new Vector3(0.9f, 0.5f, 0.18f), p1Secondary, true);

            socketWeapon = Empty("Socket_WeaponR", visual, new Vector3(0.40f, 1.00f, 0.25f), new Vector3(75f, 0, 0));
            var sword = Empty("Sword", socketWeapon, Vector3.zero);
            var grip = Prim(PrimitiveType.Cylinder, "Grip", sword, Vector3.zero, Vector3.zero, new Vector3(0.05f, 0.10f, 0.05f), m.knightDark, true);
            var guard = Prim(PrimitiveType.Cube, "Guard", sword, new Vector3(0, 0.11f, 0), Vector3.zero, new Vector3(0.28f, 0.04f, 0.06f), m.knightSteel, true);
            var blade = Prim(PrimitiveType.Cube, "Blade", sword, new Vector3(0, 0.66f, 0), Vector3.zero, new Vector3(0.09f, 1.05f, 0.025f), m.swordBlade, true);

            // Body renderers = what flashes when hurt. NOT the team-colour pieces or the ring (spec 2.5).
            bodyRenderers = new Renderer[]
            {
                R(body), R(belt), R(helmet), R(visor), R(pauldronL), R(pauldronR), R(grip), R(guard), R(blade)
            };
            teamPrimaryRenderers = new Renderer[] { R(tabard), R(plume), R(shield) };
            teamSecondaryRenderers = new Renderer[] { R(bandL), R(bandR), R(crossV), R(crossH) };
            weaponRenderer = R(blade);
        }

        /// <summary>
        /// Parent SirAldric.fbx under Visual/ModelOffset with Z-up→Y-up, ~1.8 m height, feet at y=0.
        /// Returns false if the FBX is missing so the caller can use primitives instead.
        ///
        /// WHY ModelOffset (not .meta)?
        /// Dustin lets Unity generate .meta files — we must NOT hand-write FBX import settings.
        /// So axis / scale / pivot fixes live on this empty transform instead.
        ///
        /// FBX facts (parsed from the binary file, Vertices + GlobalSettings):
        ///   UpAxis metadata = Y (1), but mesh height is along Z (~0.998 m), feet at Z=-0.5 (centered pivot).
        ///   UnitScaleFactor = 1.0 (Autodesk cm convention). Vertex numbers are ~1 m already, so Unity's
        ///   default "Convert Units" makes the mesh ~100× too small — we fix that by measuring bounds
        ///   AFTER instantiate and scaling to KnightTargetHeightM (that automatically eats the 100×).
        /// </summary>
        private static bool TryAttachSirAldricModel(Transform visual, out Renderer[] bodyRenderers)
        {
            bodyRenderers = null;

            // Application.dataPath = <Project>/Assets. Prefer that over File.Exists("Assets/...") which
            // depends on the process working directory.
            string fullPath = Path.Combine(Application.dataPath, "Art/Characters/SirAldric/SirAldric.fbx");
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("[Knight Survivors] Sir Aldric FBX not found at " + SirAldricFbxPath +
                                 " — using primitive placeholder knight.");
                return false;
            }

            // First import creates Unity's default .meta (we never hand-write one).
            AssetDatabase.ImportAsset(SirAldricFbxPath, ImportAssetOptions.ForceSynchronousImport);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SirAldricFbxPath);
            if (modelAsset == null)
            {
                Debug.LogWarning("[Knight Survivors] Could not load " + SirAldricFbxPath +
                                 " — using primitive placeholder knight.");
                return false;
            }

            // Intermediate offset: Visual stays at identity (death tip / facing use it); fixes go here.
            var modelOffset = Empty("ModelOffset", visual, Vector3.zero);

            // -90° X: Z-up mesh → Y-up. +180° Y: after that spin the mesh faced -Z; Unity forward is +Z.
            modelOffset.localRotation = Quaternion.Euler(-90f, 180f, 0f);
            modelOffset.localScale = Vector3.one;
            modelOffset.localPosition = Vector3.zero;

            // Object.Instantiate (not PrefabUtility.InstantiatePrefab): the FBX is baked into
            // PF_PH_Knight by SaveAsPrefabAsset below, so we don't need a live prefab link.
            var instance = Object.Instantiate(modelAsset, modelOffset);
            instance.name = "SirAldric";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Prefab rule: no colliders on visual children (root CharacterController is the only one).
            foreach (var col in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            // Avoid pink materials: Tripo often ships Built-in/Standard shaders; project is URP.
            UpgradeImportedMaterialsToUrpLit(instance);

            // Measure mesh bounds in WORLD space (Visual is at origin while we build the prefab).
            Bounds b = EncapsulateRendererBounds(instance);
            if (b.size.y < 0.0001f)
            {
                Debug.LogWarning("[Knight Survivors] Sir Aldric bounds look empty — using primitive placeholder.");
                Object.DestroyImmediate(modelOffset.gameObject);
                bodyRenderers = null;
                return false;
            }

            // Scale so standing height ≈ 1.8 m. If Convert Units shrunk the mesh 100×, this scale is ~180.
            float scale = KnightTargetHeightM / b.size.y;
            modelOffset.localScale = Vector3.one * scale;

            // Re-measure after scale; lift so the lowest point sits on y=0 (feet at the pivot).
            b = EncapsulateRendererBounds(instance);
            modelOffset.localPosition = new Vector3(0f, -b.min.y, 0f);

            bodyRenderers = instance.GetComponentsInChildren<Renderer>(true);
            Debug.Log("[Knight Survivors] Sir Aldric FBX attached under Visual/ModelOffset. " +
                      "preScaleHeight=" + (KnightTargetHeightM / scale).ToString("F4") +
                      "m scale=" + scale.ToString("F3") +
                      " yOffset=" + modelOffset.localPosition.y.ToString("F3") +
                      " rot=" + modelOffset.localEulerAngles +
                      " renderers=" + bodyRenderers.Length);
            return true;
        }

        /// <summary>World-space AABB of every Renderer under <paramref name="root"/>.</summary>
        private static Bounds EncapsulateRendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var b = new Bounds(root.transform.position, Vector3.zero);
            bool any = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (!any) { b = renderers[i].bounds; any = true; }
                else b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        /// <summary>
        /// Point imported materials at URP/Lit and keep albedo texture / colour when possible.
        /// Stops the classic "pink missing shader" look after dropping a Tripo FBX into a URP project.
        /// Edits the material assets in place (graybox OK; a real art pass will author proper URP mats).
        /// </summary>
        private static void UpgradeImportedMaterialsToUrpLit(GameObject root)
        {
            var lit = Shader.Find(LitShader);
            if (lit == null) return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var mats = renderers[i].sharedMaterials;
                if (mats == null) continue;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null || mat.shader == null) continue;
                    string sn = mat.shader.name;
                    if (sn.Contains("Universal Render Pipeline")) continue;

                    Texture tex = null;
                    if (mat.HasProperty("_BaseMap")) tex = mat.GetTexture("_BaseMap");
                    else if (mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");

                    Color col = Color.white;
                    if (mat.HasProperty("_BaseColor")) col = mat.GetColor("_BaseColor");
                    else if (mat.HasProperty("_Color")) col = mat.GetColor("_Color");

                    mat.shader = lit;
                    if (tex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                }
            }
        }

        private static Renderer R(GameObject go) => go.GetComponent<Renderer>();

        // ------------------------------------------------------------------ zombies (PlaceholderSpec 3.1 / 3.2)

        public static GameObject BuildShambler(Materials m)
        {
            var root = new GameObject("PF_PH_Zombie_Shambler");
            var visual = Empty("Visual", root.transform, Vector3.zero, new Vector3(12f, 0, 0));
            Prim(PrimitiveType.Capsule, "Body", visual, new Vector3(0, 0.80f, 0), Vector3.zero, new Vector3(0.55f, 0.80f, 0.50f), m.zombieRags, false);
            var head = Prim(PrimitiveType.Sphere, "Head", visual, new Vector3(0, 1.45f, 0.10f), Vector3.zero, Vector3.one * 0.32f, m.zombieSkin, false);
            Eyes(head.transform, 0.07f, m.eyeShambler);
            Prim(PrimitiveType.Cylinder, "Collar", visual, new Vector3(0, 1.25f, 0.02f), Vector3.zero, new Vector3(0.58f, 0.02f, 0.55f), m.eyeShambler, false);
            Prim(PrimitiveType.Cube, "Arm_L", visual, new Vector3(-0.22f, 1.15f, 0.35f), Vector3.zero, new Vector3(0.12f, 0.12f, 0.65f), m.zombieSkin, false);
            Prim(PrimitiveType.Cube, "Arm_R", visual, new Vector3(0.22f, 1.10f, 0.35f), new Vector3(8f, 0, 0), new Vector3(0.12f, 0.12f, 0.60f), m.zombieSkin, false);
            FinishEnemy(root, visual, 1.1f);
            return SavePrefab(root, PrefabsDir + "/PF_PH_Zombie_Shambler.prefab");
        }

        public static GameObject BuildRunner(Materials m)
        {
            var root = new GameObject("PF_PH_Zombie_Runner");
            var visual = Empty("Visual", root.transform, Vector3.zero, new Vector3(30f, 0, 0));
            Prim(PrimitiveType.Capsule, "Body", visual, new Vector3(0, 0.78f, 0), Vector3.zero, new Vector3(0.40f, 0.78f, 0.36f), m.zombieRags, false);
            var head = Prim(PrimitiveType.Sphere, "Head", visual, new Vector3(0, 1.38f, 0.08f), Vector3.zero, Vector3.one * 0.28f, m.zombieSkin, false);
            Eyes(head.transform, 0.09f, m.eyeRunner);
            Prim(PrimitiveType.Cylinder, "Collar", visual, new Vector3(0, 1.18f, 0), Vector3.zero, new Vector3(0.42f, 0.02f, 0.38f), m.eyeRunner, false);
            Prim(PrimitiveType.Cube, "Arm_L", visual, new Vector3(-0.20f, 0.95f, -0.30f), new Vector3(-35f, 0, 0), new Vector3(0.10f, 0.10f, 0.55f), m.zombieSkin, false);
            Prim(PrimitiveType.Cube, "Arm_R", visual, new Vector3(0.20f, 0.95f, -0.30f), new Vector3(-35f, 0, 0), new Vector3(0.10f, 0.10f, 0.55f), m.zombieSkin, false);
            FinishEnemy(root, visual, 1.0f);
            return SavePrefab(root, PrefabsDir + "/PF_PH_Zombie_Runner.prefab");
        }

        /// <summary>Two eye spheres at head-local (+-0.12, 0.05, 0.42), the type's unlit eye material.</summary>
        private static void Eyes(Transform head, float size, Material mat)
        {
            // Positions are in the head's own units (its surface is at 0.5), as the spec says "head-local".
            // The size is read as metres: the head is scaled about 0.3, so local scale = size / headScale.
            // (A literal local 0.07 would be a 2 cm eye, invisible from the game camera. Open question in README.)
            float local = size / Mathf.Max(0.01f, head.localScale.x);
            Prim(PrimitiveType.Sphere, "Eye_L", head, new Vector3(-0.12f, 0.05f, 0.42f), Vector3.zero, Vector3.one * local, mat, false);
            Prim(PrimitiveType.Sphere, "Eye_R", head, new Vector3(0.12f, 0.05f, 0.42f), Vector3.zero, Vector3.one * local, mat, false);
        }

        /// <summary>Logic components every crowd zombie shares. No collider: enemies use the spatial hash.</summary>
        private static void FinishEnemy(GameObject root, Transform visual, float hitHeight)
        {
            root.AddComponent<Enemy>(); // also adds Health ([RequireComponent])
            var hit = Empty("Socket_Hit", root.transform, new Vector3(0, hitHeight, 0));
            var binder = root.AddComponent<VisualBinder>();
            binder.visualRoot = visual;
            binder.hitSocket = hit;
            binder.bodyRenderers = visual.GetComponentsInChildren<Renderer>(true);
        }

        // ------------------------------------------------------------------ gems (PlaceholderSpec 4)

        public static XpGem BuildGem(int tier, Materials m)
        {
            string[] names = { "PF_PH_Gem_Small", "PF_PH_Gem_Medium", "PF_PH_Gem_Large" };
            var root = new GameObject(names[tier]);
            var visual = Empty("Visual", root.transform, new Vector3(0, 0.35f, 0));
            if (tier == 0)
                Prim(PrimitiveType.Cube, "Diamond", visual, Vector3.zero, new Vector3(45, 0, 45), Vector3.one * 0.22f, m.gemSmall, false);
            else if (tier == 1)
                Prim(PrimitiveType.Cube, "Diamond", visual, Vector3.zero, new Vector3(45, 0, 45), new Vector3(0.30f, 0.45f, 0.30f), m.gemMedium, false);
            else
            {
                Prim(PrimitiveType.Cube, "Star_A", visual, Vector3.zero, new Vector3(45, 0, 45), Vector3.one * 0.40f, m.gemLarge, false);
                Prim(PrimitiveType.Cube, "Star_B", visual, Vector3.zero, new Vector3(45, 45, 45), Vector3.one * 0.40f, m.gemLarge, false);
            }

            var gem = root.AddComponent<XpGem>();
            gem.tier = tier;
            gem.visual = visual;
            return SavePrefab(root, $"{PrefabsDir}/{names[tier]}.prefab").GetComponent<XpGem>();
        }

        // ------------------------------------------------------------------ longsword weapon prefab

        /// <summary>
        /// The weapon prefab: a LongswordWeapon plus a hidden "slash fan" template the weapon copies at start.
        /// The fan mesh itself is generated in code (a primitive can't make a pie slice).
        /// </summary>
        public static GameObject BuildLongswordPrefab(Materials m)
        {
            var root = new GameObject("PF_PH_Weapon_Longsword");
            var weapon = root.AddComponent<LongswordWeapon>();

            var template = new GameObject("SlashTemplate");
            template.transform.SetParent(root.transform, false);
            template.AddComponent<MeshFilter>();
            var r = template.AddComponent<MeshRenderer>();
            r.sharedMaterial = m.slash;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            weapon.slashVisualTemplate = r;

            return SavePrefab(root, PrefabsDir + "/PF_PH_Weapon_Longsword.prefab");
        }

        // ------------------------------------------------------------------ blood (PlaceholderSpec 6.1 / 6.2)

        public enum BurstKind { Hit, Crit, Kill, Big }

        public static ParticleSystem BuildBlood(BurstKind kind, Material material, string prefabName, bool dust = false)
        {
            var root = new GameObject(prefabName);
            var ps = root.AddComponent<ParticleSystem>();
            // A fresh ParticleSystem starts playing a default effect; stop it while we configure it.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Per-prefab numbers from the PlaceholderSpec 6.1 table.
            float duration = 0.20f, lifeMin = 0.35f, lifeMax = 0.55f, speedMin = 3f, speedMax = 6f;
            float sizeMin = 0.06f, sizeMax = 0.14f, gravity = 2.5f;
            int maxParticles = 24;
            switch (kind)
            {
                case BurstKind.Crit:
                    duration = 0.25f; lifeMin = 0.40f; lifeMax = 0.60f; speedMin = 5f; speedMax = 9f;
                    sizeMin = 0.08f; sizeMax = 0.16f; maxParticles = 36; break;
                case BurstKind.Kill:
                    duration = 0.30f; lifeMin = 0.45f; lifeMax = 0.70f; speedMin = 4f; speedMax = 8f;
                    sizeMin = 0.10f; sizeMax = 0.22f; gravity = 3f; maxParticles = 32; break;
                case BurstKind.Big:
                    duration = 0.30f; lifeMin = 0.45f; lifeMax = 0.70f; speedMin = 4f; speedMax = 8f;
                    sizeMin = 0.12f; sizeMax = 0.28f; gravity = 3f; maxParticles = 80; break;
            }

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false; // GoreManager calls Emit(n) itself
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            // Random between fresh and dark blood (spec 6.1). Note: the shader MULTIPLIES this by the material's
            // #B3121B, so on screen it's darker than either; if it reads too dark, set M_PH_Blood_Particle to white.
            // Dust (the "Blood: Off" setting) uses white so the grey material colour shows unchanged.
            main.startColor = dust
                ? new ParticleSystem.MinMaxGradient(Color.white, new Color(0.8f, 0.8f, 0.8f, 1f))
                : new ParticleSystem.MinMaxGradient(Hex("#B3121B"), Hex("#7A0A10"));
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.stopAction = ParticleSystemStopAction.None; // GoreManager despawns it

            var emission = ps.emission;
            emission.enabled = false; // no automatic emission; bursts come from Emit(n)

            var shape = ps.shape;
            shape.enabled = true;
            if (kind == BurstKind.Kill || kind == BurstKind.Big)
            {
                shape.shapeType = ParticleSystemShapeType.Hemisphere;
                shape.radius = kind == BurstKind.Big ? 0.3f : 0.15f;
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = kind == BurstKind.Crit ? 35f : 25f;
                shape.radius = 0.05f;
            }

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f, 0f, -0.5f), new Keyframe(1f, 0.3f)));

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

            var psr = root.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sharedMaterial = material;
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;
            psr.sortMode = ParticleSystemSortMode.None;

            return SavePrefab(root, $"{VfxDir}/{prefabName}.prefab").GetComponent<ParticleSystem>();
        }

        /// <summary>Flat Quad splat (PlaceholderSpec 6.2 fallback). GoreManager sets its size, height and rotation.</summary>
        public static GameObject BuildDecalQuad(Materials m)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Quad);
            root.name = "PF_PH_BloodDecal_Quad";
            Object.DestroyImmediate(root.GetComponent<Collider>());
            root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var r = root.GetComponent<MeshRenderer>();
            r.sharedMaterial = m.bloodDecal;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return SavePrefab(root, $"{VfxDir}/PF_PH_BloodDecal_Quad.prefab");
        }

        // ------------------------------------------------------------------ folders

        /// <summary>Create "Assets/A/B/C" one level at a time (AssetDatabase.CreateFolder only makes one level).</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
