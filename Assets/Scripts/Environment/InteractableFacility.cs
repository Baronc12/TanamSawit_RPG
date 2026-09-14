using System;
using System.Collections;
using UnityEngine;
using TanamSawit.Managers;
using TanamSawit.Environment;
using TanamSawit.Player;
using TanamSawit.UI;

namespace TanamSawit.Environment
{
    /// <summary>
    /// Jenis fasilitas interaktif yang dapat ditemukan di keempat area dunia game.
    /// </summary>
    public enum FacilityType
    {
        PapanLahan,
        MandorKebun,
        GudangTBS,
        MessPekerja,
        KosKosan,
        Bank,
        Pinjol,
        Rentenir,
        PabrikCPO,
        YayasanCSR,
        BillboardSepupu,
        PosGerbang,
    }

    /// <summary>
    /// Komponen serbaguna untuk bangunan, NPC, dan fasilitas interaktif di keempat area dunia.
    /// Memunculkan indikator [E] / [Spasi] saat MC mendekat dan membuka modal UI sesuai jenis fasilitas.
    /// Dilengkapi fallback procedural tanpa bergantung pada prefab eksternal.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public class InteractableFacility : MonoBehaviour
    {
        [Header("Konfigurasi Fasilitas")]
        [SerializeField] private FacilityType facilityType = FacilityType.MandorKebun;
        [SerializeField] private string displayName = "Mandor Kebun";
        [SerializeField] private string interactionHint = "Tekan [E] untuk Bicara";

        [Header("Pengaturan Jarak Interaksi")]
        [SerializeField] private float interactionRadius = 2.0f;

        [Header("Visual Floating Prompt (IMGUI)")]
        [SerializeField] private bool showFloatingPrompt = true;

        // Runtime state
        private bool playerInRange = false;
        private Transform playerTransform;
        private Texture2D promptBg;
        private GUIStyle promptStyle;
        private bool stylesReady = false;

        // Diagnostic state (temporary — for debugging interaction issue)
        internal static bool _lastEdgeDetected;       // true if EOrSpace() returned true this frame
        internal static bool _lastInteractCalled;     // true if Interact() was called this frame
        internal static int _lastEdgeFrame = -1;       // frame count of last edge detection
        internal static string _lastDiagMsg = "";      // diagnostic message

        // Event untuk dipanggil FacilityModalUI / DialogueActionRouter
        public static event Action<FacilityType, string> OnFacilityInteracted;

        /// <summary>Memicu OnFacilityInteracted dari luar (mis. dari dialog NPC).</summary>
        public static void InvokeInteraction(FacilityType type, string name)
        {
            OnFacilityInteracted?.Invoke(type, name);
        }

        // Singleton register agar ModernTycoonHUD bisa subscribe
        private static System.Collections.Generic.List<InteractableFacility> allFacilities
            = new System.Collections.Generic.List<InteractableFacility>();

        public static System.Collections.Generic.IReadOnlyList<InteractableFacility> AllFacilities => allFacilities;

        public FacilityType Type => facilityType;
        public string DisplayName => displayName;
        public bool PlayerInRange => playerInRange;

        private void Awake()
        {
            EnsureCollider();
            allFacilities.Add(this);
            CreatePromptTexture();
        }

        private void OnDestroy()
        {
            allFacilities.Remove(this);
        }

        private void EnsureCollider()
        {
            if (GetComponent<Collider2D>() == null)
            {
                var col = gameObject.AddComponent<CircleCollider2D>();
                col.radius = interactionRadius;
                col.isTrigger = true;
            }
        }

        private void CreatePromptTexture()
        {
            promptBg = new Texture2D(2, 2);
            Color bg = new Color(0f, 0f, 0f, 0.72f);
            promptBg.SetPixels(new Color[] { bg, bg, bg, bg });
            promptBg.Apply();
        }

        private void InitStyles()
        {
            if (stylesReady) return;
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            promptStyle.normal.textColor = new Color(1f, 0.92f, 0.3f);
            stylesReady = true;
        }

        private void Update()
        {
            _lastInteractCalled = false;

            // Jangan proses interaksi jika modal (facility/dialog/gameover) sedang terbuka
            if (UIRoot.IsModalOpen)
            {
                playerInRange = false;
                return;
            }

            // Radius-based proximity check (tidak bergantung Trigger physics)
            Transform playerT = GetPlayerTransform();
            if (playerT == null) return;

            float dist = Vector2.Distance(transform.position, playerT.position);
            playerInRange = dist <= interactionRadius;

            if (playerInRange && IsInteractPressed())
            {
                _lastEdgeDetected = true;
                _lastEdgeFrame = Time.frameCount;
                Debug.Log($"<color=#FFEE55>[InteractableFacility]</color> Edge detected on frame {Time.frameCount} for {displayName}");
                Interact();
            }
            else
            {
                _lastEdgeDetected = false;
            }
        }

        private void Interact()
        {
            _lastInteractCalled = true;

            // Kunci pergerakan MC saat modal sedang dibuka
#pragma warning disable CS0618
            var controller = FindFirstObjectByType<OpeningGameplayPlayerController>();
#pragma warning restore CS0618
            if (controller != null) controller.SetMovementLocked(true);

            if (OnFacilityInteracted == null)
            {
                _lastDiagMsg = "OnFacilityInteracted has NO subscribers! FacilityModalUI not initialized?";
                Debug.LogWarning("[InteractableFacility] " + _lastDiagMsg);
            }
            else
            {
                _lastDiagMsg = $"Firing OnFacilityInteracted for {displayName} ({facilityType})";
                OnFacilityInteracted.Invoke(facilityType, displayName);
            }
            Debug.Log($"<color=#00FF88>[Interaksi]</color> Memilih fasilitas: {displayName} ({facilityType})");
        }

        private Transform GetPlayerTransform()
        {
            if (playerTransform != null) return playerTransform;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                return playerTransform;
            }

#pragma warning disable CS0618
            var ctrl = FindFirstObjectByType<OpeningGameplayPlayerController>();
#pragma warning restore CS0618
            if (ctrl != null)
            {
                playerTransform = ctrl.transform;
                return playerTransform;
            }

            return null;
        }

