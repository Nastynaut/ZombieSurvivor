using System.Text;
using KnightSurvivors.Player;
using UnityEngine;
using UnityEngine.UI;

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup).
    ///
    /// A text panel in the top-left corner of the Game view, VISIBLE BY DEFAULT (F3 hides/shows it), with
    /// numbers that tell us WHERE the jitter comes from. Every value is measured once per rendered frame,
    /// after the camera has moved.
    ///
    /// WHY A uGUI CANVAS (and not OnGUI like the a3146a4 version): OnGUI depends on the IMGUI skin and is
    /// easy to miss; a Screen Space Overlay canvas with the highest sorting order (32767) is drawn on top
    /// of everything else (HUD, level-up screen) and uses Unity's built-in font, so it can't be hidden by a
    /// missing font or a UI canvas in front of it.
    ///
    /// The first line is a BUILD STAMP. If you don't see the stamp below on screen, Unity is not running
    /// the newest scripts (see README "Unity didn't pick up the new scripts").
    ///
    /// HOW TO READ IT (walk in a straight line at full speed, first A/D, then W/S):
    ///   - Axes are the CAMERA's: x = screen left/right, y = screen up/down, z = into the screen.
    ///     Walking A/D moves things on x only. Walking W/S moves them on y AND z (the camera looks 50° down).
    ///   - Each line shows "min .. max" of the speed (m/s) on that axis over the last 0.5 s. Smooth motion
    ///     = min and max close together. Stepping motion = a wide range (e.g. "0.0 .. 9.5").
    ///   - Root (50 Hz): SHOULD step. It's the gameplay object, it only moves on ticks.
    ///   - CameraTarget: should be smooth on every axis. If it steps like the root, smoothing is broken.
    ///   - Camera: should be smooth. If CameraTarget is smooth but the camera isn't, the rig is the problem.
    ///   - Knight on screen px: how many pixels the knight moved on screen this frame, x and y separately.
    ///     Near 0 = the knight stays still on screen (good).
    ///   - Lookahead: how far ahead of the knight the camera aims (PlayerCameraGroup), and how fast that
    ///     lead changes. On a stop it should shrink to 0 smoothly (change speed ~1-2 m/s), never in one frame.
    ///   - Rig: the composer's damping / lookahead / distance as they really are at runtime.
    ///   - ticks/frame: how many 50 Hz FixedUpdates ran before each frame (1,1,1 at 50 fps; 0,1,0,1 at
    ///     100 fps; 1,1,1,1,0 at 60 fps). alpha: TickInterpolator's 0..1 blend value.
    /// </summary>
    [DefaultExecutionOrder(10000)] // LateUpdate after everything that moves the camera
    public class CameraJitterReadout : MonoBehaviour
    {
        /// <summary>Shown on screen and in the Console, so we can tell which version of the scripts Unity runs.</summary>
        public const string BuildStamp = "diag-2026-10-30c (smooth lookahead)";

        private const int History = 240;     // frames kept (enough for 0.5 s at 480 fps)
        private const float Window = 0.5f;   // seconds shown as min..max

        public bool visible = true;

        // Per-frame history. One "track" per number we show as min..max.
        private readonly float[] frameTimes = new float[History];
        private readonly float[][] tracks = new float[TrackCount][];
        private const int CamX = 0, CamY = 1, CamZ = 2, TgtX = 3, TgtY = 4, TgtZ = 5, RootSpeed = 6, RootY = 7,
            ScreenX = 8, ScreenY = 9, LookSize = 10, LookRate = 11, TrackCount = 12;
        private readonly int[] ticks = new int[History];
        private int head;
        private int filled;

        private float smoothedMs;
        private int ticksThisFrame;
        private bool hasLast;
        private Vector3 lastCamera, lastTarget, lastRoot, lastScreen, lastLook;
        private float lastAlpha;
        private int frameCounter;

        private readonly StringBuilder text = new StringBuilder(2048);
        private GameObject canvasObject;
        private Text label;

        /// <summary>Set by CameraRigSelfCheck / CameraDebugHost: a one-line summary of the camera wiring.</summary>
        public static string WiringStatus { get; set; } = "not checked yet";

        /// <summary>Set by CameraRigSelfCheck: the composer's damping, lookahead and distance at runtime.</summary>
        public static string RigStatus { get; set; } = "not checked yet";

        private void Awake()
        {
            for (int i = 0; i < TrackCount; i++) tracks[i] = new float[History];
            BuildCanvas();
            // A WARNING (yellow) on purpose: it still shows if the Console's grey "info" filter is switched off.
            Debug.LogWarning("[TEMP DEBUG] Camera jitter readout created (" + BuildStamp + ") on '" + name +
                             "', canvas active: " + (canvasObject != null && canvasObject.activeInHierarchy) +
                             ", label: " + (label != null) + ". Visible by default, F3 hides it.");
        }

        private void OnEnable()
        {
            if (canvasObject != null) canvasObject.SetActive(visible);
        }

        private void OnDisable()
        {
            if (canvasObject != null) canvasObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (canvasObject != null) Destroy(canvasObject);
        }

        // A full-screen overlay canvas with a dark box and white text in the top-left corner.
        private void BuildCanvas()
        {
            canvasObject = new GameObject("TEMP_JitterReadoutCanvas");
            canvasObject.transform.SetParent(transform, false); // dies with us, survives what we survive
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767; // the highest possible: drawn over the HUD and every menu

            // Dark background panel, anchored to the top-left corner.
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(8f, -8f);
            panelRect.sizeDelta = new Vector2(620f, 370f);
            var background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.78f);
            background.raycastTarget = false; // never blocks clicks on the game UI

            // The text, inset a little inside the panel.
            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(panel.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);
            label = textObject.AddComponent<Text>();
            // LegacyRuntime.ttf ships inside Unity itself (no asset needed), so the text can't go missing.
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = "TEMP camera readout " + BuildStamp + " (waiting for the player...)";

            canvasObject.SetActive(visible);
        }

        private void FixedUpdate()
        {
            ticksThisFrame++;
        }

        private void LateUpdate()
        {
            // F3 is read through DebugKeys, which works whatever the Active Input Handling setting is.
            if (DebugKeys.WasPressed(DebugKeys.Key.F3))
            {
                visible = !visible;
                if (canvasObject != null) canvasObject.SetActive(visible);
                Debug.LogWarning("[TEMP DEBUG] F3 readout: " + (visible ? "ON" : "OFF"));
            }

            float dt = Time.unscaledDeltaTime;
            smoothedMs = smoothedMs <= 0f ? dt * 1000f : Mathf.Lerp(smoothedMs, dt * 1000f, 0.05f);

            Measure(dt);

            // Rebuilding the text every frame would allocate garbage every frame; 6 times a second is plenty.
            frameCounter++;
            if (visible && label != null && (frameCounter % 10 == 0 || !hasLast)) label.text = BuildText();
        }

        private void Measure(float dt)
        {
            var cam = Camera.main;
            PlayerController player = FirstPlayer();
            if (cam == null || player == null || dt <= 0f)
            {
                hasLast = false;
                ticksThisFrame = 0;
                return;
            }

            Transform target = player.cameraTarget != null ? player.cameraTarget : player.transform;
            Transform camTransform = cam.transform;
            Vector3 camPos = camTransform.position;
            Vector3 targetPos = target.position;
            Vector3 rootPos = player.transform.position;
            Vector3 screen = cam.WorldToScreenPoint(targetPos);

            // The camera lookahead offset (zero if there's no PlayerCameraGroup in the scene).
            var cameraGroup = PlayerCameraGroup.Current;
            Vector3 look = cameraGroup != null ? cameraGroup.LookaheadOffset : Vector3.zero;

            var interp = player.GetComponent<TickInterpolator>();
            lastAlpha = interp != null ? interp.LastAlpha : -1f;

            if (hasLast)
            {
                head = (head + 1) % History;
                if (filled < History) filled++;
                frameTimes[head] = dt;

                // Movement this frame expressed in the CAMERA's axes (right, up, forward), in m/s.
                Vector3 camStep = camTransform.InverseTransformDirection(camPos - lastCamera) / dt;
                Vector3 tgtStep = camTransform.InverseTransformDirection(targetPos - lastTarget) / dt;
                tracks[CamX][head] = camStep.x;
                tracks[CamY][head] = camStep.y;
                tracks[CamZ][head] = camStep.z;
                tracks[TgtX][head] = tgtStep.x;
                tracks[TgtY][head] = tgtStep.y;
                tracks[TgtZ][head] = tgtStep.z;
                tracks[RootSpeed][head] = (rootPos - lastRoot).magnitude / dt;
                tracks[RootY][head] = rootPos.y;
                tracks[ScreenX][head] = screen.x - lastScreen.x; // pixels this frame
                tracks[ScreenY][head] = screen.y - lastScreen.y;
                tracks[LookSize][head] = look.magnitude;                // metres ahead of the knight
                tracks[LookRate][head] = (look - lastLook).magnitude / dt; // how fast the lead changes, m/s
                ticks[head] = ticksThisFrame;
            }

            lastCamera = camPos;
            lastTarget = targetPos;
            lastRoot = rootPos;
            lastScreen = screen;
            lastLook = look;
            hasLast = true;
            ticksThisFrame = 0;
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

        // Min and max of one track over the last Window seconds.
        private void Range(int track, out float min, out float max)
        {
            float[] values = tracks[track];
            min = float.MaxValue;
            max = float.MinValue;
            float time = 0f;
            for (int i = 0; i < filled && time < Window; i++)
            {
                int index = (head - i + History) % History;
                float v = values[index];
                if (v < min) min = v;
                if (v > max) max = v;
                time += frameTimes[index];
            }
            if (min == float.MaxValue) { min = 0f; max = 0f; }
        }

        private void AppendRange(int track, string format)
        {
            Range(track, out float min, out float max);
            text.Append(min.ToString(format)).Append("..").Append(max.ToString(format));
        }

        private void AppendAxes(string label, int x, int y, int z)
        {
            text.Append(label).Append("  x ");
            AppendRange(x, "0.0");
            text.Append("   y ");
            AppendRange(y, "0.0");
            text.Append("   z ");
            AppendRange(z, "0.0");
            text.Append('\n');
        }

        // "Lookahead offset 0.00..1.20 m (now 0.83 -> target 1.20)  change max 1.9 m/s  ease 0.40 out / 0.50 in"
        private void AppendLookahead()
        {
            var cameraGroup = PlayerCameraGroup.Current;
            if (cameraGroup == null)
            {
                text.Append("Lookahead: no PlayerCameraGroup in the scene\n");
                return;
            }
            text.Append("Lookahead offset ");
            AppendRange(LookSize, "0.00");
            text.Append(" m (now ").Append(cameraGroup.LookaheadOffset.magnitude.ToString("0.00"))
                .Append(" -> target ").Append(cameraGroup.LookaheadTarget.magnitude.ToString("0.00"))
                .Append(")  change max ");
            Range(LookRate, out float _, out float maxRate);
            text.Append(maxRate.ToString("0.0")).Append(" m/s  ease ")
                .Append(cameraGroup.lookaheadSmoothTime.ToString("0.00")).Append(" out / ")
                .Append(cameraGroup.lookaheadReleaseSmoothTime.ToString("0.00")).Append(" in")
                .Append(cameraGroup.lookaheadEnabled ? "" : "  (DISABLED)").Append('\n');
        }

        private string BuildText()
        {
            text.Clear();
            text.Append("TEMP camera readout  ").Append(BuildStamp).Append("   (F3 hide, F2 follow mode)\n");
            float fps = smoothedMs > 0f ? 1000f / smoothedMs : 0f;
            text.Append("FPS ").Append(fps.ToString("0")).Append("  frame ").Append(smoothedMs.ToString("0.0"))
                .Append(" ms  tick ").Append((Time.fixedDeltaTime * 1000f).ToString("0.0")).Append(" ms  alpha ")
                .Append(lastAlpha.ToString("0.00")).Append('\n');
            text.Append("ticks/frame: ");
            for (int i = Mathf.Min(filled, 24) - 1; i >= 0; i--) text.Append(ticks[(head - i + History) % History]);
            text.Append('\n');
            text.Append("Follow mode: ").Append(CameraFollowModeToggle.Current.ToString()).Append('\n');
            text.Append("Active Input Handling: ").Append(DebugKeys.ActiveInputHandling).Append('\n');
            text.Append("Speeds in camera axes, m/s, min..max over 0.5 s (x=screen L/R, y=screen up, z=depth)\n");
            AppendAxes("Camera      ", CamX, CamY, CamZ);
            AppendAxes("CameraTarget", TgtX, TgtY, TgtZ);
            text.Append("Root (50 Hz) speed ");
            AppendRange(RootSpeed, "0.0");
            text.Append(" m/s   root world Y ");
            AppendRange(RootY, "0.000");
            text.Append('\n');
            text.Append("Knight on screen px/frame  x ");
            AppendRange(ScreenX, "0.0");
            text.Append("   y ");
            AppendRange(ScreenY, "0.0");
            text.Append('\n');
            AppendLookahead();
            text.Append("Rig: ").Append(RigStatus).Append('\n');
            text.Append("Wiring: ").Append(WiringStatus);
            return text.ToString();
        }
    }
}
