using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TanamSawit.UI
{
    /// <summary>
    /// SettingsManager mengontrol jendela popup Pengaturan (Settings Popup Window) di dalam game.
    /// Dilengkapi dengan:
    /// 1. Sliding bar untuk kontrol Volume Suara (AudioListener.volume).
    /// 2. Toggle switch untuk Mode Layar (Fullscreen vs Windowed Mode).
    /// 3. Tombol Tutup (Close Button) di bagian bawah jendela popup.
    /// 4. Deteksi otomatis klik pada objek in-scene 'setting' (huruf SETTINGS di scene) saat play dimulai.
    /// 5. Integrasi dengan TycoonHUD dan MainMenuController.
    /// </summary>
    [DefaultExecutionOrder(-65)]
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Status Popup Window")]
        [SerializeField] private bool isPopupOpen = false;
        public bool IsPopupOpen => isPopupOpen;

        [Header("Nilai Pengaturan")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 0.8f;
        public float MasterVolume => masterVolume;

        [SerializeField] private bool isFullscreen = true;
        public bool IsFullscreen => isFullscreen;

        [Header("Deteksi Objek 'setting' di Scene")]
        [SerializeField] private GameObject inSceneSettingObject;

        // Events
        public event Action<bool> OnSettingsPopupToggled;
        public event Action<float> OnVolumeChanged;
        public event Action<bool> OnFullscreenChanged;

        // Custom GUI Styles
        private GUIStyle windowBoxStyle;
        private GUIStyle headerTitleStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle valueBadgeStyle;
        private GUIStyle closeButtonStyle;
        private bool stylesInitialized = false;

        #region Auto-Bootstrapping
        // Dinonaktifkan agar tidak otomatis membuat objek di scene yang ingin dimulai dari nol
        public static void EnsureInstanceExists()
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (IsExcludedScene(currentSceneName)) return;

            if (Instance == null)
            {
                GameObject managersGo = GameObject.Find("[MANAGERS]");
                if (managersGo != null)
                {
                    if (managersGo.GetComponent<SettingsManager>() == null)
                    {
                        managersGo.AddComponent<SettingsManager>();
                    }
                }
                else
                {
                    GameObject settingsHost = new GameObject("[SETTINGS_MANAGER]");
                    settingsHost.AddComponent<SettingsManager>();
                    DontDestroyOnLoad(settingsHost);
                }
            }
        }

        public static bool IsExcludedScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            string lower = sceneName.ToLower();
            return lower.Contains("movement") || lower.Contains("prototype_movement") || lower == "opening gameplay";
        }
        #endregion

        private void Awake()
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (IsExcludedScene(currentSceneName))
            {
                Destroy(gameObject);
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            LoadSettingsPreferences();
        }

        private void Start()
        {
            FindAndSetupInSceneSettingObject();
            FindAndSetupMainMenuHoverButtons();
            FindAndSetupQuitGraphicHover();
        }

        private void Update()
        {
            // Tekan tombol ESC di keyboard untuk menutup popup jika sedang terbuka
            if (isPopupOpen)
            {
                if (IsEscapePressed())
                {
                    CloseSettings();
                    return;
                }
            }
            else
            {
                // Deteksi klik mouse pada objek 'setting' di scene jika popup belum terbuka
                if (IsMouseButtonDown())
                {
                    CheckInSceneSettingClick();
                }
            }
        }

        private static bool IsEscapePressed()
        {
            return TanamSawit.Core.InputEdgeDetection.Escape();
        }

        private static bool IsMouseButtonDown()
        {
            return TanamSawit.Core.InputEdgeDetection.MouseLeft();
        }

        private static Vector3 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 pos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                return new Vector3(pos.x, pos.y, 0f);
            }
