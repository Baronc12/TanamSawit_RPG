using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TanamSawit.Managers
{
    /// <summary>
    /// State global alur permainan Tanam Sawit.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// GameManager bertindak sebagai pusat kendali state game dan orkestrator siklus hidup game.
    /// Menggunakan arsitektur Singleton dan Event-Driven.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Memastikan GameManager dieksekusi lebih awal dari script lainnya
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        public static GameManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [Tooltip("Jika dicentang, objek ini tidak akan hancur saat berpindah scene.")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        #endregion

        #region Game State
        [Header("State Permainan")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        /// <summary>
        /// Mendapatkan state aktif saat ini.
        /// </summary>
        public GameState CurrentState => currentState;

        /// <summary>
        /// Event yang dipicu setiap kali terjadi transisi GameState.
        /// Komponen lain (UI, Audio, AI Buruh, dsb) cukup mendengarkan event ini tanpa polling.
        /// </summary>
        public static event Action<GameState> OnGameStateChanged;
        #endregion

        private void Awake()
        {
            // Inisialisasi Singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[GameManager] Ditemukan instance duplikat pada GameObject {gameObject.name}. Menghancurkan objek ini.");
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
            // Set initial state saat pertama kali dijalankan
            ChangeState(currentState);
        }

        /// <summary>
        /// Mengubah state permainan ke state baru dan memicu OnGameStateChanged.
        /// </summary>
        /// <param name="newState">State tujuan</param>
        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            Debug.Log($"[GameManager] Game State berubah menjadi: {currentState}");

            // Handle logika spesifik tiap state
            switch (currentState)
            {
                case GameState.MainMenu:
                    HandleMainMenu();
                    break;
                case GameState.Playing:
                    HandlePlaying();
                    break;
                case GameState.Paused:
                    HandlePaused();
                    break;
                case GameState.GameOver:
                    HandleGameOver();
                    break;
            }

            // Pemicu event untuk sistem eksternal
            OnGameStateChanged?.Invoke(currentState);
        }

        private void HandleMainMenu()
        {
            Time.timeScale = 1f;
        }

        private void HandlePlaying()
        {
            Time.timeScale = 1f;
        }

        private void HandlePaused()
        {
            Time.timeScale = 0f;
        }

        private void HandleGameOver()
        {
            Time.timeScale = 0f;
            Debug.Log("[GameManager] Game Over tercapai! Evaluasi skor / net worth akhir.");
        }

        #region Public Helper Functions
        /// <summary>
        /// Menghentikan sementara permainan.
        /// </summary>
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        /// <summary>
        /// Melanjutkan permainan dari keadaan pause.
        /// </summary>
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }

        /// <summary>
        /// Memulai permainan gameplay dari Main Menu.
        /// </summary>
        public void StartGame()
        {
            ChangeState(GameState.Playing);
        }

        /// <summary>
        /// Memuat ulang scene aktif saat ini.
        /// </summary>
        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
        #endregion
    }
}
