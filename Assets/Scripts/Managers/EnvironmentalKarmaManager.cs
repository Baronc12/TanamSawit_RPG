using System;
using UnityEngine;

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
    /// EnvironmentalKarmaManager memantau persentase kepemilikan lahan dari EconomyManager.
    /// Ketika deforestasi melewati ambang batas kritis (75%, 85%, 90%, 100%),
    /// sistem ini memicu bencana sosial-ekologis sesuai GDD.
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

        #region Events
        public event Action<KarmaLevel, string> OnKarmaTriggered;
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
                CheckEnvironmentalKarma(EconomyManager.Instance.CurrentLandPercentage, 0);
            }
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLandPercentageChanged -= CheckEnvironmentalKarma;
            }
        }

        /// <summary>
        /// Evaluasi dampak ekologis setiap kali luas lahan bertambah.
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

        private void TriggerMonyetInvasion()
        {
            lastIncidentLog = "[EKOLOGI 75%] Monyet kehilangan habitat asli dan mulai menyerbu pemukiman/kebun. Sebagian hasil panen dicuri kawanan kera!";
            Debug.LogWarning(lastIncidentLog);
            OnKarmaTriggered?.Invoke(KarmaLevel.InvasiMonyet, lastIncidentLog);
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

            OnKarmaTriggered?.Invoke(KarmaLevel.SeranganGajah, lastIncidentLog);
        }

        private void TriggerMacanAttack()
        {
            lastIncidentLog = "[EKOLOGI 90%] BAHAYA! Macan tutul masuk ke areal panen. Pekerja panen panik, 1 pekerja dilarikan ke rumah sakit! Moral kerja anjlok.";
            Debug.LogError(lastIncidentLog);
            OnKarmaTriggered?.Invoke(KarmaLevel.TerorMacan, lastIncidentLog);
        }

        private void TriggerCatastropheEnding()
        {
            lastIncidentLog = "[EKOLOGI 100% - SECRET ENDING] Hutan gundul total! Hujan lebat memicu BANJIR BANDANG & TANAH LONGSOR DAHSYAT. Seluruh kebun sawit dan desa tersapu air bah!";
            Debug.LogError(lastIncidentLog);

            OnKarmaTriggered?.Invoke(KarmaLevel.KiamatLongsor, lastIncidentLog);

            // Memicu Game Over / Secret Ending di GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameState.GameOver);
            }
        }

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
        #endregion
    }
}
