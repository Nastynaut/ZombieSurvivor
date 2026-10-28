using KnightSurvivors.Core;
using KnightSurvivors.Player;
using KnightSurvivors.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace KnightSurvivors.UI
{
    /// <summary>
    /// The in-run HUD: HP bar (player 1), shared XP bar + level, run timer, kill count and Crowns.
    /// Put it on a Canvas. It builds its own children in Awake, and reads values every frame
    /// (cheap: a few numbers). It never changes game state - it only displays it.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class HudController : MonoBehaviour
    {
        private Image hpFill;
        private Image xpFill;
        private Text levelText;
        private Text timerText;
        private Text killsText;
        private Text crownsText;
        private RunManager run;

        private void Awake()
        {
            var root = transform;

            // XP bar across the top (VS style).
            xpFill = UIFactory.CreateBar("XpBar", root, new Color(0f, 0f, 0f, 0.6f), new Color(0.3f, 0.6f, 1f));
            var xpRect = (RectTransform)xpFill.transform.parent;
            xpRect.anchorMin = new Vector2(0f, 1f);
            xpRect.anchorMax = new Vector2(1f, 1f);
            xpRect.pivot = new Vector2(0.5f, 1f);
            xpRect.sizeDelta = new Vector2(0f, 18f);
            xpRect.anchoredPosition = Vector2.zero;

            levelText = UIFactory.CreateText("Level", xpRect, "Lv 1", 16, TextAnchor.MiddleRight);
            UIFactory.Stretch(levelText.rectTransform);
            levelText.rectTransform.offsetMax = new Vector2(-8f, 0f);

            timerText = UIFactory.CreateText("Timer", root, "00:00", 36, TextAnchor.UpperCenter);
            UIFactory.Place(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(240f, 50f), new Vector2(0f, -24f));

            killsText = UIFactory.CreateText("Kills", root, "Kills 0", 22, TextAnchor.UpperRight);
            UIFactory.Place(killsText.rectTransform, new Vector2(1f, 1f), new Vector2(300f, 34f), new Vector2(-16f, -28f));

            crownsText = UIFactory.CreateText("Crowns", root, "Crowns 0", 22, TextAnchor.UpperRight);
            UIFactory.Place(crownsText.rectTransform, new Vector2(1f, 1f), new Vector2(300f, 34f), new Vector2(-16f, -60f));

            // HP bar bottom-left (HP red #E23A3A).
            hpFill = UIFactory.CreateBar("HpBar", root, new Color(0f, 0f, 0f, 0.6f), new Color32(0xE2, 0x3A, 0x3A, 0xFF));
            UIFactory.Place((RectTransform)hpFill.transform.parent, new Vector2(0f, 0f), new Vector2(320f, 22f), new Vector2(16f, 16f));
        }

        private void Start()
        {
            run = ServiceLocator.Get<RunManager>();
        }

        private void Update()
        {
            if (run == null || run.State == null) return;
            var state = run.State;

            xpFill.fillAmount = state.xpToNextLevel > 0 ? Mathf.Clamp01(state.runXp / state.xpToNextLevel) : 0f;
            levelText.text = $"Lv {state.runLevel}";
            timerText.text = UIFactory.FormatTime(state.elapsedTime);
            killsText.text = $"Kills {state.totalKills}";
            crownsText.text = $"Crowns {Mathf.FloorToInt(run.RunCrowns)}";

            var players = PlayerRegistry.Players;
            if (players.Count > 0 && players[0] != null && players[0].Health != null)
            {
                hpFill.fillAmount = players[0].Health.Normalized;
            }
        }
    }
}
