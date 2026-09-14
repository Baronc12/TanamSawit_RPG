using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TanamSawit.NPC;

namespace TanamSawit.UI
{
    /// <summary>
    /// Root Canvas untuk seluruh UI uGUI TanamSawit. Dibangun saat runtime
    /// (tanpa prefab) agar cocok dengan pola bootstrapper yang ada.
    ///
    /// Struktur: Canvas (Screen Space-Overlay, sortingOrder 100) + CanvasScaler
    /// (1920x1080, match 0.5) + GraphicRaycaster + EventSystem.
    ///
    /// Panel-panel (TopBar, Tablet, Modal, dll) dibuat oleh controller masing-masing
    /// dan ditempelkan ke <see cref="PanelRoot"/>.
    /// </summary>
    [DefaultExecutionOrder(-25)]
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; }

        /// <summary>RectTransform induk tempat semua panel ditempelkan.</summary>
        public static RectTransform PanelRoot { get; private set; }

        // ── Singleton sprite untuk background Image (1x1 putih, di-tint via color) ──
        private static Sprite _whiteSprite;
        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
                }
                return _whiteSprite;
            }
        }

        // ── Warna tema (mirip IMGUI lama) ──
        public static readonly Color BgDark = new Color(0f, 0f, 0f, 0.80f);
        public static readonly Color BgPanel = new Color(0.07f, 0.10f, 0.08f, 0.94f);
        public static readonly Color BgButton = new Color(0.2f, 0.4f, 0.25f, 0.95f);
        public static readonly Color BgButtonDanger = new Color(0.5f, 0.1f, 0.1f, 0.95f);
        public static readonly Color BgButtonSuccess = new Color(0.1f, 0.4f, 0.15f, 0.95f);
        public static readonly Color TextDefault = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color TextGold = new Color(1f, 0.88f, 0.3f);
        public static readonly Color TextGreen = new Color(0.4f, 1f, 0.55f);
        public static readonly Color TextRed = new Color(1f, 0.4f, 0.4f);
        public static readonly Color TextYellow = new Color(1f, 0.92f, 0.45f);

        // ── Modal management (hanya satu modal pada satu waktu) ──
        private GameObject _currentModal;
        /// <summary>True jika ada modal (facility/settings/gameover) yang sedang terbuka.</summary>
        public static bool IsModalOpen => Instance != null && Instance._currentModal != null;

        /// <summary>Dipanggil oleh PrototypeAutoBootstrapper setelah manager dibuat.</summary>
        public static void EnsureExists()
        {
            if (Instance != null) return;

            var go = new GameObject("[UI_ROOT]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<UIRoot>();

            // Tambahkan panel controller setelah Canvas siap
            go.AddComponent<TopBarUI>();
            go.AddComponent<NotificationTickerUI>();
            go.AddComponent<TabletUI>();
            go.AddComponent<FacilityModalUI>();
            go.AddComponent<DialogueUI>();
            go.AddComponent<GameOverUI>();
            go.AddComponent<MinimapUI>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildCanvas();
            EnsureEventSystem();
        }

        private void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // Panel root menutupi seluruh layar
            PanelRoot = CreateRect("PanelRoot", transform, stretch: true);
        }

        private void EnsureEventSystem()
        {
#pragma warning disable CS0618
            if (FindObjectOfType<EventSystem>() != null) return;
#pragma warning restore CS0618

            var esGo = new GameObject("[EventSystem]");
            DontDestroyOnLoad(esGo);
            esGo.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            // Input System: gunakan InputSystemUIInputModule
            try
            {
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            catch
            {
                // Fallback jika assembly tidak ter-resolve
                esGo.AddComponent<StandaloneInputModule>();
            }
#else
            esGo.AddComponent<StandaloneInputModule>();
#endif
        }

        // ════════════════════════════════════════════════════════════════
        //  Helper builders — dipakai oleh semua panel controller
        // ════════════════════════════════════════════════════════════════

        /// <summary>Membuat RectTransform yang menempel ke parent.</summary>
        public static RectTransform CreateRect(string name, Transform parent, bool stretch = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            return rt;
        }

        /// <summary>Membuat panel dengan background gelap semi-transparan.</summary>
        public static RectTransform CreatePanel(string name, Transform parent, Color bg, bool stretch = false)
        {
            var rt = CreateRect(name, parent, stretch);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = bg;
            img.type = Image.Type.Sliced;
            return rt;
        }

        /// <summary>Membuat Text uGUI.</summary>
        public static Text CreateText(string name, Transform parent, string content,
            int fontSize = 14, Color color = default,
            TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = CreateRect(name, parent, stretch: false);
            var txt = rt.gameObject.AddComponent<Text>();
            txt.text = content;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color == default ? TextDefault : color;
            txt.alignment = alignment;
            txt.fontStyle = style;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        /// <summary>Membuat Button dengan latar berwarna.</summary>
        public static Button CreateButton(string name, Transform parent, string label,
            Color bgColor, UnityEngine.Events.UnityAction onClick, int fontSize = 14)
        {
            var rt = CreateRect(name, parent, stretch: false);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = bgColor;
            img.type = Image.Type.Sliced;

            // Label
            var labelRt = CreateRect("Label", rt, stretch: true);
            var txt = labelRt.gameObject.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
            txt.raycastTarget = false;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            // Hover tint sederhana
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            return btn;
        }

        // ════════════════════════════════════════════════════════════════
        //  Modal management
        // ════════════════════════════════════════════════════════════════

        /// <summary>Menampilkan sebuah panel sebagai modal (menutup modal sebelumnya).</summary>
        public void ShowModal(GameObject panel)
        {
            if (_currentModal != null && _currentModal != panel)
                _currentModal.SetActive(false);
            _currentModal = panel;
            panel.SetActive(true);
        }

        /// <summary>Menutup modal yang sedang aktif.</summary>
        public void CloseModal()
        {
            if (_currentModal != null)
            {
                _currentModal.SetActive(false);
                _currentModal = null;
            }

            // Buka kunci pergerakan MC
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var ctrl = player.GetComponent<TanamSawit.Player.OpeningGameplayPlayerController>();
                if (ctrl != null) ctrl.SetMovementLocked(false);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Notification API (dipanggil oleh mana event handler)
        // ════════════════════════════════════════════════════════════════

        /// <summary>Notifikasi singkat (diteruskan ke NotificationTickerUI jika ada).</summary>
        public static void Notify(string message, float duration = 5f)
        {
            if (NotificationTickerUI.Instance != null)
                NotificationTickerUI.Instance.Show(message, duration);
            else if (ModernTycoonHUD.Instance != null)
                ModernTycoonHUD.Instance.ShowNotification(message, duration);
        }
    }
}
