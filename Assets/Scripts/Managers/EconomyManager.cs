using System;
using System.Globalization;
using UnityEngine;

namespace TanamSawit.Managers
{
    /// <summary>
    /// EconomyManager bertanggung jawab mencatat dan memproses transaksi finansial,
    /// kepemilikan lahan (0-100%), dan perhitungan Total Kekayaan Bersih (Net Worth).
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class EconomyManager : MonoBehaviour
    {
        #region Singleton
        public static EconomyManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        #endregion

        #region Saldo & Keuangan
        [Header("Keuangan Pemain")]
        [Tooltip("Uang tunai awal pemain dalam Rupiah (Rp).")]
        [SerializeField] private double startingMoney = 50_000_000; // Default Rp 50 Juta

        [SerializeField] private double currentMoney;
        public double CurrentMoney => currentMoney;
        #endregion

        #region Kepemilikan Lahan
        [Header("Ekspansi Lahan")]
        [Tooltip("Persentase kepemilikan lahan awal (0% - 100%).")]
        [Range(0f, 100f)]
        [SerializeField] private float startingLandPercentage = 5f; // Awal mula 5%

        [Range(0f, 100f)]
        [SerializeField] private float currentLandPercentage;
        public float CurrentLandPercentage => currentLandPercentage;

        [Tooltip("Estimasi valuasi harga per 1% lahan (dalam Rupiah) untuk menghitung Net Worth.")]
        [SerializeField] private double landValuationPerPercent = 15_000_000; // Rp 15 Juta per 1%
        #endregion

        #region Valuasi Tambahan (Pabrik, Truk, Stok Sawit)
        [Header("Aset Lainnya (Mesin/Pabrik/Stok)")]
        [Tooltip("Total nilai buku aset fisik di luar lahan dan kas (misal: pabrik, bibit, truk).")]
        [SerializeField] private double otherAssetsValuation = 0;
        #endregion

        #region Ambang Batas Kebangkrutan
        [Header("Ambang Batas Kebangkrutan (Game Over Dini)")]
        [Tooltip("Batas minimum Net Worth sebelum pemain dinyatakan bangkrut total (default: Rp -50.000.000).")]
        [SerializeField] private double bankruptcyThreshold = -50_000_000;
        public double BankruptcyThreshold => bankruptcyThreshold;

        private bool hasTriggeredBankruptcy = false;
        public bool HasTriggeredBankruptcy => hasTriggeredBankruptcy;
        #endregion

        #region Events (Observer Pattern)
        /// <summary>
        /// Dipanggil saat saldo uang berubah. Parameter: (currentMoney, deltaMoney).
        /// </summary>
        public event Action<double, double> OnMoneyChanged;

        /// <summary>
        /// Dipanggil saat persentase lahan bertambah/berkurang. Parameter: (currentPercentage, deltaPercentage).
        /// </summary>
        public event Action<float, float> OnLandPercentageChanged;

        /// <summary>
        /// Dipanggil saat Total Net Worth dievaluasi ulang. Parameter: (currentNetWorth).
        /// </summary>
        public event Action<double> OnNetWorthChanged;

        /// <summary>
        /// Dipanggil saat Net Worth pemain jatuh di bawah ambang batas kebangkrutan (Game Over dini).
        /// </summary>
        public event Action OnBankruptcy;
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[EconomyManager] Ditemukan instance duplikat pada GameObject {gameObject.name}. Menghancurkan objek ini.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            // Inisialisasi nilai awal
            currentMoney = startingMoney;
            currentLandPercentage = Mathf.Clamp(startingLandPercentage, 0f, 100f);
        }

        private void Start()
        {
            // Trigger event awal agar UI langsung sinkron saat start
            OnMoneyChanged?.Invoke(currentMoney, 0);
            OnLandPercentageChanged?.Invoke(currentLandPercentage, 0);
            RefreshNetWorth();
        }

        #region Operasi Uang
        /// <summary>
        /// Cek apakah pemain memiliki cukup uang untuk transaksi tertentu.
        /// </summary>
        public bool CanAfford(double amount)
        {
            return currentMoney >= amount;
        }

