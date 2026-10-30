using System.Collections.Generic;
using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Combat
{
    /// <summary>
    /// GDD 3.2: the Longsword (archetype MeleeArc). Every cooldown it slashes a 120° arc of radius
    /// "area" in the aim direction and hits every enemy inside (pierce -1 = unlimited).
    ///   amount 1 = front slash, amount 2 = + rear slash, amount 3+ = + both side slashes.
    ///
    /// HOW THE HIT TEST WORKS (no physics):
    ///   1. Ask the EnemyManager (IHitQuery) for every enemy within the radius.
    ///   2. Keep only those whose direction is within half the arc angle of the slash direction.
    ///   3. Damage each one once per swing (an enemy inside two overlapping arcs is hit only once).
    /// </summary>
    public class LongswordWeapon : WeaponBase
    {
        [Tooltip("Template for the slash visual (a flat fan mesh). Copies are made at start; leave empty for no visual.")]
        public MeshRenderer slashVisualTemplate;

        [Tooltip("Height of the slash visual above the ground (m).")]
        public float slashHeight = 0.6f;

        private readonly List<Health> candidates = new List<Health>(128);
        private readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
        private readonly List<SlashVisual> visuals = new List<SlashVisual>(4);
        private IHitQuery hitQuery;

        // Slash directions, measured from the aim direction (degrees around the vertical axis).
        private static readonly float[] SlashAngles = { 0f, 180f, 90f, -90f }; // front, rear, right, left

        /// <summary>
        /// GDD 3.2 direction pattern. "amount" is NOT simply "number of slashes":
        ///   amount 1 -> 1 slash  (front)
        ///   amount 2 -> 2 slashes (front + rear)
        ///   amount 3 -> 4 slashes (front + rear + BOTH sides). Adding only one side would look lopsided.
        /// Extra amount above 3 (from a future passive) changes nothing, because all 4 directions are used.
        /// </summary>
        private static int SlashCountForAmount(int amount)
        {
            if (amount <= 1) return 1;
            if (amount == 2) return 2;
            return 4;
        }

        public override void Initialize(IWeaponOwner owner, Data.WeaponDefinition definition)
        {
            base.Initialize(owner, definition);
            BuildVisuals();
        }

        private void BuildVisuals()
        {
            if (slashVisualTemplate == null || visuals.Count > 0) return;

            // One fan mesh shared by all 4 copies. Radius 1, scaled to the real radius when shown.
            var mesh = SlashVisual.BuildFanMesh(Definition.arcDegrees, 16);
            for (int i = 0; i < SlashAngles.Length; i++)
            {
                // Parent to the owner's SMOOTHED anchor, not to this weapon. This weapon object sits under the
                // player ROOT, which only moves at 50 Hz; a slash parented there stepped every tick while the
                // knight and the camera glided, which read as fast jitter while walking (the sword swings
                // on its cooldown even with no enemies around).
                var copy = Instantiate(slashVisualTemplate, VisualParent());
                copy.gameObject.name = "Slash_" + i;
                copy.GetComponent<MeshFilter>().sharedMesh = mesh;
                copy.gameObject.SetActive(false);
                visuals.Add(new SlashVisual(copy));
            }

            slashVisualTemplate.gameObject.SetActive(false);
        }

        protected override void Fire()
        {
            if (hitQuery == null && !ServiceLocator.TryGet(out hitQuery)) return;

            var level = CurrentLevelData;
            var stats = Owner.Stats;

            float radius = level.area * stats.area;
            float halfArc = Definition.arcDegrees * 0.5f;
            int slashes = SlashCountForAmount(level.amount + stats.amount);
            int pierceLeft = level.pierce < 0 ? int.MaxValue : level.pierce;

            Vector3 origin = Owner.Position;
            Vector3 aim = Owner.AimDirection;
            if (aim.sqrMagnitude < 0.0001f) aim = Vector3.forward;

            // Start every swing with empty lists. They are reused (no garbage), so leftovers from the last
            // swing would otherwise be hit again, wherever those enemies are now.
            hitThisSwing.Clear();
            candidates.Clear();
            hitQuery.QueryEnemies(origin, radius, candidates);

            for (int s = 0; s < slashes; s++)
            {
                Vector3 slashDir = Quaternion.Euler(0f, SlashAngles[s], 0f) * aim;
                ShowVisual(s, origin, slashDir, radius, level.duration * stats.duration);

                for (int i = 0; i < candidates.Count && pierceLeft > 0; i++)
                {
                    var target = candidates[i];
                    if (target == null || target.IsDead || target.team == Owner.Team) continue;
                    if (hitThisSwing.Contains(target)) continue;

                    Vector3 toTarget = target.transform.position - origin;
                    toTarget.y = 0f;

                    // 1) RANGE: the GDD reach is the weapon's "area" (2.8 m at level 1, times the Area stat).
                    //    We measure to the enemy's centre and add its body radius, so the blade only has to
                    //    touch the edge of a zombie. Checked here too (not only in the query) so the arc test
                    //    stays correct even if some other code hands us a longer list.
                    float reach = radius + target.bodyRadius;
                    if (toTarget.sqrMagnitude > reach * reach) continue;

                    // 2) ANGLE: inside the 120° fan. Enemies standing right on top of the knight always count.
                    if (toTarget.sqrMagnitude > 0.25f && Vector3.Angle(slashDir, toTarget) > halfArc) continue;

                    float damage = RollDamage(out bool isCrit);
                    Vector3 pushDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : slashDir;

                    var info = new DamageInfo(damage, target.HitPosition, pushDir, level.knockback, Owner.OwnerObject,
                        isCrit, Definition.damageType);
                    target.TakeDamage(info);

                    hitThisSwing.Add(target);
                    pierceLeft--;
                }
            }
        }

        private void ShowVisual(int index, Vector3 origin, Vector3 direction, float radius, float duration)
        {
            if (index >= visuals.Count) return;
            // "origin" is the gameplay position (this tick). The visual is placed relative to its smoothed
            // parent instead, so it appears exactly under the knight you SEE, not one tick ahead of it.
            Transform parent = VisualParent();
            bool attached = visuals[index].Parent == parent && parent != transform;
            Vector3 position = attached ? parent.position : origin;
            visuals[index].Show(position + Vector3.up * slashHeight, direction, radius, Mathf.Max(0.1f, duration));
        }

        // Where the slash copies live: the owner's smoothed VisualAnchor if it has one, else this weapon.
        private Transform VisualParent()
        {
            Transform anchor = Owner != null ? Owner.VisualAnchor : null;
            return anchor != null ? anchor : transform;
        }

        private void OnDestroy()
        {
            // The copies may live under the owner, not under us, so clean them up ourselves.
            for (int i = 0; i < visuals.Count; i++)
            {
                if (visuals[i].Parent != null && visuals[i].Parent != transform) Destroy(visuals[i].GameObject);
            }
        }

        private void Update()
        {
            // Presentation only (runs every rendered frame): hide slashes whose time is up.
            for (int i = 0; i < visuals.Count; i++) visuals[i].Tick(Time.deltaTime);
        }

        /// <summary>One flat fan-shaped slash mesh that appears for a moment.</summary>
        private class SlashVisual
        {
            private readonly MeshRenderer renderer;
            private float timeLeft;
            private float totalTime;
            private float radius;

            public SlashVisual(MeshRenderer renderer)
            {
                this.renderer = renderer;
            }

            public Transform Parent => renderer != null ? renderer.transform.parent : null;
            public GameObject GameObject => renderer != null ? renderer.gameObject : null;

            public void Show(Vector3 position, Vector3 direction, float slashRadius, float duration)
            {
                var t = renderer.transform;
                t.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));
                radius = slashRadius;
                t.localScale = new Vector3(radius, 1f, radius);
                totalTime = duration;
                timeLeft = duration;
                renderer.gameObject.SetActive(true);
            }

            public void Tick(float dt)
            {
                if (timeLeft <= 0f) return;
                timeLeft -= dt;

                // Grow quickly from 70% to 100% of the radius: reads as a "swing" instead of a pop-in.
                float progress = 1f - Mathf.Clamp01(timeLeft / totalTime);
                float scale = radius * Mathf.Lerp(0.7f, 1f, Mathf.Sqrt(progress));
                renderer.transform.localScale = new Vector3(scale, 1f, scale);

                if (timeLeft <= 0f) renderer.gameObject.SetActive(false);
            }

            /// <summary>
            /// Build a flat fan (pie slice) on the XZ plane, pointing along +Z, radius 1.
            /// arcDegrees = 120 gives a 120° slice. segments = how smooth the curved edge is.
            /// </summary>
            public static Mesh BuildFanMesh(float arcDegrees, int segments)
            {
                var vertices = new Vector3[segments + 2];
                var triangles = new int[segments * 3];
                vertices[0] = Vector3.zero;

                float start = -arcDegrees * 0.5f;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (start + arcDegrees * i / segments) * Mathf.Deg2Rad;
                    vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                }

                for (int i = 0; i < segments; i++)
                {
                    // Clockwise winding seen from above, so the fan faces up toward the camera.
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }

                var mesh = new Mesh { name = "SlashFan" };
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
