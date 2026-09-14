using System;
using UnityEngine;

namespace TanamSawit.Managers
{
    public enum GameEnding
    {
        BelumSelesai,
        GoodEnding_SawitTycoon,
        BadEnding_FailedHeir,
        SecretEnding_KiamatLingkungan
    }

    /// <summary>
    /// RivalManager melacak kekayaan Sepupu MC dan mengevaluasi kemenangan
    /// di batas akhir permainan (misal: Tahun ke-10).
    /// </summary>
    [DefaultExecutionOrder(-65)]
    public class RivalManager : MonoBehaviour
    {
        public static RivalManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Sepupu MC (Rival Utama)")]
        [Tooltip("Kekayaan awal sepupu (warisan rumah mewah, supercar, dsb).")]
        [SerializeField] private double cousinInitialNetWorth = 500_000_000; // Rp 500 Juta
        [SerializeField] private double cousinCurrentNetWorth;
        public double CousinCurrentNetWorth => cousinCurrentNetWorth;

        [Tooltip("Pertumbuhan kekayaan pasif sepupu per tahun (dalam persen, misal 8%).")]
        [SerializeField] private float cousinAnnualGrowthRate = 0.08f;

        [Header("Batas Waktu Kompetisi")]
        [Tooltip("Tahun game batas akhir evaluasi warisan.")]
        [SerializeField] private int targetEvaluationYear = 2034; // 10 tahun dari 2024
        public int TargetEvaluationYear => targetEvaluationYear;

        [Tooltip("Target Net Worth absolut untuk Good Ending (Rp 1 Miliar). Ported dari EcologyManager.")]
        [SerializeField] private double targetNetWorthForGoodEnding = 1_000_000_000;

        [Header("Hasil Akhir Permainan")]
        [SerializeField] private GameEnding finalEnding = GameEnding.BelumSelesai;
        public GameEnding FinalEnding => finalEnding;

        public event Action<GameEnding, string> OnEndingReached;

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

            cousinCurrentNetWorth = cousinInitialNetWorth;
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnYearPassed += HandleYearPassed;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnYearPassed -= HandleYearPassed;
            }
        }

        private void HandleYearPassed(int newYear)
        {
            // Kekayaan sepupu tumbuh setiap tahun (misal bisnis gaya hidup / flexingnya)
            cousinCurrentNetWorth += cousinCurrentNetWorth * cousinAnnualGrowthRate;
            Debug.Log($"[RivalManager] Tahun {newYear}: Kekayaan Sepupu naik menjadi {EconomyManager.FormatCurrency(cousinCurrentNetWorth)}");

            // Evaluasi saat mencapai tahun target
            if (newYear >= targetEvaluationYear && finalEnding == GameEnding.BelumSelesai)
            {
                EvaluateTenYearDeadline();
            }
        }

        /// <summary>
        /// Mengevaluasi siapa pewaris tersukses di akhir tahun ke-10.
        /// </summary>
        public void EvaluateTenYearDeadline()
        {
            // Jika sudah terkena Secret Ending Kiamat Lingkungan, jangan overwrite
            if (EnvironmentalKarmaManager.Instance != null && 
                EnvironmentalKarmaManager.Instance.CurrentKarma == KarmaLevel.KiamatLongsor)
            {
                finalEnding = GameEnding.SecretEnding_KiamatLingkungan;
                return;
            }

            double playerNetWorth = EconomyManager.Instance != null ? EconomyManager.Instance.GetNetWorth() : 0;

            if (playerNetWorth > cousinCurrentNetWorth)
            {
                finalEnding = GameEnding.GoodEnding_SawitTycoon;
                string msg = $"[SELAMAT - GOOD ENDING] Raja Sawit Sejati! Net Worth Anda ({EconomyManager.FormatCurrency(playerNetWorth)}) berhasil melampaui sepupu ({EconomyManager.FormatCurrency(cousinCurrentNetWorth)}). Sepupu Anda terkejut dan memohon pinjaman modal!";
                Debug.Log(msg);
                OnEndingReached?.Invoke(finalEnding, msg);
            }
            else if (playerNetWorth < targetNetWorthForGoodEnding)
            {
                // Worse bad ending — ported from EcologyManager (failed to reach Rp 1 Miliar absolute target)
                finalEnding = GameEnding.BadEnding_FailedHeir;
                string msg = $"[BAD ENDING: THE FAILED HEIR]\nGame Over! Net Worth Anda hanya {EconomyManager.FormatCurrency(playerNetWorth)} (Gagal mencapai target Rp 1 Miliar). Warisan kakek habis dan Anda kalah dari sepupu!";
                Debug.LogWarning(msg);
                OnEndingReached?.Invoke(finalEnding, msg);
            }
            else
            {
                finalEnding = GameEnding.BadEnding_FailedHeir;
                string msg = $"[GAME OVER - BAD ENDING] Pewaris Gagal! Kekayaan Anda ({EconomyManager.FormatCurrency(playerNetWorth)}) kalah jauh dari gaya hidup sepupu ({EconomyManager.FormatCurrency(cousinCurrentNetWorth)}). Anda dicoret dari silsilah keluarga konglomerat!";
                Debug.LogWarning(msg);
                OnEndingReached?.Invoke(finalEnding, msg);
            }

            GameManager.Instance?.ChangeState(GameState.GameOver);
        }

        #region Save/Load State
        /// <summary>
        /// Memuat status kekayaan sepupu dan ending dari save data.
        /// </summary>
        public void LoadState(double cousinNetWorth, GameEnding ending)
        {
            cousinCurrentNetWorth = Math.Max(0, cousinNetWorth);
            finalEnding = ending;
        }
        #endregion
    }
}