#endif
            try
            {
                return Input.mousePosition;
            }
            catch
            {
                return Vector3.zero;
            }
        }

        #region In-Scene Setting Object Detection
        /// <summary>
        /// Mencari objek 'setting' di dalam scene (misal teks 3D/2D SETTINGS)
        /// dan memasangkan Collider serta script trigger agar bisa diklik langsung.
        /// </summary>
        public void FindAndSetupInSceneSettingObject()
        {
            if (inSceneSettingObject != null) return;

            // 1. Cari berdasarkan nama tepat "setting"
            inSceneSettingObject = GameObject.Find("setting");

            // 2. Fallback: Cari yang mengandung kata "setting"
            if (inSceneSettingObject == null)
            {
#pragma warning disable CS0618
                var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#pragma warning restore CS0618
                foreach (var go in allObjects)
                {
                    string lowerName = go.name.ToLower();
                    if (lowerName.Contains("setting"))
                    {
                        inSceneSettingObject = go;
                        break;
                    }
                }
            }

            if (inSceneSettingObject != null)
            {
                // Pastikan memiliki BoxCollider2D agar bisa mendeteksi klik kursor
                BoxCollider2D col2D = inSceneSettingObject.GetComponent<BoxCollider2D>();
                if (col2D == null)
                {
                    col2D = inSceneSettingObject.AddComponent<BoxCollider2D>();
                    
                    // Sesuaikan ukuran dengan bounds MeshRenderer / Renderer
                    Renderer r = inSceneSettingObject.GetComponent<Renderer>();
                    if (r != null)
                    {
                        col2D.size = new Vector2(Mathf.Max(2f, r.bounds.size.x), Mathf.Max(1f, r.bounds.size.y));
                    }
                    else
                    {
                        col2D.size = new Vector2(3.5f, 1.2f);
                    }
                }

                // Pasang trigger helper script
                if (inSceneSettingObject.GetComponent<WorldSettingClickTrigger>() == null)
                {
                    inSceneSettingObject.AddComponent<WorldSettingClickTrigger>();
                }

                Debug.Log($"<color=#00FF66>[SettingsManager]</color> Berhasil menghubungkan objek interaktif '{inSceneSettingObject.name}' di scene.");
            }
        }

        /// <summary>
        /// Mendeteksi klik mouse pada objek 'setting' di scene menggunakan raycast & bounds fallback.
        /// </summary>
        private void CheckInSceneSettingClick()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(GetMousePosition());
            Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            // Cek Raycast 2D
            RaycastHit2D hit2D = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit2D.collider != null && hit2D.collider.gameObject != null)
            {
                if (IsMatchingSettingObject(hit2D.collider.gameObject))
                {
                    OpenSettings();
                    return;
                }
            }

            // Fallback via bounds renderer jika collider physics belum memproses
            if (inSceneSettingObject != null)
            {
                Renderer r = inSceneSettingObject.GetComponent<Renderer>();
                if (r != null)
                {
                    Bounds b = r.bounds;
                    b.Expand(new Vector3(0.5f, 0.5f, 5f)); // Beri sedikit kelonggaran klik
                    if (b.Contains(new Vector3(mouseWorldPos.x, mouseWorldPos.y, b.center.z)))
                    {
                        OpenSettings();
                        return;
                    }
                }
                else
                {
                    float dist = Vector2.Distance(mousePos2D, new Vector2(inSceneSettingObject.transform.position.x, inSceneSettingObject.transform.position.y));
                    if (dist <= 2.2f)
                    {
                        OpenSettings();
                        return;
                    }
                }
            }
        }

        private bool IsMatchingSettingObject(GameObject target)
        {
            if (target == null) return false;
            if (target == inSceneSettingObject) return true;
            string name = target.name.ToLower();
            return name.Contains("setting");
        }

        /// <summary>
        /// Memasang efek hover pada label menu utama yang tersedia di scene.
        /// PLAY memakai bold + border abu-abu, sedangkan QUIT/EXIT memakai bold
        /// + border putih. Pencarian berdasarkan nama dan isi label agar tetap
        /// bekerja jika prefab menu diganti.
        /// </summary>
        private void FindAndSetupMainMenuHoverButtons()
        {
#pragma warning disable CS0618
            TextMesh[] menuLabels = FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
#pragma warning restore CS0618
            foreach (TextMesh menuLabel in menuLabels)
            {
                string identifier = $"{menuLabel.gameObject.name} {menuLabel.text}".ToLowerInvariant();
                if (identifier.Contains("setting")) continue; // Ditangani WorldSettingClickTrigger.

                if (identifier.Contains("play"))
                    SetupMenuTextHover(menuLabel.gameObject, Color.grey);
                else if (identifier.Contains("quit") || identifier.Contains("exit"))
                    SetupMenuTextHover(menuLabel.gameObject, Color.white);
            }
        }

        private static void SetupMenuTextHover(GameObject target, Color borderColor)
        {
            if (target.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
                Renderer renderer = target.GetComponent<Renderer>();
                collider.size = renderer != null
                    ? new Vector2(Mathf.Max(1f, renderer.bounds.size.x), Mathf.Max(0.5f, renderer.bounds.size.y))
                    : new Vector2(3f, 1f);
            }

            WorldMenuTextHover hover = target.GetComponent<WorldMenuTextHover>();
            if (hover == null) hover = target.AddComponent<WorldMenuTextHover>();
            hover.Configure(borderColor);
        }

        /// <summary>
        /// Tombol Quit pada prototipe adalah gambar Tilemap di bawah MAINMENU.
        /// Jangan mencari Tilemap secara global karena gameplay juga memiliki
        /// Tilemap yang harus tetap walkable.
        /// </summary>
        private static void FindAndSetupQuitGraphicHover()
        {
            GameObject menuRoot = GameObject.Find("MAINMENU");
            if (menuRoot == null) return;

            Tilemap quitTilemap = menuRoot.GetComponentInChildren<Tilemap>(true);
            if (quitTilemap == null) return;

            GameObject quitGraphic = quitTilemap.gameObject;

            Renderer renderer = quitGraphic.GetComponent<Renderer>();
            if (renderer == null) return;

            if (quitGraphic.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D collider = quitGraphic.AddComponent<BoxCollider2D>();
                Bounds bounds = renderer.bounds;
                collider.offset = quitGraphic.transform.InverseTransformPoint(bounds.center);
                collider.size = quitGraphic.transform.InverseTransformVector(bounds.size);
            }

            if (quitGraphic.GetComponent<WorldMenuQuitGraphicHover>() == null)
                quitGraphic.AddComponent<WorldMenuQuitGraphicHover>();
        }
        #endregion

        #region Public Control Methods
        /// <summary>
        /// Membuka jendela popup Pengaturan.
        /// </summary>
        public void OpenSettings()
        {
            isPopupOpen = true;
            OnSettingsPopupToggled?.Invoke(true);
            Debug.Log("[SettingsManager] Popup Pengaturan dibuka.");
        }

        /// <summary>
        /// Menutup jendela popup Pengaturan.
        /// </summary>
        public void CloseSettings()
        {
            isPopupOpen = false;
            OnSettingsPopupToggled?.Invoke(false);
            SaveSettingsPreferences();
            Debug.Log("[SettingsManager] Popup Pengaturan ditutup.");
        }

        /// <summary>
        /// Buka/tutup jendela popup Pengaturan secara bergantian (toggle).
        /// </summary>
        public void ToggleSettings()
        {
            if (isPopupOpen)
                CloseSettings();
            else
                OpenSettings();
        }

        /// <summary>
        /// Mengatur volume master game (0.0 - 1.0).
        /// </summary>
        public void SetVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = masterVolume;
            OnVolumeChanged?.Invoke(masterVolume);
        }

        /// <summary>
        /// Mengatur mode tampilan layar (Fullscreen vs Windowed).
        /// </summary>
        public void SetFullscreenMode(bool fullscreen)
        {
            isFullscreen = fullscreen;
            Screen.fullScreen = isFullscreen;
            Screen.fullScreenMode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            OnFullscreenChanged?.Invoke(isFullscreen);
            Debug.Log($"[SettingsManager] Mode Layar diubah ke: {(isFullscreen ? "FULLSCREEN (Layar Penuh)" : "WINDOWED (Mode Jendela)")}");
        }
        #endregion

        #region Preferences Persistence
        private void LoadSettingsPreferences()
        {
            masterVolume = PlayerPrefs.GetFloat("Settings_Volume", 0.8f);
            AudioListener.volume = masterVolume;

            int fsVal = PlayerPrefs.GetInt("Settings_Fullscreen", Screen.fullScreen ? 1 : 0);
            isFullscreen = (fsVal == 1);
            Screen.fullScreen = isFullscreen;
            Screen.fullScreenMode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        private void SaveSettingsPreferences()
        {
            PlayerPrefs.SetFloat("Settings_Volume", masterVolume);
            PlayerPrefs.SetInt("Settings_Fullscreen", isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
        #endregion

        #region OnGUI Popup Rendering
        private void InitStylesIfNeeded()
        {
            if (stylesInitialized) return;

            // Box Style untuk popup window
            windowBoxStyle = new GUIStyle(GUI.skin.box);
            windowBoxStyle.fontSize = 14;
            windowBoxStyle.fontStyle = FontStyle.Bold;

            headerTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerTitleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f); // Golden yellow

            sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            sectionHeaderStyle.normal.textColor = Color.white;

            valueBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleRight
            };
            valueBadgeStyle.normal.textColor = new Color(0.2f, 1f, 0.4f); // Neon green

            closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (IsExcludedScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)) return;
            if (!isPopupOpen) return;

            InitStylesIfNeeded();

            // Atur kedalaman GUI paling atas agar jendela popup menutupi elemen lain
            GUI.depth = -100;

            // 1. DIMMED BACKDROP (Latar Belakang Redup Transparan)
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 2. MODAL POPUP WINDOW (Di Tengah Layar)
            float popupWidth = 460f;
            float popupHeight = 400f;
            float popupX = (Screen.width - popupWidth) / 2f;
            float popupY = (Screen.height - popupHeight) / 2f;
            Rect popupRect = new Rect(popupX, popupY, popupWidth, popupHeight);

            // Jangan gunakan GUI.Button fullscreen sebagai blocker: kontrol tersebut
            // mengambil hotControl lebih dulu dan membuat tombol di dalam modal tidak
            // pernah menerima MouseUp. Hanya klik DI LUAR modal yang dikonsumsi.
            if (Event.current.type == EventType.MouseDown && !popupRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
            }

            // Box Container Jendela Popup
            GUI.Box(popupRect, GUIContent.none);

            // Konten di dalam Popup Window
            GUILayout.BeginArea(new Rect(popupX + 20, popupY + 15, popupWidth - 40, popupHeight - 30));

            // --- HEADER TITLE ---
            GUILayout.Label("<b>⚙️ PENGATURAN / SETTINGS</b>", headerTitleStyle);
            GUILayout.Label("<color=#CCCCCC><size=11>Atur preferensi audio dan mode tampilan game Anda.</size></color>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(12);

            // --- SECTION 1: VOLUME CONTROL (SLIDING BAR) ---
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>🔊 Kontrol Volume Master</b>", sectionHeaderStyle);
            int volumePercent = Mathf.RoundToInt(masterVolume * 100f);
            string volumeTag = masterVolume == 0f ? "<color=#FF5555>MUTE (0%)</color>" : $"<color=#00FF66>{volumePercent}%</color>";
            GUILayout.Label(volumeTag, valueBadgeStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // SLIDING BAR (Volume Slider)
            float newVolume = GUILayout.HorizontalSlider(masterVolume, 0f, 1f, GUILayout.Height(22));
            if (Math.Abs(newVolume - masterVolume) > 0.001f)
            {
                SetVolume(newVolume);
            }

            // Tombol Preset Cepat Volume
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mute (0%)", GUILayout.Height(20))) SetVolume(0f);
            if (GUILayout.Button("25%", GUILayout.Height(20))) SetVolume(0.25f);
            if (GUILayout.Button("50%", GUILayout.Height(20))) SetVolume(0.5f);
            if (GUILayout.Button("75%", GUILayout.Height(20))) SetVolume(0.75f);
            if (GUILayout.Button("100%", GUILayout.Height(20))) SetVolume(1f);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(12);

            // --- SECTION 2: WINDOW MODE / FULLSCREEN (TOGGLE SWITCH) ---
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>🖥️ Mode Layar (Display Mode)</b>", sectionHeaderStyle);
            string modeTag = isFullscreen ? "<color=#00FF66>FULLSCREEN</color>" : "<color=#FFCC00>WINDOWED</color>";
            GUILayout.Label(modeTag, valueBadgeStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // TOGGLE SWITCH (Checkbox Switch)
            string toggleLabel = isFullscreen
                ? "  <b>[✔] Layar Penuh (Fullscreen Mode)</b>"
                : "  <b>[   ] Mode Jendela (Windowed Mode)</b>";

            bool newToggleState = GUILayout.Toggle(isFullscreen, toggleLabel, GUILayout.Height(26));
            if (newToggleState != isFullscreen)
            {
                SetFullscreenMode(newToggleState);
            }

            // Pilihan tombol alternatif Fullscreen vs Windowed
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUI.color = isFullscreen ? new Color(0.4f, 1f, 0.4f) : Color.white;
            if (GUILayout.Button("🔲 Mode Fullscreen", GUILayout.Height(22)))
            {
                SetFullscreenMode(true);
            }

            GUI.color = !isFullscreen ? new Color(1f, 0.8f, 0.3f) : Color.white;
            if (GUILayout.Button("🪟 Mode Windowed", GUILayout.Height(22)))
            {
                SetFullscreenMode(false);
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndArea();

            // Tombol close dibuat dengan Rect tetap dan digambar PALING AKHIR.
            // Ini mencegah kontrol GUILayout/OnGUI lain menangkap klik sebelum tombol tutup.
            Rect closeButtonRect = new Rect(popupX + 20f, popupY + popupHeight - 55f, popupWidth - 40f, 40f);
            GUI.backgroundColor = new Color(0.9f, 0.25f, 0.25f);
            bool closeClicked = GUI.Button(closeButtonRect, "<b>✖ TUTUP / CLOSE</b>", closeButtonStyle);
            GUI.backgroundColor = Color.white;

            if (closeClicked)
            {
                // Konsumsi event agar tidak diteruskan ke HUD atau objek setting di belakang modal.
                Event.current.Use();
                CloseSettings();
            }
        }
        #endregion
    }

    /// <summary>
    /// Script pembantu yang ditempelkan ke objek in-scene 'setting'. Selain membuka
    /// popup saat diklik, script ini memberi efek hover pada TextMesh: font menjadi
    /// bold putih dengan border putih agar tombol Settings lebih jelas.
    /// </summary>
    public class WorldSettingClickTrigger : MonoBehaviour
    {
        private static readonly Vector2[] OutlineOffsets =
        {
            new Vector2(-0.025f, 0f), new Vector2(0.025f, 0f),
            new Vector2(0f, -0.025f), new Vector2(0f, 0.025f),
            new Vector2(-0.018f, -0.018f), new Vector2(-0.018f, 0.018f),
            new Vector2(0.018f, -0.018f), new Vector2(0.018f, 0.018f)
        };

        private TextMesh label;
        private Color defaultColor;
        private FontStyle defaultFontStyle;
        private GameObject[] outlineLetters;

        private void Awake()
        {
            label = GetComponent<TextMesh>();
            if (label == null) return;

            defaultColor = label.color;
            defaultFontStyle = label.fontStyle;
            CreateWhiteOutline();
        }

        private void OnMouseEnter()
        {
            SetHoverVisual(true);
        }

        private void OnMouseExit()
        {
            SetHoverVisual(false);
        }

        private void OnMouseDown()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OpenSettings();
            }
        }

        private void OnDisable()
        {
            SetHoverVisual(false);
        }

        private void CreateWhiteOutline()
        {
            outlineLetters = new GameObject[OutlineOffsets.Length];
            MeshRenderer baseRenderer = GetComponent<MeshRenderer>();

            for (int i = 0; i < OutlineOffsets.Length; i++)
            {
                GameObject outlineObject = new GameObject("SettingsHoverWhiteOutline");
                outlineObject.transform.SetParent(transform, false);
                outlineObject.transform.localPosition = new Vector3(OutlineOffsets[i].x, OutlineOffsets[i].y, 0.01f);

                TextMesh outlineText = outlineObject.AddComponent<TextMesh>();
                outlineText.text = label.text;
                outlineText.anchor = label.anchor;
                outlineText.alignment = label.alignment;
                outlineText.characterSize = label.characterSize;
                outlineText.font = label.font;
                outlineText.fontSize = label.fontSize;
                outlineText.fontStyle = FontStyle.Bold;
                outlineText.richText = label.richText;
                outlineText.color = Color.white;

                MeshRenderer outlineRenderer = outlineObject.GetComponent<MeshRenderer>();
                if (baseRenderer != null && outlineRenderer != null)
                {
                    outlineRenderer.sortingLayerID = baseRenderer.sortingLayerID;
                    outlineRenderer.sortingOrder = baseRenderer.sortingOrder - 1;
                }

                outlineObject.SetActive(false);
                outlineLetters[i] = outlineObject;
            }
        }

        private void SetHoverVisual(bool isHovered)
        {
            if (label == null) return;

            label.color = isHovered ? Color.white : defaultColor;
            label.fontStyle = isHovered ? FontStyle.Bold : defaultFontStyle;

            if (outlineLetters == null) return;
            foreach (GameObject outlineObject in outlineLetters)
            {
                if (outlineObject != null)
                    outlineObject.SetActive(isHovered);
            }
        }
    }

    /// <summary>
    /// Efek hover reusable untuk tombol menu berbasis TextMesh di dunia 2D.
    /// Membuat salinan teks kecil di delapan arah sebagai border berwarna.
    /// </summary>
    public class WorldMenuTextHover : MonoBehaviour
    {
        private static readonly Vector2[] OutlineOffsets =
        {
            new Vector2(-0.025f, 0f), new Vector2(0.025f, 0f),
            new Vector2(0f, -0.025f), new Vector2(0f, 0.025f),
            new Vector2(-0.018f, -0.018f), new Vector2(-0.018f, 0.018f),
            new Vector2(0.018f, -0.018f), new Vector2(0.018f, 0.018f)
        };

        private TextMesh label;
        private Color defaultColor;
        private FontStyle defaultFontStyle;
        private Color hoverColor = Color.grey;
        private GameObject[] outlines;

        public void Configure(Color color)
        {
            hoverColor = color;
            if (outlines == null) return;
            foreach (GameObject outline in outlines)
            {
                if (outline != null)
                    outline.GetComponent<TextMesh>().color = hoverColor;
            }
        }

        private void Awake()
        {
            label = GetComponent<TextMesh>();
            if (label == null) return;

            defaultColor = label.color;
            defaultFontStyle = label.fontStyle;
            CreateOutlines();
        }

        private void OnMouseEnter() => SetHovered(true);
        private void OnMouseExit() => SetHovered(false);
        private void OnDisable() => SetHovered(false);

        private void CreateOutlines()
        {
            outlines = new GameObject[OutlineOffsets.Length];
            MeshRenderer baseRenderer = GetComponent<MeshRenderer>();

            for (int i = 0; i < OutlineOffsets.Length; i++)
            {
                GameObject outline = new GameObject("MenuHoverOutline");
                outline.transform.SetParent(transform, false);
                outline.transform.localPosition = new Vector3(OutlineOffsets[i].x, OutlineOffsets[i].y, 0.01f);

                TextMesh outlineText = outline.AddComponent<TextMesh>();
                outlineText.text = label.text;
                outlineText.anchor = label.anchor;
                outlineText.alignment = label.alignment;
                outlineText.characterSize = label.characterSize;
                outlineText.font = label.font;
                outlineText.fontSize = label.fontSize;
                outlineText.fontStyle = FontStyle.Bold;
                outlineText.richText = label.richText;
                outlineText.color = hoverColor;

                MeshRenderer outlineRenderer = outline.GetComponent<MeshRenderer>();
                if (baseRenderer != null && outlineRenderer != null)
                {
                    outlineRenderer.sortingLayerID = baseRenderer.sortingLayerID;
                    outlineRenderer.sortingOrder = baseRenderer.sortingOrder - 1;
                }

                outline.SetActive(false);
                outlines[i] = outline;
            }
        }

        private void SetHovered(bool isHovered)
        {
            if (label == null) return;
            label.fontStyle = isHovered ? FontStyle.Bold : defaultFontStyle;

            if (outlines == null) return;
            foreach (GameObject outline in outlines)
            {
                if (outline != null)
                    outline.SetActive(isHovered);
            }
        }
    }

    /// <summary>
    /// Border hover untuk tombol Quit yang dibuat dari Tilemap/sprite, bukan font.
    /// </summary>
    public class WorldMenuQuitGraphicHover : MonoBehaviour
    {
        private readonly SpriteRenderer[] borderEdges = new SpriteRenderer[4];

        private void Awake()
        {
            CreateBorder();
            SetBorderVisible(false);
        }

        private void OnMouseEnter() => SetBorderVisible(true);
        private void OnMouseExit() => SetBorderVisible(false);
        private void OnDisable() => SetBorderVisible(false);

        private void CreateBorder()
        {
            Renderer targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null) return;

            Bounds bounds = targetRenderer.bounds;
            const float thickness = 0.055f;
            Sprite whiteSprite = CreateWhiteSprite();

            borderEdges[0] = CreateBorderEdge("QuitHoverBorderTop", whiteSprite,
                new Vector3(bounds.center.x, bounds.max.y + thickness * 0.5f, bounds.center.z),
                new Vector2(bounds.size.x + thickness * 2f, thickness));
            borderEdges[1] = CreateBorderEdge("QuitHoverBorderBottom", whiteSprite,
                new Vector3(bounds.center.x, bounds.min.y - thickness * 0.5f, bounds.center.z),
                new Vector2(bounds.size.x + thickness * 2f, thickness));
            borderEdges[2] = CreateBorderEdge("QuitHoverBorderLeft", whiteSprite,
                new Vector3(bounds.min.x - thickness * 0.5f, bounds.center.y, bounds.center.z),
                new Vector2(thickness, bounds.size.y));
            borderEdges[3] = CreateBorderEdge("QuitHoverBorderRight", whiteSprite,
                new Vector3(bounds.max.x + thickness * 0.5f, bounds.center.y, bounds.center.z),
                new Vector2(thickness, bounds.size.y));
        }

        private SpriteRenderer CreateBorderEdge(string edgeName, Sprite sprite, Vector3 position, Vector2 size)
        {
            GameObject edge = new GameObject(edgeName);
            edge.transform.position = position + Vector3.back * 0.01f;
            SpriteRenderer edgeRenderer = edge.AddComponent<SpriteRenderer>();
            edgeRenderer.sprite = sprite;
            edgeRenderer.color = Color.white;
            edgeRenderer.sortingOrder = 10;
            edge.transform.localScale = new Vector3(size.x, size.y, 1f);
            return edgeRenderer;
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void SetBorderVisible(bool isVisible)
        {
            foreach (SpriteRenderer edge in borderEdges)
            {
                if (edge != null)
                    edge.enabled = isVisible;
            }
        }
    }
}
