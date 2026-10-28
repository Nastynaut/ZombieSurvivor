using UnityEngine;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// The ONLY way gameplay code talks to an entity's model (PlaceholderSpec 7.1).
    ///
    /// Every character / enemy / gem prefab looks like this:
    ///   PF_Name            (logic root: controller, Health, VisualBinder)
    ///   ├─ Visual          (SWAPPABLE: placeholder primitives now, real model later)
    ///   ├─ Socket_Hit      (where blood spawns)
    ///   └─ Socket_Overhead (name tag / HP bar later)
    ///
    /// Gameplay code must NEVER do transform.Find("Visual/Head"): that path breaks the day the
    /// artist swaps the model. Instead it asks this component for the renderers / sockets. When
    /// the art changes, only the references in this component need re-assigning (or leave the
    /// arrays empty and it auto-fills them from the Visual child).
    /// </summary>
    public class VisualBinder : MonoBehaviour
    {
        [Tooltip("The swappable model root (the 'Visual' child).")]
        public Transform visualRoot;

        [Tooltip("Optional Animator on the Visual (placeholders have none yet).")]
        public Animator animator;

        [Tooltip("All renderers of the model. Leave empty to auto-fill from visualRoot.")]
        public Renderer[] bodyRenderers;

        [Tooltip("Knight only: renderers tinted with the player's primary team color.")]
        public Renderer[] teamPrimaryRenderers;

        [Tooltip("Knight only: renderers tinted with the player's secondary team color.")]
        public Renderer[] teamSecondaryRenderers;

        [Tooltip("Knight only: the weapon renderer (for blood build-up later).")]
        public Renderer weaponRenderer;

        [Tooltip("Knight only: where the held weapon is attached.")]
        public Transform weaponSocket;

        [Tooltip("Chest-height point where blood bursts spawn.")]
        public Transform hitSocket;

        [Tooltip("Above the head: name tag / HP bar later.")]
        public Transform overheadSocket;

        // Original shared materials of each body renderer, cached once so the hit flash can swap
        // to the flash material and back WITHOUT creating new material copies (keeps batching fast).
        private Material[][] originalMaterials;
        private Material[][] flashMaterials;
        private Material cachedFlashMaterial;
        private bool isFlashing;

        private void Awake()
        {
            if (visualRoot == null)
            {
                var child = transform.Find("Visual"); // only allowed here, inside the binder itself
                if (child != null) visualRoot = child;
            }

            if ((bodyRenderers == null || bodyRenderers.Length == 0) && visualRoot != null)
            {
                bodyRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            }

            RefreshMaterialCache();
        }

        /// <summary>
        /// Remember the renderers' current materials as "normal". Call this after swapping materials
        /// on purpose (for example KnightTint's team colors), or the flash would restore old ones.
        /// </summary>
        public void RefreshMaterialCache()
        {
            if (isFlashing) SetFlash(false, cachedFlashMaterial);
            flashMaterials = null;
            if (bodyRenderers == null) bodyRenderers = new Renderer[0];
            originalMaterials = new Material[bodyRenderers.Length][];
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                originalMaterials[i] = bodyRenderers[i] != null ? bodyRenderers[i].sharedMaterials : new Material[0];
            }
        }

        /// <summary>Where blood should spawn: the hit socket, or 1 m above the feet as a fallback.</summary>
        public Vector3 HitPosition => hitSocket != null ? hitSocket.position : transform.position + Vector3.up;

        /// <summary>
        /// Turn the white hit flash on or off by swapping to a shared flash material (PlaceholderSpec 3.7).
        /// </summary>
        public void SetFlash(bool on, Material flashMaterial)
        {
            if (on == isFlashing || (on && flashMaterial == null)) return;
            isFlashing = on;

            if (on && (flashMaterials == null || cachedFlashMaterial != flashMaterial))
            {
                // Build "all slots = flash material" arrays once and keep them.
                cachedFlashMaterial = flashMaterial;
                flashMaterials = new Material[bodyRenderers.Length][];
                for (int i = 0; i < bodyRenderers.Length; i++)
                {
                    var slots = new Material[originalMaterials[i].Length];
                    for (int s = 0; s < slots.Length; s++) slots[s] = flashMaterial;
                    flashMaterials[i] = slots;
                }
            }

            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] == null) continue;
                bodyRenderers[i].sharedMaterials = on ? flashMaterials[i] : originalMaterials[i];
            }
        }
    }
}
