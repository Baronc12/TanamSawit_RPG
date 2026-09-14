using UnityEngine;
using UnityEngine.UI;
using TanamSawit.Managers;
using TanamSawit.SaveSystem;
using TanamSawit.Environment;
using TanamSawit.Buildings;

namespace TanamSawit.UI
{
    /// <summary>
    /// Tablet uGUI dengan 5 tab: Keuangan, Pekerja, Ekologi, Rival, Simpan/Muat.
    /// Toggle dengan Tab key. Menggantikan DrawPocketTablet() IMGUI.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class TabletUI : MonoBehaviour
    {
        public static TabletUI Instance { get; private set; }

        private GameObject _panel;
        private bool _isOpen = false;
        public bool IsOpen => _isOpen;

        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Keuangan", "Pekerja", "Ekologi", "Rival", "Simpan" };

        private Button[] _tabButtons;
        private RectTransform _contentArea;
        private Text _contentText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
            _panel.SetActive(false);
        }

        // ── Build UI ─────────────────────────────────────────────────────

        private void BuildUI()
        {
            _panel = new GameObject("TabletPanel");
            _panel.transform.SetParent(UIRoot.PanelRoot, false);

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1, 0.5f);
            panelRt.anchorMax = new Vector2(1, 0.5f);
            panelRt.pivot = new Vector2(1, 0.5f);
            panelRt.sizeDelta = new Vector2(560, 600);
            panelRt.anchoredPosition = new Vector2(-20, 0);

            var bg = _panel.AddComponent<Image>();
            bg.sprite = UIRoot.WhiteSprite;
            bg.color = UIRoot.BgPanel;
            bg.type = Image.Type.Sliced;

            // Header
            var header = UIRoot.CreateText("Header", _panel.transform, "\uD83D\uDCF1 STATUS RINGKASAN", 22, UIRoot.TextGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            AddTextOutline(header);
            SetRect(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -42), new Vector2(0, -10));

