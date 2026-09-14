using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TanamSawit.Managers;

namespace TanamSawit.UI
{
    /// <summary>
    /// Ticker notifikasi uGUI di bawah top bar. Menampilkan queue pesan (maks 5),
    /// hilang setelah durasi berlalu. Menggantikan DrawNotificationTicker() IMGUI.
    /// Berlangganan ke event ecology, loan, worker, economy, ending.
    /// </summary>
    [DefaultExecutionOrder(-21)]
    public class NotificationTickerUI : MonoBehaviour
    {
        public static NotificationTickerUI Instance { get; private set; }

        private struct NotifEntry
        {
            public string message;
            public float remaining;
        }

        private readonly List<NotifEntry> _queue = new List<NotifEntry>();
        private const int MaxVisible = 5;
        private const float DefaultDuration = 5f;

        private Text[] _labels;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
        }

        private void OnEnable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged += HandleMoney;
                EconomyManager.Instance.OnBankruptcy += HandleBankruptcy;
            }
            if (EnvironmentalKarmaManager.Instance != null)
            {
                EnvironmentalKarmaManager.Instance.OnEcologyEvent += HandleEcology;
                EnvironmentalKarmaManager.Instance.OnEndingTriggered += HandleEnding;
            }
            if (LoanManager.Instance != null)
                LoanManager.Instance.OnLoanEventTriggered += HandleLoan;
            if (WorkerManager.Instance != null)
                WorkerManager.Instance.OnWorkerEventTriggered += HandleLoan;
        }

        private void OnDisable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged -= HandleMoney;
                EconomyManager.Instance.OnBankruptcy -= HandleBankruptcy;
            }
            if (EnvironmentalKarmaManager.Instance != null)
            {
                EnvironmentalKarmaManager.Instance.OnEcologyEvent -= HandleEcology;
                EnvironmentalKarmaManager.Instance.OnEndingTriggered -= HandleEnding;
            }
            if (LoanManager.Instance != null)
                LoanManager.Instance.OnLoanEventTriggered -= HandleLoan;
            if (WorkerManager.Instance != null)
                WorkerManager.Instance.OnWorkerEventTriggered -= HandleLoan;
        }

        // ── Build UI ─────────────────────────────────────────────────────

        private void BuildUI()
        {
            // Panel di bawah top bar
            var panel = UIRoot.CreatePanel("TickerPanel", UIRoot.PanelRoot, UIRoot.BgDark);
            panel.anchorMin = new Vector2(0, 1);
            panel.anchorMax = new Vector2(1, 1);
            panel.pivot = new Vector2(0.5f, 1);
            panel.sizeDelta = new Vector2(0, 110);
            panel.anchoredPosition = new Vector2(0, -40);

            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 3, 3);
            vlg.spacing = 2;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            _labels = new Text[MaxVisible];
            for (int i = 0; i < MaxVisible; i++)
            {
                var rt = UIRoot.CreateRect($"Notif_{i}", panel);
                var txt = rt.gameObject.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 12;
                txt.color = UIRoot.TextYellow;
                txt.fontStyle = FontStyle.Italic;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.raycastTarget = false;
                _labels[i] = txt;
            }
        }

        // ── Public API ───────────────────────────────────────────────────

        public void Show(string message, float duration = DefaultDuration)
        {
            _queue.Insert(0, new NotifEntry { message = message, remaining = duration });
            if (_queue.Count > MaxVisible)
                _queue.RemoveAt(_queue.Count - 1);
            UpdateLabels();
        }

        // ── Event handlers ────────────────────────────────────────────────

        private void HandleMoney(double money, double delta)
        {
            if (System.Math.Abs(delta) < 100) return;
            string prefix = delta >= 0 ? "+" : "";
            Show($"\uD83D\uDCB5 {prefix}{EconomyManager.FormatCurrencyCompact(delta)} | Kas: {EconomyManager.FormatCurrencyCompact(money)}");
        }

        private void HandleBankruptcy()
        {
            Show("\u26A0\uFE0F BANGKRUT! Net Worth jatuh di bawah batas minimum!", 8f);
        }

        private void HandleEcology(KarmaLevel level, string msg) => Show($"\uD83C\uDF3E {msg}", 6f);
        private void HandleEnding(string msg) => Show($"\uD83C\uDFC1 {msg}", 10f);
        private void HandleLoan(string msg) => Show(msg);

        // ── Update ───────────────────────────────────────────────────────

        private void Update()
        {
            bool changed = false;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                _queue[i] = new NotifEntry
                {
                    message = _queue[i].message,
                    remaining = _queue[i].remaining - Time.deltaTime
                };
                if (_queue[i].remaining <= 0)
                {
                    _queue.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed) UpdateLabels();
        }

        private void UpdateLabels()
        {
            for (int i = 0; i < _labels.Length; i++)
            {
                _labels[i].text = i < _queue.Count ? _queue[i].message : "";
            }
        }
    }
}
