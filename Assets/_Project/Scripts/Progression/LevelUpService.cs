using System;
using System.Collections.Generic;
using KnightSurvivors.Core;
using KnightSurvivors.Data;
using KnightSurvivors.Player;
using UnityEngine;

namespace KnightSurvivors.Progression
{
    /// <summary>What a level-up card offers.</summary>
    public enum OfferKind { Weapon, Passive, Fallback }

    /// <summary>One level-up card. Plain class so the UI can show it without knowing game rules.</summary>
    [Serializable]
    public class LevelUpOffer
    {
        public OfferKind kind;
        public WeaponDefinition weapon;
        public PassiveDefinition passive;
        public PickupType fallback;

        /// <summary>The level the item will have after picking this card (1 = new item).</summary>
        public int nextLevel;
        public bool isNew;
        public string title;
        public string description;
    }

    /// <summary>
    /// Level-up "pick 1 of 3" (GDD 4.3):
    ///   * When the RunManager says a level-up is waiting, the game PAUSES (Time.timeScale = 0 through
    ///     PauseService) and 3 cards are rolled.
    ///   * Cards: upgrades for owned weapons/passives below max level (weight 1.5), plus new items if a
    ///     slot of that type is free (weight 1.0). Banished items never show. No duplicates in one roll.
    ///   * Nothing left? Show the fallback cards (Gold Pouch +25 Crowns, Roast Chicken heal 30).
    ///   * Reroll (2 per run) redraws; Banish (1 per run) removes an item for the rest of the run;
    ///     Skip is unlimited and gives +3 Crowns and heals 5%.
    /// The graybox UI shows pick and skip; reroll and banish work in code (UI buttons deferred).
    ///
    /// Co-op note: XP is shared, so a level-up is offered to players in turn (round-robin).
    /// </summary>
    public class LevelUpService : MonoBehaviour
    {
        public RunConfig runConfig;

        public const int GoldPouchCrowns = 25;
        public const float RoastChickenHeal = 30f;

        public IReadOnlyList<LevelUpOffer> CurrentOffers => offers;
        public bool IsShowing { get; private set; }
        public PlayerController CurrentPlayer { get; private set; }

        /// <summary>Fired when new cards are ready to show.</summary>
        public event Action<IReadOnlyList<LevelUpOffer>> OffersShown;

        /// <summary>Fired when the screen should close.</summary>
        public event Action Closed;

        private readonly List<LevelUpOffer> offers = new List<LevelUpOffer>();
        private readonly HashSet<ScriptableObject> banished = new HashSet<ScriptableObject>();
        private readonly List<LevelUpOffer> candidates = new List<LevelUpOffer>();
        private readonly List<float> weights = new List<float>();
        private RunManager run;
        private RunState runState;
        private int nextPlayerTurn;
        private const string PauseReason = "LevelUp";

        private LevelUpRules Rules => runConfig != null ? runConfig.levelUpRules : null;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            run = ServiceLocator.Get<RunManager>();
            runState = ServiceLocator.Get<RunState>();
            if (run != null) run.LevelUpReady += OnLevelUpReady;
        }

        private void OnDestroy()
        {
            if (run != null) run.LevelUpReady -= OnLevelUpReady;
            ServiceLocator.Unregister(this);
            PauseService.RemovePause(PauseReason);
        }

        private void OnLevelUpReady()
        {
            if (!IsShowing) ShowNext();
        }

        private void ShowNext()
        {
            if (run == null || run.PendingLevelUps <= 0 || runState.isRunOver)
            {
                Close();
                return;
            }

            CurrentPlayer = PickPlayer();
            if (CurrentPlayer == null)
            {
                Close();
                return;
            }

            IsShowing = true;
            PauseService.AddPause(PauseReason);
            RollOffers();
        }

        private PlayerController PickPlayer()
        {
            var players = PlayerRegistry.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[(nextPlayerTurn + i) % players.Count];
                if (p != null && !p.IsDead)
                {
                    nextPlayerTurn = (nextPlayerTurn + i + 1) % Mathf.Max(1, players.Count);
                    return p;
                }
            }

