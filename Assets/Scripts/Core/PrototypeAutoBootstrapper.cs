using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TanamSawit.Managers;
using TanamSawit.Buildings;
using TanamSawit.SaveSystem;
using TanamSawit.Environment;
using TanamSawit.UI;

namespace TanamSawit.Core
{
    /// <summary>
    /// AutoBootstrapper memastikan bahwa setiap kali Anda menekan tombol Play di scene Tycoon gameplay,
    /// semua sistem game (GameManager, Economy, Time, Workers, Building, Loan, Ecology, Rival, HUD, SaveManager, Grid)
    /// otomatis dibuat dan berjalan.
    ///
    /// PENTING:
    /// Pada scene pergerakan karakter seperti 'prototype_Movement' atau scene yang mengandung kata 'movement',
    /// bootstrapper ini TIDAK AKAN membuat [MANAGERS] maupun TycoonHUD, dan akan membersihkan objek DontDestroyOnLoad
    /// agar scene pergerakan karakter tetap bersih sesuai fungsinya.
    /// </summary>
    public static class PrototypeAutoBootstrapper
    {
        // Auto-bootstrapper dinonaktifkan agar scene baru (seperti prototype_Movement)
        // tidak memuat apa pun secara otomatis di background.
        // Jika ingin memasang [MANAGERS] di suatu scene, gunakan menu:
        // 'Tanam Sawit > 🚀 1-Klik: Pasang [MANAGERS] ke Scene Ini'

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeOnPlay()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            // Jangan jalankan auto-bootstrapper di scene pergerakan karakter (prototype_Movement)
            if (IsExcludedScene(currentSceneName))
            {
                Debug.Log($"<color=#FFFF00><b>[Tanam Sawit]</b> Scene '{currentSceneName}' terdeteksi sebagai scene pergerakan karakter. DontDestroyOnLoad [MANAGERS] & TycoonHUD TIDAK dimuat.</color>");
                CleanupDontDestroyOnLoadManagers();
                return;
            }

            // Cari atau buat GameObject [MANAGERS]
            GameObject managersHost = GameObject.Find("[MANAGERS]");
            if (managersHost == null)
            {
                Debug.Log("<color=#00FF66><b>[Tanam Sawit]</b> Memulai Auto-Bootstrapper Prototype Tycoon...</color>");

                managersHost = new GameObject("[MANAGERS]");
                UnityEngine.Object.DontDestroyOnLoad(managersHost);

                // Pasang semua sistem core
                managersHost.AddComponent<GameManager>();
                managersHost.AddComponent<EconomyManager>();
                managersHost.AddComponent<TimeManager>();
                managersHost.AddComponent<WorkerManager>();
                managersHost.AddComponent<BuildingManager>();
                managersHost.AddComponent<LoanManager>();
                managersHost.AddComponent<EnvironmentalKarmaManager>();
                managersHost.AddComponent<RivalManager>();
                managersHost.AddComponent<SaveManager>();
                managersHost.AddComponent<SettingsManager>();

                // Pasang visual 2D Grid & Ecology Spawner
                managersHost.AddComponent<GridManager>();
                managersHost.AddComponent<EcologySpawner>();

                // Pasang siklus siang-malam
                managersHost.AddComponent<TanamSawit.Environment.DayNightCycle>();

                // Pasang UI uGUI (menggantikan IMGUI TycoonHUD/ModernTycoonHUD)
                UIRoot.EnsureExists();

                Debug.Log("<color=#00FF66><b>[Tanam Sawit]</b> Semua Manager & UI berhasil diaktifkan secara otomatis!</color>");
            }

            // Pastikan SettingsManager selalu ada (bahkan jika [MANAGERS] sudah ada di scene)
            EnsureComponent<SettingsManager>(managersHost);

            // Pastikan MainMenuSystem selalu ada untuk mengatur transisi menu ke gameplay
            if (managersHost.GetComponent<TanamSawit.Core.MainMenuSystem>() == null)
            {
                managersHost.AddComponent<TanamSawit.Core.MainMenuSystem>();
                Debug.Log("<color=#00FF66><b>[Tanam Sawit]</b> MainMenuSystem ditambahkan ke [MANAGERS].</color>");
            }
        }

        /// <summary>
        /// Menambahkan komponen T ke GameObject jika belum ada.
        /// </summary>
        private static void EnsureComponent<T>(GameObject host) where T : Component
        {
            if (host.GetComponent<T>() == null)
            {
                host.AddComponent<T>();
                Debug.Log($"<b>[Tanam Sawit]</b> {typeof(T).Name} ditambahkan ke {host.name}.");
            }
        }

        /// <summary>
        /// Mengecek apakah scene merupakan scene pergerakan karakter atau scene khusus
        /// yang tidak boleh memuat DontDestroyOnLoad sistem Tycoon.
        /// </summary>
        public static bool IsExcludedScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            string lower = sceneName.ToLower();
            return lower.Contains("movement") || lower.Contains("prototype_movement") || lower == "opening gameplay";
        }

        /// <summary>
        /// Menghapus objek [MANAGERS], [SETTINGS_MANAGER], dan plot container dari DontDestroyOnLoad
        /// jika masuk ke scene movement agar scene tetap bersih.
        /// </summary>
        public static void CleanupDontDestroyOnLoadManagers()
        {
            GameObject managersHost = GameObject.Find("[MANAGERS]");
            if (managersHost != null)
            {
                Debug.Log("<color=#FF9900><b>[Tanam Sawit]</b> Membersihkan [MANAGERS] DontDestroyOnLoad agar scene movement bersih.</color>");
                UnityEngine.Object.Destroy(managersHost);
            }

            GameObject settingsHost = GameObject.Find("[SETTINGS_MANAGER]");
            if (settingsHost != null)
            {
                UnityEngine.Object.Destroy(settingsHost);
            }

            GameObject plotsContainer = GameObject.Find("--- [PLOTS_CONTAINER] ---");
            if (plotsContainer != null)
            {
                UnityEngine.Object.Destroy(plotsContainer);
            }

            // Atur warna background kamera menjadi hitam netral (bukan biru default)
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = Color.black;
            }
        }
    }
}
