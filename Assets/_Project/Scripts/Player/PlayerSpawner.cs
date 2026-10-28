using KnightSurvivors.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KnightSurvivors.Player
{
    /// <summary>
    /// Creates the player(s) from the CharacterDefinition in the RunConfig.
    ///
    /// Single player (the graybox): spawns player 0 at the first spawn point on Start.
    ///
    /// Couch co-op hook (GDD 11): tick <see cref="useJoinHook"/> and add a PlayerInputManager to the
    /// same object. Each press on a new device (keyboard, gamepad 2, ...) then spawns one more knight
    /// bound to that device only ("one device per player"), and this script calls Setup on it.
    /// Online co-op later replaces this with NGO's spawn, calling the same Setup.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        public RunConfig runConfig;

        [Tooltip("Where players appear. Player N uses point N (wrapping). Empty = this object's position.")]
        public Transform[] spawnPoints;

        [Tooltip("Off = spawn one player on Start. On = wait for PlayerInputManager joins (couch co-op).")]
        public bool useJoinHook;

        [Tooltip("Max players (GDD 11: 4).")]
        [Range(1, 4)] public int maxPlayers = 4;

        private PlayerInputManager inputManager;

        private void Start()
        {
            var character = runConfig != null ? runConfig.defaultCharacter : null;
            if (character == null || character.prefab == null)
            {
                Debug.LogError("[PlayerSpawner] RunConfig.defaultCharacter (with a prefab) is missing. " +
                               "Run 'Knight Survivors > Build Graybox Scene'.");
                return;
            }

            if (useJoinHook && TryGetComponent(out inputManager))
            {
                inputManager.playerPrefab = character.prefab;
                inputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
                inputManager.onPlayerJoined += OnPlayerJoined;
                inputManager.EnableJoining();
                return;
            }

            // One player: a normal Instantiate. Its PlayerInput then accepts keyboard+mouse AND gamepad
            // (with a single PlayerInput, Unity auto-switches control schemes).
            var go = Instantiate(character.prefab, GetSpawnPosition(0), Quaternion.identity);
            var controller = go.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogError("[PlayerSpawner] The character prefab has no PlayerController.");
                return;
            }

            controller.Setup(character, 0, runConfig);
        }

        private void OnDestroy()
        {
            if (inputManager != null) inputManager.onPlayerJoined -= OnPlayerJoined;
        }

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            int index = playerInput.playerIndex;
            if (index >= maxPlayers)
            {
                Destroy(playerInput.gameObject);
                return;
            }

            var controller = playerInput.GetComponent<PlayerController>();
            if (controller == null) return;
            controller.Motor.Teleport(GetSpawnPosition(index));
            controller.Setup(runConfig.defaultCharacter, index, runConfig);
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return transform.position + new Vector3(index * 1.5f, 0f, 0f);
            var point = spawnPoints[index % spawnPoints.Length];
            return point != null ? point.position : transform.position;
        }
    }
}
