using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// PlaceholderSpec 2.2: gives each player their team colors (P1 azure, P2 orange, P3 teal, P4 white)
    /// by SWAPPING to shared material assets. No runtime material copies are made, so batching stays fast.
    /// </summary>
    public class KnightTint : MonoBehaviour
    {
        [Tooltip("M_PH_Team_Primary_P1..P4, in player order.")]
        public Material[] teamPrimaryMaterials = new Material[4];

        [Tooltip("M_PH_Team_Secondary_P1..P4, in player order.")]
        public Material[] teamSecondaryMaterials = new Material[4];

        [Tooltip("M_PH_Ring_P1..P4, in player order.")]
        public Material[] ringMaterials = new Material[4];

        [Tooltip("Ground ring disc + facing arrow renderers.")]
        public Renderer[] ringRenderers;

        public void Apply(int playerIndex)
        {
            var binder = GetComponent<VisualBinder>();
            if (binder != null)
            {
                SetAll(binder.teamPrimaryRenderers, Pick(teamPrimaryMaterials, playerIndex));
                SetAll(binder.teamSecondaryRenderers, Pick(teamSecondaryMaterials, playerIndex));
            }

            SetAll(ringRenderers, Pick(ringMaterials, playerIndex));
        }

        private static Material Pick(Material[] materials, int index)
        {
            if (materials == null || materials.Length == 0) return null;
            return materials[Mathf.Clamp(index, 0, materials.Length - 1)];
        }

        private static void SetAll(Renderer[] renderers, Material material)
        {
            if (renderers == null || material == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].sharedMaterial = material;
            }
        }
    }
}
