using System;
using UnityEngine;

namespace TanamSawit.Managers
{
    public enum LoanType
    {
        BankKonvensional,
        Pinjol,
        RentenirMadura
    }

    /// <summary>
    /// LoanManager mengelola 3 jenis pinjaman: Bank Konvensional, Pinjol, dan Rentenir Madura.
    /// Terhubung langsung dengan TimeManager (OnDayPassed) untuk memotong cicilan/bunga harian otomatis,
    /// serta memicu Teror Debt Collector jika uang pemain tidak cukup saat jatuh tempo Pinjol.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class LoanManager : MonoBehaviour
    {
        public static LoanManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Saldo Hutang Aktif (Live)")]
        [SerializeField] private double bankDebt = 0;
        [SerializeField] private double pinjolDebt = 0;
        [SerializeField] private double rentenirDebt = 0;

        public double TotalDebt => bankDebt + pinjolDebt + rentenirDebt;
        public double BankDebt => bankDebt;
        public double PinjolDebt => pinjolDebt;
        public double RentenirDebt => rentenirDebt;

        [Header("Suku Bunga Harian")]
        [Tooltip("Bunga harian Bank kecil (0.05% per hari).")]
        [SerializeField] private float bankDailyRate = 0.0005f;

        [Tooltip("Bunga harian Pinjol mencekik (2.0% per hari dan berlipat ganda).")]
        [SerializeField] private float pinjolDailyRate = 0.02f;

        [Tooltip("Bunga harian Rentenir Madura (1.0% per hari).")]
        [SerializeField] private float rentenirDailyRate = 0.01f;

        [Header("Properti Kos-kosan (Passive Income)")]
        [SerializeField] private int ownedBoardingHouses = 0;
        [SerializeField] private double incomePerBoardingHouse = 1_500_000;
        [SerializeField] private double costPerBoardingHouse = 75_000_000;
        public int OwnedBoardingHouses => ownedBoardingHouses;

        public event Action<string> OnLoanEventTriggered;

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
            // Sambungkan ke TimeManager: Setiap hari berganti, cicilan/bunga diproses
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed += HandleDailyLoanProcessing;
                TimeManager.Instance.OnMonthPassed += HandleMonthlyPassiveIncome;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed -= HandleDailyLoanProcessing;
                TimeManager.Instance.OnMonthPassed -= HandleMonthlyPassiveIncome;
            }
        }

        #region Pengambilan Pinjaman
        /// <summary>
        /// Mengajukan pinjaman Bank Konvensional (Bunga harian rendah, cicilan ringan).
        /// </summary>
        public bool TakeBankLoan(double amount = 50_000_000)
        {
            if (EconomyManager.Instance == null) return false;

            bankDebt += amount;
            EconomyManager.Instance.AddMoney(amount, "Pinjaman Bank Konvensional");

            string msg = $"[BANK] Pinjaman Bank disetujui: +{EconomyManager.FormatCurrency(amount)}. Cicilan harian rendah.";
            Notify(msg);
            return true;
        }

        /// <summary>
        /// Mengajukan Pinjol (Cair instan, bunga harian mencekik & berlipat ganda).
        /// </summary>
        public void TakePinjol(double amount = 20_000_000)
        {
            if (EconomyManager.Instance == null) return;

            pinjolDebt += amount;
            EconomyManager.Instance.AddMoney(amount, "Pencairan Pinjol Instan");

            string msg = $"[PINJOL] Dana instan cair: +{EconomyManager.FormatCurrency(amount)}! Hati-hati bunga harian tinggi dan teror debt collector.";
            Notify(msg);
        }

        /// <summary>
        /// Mengajukan pinjaman Rentenir Madura.
        /// </summary>
        public void TakePinjamanMadura(double amount = 30_000_000)
        {
            if (EconomyManager.Instance == null) return;

            rentenirDebt += amount;
            EconomyManager.Instance.AddMoney(amount, "Pinjaman Tunai Rentenir");

            string msg = $"[RENTENIR] Pinjaman tunai diterima: +{EconomyManager.FormatCurrency(amount)}. Jangan sampai gagal bayar!";
            Notify(msg);
        }

        // Aliases untuk kompatibilitas penuh dengan UI
        public bool BorrowBank(double amount) => TakeBankLoan(amount);
        public void BorrowPinjol(double amount) => TakePinjol(amount);
        public void BorrowRentenirMadura(double amount) => TakePinjamanMadura(amount);
        #endregion

        #region Pelunasan Hutang
        public bool RepayDebt(LoanType type, double amount)
        {
            if (EconomyManager.Instance == null || !EconomyManager.Instance.CanAfford(amount))
            {
                Notify("[HUTANG] Saldo kas tidak cukup untuk melunasi cicilan.");
                return false;
            }

            switch (type)
            {
                case LoanType.BankKonvensional:
                    if (bankDebt <= 0) return false;
                    amount = Math.Min(amount, bankDebt);
                    if (EconomyManager.Instance.SpendMoney(amount, "Bayar Hutang Bank")) bankDebt -= amount;
                    break;
                case LoanType.Pinjol:
                    if (pinjolDebt <= 0) return false;
                    amount = Math.Min(amount, pinjolDebt);
                    if (EconomyManager.Instance.SpendMoney(amount, "Bayar Hutang Pinjol")) pinjolDebt -= amount;
                    break;
                case LoanType.RentenirMadura:
                    if (rentenirDebt <= 0) return false;
                    amount = Math.Min(amount, rentenirDebt);
                    if (EconomyManager.Instance.SpendMoney(amount, "Bayar Rentenir")) rentenirDebt -= amount;
                    break;
            }

            string msg = $"[HUTANG] Pembayaran {EconomyManager.FormatCurrency(amount)} berhasil. Sisa hutang: {EconomyManager.FormatCurrency(TotalDebt)}";
            Notify(msg);
            return true;
        }
        #endregion

        #region Pemrosesan Harian & Teror Pinjol
        /// <summary>
        /// Dipanggil setiap hari berganti oleh TimeManager.
        /// </summary>
        private void HandleDailyLoanProcessing(int day, int month, int year)
        {
            if (EconomyManager.Instance == null) return;

            // 1. Proses Bunga Bank
            if (bankDebt > 0)
            {
                double dailyInterest = bankDebt * bankDailyRate;
                bankDebt += dailyInterest;

                // Potong cicilan harian ringan (1% dari pokok per hari)
                double dailyPayment = Math.Min(bankDebt, 500_000);
                if (EconomyManager.Instance.CanAfford(dailyPayment))
                {
                    EconomyManager.Instance.SpendMoney(dailyPayment, "Cicilan Harian Bank");
                    bankDebt -= dailyPayment;
                }
            }

            // 2. Proses Bunga Pinjol (Mencekik & Berlipat Ganda)
            if (pinjolDebt > 0)
            {
                double interest = pinjolDebt * pinjolDailyRate;
                pinjolDebt += interest; // Bunga berbunga

                // Tagihan harian pinjol
                double requiredDailyCut = pinjolDebt * 0.05; // 5% pokok ditagih tiap hari

                if (EconomyManager.Instance.CanAfford(requiredDailyCut))
                {
                    EconomyManager.Instance.SpendMoney(requiredDailyCut, "Potongan Harian Pinjol");
                    pinjolDebt -= requiredDailyCut;
                }
                else
                {
                    // Uang tidak cukup / minus saat ditagih pinjol!
                    TriggerPinjolTerror();
                }
            }

            // 3. Proses Rentenir Madura
            if (rentenirDebt > 0)
            {
                double rentenirInterest = rentenirDebt * rentenirDailyRate;
                rentenirDebt += rentenirInterest;
            }

            // Segarkan Net Worth setelah akumulasi bunga harian
            EconomyManager.Instance?.RefreshNetWorth();
        }

        /// <summary>
        /// Memicu Teror Debt Collector Pinjol jika pemain gagal membayar tagihan harian.
        /// Mengurangi stamina dan moral pekerja secara instan!
        /// </summary>
        public void TriggerPinjolTerror()
        {
            string msg = "[TEROR PINJOL] Gagal bayar tagihan pinjol! Debt collector menyerbu kebun & mess buruh! Seluruh pekerja panik dan stres berat!";
            Debug.LogError(msg);
            Notify(msg);

            // Berdampak ke stamina pekerja di WorkerManager
            WorkerManager.Instance?.ApplyPinjolTerrorToWorkers(30f);
        }

        private void HandleMonthlyPassiveIncome(int month, int year)
        {
            if (ownedBoardingHouses > 0 && EconomyManager.Instance != null)
            {
                double income = ownedBoardingHouses * incomePerBoardingHouse;
                EconomyManager.Instance.AddMoney(income, $"Passive Income Kos ({ownedBoardingHouses} Unit)");
            }
        }
        #endregion

        #region Properti Kos-kosan
        public bool BuyBoardingHouse()
        {
            if (EconomyManager.Instance == null || !EconomyManager.Instance.SpendMoney(costPerBoardingHouse, "Beli Kos-kosan"))
                return false;

            ownedBoardingHouses++;
            EconomyManager.Instance.SetOtherAssetsValuation(ownedBoardingHouses * costPerBoardingHouse);
            Notify($"[KOS] Bangun 1 unit Kos-kosan berhasil! Total unit: {ownedBoardingHouses}.");
            return true;
        }
        #endregion

        private void Notify(string msg)
        {
            Debug.Log(msg);
            OnLoanEventTriggered?.Invoke(msg);
        }

        #region Save / Load
        public void LoadState(double bank, double pinjol, double rentenir, int boardingHouses)
        {
            bankDebt = Math.Max(0, bank);
            pinjolDebt = Math.Max(0, pinjol);
            rentenirDebt = Math.Max(0, rentenir);
            ownedBoardingHouses = Math.Max(0, boardingHouses);
        }
        #endregion
    }
}
