using KnightSurvivors.Core;
using UnityEngine;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// Makes the player move smoothly on screen even though gameplay only moves the player 50 times
    /// a second (see GameTick).
    ///
    /// THE PROBLEM IT SOLVES
    /// The ROOT object (with the CharacterController and all logic) is moved in FixedUpdate, so it
    /// jumps from tick to tick: at 144 fps it stands still for 2-3 frames, then jumps 1/50 s of
    /// movement. Anything that looks at the root directly (the model, the ground ring, or THE CAMERA)
    /// stutters. That stutter is the fast "jitter" you see while just walking around.
    ///
    /// THE FIX
    /// Every rendered frame, this script places the listed children (Visual, GroundRing and the
    /// CameraTarget) part-way between the previous and the current tick, both position AND
    /// rotation. The camera follows the CameraTarget child, so it moves exactly as smoothly as the
    /// model. The gameplay logic never sees this: it's purely cosmetic.
    ///
    /// WHY Update (not LateUpdate)
    /// Unity runs every Update before any LateUpdate. Cinemachine (TargetGroup and Brain) moves the
    /// camera in LateUpdate, so smoothing in Update guarantees the camera always reads THIS frame's
    /// smoothed position, never last frame's. (If both ran in LateUpdate, the order between them
    /// would decide whether the camera lags one frame behind, which also looks like jitter.)
    /// </summary>
    [DefaultExecutionOrder(100)] // FixedUpdate: record AFTER the motor has moved this tick
    public class TickInterpolator : MonoBehaviour
    {
        [Tooltip("Children to smooth (Visual, GroundRing, CameraTarget). They must be direct children of this object.")]
        public Transform[] smoothedChildren;

        private Vector3 previousPosition;
        private Vector3 currentPosition;
        private Quaternion previousRotation;
        private Quaternion currentRotation;

        // Where each child sits relative to the root when there is no smoothing.
        private Vector3[] baseLocalPositions;
        private Quaternion[] baseLocalRotations;
        // The rotation we wrote last frame. If a child's rotation is different next frame, other code
        // changed it on purpose (e.g. the graybox death pose tips the knight over), so we adopt it.
        private Quaternion[] lastWrittenRotations;

        private void Awake()
        {
            if (smoothedChildren == null) smoothedChildren = new Transform[0];
            int n = smoothedChildren.Length;
            baseLocalPositions = new Vector3[n];
            baseLocalRotations = new Quaternion[n];
            lastWrittenRotations = new Quaternion[n];
            for (int i = 0; i < n; i++)
            {
                var child = smoothedChildren[i];
                if (child == null) continue;
                baseLocalPositions[i] = child.localPosition;
                baseLocalRotations[i] = child.localRotation;
                lastWrittenRotations[i] = child.localRotation;
            }

            Snap();
        }

        /// <summary>Forget history (call after teleporting), so nothing slides across the map.</summary>
        public void Snap()
        {
            previousPosition = currentPosition = transform.position;
            previousRotation = currentRotation = transform.rotation;
        }

        private void FixedUpdate()
        {
            previousPosition = currentPosition;
            previousRotation = currentRotation;
            currentPosition = transform.position;
            currentRotation = transform.rotation;
        }

        private void Update()
        {
            float alpha = GameTick.InterpolationAlpha;
            Vector3 smoothedPosition = Vector3.Lerp(previousPosition, currentPosition, alpha);
            Quaternion smoothedRotation = Quaternion.Slerp(previousRotation, currentRotation, alpha);

            // Child world pose we want = smoothed root pose * the child's normal offset.
            // Convert that back into the real root's local space, because children are positioned locally.
            Quaternion inverseRoot = Quaternion.Inverse(transform.rotation);
            for (int i = 0; i < smoothedChildren.Length; i++)
            {
                var child = smoothedChildren[i];
                if (child == null) continue;

                if (child.localRotation != lastWrittenRotations[i]) baseLocalRotations[i] = child.localRotation;

                Vector3 worldPosition = smoothedPosition + smoothedRotation * baseLocalPositions[i];
                child.localPosition = inverseRoot * (worldPosition - transform.position); // root scale is always 1
                Quaternion local = inverseRoot * smoothedRotation * baseLocalRotations[i];
                child.localRotation = local;
                lastWrittenRotations[i] = child.localRotation; // read back: Unity may normalise it slightly
            }
        }
    }
}
