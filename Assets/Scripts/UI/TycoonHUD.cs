using UnityEngine;
using TMPro;
using TanamSawit.Managers;
using TanamSawit.SaveSystem;
using TanamSawit.Environment;

namespace TanamSawit.UI
{
    /// <summary>
    /// TycoonHUD adalah pusat kontrol visual prototype gameplay Tanam Sawit.
    /// Dilengkapi dengan GUI interaktif lengkap untuk menguji seluruh mekanik GDD:
    /// Panen TBS/CPO, Ekspansi Lahan, Karma Ekologi (75-100%), 3 Jalur Hutang (Pinjol/Bank/Madura),
    /// Persaingan Net Worth vs Sepupu, Evaluasi Ending, serta Sistem Save/Load JSON.
    /// </summary>
    public class TycoonHUD : MonoBehaviour
    {
        [Header("Pengaturan Tampilan")]
        [SerializeField] private bool showPrototypeGUI = true;

        private int selectedTab = 0;
        private readonly string[] tabNames = new string[] { "🌾 Kebun", "🚜 Lahan", "💳 Kredit", "📊 Sepupu", "💾 Save" };
        private string lastSaveMessage = "";

        private void OnEnable()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnSaveStatusChanged += HandleSaveStatus;
            }
        }

        private void OnDisable()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnSaveStatusChanged -= HandleSaveStatus;
            }
        }

        private void HandleSaveStatus(string msg)
        {
            lastSaveMessage = msg;
        }

        private void OnGUI()
        {
            if (!showPrototypeGUI) return;

            // Box Panel Utama
            GUI.Box(new Rect(15, 15, 480, 520), "<b>=== TANAM SAWIT: CORE GAMEPLAY PROTOTYPE ===</b>");

            GUILayout.BeginArea(new Rect(25, 45, 460, 480));

            // 1. STATS BAR ATAS
            DrawTopStatsBar();

            GUILayout.Space(8);

            // 2. KONTROL WAKTU
            DrawTimeControls();

            GUILayout.Space(8);

            // 3. TAB NAVIGASI
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            GUILayout.Space(8);

            // 4. KONTEN TAB SESUAI PILIHAN
            switch (selectedTab)
            {
                case 0:
                    DrawHarvestTab();
                    break;
                case 1:
                    DrawLandAndKarmaTab();
                    break;
                case 2:
                    DrawLoansAndPropertyTab();
                    break;
                case 3:
                    DrawRivalTab();
                    break;
                case 4:
                    DrawSaveLoadTab();
                    break;
            }

            // 5. BANNER ENDING JIKA GAME OVER
            DrawEndingBanner();

            GUILayout.EndArea();
        }

        private void DrawTopStatsBar()
        {
            GUILayout.BeginVertical("box");
            if (EconomyManager.Instance != null)
            {
                GUILayout.Label($"<b>Uang Kas:</b> <color=#00FF66>{EconomyManager.FormatCurrency(EconomyManager.Instance.CurrentMoney)}</color> | <b>Net Worth:</b> {EconomyManager.FormatCurrency(EconomyManager.Instance.GetNetWorth())}");
                GUILayout.Label($"<b>Lahan Dikuasai:</b> <b>{EconomyManager.Instance.CurrentLandPercentage:F1}%</b> / 100%");
            }

            if (LoanManager.Instance != null && LoanManager.Instance.TotalDebt > 0)
            {
                GUILayout.Label($"<color=#FF5555><b>Total Hutang Aktif:</b> {EconomyManager.FormatCurrency(LoanManager.Instance.TotalDebt)}</color>");
            }
            GUILayout.EndVertical();
        }

        private void DrawTimeControls()
        {
            if (TimeManager.Instance == null) return;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>📅 {TimeManager.Instance.GetFormattedDate()}</b>", GUILayout.Width(180));

            if (GUILayout.Button("||", GUILayout.Width(35))) TimeManager.Instance.SetSpeed(GameSpeed.Paused);
            if (GUILayout.Button("1x", GUILayout.Width(35))) TimeManager.Instance.SetSpeed(GameSpeed.Normal);
            if (GUILayout.Button("2x", GUILayout.Width(35))) TimeManager.Instance.SetSpeed(GameSpeed.Fast);
            if (GUILayout.Button("4x", GUILayout.Width(35))) TimeManager.Instance.SetSpeed(GameSpeed.SuperFast);

            // Tombol fast-forward untuk test bulanan/tahunan
            if (GUILayout.Button("+1 Bln", GUILayout.Width(60)))
            {
                for (int i = 0; i < 30; i++) TimeManager.Instance.AdvanceDay();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawHarvestTab()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>MANAJEMEN PANEN & PABRIK</b>");

            var wm = WorkerManager.Instance;
            if (wm != null)
            {
                GUILayout.Label($"<b>Gudang TBS:</b> {wm.TbsStockTon:F1} Ton | <b>Minyak CPO:</b> {wm.CpoStockTon:F1} Ton");
                GUILayout.Label($"<b>Status Pabrik CPO:</b> {(wm.HasFactory ? "<color=#00FF66>Sudah Dibangun</color>" : "<color=#FF9900>Belum Memiliki Pabrik</color>")}");

                GUILayout.Space(5);
                GUILayout.Label("<b>Status 3 Pekerja Veteran Kakek:</b>");
                foreach (var w in wm.Workers)
                {
                    string status = w.IsExhausted ? "<color=#FF5555>Kelelahan!</color>" : $"Stamina: {w.stamina:F0}%";
                    GUILayout.Label($"- {w.workerName} | {status} | Exp: {w.experience}");
                }

                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Panen TBS di Kebun"))
                {
                    wm.HarvestTBS();
                }
                if (GUILayout.Button("Jual TBS (Mentah)"))
                {
                    wm.SellTBS();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (!wm.HasFactory)
                {
                    if (GUILayout.Button("Bangun Pabrik CPO (Rp 100 Jt)"))
                    {
                        wm.BuildFactory();
                    }
                }
                else
                {
                    if (GUILayout.Button("Olah TBS jadi CPO (5:1)"))
                    {
                        wm.ProcessTbsToCpo();
                    }
                    if (GUILayout.Button("Ekspor CPO (Untung Besar)"))
                    {
                        wm.SellCPO();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawLandAndKarmaTab()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>PERLUASAN LAHAN & KARMA EKOLOGI</b>");

            var karma = EnvironmentalKarmaManager.Instance;
            if (karma != null)
            {
                string karmaColor = karma.CurrentKarma == KarmaLevel.Aman ? "#00FF66" : "#FF5555";
                GUILayout.Label($"<b>Status Ekologi:</b> <color={karmaColor}><b>{karma.CurrentKarma}</b></color>");
                GUILayout.Label($"<i>{karma.LastIncidentLog}</i>");
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Beli Plot Lahan Baru:</b> (Memicu reaksi alam)");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+2.5% Lahan (Rp 30 Jt)"))
            {
                EconomyManager.Instance?.PurchaseLand(2.5f, 30_000_000);
            }
            if (GUILayout.Button("+10% Lahan (Rp 120 Jt)"))
            {
                EconomyManager.Instance?.PurchaseLand(10.0f, 120_000_000);
            }
            GUILayout.EndHorizontal();

            // Status Grid 2D & Tombol Uji Spawner
            if (GridManager.Instance != null)
            {
                GUILayout.Space(4);
                GUILayout.Label($"<b>Visual Grid:</b> {GridManager.Instance.TotalSpawnedPlots} Plot Pohon Sawit aktif di scene.");
            }

            if (EcologySpawner.Instance != null)
            {
                if (GUILayout.Button("🐒 Uji Coba Spawn Monyet Sekarang"))
                {
                    EcologySpawner.Instance.SpawnMonkeys();
                }
            }

            GUILayout.Space(5);
            GUILayout.Label("<color=#FFFF55><b>Peringatan Batas Ekologi:</b>\n• 75%: Invasi Monyet | • 85%: Serangan Gajah\n• 90%: Teror Macan Tutul | • 100%: Kiamat Longsor (Secret Ending)</color>");
            GUILayout.EndVertical();
        }

        private void DrawLoansAndPropertyTab()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>SISTEM KREDIT & BISNIS SAMPINGAN</b>");

            var lm = LoanManager.Instance;
            if (lm != null)
            {
                GUILayout.Label($"• Hutang Bank: {EconomyManager.FormatCurrency(lm.BankDebt)} (Bunga 5%/thn)");
                GUILayout.Label($"• Hutang Pinjol: <color=#FF5555>{EconomyManager.FormatCurrency(lm.PinjolDebt)}</color> (Bunga 10%/bln)");
                GUILayout.Label($"• Hutang Madura: <color=#FF9900>{EconomyManager.FormatCurrency(lm.RentenirDebt)}</color> (Bunga 15%/bln)");

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Pinjam Bank (50 Jt)")) lm.BorrowBank(50_000_000);
                if (GUILayout.Button("Cairkan Pinjol (20 Jt)")) lm.BorrowPinjol(20_000_000);
                if (GUILayout.Button("Pinjam Madura (30 Jt)")) lm.BorrowRentenirMadura(30_000_000);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Bayar Pinjol (10 Jt)")) lm.RepayDebt(LoanType.Pinjol, 10_000_000);
                if (GUILayout.Button("Bayar Madura (15 Jt)")) lm.RepayDebt(LoanType.RentenirMadura, 15_000_000);
                if (GUILayout.Button("Bayar Bank (25 Jt)")) lm.RepayDebt(LoanType.BankKonvensional, 25_000_000);
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label($"<b>Bisnis Kos-kosan:</b> {lm.OwnedBoardingHouses} Unit dimiliki");
                if (GUILayout.Button("Bangun Kos-kosan (Rp 75 Jt | Passive Income 1.5 Jt/bln)"))
                {
                    lm.BuyBoardingHouse();
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawRivalTab()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>KOMPETISI PEWARIS: ANDA VS SEPUPU</b>");

            var rm = RivalManager.Instance;
            var em = EconomyManager.Instance;

            if (rm != null && em != null)
            {
                double playerNet = em.GetNetWorth();
                double cousinNet = rm.CousinCurrentNetWorth;

                GUILayout.Label($"<b>Net Worth Anda:</b> <color=#00FF66>{EconomyManager.FormatCurrency(playerNet)}</color>");
                GUILayout.Label($"<b>Net Worth Sepupu:</b> <color=#FFCC00>{EconomyManager.FormatCurrency(cousinNet)}</color>");
                GUILayout.Label($"<b>Batas Waktu:</b> Tahun {rm.TargetEvaluationYear} (Sisa evaluasi)");

                string leadStatus = playerNet >= cousinNet ? "<color=#00FF66>Anda sedang memimpin!</color>" : "<color=#FF5555>Anda masih tertinggal dari gaya hidup sepupu!</color>";
                GUILayout.Label($"Status: {leadStatus}");

                GUILayout.Space(8);
                if (GUILayout.Button("⏩ Test Evaluasi Akhir Sekarang (Ending Check)"))
                {
                    rm.EvaluateTenYearDeadline();
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawSaveLoadTab()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>SISTEM PENYIMPANAN DATA (SAVE & LOAD)</b>");

            var sm = SaveManager.Instance;
            if (sm != null)
            {
                bool hasSave = sm.HasSaveFile();
                string statusColor = hasSave ? "#00FF66" : "#FFCC00";
                string fileStatus = hasSave ? "File Save Ditemukan" : "Belum Ada File Save";
                GUILayout.Label($"<b>Status File:</b> <color={statusColor}>{fileStatus}</color>");
                GUILayout.Label($"<b>Waktu Simpan Terakhir:</b> {sm.LastSaveTime}");

                if (!string.IsNullOrEmpty(lastSaveMessage))
                {
                    GUILayout.Space(3);
                    GUILayout.Label($"<color=#FFFF55><i>{lastSaveMessage}</i></color>");
                }

                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("💾 Simpan Game (Manual Save)"))
                {
                    sm.SaveGame(isAutoSave: false);
                }
                if (GUILayout.Button("📂 Muat Game (Manual Load)"))
                {
                    sm.LoadGame();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (hasSave)
                {
                    if (GUILayout.Button("🗑️ Hapus File Savegame (Reset)"))
                    {
                        sm.DeleteSaveFile();
                    }
                }

                GUILayout.Space(8);
                GUILayout.Label("<color=#CCCCCC><b>Catatan Sistem:</b>\n• Auto-save otomatis berjalan setiap berganti bulan baru.\n• Data disimpan dalam format JSON terstruktur di disk lokal.</color>");
            }
            GUILayout.EndVertical();
        }

        private void DrawEndingBanner()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
            {
                GUILayout.Space(8);
                GUILayout.BeginVertical("box");
                GUILayout.Label("<color=#FF2222><b>=== PERMAINAN BERAKHIR ===</b></color>");

                if (RivalManager.Instance != null && RivalManager.Instance.FinalEnding != GameEnding.BelumSelesai)
                {
                    GUILayout.Label($"<b>Hasil:</b> {RivalManager.Instance.FinalEnding}");
                }

                if (EnvironmentalKarmaManager.Instance != null && EnvironmentalKarmaManager.Instance.CurrentKarma == KarmaLevel.KiamatLongsor)
                {
                    GUILayout.Label("<color=#FF5555>Terkubur Longsor & Banjir Bandang akibat 100% Lahan Monopoli!</color>");
                }

                if (GUILayout.Button("🔄 Mulai Ulang Permainan"))
                {
                    GameManager.Instance.RestartCurrentScene();
                }
                GUILayout.EndVertical();
            }
        }
    }
}
