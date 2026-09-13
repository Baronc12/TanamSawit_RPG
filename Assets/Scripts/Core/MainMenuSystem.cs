using System.Collections;
using UnityEngine;
using TanamSawit.Managers;
using TanamSawit.Player;
using TanamSawit.UI;

namespace TanamSawit.Core
{
    /// <summary>
    /// MainMenuSystem adalah gerbang utama menu. Pada awal game,
    /// kamera fokus pada area menu dan karakter tidak dapat bergerak.
    /// Setelah pengguna menekan tombol PLAY, sistem beralih ke
    /// state Gameplay: kamera mulai mengikuti karakter dan gerakan
    /// pemain terbuka.
    ///
    /// Script ini juga bertanggung jawab untuk menemukan objek PLAY
    /// (TextMesh) di scene dan memasang MainMenuController padanya
    /// agar tombol Play benar-benar berfungsi sebagai gerbang utama.
    /// </summary>
    [DefaultExecutionOrder(-50)] // Jalankan sebelum player controller & kamera follow
    public class MainMenuSystem : MonoBehaviour
    {
        [Header("Kamera Menu")]
        [Tooltip("Posisi kamera saat berada di main menu (sebelum Play ditekan).")]
        [SerializeField] private Vector3 menuCameraPosition = new Vector3(0f, -3f, -10f);

        [Header("Transisi ke Gameplay")]
        [Tooltip("Apakah kamera harus halus beralih ke karakter setelah Play ditekan.")]
        [SerializeField] private bool smoothTransitionToCharacter = true;
        [Tooltip("Durasi transisi kamera menu ke karakter (detik). 0 = langsung snap.")]
        [SerializeField] private float transitionDuration = 0.8f;

        private OpeningGameplayPlayerController cachedPlayer;
        private bool gameStarted = false;

        private void Awake()
        {
            // Cache referensi pemain sekali untuk menghindari pencarian berulang
#pragma warning disable CS0618
            cachedPlayer = FindFirstObjectByType<OpeningGameplayPlayerController>();
#pragma warning restore CS0618
        }

        private void Start()
        {
            // Pastikan kamera berada di posisi menu dan follow dimatikan.
            EnsureMenuCameraSetup();

            // Kunci gerakan pemain selama menu.
            LockPlayerMovement();

            // Temukan dan setup tombol PLAY jika belum ada MainMenuController.
            SetupPlayButton();
        }

        private void EnsureMenuCameraSetup()
        {
            if (SmoothFollowCamera2D.Instance != null)
            {
                SmoothFollowCamera2D.Instance.FollowEnabled = false;
                SmoothFollowCamera2D.Instance.SnapTo(menuCameraPosition);
            }
            else
            {
                if (Camera.main != null)
                {
                    Camera.main.transform.position = menuCameraPosition;
                }
            }
        }

        private void LockPlayerMovement()
        {
            if (cachedPlayer != null)
            {
                cachedPlayer.SetMovementLocked(true);
            }
        }

        private void UnlockPlayerMovement()
        {
            if (cachedPlayer != null)
            {
                cachedPlayer.SetMovementLocked(false);
            }
        }

        /// <summary>
        /// Mencari TextMesh dengan teks mengandung "PLAY" di scene,
        /// lalu memasang MainMenuController untuk menangani klik.
        /// </summary>
        private void SetupPlayButton()
        {
#pragma warning disable CS0618
            TextMesh[] labels = FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
#pragma warning restore CS0618
            foreach (TextMesh label in labels)
            {
                if (label.text != null && label.text.ToUpperInvariant().Contains("PLAY"))
                {
                    if (label.GetComponent<MainMenuController>() == null)
                    {
                        label.gameObject.AddComponent<MainMenuController>();
                        Debug.Log($"[MainMenuSystem] MainMenuController dipasang pada tombol: {label.gameObject.name}");
                    }
                    return;
                }
            }
            Debug.LogWarning("[MainMenuSystem] Tidak ditemukan TextMesh dengan teks 'PLAY' di scene. Pastikan tombol Play ada.");
        }

        /// <summary>
        /// Dipanggil oleh tombol PLAY. Memulai gameplay: state game ke Playing,
        /// kamera mengikuti karakter, gerakan pemain terbuka.
        /// </summary>
        public void StartGameplay()
        {
            if (gameStarted) return;
            gameStarted = true;

            // Alihkan state game ke Playing.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }

            // Buka gerakan pemain.
            UnlockPlayerMovement();

            // Aktifkan follow kamera.
            if (SmoothFollowCamera2D.Instance != null)
            {
                if (smoothTransitionToCharacter)
                {
                    StartCoroutine(SmoothTransitionToPlayer(transitionDuration));
                }
                else
                {
                    SmoothFollowCamera2D.Instance.FollowEnabled = true;
                    if (cachedPlayer != null)
                    {
                        SmoothFollowCamera2D.Instance.SnapTo(cachedPlayer.transform.position);
                    }
                }
            }

            Debug.Log("[MainMenuSystem] Gameplay dimulai. Kamera mengikuti karakter.");
        }

        private IEnumerator SmoothTransitionToPlayer(float duration)
        {
            if (duration <= 0f)
            {
                if (cachedPlayer != null)
                {
                    SmoothFollowCamera2D.Instance.FollowEnabled = true;
                    SmoothFollowCamera2D.Instance.SnapTo(cachedPlayer.transform.position);
                }
                yield break;
            }

            Transform camTransform = Camera.main != null ? Camera.main.transform : null;
            if (camTransform == null)
            {
                SmoothFollowCamera2D.Instance.FollowEnabled = true;
                yield break;
            }

            if (cachedPlayer == null)
            {
                SmoothFollowCamera2D.Instance.FollowEnabled = true;
                yield break;
            }

            Transform playerTransform = cachedPlayer.transform;
            Vector3 startPosition = camTransform.position;
            Vector3 targetPosition = playerTransform.position + new Vector3(0f, 0f, -10f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                camTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            SmoothFollowCamera2D.Instance.FollowEnabled = true;
        }

        private void Update()
        {
            // Fallback: tekan Space untuk mulai gameplay jika masih di menu.
            if (!gameStarted && GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameState.MainMenu)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null &&
                    UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    StartGameplay();
                }
#endif
                try
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        StartGameplay();
                    }
                }
                catch {}
            }
        }
    }
}
