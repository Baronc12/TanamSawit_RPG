using System;
using System.Collections.Generic;
using UnityEngine;
using TanamSawit.Workers;

namespace TanamSawit.Managers
{
    [System.Serializable]
    public class Worker
    {
        public string workerName;
        [Range(0, 100)] public float stamina = 100f;
        public int experience = 10;
        public int intelligence = 10;

        public bool IsExhausted => stamina < 15f;
    }

    /// <summary>
    /// WorkerManager mengelola pekerja (baik list data maupun WorkerController aktif di scene),
    /// kuota pekerja, stok panen TBS/CPO, serta peristiwa pekerja terluka/hilang.
    /// </summary>
    [DefaultExecutionOrder(-55)]
    public class WorkerManager : MonoBehaviour
    {
        public static WorkerManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Prefab Pekerja di Scene (Opsional)")]
        [SerializeField] private GameObject workerPrefab;
        [SerializeField] private Transform workersContainer;

        [Header("Daftar Pekerja Aktif di Scene")]
        [SerializeField] private List<WorkerController> activeControllers = new List<WorkerController>();
        public List<WorkerController> ActiveControllers => activeControllers;
        public int ActiveWorkersCount => activeControllers.Count;

        [Header("Pekerja Veteran Kakek (Data Backup / Save)")]
        [SerializeField] private List<Worker> workers = new List<Worker>();
        public List<Worker> Workers => workers;

        [Header("Gudang Hasil Kebun")]
        [SerializeField] private float tbsStockTon = 0f;
        [SerializeField] private float cpoStockTon = 0f;

        public float TbsStockTon => tbsStockTon;
        public float CpoStockTon => cpoStockTon;

        [Header("Harga Komoditas")]
        [SerializeField] private double pricePerTonTBS = 2_500_000;
        [SerializeField] private double pricePerTonCPO = 12_000_000;

        [Header("Pabrik Sawit")]
        [SerializeField] private bool hasFactory = false;
        public bool HasFactory => hasFactory;
        [SerializeField] private double factoryBuildCost = 100_000_000;

        public event Action<string> OnWorkerEventTriggered;

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

            if (workersContainer == null)
            {
                GameObject host = new GameObject("--- [WORKERS_CONTAINER] ---");
                host.transform.SetParent(transform);
                workersContainer = host.transform;
            }

            // Inisialisasi 3 pekerja veteran jika kosong
            if (workers.Count == 0)
            {
                workers.Add(new Worker { workerName = "Pak Slamet (Mandor Panen)", stamina = 100f, experience = 25, intelligence = 15 });
                workers.Add(new Worker { workerName = "Pak Joko (Pemanen Tangguh)", stamina = 100f, experience = 20, intelligence = 10 });
                workers.Add(new Worker { workerName = "Mas Asep (Spesialis Angkut)", stamina = 100f, experience = 15, intelligence = 12 });
            }
        }

