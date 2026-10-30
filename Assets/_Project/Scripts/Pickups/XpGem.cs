using KnightSurvivors.Player;
using KnightSurvivors.Pooling;
using UnityEngine;

namespace KnightSurvivors.Pickups
{
    /// <summary>
    /// One XP gem on the ground. It holds only data; the PickupManager moves all gems in one loop
    /// (faster than 400 separate Update() calls and easier to follow).
    /// Tiers (GDD 4.1 / PlaceholderSpec): 0 = small cyan (1 XP), 1 = medium green (5), 2 = large gold (25).
    /// </summary>
    public class XpGem : MonoBehaviour, IPoolable
    {
        [Tooltip("0 small, 1 medium, 2 large.")]
        public int tier;

        [Tooltip("The child that bobs and spins (purely visual).")]
        public Transform visual;

        /// <summary>XP this gem gives. Can be more than the tier value when gems merge at the 400 cap.</summary>
        [HideInInspector] public float value;

        // Runtime data used by PickupManager.
        [HideInInspector] public bool magnetized;
        // The player this gem is flying to. A direct reference, not a list index: if a co-op player
        // leaves mid-run the PlayerRegistry list shifts, and an index would then point at someone else.
        // [NonSerialized]: runtime-only, Unity should never save it into a prefab or scene.
        [System.NonSerialized] public PlayerController targetPlayer;
        [HideInInspector] public float speed;
        [HideInInspector] public float spawnTime;

        // Gameplay position (moved at 50 Hz) and the previous tick, so Update can lerp the drawn
        // transform smoothly between them — same prev/curr pattern as EnemyManager / TickInterpolator.
        [HideInInspector] public Vector3 simPosition;
        [HideInInspector] public Vector3 previousPosition;

        private Vector3 visualBaseLocal;

        private void Awake()
        {
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual != null) visualBaseLocal = visual.localPosition;
        }

        public void OnSpawned()
        {
            magnetized = false;
            targetPlayer = null;
            speed = 0f;
            spawnTime = Time.time;
            simPosition = previousPosition = transform.position;
        }

        public void OnDespawned()
        {
        }

        /// <summary>Bob 0.1 m and spin (PlaceholderSpec), called by PickupManager every frame.</summary>
        public void Animate(float time)
        {
            if (visual == null) return;
            visual.localPosition = visualBaseLocal + Vector3.up * (Mathf.Sin(time * 3f + spawnTime) * 0.1f);
            visual.localRotation = Quaternion.Euler(0f, time * 90f, 0f) * Quaternion.Euler(45f, 0f, 45f);
        }
    }
}