        /// <summary>
        /// Menambahkan uang ke kas pemain (hasil panen TBS, penjualan CPO, subsidi, dsb).
        /// </summary>
        /// <param name="amount">Jumlah uang yang ditambah (harus positif)</param>
        /// <param name="source">Keterangan sumber pendapatan untuk audit log</param>
        public void AddMoney(double amount, string source = "")
        {
            if (amount <= 0)
            {
                Debug.LogWarning("[EconomyManager] Jumlah uang yang ditambahkan harus lebih besar dari 0.");
                return;
            }

            currentMoney += amount;
            Debug.Log($"[EconomyManager] +{FormatCurrency(amount)} | Sumber: {(string.IsNullOrEmpty(source) ? "Pemasukan Umum" : source)} | Total Kas: {FormatCurrency(currentMoney)}");

            OnMoneyChanged?.Invoke(currentMoney, amount);
            RefreshNetWorth();
        }

        /// <summary>
        /// Mengurangi uang dari kas pemain (pembelian pupuk, upah buruh, beli bibit, dsb).
        /// </summary>
        /// <param name="amount">Jumlah uang yang dibelanjakan</param>
        /// <param name="reason">Keterangan pengeluaran</param>
        /// <returns>True jika saldo cukup dan berhasil dipotong, False jika saldo tidak cukup.</returns>
        public bool SpendMoney(double amount, string reason = "")
        {
            if (amount <= 0)
            {
                Debug.LogWarning("[EconomyManager] Jumlah pengeluaran harus lebih besar dari 0.");
                return false;
            }

            if (!CanAfford(amount))
            {
                Debug.LogWarning($"[EconomyManager] Gagal membelanjakan {FormatCurrency(amount)} untuk '{reason}'. Saldo tidak mencukupi ({FormatCurrency(currentMoney)}).");
                return false;
            }

            currentMoney -= amount;
            Debug.Log($"[EconomyManager] -{FormatCurrency(amount)} | Keperluan: {(string.IsNullOrEmpty(reason) ? "Pengeluaran Umum" : reason)} | Sisa Kas: {FormatCurrency(currentMoney)}");

            OnMoneyChanged?.Invoke(currentMoney, -amount);
            RefreshNetWorth();
            return true;
        }
        #endregion

        #region Operasi Lahan
        /// <summary>
        /// Menambahkan persentase kepemilikan lahan (maksimal 100%).
        /// </summary>
        /// <param name="percentageToAdd">Persentase yang ditambahkan (misal 2.5f)</param>
        public void AddLandPercentage(float percentageToAdd)
        {
            if (percentageToAdd <= 0) return;

            float previous = currentLandPercentage;
            currentLandPercentage = Mathf.Clamp(currentLandPercentage + percentageToAdd, 0f, 100f);
            float delta = currentLandPercentage - previous;

            Debug.Log($"[EconomyManager] Kepemilikan lahan bertambah +{delta:F1}%. Total saat ini: {currentLandPercentage:F1}%");

            OnLandPercentageChanged?.Invoke(currentLandPercentage, delta);
            RefreshNetWorth();

            if (currentLandPercentage >= 100f)
            {
                Debug.Log("[EconomyManager] LUAR BIASA! Seluruh lahan pulau sawit (100%) berhasil dikuasai!");
            }
        }

        /// <summary>
        /// Membeli lahan baru dengan biaya tertentu.
        /// </summary>
        public bool PurchaseLand(float percentageToAdd, double cost)
        {
            if (currentLandPercentage >= 100f)
            {
                Debug.LogWarning("[EconomyManager] Lahan sudah 100%, tidak bisa membeli lagi.");
                return false;
            }

            if (!SpendMoney(cost, $"Beli Lahan Sawit +{percentageToAdd:F1}%"))
            {
                return false;
            }

            AddLandPercentage(percentageToAdd);
            return true;
        }

        /// <summary>
        /// Mengatur langsung persentase lahan (misal saat load savegame).
        /// </summary>
        public void SetLandPercentage(float targetPercentage)
        {
            float previous = currentLandPercentage;
            currentLandPercentage = Mathf.Clamp(targetPercentage, 0f, 100f);
            float delta = currentLandPercentage - previous;

            OnLandPercentageChanged?.Invoke(currentLandPercentage, delta);
            RefreshNetWorth();
        }

