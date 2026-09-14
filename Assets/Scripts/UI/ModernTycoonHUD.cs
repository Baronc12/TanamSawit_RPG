using System;
using System.Collections;
using UnityEngine;
using TanamSawit.Managers;
using TanamSawit.SaveSystem;
using TanamSawit.Environment;
using TanamSawit.Player;
using TanamSawit.Buildings;

namespace TanamSawit.UI
{
    /// <summary>
    /// HUD Modern Tanam Sawit — pengganti TycoonHUD IMGUI debug lama.
    ///
    /// Fitur:
    ///   - Top Bar minimalis: Kas, Net Worth, Kalender, Kecepatan Waktu, Nama Area aktif.
    ///   - Banner Notifikasi: Ticker pesan event bergeser dari kanan ke kiri.
    ///   - Pocket Tablet [Tab]: Ringkasan global status, Save, dan perbandingan rival.
    ///   - Modal Interaksi Fasilitas: Dibuka lewat event InteractableFacility.OnFacilityInteracted.
    ///   - Tombol [Esc] / klik di luar modal: Menutup modal dan membuka kembali pergerakan MC.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class ModernTycoonHUD : MonoBehaviour
    {
        public static ModernTycoonHUD Instance { get; private set; }

        // ── Strangler flags: set to false when uGUI panel replaces each section ──
        private const bool SHOW_TOPBAR = false;       // replaced by TopBarUI
        private const bool SHOW_TICKER = false;       // replaced by NotificationTickerUI
        private const bool SHOW_TABLET = false;        // replaced by TabletUI
        private const bool SHOW_FACILITY_MODAL = false; // replaced by FacilityModalUI
        private const bool SHOW_GAME_OVER = false;     // replaced by GameOverUI

        // ── Styles ────────────────────────────────────────────────────────
        private GUIStyle topBarStyle;
        private GUIStyle topBarLabelStyle;
        private GUIStyle notifStyle;
        private GUIStyle tabletPanelStyle;
        private GUIStyle tabletHeaderStyle;
        private GUIStyle modalPanelStyle;
        private GUIStyle modalHeaderStyle;
        private GUIStyle btnStyle;
        private GUIStyle btnDangerStyle;
        private GUIStyle btnSuccessStyle;
        private bool stylesReady = false;
        private Texture2D bgTex;
        private Texture2D modalBgTex;
        private Texture2D btnTex;
        private Texture2D btnDangerTex;
        private Texture2D btnSuccessTex;

        // ── State ─────────────────────────────────────────────────────────
        private bool tabletOpen = false;
        private bool facilityModalOpen = false;
        private FacilityType currentFacilityType;
        private string currentFacilityName = "";

        // Ticker notifikasi
        private string currentNotification = "";
        private float notifTimer = 0f;
#pragma warning disable CS0414
        private float notifDuration = 5f;
        private float tickerX = 0f;
#pragma warning restore CS0414

        // Shortcut ke manager instances
        private EconomyManager econ => EconomyManager.Instance;
        private TimeManager time => TimeManager.Instance;
        private LoanManager loan => LoanManager.Instance;
        private WorkerManager workers => WorkerManager.Instance;
        private RivalManager rival => RivalManager.Instance;
        private EnvironmentalKarmaManager karma => EnvironmentalKarmaManager.Instance;
        private SaveManager save => SaveManager.Instance;
        private AreaTransitionManager transition => AreaTransitionManager.Instance;

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildTextures();
        }

        private void OnEnable()
        {
            InteractableFacility.OnFacilityInteracted += HandleFacilityInteracted;
            if (econ != null)
            {
                econ.OnMoneyChanged += HandleMoneyChanged;
                econ.OnBankruptcy += HandleBankruptcy;
            }
            if (EnvironmentalKarmaManager.Instance != null)
            {
                EnvironmentalKarmaManager.Instance.OnEcologyEvent += HandleEcologyEvent;
                EnvironmentalKarmaManager.Instance.OnEndingTriggered += HandleEndingEvent;
            }
            if (loan != null)
                loan.OnLoanEventTriggered += HandleLoanEvent;
            if (workers != null)
                workers.OnWorkerEventTriggered += HandleLoanEvent;
        }

