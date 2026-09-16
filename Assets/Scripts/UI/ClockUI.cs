using UnityEngine;
using UnityEngine.UI;
using TanamSawit.Managers;

namespace TanamSawit.UI
{
    /// <summary>
    /// Clock UI widget positioned in the top right corner of the screen.
    /// Displays game time in HH:MM format with sun/moon icon.
    /// Updates every frame to show smooth time progression.
    /// </summary>
    [DefaultExecutionOrder(-21)]
    public class ClockUI : MonoBehaviour
    {
        public static ClockUI Instance { get; private set; }

        private Text _timeText;
        private Text _dateText;
        private Image _iconImage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            // Root panel di kanan atas
            var panel = UIRoot.CreatePanel("ClockUI", UIRoot.PanelRoot, new Color(0f, 0f, 0f, 0.7f));
            panel.anchorMin = new Vector2(1, 1);
            panel.anchorMax = new Vector2(1, 1);
            panel.pivot = new Vector2(1, 1);
            panel.sizeDelta = new Vector2(180, 60);
            panel.anchoredPosition = new Vector2(-12, -52); // Offset dari kanan atas (di bawah top bar)

            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 6, 6);
            vlg.spacing = 2;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Ikon siang/malam
            var iconRt = UIRoot.CreateRect("ClockIcon", panel);
            iconRt.sizeDelta = new Vector2(20, 20);
            _iconImage = iconRt.gameObject.AddComponent<Image>();
            _iconImage.color = Color.white;
            _iconImage.raycastTarget = false;

            // Teks jam
            var timeRt = UIRoot.CreateRect("ClockTime", panel);
            timeRt.sizeDelta = new Vector2(140, 22);
            _timeText = timeRt.gameObject.AddComponent<Text>();
            _timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _timeText.fontSize = 16;
            _timeText.color = UIRoot.TextGold;
            _timeText.alignment = TextAnchor.MiddleCenter;
            _timeText.fontStyle = FontStyle.Bold;
            _timeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _timeText.raycastTarget = false;

            // Teks tanggal
            var dateRt = UIRoot.CreateRect("ClockDate", panel);
            dateRt.sizeDelta = new Vector2(140, 16);
            _dateText = dateRt.gameObject.AddComponent<Text>();
            _dateText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _dateText.fontSize = 11;
            _dateText.color = new Color(0.8f, 0.8f, 0.8f);
            _dateText.alignment = TextAnchor.MiddleCenter;
            _dateText.fontStyle = FontStyle.Normal;
            _dateText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _dateText.raycastTarget = false;
        }

        private void Update()
        {
            if (TimeManager.Instance == null) return;

            float hour = TimeManager.Instance.HourOfDay;
            int hourInt = Mathf.FloorToInt(hour);
            int minuteInt = Mathf.FloorToInt((hour - hourInt) * 60f);

            // Ikon siang/malam
            bool isDay = hour >= 6f && hour < 18f;
            string icon = isDay ? "☀" : "☽";

            // Format waktu HH:MM
            string timeStr = $"{hourInt:D2}:{minuteInt:D2}";

            // Update teks
            _timeText.text = $"{icon} {timeStr}";

            // Update tanggal
            _dateText.text = TimeManager.Instance.GetFormattedDate();
        }
    }
}
