using System.Text;
using KnightSurvivors.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup).
    ///
    /// Checks twice a second that the camera rig is wired the way the jitter fix needs, REPAIRS what it
    /// can, and prints a warning for anything it changed (so a broken prefab or scene is visible instead
    /// of silently jittering). The result line is shown in the F3 readout ("Wiring: ...").
    ///
    /// What "correct" means:
    ///   1. The CinemachineBrain moves the camera in LateUpdate (not SmartUpdate/FixedUpdate).
    ///   2. The iso vcam follows the PlayerTargetGroup, and the group updates in LateUpdate.
    ///   3. The group's members are the players' smoothed CameraTarget children, never a player ROOT
    ///      (the root only moves at 50 Hz).
    ///   4. Every player's CameraTarget is a smoothed child of its TickInterpolator.
    ///   5. The PositionComposer damps all three camera axes the SAME amount. Dustin's recording of the
    ///      W/S-only jitter matched a scene where damping was (0.25, 0, 0.25): y = 0 lets every 50 Hz step
    ///      of the followed point straight through on the screen-vertical axis, while x = 0.25 hides them
    ///      on the screen-horizontal axis. So an old scene with y = 0 is repaired here and reported.
    /// </summary>
    public class CameraRigSelfCheck : MonoBehaviour
    {
        public CinemachineBrain brain;
        public CinemachineCamera virtualCamera;

        private float nextCheck;
        private readonly StringBuilder status = new StringBuilder(256);

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.5f;
            CheckAndRepair();
        }

        private void CheckAndRepair()
        {
            status.Clear();
            int problems = 0;

            if (brain != null)
            {
                if (brain.UpdateMethod != CinemachineBrain.UpdateMethods.LateUpdate)
                {
                    Warn($"CinemachineBrain.UpdateMethod was {brain.UpdateMethod}, set to LateUpdate.");
                    brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
                    problems++;
                }
                if (brain.BlendUpdateMethod != CinemachineBrain.BrainUpdateMethods.LateUpdate)
                {
                    Warn($"CinemachineBrain.BlendUpdateMethod was {brain.BlendUpdateMethod}, set to LateUpdate.");
                    brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
                    problems++;
                }
            }
            else
            {
                status.Append("no brain! ");
            }

            CinemachineTargetGroup group = null;
            if (virtualCamera != null && virtualCamera.Follow != null)
            {
                group = virtualCamera.Follow.GetComponent<CinemachineTargetGroup>();
                if (group == null)
                {
                    // Following something that isn't the group: if it's a player root, point at the group instead.
                    status.Append("vcam Follow is '").Append(virtualCamera.Follow.name).Append("', not a TargetGroup! ");
                    problems++;
                }
            }
            else
            {
                status.Append("vcam has no Follow! ");
                problems++;
            }

            if (group != null)
            {
                if (group.UpdateMethod != CinemachineTargetGroup.UpdateMethods.LateUpdate)
                {
                    Warn($"TargetGroup.UpdateMethod was {group.UpdateMethod}, set to LateUpdate.");
                    group.UpdateMethod = CinemachineTargetGroup.UpdateMethods.LateUpdate;
                    problems++;
                }

                // Remove player ROOTS (or anything that isn't a camera target) from the group.
                for (int i = group.Targets.Count - 1; i >= 0; i--)
                {
                    Transform member = group.Targets[i].Object;
                    if (member == null) continue;
                    var asPlayer = member.GetComponent<PlayerController>();
                    if (asPlayer != null)
                    {
                        Warn($"TargetGroup member '{member.name}' is a player ROOT (50 Hz). Replaced with its CameraTarget.");
                        group.Targets.RemoveAt(i);
                        problems++;
                    }
                }
            }

            CheckComposer(ref problems);

            int players = 0;
            var list = PlayerRegistry.Players;
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p == null) continue;
                players++;

                if (p.EnsureSmoothedCameraTarget()) problems++;

                if (group != null && group.FindMember(p.cameraTarget) < 0)
                {
                    Warn($"Player '{p.name}' CameraTarget was not in the TargetGroup. Added.");
                    group.AddMember(p.cameraTarget, p.IsDead ? 0f : 1f, 1.5f);
                    problems++;
                }
            }

            if (status.Length == 0 && problems == 0)
            {
                status.Append("OK (brain+group LateUpdate, vcam -> group -> ").Append(players)
                    .Append(" smoothed CameraTarget").Append(players == 1 ? "" : "s").Append(')');
            }
            else if (problems > 0)
            {
                status.Append("repaired ").Append(problems).Append(" problem(s), see Console");
            }

            CameraJitterReadout.WiringStatus = status.ToString();
        }

        // Point 5 above, plus the "Rig:" line of the readout (what the composer REALLY uses at runtime).
        private void CheckComposer(ref int problems)
        {
            var composer = virtualCamera != null ? virtualCamera.GetComponent<CinemachinePositionComposer>() : null;
            if (composer == null)
            {
                CameraJitterReadout.RigStatus = "no PositionComposer on the vcam";
                return;
            }

            Vector3 damping = composer.Damping;
            float wanted = Mathf.Max(damping.x, damping.y, damping.z);
            if (!Mathf.Approximately(damping.x, damping.y) || !Mathf.Approximately(damping.y, damping.z))
            {
                Warn($"PositionComposer damping was {damping} (different per axis = W/S and A/D follow differently, " +
                     $"y = 0 shows 50 Hz steps on W/S). Set to {wanted} on all axes. Rebuild the scene to make it permanent.");
                composer.Damping = new Vector3(wanted, wanted, wanted);
                damping = composer.Damping;
                problems++;
            }

            var look = composer.Lookahead;
            CameraJitterReadout.RigStatus =
                $"damping {damping.x:0.00}/{damping.y:0.00}/{damping.z:0.00}  lookahead " +
                (look.Enabled ? $"ON {look.Time:0.00}s smooth {look.Smoothing:0.0}{(look.IgnoreY ? " ignoreY" : "")}" : "OFF") +
                $"  distance {composer.CameraDistance:0.0}";
        }

        private void Warn(string message)
        {
            Debug.LogWarning("[TEMP CameraRigSelfCheck] " + message, this);
        }
    }
}
