using KnightSurvivors.Core;
using KnightSurvivors.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KnightSurvivors.UI
{
    /// <summary>
    /// Shown when every player is dead. Shows time, kills, Crowns earned, and a Restart button
    /// (also R / Enter / gamepad South). Restart reloads the scene through RunManager.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class GameOverScreen : MonoBehaviour
    {
        private GameObject panel;
        private Text title;
        private Text stats;
        private Button restart;
        private RunManager run;

        private void Awake()
        {
            var bg = UIFactory.CreateImage("GameOverPanel", transform, new Color(0.1f, 0f, 0f, 0.8f));
            bg.raycastTarget = true;
            UIFactory.Stretch(bg.rectTransform);
            panel = bg.gameObject;

            title = UIFactory.CreateText("Title", bg.transform, "YOU DIED", 64, TextAnchor.MiddleCenter);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(800f, 90f), new Vector2(0f, 140f));

            stats = UIFactory.CreateText("Stats", bg.transform, "", 26, TextAnchor.MiddleCenter);
            UIFactory.Place(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(800f, 140f), new Vector2(0f, 10f));

            restart = UIFactory.CreateButton("Restart", bg.transform, "Restart  [R]", 26, out _);
            UIFactory.Place((RectTransform)restart.transform, new Vector2(0.5f, 0.5f), new Vector2(300f, 64f), new Vector2(0f, -130f));
            restart.onClick.AddListener(() => run?.RestartRun());

            panel.SetActive(false);
        }

        private void Start()
        {
            run = ServiceLocator.Get<RunManager>();
            if (run != null) run.RunEnded += Show;
        }

        private void OnDestroy()
        {
            if (run != null) run.RunEnded -= Show;
        }

        private void Show(bool victory)
        {
            title.text = victory ? "VICTORY" : "YOU DIED";
            var s = run.State;
            stats.text = $"Survived {UIFactory.FormatTime(s.elapsedTime)}   Level {s.runLevel}\n" +
                         $"Kills {s.totalKills}   Crowns +{Mathf.FloorToInt(run.RunCrowns)} (total {Mathf.FloorToInt(run.Save.Data.crowns)})";
            panel.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(restart.gameObject);
        }

        private void Update()
        {
            if (!panel.activeSelf || run == null) return;
            var kb = Keyboard.current;
            if (kb != null && (kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) run.RestartRun();
        }
    }
}
