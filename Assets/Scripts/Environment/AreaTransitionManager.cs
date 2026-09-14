using System;
using System.Collections;
using UnityEngine;
using TanamSawit.Player;

namespace TanamSawit.Environment
{
    /// <summary>
    /// Mengelola animasi transisi fade-to-black dan loading saat berpindah area.
    /// Memindahkan posisi karakter pemain dan kamera ke area tujuan.
    /// Dilengkapi dengan overlay visual yang langsung berfungsi tanpa ketergantungan Canvas prefab eksternal.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    public class AreaTransitionManager : MonoBehaviour
    {
        public static AreaTransitionManager Instance { get; private set; }

        [Header("Status Area")]
        [SerializeField] private string currentAreaName = "Area Kebun Sawit";
        [SerializeField] private string currentAreaId = "kebun";
        [SerializeField] private bool isTransitioning = false;

        public string CurrentAreaName => currentAreaName;
        public string CurrentAreaId => currentAreaId;
        public bool IsTransitioning => isTransitioning;

        /// <summary>
        /// Maps area IDs to center positions (must match WorldBuildingBuilder constants).
        /// </summary>
        public static readonly System.Collections.Generic.Dictionary<string, Vector3> AreaCenters =
            new System.Collections.Generic.Dictionary<string, Vector3>
            {
                { "kebun", new Vector3(0f, 0f, 0f) },
                { "perumahan", new Vector3(60f, 0f, 0f) },
                { "kota", new Vector3(120f, 0f, 0f) },
                { "pabrik", new Vector3(0f, -60f, 0f) },
            };

        /// <summary>
        /// Maps area display names to machine-readable IDs.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> AreaNameToId =
            new System.Collections.Generic.Dictionary<string, string>
            {
                { "Area Kebun Sawit", "kebun" },
                { "Area Perumahan", "perumahan" },
                { "Area Kota", "kota" },
                { "Area Pabrik", "pabrik" },
            };

        [Header("Pengaturan Waktu Fade")]
        [SerializeField] private float fadeDuration = 0.45f;
        [SerializeField] private float holdDuration = 0.5f;

        public event Action<string> OnAreaChanged;

        private float currentAlpha = 0f;
        private string transitionTargetArea = "";
        private Texture2D blackTexture;
        private GUIStyle titleCardStyle;
        private GUIStyle subTitleStyle;
        private bool stylesReady = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateBlackTexture();
        }

        private void CreateBlackTexture()
        {
            blackTexture = new Texture2D(2, 2);
            Color[] colors = new Color[] { Color.black, Color.black, Color.black, Color.black };
            blackTexture.SetPixels(colors);
            blackTexture.Apply();
        }

        /// <summary>
        /// Memulai proses perpindahan area dengan animasi fade out -> pindah koordinat -> fade in.
        /// </summary>
        public void TransitionToArea(string targetArea, Vector3 targetPosition)
        {
            if (isTransitioning) return;
            StartCoroutine(TransitionRoutine(targetArea, targetPosition));
        }

        /// <summary>
        /// Teleports player to an area by ID without requiring a portal.
        /// Used by SaveManager on load. Optionally skips fade animation.
        /// </summary>
        public void TeleportToArea(string areaId, Vector3 playerPosition, bool skipFade = true)
        {
            if (skipFade)
            {
                // Instant teleport — no fade
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    player.transform.position = new Vector3(playerPosition.x, playerPosition.y, player.transform.position.z);
                }

                if (SmoothFollowCamera2D.Instance != null)
                {
                    SmoothFollowCamera2D.Instance.SnapTo(playerPosition);
                }

                currentAreaId = areaId;
                currentAreaName = GetAreaDisplayName(areaId);
                OnAreaChanged?.Invoke(currentAreaName);
            }
            else
            {
                // Full transition with fade
                string displayName = GetAreaDisplayName(areaId);
                TransitionToArea(displayName, playerPosition);
            }
        }

        private string GetAreaDisplayName(string areaId)
        {
            switch (areaId)
            {
                case "kebun": return "Area Kebun Sawit";
                case "perumahan": return "Area Perumahan";
                case "kota": return "Area Kota";
                case "pabrik": return "Area Pabrik";
                default: return areaId;
            }
        }

        private IEnumerator TransitionRoutine(string targetArea, Vector3 targetPosition)
        {
            isTransitioning = true;
            transitionTargetArea = targetArea;

            // 1. FADE OUT (Menjadi Gelap)
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                currentAlpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            currentAlpha = 1f;

            // 2. PINDAHKAN PEMAIN & KAMERA KE POSISI TUJUAN
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                // Fallback cari objek karakter
                #pragma warning disable CS0618
                var ctrl = FindFirstObjectByType<OpeningGameplayPlayerController>();
                #pragma warning restore CS0618
                if (ctrl != null) player = ctrl.gameObject;
            }

            if (player != null)
            {
                player.transform.position = new Vector3(targetPosition.x, targetPosition.y, player.transform.position.z);
            }

            // Reposisi kamera langsung (Snap) agar tidak bergeser meluncur
            if (SmoothFollowCamera2D.Instance != null)
            {
                SmoothFollowCamera2D.Instance.SnapTo(targetPosition);
            }
            else if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(targetPosition.x, targetPosition.y, Camera.main.transform.position.z);
            }

            currentAreaName = targetArea;
            currentAreaId = AreaNameToId.TryGetValue(targetArea, out var id) ? id : currentAreaId;
            OnAreaChanged?.Invoke(currentAreaName);

            // Jeda sesaat di layar gelap (Simulasi Loading)
            yield return new WaitForSecondsRealtime(holdDuration);

            // 3. FADE IN (Layar Kembali Terang)
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                currentAlpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                yield return null;
            }
            currentAlpha = 0f;
            isTransitioning = false;
        }

        private void InitStyles()
        {
            if (stylesReady) return;

            titleCardStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleCardStyle.normal.textColor = new Color(1f, 0.88f, 0.3f);

            subTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter
            };
            subTitleStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            stylesReady = true;
        }

        private void OnGUI()
        {
            if (currentAlpha <= 0.001f) return;

            InitStyles();

            GUI.depth = -200; // Selalu di atas segalanya

            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, currentAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);

            // Tampilkan teks nama area tujuan saat layar hampir gelap penuh
            if (currentAlpha >= 0.6f && !string.IsNullOrEmpty(transitionTargetArea))
            {
                float centerX = Screen.width * 0.5f;
                float centerY = Screen.height * 0.5f;

                GUI.Label(new Rect(centerX - 300, centerY - 45, 600, 40), $"🌴 MEMASUKI {transitionTargetArea.ToUpper()}", titleCardStyle);
                GUI.Label(new Rect(centerX - 300, centerY + 5, 600, 30), "Memuat simulasi & kondisi lingkungan...", subTitleStyle);
            }

            GUI.color = prevColor;
        }
    }
}
