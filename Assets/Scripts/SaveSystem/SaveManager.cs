using System;
using System.IO;
using UnityEngine;
using TanamSawit.Managers;
using TanamSawit.Buildings;
using TanamSawit.Environment;

namespace TanamSawit.SaveSystem
{
    /// <summary>
    /// SaveManager v2: Multi-slot JSON save system with versioning and migration.
    /// 3 manual slots (0-2) + 1 autosave slot (99).
    /// Saves player position, current area, game speed, and play time.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Konfigurasi Penyimpanan")]
        [Tooltip("Nama file save lama (single-slot) untuk migrasi otomatis.")]
        [SerializeField] private string legacySaveFileName = "tanamsawit_save.json";

        [Tooltip("Otomatis simpan data saat berganti bulan.")]
        [SerializeField] private bool enableAutoSaveMonthly = true;

        [Tooltip("Otomatis simpan saat game ditutup.")]
        [SerializeField] private bool autoSaveOnApplicationQuit = true;

        [Header("Status Simpan Terakhir")]
        [SerializeField] private string lastSaveTime = "-";
        public string LastSaveTime => lastSaveTime;

        #region Constants
        public const int ManualSlotCount = 3;
        private const int AutoSaveSlot = 99;
        #endregion

        #region Events
        public event Action<string> OnSaveStatusChanged;
        #endregion

        private int lastUsedSlot = 0;
        private float accumulatedPlayTime = 0f;
        private float sessionStartTime = 0f;
        private bool legacyImported = false;

