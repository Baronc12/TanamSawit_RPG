using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TanamSawit.Environment;
using TanamSawit.Player;

namespace TanamSawit.UI
{
    /// <summary>
    /// Modal fasilitas uGUI yang reusable. Satu modal untuk semua 12 FacilityType.
    /// Berlangganan ke <see cref="InteractableFacility.OnFacilityInteracted"/> dan
    /// membangun konten dari <see cref="FacilityModalDefinitions"/>.
    /// Menggantikan 10 method DrawModalXXX di ModernTycoonHUD.
    /// </summary>
    [DefaultExecutionOrder(-19)]
    public class FacilityModalUI : MonoBehaviour
    {
        public static FacilityModalUI Instance { get; private set; }

        private GameObject _panel;
        private Text _titleText;
        private Text _descText;
        private RectTransform _buttonList;
        private FacilityDefinition _currentDef;

        // Pool tombol (dibuat sekali, dipakai ulang)
        private readonly List<Button> _buttonPool = new List<Button>();
        private readonly List<GameObject> _buttonGOs = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
            _panel.SetActive(false);
            Debug.Log("[FacilityModalUI] Awake complete. Panel built and hidden.");
        }

        private void OnEnable()
        {
            Debug.Log("[FacilityModalUI] OnEnable: subscribing to OnFacilityInteracted.");
            InteractableFacility.OnFacilityInteracted += HandleFacilityInteracted;
        }

        private void OnDisable()
        {
            InteractableFacility.OnFacilityInteracted -= HandleFacilityInteracted;
            Debug.Log("[FacilityModalUI] OnDisable: unsubscribed from OnFacilityInteracted.");
        }

        // ── Build UI ─────────────────────────────────────────────────────

        private void BuildUI()
        {
            // Dim backdrop
            _panel = new GameObject("FacilityModal");
            if (UIRoot.PanelRoot == null)
            {
                Debug.LogError("[FacilityModalUI] UIRoot.PanelRoot is null! Panel will not be parented to canvas.");
            }
            _panel.transform.SetParent(UIRoot.PanelRoot, false);

            var backdropRt = _panel.AddComponent<RectTransform>();
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;

            var backdropImg = _panel.AddComponent<Image>();
            backdropImg.color = new Color(0, 0, 0, 0.55f);

            // Center panel
            var center = UIRoot.CreatePanel("CenterPanel", _panel.transform, UIRoot.BgPanel);
            center.anchorMin = new Vector2(0.5f, 0.5f);
            center.anchorMax = new Vector2(0.5f, 0.5f);
            center.pivot = new Vector2(0.5f, 0.5f);
            center.sizeDelta = new Vector2(560, 480);
            center.anchoredPosition = Vector2.zero;

            // Title
            _titleText = UIRoot.CreateText("Title", center, "", 18, UIRoot.TextGold, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetRect(_titleText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -14), new Vector2(-16, -46));

            // Description
            _descText = UIRoot.CreateText("Desc", center, "", 13, UIRoot.TextDefault, TextAnchor.UpperLeft, FontStyle.Normal);
            SetRect(_descText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -54), new Vector2(-16, -260));

