using System;
using UnityEngine;
using TanamSawit.Buildings;

namespace TanamSawit.Managers
{
    /// <summary>
    /// Tingkat keparahan hukuman lingkungan akibat deforestasi perkebunan sawit.
    /// </summary>
    public enum KarmaLevel
    {
        Aman,           // < 75%
        InvasiMonyet,   // 75% - 84% (Pencurian panen)
        SeranganGajah,  // 85% - 89% (Infrastruktur rusak)
        TerorMacan,     // 90% - 99% (Pekerja diserang)
        KiamatLongsor   // 100% (Banjir Bandang & Longsor, Secret Ending)
    }

    /// <summary>
    /// EnvironmentalKarmaManager is the SINGLE source of truth for ecology/karma events.
    /// It memantau persentase kepemilikan lahan dari EconomyManager.
    /// Ketika deforestasi melewati ambang batas kritis (75%, 85%, 90%, 100%),
    /// sistem ini memicu bencana sosial-ekologis sesuai GDD.
    ///
    /// Adapter events (OnEcologyEvent, OnEndingTriggered) match the old EcologyManager
    /// signatures so UI consumers (ModernTycoonHUD, UIManager) can subscribe unchanged.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public class EnvironmentalKarmaManager : MonoBehaviour
    {
        public static EnvironmentalKarmaManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Status Karma Ekologi (Live)")]
        [SerializeField] private KarmaLevel currentKarma = KarmaLevel.Aman;
        public KarmaLevel CurrentKarma => currentKarma;

        [Header("Riwayat Bencana Terakhir")]
        [TextArea(2, 4)]
        [SerializeField] private string lastIncidentLog = "Kondisi ekologi hutan sekitar masih seimbang.";
        public string LastIncidentLog => lastIncidentLog;

        // Flags untuk mencegah pemicuan berulang
        private bool triggered75 = false;
        private bool triggered85 = false;
        private bool triggered90 = false;
        private bool triggered100 = false;
        private bool isGameOverTriggered = false;

        #region Events
        // Primary event (existing)
        public event Action<KarmaLevel, string> OnKarmaTriggered;

        // Adapter events — match old EcologyManager signatures for UI compatibility
        public event Action<KarmaLevel, string> OnEcologyEvent;
        public event Action<string> OnEndingTriggered;
        #endregion

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
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLandPercentageChanged += CheckEnvironmentalKarma;
                EconomyManager.Instance.OnBankruptcy += HandleBankruptcy;
                CheckEnvironmentalKarma(EconomyManager.Instance.CurrentLandPercentage, 0);
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed += HandleDailyEcologyCheck;
            }
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLandPercentageChanged -= CheckEnvironmentalKarma;
                EconomyManager.Instance.OnBankruptcy -= HandleBankruptcy;
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed -= HandleDailyEcologyCheck;
            }
        }

        /// <summary>
        /// Evaluasi dampak ekologis setiap kali luas lahan bertambah (threshold one-shot triggers).
        /// </summary>
        public void CheckEnvironmentalKarma(float currentLandPct, float delta)
        {
            if (currentLandPct >= 100f && !triggered100)
            {
                triggered100 = true;
                currentKarma = KarmaLevel.KiamatLongsor;
                TriggerCatastropheEnding();
            }
            else if (currentLandPct >= 90f && !triggered90)
            {
                triggered90 = true;
                currentKarma = KarmaLevel.TerorMacan;
                TriggerMacanAttack();
            }
            else if (currentLandPct >= 85f && !triggered85)
            {
                triggered85 = true;
                currentKarma = KarmaLevel.SeranganGajah;
                TriggerGajahRaid();
            }
            else if (currentLandPct >= 75f && !triggered75)
            {
                triggered75 = true;
                currentKarma = KarmaLevel.InvasiMonyet;
                TriggerMonyetInvasion();
            }
        }

        /// <summary>
        /// Cek harian untuk event random (pencurian monyet, serangan macan).
        /// Ported from EcologyManager — these are ongoing gameplay mechanics, not one-shot triggers.
        /// </summary>
        private void HandleDailyEcologyCheck(int day, int month, int year)
        {
            if (EconomyManager.Instance == null) return;

            float landPct = EconomyManager.Instance.CurrentLandPercentage;

            if (landPct >= 75f && landPct < 85f)
            {
                ProcessMonkeyEvent();
            }
            else if (landPct >= 90f && landPct < 100f)
            {
                ProcessTigerEvent();
            }
        }

        #region Threshold Trigger Methods (one-shot)

        private void TriggerMonyetInvasion()
        {
            lastIncidentLog = "[EKOLOGI 75%] Monyet kehilangan habitat asli dan mulai menyerbu pemukiman/kebun. Sebagian hasil panen dicuri kawanan kera!";
            Debug.LogWarning(lastIncidentLog);
            OnKarmaTriggered?.Invoke(KarmaLevel.InvasiMonyet, lastIncidentLog);
            OnEcologyEvent?.Invoke(KarmaLevel.InvasiMonyet, lastIncidentLog);
        }

        private void TriggerGajahRaid()
        {
            lastIncidentLog = "[EKOLOGI 85%] Kawanan gajah liar turun ke perkebunan karena koridor jelajahnya terputus! Pagar pembatas roboh dan infrastruktur pabrik rusak (perlu biaya perbaikan).";
            Debug.LogWarning(lastIncidentLog);

            // Mengurangi kas untuk perbaikan darurat jika uang cukup
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SpendMoney(15_000_000, "Biaya Darurat Perbaikan Pabrik Akibat Serangan Gajah");
            }

            // Damage the factory (ported from EcologyManager.ProcessElephantEvent)
            BuildingManager.Instance?.DamageFactory();

            OnKarmaTriggered?.Invoke(KarmaLevel.SeranganGajah, lastIncidentLog);
            OnEcologyEvent?.Invoke(KarmaLevel.SeranganGajah, lastIncidentLog);
        }

        private void TriggerMacanAttack()
        {
            lastIncidentLog = "[EKOLOGI 90%] BAHAYA! Macan tutul masuk ke areal panen. Pekerja panen panik, 1 pekerja dilarikan ke rumah sakit! Moral kerja anjlok.";
            Debug.LogError(lastIncidentLog);
            OnKarmaTriggered?.Invoke(KarmaLevel.TerorMacan, lastIncidentLog);
            OnEcologyEvent?.Invoke(KarmaLevel.TerorMacan, lastIncidentLog);
        }

        private void TriggerCatastropheEnding()
        {
            lastIncidentLog = "[EKOLOGI 100% - SECRET ENDING] Hutan gundul total! Hujan lebat memicu BANJIR BANDANG & TANAH LONGSOR DAHSYAT. Seluruh kebun sawit dan desa tersapu air bah!";
            Debug.LogError(lastIncidentLog);

            OnKarmaTriggered?.Invoke(KarmaLevel.KiamatLongsor, lastIncidentLog);
            OnEcologyEvent?.Invoke(KarmaLevel.KiamatLongsor, lastIncidentLog);
            OnEndingTriggered?.Invoke(lastIncidentLog);

            if (!isGameOverTriggered)
            {
                isGameOverTriggered = true;
                GameManager.Instance?.ChangeState(GameState.GameOver);
            }
        }

        #endregion

        #region Daily Random Events (ported from EcologyManager)

        /// <summary>
        /// Lahan >= 75%: Monyet mencuri uang/panen acak setiap hari (40% chance).
        /// </summary>
        private void ProcessMonkeyEvent()
        {
            if (UnityEngine.Random.value < 0.4f)
            {
                double stolenAmount = UnityEngine.Random.Range(500_000, 2_500_000);
                if (EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(stolenAmount))
                {
                    EconomyManager.Instance.SpendMoney(stolenAmount, "Pencurian Panen oleh Kawanan Monyet");
                    string msg = $"[EVENT MONYET] Kawanan kera liar menyerbu gudang! Kerugian: -{EconomyManager.FormatCurrency(stolenAmount)}.";
                    OnEcologyEvent?.Invoke(KarmaLevel.InvasiMonyet, msg);
                }
            }
        }

        /// <summary>
        /// Lahan >= 90%: Macan memangsa pekerja (25% chance per day).
        /// </summary>
        private void ProcessTigerEvent()
        {
            if (UnityEngine.Random.value < 0.25f)
            {
                string msg = "[EVENT MACAN] Macan tutul masuk ke areal perkebunan! Satu pekerja menjadi korban!";
                bool workerLost = WorkerManager.Instance != null && WorkerManager.Instance.KillRandomWorker();
                if (workerLost)
                {
                    OnEcologyEvent?.Invoke(KarmaLevel.TerorMacan, msg);
                }
            }
        }

        #endregion

        #region Bankruptcy Ending

        /// <summary>
        /// Dipanggil saat pemain mengalami kebangkrutan total (Net Worth jatuh di bawah ambang batas toleransi).
        /// Ported from EcologyManager.HandleBankruptcy.
        /// </summary>
        public void HandleBankruptcy()
        {
            if (isGameOverTriggered) return;
            isGameOverTriggered = true;

            string netWorthFormatted = EconomyManager.Instance != null ? EconomyManager.FormatCurrency(EconomyManager.Instance.GetNetWorth()) : "Rp 0";
            string thresholdFormatted = EconomyManager.Instance != null ? EconomyManager.FormatCurrency(EconomyManager.Instance.BankruptcyThreshold) : "Rp -50.000.000";

            string bankruptcyEnding = $"[BAD ENDING: BANGKRUT]\nGame Over! Perusahaan perkebunan Anda bangkrut total! Net Worth ({netWorthFormatted}) telah jatuh di bawah batas minimum ({thresholdFormatted}). Hutang tak terbayar dan seluruh lahan disita!";
            Debug.LogError(bankruptcyEnding);
            lastIncidentLog = bankruptcyEnding;
            OnEcologyEvent?.Invoke(KarmaLevel.Aman, bankruptcyEnding);
            OnEndingTriggered?.Invoke(bankruptcyEnding);

            GameManager.Instance?.ChangeState(GameState.GameOver);
        }

        #endregion

        #region Save/Load State

        /// <summary>
        /// Memuat status karma ekologi dan insiden bencana dari save data.
        /// </summary>
        public void LoadState(KarmaLevel karma, string log, bool t75, bool t85, bool t90, bool t100)
        {
            currentKarma = karma;
            lastIncidentLog = log;
            triggered75 = t75;
            triggered85 = t85;
            triggered90 = t90;
            triggered100 = t100;
        }

        public (bool t75, bool t85, bool t90, bool t100) GetTriggerFlags()
        {
            return (triggered75, triggered85, triggered90, triggered100);
        }

        /// <summary>
        /// Resets ending/karma state (e.g. on Load Game or Restart).
        /// Replaces the old EcologyManager.ResetEndingStates() call.
        /// </summary>
        public void ResetEndingStates()
        {
            isGameOverTriggered = false;
            triggered75 = false;
            triggered85 = false;
            triggered90 = false;
            triggered100 = false;
            currentKarma = KarmaLevel.Aman;
            lastIncidentLog = "Kondisi ekologi hutan sekitar masih seimbang.";
        }

        /// <summary>
        /// Re-applies side effects after loading a save (e.g. factory damage if karma >= SeranganGajah).
        /// Call after BuildingManager.LoadState.
        /// </summary>
        public void RestoreKarmaSideEffects()
        {
            if (currentKarma >= KarmaLevel.SeranganGajah)
            {
                BuildingManager.Instance?.DamageFactory();
            }
        }

        #endregion
    }
}