            // Tab buttons row
            var tabRow = UIRoot.CreateRect("TabRow", _panel.transform);
            SetRect(tabRow, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -82), new Vector2(-10, -48));
            var hlg = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;

            _tabButtons = new Button[_tabNames.Length];
            for (int i = 0; i < _tabNames.Length; i++)
            {
                int idx = i;
                _tabButtons[i] = UIRoot.CreateButton($"Tab_{_tabNames[i]}", tabRow, _tabNames[i],
                    UIRoot.BgButton, () => SelectTab(idx), 16);
                AddTextOutline(_tabButtons[i].GetComponentInChildren<Text>());
            }

            // Content area (scrollable text)
            _contentArea = UIRoot.CreateRect("ContentArea", _panel.transform);
            SetRect(_contentArea, Vector2.zero, Vector2.one, new Vector2(10, 58), new Vector2(-10, -90));

            var scrollBg = _contentArea.gameObject.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.3f);
            scrollBg.raycastTarget = true;

            _contentText = UIRoot.CreateText("Content", _contentArea, "", 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal);
            _contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _contentText.verticalOverflow = VerticalWrapMode.Overflow;
            AddTextOutline(_contentText);
            SetRect(_contentText.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 12), new Vector2(-14, -12));

            // Close button
            var closeBtn = UIRoot.CreateButton("CloseBtn", _panel.transform, "\u274C Tutup [Tab]",
                UIRoot.BgButtonDanger, () => Toggle(), 16);
            AddTextOutline(closeBtn.GetComponentInChildren<Text>());
            SetRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 10), new Vector2(-10, 50));

            SelectTab(0);
        }

        private void SelectTab(int index)
        {
            _selectedTab = index;
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                var colors = _tabButtons[i].colors;
                colors.normalColor = i == index ? UIRoot.BgButtonSuccess : UIRoot.BgButton;
                _tabButtons[i].colors = colors;
            }
            RefreshContent();
        }

        // ── Toggle ───────────────────────────────────────────────────────

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _panel.SetActive(_isOpen);
            if (_isOpen) RefreshContent();
        }

        // ── Content refresh ──────────────────────────────────────────────

        private void RefreshContent()
        {
            if (!_isOpen) return;
            switch (_selectedTab)
            {
                case 0: BindFinance(); break;
                case 1: BindWorkers(); break;
                case 2: BindEcology(); break;
                case 3: BindRival(); break;
                case 4: BindSaveLoad(); break;
            }
        }

        private void BindFinance()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>\uD83D\uDCB0 Keuangan</b>");
            if (EconomyManager.Instance != null)
            {
                sb.AppendLine($"  Kas Tunai : {EconomyManager.FormatCurrency(EconomyManager.Instance.CurrentMoney)}");
                sb.AppendLine($"  Net Worth : {EconomyManager.FormatCurrency(EconomyManager.Instance.GetNetWorth())}");
                sb.AppendLine($"  Lahan     : {EconomyManager.Instance.CurrentLandPercentage:F1}%");
            }
            if (LoanManager.Instance != null && LoanManager.Instance.TotalDebt > 0)
            {
                sb.AppendLine($"  Hutang Total : {EconomyManager.FormatCurrency(LoanManager.Instance.TotalDebt)}");
            }
            _contentText.text = sb.ToString();
        }

        private void BindWorkers()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>\uD83D\uDC77 Pekerja</b>");
            if (WorkerManager.Instance != null)
            {
                var wm = WorkerManager.Instance;
                sb.AppendLine($"  Pekerja Aktif : {wm.ActiveWorkersCount} orang");
                sb.AppendLine($"  Stok TBS : {wm.TbsStockTon:F1} Ton | CPO : {wm.CpoStockTon:F1} Ton");
                sb.AppendLine($"  Pabrik : {(wm.HasFactory ? "Beroperasi" : "Belum Dibangun")}");
                sb.AppendLine();
                foreach (var w in wm.Workers)
                {
                    string status = w.IsExhausted ? "Kelelahan!" : $"Stamina: {w.stamina:F0}%";
                    sb.AppendLine($"  \u2022 {w.workerName}  {status}  Exp:{w.experience}");
                }
            }
            _contentText.text = sb.ToString();
        }

        private void BindEcology()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>\uD83C\uDF3E Ekologi</b>");
            if (EnvironmentalKarmaManager.Instance != null)
            {
                sb.AppendLine($"  Karma : {EnvironmentalKarmaManager.Instance.CurrentKarma}");
                sb.AppendLine($"  Log  : {EnvironmentalKarmaManager.Instance.LastIncidentLog}");
            }
            if (EconomyManager.Instance != null)
                sb.AppendLine($"  Lahan : {EconomyManager.Instance.CurrentLandPercentage:F1}% / 100%");
            sb.AppendLine();
            sb.AppendLine("Peringatan:");
            sb.AppendLine("  75%: Invasi Monyet");
            sb.AppendLine("  85%: Serangan Gajah + Rusak Pabrik");
            sb.AppendLine("  90%: Teror Macan Tutul");
            sb.AppendLine("  100%: Kiamat Longsor (GameOver)");
            _contentText.text = sb.ToString();
        }

        private void BindRival()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>\uD83C\uDFAF Rival</b>");
            if (RivalManager.Instance != null && EconomyManager.Instance != null)
            {
                double playerNW = EconomyManager.Instance.GetNetWorth();
                double cousinNW = RivalManager.Instance.CousinCurrentNetWorth;
                sb.AppendLine($"  Net Worth Anda   : {EconomyManager.FormatCurrencyCompact(playerNW)}");
                sb.AppendLine($"  Net Worth Sepupu : {EconomyManager.FormatCurrencyCompact(cousinNW)}");
                sb.AppendLine($"  Status: {(playerNW >= cousinNW ? "Anda Memimpin!" : "Tertinggal")}");
                sb.AppendLine($"  Evaluasi: Tahun {RivalManager.Instance.TargetEvaluationYear}");
            }
            _contentText.text = sb.ToString();
        }

        private void BindSaveLoad()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>\uD83D\uDCBE Simpan / Muat</b>");
            if (SaveManager.Instance != null)
            {
                var summaries = SaveManager.Instance.GetSlotSummaries();
                for (int i = 0; i < summaries.Length; i++)
                {
                    var s = summaries[i];
                    sb.AppendLine();
                    if (s.Exists)
                    {
                        sb.AppendLine($"  Slot {s.SlotIndex}: {s.InGameDay}/{s.InGameMonth}/{s.InGameYear}");
                        sb.AppendLine($"    NW: {EconomyManager.FormatCurrencyCompact(s.NetWorth)}");
                    }
                    else
                    {
                        sb.AppendLine($"  Slot {s.SlotIndex}: (kosong)");
                    }
                }
            }
            _contentText.text = sb.ToString();
        }

        // ── Layout helpers ────────────────────────────────────────────────

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void AddTextOutline(Text text)
        {
            if (text == null) return;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        // ── Update (refresh saat terbuka) ─────────────────────────────────

        private void Update()
        {
            if (_isOpen) RefreshContent();
        }
    }
}