            // Button list area
            _buttonList = UIRoot.CreateRect("ButtonList", center);
            SetRect(_buttonList, new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 50), new Vector2(-16, -268));

            var vlg = _buttonList.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Close button
            var closeBtn = UIRoot.CreateButton("CloseBtn", center, "\u274C Tutup [Esc]",
                UIRoot.BgButtonDanger, () => UIRoot.Instance.CloseModal(), 14);
            SetRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 8), new Vector2(-16, 42));
        }

        // ── Event handler ────────────────────────────────────────────────

        private void HandleFacilityInteracted(FacilityType type, string name)
        {
            Debug.Log($"<color=#00FF88>[FacilityModalUI]</color> HandleFacilityInteracted called: {type} / {name}");

            // Bangun konten modal dengan try-catch agar ShowModal selalu dipanggil
            // meski ada exception saat membangun konten (mis. manager null).
            try
            {
                _currentDef = FacilityModalDefinitions.Get(type);
                Debug.Log($"[FacilityModalUI] _currentDef set. title='{_currentDef?.title}', hasDesc={_currentDef?.getDescription != null}, buttonCount={_currentDef?.buttons?.Count}");
                if (_titleText != null)
                    _titleText.text = _currentDef.title;
                RebuildButtons();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FacilityModalUI] Exception building content: {ex}");
            }

            // Kunci pergerakan MC
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var ctrl = player.GetComponent<OpeningGameplayPlayerController>();
                if (ctrl != null) ctrl.SetMovementLocked(true);
            }

            // ShowModal HARUS selalu dipanggil
            if (UIRoot.Instance != null)
            {
                UIRoot.Instance.ShowModal(_panel);
                Debug.Log("<color=#00FF88>[FacilityModalUI]</color> ShowModal called successfully.");
                // Refresh content immediately so title/desc/buttons are up-to-date on first frame
                RefreshContent();
            }
            else
            {
                Debug.LogError("[FacilityModalUI] UIRoot.Instance is null! Cannot show modal.");
                if (_panel != null) _panel.SetActive(true);
            }
        }

        // ── Button management ────────────────────────────────────────────

        private void RebuildButtons()
        {
            // Sembunyikan semua tombol lama
            foreach (var go in _buttonGOs)
            {
                if (go != null) go.SetActive(false);
            }

            if (_currentDef?.buttons == null) return;

            // Buat tombol baru atau pakai ulang dari pool
            for (int i = 0; i < _currentDef.buttons.Count; i++)
            {
                var spec = _currentDef.buttons[i];
                bool visible = spec.isVisible == null || spec.isVisible();

                if (i < _buttonGOs.Count)
                {
                    // Pakai ulang tombol dari pool
                    var go = _buttonGOs[i];
                    go.SetActive(visible);
                    var btn = _buttonPool[i];
                    var txt = go.GetComponentInChildren<Text>();
                    txt.text = spec.label;
                    var img = go.GetComponent<Image>();
                    img.color = spec.buttonColor;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { spec.onClick?.Invoke(); RefreshContent(); });
                }
                else
                {
                    // Buat tombol baru
                    var btn = UIRoot.CreateButton($"Btn_{i}", _buttonList, spec.label,
                        spec.buttonColor, () => { spec.onClick?.Invoke(); RefreshContent(); }, 14);
                    var le = btn.gameObject.AddComponent<LayoutElement>();
                    le.preferredHeight = 36;
                    _buttonPool.Add(btn);
                    _buttonGOs.Add(btn.gameObject);
                    btn.gameObject.SetActive(visible);
                }

                if (visible && i < _buttonGOs.Count)
                {
                    _buttonGOs[i].SetActive(true);
                }
            }
        }

        // ── Refresh (update description + button states) ──────────────────

        private void RefreshContent()
        {
            if (_currentDef == null || !_panel.activeSelf)
            {
                Debug.Log($"[FacilityModalUI] RefreshContent early-out: _currentDef==null={_currentDef == null}, _panel.activeSelf={_panel?.activeSelf}");
                return;
            }

            // Update description
            if (_currentDef.getDescription != null)
            {
                string desc = _currentDef.getDescription();
                _descText.text = desc;
                Debug.Log($"[FacilityModalUI] RefreshContent: desc set to '{desc?.Substring(0, Mathf.Min(50, desc.Length))}...'");
            }
            else
            {
                Debug.LogWarning("[FacilityModalUI] RefreshContent: _currentDef.getDescription is null");
            }

            // Update button visibility
            if (_currentDef.buttons != null)
            {
                for (int i = 0; i < _currentDef.buttons.Count && i < _buttonGOs.Count; i++)
                {
                    var spec = _currentDef.buttons[i];
                    bool visible = spec.isVisible == null || spec.isVisible();
                    _buttonGOs[i].SetActive(visible);

                    if (visible)
                    {
                        var btn = _buttonPool[i];
                        bool enabled = spec.isEnabled == null || spec.isEnabled();
                        btn.interactable = enabled;
                    }
                }
            }
        }

        private void Update()
        {
            if (_panel.activeSelf) RefreshContent();
        }

        // ── Layout helper ────────────────────────────────────────────────

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
