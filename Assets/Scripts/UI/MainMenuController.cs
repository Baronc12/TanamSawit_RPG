using UnityEngine;

namespace TanamSawit.UI
{
    /// <summary>
    /// Mengontrol tombol Play, Settings, dan Quit pada main menu.
    /// Pada arsitektur ini, main menu dan gameplay berada di scene yang sama.
    /// Tekan PLAY akan memulai gameplay: mengaktifkan kamera follow dan
    /// membuka gerakan pemain melalui MainMenuSystem.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Referensi")]
        [Tooltip("Referensi ke MainMenuSystem yang mengatur transisi menu ke gameplay.")]
        [SerializeField] private TanamSawit.Core.MainMenuSystem mainMenuSystem;

        [Header("Konfigurasi Scene")]
        [Tooltip("Nama scene gameplay (untuk referensi / debug).")]
        [SerializeField] private string gameplaySceneName = "SampleScene";

        private void Awake()
        {
            if (mainMenuSystem == null)
            {
#pragma warning disable CS0618
                mainMenuSystem = FindFirstObjectByType<TanamSawit.Core.MainMenuSystem>();
#pragma warning restore CS0618
            }
        }

        /// <summary>
        /// Dipanggil saat tombol PLAY ditekan.
        /// </summary>
        public void OnPlayButtonClicked()
        {
            Debug.Log($"[MainMenu] Memulai gameplay dari menu (scene: {gameplaySceneName})...");
            if (mainMenuSystem != null)
            {
                mainMenuSystem.StartGameplay();
            }
            else
            {
                Debug.LogWarning("[MainMenu] MainMenuSystem tidak ditemukan. Gameplay tidak dimulai.");
            }
        }

        // Dukungan klik via OnMouseDown untuk tombol berbasis TextMesh world-space
        // (yang tidak memakai Unity UI Button). Membutuhkan Collider2D pada GameObject.
        private void OnMouseDown()
        {
            OnPlayButtonClicked();
        }

        /// <summary>
        /// Dipanggil saat tombol SETTINGS ditekan.
        /// </summary>
        public void OnSettingsButtonClicked()
        {
            Debug.Log("[MainMenu] Membuka menu Pengaturan...");
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OpenSettings();
            }
            else
            {
                Debug.Log("[MainMenu] SettingsManager tidak tersedia. Membuka panel settings...");
                // TODO: Buka panel settings
            }
        }

        /// <summary>
        /// Dipanggil saat tombol QUIT ditekan.
        /// </summary>
        public void OnQuitButtonClicked()
        {
            Debug.Log("[MainMenu] Keluar dari game.");
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
