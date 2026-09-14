using System;
using UnityEngine;
using UnityEngine.UI;
using TanamSawit.Managers;

namespace TanamSawit.Environment
{
    /// <summary>
    /// Menggerakkan overlay pencahayaan layar penuh berdasarkan jam dalam
    /// sehari (TimeManager.HourOfDay). Memberi efek siang-malam: hangat saat
    /// tengah hari, biru gelap saat malam, jingga saat fajar/senja.
    ///
    /// Implementasi: Image overlay pada Canvas ScreenSpace-Overlay terpisah
    /// (sortingOrder 200, di atas UIRoot 100). raycastTarget=false agar tidak
    /// memblokir input. Juga mengatur background color Camera.main.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class DayNightCycle : MonoBehaviour
    {
        // ── Keyframe kurva warna (hour, tint, alpha) ──
        private struct LightKey
        {
            public float hour;
            public Color tint;
            public float alpha;
            public LightKey(float h, float r, float g, float b, float a)
            {
                hour = h; tint = new Color(r, g, b); alpha = a;
            }
        }

        private static readonly LightKey[] Curve =
        {
            new LightKey(0f,  0.05f, 0.08f, 0.20f, 0.55f), // tengah malam
            new LightKey(5f,  0.08f, 0.10f, 0.22f, 0.50f), // dini hari
            new LightKey(6f,  0.95f, 0.55f, 0.20f, 0.25f), // fajar (jingga)
            new LightKey(8f,  1.00f, 0.92f, 0.70f, 0.06f), // pagi
            new LightKey(12f, 1.00f, 0.98f, 0.85f, 0.00f), // tengah hari
            new LightKey(16f, 1.00f, 0.92f, 0.70f, 0.04f), // sore
            new LightKey(18f, 0.95f, 0.50f, 0.15f, 0.20f), // senja
            new LightKey(20f, 0.15f, 0.10f, 0.25f, 0.40f), // malam awal
            new LightKey(22f, 0.05f, 0.08f, 0.20f, 0.55f), // malam
            new LightKey(24f, 0.05f, 0.08f, 0.20f, 0.55f), // akhir hari (wrap)
        };

        private static readonly Color NightBg = new Color(0.03f, 0.04f, 0.08f);
        private static readonly Color DayBg = new Color(0.18f, 0.22f, 0.16f);
        private static readonly Color DuskBg = new Color(0.12f, 0.08f, 0.10f);

        private Image overlayImage;

        private void Awake()
        {
            CreateOverlay();
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnHourChanged += OnHourChanged;
                UpdateOverlay(TimeManager.Instance.HourOfDay);
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnHourChanged -= OnHourChanged;
        }

        private void LateUpdate()
        {
            // Update tiap frame untuk transisi mulus antar keyframe
            if (TimeManager.Instance != null)
                UpdateOverlay(TimeManager.Instance.HourOfDay);
        }

        private void CreateOverlay()
        {
            var go = new GameObject("[DAY_NIGHT_OVERLAY]");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            go.AddComponent<GraphicRaycaster>();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            overlayImage = go.AddComponent<Image>();
            overlayImage.sprite = CreateWhiteSprite();
            overlayImage.color = Color.clear;
            overlayImage.raycastTarget = false;

            DontDestroyOnLoad(go);
        }

        private static Sprite CreateWhiteSprite()
        {
            try { return TanamSawit.UI.UIRoot.WhiteSprite; }
            catch { /* UIRoot belum ada — buat sendiri */ }

            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        }

        private void OnHourChanged(float hour)
        {
            UpdateOverlay(hour);
        }

        private void UpdateOverlay(float hour)
        {
            if (overlayImage == null) return;

            EvaluateCurve(hour, out Color tint, out float alpha, out Color bgColor);
            overlayImage.color = new Color(tint.r, tint.g, tint.b, alpha);

            // Update background kamera
            Camera cam = Camera.main;
            if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor)
                cam.backgroundColor = bgColor;
        }

        private void EvaluateCurve(float hour, out Color tint, out float alpha, out Color bgColor)
        {
            // Default ke keyframe pertama
            tint = Curve[0].tint;
            alpha = Curve[0].alpha;
            bgColor = NightBg;

            for (int i = 0; i < Curve.Length - 1; i++)
            {
                if (hour >= Curve[i].hour && hour <= Curve[i + 1].hour)
                {
                    float span = Curve[i + 1].hour - Curve[i].hour;
                    float t = span > 0.001f ? (hour - Curve[i].hour) / span : 0f;
                    tint = Color.Lerp(Curve[i].tint, Curve[i + 1].tint, t);
                    alpha = Mathf.Lerp(Curve[i].alpha, Curve[i + 1].alpha, t);

                    // Background color: night (0-6, 20-24), dusk (6-8, 18-20), day (8-18)
                    if (hour >= 8f && hour < 18f)
                        bgColor = Color.Lerp(DuskBg, DayBg, Mathf.Clamp01((hour - 8f) / 2f));
                    else if (hour >= 6f && hour < 8f)
                        bgColor = Color.Lerp(NightBg, DuskBg, (hour - 6f) / 2f);
                    else if (hour >= 18f && hour < 20f)
                        bgColor = Color.Lerp(DayBg, DuskBg, (hour - 18f) / 2f);
                    else if (hour >= 20f || hour < 6f)
                        bgColor = NightBg;
                    else
                        bgColor = DayBg;

                    return;
                }
            }
        }
    }
}
