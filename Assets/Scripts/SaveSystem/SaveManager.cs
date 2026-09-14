using System;
using System.IO;
using UnityEngine;
using TanamSawit.Managers;
using TanamSawit.Buildings;

namespace TanamSawit.SaveSystem
{
    /// <summary>
    /// SaveManager mengelola serialisasi data progres pemain ke format JSON di disk lokal.
    /// Dilengkapi fitur Manual Save, Manual Load, dan Auto-Save otomatis setiap pergantian bulan.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Konfigurasi Penyimpanan")]
        [Tooltip("Nama file save di persistentDataPath.")]
        [SerializeField] private string saveFileName = "tanamsawit_save.json";

        [Tooltip("Otomatis simpan data saat berganti bulan.")]
        [SerializeField] private bool enableAutoSaveMonthly = true;

        [Tooltip("Otomatis simpan saat game ditutup.")]
        [SerializeField] private bool autoSaveOnApplicationQuit = true;

        [Header("Status Simpan Terakhir")]
        [SerializeField] private string lastSaveTime = "-";
        public string LastSaveTime => lastSaveTime;

        #region Events
        /// <summary>
        /// Dipanggil setiap kali terjadi operasi simpan/muat data dengan pesan status untuk UI.
        /// </summary>
        public event Action<string> OnSaveStatusChanged;
        #endregion

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

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
            // Subscribe ke event pergantian bulan TimeManager untuk Auto-Save
            if (TimeManager.Instance != null && enableAutoSaveMonthly)
            {
                TimeManager.Instance.OnMonthPassed += HandleAutoSaveOnMonthPassed;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMonthPassed -= HandleAutoSaveOnMonthPassed;
            }
        }

        private void OnApplicationQuit()
        {
            if (autoSaveOnApplicationQuit && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                SaveGame(isAutoSave: true);
            }
        }

        #region Operasi Save
        /// <summary>
        /// Mengumpulkan seluruh data dari manager dan menyimpannya ke file JSON.
        /// </summary>
        /// <param name="isAutoSave">True jika dipicu oleh sistem otomatis</param>
        public void SaveGame(bool isAutoSave = false)
        {
            try
            {
                GameSaveData data = new GameSaveData();

                // 1. Metadata
                data.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                lastSaveTime = data.saveTimestamp;

                // 2. Kalender & Waktu
                if (TimeManager.Instance != null)
                {
                    data.currentDay = TimeManager.Instance.CurrentDay;
                    data.currentMonth = TimeManager.Instance.CurrentMonth;
                    data.currentYear = TimeManager.Instance.CurrentYear;
                }

                // 3. Keuangan & Lahan
                if (EconomyManager.Instance != null)
                {
                    data.currentMoney = EconomyManager.Instance.CurrentMoney;
                    data.savedNetWorth = EconomyManager.Instance.GetNetWorth();
                    data.currentLandPercentage = EconomyManager.Instance.CurrentLandPercentage;
                }

                // 4. Hutang & Properti Kos
                if (LoanManager.Instance != null)
                {
                    data.bankDebt = LoanManager.Instance.BankDebt;
                    data.pinjolDebt = LoanManager.Instance.PinjolDebt;
                    data.rentenirDebt = LoanManager.Instance.RentenirDebt;
                    data.ownedBoardingHouses = LoanManager.Instance.OwnedBoardingHouses;
                }

                // 5. Pekerja & Pabrik Sawit
                if (WorkerManager.Instance != null)
                {
                    data.workers = new System.Collections.Generic.List<Worker>(WorkerManager.Instance.Workers);
                    data.tbsStockTon = WorkerManager.Instance.TbsStockTon;
                    data.cpoStockTon = WorkerManager.Instance.CpoStockTon;
                }

                // 5b. Bangunan & Pabrik (BuildingManager owns factory state)
                if (BuildingManager.Instance != null)
                {
                    data.hasFactory = BuildingManager.Instance.HasFactory;
                    data.factoryDamaged = BuildingManager.Instance.IsFactoryDamaged;
                    data.maxWorkerCapacity = BuildingManager.Instance.MaxWorkerCapacity;
                }

                // 6. Karma Ekologi
                if (EnvironmentalKarmaManager.Instance != null)
                {
                    data.karmaLevel = (int)EnvironmentalKarmaManager.Instance.CurrentKarma;
                    data.lastIncidentLog = EnvironmentalKarmaManager.Instance.LastIncidentLog;
                    var flags = EnvironmentalKarmaManager.Instance.GetTriggerFlags();
                    data.triggered75 = flags.t75;
                    data.triggered85 = flags.t85;
                    data.triggered90 = flags.t90;
                    data.triggered100 = flags.t100;
                }

                // 7. Sepupu Rival
                if (RivalManager.Instance != null)
                {
                    data.cousinNetWorth = RivalManager.Instance.CousinCurrentNetWorth;
                    data.finalEnding = (int)RivalManager.Instance.FinalEnding;
                }

                // Serialisasi ke format JSON
                string json = JsonUtility.ToJson(data, true);

                // Tulis ke file persistent disk
                File.WriteAllText(SaveFilePath, json);

                string typePrefix = isAutoSave ? "[AUTO-SAVE]" : "[MANUAL-SAVE]";
                string statusMsg = $"{typePrefix} Data berhasil disimpan ke JSON ({lastSaveTime})";
                Debug.Log($"<color=#00FF66>{statusMsg}</color>\nPath: {SaveFilePath}");
                OnSaveStatusChanged?.Invoke(statusMsg);
            }
            catch (Exception ex)
            {
                string errMsg = $"[SAVE ERROR] Gagal menyimpan data: {ex.Message}";
                Debug.LogError(errMsg);
                OnSaveStatusChanged?.Invoke(errMsg);
            }
        }
        #endregion

        #region Operasi Load
        /// <summary>
        /// Membaca file JSON dari disk dan mendistribusikan datanya kembali ke semua Manager.
        /// </summary>
        /// <returns>True jika file ditemukan dan berhasil dimuat, False jika gagal.</returns>
        public bool LoadGame()
        {
            if (!HasSaveFile())
            {
                string noFileMsg = "[LOAD] Tidak ditemukan file savegame di disk!";
                Debug.LogWarning(noFileMsg);
                OnSaveStatusChanged?.Invoke(noFileMsg);
                return false;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

                if (data == null)
                {
                    Debug.LogError("[LOAD] Data JSON rusak atau kosong!");
                    return false;
                }

                // 1. Pulihkan Waktu
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.SetDate(data.currentDay, data.currentMonth, data.currentYear);
                }

                // 2. Pulihkan Hutang & Kos-kosan terlebih dahulu agar valuasi liabilitas tepat
                if (LoanManager.Instance != null)
                {
                    LoanManager.Instance.LoadState(data.bankDebt, data.pinjolDebt, data.rentenirDebt, data.ownedBoardingHouses);
                }

                // 3. Pulihkan Ekonomi & Lahan (mereset flag kebangkrutan)
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.LoadState(data.currentMoney, data.currentLandPercentage, data.otherAssetsValuation);
                }

                // 4. Pulihkan Pekerja & Pabrik
                if (WorkerManager.Instance != null)
                {
                    WorkerManager.Instance.LoadState(data.workers, data.tbsStockTon, data.cpoStockTon);
                }

                // 4b. Pulihkan Bangunan (BuildingManager owns factory/capacity state)
                if (BuildingManager.Instance != null)
                {
                    BuildingManager.Instance.LoadState(data.hasFactory, data.factoryDamaged, data.landCount, data.maxWorkerCapacity);
                }

                // 5. Pulihkan Karma Ekologi
                if (EnvironmentalKarmaManager.Instance != null)
                {
                    EnvironmentalKarmaManager.Instance.LoadState(
                        (KarmaLevel)data.karmaLevel, 
                        data.lastIncidentLog, 
                        data.triggered75, 
                        data.triggered85, 
                        data.triggered90, 
                        data.triggered100
                    );
                }

                // 6. Pulihkan Sepupu Rival & Reset State Ending
                if (RivalManager.Instance != null)
                {
                    RivalManager.Instance.LoadState(data.cousinNetWorth, (GameEnding)data.finalEnding);
                }

                if (EnvironmentalKarmaManager.Instance != null)
                {
                    EnvironmentalKarmaManager.Instance.ResetEndingStates();
                    EnvironmentalKarmaManager.Instance.RestoreKarmaSideEffects();
                }

                // Pulihkan state ke Playing jika save data masih sehat
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
                {
                    if (EconomyManager.Instance == null || !EconomyManager.Instance.HasTriggeredBankruptcy)
                    {
                        GameManager.Instance.ChangeState(GameState.Playing);
                    }
                }

                lastSaveTime = data.saveTimestamp;
                string statusMsg = $"[LOAD SUKSES] Data savegame ({lastSaveTime}) berhasil dipulihkan!";
                Debug.Log($"<color=#00FF66>{statusMsg}</color>");
                OnSaveStatusChanged?.Invoke(statusMsg);
                return true;
            }
            catch (Exception ex)
            {
                string errMsg = $"[LOAD ERROR] Terjadi kesalahan saat memuat: {ex.Message}";
                Debug.LogError(errMsg);
                OnSaveStatusChanged?.Invoke(errMsg);
                return false;
            }
        }
        #endregion

        #region Helper Functions
        /// <summary>
        /// Cek apakah file savegame tersedia di disk.
        /// </summary>
        public bool HasSaveFile()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Menghapus file savegame (untuk fitur 'Mulai Game Baru dari Nol').
        /// </summary>
        public void DeleteSaveFile()
        {
            if (HasSaveFile())
            {
                File.Delete(SaveFilePath);
                string msg = "[SAVE] File penyimpanan berhasil dihapus.";
                Debug.Log(msg);
                OnSaveStatusChanged?.Invoke(msg);
            }
        }

        /// <summary>
        /// Mendapatkan path file penyimpanan lokal di OS pemain.
        /// </summary>
        public string GetSaveFilePath()
        {
            return SaveFilePath;
        }

        private void HandleAutoSaveOnMonthPassed(int month, int year)
        {
            Debug.Log($"[SaveManager] Pergantian bulan ({month}/{year}) tercapai -> Melakukan Auto-Save...");
            SaveGame(isAutoSave: true);
        }
        #endregion
    }
}