        private void OnDisable()
        {
            InteractableFacility.OnFacilityInteracted -= HandleFacilityInteracted;
            if (econ != null)
            {
                econ.OnMoneyChanged -= HandleMoneyChanged;
                econ.OnBankruptcy -= HandleBankruptcy;
            }
            if (EnvironmentalKarmaManager.Instance != null)
            {
                EnvironmentalKarmaManager.Instance.OnEcologyEvent -= HandleEcologyEvent;
                EnvironmentalKarmaManager.Instance.OnEndingTriggered -= HandleEndingEvent;
            }
            if (loan != null)
                loan.OnLoanEventTriggered -= HandleLoanEvent;
            if (workers != null)
                workers.OnWorkerEventTriggered -= HandleLoanEvent;
        }

        private void Update()
        {
            // [Tab] toggle Pocket Tablet
            if (IsTabPressed())
            {
                if (facilityModalOpen)
                    CloseModal();
                else
                    tabletOpen = !tabletOpen;
            }

            // [Esc] tutup modal atau tablet
            if (IsEscPressed())
            {
                if (facilityModalOpen) CloseModal();
                else if (tabletOpen) tabletOpen = false;
            }

            // Ticker
            if (!string.IsNullOrEmpty(currentNotification))
            {
                notifTimer -= Time.deltaTime;
                if (notifTimer <= 0f) currentNotification = "";
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────

        private void HandleFacilityInteracted(FacilityType type, string name)
        {
            currentFacilityType = type;
            currentFacilityName = name;
            facilityModalOpen = true;
            tabletOpen = false;
        }

        private void HandleMoneyChanged(double money, double delta)
        {
            if (Math.Abs(delta) < 100) return;
            string prefix = delta >= 0 ? "+" : "";
            ShowNotification($"💵 {prefix}{EconomyManager.FormatCurrencyCompact(delta)} | Kas: {EconomyManager.FormatCurrencyCompact(money)}");
        }

        private void HandleBankruptcy()
        {
            ShowNotification("⚠️ BANGKRUT! Net Worth jatuh di bawah batas minimum!", 8f);
        }

        private void HandleEcologyEvent(KarmaLevel level, string msg) => ShowNotification($"🌿 {msg}", 6f);
        private void HandleEndingEvent(string msg) => ShowNotification($"🏁 {msg}", 10f);
        private void HandleLoanEvent(string msg) => ShowNotification(msg);

        public void ShowNotification(string msg, float duration = 5f)
        {
            currentNotification = msg;
            notifTimer = duration;
        }

        private void CloseModal()
        {
            facilityModalOpen = false;
            // Buka kembali pergerakan MC
#pragma warning disable CS0618
            var controller = FindFirstObjectByType<OpeningGameplayPlayerController>();
#pragma warning restore CS0618
            if (controller != null) controller.SetMovementLocked(false);
        }

        // ── Textures ──────────────────────────────────────────────────────

        private void BuildTextures()
        {
            bgTex = MakeTex(new Color(0f, 0f, 0f, 0.80f));
            modalBgTex = MakeTex(new Color(0.07f, 0.10f, 0.08f, 0.94f));
            btnTex = MakeTex(new Color(0.2f, 0.4f, 0.25f, 0.95f));
            btnDangerTex = MakeTex(new Color(0.5f, 0.1f, 0.1f, 0.95f));
            btnSuccessTex = MakeTex(new Color(0.1f, 0.4f, 0.15f, 0.95f));
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(2, 2);
            t.SetPixels(new Color[] { c, c, c, c });
            t.Apply();
            return t;
        }

        // ── Style Init ────────────────────────────────────────────────────

        private void InitStyles()
        {
            if (stylesReady) return;

            topBarStyle = new GUIStyle(GUI.skin.box);
            topBarStyle.normal.background = bgTex;

            topBarLabelStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 13, fontStyle = FontStyle.Bold, wordWrap = false };
            topBarLabelStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f);

            notifStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 12, fontStyle = FontStyle.Italic, wordWrap = false };
            notifStyle.normal.textColor = new Color(1f, 0.92f, 0.45f);

