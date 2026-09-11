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

        [Header("Evaluasi Akhir Tahun ke-10")]
        [Tooltip("Tahun evaluasi akhir warisan kakek.")]
        [SerializeField] private int targetYear = 2034; // 10 tahun dari 2024
        [Tooltip("Target Net Worth untuk memenangkan Good Ending (Rp 1 Miliar).")]
        [SerializeField] private double targetNetWorthForGoodEnding = 1_000_000_000;

        [Header("Riwayat Peristiwa Terakhir")]
        [TextArea(2, 3)]
        [SerializeField] private string latestEcologyNews = "Kondisi ekologi hutan sekitar masih seimbang.";
        public string LatestEcologyNews => latestEcologyNews;

        // Flags agar event besar tidak berulang setiap hari
        private bool elephantRaidTriggered = false;
        private bool secretEndingTriggered = false;

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
            // Sambungkan ke TimeManager
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed += HandleDailyEcologyCheck;
                TimeManager.Instance.OnYearPassed += HandleYearlyEndingCheck;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed -= HandleDailyEcologyCheck;
                TimeManager.Instance.OnYearPassed -= HandleYearlyEndingCheck;
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
            latestEcologyNews = "[SECRET ENDING - KIAMAT LINGKUNGAN] 100% Hutan Gundul! Banjir bandang dan tanah longsor dahsyat menyapu seluruh kebun, aset, dan desa! MC kehilangan segalanya!";
            Debug.LogError(latestEcologyNews);
            NotifyEcology(KarmaLevel.KiamatLongsor, latestEcologyNews);
            OnEndingTriggered?.Invoke(latestEcologyNews);

            GameManager.Instance?.ChangeState(GameState.GameOver);
        }
        #endregion

        #region Evaluasi Tahun ke-10 (Good vs Bad Ending)
        private void HandleYearlyEndingCheck(int currentYear)
        {
            if (currentYear >= targetYear)
            {
                EvaluateTenYearDeadline();
            }
        }

        public void EvaluateTenYearDeadline()
        {
            if (secretEndingTriggered) return;

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
        #endregion

        private void NotifyEcology(KarmaLevel level, string msg)
        {
            Debug.LogWarning(msg);
            OnEcologyEvent?.Invoke(level, msg);
        }
    }
}
