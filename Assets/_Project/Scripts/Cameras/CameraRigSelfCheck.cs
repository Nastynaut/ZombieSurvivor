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

        private void Warn(string message)
        {
            Debug.LogWarning("[TEMP CameraRigSelfCheck] " + message, this);
        }
    }
}
