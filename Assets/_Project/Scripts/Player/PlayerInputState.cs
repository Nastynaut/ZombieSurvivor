using UnityEngine;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// A plain snapshot of "what this player is pressing right now".
    ///
    /// WHY A STRUCT INSTEAD OF READING THE KEYBOARD DIRECTLY?
    /// GDD 9.14: gameplay code never reads Keyboard.current or Gamepad.current. Only the
    /// PlayerInputHandler talks to the Input System and fills in this struct. Gameplay (movement,
    /// aim) only reads the struct. Benefits:
    ///   * Couch co-op: each player has their own handler bound to their own device.
    ///   * Online co-op later: a remote player's struct can be filled from network packets instead.
    ///   * Bots / replays / tests can fill the struct too.
    /// </summary>
    public struct PlayerInputState
    {
        /// <summary>Movement stick / WASD, each axis -1..1 (x = right, y = up-screen).</summary>
        public Vector2 Move;

        /// <summary>Right stick, -1..1, already zeroed inside the 0.25 deadzone.</summary>
        public Vector2 AimStick;

        /// <summary>Mouse cursor position in screen pixels (KB+M player only).</summary>
        public Vector2 PointerScreenPosition;

        /// <summary>True if this player uses a mouse/pointer at all.</summary>
        public bool HasPointer;

        /// <summary>Time.time when the mouse last moved (for assisted aim).</summary>
        public float PointerLastMovedTime;

        /// <summary>Dodge was pressed since gameplay last consumed it (dodge roll itself comes later).</summary>
        public bool DodgePressed;

        /// <summary>Pause was pressed since it was last consumed (pause menu comes later).</summary>
        public bool PausePressed;
    }

    /// <summary>Anything that can provide a player's input (the real handler, a network stub, a bot).</summary>
    public interface IPlayerInputSource
    {
        PlayerInputState State { get; }

        /// <summary>Mark one-shot presses (dodge, pause) as handled.</summary>
        void ConsumeButtons();
    }
}
