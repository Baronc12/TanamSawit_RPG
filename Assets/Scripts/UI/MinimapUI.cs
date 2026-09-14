using UnityEngine;
using UnityEngine.UI;
using TanamSawit.Environment;
using TanamSawit.Managers;

namespace TanamSawit.UI
{
    /// <summary>
    /// Minimap sederhana: kamera ortografik top-down merender dunia ke
    /// RenderTexture yang ditampilkan di RawImage pojok kanan-bawah UIRoot.
    /// Kamera mengikuti pemain dan re-center saat area berganti.
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        private Camera minimapCam;
        private RenderTexture renderTex;
        private RawImage minimapRaw;
        private Text areaLabel;

        private const int TexSize = 256;
        private const float DisplaySize = 200f;
        private const float CamOrthoSize = 28f;
        private const float CamZ = -12f;
        private const float Padding = 20f;

        private string currentAreaId = "kebun";

        private void Awake()
        {
            CreateMinimapCamera();
            CreateMinimapUI();
        }

        private void Start()
        {
            if (AreaTransitionManager.Instance != null)
            {
                AreaTransitionManager.Instance.OnAreaChanged += OnAreaChanged;
                currentAreaId = AreaTransitionManager.Instance.CurrentAreaId;
            }
            UpdateAreaLabel();
        }

        private void OnDestroy()
        {
            if (AreaTransitionManager.Instance != null)
                AreaTransitionManager.Instance.OnAreaChanged -= OnAreaChanged;

            if (renderTex != null)
            {
                renderTex.Release();
                renderTex = null;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Setup
        // ════════════════════════════════════════════════════════════════

        private void CreateMinimapCamera()
        {
            var go = new GameObject("[MINIMAP_CAM]");
            minimapCam = go.AddComponent<Camera>();
            minimapCam.orthographic = true;
            minimapCam.orthographicSize = CamOrthoSize;
            minimapCam.clearFlags = CameraClearFlags.SolidColor;
            minimapCam.backgroundColor = new Color(0.06f, 0.08f, 0.05f);
            minimapCam.depth = -10f; // render sebelum main camera

            // Exclude UI layer (5) dan DayNightCycle overlay
            minimapCam.cullingMask = ~(1 << 5); // ~UI

            // Render to texture
            renderTex = new RenderTexture(TexSize, TexSize, 16, RenderTextureFormat.ARGB32);
            renderTex.antiAliasing = 2;
            minimapCam.targetTexture = renderTex;
            minimapCam.aspect = 1f; // square

            DontDestroyOnLoad(go);
        }

        private void CreateMinimapUI()
        {
            // Parent panel (border + background)
            var panelRt = UIRoot.CreatePanel("[MINIMAP_PANEL]",
                UIRoot.PanelRoot, new Color(0.05f, 0.07f, 0.05f, 0.88f));
            panelRt.anchorMin = new Vector2(1f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(1f, 0f);
            panelRt.sizeDelta = new Vector2(DisplaySize + 8f, DisplaySize + 28f);
            panelRt.anchoredPosition = new Vector2(-Padding, Padding);

            // RawImage untuk menampilkan RenderTexture
            var rawGo = new GameObject("[MINIMAP_RAW]");
            rawGo.transform.SetParent(panelRt, false);
            var rawRt = (RectTransform)rawGo.transform;
            rawRt.anchorMin = new Vector2(0.5f, 1f);
            rawRt.anchorMax = new Vector2(0.5f, 1f);
            rawRt.pivot = new Vector2(0.5f, 1f);
            rawRt.sizeDelta = new Vector2(DisplaySize, DisplaySize);
            rawRt.anchoredPosition = new Vector2(0f, -4f);

            minimapRaw = rawGo.AddComponent<RawImage>();
            minimapRaw.texture = renderTex;
            minimapRaw.color = Color.white;

            // Label nama area
            areaLabel = UIRoot.CreateText("[MINIMAP_LABEL]", panelRt, "Area",
                fontSize: 12, color: UIRoot.TextGold, alignment: TextAnchor.MiddleCenter,
                style: FontStyle.Bold);
            var labelRt = (RectTransform)areaLabel.transform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.sizeDelta = new Vector2(0, 20f);
            labelRt.anchoredPosition = new Vector2(0f, 2f);
        }

        // ════════════════════════════════════════════════════════════════
        //  Update loop
        // ════════════════════════════════════════════════════════════════

        private void LateUpdate()
        {
            if (minimapCam == null) return;

            // Ikuti pemain
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Vector3 p = player.transform.position;
                minimapCam.transform.position = new Vector3(p.x, p.y, CamZ);
            }
            else
            {
                // Fallback: center on area
                var bounds = AreaBounds.Get(currentAreaId);
                minimapCam.transform.position = new Vector3(bounds.center.x, bounds.center.y, CamZ);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Area change
        // ════════════════════════════════════════════════════════════════

        private void OnAreaChanged(string areaName)
        {
            if (AreaTransitionManager.Instance != null)
                currentAreaId = AreaTransitionManager.Instance.CurrentAreaId;

            // Sesuaikan zoom berdasarkan ukuran area
            var bounds = AreaBounds.Get(currentAreaId);
            float fitSize = Mathf.Max(bounds.size.x, bounds.size.y) * 0.6f;
            if (minimapCam != null)
                minimapCam.orthographicSize = Mathf.Max(fitSize, 20f);

            UpdateAreaLabel();
        }

        private void UpdateAreaLabel()
        {
            if (areaLabel == null) return;

            string displayName = "Kebun Sawit";
            switch (currentAreaId)
            {
                case "kebun": displayName = "Kebun Sawit"; break;
                case "perumahan": displayName = "Perumahan"; break;
                case "kota": displayName = "Kota"; break;
                case "pabrik": displayName = "Pabrik CPO"; break;
            }
            areaLabel.text = displayName;
        }
    }
}
