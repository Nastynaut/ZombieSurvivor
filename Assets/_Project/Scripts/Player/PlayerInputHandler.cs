using UnityEngine;
using UnityEngine.InputSystem;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// The ONLY script in the project that reads the Input System (GDD 9.14).
    ///
    /// It reads this player's actions from its own PlayerInput component (one PlayerInput per
    /// player = one device per player in couch co-op) and writes a <see cref="PlayerInputState"/>.
    ///
    /// Actions it expects in the "Gameplay" map of KnightSurvivors.inputactions
    /// (the one-click builder creates that file):
    ///   Move (Vector2), Aim (Vector2, right stick), Point (Vector2, mouse position),
    ///   Dodge (Button), Pause (Button).
    /// If no PlayerInput / actions are found, it builds the same bindings in code so the game
    /// is still playable with keyboard, mouse and gamepad.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputHandler : MonoBehaviour, IPlayerInputSource
    {
        [Tooltip("Right stick values below this are ignored (GDD 2.4: 0.25).")]
        [Range(0f, 0.9f)] public float aimDeadzone = 0.25f;

        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction aimAction;
        private InputAction pointAction;
        private InputAction dodgeAction;
        private InputAction pauseAction;

        // Fallback actions we created ourselves (and therefore must enable / dispose ourselves).
        private InputActionMap fallbackMap;

        private PlayerInputState state;
        private Vector2 lastPointer;

        public PlayerInputState State => state;

        /// <summary>This player's index in PlayerInputManager terms (0 for single player).</summary>
        public int PlayerIndex => playerInput != null ? playerInput.playerIndex : 0;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            var actions = playerInput != null ? playerInput.actions : null;

            if (actions != null)
            {
                // "Gameplay/Move" means action "Move" in map "Gameplay". false = don't throw if missing.
                moveAction = actions.FindAction("Gameplay/Move", false);
                aimAction = actions.FindAction("Gameplay/Aim", false);
                pointAction = actions.FindAction("Gameplay/Point", false);
                dodgeAction = actions.FindAction("Gameplay/Dodge", false);
                pauseAction = actions.FindAction("Gameplay/Pause", false);
            }

            if (moveAction == null)
            {
                Debug.LogWarning("[PlayerInputHandler] No 'Gameplay/Move' action found on the PlayerInput. " +
                                 "Using built-in default bindings. Run 'Knight Survivors > Build Graybox Scene' to create the input asset.");
                BuildFallbackActions();
            }
        }

        private void OnEnable()
        {
            fallbackMap?.Enable();
        }

        private void OnDisable()
        {
            fallbackMap?.Disable();
        }

        private void OnDestroy()
        {
            fallbackMap?.Dispose();
        }

        private void Update()
        {
            // Input is read every rendered frame (so no press is missed); gameplay reads the
            // result on its fixed 50 Hz tick. One-shot presses stay "true" until consumed.
            state.Move = moveAction != null ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;

            Vector2 stick = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;
            state.AimStick = stick.magnitude >= aimDeadzone ? stick : Vector2.zero;

            if (pointAction != null && pointAction.activeControl != null)
            {
                state.HasPointer = true;
                Vector2 pointer = pointAction.ReadValue<Vector2>();
                if ((pointer - lastPointer).sqrMagnitude > 4f) // moved more than 2 pixels
                {
                    state.PointerLastMovedTime = Time.time;
                    lastPointer = pointer;
                }

                state.PointerScreenPosition = pointer;
            }

            if (dodgeAction != null && dodgeAction.WasPressedThisFrame()) state.DodgePressed = true;
            if (pauseAction != null && pauseAction.WasPressedThisFrame()) state.PausePressed = true;
        }

        public void ConsumeButtons()
        {
            state.DodgePressed = false;
            state.PausePressed = false;
        }

        /// <summary>Same bindings as the .inputactions asset, built in code (safety net only).</summary>
        private void BuildFallbackActions()
        {
            fallbackMap = new InputActionMap("Gameplay");

            moveAction = fallbackMap.AddAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            aimAction = fallbackMap.AddAction("Aim", InputActionType.Value, "<Gamepad>/rightStick");
            pointAction = fallbackMap.AddAction("Point", InputActionType.Value, "<Pointer>/position");

            dodgeAction = fallbackMap.AddAction("Dodge", InputActionType.Button, "<Keyboard>/space");
            dodgeAction.AddBinding("<Keyboard>/leftShift");
            dodgeAction.AddBinding("<Gamepad>/buttonSouth");
            dodgeAction.AddBinding("<Gamepad>/rightShoulder");

            pauseAction = fallbackMap.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            pauseAction.AddBinding("<Gamepad>/start");
        }
    }
}
