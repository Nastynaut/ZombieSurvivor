using System.Text;
using KnightSurvivors.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup).
    ///
    /// A small text box in the top-left corner (F3 hides/shows it) with numbers that tell us WHERE the
    /// jitter comes from. Every value is measured once per rendered frame, after the camera has moved.
    ///
    /// HOW TO READ IT (walking in a straight line at full speed):
    ///   - "speed" = distance moved this frame / frame time, in m/s. For something moving smoothly it is
    ///     nearly the same every frame (the knight's move speed). The min..max range over the last 0.5 s
    ///     is the important part: a narrow range = smooth, a range like "0 .. 15" = it moves in steps.
    ///   - Root: SHOULD step (0 on most frames, a jump on tick frames). That's normal: it is the 50 Hz
    ///     gameplay object. If it does NOT step, the game runs at or below 50 fps.
    ///   - CameraTarget: should be smooth. If it steps like the root, the smoothing is broken.
    ///   - Camera: should be smooth. If CameraTarget is smooth but the camera isn't, the camera rig is
    ///     the problem (compare F2 modes).
    ///   - "knight on screen": how much the knight moved relative to the camera this frame. Close to 0
    ///     while walking = the knight stays still on screen (good). Big jumpy values = visible jitter.
    ///   - "ticks/frame": how many 50 Hz FixedUpdates ran before each frame. A steady pattern (1,1,1 at
    ///     50 fps; 0,1,0,1 at 100 fps; 0,0,1 at 144 fps) is normal. Lots of 2s and 3s = frame hitches.
    ///   - alpha: TickInterpolator's 0..1 blend value. It should sweep 0 -> 1 between ticks.
    /// </summary>
    [DefaultExecutionOrder(10000)] // LateUpdate after everything that moves the camera
    public class CameraJitterReadout : MonoBehaviour
    {
        private const int History = 240;     // frames kept (enough for 0.5 s at 480 fps)
        private const float Window = 0.5f;   // seconds shown as min..max

        public bool visible = true;

        private readonly float[] frameTimes = new float[History];
        private readonly float[] cameraSpeeds = new float[History];
        private readonly float[] targetSpeeds = new float[History];
        private readonly float[] rootSpeeds = new float[History];
        private readonly float[] screenDeltas = new float[History];
        private readonly int[] ticks = new int[History];
        private int head;
        private int filled;

        private float smoothedMs;
        private int ticksThisFrame;
        private bool hasLast;
        private Vector3 lastCamera, lastTarget, lastRoot, lastOffset;
        private float lastAlpha;
        private string wiring = "";

        private readonly StringBuilder text = new StringBuilder(1024);
        private GUIStyle style;

        /// <summary>Set by CameraRigSelfCheck: a one-line summary of the camera wiring.</summary>
        public static string WiringStatus { get; set; } = "not checked yet";

        private void FixedUpdate()
        {
            ticksThisFrame++;
        }

        private void LateUpdate()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame) visible = !visible;

            float dt = Time.unscaledDeltaTime;
            smoothedMs = smoothedMs <= 0f ? dt * 1000f : Mathf.Lerp(smoothedMs, dt * 1000f, 0.05f);

            var cam = Camera.main;
            PlayerController player = FirstPlayer();
            if (cam == null || player == null || dt <= 0f)
            {
                hasLast = false;
                ticksThisFrame = 0;
                return;
            }

            Transform target = player.cameraTarget != null ? player.cameraTarget : player.transform;
            Vector3 camPos = cam.transform.position;
            Vector3 targetPos = target.position;
            Vector3 rootPos = player.transform.position;
            Vector3 offset = targetPos - camPos;

            var interp = player.GetComponent<TickInterpolator>();
            lastAlpha = interp != null ? interp.LastAlpha : -1f;

            if (hasLast)
            {
                head = (head + 1) % History;
                if (filled < History) filled++;
                frameTimes[head] = dt;
                cameraSpeeds[head] = (camPos - lastCamera).magnitude / dt;
                targetSpeeds[head] = (targetPos - lastTarget).magnitude / dt;
                rootSpeeds[head] = (rootPos - lastRoot).magnitude / dt;
                screenDeltas[head] = (offset - lastOffset).magnitude * 1000f; // millimetres per frame
                ticks[head] = ticksThisFrame;
            }

            lastCamera = camPos;
            lastTarget = targetPos;
            lastRoot = rootPos;
            lastOffset = offset;
            hasLast = true;
            ticksThisFrame = 0;
            wiring = WiringStatus;
        }

        private static PlayerController FirstPlayer()
        {
            var players = PlayerRegistry.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null) return players[i];
            }
            return null;
        }

        // Min, max and last value over the last Window seconds.
        private void Range(float[] values, out float min, out float max, out float last)
        {
            min = float.MaxValue;
            max = 0f;
            last = filled > 0 ? values[head] : 0f;
            float time = 0f;
            for (int i = 0; i < filled && time < Window; i++)
            {
                int index = (head - i + History) % History;
                float v = values[index];
                if (v < min) min = v;
                if (v > max) max = v;
                time += frameTimes[index];
            }
            if (min == float.MaxValue) min = 0f;
        }

        private void AppendSpeed(string label, float[] values)
        {
            Range(values, out float min, out float max, out float last);
            text.Append(label).Append(": ").Append(last.ToString("0.00"))
                .Append(" m/s   (0.5 s: ").Append(min.ToString("0.00")).Append(" .. ").Append(max.ToString("0.00"))
                .Append(")\n");
        }

        private void OnGUI()
        {
            if (!visible) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 14,
                    richText = false,
                    padding = new RectOffset(8, 8, 6, 6),
                };
                style.normal.textColor = Color.white;
            }

            text.Clear();
            text.Append("TEMP camera jitter readout (F3 hide, F2 switch follow mode)\n");
            float fps = smoothedMs > 0f ? 1000f / smoothedMs : 0f;
            text.Append("FPS ").Append(fps.ToString("0")).Append("   frame ").Append(smoothedMs.ToString("0.00"))
                .Append(" ms   tick ").Append((Time.fixedDeltaTime * 1000f).ToString("0.0")).Append(" ms\n");
            text.Append("Follow mode: ").Append(CameraFollowModeToggle.Current.ToString()).Append('\n');
            AppendSpeed("Camera      ", cameraSpeeds);
            AppendSpeed("CameraTarget", targetSpeeds);
            AppendSpeed("Root (50 Hz)", rootSpeeds);
            Range(screenDeltas, out float sMin, out float sMax, out float sLast);
            text.Append("Knight on screen: ").Append(sLast.ToString("0.0")).Append(" mm/frame   (0.5 s max ")
                .Append(sMax.ToString("0.0")).Append(")\n");
            text.Append("Tick alpha: ").Append(lastAlpha.ToString("0.00")).Append("   ticks/frame: ");
            for (int i = Mathf.Min(filled, 16) - 1; i >= 0; i--) text.Append(ticks[(head - i + History) % History]);
            text.Append('\n');
            text.Append("Wiring: ").Append(wiring);

            var content = new GUIContent(text.ToString());
            Vector2 size = style.CalcSize(content);
            GUI.Box(new Rect(10f, 10f, size.x, size.y), content, style);
        }
    }
}
