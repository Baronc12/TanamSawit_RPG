using System;
using UnityEngine;
using TanamSawit.Buildings;

namespace TanamSawit.Managers
{
    /// <summary>
    /// EcologyManager memantau persentase penguasaan lahan sawit setiap hari dan memicu
    /// konsekuensi karma deforestasi:
    /// - 75%: Event Monyet (Uang berkurang acak setiap hari karena pencurian panen).
    /// - 85%: Event Gajah (Pabrik rusak & berhenti beroperasi hingga diperbaiki).
    /// - 90%: Event Macan (Pekerja dimangsa/hilang dari WorkerManager).
    /// - 100%: Secret Ending (Kiamat Lingkungan: Banjir Bandang & Longsor).
    /// Serta mengevaluasi Good Ending vs Bad Ending di Tahun ke-10.
    /// </summary>
    [DefaultExecutionOrder(-68)]
    public class EcologyManager : MonoBehaviour
    {
        public static EcologyManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Status Karma Ekologi")]
        [SerializeField] private KarmaLevel activeKarma = KarmaLevel.Aman;
        public KarmaLevel ActiveKarma => activeKarma;

        [Header("Evaluasi Akhir Waktu (Dinamis)")]
        [Tooltip("Durasi evaluasi permainan (dalam tahun) dihitung sejak tahun awal game.")]
        [SerializeField] private int evaluationDurationYears = 10;
        public int EvaluationDurationYears => evaluationDurationYears;

        [Tooltip("Target Net Worth untuk memenangkan Good Ending (Rp 1 Miliar).")]
        [SerializeField] private double targetNetWorthForGoodEnding = 1_000_000_000;

        [Header("Target Tahun Terhitung (Dinamis)")]
        [SerializeField] private int computedTargetYear;
        public int ComputedTargetYear => computedTargetYear;

        [Header("Riwayat Peristiwa Terakhir")]
        [TextArea(2, 3)]
        [SerializeField] private string latestEcologyNews = "Kondisi ekologi hutan sekitar masih seimbang.";
        public string LatestEcologyNews => latestEcologyNews;

        // Flags agar event besar tidak berulang setiap hari
        private bool elephantRaidTriggered = false;
        private bool secretEndingTriggered = false;
        private bool isGameOverTriggered = false;

        public event Action<KarmaLevel, string> OnEcologyEvent;
        public event Action<string> OnEndingTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (dontDestroyOnLoad)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            // Sambungkan ke TimeManager dan hitung target tahun secara dinamis
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed += HandleDailyEcologyCheck;
                TimeManager.Instance.OnYearPassed += HandleYearlyEndingCheck;
                computedTargetYear = TimeManager.Instance.CurrentYear + evaluationDurationYears;
            }
            else
            {
                // Fallback aman jika TimeManager belum siap
                computedTargetYear = 2024 + evaluationDurationYears;
            }

