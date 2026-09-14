using UnityEngine;
using UnityEngine.UI;
using TanamSawit.Managers;

namespace TanamSawit.UI
{
    /// <summary>
    /// Banner Game Over uGUI dengan tombol "Mulai Ulang".
    /// Menggantikan DrawGameOverBanner() IMGUI.
    /// Terlihat saat GameManager.CurrentState == GameState.GameOver.
    /// </summary>
    [DefaultExecutionOrder(-18)]
    public class GameOverUI : MonoBehaviour
    {
        private GameObject _panel;
        private Text _resultText;

        private void Awake()
        {
            BuildUI();
            _panel.SetActive(false);
        }

        private void Update()
        {
            bool show = GameManager.Instance != null &&
                        GameManager.Instance.CurrentState == GameState.GameOver;
            if (show && !_panel.activeSelf)
            {
                _panel.SetActive(true);
                if (UIRoot.Instance != null) UIRoot.Instance.ShowModal(_panel);

                if (RivalManager.Instance != null && RivalManager.Instance.FinalEnding != GameEnding.BelumSelesai)
                    _resultText.text = $"Hasil: {RivalManager.Instance.FinalEnding}";
                else if (EnvironmentalKarmaManager.Instance != null &&
                         EnvironmentalKarmaManager.Instance.CurrentKarma == KarmaLevel.KiamatLongsor)
                    _resultText.text = "Terkubur Longsor & Banjir Bandang akibat 100% Lahan Monopoli!";
                else
                    _resultText.text = "Permainan Berakhir";
            }
            else if (!show && _panel.activeSelf)
            {
                _panel.SetActive(false);
            }
        }

        private void BuildUI()
        {
            _panel = new GameObject("GameOverModal");
            _panel.transform.SetParent(UIRoot.PanelRoot, false);

            var rt = _panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var backdrop = _panel.AddComponent<Image>();
            backdrop.color = new Color(0, 0, 0, 0.8f);

            var center = UIRoot.CreatePanel("Center", _panel.transform, UIRoot.BgPanel);
            center.anchorMin = new Vector2(0.5f, 0.5f);
            center.anchorMax = new Vector2(0.5f, 0.5f);
            center.pivot = new Vector2(0.5f, 0.5f);
            center.sizeDelta = new Vector2(520, 220);
            center.anchoredPosition = Vector2.zero;

            var title = UIRoot.CreateText("Title", center, "\u26A0\uFE0F PERMAINAN BERAKHIR \u26A0\uFE0F",
                20, UIRoot.TextRed, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -16), new Vector2(-16, -50));

            _resultText = UIRoot.CreateText("Result", center, "", 15, UIRoot.TextDefault, TextAnchor.MiddleCenter, FontStyle.Normal);
            SetRect(_resultText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -60), new Vector2(-16, -110));

            var restartBtn = UIRoot.CreateButton("RestartBtn", center, "\uD83D\uDD04 Mulai Ulang",
                UIRoot.BgButton, () => GameManager.Instance?.RestartCurrentScene(), 16);
            SetRect(restartBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-100, 16), new Vector2(100, 56));
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
