using System.Collections.Generic;
using KnightSurvivors.Core;
using KnightSurvivors.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KnightSurvivors.UI
{
    /// <summary>
    /// "Pick 1 of 3" screen. It only SHOWS what LevelUpService offers and forwards clicks back to it.
    /// The game is already paused by the service (timeScale 0); UI and the Input System still run
    /// while paused because they use unscaled time.
    /// Controls: click a card, press 1/2/3 (or 4), gamepad: navigate + South button.
    /// Skip: X or the Skip button (disabled when skipsLeft is 0). Reroll: R.
    /// WHY X AND NOT S: S is "move down". A player who is holding or tapping S when the screen pops up
    /// would skip the level-up by accident. Every key here (1-4, X, R) is unused by gameplay
    /// (gameplay uses WASD, arrows, Space, Left Shift, Esc and Tab).
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class LevelUpScreen : MonoBehaviour
    {
        private const int MaxCards = 4;

        private GameObject panel;
        private Text header;
        private readonly List<Button> cards = new List<Button>();
        private readonly List<Text> cardTexts = new List<Text>();
        private Button skipButton;
        private Text skipButtonLabel;
        private LevelUpService service;

        // Keys are ignored for a moment after the screen opens, so a key that was already being
        // pressed for something else can't pick a card by accident. Real (unscaled) seconds,
        // because the game is paused (timeScale 0) while this screen is open.
        private const float InputGraceSeconds = 0.25f;
        private float acceptKeysAt;

        private void Awake()
        {
            var bg = UIFactory.CreateImage("LevelUpPanel", transform, new Color(0f, 0f, 0f, 0.75f));
            bg.raycastTarget = true;
            UIFactory.Stretch(bg.rectTransform);
            panel = bg.gameObject;

            header = UIFactory.CreateText("Header", bg.transform, "LEVEL UP!", 44, TextAnchor.MiddleCenter);
            UIFactory.Place(header.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, 60f), new Vector2(0f, 220f));

            for (int i = 0; i < MaxCards; i++)
            {
                int index = i; // copy for the lambda below (each button needs its own number)
                var button = UIFactory.CreateButton($"Card{i + 1}", bg.transform, "", 22, out var text);
                UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(560f, 90f), new Vector2(0f, 120f - i * 100f));
                button.onClick.AddListener(() => service?.Choose(index));
                cards.Add(button);
                cardTexts.Add(text);
            }

            skipButton = UIFactory.CreateButton("Skip", bg.transform, "Skip (+3 Crowns, heal 5%)  [X]", 18, out skipButtonLabel);
            UIFactory.Place((RectTransform)skipButton.transform, new Vector2(0.5f, 0.5f), new Vector2(360f, 50f), new Vector2(0f, -290f));
            skipButton.onClick.AddListener(() => service?.Skip());

            // Key hint line, so players don't have to guess the shortcuts.
            var hint = UIFactory.CreateText("KeyHint", bg.transform, "1-4 pick   |   X skip   |   R reroll", 16, TextAnchor.MiddleCenter);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, 30f), new Vector2(0f, -340f));

            panel.SetActive(false);
        }

        private void Start()
        {
            service = ServiceLocator.Get<LevelUpService>();
            if (service == null) return;
            service.OffersShown += Show;
            service.Closed += Hide;
        }

        private void OnDestroy()
        {
            if (service == null) return;
            service.OffersShown -= Show;
            service.Closed -= Hide;
        }

        private void Show(IReadOnlyList<LevelUpOffer> offers)
        {
            panel.SetActive(true);
            acceptKeysAt = Time.unscaledTime + InputGraceSeconds;
            for (int i = 0; i < cards.Count; i++)
            {
                bool used = i < offers.Count;
                cards[i].gameObject.SetActive(used);
                if (used) cardTexts[i].text = $"[{i + 1}]  {offers[i].title}\n<size=16>{offers[i].description}</size>";
            }

            RefreshSkipButton();

            // Select the first card so a gamepad can navigate right away.
            if (EventSystem.current != null && offers.Count > 0) EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
        }

        /// <summary>
        /// Updates the Skip button label and interactable state from the current player's skipsLeft.
        /// At 0 the button is greyed out (interactable = false) so the player cannot spend a skip they do not have.
        /// </summary>
        private void RefreshSkipButton()
        {
            if (skipButton == null || service == null) return;
            var player = service.CurrentPlayer;
            int left = player != null ? player.State.skipsLeft : 0;
            bool canSkip = left > 0;
            skipButton.interactable = canSkip;
            if (skipButtonLabel != null)
            {
                skipButtonLabel.text = canSkip
                    ? $"Skip (+3 Crowns, heal 5%)  [X]  ({left} left)"
                    : "Skip (0 left)";
            }
        }

        private void Hide()
        {
            panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void Update()
        {
            if (!panel.activeSelf || service == null) return;
            var kb = Keyboard.current;
            if (kb == null || Time.unscaledTime < acceptKeysAt) return;

            if (kb.digit1Key.wasPressedThisFrame) service.Choose(0);
            else if (kb.digit2Key.wasPressedThisFrame) service.Choose(1);
            else if (kb.digit3Key.wasPressedThisFrame) service.Choose(2);
            else if (kb.digit4Key.wasPressedThisFrame) service.Choose(3);
            // Skip() itself rejects when skipsLeft is 0; RefreshSkipButton greys the UI.
            else if (kb.xKey.wasPressedThisFrame) service.Skip();
            else if (kb.rKey.wasPressedThisFrame) service.Reroll();
        }
    }
}
