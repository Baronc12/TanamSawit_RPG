using UnityEngine;
using UnityEngine.SceneManagement;

namespace TanamSawit.UI
{
    /// <summary>
    /// Mengontrol tombol Play, Settings, dan Quit pada scene Main Menu.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Konfigurasi Scene")]
        [Tooltip("Nama scene gameplay yang akan dimuat saat tombol PLAY ditekan.")]
        [SerializeField] private string gameplaySceneName = "GameplayScene";

        /// <summary>
        /// Dipanggil saat tombol PLAY ditekan.
        /// </summary>
        public void OnPlayButtonClicked()
        {
            Debug.Log($"[MainMenu] Memuat scene gameplay: {gameplaySceneName}...");
            SceneManager.LoadScene(gameplaySceneName);
        }

        /// <summary>
        /// Dipanggil saat tombol SETTINGS ditekan.
        /// </summary>
        public void OnSettingsButtonClicked()
        {
            Debug.Log("[MainMenu] Membuka menu Pengaturan...");
            // TODO: Buka panel settings
        }

        /// <summary>
        /// Dipanggil saat tombol KELUAR [X] ditekan.
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