            // Sambungkan ke EconomyManager untuk mendeteksi kebangkrutan dini
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnBankruptcy += HandleBankruptcy;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed -= HandleDailyEcologyCheck;
                TimeManager.Instance.OnYearPassed -= HandleYearlyEndingCheck;
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnBankruptcy -= HandleBankruptcy;
            }
        }

        /// <summary>
        /// Mengecek persentase lahan dari EconomyManager setiap hari.
        /// </summary>
        private void HandleDailyEcologyCheck(int day, int month, int year)
        {
            if (EconomyManager.Instance == null) return;

            float landPct = EconomyManager.Instance.CurrentLandPercentage;

            // Logika if-else bertingkat sesuai prompt
            if (landPct >= 100f)
            {
                activeKarma = KarmaLevel.KiamatLongsor;
                if (!secretEndingTriggered)
                {
                    secretEndingTriggered = true;
                    TriggerSecretEnding();
                }
            }
            else if (landPct >= 90f)
            {
                activeKarma = KarmaLevel.TerorMacan;
                ProcessTigerEvent();
            }
            else if (landPct >= 85f)
            {
                activeKarma = KarmaLevel.SeranganGajah;
                ProcessElephantEvent();
            }
            else if (landPct >= 75f)
            {
                activeKarma = KarmaLevel.InvasiMonyet;
                ProcessMonkeyEvent();
            }
            else
            {
                activeKarma = KarmaLevel.Aman;
            }
        }

        #region Event Logika Spesifik
        /// <summary>
        /// Lahan >= 75%: Monyet mencuri uang/panen acak setiap hari.
        /// </summary>
        private void ProcessMonkeyEvent()
        {
            // Kemungkinan 40% per hari terjadi pencurian panen
            if (UnityEngine.Random.value < 0.4f)
            {
                double stolenAmount = UnityEngine.Random.Range(500_000, 2_500_000);
                if (EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(stolenAmount))
                {
                    EconomyManager.Instance.SpendMoney(stolenAmount, "Pencurian Panen oleh Kawanan Monyet");
                    latestEcologyNews = $"[EVENT MONYET] Kawanan kera liar menyerbu gudang! Kerugian: -{EconomyManager.FormatCurrency(stolenAmount)}.";
                    NotifyEcology(KarmaLevel.InvasiMonyet, latestEcologyNews);
                }
            }
        }

        /// <summary>
        /// Lahan >= 85%: Gajah merusak pabrik & menghentikan income multiplier pabrik.
        /// </summary>
        private void ProcessElephantEvent()
        {
            if (!elephantRaidTriggered)
            {
                elephantRaidTriggered = true;
                latestEcologyNews = "[EVENT GAJAH] Kawanan gajah mengamuk merusak pagar dan mesin Pabrik Sawit! Multiplier pabrik mati sampai diperbaiki.";
                BuildingManager.Instance?.DamageFactory();
                NotifyEcology(KarmaLevel.SeranganGajah, latestEcologyNews);
            }
        }

        /// <summary>
        /// Lahan >= 90%: Macan memangsa pekerja dari daftar WorkerManager.
        /// </summary>
        private void ProcessTigerEvent()
        {
            // Kemungkinan 25% terjadi serangan macan jika pekerja masih ada
            if (UnityEngine.Random.value < 0.25f)
            {
                latestEcologyNews = "[EVENT MACAN] Macan tutul masuk ke areal perkebunan! Satu pekerja menjadi korban!";
                bool workerLost = WorkerManager.Instance != null && WorkerManager.Instance.KillRandomWorker();
                if (workerLost)
                {
                    NotifyEcology(KarmaLevel.TerorMacan, latestEcologyNews);
                }
            }
        }

        /// <summary>
        /// Lahan == 100%: Secret Ending (Bencana Alam Banjir Bandang & Longsor).
        /// </summary>
        public void TriggerSecretEnding()
        {
            if (isGameOverTriggered) return;
            isGameOverTriggered = true;
            secretEndingTriggered = true;

            latestEcologyNews = "[SECRET ENDING - KIAMAT LINGKUNGAN] 100% Hutan Gundul! Banjir bandang dan tanah longsor dahsyat menyapu seluruh kebun, aset, dan desa! MC kehilangan segalanya!";
            Debug.LogError(latestEcologyNews);
            NotifyEcology(KarmaLevel.KiamatLongsor, latestEcologyNews);
            OnEndingTriggered?.Invoke(latestEcologyNews);

            GameManager.Instance?.ChangeState(GameState.GameOver);
        }

        /// <summary>
        /// Dipanggil saat pemain mengalami kebangkrutan total (Net Worth jatuh di bawah ambang batas toleransi).
        /// Memicu Bad Ending dini dan mengubah GameState ke GameOver.
        /// </summary>
        public void HandleBankruptcy()
        {
            if (isGameOverTriggered || secretEndingTriggered) return;
            isGameOverTriggered = true;

            string netWorthFormatted = EconomyManager.Instance != null ? EconomyManager.FormatCurrency(EconomyManager.Instance.GetNetWorth()) : "Rp 0";
            string thresholdFormatted = EconomyManager.Instance != null ? EconomyManager.FormatCurrency(EconomyManager.Instance.BankruptcyThreshold) : "Rp -50.000.000";

            string bankruptcyEnding = $"[BAD ENDING: BANGKRUT]\nGame Over! Perusahaan perkebunan Anda bangkrut total! Net Worth ({netWorthFormatted}) telah jatuh di bawah batas minimum ({thresholdFormatted}). Hutang tak terbayar dan seluruh lahan disita!";
            Debug.LogError(bankruptcyEnding);
            latestEcologyNews = bankruptcyEnding;
            NotifyEcology(KarmaLevel.Aman, bankruptcyEnding);
            OnEndingTriggered?.Invoke(bankruptcyEnding);

            GameManager.Instance?.ChangeState(GameState.GameOver);
        }
        #endregion

        #region Evaluasi Akhir Waktu (Good vs Bad Ending)
        private void HandleYearlyEndingCheck(int currentYear)
        {
            // Pastikan target tahun sudah terhitung
            if (computedTargetYear <= 0)
            {
                computedTargetYear = (TimeManager.Instance != null ? TimeManager.Instance.CurrentYear : 2024) + evaluationDurationYears;
            }

            if (currentYear >= computedTargetYear)
            {
                EvaluateTenYearDeadline();
            }
        }

        public void EvaluateTenYearDeadline()
        {
            if (isGameOverTriggered || secretEndingTriggered) return;
            isGameOverTriggered = true;

            double netWorth = EconomyManager.Instance != null ? EconomyManager.Instance.GetNetWorth() : 0;
            string endingTitle;

            if (netWorth >= targetNetWorthForGoodEnding)
            {
                endingTitle = $"[GOOD ENDING: THE SAWIT TYCOON]\nSelamat! Net Worth Anda mencapai {EconomyManager.FormatCurrency(netWorth)} (Melebihi target Rp 1 Miliar)! Sepupu Anda bangkrut dan terpaksa memohon kerjaan kepada Anda!";
            }
            else
            {
                endingTitle = $"[BAD ENDING: THE FAILED HEIR]\nGame Over! Net Worth Anda hanya {EconomyManager.FormatCurrency(netWorth)} (Gagal mencapai target Rp 1 Miliar). Warisan kakek habis dan Anda kalah dari sepupu!";
            }

            Debug.Log(endingTitle);
            latestEcologyNews = endingTitle;
            OnEndingTriggered?.Invoke(endingTitle);

            GameManager.Instance?.ChangeState(GameState.GameOver);
        }

        /// <summary>
        /// Mereset flag ending (misal saat Load Game atau Restart).
        /// </summary>
        public void ResetEndingStates()
        {
            isGameOverTriggered = false;
            secretEndingTriggered = false;
            elephantRaidTriggered = false;
        }
        #endregion

        private void NotifyEcology(KarmaLevel level, string msg)
        {
            Debug.LogWarning(msg);
            OnEcologyEvent?.Invoke(level, msg);
        }
    }
}