        private void OnGUI()
        {
            if (!showFloatingPrompt || !playerInRange) return;
            if (AreaTransitionManager.Instance != null && AreaTransitionManager.Instance.IsTransitioning) return;

            InitStyles();

            Camera cam = Camera.main;
            if (cam == null) return;

            // Konversi posisi dunia ke koordinat layar
            Vector3 worldPos = transform.position + Vector3.up * 1.2f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            // Balik Y koordinat karena GUI menggunakan koordinat layar terbalik
            screenPos.y = Screen.height - screenPos.y;

            if (screenPos.z < 0) return; // Di belakang kamera, jangan render

            float w = 220f;
            float h = 34f;
            Rect boxRect = new Rect(screenPos.x - w * 0.5f, screenPos.y - h, w, h);

            // Background gelap transparan
            GUI.DrawTexture(boxRect, promptBg);

            // Teks interaksi
            string promptText = $"<b>{displayName}</b>  |  <color=#FFEE55>[E]</color> {interactionHint}";
            GUI.Label(boxRect, promptText, promptStyle);

            // ── Diagnostic overlay (temporary) ──
            DrawDiagnosticOverlay();
        }

        /// <summary>Temporary on-screen diagnostic to trace interaction failures.</summary>
        private void DrawDiagnosticOverlay()
        {
            float dy = 60f;
            float dw = 520f;
            float dh = 140f;
            float dx = (Screen.width - dw) * 0.5f;

            Rect diagRect = new Rect(dx, dy, dw, dh);
            GUI.DrawTexture(diagRect, promptBg);

            GUIStyle ds = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperLeft,
                richText = true
            };
            ds.normal.textColor = Color.white;

            bool hasSubs = OnFacilityInteracted != null;
            bool uiRootOk = UIRoot.Instance != null;
            bool modalOpen = UIRoot.IsModalOpen;
            bool facilityModalOk = FacilityModalUI.Instance != null;

            int framesSinceEdge = (_lastEdgeFrame >= 0) ? (Time.frameCount - _lastEdgeFrame) : -1;
            string edgeStr = _lastEdgeDetected ? "THIS FRAME" :
                              (framesSinceEdge >= 0 ? $"{framesSinceEdge} frames ago" : "never");

            string diag = $"<color=#00FF66><b>--- INTERACTION DEBUG ---</b></color>\n" +
                          $"InRange: <color=#{(playerInRange ? "00FF00" : "FF0000")}>{playerInRange}</color>  " +
                          $"Edge: <color=#{(_lastEdgeDetected ? "00FF00" : "FFAA00")}>{edgeStr}</color>\n" +
                          $"OnFacilityInteracted subs: <color=#{(hasSubs ? "00FF00" : "FF0000")}>{hasSubs}</color>  " +
                          $"FacilityModalUI: <color=#{(facilityModalOk ? "00FF00" : "FF0000")}>{facilityModalOk}</color>\n" +
                          $"UIRoot.Instance: <color=#{(uiRootOk ? "00FF00" : "FF0000")}>{uiRootOk}</color>  " +
                          $"IsModalOpen: <color=#{(modalOpen ? "FF0000" : "00FF00")}>{modalOpen}</color>\n" +
                          $"LastMsg: {_lastDiagMsg}";

            GUI.Label(diagRect, diag, ds);
        }

        // ── Input ─────────────────────────────────────────────────────

        private static bool IsInteractPressed()
        {
            return TanamSawit.Core.InputEdgeDetection.EOrSpace();
        }

        public void Configure(FacilityType type, string name, string hint = "Tekan [E] untuk Interaksi", float radius = 2f)
        {
            facilityType = type;
            displayName = name;
            interactionHint = hint;
            interactionRadius = radius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
