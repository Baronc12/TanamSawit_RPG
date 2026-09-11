using System.Collections;
using UnityEngine;
using TanamSawit.Managers;

namespace TanamSawit.Workers
{
    public enum WorkerState
    {
        Idle,
        Working,
        Resting
    }

    /// <summary>
    /// WorkerController ditempelkan ke objek/prefab pekerja di scene (Kotak/Kapsul/Sprite).
    /// Mengelola siklus kerja panen otomatis, konsumsi stamina, dan penyetoran uang ke EconomyManager.
    /// </summary>
    public class WorkerController : MonoBehaviour
    {
        [Header("Konfigurasi Data")]
        [SerializeField] private WorkerData data;

        [Header("Stats Aktif (Live RPG Stats)")]
        [SerializeField] private string workerName = "Pekerja Sawit";
        [SerializeField] private float currentStamina;
        [SerializeField] private int currentExperience;
        [SerializeField] private int currentIntelligence;

        [Header("Siklus Kerja")]
        [Tooltip("Berapa detik sekali pekerja menyelesaikan 1 siklus panen sawit.")]
        [SerializeField] private float harvestCycleDuration = 4.0f;
        [SerializeField] private float staminaDrainPerCycle = 10f;
        [SerializeField] private float staminaRecoveryPerSecond = 5f;

        [Header("Status Saat Ini")]
        [SerializeField] private WorkerState currentState = WorkerState.Working;

        public string WorkerName => workerName;
        public float CurrentStamina => currentStamina;
        public float MaxStamina => data != null ? data.baseStamina : 100f;
        public int CurrentExperience => currentExperience;
        public int CurrentIntelligence => currentIntelligence;
        public WorkerState CurrentState => currentState;

        private void Start()
        {
            InitializeStats();
            StartCoroutine(WorkCycleRoutine());
        }

        public void SetWorkerData(WorkerData workerData, int bonusIntelligence = 0)
        {
            data = workerData;
            InitializeStats(bonusIntelligence);
        }

        private void InitializeStats(int bonusIntelligence = 0)
        {
            if (data != null)
            {
                workerName = data.workerTitle;
                currentStamina = data.baseStamina;
                currentExperience = data.baseExperience;
                currentIntelligence = data.baseIntelligence + bonusIntelligence;

                // Terapkan warna penanda jika ada SpriteRenderer
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = data.workerColor;
            }
            else
            {
                currentStamina = 100f;
                currentExperience = 10;
                currentIntelligence = 10;
            }
        }

        private IEnumerator WorkCycleRoutine()
        {
            while (true)
            {
                // Jika stamina habis, masuk fase istirahat (Resting)
                if (currentStamina <= 0f)
                {
                    currentState = WorkerState.Resting;
                    while (currentStamina < MaxStamina)
                    {
                        currentStamina = Mathf.Min(MaxStamina, currentStamina + (staminaRecoveryPerSecond * Time.deltaTime));
                        yield return null;
                    }
                    currentState = WorkerState.Working;
                }

                // Fase Bekerja (Panen)
                currentState = WorkerState.Working;
                yield return new WaitForSeconds(harvestCycleDuration);

                // Eksekusi Panen jika game sedang aktif berjalan
                if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing)
                {
                    PerformHarvestCycle();
                }
            }
        }

        private void PerformHarvestCycle()
        {
            currentStamina = Mathf.Max(0f, currentStamina - staminaDrainPerCycle);

            // Hitung hasil panen berdasarkan Experience & Intelligence
            // Rumus: Base Rp 250.000 + (Exp * 25.000) + (Int * 10.000)
            double baseEarnings = 250_000 + (currentExperience * 25_000) + (currentIntelligence * 10_000);

            // Periksa apakah ada multiplier dari Pabrik Sawit (BuildingManager)
            double factoryMultiplier = 1.0;
            var bmType = System.Type.GetType("TanamSawit.Buildings.BuildingManager, Assembly-CSharp");
            if (bmType != null)
            {
                var instanceProp = bmType.GetProperty("Instance");
                var instance = instanceProp?.GetValue(null);
                if (instance != null)
                {
                    var multiplierProp = bmType.GetProperty("FactoryMultiplier");
                    if (multiplierProp != null)
                    {
                        factoryMultiplier = (double)multiplierProp.GetValue(instance);
                    }
                }
            }

            double totalIncome = baseEarnings * factoryMultiplier;

            // Setor pendapatan ke kas pemain
            EconomyManager.Instance?.AddMoney(totalIncome, $"Panen oleh {workerName}");

            // Sedikit menambah Experience seiring waktu kerja
            if (Random.value < 0.2f)
            {
                currentExperience += 1;
            }
        }

        /// <summary>
        /// Mengurangi stamina/moral pekerja secara instan (misal akibat Teror Pinjol).
        /// </summary>
        public void ApplyStressPenalty(float amount)
        {
            currentStamina = Mathf.Max(0f, currentStamina - amount);
            Debug.LogWarning($"[WorkerController] {workerName} terkena stres berat! Stamina berkurang -{amount}.");
        }
    }
}
