using UnityEngine;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// Lives on the knight's <c>Visual/ModelOffset</c> empty. Two rotations, applied in order:
    ///   1) <see cref="upAxisEulerDegrees"/> — stand a Z-up FBX up in Unity (Y-up), usually (-90, 0, 0)
    ///   2) <see cref="yawOffsetDegrees"/> — spin around world-up so the model's face points at Unity +Z
    ///
    /// SOURCE OF TRUTH: <c>CharacterDefinition.visualModelYawOffset</c> on the CHR_SirAldric asset.
    /// The graybox builder copies that value here. Tweak live on this component in Play mode if you
    /// want; the next "Build Graybox Scene" overwrites it from the ScriptableObject again.
    ///
    /// Where to edit for keeps: Project window → Assets/_Project/Data/Characters/CHR_SirAldric →
    /// Inspector → Visual Model Yaw Offset.
    /// </summary>
    public class ModelOffsetYaw : MonoBehaviour
    {
        [Tooltip(
            "Yaw in degrees around Y, applied AFTER the up-axis fix. " +
            "Sir Aldric (Tripo) default is 90: file-space front is -X, which becomes Unity +Z at 90°. " +
            "If the knight faces the wrong way by 180°, add or subtract 180 here (or on CHR_SirAldric).")]
        public float yawOffsetDegrees = 90f;

        [Tooltip("Euler applied first. (-90,0,0) turns a Z-up mesh into Y-up (head on +Y, feet on -Y before scale).")]
        public Vector3 upAxisEulerDegrees = new Vector3(-90f, 0f, 0f);

        private void Awake() => Apply();

        private void OnValidate() => Apply();

        /// <summary>Recompute localRotation from the two fields. Safe to call from the builder.</summary>
        public void Apply()
        {
            // Yaw is a parent-space spin around vertical AFTER the mesh has been stood up.
            transform.localRotation =
                Quaternion.Euler(0f, yawOffsetDegrees, 0f) * Quaternion.Euler(upAxisEulerDegrees);
        }
    }
}
