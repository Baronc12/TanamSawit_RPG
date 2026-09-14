using System;
using UnityEngine;
using TanamSawit.Managers;

namespace TanamSawit.Buildings
{
    /// <summary>
    /// BuildingManager mengatur pembangunan infrastruktur:
    /// 1. Perluasan Lahan (Meningkatkan % lahan dan kapasitas pekerja).
    /// 2. Pabrik Pengolahan Sawit (Memberikan multiplier pendapatan panen pekerja).
    /// 3. Yayasan Pendidikan CSR (Memberikan bonus stat Intelligence pada pekerja baru).
    /// </summary>
    [DefaultExecutionOrder(-65)]
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Konfigurasi Pembelian Lahan")]
        [Tooltip("Biaya per 2.5% pembelian lahan.")]
        [SerializeField] private double landPlotCost = 30_000_000; // Rp 30 Jt
        [SerializeField] private float landPercentageIncrement = 2.5f;
        [SerializeField] private int workerCapacityIncrement = 2;

        [Header("Kapasitas Pekerja")]
        [SerializeField] private int maxWorkerCapacity = 5; // Awal kapasitas 5 pekerja
        public int MaxWorkerCapacity => maxWorkerCapacity;

        [Header("Pabrik Pengolahan Sawit")]
        [Tooltip("Biaya pembangunan pabrik CPO.")]
        [SerializeField] private double factoryCost = 150_000_000; // Rp 150 Jt
        [SerializeField] private bool hasFactory = false;
        [SerializeField] private bool isFactoryDamaged = false;
        [Tooltip("Pengali pendapatan panen saat pabrik beroperasi normal.")]
        [SerializeField] private double factoryMultiplierValue = 2.2; // 2.2x lipat
        [Tooltip("Biaya perbaikan pabrik jika dirusak gajah.")]
        [SerializeField] private double factoryRepairCost = 25_000_000;

        public bool HasFactory => hasFactory;
        public bool IsFactoryDamaged => isFactoryDamaged;
        public double FactoryMultiplier => (hasFactory && !isFactoryDamaged) ? factoryMultiplierValue : 1.0;

        [Header("Yayasan Pendidikan Desa (CSR)")]
        [Tooltip("Biaya pembangunan Yayasan CSR.")]
        [SerializeField] private double foundationCost = 80_000_000; // Rp 80 Jt
        [SerializeField] private bool hasFoundation = false;
        public bool HasFoundation => hasFoundation;

        public event Action<string> OnBuildingEventTriggered;

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

        #region Fungsi Utama Sesuai Prompt
        /// <summary>
        /// Membeli plot lahan baru: Memotong uang, menambah % lahan, dan menaikkan kuota pekerja.
        /// </summary>
        public bool BuyLandPlot()
        {
            if (EconomyManager.Instance == null) return false;

            if (EconomyManager.Instance.CurrentLandPercentage >= 100f)
            {
                Notify("[LAHAN] Lahan sudah mencapai 100%! Tidak bisa memperluas lagi.");
                return false;
            }

            if (!EconomyManager.Instance.SpendMoney(landPlotCost, $"Perluasan Lahan +{landPercentageIncrement:F1}%"))
            {
                Notify($"[LAHAN] Saldo tidak cukup untuk membeli lahan ({EconomyManager.FormatCurrency(landPlotCost)}).");
                return false;
            }

            EconomyManager.Instance.AddLandPercentage(landPercentageIncrement);
            maxWorkerCapacity += workerCapacityIncrement;

            string msg = $"[LAHAN] Sukses membeli lahan! Lahan bertambah +{landPercentageIncrement}%. Kuota pekerja naik jadi {maxWorkerCapacity}.";
            Notify(msg);
            return true;
        }

        /// <summary>
        /// Membangun Pabrik Pengolahan Sawit (Multiplier pendapatan panen).
        /// </summary>
        public bool BuildFactory()
        {
            if (hasFactory)
            {
                Notify("[PABRIK] Anda sudah memiliki Pabrik Sawit!");
                return false;
            }

            if (EconomyManager.Instance == null || !EconomyManager.Instance.SpendMoney(factoryCost, "Pembangunan Pabrik CPO"))
            {
                Notify($"[PABRIK] Uang kas tidak cukup untuk membangun pabrik ({EconomyManager.FormatCurrency(factoryCost)}).");
                return false;
            }

            hasFactory = true;
            isFactoryDamaged = false;
            EconomyManager.Instance.SetOtherAssetsValuation(factoryCost);

            string msg = $"[PABRIK] Pabrik Sawit beroperasi! Pendapatan panen pekerja kini berlipat ganda ({factoryMultiplierValue}x)!";
            Notify(msg);
            return true;
        }

        /// <summary>
        /// Memperbaiki pabrik jika dirusak oleh serangan gajah.
        /// </summary>
        public bool RepairFactory()
        {
            if (!hasFactory || !isFactoryDamaged) return false;

            if (EconomyManager.Instance == null || !EconomyManager.Instance.SpendMoney(factoryRepairCost, "Perbaikan Kerusakan Pabrik"))
            {
                Notify($"[PABRIK] Uang kas tidak cukup untuk perbaikan ({EconomyManager.FormatCurrency(factoryRepairCost)}).");
                return false;
            }

            isFactoryDamaged = false;
            string msg = "[PABRIK] Pabrik berhasil diperbaiki dan kembali beroperasi penuh!";
            Notify(msg);
            return true;
        }

        /// <summary>
        /// Dipanggil oleh EnvironmentalKarmaManager saat Event Gajah merusak pabrik.
        /// </summary>
        public void DamageFactory()
        {
            if (hasFactory && !isFactoryDamaged)
            {
                isFactoryDamaged = true;
                string msg = "[BENCANA GAJAH] Pabrik Sawit dirusak kawanan gajah! Multiplier mati hingga diperbaiki.";
                Debug.LogError(msg);
                Notify(msg);
            }
        }

        /// <summary>
        /// Membangun Yayasan Pendidikan (Bonus stat Intelligence pada pekerja baru).
        /// </summary>
        public bool BuildFoundation()
        {
            if (hasFoundation)
            {
                Notify("[YAYASAN] Yayasan Pendidikan CSR sudah berdiri di desa!");
                return false;
            }

            if (EconomyManager.Instance == null || !EconomyManager.Instance.SpendMoney(foundationCost, "Pembangunan Yayasan CSR"))
            {
                Notify($"[YAYASAN] Uang tidak cukup untuk mendanai yayasan ({EconomyManager.FormatCurrency(foundationCost)}).");
                return false;
            }

            hasFoundation = true;
            string msg = "[YAYASAN] Yayasan Pendidikan Desa diresmikan! Semua pekerja baru akan mendapat bonus Intelligence +10!";
            Notify(msg);
            return true;
        }
        #endregion

        #region Save/Load
        /// <summary>
        /// Restores building/factory state from save data.
        /// </summary>
        public void LoadState(bool savedHasFactory, bool savedIsFactoryDamaged, int savedLandCount, int savedMaxWorkerCapacity)
        {
            hasFactory = savedHasFactory;
            isFactoryDamaged = savedIsFactoryDamaged;
            maxWorkerCapacity = Mathf.Max(1, savedMaxWorkerCapacity);

            if (hasFactory && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SetOtherAssetsValuation(factoryCost);
            }
        }
        #endregion

        private void Notify(string msg)
        {
            Debug.Log(msg);
            OnBuildingEventTriggered?.Invoke(msg);
        }
    }
}
