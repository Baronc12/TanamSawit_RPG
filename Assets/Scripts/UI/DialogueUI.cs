using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TanamSawit.Player;

namespace TanamSawit.NPC
{
    /// <summary>
    /// Panel dialog uGUI: nama pembicara, teks typewriter, tombol pilihan.
    /// Mengunci pergerakan MC saat terbuka, membuka kunci saat ditutup.
    /// Dipanggil oleh NPCInteractable. Mengelola alur via DialogueRunner.
    /// </summary>
    [DefaultExecutionOrder(-17)]
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        /// <summary>True jika dialog sedang terbuka.</summary>
        public bool IsOpen => _isOpen;

        private GameObject _panel;
        private Text _nameText;
        private Text _bodyText;
        private RectTransform _choiceArea;
        private Button[] _choiceButtons = new Button[4];

        private DialogueRunner _runner;
        private Coroutine _typewriterCoroutine;
        private bool _isTyping = false;
        private string _fullText = "";
        private bool _isOpen = false;

        [SerializeField] private float typeSpeed = 0.03f; // detik per karakter

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
        }

        private void OnDisable()
        {
            // Selalu lepas lock saat dimatikan
            UnlockPlayer();
        }

        // ── Build UI ─────────────────────────────────────────────────────

        private void BuildUI()
        {
            _panel = new GameObject("DialoguePanel");
            _panel.transform.SetParent(TanamSawit.UI.UIRoot.PanelRoot, false);

            var rt = _panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(-80, 200);
            rt.anchoredPosition = new Vector2(0, 20);

            var bg = _panel.AddComponent<Image>();
            bg.sprite = TanamSawit.UI.UIRoot.WhiteSprite;
            bg.color = TanamSawit.UI.UIRoot.BgPanel;
            bg.type = Image.Type.Sliced;

            // Nama pembicara
            _nameText = TanamSawit.UI.UIRoot.CreateText("Name", _panel.transform, "", 16,
                TanamSawit.UI.UIRoot.TextGold, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetRect(_nameText.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(20, -12), new Vector2(-20, -38));

            // Body text (typewriter)
            _bodyText = TanamSawit.UI.UIRoot.CreateText("Body", _panel.transform, "", 14,
                TanamSawit.UI.UIRoot.TextDefault, TextAnchor.UpperLeft, FontStyle.Normal);
            SetRect(_bodyText.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(20, -44), new Vector2(-20, -100));

            // Pilihan area
            _choiceArea = TanamSawit.UI.UIRoot.CreateRect("ChoiceArea", _panel.transform);
            SetRect(_choiceArea, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(20, 12), new Vector2(-20, -104));

            var vlg = _choiceArea.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            // Pre-create 4 choice buttons (hidden by default)
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var btn = TanamSawit.UI.UIRoot.CreateButton($"Choice_{i}", _choiceArea, "",
                    TanamSawit.UI.UIRoot.BgButton, () => OnChoiceClicked(idx), 13);
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 30;
                _choiceButtons[i] = btn;
                btn.gameObject.SetActive(false);
            }
        }

        // ── Public API ───────────────────────────────────────────────────

        public void StartDialogue(DialogueAsset asset, System.Func<int, string> dynamicResolver = null)
        {
            if (asset == null || asset.nodes.Count == 0) return;

            _runner = new DialogueRunner();
            _runner.OnActionId += HandleActionId;
            _runner.OnDialogueEnd += CloseDialogue;
            _runner.Start(asset, dynamicResolver);

            LockPlayer();
            _isOpen = true;
            if (TanamSawit.UI.UIRoot.Instance != null)
                TanamSawit.UI.UIRoot.Instance.ShowModal(_panel);
            else
                _panel.SetActive(true);

            ShowCurrentNode();
        }

        public void CloseDialogue()
        {
            _isOpen = false;
            _panel.SetActive(false);
            if (TanamSawit.UI.UIRoot.Instance != null)
                TanamSawit.UI.UIRoot.Instance.CloseModal();
            UnlockPlayer();

            if (_runner != null)
            {
                _runner.OnActionId -= HandleActionId;
                _runner.OnDialogueEnd -= CloseDialogue;
                _runner = null;
            }

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
        }

        // ── Node display ─────────────────────────────────────────────────

        private void ShowCurrentNode()
        {
            var node = _runner?.Current;
            if (node == null) { CloseDialogue(); return; }

            // Update speaker name
            var speaker = _runner.Speaker;
            if (speaker != null)
            {
                _nameText.text = speaker.displayName;
                _nameText.color = speaker.accentColor;
            }

            // Typewriter text
            _fullText = _runner.GetCurrentText();
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(_fullText));

            // Setup choices
            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                if (i < node.choices.Count)
                {
                    _choiceButtons[i].GetComponentInChildren<Text>().text = node.choices[i].text;
                    _choiceButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    _choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            _isTyping = true;
            _bodyText.text = "";

            for (int i = 0; i < text.Length; i++)
            {
                _bodyText.text += text[i];
                yield return new WaitForSeconds(typeSpeed);
            }

            _isTyping = false;

            // Jika tidak ada choices, tampilkan hint lanjut
            var node = _runner?.Current;
            if (node != null && node.choices.Count == 0)
            {
                _bodyText.text = text + "\n\n<size=10><i>[E] / klik untuk lanjut</i></size>";
            }
        }

        // ── Input handling ───────────────────────────────────────────────

        private void Update()
        {
            if (!_isOpen) return;

            // Self-heal: jika panel ditutup secara eksternal (ShowModal untuk
            // modal fasilitas lain, atau Tab/Escape via TopBarUI), bersihkan
            // state dialog tanpa menutup modal yang sekarang aktif.
            if (!_panel.activeSelf)
            {
                _isOpen = false;
                _isTyping = false;
                if (_runner != null)
                {
                    _runner.OnActionId -= HandleActionId;
                    _runner.OnDialogueEnd -= CloseDialogue;
                    _runner = null;
                }
                if (_typewriterCoroutine != null)
                {
                    StopCoroutine(_typewriterCoroutine);
                    _typewriterCoroutine = null;
                }
                return;
            }

            // E / klik untuk advance atau skip typewriter
            bool advancePressed = IsAdvancePressed();

            if (advancePressed)
            {
                if (_isTyping)
                {
                    // Skip typewriter
                    if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                    _isTyping = false;
                    _bodyText.text = _fullText;

                    var node = _runner?.Current;
                    if (node != null && node.choices.Count == 0)
                        _bodyText.text = _fullText + "\n\n<size=10><i>[E] / klik untuk lanjut</i></size>";
                }
                else
                {
                    // Advance hanya jika tidak ada choices
                    var node = _runner?.Current;
                    if (node != null && node.choices.Count == 0)
                        _runner.Advance();

                    var next = _runner?.Current;
                    if (next != null) ShowCurrentNode();
                }
            }

            // ESC menutup dialog
            if (IsEscPressed())
                CloseDialogue();
        }

        private void OnChoiceClicked(int index)
        {
            if (_isTyping) return; // Jangan izinkan pilih saat typewriter
            _runner?.Choose(index);

            var next = _runner?.Current;
            if (next != null) ShowCurrentNode();
        }

        private void HandleActionId(string actionId)
        {
            DialogueActionRouter.Route(actionId);
        }

        // ── Player lock ──────────────────────────────────────────────────

        private void LockPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var ctrl = player.GetComponent<OpeningGameplayPlayerController>();
                if (ctrl != null) ctrl.SetMovementLocked(true);
            }
        }

        private void UnlockPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var ctrl = player.GetComponent<OpeningGameplayPlayerController>();
                if (ctrl != null) ctrl.SetMovementLocked(false);
            }
        }

        // ── Input helpers ───────────────────────────────────────────────

        private static bool IsAdvancePressed()
        {
            return TanamSawit.Core.InputEdgeDetection.AdvanceDialogue();
        }

        private static bool IsEscPressed()
        {
            return TanamSawit.Core.InputEdgeDetection.Escape();
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