        /// <summary>
        /// Mengatur langsung saldo kas (biasanya dipanggil oleh SaveManager saat Load).
        /// </summary>
        public void SetMoney(double amount)
        {
            currentMoney = Math.Max(0, amount);
            OnMoneyChanged?.Invoke(currentMoney, 0);
            RefreshNetWorth();
        }

        /// <summary>
        /// Memuat state ekonomi dari save data.
        /// </summary>
        public void LoadState(double money, float landPct, double otherAssets)
        {
            currentMoney = Math.Max(0, money);
            currentLandPercentage = Mathf.Clamp(landPct, 0f, 100f);
            otherAssetsValuation = Math.Max(0, otherAssets);

            // Reset flag kebangkrutan saat memuat data save
            hasTriggeredBankruptcy = false;

            OnMoneyChanged?.Invoke(currentMoney, 0);
            OnLandPercentageChanged?.Invoke(currentLandPercentage, 0);
            RefreshNetWorth();
        }
        #endregion

        #region Perhitungan Net Worth & Deteksi Kebangkrutan
        /// <summary>
        /// Menghitung total valuasi kekayaan pemain:
        /// Kas Tunai + (Persentase Lahan * Valuasi per 1%) + Aset Lainnya - Total Hutang.
        /// </summary>
        public double GetNetWorth()
        {
            double landValue = currentLandPercentage * landValuationPerPercent;
            double totalDebt = LoanManager.Instance != null ? LoanManager.Instance.TotalDebt : 0;
            return (currentMoney + landValue + otherAssetsValuation) - totalDebt;
        }

        /// <summary>
        /// Mengevaluasi apakah Net Worth pemain berada di bawah ambang batas kebangkrutan.
        /// Hanya memicu event OnBankruptcy SEKALI jika ambang batas dilewati.
        /// </summary>
        public void CheckBankruptcy()
        {
            if (!hasTriggeredBankruptcy && GetNetWorth() <= bankruptcyThreshold)
            {
                hasTriggeredBankruptcy = true;
                Debug.LogError($"[EconomyManager] KEBANGKRUTAN TERJADI! Net Worth ({FormatCurrency(GetNetWorth())}) telah jatuh di bawah batas minimum ({FormatCurrency(bankruptcyThreshold)}).");
                OnBankruptcy?.Invoke();
            }
        }

        /// <summary>
        /// Memperbarui event OnNetWorthChanged dan mengevaluasi status kebangkrutan.
        /// </summary>
        public void RefreshNetWorth()
        {
            OnNetWorthChanged?.Invoke(GetNetWorth());
            CheckBankruptcy();
        }

        /// <summary>
        /// Mereset flag kebangkrutan (dipanggil saat Load Game atau Game Restart).
        /// </summary>
        public void ResetBankruptcyState(bool forceState = false)
        {
            hasTriggeredBankruptcy = forceState;
        }

        /// <summary>
        /// Mengubah nilai buku aset mesin/fasilitas tambahan dan merefresh Net Worth.
        /// </summary>
        public void SetOtherAssetsValuation(double value)
        {
            otherAssetsValuation = Math.Max(0, value);
            RefreshNetWorth();
        }
        #endregion

        #region Currency Formatting Helper
        /// <summary>
        /// Memformat angka uang ke format standar Rupiah Indonesia (contoh: "Rp 15.000.000").
        /// </summary>
        public static string FormatCurrency(double amount)
        {
            var culture = new CultureInfo("id-ID");
            return string.Format(culture, "Rp {0:N0}", amount);
        }

        /// <summary>
        /// Memformat angka uang ke format singkat untuk UI sempit (contoh: "Rp 15,2 Jt" atau "Rp 1,5 M").
        /// </summary>
        public static string FormatCurrencyCompact(double amount)
        {
            if (amount >= 1_000_000_000_000)
                return $"Rp {(amount / 1_000_000_000_000):F2} T"; // Triliun
            if (amount >= 1_000_000_000)
                return $"Rp {(amount / 1_000_000_000):F2} M"; // Miliar
            if (amount >= 1_000_000)
                return $"Rp {(amount / 1_000_000):F1} Jt"; // Juta
            if (amount >= 1_000)
                return $"Rp {(amount / 1_000):F0} Rb"; // Ribu

            return $"Rp {amount:F0}";
        }
        #endregion
    }
}