            tabletPanelStyle = new GUIStyle(GUI.skin.box);
            tabletPanelStyle.normal.background = modalBgTex;

            tabletHeaderStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            tabletHeaderStyle.normal.textColor = new Color(0.4f, 1f, 0.55f);

            modalPanelStyle = new GUIStyle(GUI.skin.box);
            modalPanelStyle.normal.background = modalBgTex;

            modalHeaderStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            modalHeaderStyle.normal.textColor = new Color(1f, 0.88f, 0.3f);

            btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold };
            btnStyle.normal.background = btnTex;
            btnStyle.normal.textColor = Color.white;
            btnStyle.hover.background = btnSuccessTex;
            btnStyle.hover.textColor = Color.white;

            btnDangerStyle = new GUIStyle(btnStyle);
            btnDangerStyle.normal.background = btnDangerTex;

            btnSuccessStyle = new GUIStyle(btnStyle);
            btnSuccessStyle.normal.background = btnSuccessTex;

            stylesReady = true;
        }

        // ── OnGUI ─────────────────────────────────────────────────────────

        private void OnGUI()
        {
            // Skip saat transisi area
            if (transition != null && transition.IsTransitioning) return;

            InitStyles();

            if (SHOW_TOPBAR) DrawTopBar();
            if (SHOW_TICKER) DrawNotificationTicker();

            if (SHOW_FACILITY_MODAL && facilityModalOpen) DrawFacilityModal();
            else if (SHOW_TABLET && tabletOpen) DrawPocketTablet();

            if (SHOW_GAME_OVER) DrawGameOverBanner();
        }

        // ── Top Bar ───────────────────────────────────────────────────────

        private void DrawTopBar()
        {
            float barH = 36f;
            GUI.Box(new Rect(0, 0, Screen.width, barH), GUIContent.none, topBarStyle);

            GUILayout.BeginArea(new Rect(8, 4, Screen.width - 16, barH - 8));
            GUILayout.BeginHorizontal();

            // Tanggal & Area
            string areaLabel = transition != null ? transition.CurrentAreaName : "Area Kebun Sawit";
            GUILayout.Label($"📅 {(time != null ? time.GetFormattedDate() : "--")}  📍 {areaLabel}", topBarLabelStyle, GUILayout.Width(300));

            GUILayout.FlexibleSpace();

            // Uang kas
            if (econ != null)
            {
                GUILayout.Label($"💵 {EconomyManager.FormatCurrencyCompact(econ.CurrentMoney)}", topBarLabelStyle, GUILayout.Width(140));
                double nw = econ.GetNetWorth();
                Color prevColor = GUI.contentColor;
                GUI.contentColor = nw >= 0 ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
                GUILayout.Label($"📈 NW: {EconomyManager.FormatCurrencyCompact(nw)}", topBarLabelStyle, GUILayout.Width(170));
                GUI.contentColor = prevColor;
            }

            // Total hutang
            if (loan != null && loan.TotalDebt > 0)
            {
                Color prevColor = GUI.contentColor;
                GUI.contentColor = new Color(1f, 0.45f, 0.45f);
                GUILayout.Label($"💳 Hutang: {EconomyManager.FormatCurrencyCompact(loan.TotalDebt)}", topBarLabelStyle, GUILayout.Width(160));
                GUI.contentColor = prevColor;
            }

            GUILayout.FlexibleSpace();

            // Kecepatan waktu
            if (time != null)
            {
                if (GUILayout.Button("⏸", GUILayout.Width(28), GUILayout.Height(26))) time.SetSpeed(GameSpeed.Paused);
                if (GUILayout.Button("1x", GUILayout.Width(30), GUILayout.Height(26))) time.SetSpeed(GameSpeed.Normal);
                if (GUILayout.Button("2x", GUILayout.Width(30), GUILayout.Height(26))) time.SetSpeed(GameSpeed.Fast);
                if (GUILayout.Button("4x", GUILayout.Width(30), GUILayout.Height(26))) time.SetSpeed(GameSpeed.SuperFast);
                GUILayout.Space(6);
            }

            // Tombol Tablet [Tab]
            GUI.backgroundColor = tabletOpen ? new Color(0.3f, 0.8f, 0.45f) : Color.white;
            if (GUILayout.Button("📱 [Tab]", GUILayout.Width(72), GUILayout.Height(26))) tabletOpen = !tabletOpen;
            GUI.backgroundColor = Color.white;

            // Tombol Settings
            if (GUILayout.Button("⚙️", GUILayout.Width(30), GUILayout.Height(26)))
                SettingsManager.Instance?.ToggleSettings();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // ── Notification Ticker ───────────────────────────────────────────

        private void DrawNotificationTicker()
        {
            if (string.IsNullOrEmpty(currentNotification)) return;

            float tickH = 22f;
            float y = 36f;
            GUI.Box(new Rect(0, y, Screen.width, tickH), GUIContent.none, topBarStyle);
            GUI.Label(new Rect(10, y + 2, Screen.width - 20, tickH - 4), currentNotification, notifStyle);
        }

        // ── Pocket Tablet [Tab] ───────────────────────────────────────────

        private void DrawPocketTablet()
        {
            float w = 480f;
            float h = 460f;
            float x = Screen.width - w - 20f;
            float y = 70f;
            Rect panel = new Rect(x, y, w, h);

            GUI.Box(panel, GUIContent.none, tabletPanelStyle);
            GUILayout.BeginArea(new Rect(x + 14, y + 12, w - 28, h - 24));

            GUILayout.Label("📱 STATUS RINGKASAN TANAM SAWIT", tabletHeaderStyle);
            GUILayout.Space(6);

            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>💰 Keuangan</b>");
            if (econ != null)
            {
                GUILayout.Label($"  Kas Tunai : {EconomyManager.FormatCurrency(econ.CurrentMoney)}");
                GUILayout.Label($"  Net Worth : {EconomyManager.FormatCurrency(econ.GetNetWorth())}");
                GUILayout.Label($"  Lahan     : {econ.CurrentLandPercentage:F1}%");
            }
            if (loan != null && loan.TotalDebt > 0)
            {
                GUILayout.Label($"  <color=#FF6666>Hutang Total : {EconomyManager.FormatCurrency(loan.TotalDebt)}</color>");
            }
            GUILayout.EndVertical();

            GUILayout.Space(4);

            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>👷 Pekerja</b>");
            if (workers != null)
            {
                GUILayout.Label($"  Pekerja Aktif : {workers.ActiveWorkersCount} orang");
                GUILayout.Label($"  Stok TBS : {workers.TbsStockTon:F1} Ton | CPO : {workers.CpoStockTon:F1} Ton");
                GUILayout.Label($"  Pabrik : {(workers.HasFactory ? "<color=#00FF66>Beroperasi</color>" : "<color=#FFAA00>Belum Dibangun</color>")}");
            }
            GUILayout.EndVertical();

            GUILayout.Space(4);

            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>🌿 Ekologi & Rival</b>");
            if (karma != null)
            {
                string karmaColor = karma.CurrentKarma == KarmaLevel.Aman ? "#00FF66" : "#FF5555";
                GUILayout.Label($"  Karma Ekologi : <color={karmaColor}>{karma.CurrentKarma}</color>");
            }
            if (rival != null && econ != null)
            {
                double playerNW = econ.GetNetWorth();
                double cousinNW = rival.CousinCurrentNetWorth;
                string leadColor = playerNW >= cousinNW ? "#00FF66" : "#FF5555";
                GUILayout.Label($"  Net Worth Anda   : {EconomyManager.FormatCurrencyCompact(playerNW)}");
                GUILayout.Label($"  Net Worth Sepupu : {EconomyManager.FormatCurrencyCompact(cousinNW)}");
                GUILayout.Label($"  Status: <color={leadColor}>{(playerNW >= cousinNW ? "▲ Anda Memimpin!" : "▼ Tertinggal")}</color>");
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (save != null)
            {
                if (GUILayout.Button("💾 Simpan Game", GUILayout.Height(30))) save.SaveGame();
                if (save.HasSaveFile() && GUILayout.Button("📂 Muat Game", GUILayout.Height(30))) save.LoadGame();
            }
            if (GUILayout.Button("❌ Tutup [Tab]", GUILayout.Height(30))) tabletOpen = false;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        // ── Facility Modal ────────────────────────────────────────────────

        private void DrawFacilityModal()
        {
            float w = 500f;
            float h = 420f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            Rect panel = new Rect(x, y, w, h);

            // Dim backdrop
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bgTex);
            GUI.color = Color.white;

            GUI.Box(panel, GUIContent.none, modalPanelStyle);
            GUILayout.BeginArea(new Rect(x + 16, y + 14, w - 32, h - 28));

            GUILayout.Label($"🏛️ {currentFacilityName}", modalHeaderStyle);
            GUILayout.Space(8);

            switch (currentFacilityType)
            {
                case FacilityType.PapanLahan:
                case FacilityType.MandorKebun:
                    DrawModalKebun();
                    break;
                case FacilityType.GudangTBS:
                    DrawModalGudang();
                    break;
                case FacilityType.MessPekerja:
                    DrawModalMess();
                    break;
                case FacilityType.KosKosan:
                    DrawModalKos();
                    break;
                case FacilityType.Bank:
                    DrawModalBank();
                    break;
                case FacilityType.Pinjol:
                    DrawModalPinjol();
                    break;
                case FacilityType.Rentenir:
                    DrawModalRentenir();
                    break;
                case FacilityType.PabrikCPO:
                    DrawModalPabrik();
                    break;
                case FacilityType.YayasanCSR:
                    DrawModalYayasan();
                    break;
                case FacilityType.BillboardSepupu:
                    DrawModalSepupu();
                    break;
                default:
                    GUILayout.Label("Tidak ada aksi tersedia untuk fasilitas ini.");
                    break;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("❌ Tutup [Esc]", btnDangerStyle, GUILayout.Height(32))) CloseModal();

            GUILayout.EndArea();
        }

        private void DrawModalKebun()
        {
            if (econ == null) return;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Lahan Dikuasai:</b> {econ.CurrentLandPercentage:F1}% / 100%");
            GUILayout.Label($"<b>Kas Tersedia:</b> {EconomyManager.FormatCurrency(econ.CurrentMoney)}");
            GUILayout.EndVertical();
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+2.5% Lahan (Rp 30 Jt)", btnSuccessStyle, GUILayout.Height(32)))
                econ.PurchaseLand(2.5f, 30_000_000);
            if (GUILayout.Button("+10% Lahan (Rp 120 Jt)", btnSuccessStyle, GUILayout.Height(32)))
                econ.PurchaseLand(10f, 120_000_000);
            GUILayout.EndHorizontal();

            if (workers != null)
            {
                GUILayout.Space(6);
                if (GUILayout.Button("🌾 Panen TBS Sekarang", btnStyle, GUILayout.Height(32)))
                    workers.HarvestTBS();
            }
        }

        private void DrawModalGudang()
        {
            if (workers == null) return;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Stok TBS:</b> {workers.TbsStockTon:F1} Ton | <b>Stok CPO:</b> {workers.CpoStockTon:F1} Ton");
            GUILayout.Label($"<b>Status Pabrik:</b> {(workers.HasFactory ? "<color=#00FF66>Aktif</color>" : "<color=#FF9900>Belum ada</color>")}");
            GUILayout.EndVertical();
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💰 Jual TBS Mentah", btnStyle, GUILayout.Height(32))) workers.SellTBS();
            if (workers.HasFactory && GUILayout.Button("⚙️ Olah TBS → CPO", btnStyle, GUILayout.Height(32))) workers.ProcessTbsToCpo();
            if (workers.HasFactory && GUILayout.Button("🚢 Ekspor CPO", btnSuccessStyle, GUILayout.Height(32))) workers.SellCPO();
            GUILayout.EndHorizontal();
        }

        private void DrawModalMess()
        {
            if (workers == null) return;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Pekerja Aktif:</b> {workers.ActiveWorkersCount} orang");
            foreach (var w in workers.Workers)
            {
                string stamColor = w.IsExhausted ? "#FF5555" : "#00FF66";
                GUILayout.Label($"  • {w.workerName}  <color={stamColor}>Stamina: {w.stamina:F0}%</color>  Exp:{w.experience}");
            }
            GUILayout.EndVertical();
        }

        private void DrawModalKos()
        {
            if (loan == null) return;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Kos Dimiliki:</b> {loan.OwnedBoardingHouses} unit");
            GUILayout.Label("Passive Income: Rp 1.500.000 / unit / bulan");
            GUILayout.EndVertical();
            GUILayout.Space(6);
            if (GUILayout.Button("🏘️ Bangun 1 Unit Kos-kosan (Rp 75 Jt)", btnSuccessStyle, GUILayout.Height(32)))
                loan.BuyBoardingHouse();
        }

        private void DrawModalBank()
        {
            if (loan == null) return;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Hutang Bank Aktif:</b> {EconomyManager.FormatCurrency(loan.BankDebt)}");
            GUILayout.EndVertical();
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("📋 Pinjam Rp 50 Jt", btnSuccessStyle, GUILayout.Height(32))) loan.BorrowBank(50_000_000);
            if (GUILayout.Button("📋 Pinjam Rp 100 Jt", btnSuccessStyle, GUILayout.Height(32))) loan.BorrowBank(100_000_000);
            GUILayout.EndHorizontal();
            if (loan.BankDebt > 0 && GUILayout.Button("💵 Bayar Cicilan Rp 25 Jt", btnStyle, GUILayout.Height(30)))
                loan.RepayDebt(LoanType.BankKonvensional, 25_000_000);
        }

        private void DrawModalPinjol()
        {
            if (loan == null) return;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Hutang Pinjol:</b> <color=#FF5555>{EconomyManager.FormatCurrency(loan.PinjolDebt)}</color>");
            GUILayout.Label("<color=#FFAAAA><i>⚠️ Bunga harian 2% berlipat ganda! Teror debt collector jika gagal bayar!</i></color>");
            GUILayout.EndVertical();
            GUILayout.Space(6);
            if (GUILayout.Button("📱 Cairkan Pinjol Rp 20 Jt", btnDangerStyle, GUILayout.Height(32))) loan.BorrowPinjol(20_000_000);
            if (loan.PinjolDebt > 0 && GUILayout.Button("💵 Bayar Pinjol Rp 10 Jt", btnStyle, GUILayout.Height(30)))
                loan.RepayDebt(LoanType.Pinjol, 10_000_000);
        }

        private void DrawModalRentenir()
        {
            if (loan == null) return;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Hutang Rentenir:</b> <color=#FF9900>{EconomyManager.FormatCurrency(loan.RentenirDebt)}</color>");
            GUILayout.Label("<color=#FFDDAA><i>Bunga 1% per hari. Jangan sampai gagal bayar!</i></color>");
            GUILayout.EndVertical();
            GUILayout.Space(6);
            if (GUILayout.Button("💴 Pinjam Tunai Rp 30 Jt", btnDangerStyle, GUILayout.Height(32))) loan.BorrowRentenirMadura(30_000_000);
            if (loan.RentenirDebt > 0 && GUILayout.Button("💵 Bayar Rp 15 Jt", btnStyle, GUILayout.Height(30)))
                loan.RepayDebt(LoanType.RentenirMadura, 15_000_000);
        }

        private void DrawModalPabrik()
        {
            if (workers == null) return;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Status Pabrik:</b> {(workers.HasFactory ? "<color=#00FF66>Beroperasi</color>" : "<color=#FFAA00>Belum Dibangun</color>")}");
            GUILayout.Label($"<b>Stok TBS:</b> {workers.TbsStockTon:F1} Ton | <b>CPO:</b> {workers.CpoStockTon:F1} Ton");
            if (BuildingManager.Instance != null)
                GUILayout.Label($"<b>Status Kerusakan:</b> {(BuildingManager.Instance.IsFactoryDamaged ? "<color=#FF5555>RUSAK (perlu perbaikan)</color>" : "<color=#00FF66>Normal</color>")}");
            GUILayout.EndVertical();
            GUILayout.Space(6);

            if (!workers.HasFactory)
            {
                if (GUILayout.Button("🏭 Bangun Pabrik CPO (Rp 150 Jt)", btnSuccessStyle, GUILayout.Height(32)))
                    BuildingManager.Instance?.BuildFactory();
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("⚙️ Olah TBS → CPO (5:1)", btnStyle, GUILayout.Height(32))) workers.ProcessTbsToCpo();
                if (GUILayout.Button("🚢 Ekspor CPO", btnSuccessStyle, GUILayout.Height(32))) workers.SellCPO();
                GUILayout.EndHorizontal();
                if (BuildingManager.Instance != null && BuildingManager.Instance.IsFactoryDamaged)
                {
                    if (GUILayout.Button("🔧 Perbaiki Pabrik (Rp 25 Jt)", btnDangerStyle, GUILayout.Height(30)))
                        BuildingManager.Instance.RepairFactory();
                }
            }
        }

        private void DrawModalYayasan()
        {
            if (BuildingManager.Instance == null) return;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Status Yayasan:</b> {(BuildingManager.Instance.HasFoundation ? "<color=#00FF66>Sudah Berdiri</color>" : "<color=#FFAA00>Belum Dibangun</color>")}");
            GUILayout.Label("Bonus: Semua pekerja baru mendapat +10 Intelligence.");
            GUILayout.EndVertical();
            GUILayout.Space(6);
            if (!BuildingManager.Instance.HasFoundation)
            {
                if (GUILayout.Button("🎓 Danai Yayasan CSR (Rp 80 Jt)", btnSuccessStyle, GUILayout.Height(32)))
                    BuildingManager.Instance.BuildFoundation();
            }
        }

        private void DrawModalSepupu()
        {
            if (rival == null || econ == null) return;
            double playerNW = econ.GetNetWorth();
            double cousinNW = rival.CousinCurrentNetWorth;
            bool isLeading = playerNW >= cousinNW;

            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Net Worth Anda:</b> <color=#00FF66>{EconomyManager.FormatCurrency(playerNW)}</color>");
            GUILayout.Label($"<b>Net Worth Sepupu:</b> <color=#FFCC00>{EconomyManager.FormatCurrency(cousinNW)}</color>");
            string status = isLeading ? "<color=#00FF66>▲ Anda sedang MEMIMPIN!</color>" : "<color=#FF5555>▼ Anda TERTINGGAL dari sepupu!</color>";
            GUILayout.Label($"<b>Status:</b> {status}");
            GUILayout.Label($"<b>Batas Evaluasi:</b> Tahun {rival.TargetEvaluationYear}");
            GUILayout.EndVertical();
            GUILayout.Space(6);
            if (GUILayout.Button("⏩ Test Evaluasi Ending Sekarang", btnStyle, GUILayout.Height(30)))
                rival.EvaluateTenYearDeadline();
        }

        // ── Game Over Banner ──────────────────────────────────────────────

        private void DrawGameOverBanner()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.GameOver) return;

            float w = 480f;
            float h = 200f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none, modalPanelStyle);
            GUILayout.BeginArea(new Rect(x + 16, y + 14, w - 32, h - 28));
            GUILayout.Label("<color=#FF2222><b>⚠️ PERMAINAN BERAKHIR ⚠️</b></color>", modalHeaderStyle);
            GUILayout.Space(10);
            if (rival != null && rival.FinalEnding != GameEnding.BelumSelesai)
                GUILayout.Label($"Hasil: <b>{rival.FinalEnding}</b>");
            GUILayout.Space(10);
            if (GUILayout.Button("🔄 Mulai Ulang", btnStyle, GUILayout.Height(34)))
                GameManager.Instance.RestartCurrentScene();
            GUILayout.EndArea();
        }

        // ── Input Helpers ─────────────────────────────────────────────────

        private static bool IsTabPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame) return true;
#endif
            try { return Input.GetKeyDown(KeyCode.Tab); } catch { return false; }
        }

        private static bool IsEscPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
#endif
            try { return Input.GetKeyDown(KeyCode.Escape); } catch { return false; }
        }
    }
}
