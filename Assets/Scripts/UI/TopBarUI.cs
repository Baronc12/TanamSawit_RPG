using UnityEngine;
using UnityEngine.UI;
using TanamSawit.Managers;
using TanamSawit.Environment;

namespace TanamSawit.UI
{
    /// <summary>
    /// Top bar uGUI: tanggal, nama area, kas, net worth, hutang, tombol kecepatan,
    /// tombol Tablet [Tab], tombol Settings. Menggantikan DrawTopBar() IMGUI.
    /// </summary>
    [DefaultExecutionOrder(-22)]
    public class TopBarUI : MonoBehaviour
    {
        public static TopBarUI Instance { get; private set; }

        private Text _dateAreaText;
        private Text _timeText;
        private Text _cashText;
        private Text _netWorthText;
        private Text _debtText;

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
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayPassed += HandleDay;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged += HandleMoney;
                EconomyManager.Instance.OnNetWorthChanged += HandleNetWorth;
            }
            if (AreaTransitionManager.Instance != null)
                AreaTransitionManager.Instance.OnAreaChanged += HandleArea;
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayPassed -= HandleDay;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged -= HandleMoney;
                EconomyManager.Instance.OnNetWorthChanged -= HandleNetWorth;
            }
            if (AreaTransitionManager.Instance != null)
                AreaTransitionManager.Instance.OnAreaChanged -= HandleArea;
        }

        private void Start()
        {
            // Refresh awal 2Hz untuk data yang tidak punya event
            InvokeRepeating(nameof(Refresh), 0f, 0.5f);
        }

        // ── Build UI ─────────────────────────────────────────────────────

        private void BuildUI()
        {
            // Root bar di bagian atas layar
            var bar = UIRoot.CreatePanel("TopBar", UIRoot.PanelRoot, UIRoot.BgDark);
            bar.anchorMin = new Vector2(0, 1);
            bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(0.5f, 1);
            bar.sizeDelta = new Vector2(0, 40);
            bar.anchoredPosition = Vector2.zero;

            var hlg = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 4, 4);
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Tanggal & Area
            _dateAreaText = CreateLabel("DateArea", bar, 280, 13);

            // Jam dalam sehari (siang/malam)
            _timeText = CreateLabel("Clock", bar, 70, 13);
            _timeText.color = UIRoot.TextGold;
            _timeText.alignment = TextAnchor.MiddleLeft;

            // Keuangan utama di kiri agar selalu terlihat saat pemain mengambil pinjaman
            _cashText = CreateLabel("Cash", bar, 160, 13);
            _cashText.color = UIRoot.TextGreen;

            _debtText = CreateLabel("Debt", bar, 170, 13);
            _debtText.color = UIRoot.TextRed;

            // Spacer
            UIRoot.CreateRect("Spacer1", bar);

            // Net Worth
            _netWorthText = CreateLabel("NetWorth", bar, 190, 13);

            // Spacer
            UIRoot.CreateRect("Spacer2", bar);

            // Tombol kecepatan
            CreateSpeedButton("Pause", "||", GameSpeed.Paused, bar, 36);
            CreateSpeedButton("1x", "1x", GameSpeed.Normal, bar, 40);
            CreateSpeedButton("2x", "2x", GameSpeed.Fast, bar, 40);
            CreateSpeedButton("4x", "4x", GameSpeed.SuperFast, bar, 40);

            // Spacer kecil
            var gap = UIRoot.CreateRect("Gap", bar);
            gap.sizeDelta = new Vector2(8, 0);

            // Tombol Tablet
            var tabletBtn = UIRoot.CreateButton("TabletBtn", bar, " Tablet [Tab]", UIRoot.BgButton, () =>
            {
                if (TabletUI.Instance != null) TabletUI.Instance.Toggle();
            });
            SetWidth(tabletBtn.GetComponent<RectTransform>(), 90);

            // Tombol Settings
            var settingsBtn = UIRoot.CreateButton("SettingsBtn", bar, " \u2699", UIRoot.BgButton, () =>
            {
                SettingsManager.Instance?.ToggleSettings();
            });
            SetWidth(settingsBtn.GetComponent<RectTransform>(), 40);
        }

        private static Text CreateLabel(string name, RectTransform parent, int width, int fontSize)
        {
            var rt = UIRoot.CreateRect(name, parent);
            rt.sizeDelta = new Vector2(width, 0);
            var txt = rt.gameObject.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = UIRoot.TextDefault;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.fontStyle = FontStyle.Bold;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        private static void SetWidth(RectTransform rt, float w)
        {
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.minWidth = w;
        }

        private void CreateSpeedButton(string name, string label, GameSpeed speed, RectTransform parent, float w)
        {
            var btn = UIRoot.CreateButton(name, parent, label, UIRoot.BgButton, () =>
            {
                if (TimeManager.Instance != null) TimeManager.Instance.SetSpeed(speed);
            });
            SetWidth(btn.GetComponent<RectTransform>(), w);
        }

        // ── Event handlers ────────────────────────────────────────────────

        private void HandleDay(int d, int m, int y) => Refresh();
        private void HandleMoney(double current, double delta) => Refresh();
        private void HandleNetWorth(double nw) => Refresh();
        private void HandleArea(string areaName) => Refresh();

        // ── Refresh ──────────────────────────────────────────────────────

        private void Refresh()
        {
            if (TimeManager.Instance != null)
            {
                string area = AreaTransitionManager.Instance != null
                    ? AreaTransitionManager.Instance.CurrentAreaName
                    : "Area Kebun Sawit";
                _dateAreaText.text = $"\uD83D\uDCC5 {TimeManager.Instance.GetFormattedDate()}  \uD83D\uDCCD {area}";

                // Jam + ikon siang/malam
                float h = TimeManager.Instance.HourOfDay;
                string icon = (h >= 6f && h < 18f) ? "\u2600" : "\u263D";
                _timeText.text = $"{icon} {TimeManager.Instance.GetFormattedTime()}";
            }

            if (EconomyManager.Instance != null)
            {
                _cashText.text = $"\uD83D\uDCB5 {EconomyManager.FormatCurrencyCompact(EconomyManager.Instance.CurrentMoney)}";

                double nw = EconomyManager.Instance.GetNetWorth();
                _netWorthText.color = nw >= 0 ? UIRoot.TextGreen : UIRoot.TextRed;
                _netWorthText.text = $"\uD83D\uDCC8 NW: {EconomyManager.FormatCurrencyCompact(nw)}";
            }

            if (LoanManager.Instance != null)
            {
                double debt = LoanManager.Instance.TotalDebt;
                _debtText.color = debt > 0 ? UIRoot.TextRed : UIRoot.TextDefault;
                _debtText.text = $"\uD83D\uDCB3 Hutang: {EconomyManager.FormatCurrencyCompact(debt)}";
            }
            else
            {
                _debtText.text = "\uD83D\uDCB3 Hutang: --";
            }
        }

        // ── Input ────────────────────────────────────────────────────────

        private void Update()
        {
            if (TanamSawit.Core.InputEdgeDetection.Tab())
            {
                if (UIRoot.IsModalOpen)
                    UIRoot.Instance.CloseModal();
                else if (TabletUI.Instance != null)
                    TabletUI.Instance.Toggle();
            }
            if (TanamSawit.Core.InputEdgeDetection.Escape())
            {
                if (UIRoot.IsModalOpen) UIRoot.Instance.CloseModal();
                else if (TabletUI.Instance != null && TabletUI.Instance.IsOpen) TabletUI.Instance.Toggle();
            }
        }
    }
}
