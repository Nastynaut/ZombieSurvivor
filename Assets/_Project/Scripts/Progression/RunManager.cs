using System;
using KnightSurvivors.Combat;
using KnightSurvivors.Core;
using KnightSurvivors.Data;
using KnightSurvivors.Player;
using KnightSurvivors.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KnightSurvivors.Progression
{
    /// <summary>
    /// The "referee" of one run:
    ///   * sets the 50 Hz gameplay tick and creates the RunState (timer, seed, shared XP),
    ///   * counts kills and Crowns (GDD 6.1: 0.05 per kill, 10 per minute survived, x greed),
    ///   * receives XP from gems (IExperienceReceiver) and triggers level-ups,
    ///   * ends the run when ALL players are dead, saves Crowns to disk, and restarts by reloading the scene.
    ///
    /// XP is SHARED by the whole party (one bar, one level), multiplied by the co-op xpMult.
    /// This is a decision for future co-op; with one player it behaves exactly like single player.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class RunManager : MonoBehaviour, IExperienceReceiver
    {
        public RunConfig runConfig;

        [Tooltip("0 = pick a random seed each run. Any other value replays the same spawns and rolls.")]
        public int fixedSeed;

        public RunState State { get; private set; }
        public SaveSystem Save { get; private set; }

        /// <summary>Crowns earned THIS run (fractional; shown rounded down).</summary>
        public float RunCrowns { get; private set; }

        /// <summary>Level-ups waiting to be shown (several can stack from one big gem).</summary>
        public int PendingLevelUps { get; private set; }

        /// <summary>Raised when one or more level-ups are waiting. The LevelUpService listens.</summary>
        public event Action LevelUpReady;

        /// <summary>Raised once when the run ends. The bool is true for a victory (not in the graybox yet).</summary>
        public event Action<bool> RunEnded;

        public event Action XpChanged;

        private int minutesPaid;
        private bool restarting;
        private const string GameOverPauseReason = "GameOver";

        private void Awake()
        {
            GameTick.Apply();
            PauseService.ClearAll();

            State = new RunState();
            State.Begin(fixedSeed != 0 ? fixedSeed : Environment.TickCount);
            State.xpToNextLevel = XpToNext(State.runLevel);

            Save = new SaveSystem();
            Save.Load();

            ServiceLocator.Register(this);
            ServiceLocator.Register(State);
            ServiceLocator.Register(Save);
            ServiceLocator.Register<IExperienceReceiver>(this);

            CombatEvents.AnyDied += OnAnythingDied;
            PlayerRegistry.PlayerDied += OnPlayerDied;
        }

        private void OnDestroy()
        {
            CombatEvents.AnyDied -= OnAnythingDied;
            PlayerRegistry.PlayerDied -= OnPlayerDied;
            ServiceLocator.Unregister(this);
            ServiceLocator.Unregister(State);
            ServiceLocator.Unregister(Save);
            ServiceLocator.Unregister<IExperienceReceiver>(this);
        }

        // FixedUpdate = the 50 Hz gameplay tick (GameTick). The run clock MUST live here, not in Update:
        // Update runs once per rendered frame, so at 144 fps vs 30 fps the clock would cross minute
        // boundaries on different ticks and the spawner (which also runs on the tick) would spawn
        // differently. Counting whole ticks makes a seeded run replay exactly the same on every PC.
        // It also pauses by itself: at timeScale 0 (level-up screen) Unity runs no FixedUpdates.
        // [DefaultExecutionOrder(-100)] on this class means the clock ticks BEFORE the spawner reads it.
        private void FixedUpdate()
        {
            if (State.isRunOver) return;

            State.elapsedTicks++;
            State.elapsedTime = State.elapsedTicks * GameTick.TickDelta;

            // 10 Crowns for every full minute survived.
            int minutes = Mathf.FloorToInt(State.elapsedTime / 60f);
            while (minutesPaid < minutes)
            {
                minutesPaid++;
                AddCrowns(10f * PartyGreed());
            }
        }

        // ------------------------------------------------------------------ XP and levels

        private int XpToNext(int level)
        {
            return runConfig != null && runConfig.xpCurve != null
                ? runConfig.xpCurve.XpToNext(level)
                : 5 + 6 * (level - 1) + Mathf.FloorToInt(0.1f * (level - 1) * (level - 1));
        }

        /// <summary>Called by PickupManager when a gem is collected (amount already includes the player's growth).</summary>
        public void AddExperience(int playerIndex, float amount)
        {
            if (State.isRunOver || amount <= 0f) return;

            float xpMult = 1f;
            if (runConfig != null && runConfig.spawnSchedule != null && runConfig.spawnSchedule.coopScaling != null)
            {
                xpMult = CoopScaling.Pick(runConfig.spawnSchedule.coopScaling.xpMult, PlayerRegistry.PlayerCount);
            }

            State.runXp += amount * xpMult;

            bool leveled = false;
            while (State.runXp >= State.xpToNextLevel)
            {
                State.runXp -= State.xpToNextLevel;
                State.runLevel++;
                State.xpToNextLevel = XpToNext(State.runLevel);
                PendingLevelUps++;
                leveled = true;
            }

            XpChanged?.Invoke();
            if (leveled) LevelUpReady?.Invoke();
        }

        /// <summary>The LevelUpService calls this after the player picked (or skipped) one card.</summary>
        public void ConsumeLevelUp()
        {
            if (PendingLevelUps > 0) PendingLevelUps--;
        }

        // ------------------------------------------------------------------ Crowns and kills

        public void AddCrowns(float amount)
        {
            if (amount > 0f) RunCrowns += amount;
        }

        /// <summary>Greed of the first living player (all players share one Crown purse).</summary>
        public float PartyGreed()
        {
            var players = PlayerRegistry.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && !players[i].IsDead) return players[i].Stats.greed;
            }

            return 1f;
        }

        private void OnAnythingDied(Health health, DamageInfo info, float healthBefore)
        {
            if (health == null || health.team != Team.Enemy || State.isRunOver) return;

            State.totalKills++;

            // Kill credit: the damage Source is the player's GameObject (weapons pass their owner).
            PlayerController killer = info.Source != null ? info.Source.GetComponentInParent<PlayerController>() : null;
            if (killer != null) killer.State.kills++;

            float greed = killer != null ? killer.Stats.greed : PartyGreed();
            AddCrowns(0.05f * greed);
        }

        // ------------------------------------------------------------------ game over and restart

        private void OnPlayerDied(PlayerController player)
        {
            if (!State.isRunOver && PlayerRegistry.AllDead) EndRun(false);
        }

        private void EndRun(bool victory)
        {
            State.isRunOver = true;
            PauseService.AddPause(GameOverPauseReason);

            // Bank the Crowns and stats in the meta save (JSON in persistentDataPath).
            var data = Save.Data;
            data.crowns += RunCrowns;
            data.runsPlayed++;
            data.totalKills += State.totalKills;
            data.bestTimeSeconds = Mathf.Max(data.bestTimeSeconds, State.elapsedTime);
            Save.Save();

            RunEnded?.Invoke(victory);
        }

        /// <summary>Start over: unpause and reload the current scene. Reloading resets everything cleanly.</summary>
        public void RestartRun()
        {
            // Guard: a click and a key press in the same frame must not load the scene twice.
            if (restarting) return;
            restarting = true;
            PauseService.ClearAll();
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }
    }
}
