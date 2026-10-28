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
    /// Controls: click a card, press 1/2/3 (or 4), gamepad: navigate + South button. Skip: S or the Skip button.
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
        private LevelUpService service;

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

            skipButton = UIFactory.CreateButton("Skip", bg.transform, "Skip (+3 Crowns, heal 5%)  [S]", 18, out _);
            UIFactory.Place((RectTransform)skipButton.transform, new Vector2(0.5f, 0.5f), new Vector2(360f, 50f), new Vector2(0f, -290f));
            skipButton.onClick.AddListener(() => service?.Skip());

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
            for (int i = 0; i < cards.Count; i++)
            {
                bool used = i < offers.Count;
                cards[i].gameObject.SetActive(used);
                if (used) cardTexts[i].text = $"[{i + 1}]  {offers[i].title}\n<size=16>{offers[i].description}</size>";
            }

            // Select the first card so a gamepad can navigate right away.
            if (EventSystem.current != null && offers.Count > 0) EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
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
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) service.Choose(0);
            else if (kb.digit2Key.wasPressedThisFrame) service.Choose(1);
            else if (kb.digit3Key.wasPressedThisFrame) service.Choose(2);
            else if (kb.digit4Key.wasPressedThisFrame) service.Choose(3);
            else if (kb.sKey.wasPressedThisFrame) service.Skip();
            else if (kb.rKey.wasPressedThisFrame) service.Reroll();
        }
    }
}
