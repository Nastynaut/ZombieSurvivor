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
        // true = "position only": the child follows the smoothed POSITION but never turns (world rotation
        // stays identity). Used for the CameraTarget, see SetPositionOnly.
        private bool[] positionOnly;
        private bool initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        // Lazy set-up, because other components (PlayerController.Awake) may add children before our Awake runs.
        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            if (smoothedChildren == null) smoothedChildren = new Transform[0];
            int n = smoothedChildren.Length;
            baseLocalPositions = new Vector3[n];
            baseLocalRotations = new Quaternion[n];
            lastWrittenRotations = new Quaternion[n];
            positionOnly = new bool[n];
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

        /// <summary>True if this child is being smoothed.</summary>
        public bool IsSmoothed(Transform child)
        {
            if (smoothedChildren == null || child == null) return false;
            for (int i = 0; i < smoothedChildren.Length; i++)
            {
                if (smoothedChildren[i] == child) return true;
            }
            return false;
        }

        /// <summary>
        /// Start smoothing one more DIRECT child (used at runtime so an old prefab can't leave the camera
        /// target unsmoothed). Call it while the child is at its normal resting pose.
        /// </summary>
        public void AddSmoothedChild(Transform child)
        {
            EnsureInitialized();
            if (child == null || child.parent != transform || IsSmoothed(child)) return;

            int n = smoothedChildren.Length;
            System.Array.Resize(ref smoothedChildren, n + 1);
            System.Array.Resize(ref baseLocalPositions, n + 1);
            System.Array.Resize(ref baseLocalRotations, n + 1);
            System.Array.Resize(ref lastWrittenRotations, n + 1);
            System.Array.Resize(ref positionOnly, n + 1);
            smoothedChildren[n] = child;
            baseLocalPositions[n] = child.localPosition;
            baseLocalRotations[n] = child.localRotation;
            lastWrittenRotations[n] = child.localRotation;
        }

        /// <summary>
        /// Make a smoothed child follow the smoothed POSITION only, with its world rotation kept at
        /// identity (it never turns when the knight turns). Used for the CameraTarget.
        ///
        /// WHY: the camera only needs a point to follow. If that point also turned with the knight, then
        /// anything that reads the target's rotation (a follow offset in target space, a group that
        /// averages member rotations, a slash parented to it) would swing when the knight turns 180°
        /// between W and S. A target that never rotates removes that whole class of problems.
        /// </summary>
        public void SetPositionOnly(Transform child)
        {
            EnsureInitialized();
            if (child == null) return;
            for (int i = 0; i < smoothedChildren.Length; i++)
            {
                if (smoothedChildren[i] != child) continue;
                if (!positionOnly[i])
                {
                    positionOnly[i] = true;
                    // Store its resting offset in WORLD axes (it won't be rotated by the knight's facing).
                    baseLocalPositions[i] = transform.rotation * baseLocalPositions[i];
                }
                return;
            }
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
            EnsureInitialized();
            float alpha = GameTick.InterpolationAlpha;
            Vector3 smoothedPosition = Vector3.Lerp(previousPosition, currentPosition, alpha);
            Quaternion smoothedRotation = Quaternion.Slerp(previousRotation, currentRotation, alpha);

            // HOW THE CHILDREN ARE WRITTEN (and why that cancels the parent's steps):
            // We decide the child's WORLD pose first (smoothed root pose * the child's normal offset), then
            // convert it into the real root's local space using the root's CURRENT pose. Unity then computes
            // world = root * local = exactly the smoothed pose. So the root's 50 Hz steps don't "leak" into
            // the child: whatever the root does, this frame's local offset undoes it.
            Quaternion inverseRoot = Quaternion.Inverse(transform.rotation);
            for (int i = 0; i < smoothedChildren.Length; i++)
            {
                var child = smoothedChildren[i];
                if (child == null) continue;

                if (positionOnly[i])
                {
                    // Position-only child (the CameraTarget): smoothed position + a fixed world offset,
                    // world rotation = identity. localRotation = inverse(root) makes world = identity.
                    child.localPosition = inverseRoot * (smoothedPosition + baseLocalPositions[i] - transform.position);
                    child.localRotation = inverseRoot;
                    lastWrittenRotations[i] = child.localRotation;
                    continue;
                }

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
