using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KnightSurvivors.Cameras
{
    /// <summary>
    /// TEMPORARY (camera jitter diagnostics, remove in the PR #1 cleanup).
    ///
    /// Reads the developer keys F1 / F2 / F3 in a way that works with EVERY "Active Input Handling"
    /// setting (Edit > Project Settings > Player > Other Settings > Configuration):
    ///
    ///   "Input System Package (New)"  -> Unity defines ENABLE_INPUT_SYSTEM            -> we use Keyboard.current
    ///   "Input Manager (Old)"         -> Unity defines ENABLE_LEGACY_INPUT_MANAGER    -> we use UnityEngine.Input
    ///   "Both"                        -> Unity defines BOTH                           -> we try the new one first
    ///
    /// Why bother: with "Input Manager (Old)" the Input System package still compiles, but it gets no
    /// keyboard at all (Keyboard.current is null, InputActions never fire). With "Input System Package
    /// (New)" the old UnityEngine.Input class THROWS an exception when you call it. The #if lines below
    /// make sure we only ever call the one that is switched on, so this helper can never throw.
    ///
    /// Gameplay input (PlayerInput / PlayerInputHandler) is NOT changed: it stays on the Input System.
    /// </summary>
    public static class DebugKeys
    {
        public enum Key { F1, F2, F3 }

        /// <summary>True on the frame the key went down. Never throws; false if no keyboard is available.</summary>
        public static bool WasPressed(Key key)
        {
#if ENABLE_INPUT_SYSTEM
            // New Input System. Keyboard.current is null if no keyboard is connected.
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                switch (key)
                {
                    case Key.F1: return keyboard.f1Key.wasPressedThisFrame;
                    case Key.F2: return keyboard.f2Key.wasPressedThisFrame;
                    case Key.F3: return keyboard.f3Key.wasPressedThisFrame;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            // Old Input Manager (only compiled in when it's switched on, so this can't throw).
            switch (key)
            {
                case Key.F1: return Input.GetKeyDown(KeyCode.F1);
                case Key.F2: return Input.GetKeyDown(KeyCode.F2);
                case Key.F3: return Input.GetKeyDown(KeyCode.F3);
            }
#endif
            return false;
        }

        /// <summary>
        /// A short description of the project's Active Input Handling, worked out from the defines Unity
        /// sets for it. Shown in the Console at startup and on the readout.
        /// </summary>
        public static string ActiveInputHandling
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                return "Both (game input OK)";
#elif ENABLE_INPUT_SYSTEM
                return "Input System Package (New) (game input OK)";
#elif ENABLE_LEGACY_INPUT_MANAGER
                return "Input Manager (Old) ONLY - the knight can't be controlled! Fix: " + FixHint;
#else
                return "unknown (no input define set)";
#endif
            }
        }

        /// <summary>True when the Input System (which ALL gameplay input uses) is switched on.</summary>
        public static bool InputSystemEnabled
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>The exact clicks to switch the Input System on.</summary>
        public const string FixHint =
            "Edit > Project Settings > Player > Other Settings > Configuration > Active Input Handling = " +
            "\"Input System Package (New)\", click Apply, and let the Editor restart.";
    }
}