        private void Start()
        {
            // Temukan pekerja yang mungkin sudah ada di scene
            var existing = FindObjectsByType<WorkerController>();
            activeControllers.AddRange(existing);

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed += HandleDailyStaminaRecovery;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed -= HandleDailyStaminaRecovery;
            }
        }

        #region Rekrutmen & Manajemen Pekerja
        /// <summary>
        /// Merekrut pekerja baru menggunakan ScriptableObject WorkerData.
        /// </summary>
        public bool RecruitWorker(WorkerData data, Vector3 spawnPosition)
        {
            if (data == null) return false;

            // Cek biaya rekrut
            if (EconomyManager.Instance != null && !EconomyManager.Instance.SpendMoney(data.hireCost, $"Rekrut {data.workerTitle}"))
            {
                Debug.LogWarning("[WorkerManager] Uang tidak cukup untuk merekrut pekerja.");
                return false;
            }

            // Cek bonus Yayasan Pendidikan CSR jika ada
            int bonusIntelligence = 0;
            var bmType = Type.GetType("TanamSawit.Buildings.BuildingManager, Assembly-CSharp");
            if (bmType != null)
            {
                var instance = bmType.GetProperty("Instance")?.GetValue(null);
                if (instance != null)
                {
                    bool hasFoundation = (bool)(bmType.GetProperty("HasFoundation")?.GetValue(instance) ?? false);
                    if (hasFoundation) bonusIntelligence = 10;
                }
            }

            // Buat objek visual di scene
            GameObject workerObj;
            if (workerPrefab != null)
            {
                workerObj = Instantiate(workerPrefab, spawnPosition, Quaternion.identity, workersContainer);
            }
            else
            {
                // Fallback procedural capsule/circle
                workerObj = CreateFallbackWorkerVisual(data.workerTitle, spawnPosition, data.workerColor);
            }

            var controller = workerObj.GetComponent<WorkerController>() ?? workerObj.AddComponent<WorkerController>();
            controller.SetWorkerData(data, bonusIntelligence);
            activeControllers.Add(controller);

            // Tambahkan juga ke daftar data serializable
            workers.Add(new Worker
            {
                workerName = data.workerTitle,
                stamina = data.baseStamina,
                experience = data.baseExperience,
                intelligence = data.baseIntelligence + bonusIntelligence
            });

            string msg = $"[REKRUT] Berhasil merekrut {data.workerTitle}! (Bonus Int: +{bonusIntelligence})";
            Debug.Log(msg);
            OnWorkerEventTriggered?.Invoke(msg);
            return true;
        }

        /// <summary>
        /// Menghapus/memakan 1 pekerja secara acak (dipicu oleh Event Macan 90% Lahan).
        /// </summary>
        public bool KillRandomWorker()
        {
            if (activeControllers.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, activeControllers.Count);
                WorkerController victim = activeControllers[idx];
                string victimName = victim != null ? victim.WorkerName : "Pekerja";

                activeControllers.RemoveAt(idx);
                if (victim != null) Destroy(victim.gameObject);

                if (workers.Count > 0)
                {
                    workers.RemoveAt(Mathf.Min(idx, workers.Count - 1));
                }

                string msg = $"[TEROR MACAN] Innalillahi! {victimName} diterkam macan tutul di tengah kebun sawit dan tidak selamat!";
                Debug.LogError(msg);
                OnWorkerEventTriggered?.Invoke(msg);
                return true;
            }
            else if (workers.Count > 0)
            {
                var victim = workers[0];
                workers.RemoveAt(0);
                string msg = $"[TEROR MACAN] {victim.workerName} hilang di hutan sawit dimangsa macan!";
                Debug.LogError(msg);
                OnWorkerEventTriggered?.Invoke(msg);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Menerapkan hukuman stres ke seluruh pekerja saat ditagih Pinjol / Debt Collector.
        /// </summary>
        public void ApplyPinjolTerrorToWorkers(float staminaPenalty = 35f)
        {
            foreach (var w in activeControllers)
            {
                if (w != null) w.ApplyStressPenalty(staminaPenalty);
            }
            foreach (var w in workers)
            {
                w.stamina = Mathf.Max(0, w.stamina - staminaPenalty);
            }
            string msg = "[TEROR DEBT COLLECTOR] Debt collector pinjol meneror mess pekerja! Moral dan stamina pekerja anjlok drastis!";
            Debug.LogWarning(msg);
            OnWorkerEventTriggered?.Invoke(msg);
        }
        #endregion

        #region Operasi TBS & CPO Manual (Dukungan Prototype)
        public bool HarvestTBS()
        {
            int workingCount = 0;
            float totalYield = 0f;

            foreach (var w in workers)
            {
                if (!w.IsExhausted)
                {
                    w.stamina = Mathf.Max(0, w.stamina - 25f);
                    w.experience += 1;
                    totalYield += 2.0f + (w.experience * 0.1f);
                    workingCount++;
                }
            }

            if (workingCount == 0)
            {
                Debug.LogWarning("[BURUH] Semua pekerja kelelahan! Istirahatkan mereka.");
                return false;
            }

            tbsStockTon += totalYield;
            return true;
        }

        public bool SellTBS()
        {
            if (tbsStockTon <= 0) return false;
            double revenue = tbsStockTon * pricePerTonTBS;
            EconomyManager.Instance?.AddMoney(revenue, $"Jual {tbsStockTon:F1} Ton TBS");
            tbsStockTon = 0;
            return true;
        }

        public bool BuildFactory()
        {
            if (hasFactory) return false;
            if (EconomyManager.Instance != null && EconomyManager.Instance.SpendMoney(factoryBuildCost, "Bangun Pabrik CPO"))
            {
                hasFactory = true;
                EconomyManager.Instance.SetOtherAssetsValuation(factoryBuildCost);
                return true;
            }
            return false;
        }

        public bool ProcessTbsToCpo()
        {
            if (!hasFactory || tbsStockTon < 5f) return false;
            float batches = Mathf.Floor(tbsStockTon / 5f);
            tbsStockTon -= batches * 5f;
            cpoStockTon += batches * 1.0f;
            return true;
        }

        public bool SellCPO()
        {
            if (cpoStockTon <= 0) return false;
            double revenue = cpoStockTon * pricePerTonCPO;
            EconomyManager.Instance?.AddMoney(revenue, $"Ekspor {cpoStockTon:F1} Ton CPO");
            cpoStockTon = 0;
            return true;
        }
        #endregion

        private void HandleDailyStaminaRecovery(int day, int month, int year)
        {
            foreach (var w in workers)
            {
                w.stamina = Mathf.Min(100f, w.stamina + 30f);
            }
        }

        #region Fallback Visual
        private GameObject CreateFallbackWorkerVisual(string name, Vector3 pos, Color color)
        {
            GameObject go = new GameObject($"Worker_{name}");
            go.transform.position = pos;
            go.transform.SetParent(workersContainer);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(24, 36);
            Color[] colors = new Color[24 * 36];
            for (int i = 0; i < colors.Length; i++) colors[i] = color;
            tex.SetPixels(colors);
            tex.Apply();

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 24, 36), new Vector2(0.5f, 0.5f), 32);
            sr.sortingOrder = 6;
            return go;
        }
        #endregion

        #region Save / Load
        public void LoadState(List<Worker> savedWorkers, float tbs, float cpo, bool factory)
        {
            if (savedWorkers != null && savedWorkers.Count > 0)
            {
                workers = new List<Worker>(savedWorkers);
            }
            tbsStockTon = Mathf.Max(0, tbs);
            cpoStockTon = Mathf.Max(0, cpo);
            hasFactory = factory;
            if (hasFactory && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SetOtherAssetsValuation(factoryBuildCost);
            }
        }
        #endregion
    }
}