            return null;
        }

        // ------------------------------------------------------------------ rolling cards

        private void RollOffers()
        {
            offers.Clear();
            BuildCandidates(CurrentPlayer);

            int count = Rules != null ? Rules.cardsShown : 3;
            var rng = runState.Random;

            while (offers.Count < count && candidates.Count > 0)
            {
                float total = 0f;
                for (int i = 0; i < weights.Count; i++) total += weights[i];
                float roll = rng.Value() * total;
                int pick = candidates.Count - 1;
                for (int i = 0; i < weights.Count; i++)
                {
                    roll -= weights[i];
                    if (roll <= 0f)
                    {
                        pick = i;
                        break;
                    }
                }

                offers.Add(candidates[pick]);
                candidates.RemoveAt(pick);
                weights.RemoveAt(pick);
            }

            if (offers.Count == 0) AddFallbackOffers();

            OffersShown?.Invoke(offers);
        }

        private void BuildCandidates(PlayerController player)
        {
            candidates.Clear();
            weights.Clear();
            float ownedWeight = Rules != null ? Rules.ownedWeight : 1.5f;
            float newWeight = Rules != null ? Rules.newWeight : 1f;
            var loadout = player.Loadout;

            if (runConfig != null)
            {
                foreach (var w in runConfig.weaponPool)
                {
                    if (w == null || w.isEvolution || banished.Contains(w)) continue;
                    var owned = loadout.GetWeapon(w);
                    if (owned != null)
                    {
                        if (owned.IsMaxLevel) continue;
                        AddCandidate(WeaponOffer(w, owned.Level + 1, false), ownedWeight);
                    }
                    else if (loadout.HasFreeWeaponSlot)
                    {
                        AddCandidate(WeaponOffer(w, 1, true), newWeight);
                    }
                }

                foreach (var p in runConfig.passivePool)
                {
                    if (p == null || banished.Contains(p)) continue;
                    var owned = loadout.GetPassive(p);
                    if (owned != null)
                    {
                        if (owned.level >= p.maxLevel) continue;
                        AddCandidate(PassiveOffer(p, owned.level + 1, false), ownedWeight);
                    }
                    else if (loadout.HasFreePassiveSlot)
                    {
                        AddCandidate(PassiveOffer(p, 1, true), newWeight);
                    }
                }
            }
        }

        private void AddCandidate(LevelUpOffer offer, float weight)
        {
            candidates.Add(offer);
            weights.Add(Mathf.Max(0.0001f, weight));
        }

        private static LevelUpOffer WeaponOffer(WeaponDefinition w, int level, bool isNew)
        {
            string desc = isNew ? "New weapon" : w.GetLevel(level).description;
            return new LevelUpOffer
            {
                kind = OfferKind.Weapon, weapon = w, nextLevel = level, isNew = isNew,
                title = isNew ? $"{w.displayName} (new)" : $"{w.displayName} Lv {level}",
                description = string.IsNullOrEmpty(desc) ? "Stronger attacks" : desc,
            };
        }

        private static LevelUpOffer PassiveOffer(PassiveDefinition p, int level, bool isNew)
        {
            return new LevelUpOffer
            {
                kind = OfferKind.Passive, passive = p, nextLevel = level, isNew = isNew,
                title = isNew ? $"{p.displayName} (new)" : $"{p.displayName} Lv {level}",
                description = p.DescribeLevel(),
            };
        }

        private void AddFallbackOffers()
        {
            var list = Rules != null && Rules.fallbackCards != null && Rules.fallbackCards.Count > 0
                ? Rules.fallbackCards
                : new List<PickupType> { PickupType.CoinPouch, PickupType.RoastChicken };

            foreach (var type in list)
            {
                offers.Add(new LevelUpOffer
                {
                    kind = OfferKind.Fallback, fallback = type,
                    title = type == PickupType.CoinPouch ? "Gold Pouch" : "Roast Chicken",
                    description = type == PickupType.CoinPouch ? $"+{GoldPouchCrowns} Crowns" : $"Heal {RoastChickenHeal:0} HP",
                });
            }
        }

        // ------------------------------------------------------------------ player choices (called by the UI)

        public void Choose(int index)
        {
            if (!IsShowing || index < 0 || index >= offers.Count) return;
            var offer = offers[index];
            var player = CurrentPlayer;

            switch (offer.kind)
            {
                case OfferKind.Weapon:
                    player.Loadout.AddWeapon(offer.weapon);
                    break;
                case OfferKind.Passive:
                    player.Loadout.AddPassive(offer.passive);
                    break;
                case OfferKind.Fallback:
                    if (offer.fallback == PickupType.CoinPouch) run.AddCrowns(GoldPouchCrowns * run.PartyGreed());
                    else player.Health.Heal(RoastChickenHeal);
                    break;
            }

            FinishOne();
        }

        public void Skip()
        {
            if (!IsShowing) return;
            int crowns = Rules != null ? Rules.skipCrowns : 3;
            float heal = Rules != null ? Rules.skipHealPct : 0.05f;
            run.AddCrowns(crowns * run.PartyGreed());
            CurrentPlayer.Health.Heal(CurrentPlayer.Health.maxHealth * heal);
            FinishOne();
        }

        public bool Reroll()
        {
            if (!IsShowing || CurrentPlayer.State.rerollsLeft <= 0) return false;
            CurrentPlayer.State.rerollsLeft--;
            RollOffers();
            return true;
        }

        public bool Banish(int index)
        {
            if (!IsShowing || index < 0 || index >= offers.Count || CurrentPlayer.State.banishesLeft <= 0) return false;
            var offer = offers[index];
            if (offer.kind == OfferKind.Fallback) return false;
            CurrentPlayer.State.banishesLeft--;
            banished.Add(offer.kind == OfferKind.Weapon ? (ScriptableObject)offer.weapon : offer.passive);
            RollOffers();
            return true;
        }

        private void FinishOne()
        {
            run.ConsumeLevelUp();
            if (run.PendingLevelUps > 0 && !runState.isRunOver)
            {
                CurrentPlayer = PickPlayer() ?? CurrentPlayer;
                RollOffers();
            }
            else
            {
                Close();
            }
        }

        private void Close()
        {
            bool wasShowing = IsShowing;
            IsShowing = false;
            PauseService.RemovePause(PauseReason);
            if (wasShowing) Closed?.Invoke();
        }
    }
}
