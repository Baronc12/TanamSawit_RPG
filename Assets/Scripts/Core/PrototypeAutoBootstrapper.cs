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
    /// AutoBootstrapper memastikan bahwa setiap kali Anda menekan tombol Play di Unity Editor,
    /// semua sistem game (GameManager, Economy, Time, Workers, Building, Loan, Ecology, Rival, HUD, SaveManager, Grid)
    /// OTOMATIS dibuat dan langsung berjalan tanpa Anda harus membuat GameObject secara manual!
    /// </summary>
    public static class PrototypeAutoBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnPlay()
        {
            // Cek apakah GameManager sudah ada di scene
            if (GameManager.Instance == null)
            {
                Debug.Log("<color=#00FF66><b>[Tanam Sawit]</b> Memulai Auto-Bootstrapper Prototype...</color>");

                // Buat GameObject [MANAGERS] secara otomatis di memori
                GameObject managersHost = new GameObject("[MANAGERS]");
                Object.DontDestroyOnLoad(managersHost);

                // Pasang semua sistem core
                managersHost.AddComponent<GameManager>();
                managersHost.AddComponent<EconomyManager>();
                managersHost.AddComponent<TimeManager>();
                managersHost.AddComponent<WorkerManager>();
                managersHost.AddComponent<BuildingManager>();
                managersHost.AddComponent<LoanManager>();
                managersHost.AddComponent<EcologyManager>();
                managersHost.AddComponent<EnvironmentalKarmaManager>();
                managersHost.AddComponent<RivalManager>();
                managersHost.AddComponent<SaveManager>();
                managersHost.AddComponent<SettingsManager>();

                // Pasang visual 2D Grid & Ecology Spawner
                managersHost.AddComponent<GridManager>();
                managersHost.AddComponent<EcologySpawner>();

                // Scene opening gameplay dipakai untuk karakter yang dapat dijalankan.
                // HUD prototype memakai OnGUI dan selalu berada di depan SpriteRenderer,
                // jadi jangan buat HUD pada scene ini agar karakter tetap terlihat.
                bool isOpeningGameplay = SceneManager.GetActiveScene().name == "opening gameplay";
                if (!isOpeningGameplay)
                    managersHost.AddComponent<TycoonHUD>();

                Debug.Log(isOpeningGameplay
                    ? "<color=#00FF66><b>[Tanam Sawit]</b> Manager opening gameplay aktif tanpa HUD prototype.</color>"
                    : "<color=#00FF66><b>[Tanam Sawit]</b> Semua Manager & HUD berhasil diaktifkan secara otomatis!</color>");
            }
        }
    }
}
