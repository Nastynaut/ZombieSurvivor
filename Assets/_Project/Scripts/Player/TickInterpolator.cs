using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// Makes the player's model move smoothly even though gameplay only moves the player 50 times
    /// a second (see GameTick).
    ///
    /// The ROOT (with the CharacterController and all logic) jumps from tick to tick. Every
    /// rendered frame, this script places the listed children (the Visual and the GroundRing)
    /// part-way between the previous and the current tick position. The logic never sees this:
    /// it's purely cosmetic.
    /// </summary>
    [DefaultExecutionOrder(100)] // record AFTER the motor has moved this tick
    public class TickInterpolator : MonoBehaviour
    {
        [Tooltip("Children to smooth (Visual, GroundRing). They must be direct children of this object.")]
        public Transform[] smoothedChildren;

        private Vector3 previousPosition;
        private Vector3 currentPosition;
        private Vector3[] baseLocalPositions;

        private void Awake()
        {
            if (smoothedChildren == null) smoothedChildren = new Transform[0];
            baseLocalPositions = new Vector3[smoothedChildren.Length];
            for (int i = 0; i < smoothedChildren.Length; i++)
            {
                if (smoothedChildren[i] != null) baseLocalPositions[i] = smoothedChildren[i].localPosition;
            }

            Snap();
        }

        /// <summary>Forget history (call after teleporting).</summary>
        public void Snap()
        {
            previousPosition = transform.position;
            currentPosition = transform.position;
        }

        private void FixedUpdate()
        {
            previousPosition = currentPosition;
            currentPosition = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 smoothed = Vector3.Lerp(previousPosition, currentPosition, GameTick.InterpolationAlpha);
            Vector3 offset = smoothed - transform.position;
            for (int i = 0; i < smoothedChildren.Length; i++)
            {
                var child = smoothedChildren[i];
                if (child == null) continue;
                // Root scale is always 1 (PlaceholderSpec 0), so a world offset can be rotated into local space directly.
                child.localPosition = baseLocalPositions[i] + Quaternion.Inverse(transform.rotation) * offset;
            }
        }
    }
}