        private static string GetSlotPath(int slot)
        {
            string fileName = slot == AutoSaveSlot ? "save_autosave.json" : $"save_slot{slot}.json";
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        private string LegacyFilePath => Path.Combine(Application.persistentDataPath, legacySaveFileName);

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
            sessionStartTime = Time.time;

            // Auto-import legacy single-slot save into slot 0 on first run
            TryImportLegacySave();

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
                SaveGame(lastUsedSlot);
            }
        }

        #region Legacy Import
        /// <summary>
        /// Auto-imports the old single-slot save file into slot 0 on first run.
        /// </summary>
        private void TryImportLegacySave()
        {
            if (legacyImported) return;
            legacyImported = true;

            if (!File.Exists(LegacyFilePath)) return;
            if (File.Exists(GetSlotPath(0))) return; // slot 0 already exists, don't overwrite

            try
            {
                File.Copy(LegacyFilePath, GetSlotPath(0));
                Debug.Log($"[SaveManager] Legacy save imported to slot 0: {GetSlotPath(0)}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Failed to import legacy save: {ex.Message}");
            }
        }
        #endregion

        #region Migration
        /// <summary>
        /// Migrates old save data to the current version. Only adds fields; never removes.
        /// </summary>
        private GameSaveData Migrate(GameSaveData data)
        {
            if (data.saveVersion < 2)
            {
                // v1 -> v2: fill new fields with defaults
                data.currentAreaId = data.currentAreaId ?? "kebun";
                data.playerPosX = data.playerPosX; // 0 = Kebun spawn
                data.playerPosY = data.playerPosY;
                data.gameSpeedIndex = data.gameSpeedIndex == 0 ? (int)GameSpeed.Normal : data.gameSpeedIndex;
                data.factoryDamaged = data.factoryDamaged;
                data.landCount = data.landCount;
                data.maxWorkerCapacity = data.maxWorkerCapacity == 0 ? 5 : data.maxWorkerCapacity;
                data.playTimeSeconds = data.playTimeSeconds;
                data.saveSlotName = data.saveSlotName ?? $"Slot {lastUsedSlot + 1}";
                data.saveVersion = 2;
                Debug.Log("[SaveManager] Migrasi save v1 -> v2");
            }
            return data;
        }
        #endregion

        #region Operasi Save
        /// <summary>
        /// Parameterless save — delegates to last-used manual slot (default 0).
        /// </summary>
        public void SaveGame()
        {
            SaveGame(lastUsedSlot);
        }

        /// <summary>
        /// Legacy bool overload for backwards compatibility.
        /// isAutoSave=true → autosave slot; isAutoSave=false → last-used manual slot.
        /// </summary>
        public void SaveGame(bool isAutoSave)
        {
            SaveGame(isAutoSave ? AutoSaveSlot : lastUsedSlot);
        }

        /// <summary>
        /// Saves game state to a specific slot index.
        /// </summary>
        public void SaveGame(int slot)
        {
            try
            {
                GameSaveData data = new GameSaveData();

                // 1. Metadata
                data.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                data.saveVersion = 2;
                data.saveSlotName = slot == AutoSaveSlot ? "Autosave" : $"Slot {slot + 1}";
                data.playTimeSeconds = accumulatedPlayTime + (Time.time - sessionStartTime);
                lastSaveTime = data.saveTimestamp;

                // 2. Kalender & Waktu
                if (TimeManager.Instance != null)
                {
                    data.currentDay = TimeManager.Instance.CurrentDay;
                    data.currentMonth = TimeManager.Instance.CurrentMonth;
                    data.currentYear = TimeManager.Instance.CurrentYear;
                    data.gameSpeedIndex = (int)TimeManager.Instance.CurrentGameSpeed;
                }

                // 3. Posisi Pemain & Area
                if (AreaTransitionManager.Instance != null)
                {
                    data.currentAreaId = AreaTransitionManager.Instance.CurrentAreaId;
                }
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    data.playerPosX = player.transform.position.x;
                    data.playerPosY = player.transform.position.y;
                }

                // 4. Keuangan & Lahan
                if (EconomyManager.Instance != null)
                {
                    data.currentMoney = EconomyManager.Instance.CurrentMoney;
                    data.savedNetWorth = EconomyManager.Instance.GetNetWorth();
                    data.currentLandPercentage = EconomyManager.Instance.CurrentLandPercentage;
                }

                // 5. Hutang & Properti Kos
                if (LoanManager.Instance != null)
                {
                    data.bankDebt = LoanManager.Instance.BankDebt;
                    data.pinjolDebt = LoanManager.Instance.PinjolDebt;
                    data.rentenirDebt = LoanManager.Instance.RentenirDebt;
                    data.ownedBoardingHouses = LoanManager.Instance.OwnedBoardingHouses;
                }

                // 6. Pekerja & Hasil Kebun
                if (WorkerManager.Instance != null)
                {
                    data.workers = new System.Collections.Generic.List<Worker>(WorkerManager.Instance.Workers);
                    data.tbsStockTon = WorkerManager.Instance.TbsStockTon;
                    data.cpoStockTon = WorkerManager.Instance.CpoStockTon;
                }

                // 7. Bangunan & Pabrik (BuildingManager owns factory state)
                if (BuildingManager.Instance != null)
                {
                    data.hasFactory = BuildingManager.Instance.HasFactory;
                    data.factoryDamaged = BuildingManager.Instance.IsFactoryDamaged;
                    data.maxWorkerCapacity = BuildingManager.Instance.MaxWorkerCapacity;
                }

                // 8. Karma Ekologi
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

                // 9. Sepupu Rival
                if (RivalManager.Instance != null)
                {
                    data.cousinNetWorth = RivalManager.Instance.CousinCurrentNetWorth;
                    data.finalEnding = (int)RivalManager.Instance.FinalEnding;
                }

                // Serialisasi ke format JSON
                string json = JsonUtility.ToJson(data, true);

                // Tulis ke file
                string filePath = GetSlotPath(slot);
                File.WriteAllText(filePath, json);

                // Track last-used slot for manual saves
                if (slot != AutoSaveSlot)
                {
                    lastUsedSlot = slot;
                }

                // Update accumulated play time
                accumulatedPlayTime = data.playTimeSeconds;
                sessionStartTime = Time.time;

                string typePrefix = slot == AutoSaveSlot ? "[AUTO-SAVE]" : $"[MANUAL-SAVE Slot {slot + 1}]";
                string statusMsg = $"{typePrefix} Data berhasil disimpan ke JSON ({lastSaveTime})";
                Debug.Log($"<color=#00FF66>{statusMsg}</color>\nPath: {filePath}");
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
        /// Parameterless load — delegates to last-used manual slot (default 0).
        /// </summary>
        public bool LoadGame()
        {
            return LoadGame(lastUsedSlot);
        }

        /// <summary>
        /// Loads game state from a specific slot index.
        /// </summary>
        public bool LoadGame(int slot)
        {
            if (!HasSaveFile(slot))
            {
                string noFileMsg = $"[LOAD] Tidak ditemukan file savegame di slot {slot + 1}!";
                Debug.LogWarning(noFileMsg);
                OnSaveStatusChanged?.Invoke(noFileMsg);
                return false;
            }

            try
            {
                string json = File.ReadAllText(GetSlotPath(slot));
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

                if (data == null)
                {
                    Debug.LogError("[LOAD] Data JSON rusak atau kosong!");
                    return false;
                }

                // Migrate old save data
                data = Migrate(data);

                // Track slot
                lastUsedSlot = slot;

                // 1. Pulihkan Waktu
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.SetDate(data.currentDay, data.currentMonth, data.currentYear);
                }

                // 2. Pulihkan Hutang & Kos-kosan
                if (LoanManager.Instance != null)
                {
                    LoanManager.Instance.LoadState(data.bankDebt, data.pinjolDebt, data.rentenirDebt, data.ownedBoardingHouses);
                }

                // 3. Pulihkan Ekonomi & Lahan
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

                // 7. Pulihkan Posisi Pemain & Area
                string areaId = !string.IsNullOrEmpty(data.currentAreaId) ? data.currentAreaId : "kebun";
                Vector3 playerPos = new Vector3(data.playerPosX, data.playerPosY, 0f);

                // Use area center as fallback if player position is zero/unset
                if (data.playerPosX == 0f && data.playerPosY == 0f && AreaTransitionManager.AreaCenters.TryGetValue(areaId, out var center))
                {
                    playerPos = center;
                }

                if (AreaTransitionManager.Instance != null)
                {
                    AreaTransitionManager.Instance.TeleportToArea(areaId, playerPos, skipFade: true);
                }

                // 8. Pulihkan Game Speed
                if (TimeManager.Instance != null && data.gameSpeedIndex > 0)
                {
                    TimeManager.Instance.SetSpeed((GameSpeed)data.gameSpeedIndex);
                }

                // 9. Pulihkan Play Time
                accumulatedPlayTime = data.playTimeSeconds;
                sessionStartTime = Time.time;

                // Pulihkan state ke Playing jika save data masih sehat
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
                {
                    if (EconomyManager.Instance == null || !EconomyManager.Instance.HasTriggeredBankruptcy)
                    {
                        GameManager.Instance.ChangeState(GameState.Playing);
                    }
                }

                lastSaveTime = data.saveTimestamp;
                string statusMsg = $"[LOAD SUKSES] Data savegame slot {slot + 1} ({lastSaveTime}) berhasil dipulihkan!";
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

        #region Slot Summaries
        /// <summary>
        /// Returns summaries of all save slots (3 manual + 1 autosave) for UI display.
        /// Tolerates missing/corrupt files.
        /// </summary>
        public SaveSlotSummary[] GetSlotSummaries()
        {
            var summaries = new SaveSlotSummary[ManualSlotCount + 1];

            for (int i = 0; i < ManualSlotCount; i++)
            {
                summaries[i] = ReadSlotSummary(i, i);
            }
            summaries[ManualSlotCount] = ReadSlotSummary(AutoSaveSlot, ManualSlotCount);

            return summaries;
        }

        private SaveSlotSummary ReadSlotSummary(int slot, int index)
        {
            var summary = new SaveSlotSummary
            {
                SlotIndex = index,
                Exists = false,
                SlotName = slot == AutoSaveSlot ? "Autosave" : $"Slot {slot + 1}"
            };

            try
            {
                string path = GetSlotPath(slot);
                if (!File.Exists(path)) return summary;

                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null) return summary;

                summary.Exists = true;
                summary.SaveVersion = data.saveVersion;
                summary.RealWorldTimestamp = data.saveTimestamp ?? "-";
                summary.InGameYear = data.currentYear;
                summary.InGameMonth = data.currentMonth;
                summary.InGameDay = data.currentDay;
                summary.NetWorth = data.savedNetWorth;
                summary.PlayTimeSeconds = data.playTimeSeconds;
            }
            catch
            {
                // Corrupt file — treat as empty slot
                summary.Exists = false;
            }

            return summary;
        }
        #endregion

        #region Helper Functions
        /// <summary>
        /// Cek apakah file savegame tersedia di slot tertentu (default slot 0).
        /// </summary>
        public bool HasSaveFile()
        {
            return HasSaveFile(0);
        }

        public bool HasSaveFile(int slot)
        {
            return File.Exists(GetSlotPath(slot));
        }

        /// <summary>
        /// Menghapus file savegame di slot tertentu (default slot 0).
        /// </summary>
        public void DeleteSaveFile()
        {
            DeleteSaveFile(0);
        }

        public void DeleteSaveFile(int slot)
        {
            string path = GetSlotPath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
                string msg = $"[SAVE] File penyimpanan slot {slot + 1} berhasil dihapus.";
                Debug.Log(msg);
                OnSaveStatusChanged?.Invoke(msg);
            }
        }

        /// <summary>
        /// Mendapatkan path file penyimpanan lokal di OS pemain (slot 0).
        /// </summary>
        public string GetSaveFilePath()
        {
            return GetSlotPath(0);
        }

        public string GetSaveFilePath(int slot)
        {
            return GetSlotPath(slot);
        }

        private void HandleAutoSaveOnMonthPassed(int month, int year)
        {
            Debug.Log($"[SaveManager] Pergantian bulan ({month}/{year}) tercapai -> Melakukan Auto-Save...");
            SaveGame(AutoSaveSlot);
        }
        #endregion
    }
}
