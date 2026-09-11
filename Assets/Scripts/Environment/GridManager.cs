using System.Collections.Generic;
using UnityEngine;
using TanamSawit.Managers;

namespace TanamSawit.Environment
{
    /// <summary>
    /// GridManager mengatur visualisasi perkebunan sawit secara berjejer (Grid 2D).
    /// Setiap kali pemain membeli lahan (persentase lahan bertambah di EconomyManager),
    /// script ini secara otomatis memunculkan (Instantiate) Prefab TanahKosong dan PohonSawit!
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Prefab Visual (Sesuai Permintaan)")]
        [Tooltip("Prefab tanah kosong dasar lahan.")]
        [SerializeField] private GameObject tanahKosongPrefab;

        [Tooltip("Prefab pohon kelapa sawit yang ditanam di atas tanah.")]
        [SerializeField] private GameObject pohonSawitPrefab;

        [Header("Konfigurasi Grid 2D")]
        [Tooltip("Jumlah kolom per baris (misal 10 kolom x 10 baris = 100 plot untuk 100% lahan).")]
        [SerializeField] private int columns = 10;

        [Tooltip("Jarak antar plot (dalam unit Unity).")]
        [SerializeField] private float cellSpacing = 1.2f;

        [Tooltip("Titik awal koordinat (Kiri Atas) grid.")]
        [SerializeField] private Vector2 gridOrigin = new Vector2(-5.5f, 4.0f);

        [Tooltip("Berapa persen lahan yang diwakili oleh 1 plot di grid (Default 1% = 1 Plot).")]
        [SerializeField] private float landPercentagePerPlot = 1.0f;

        [Header("Parent Container")]
        [Tooltip("Transform induk penampung objek-objek plot agar Hierarchy tetap rapi.")]
        [SerializeField] private Transform plotsContainer;

        // Daftar plot yang sedang aktif di scene
        private readonly List<GameObject> spawnedPlots = new List<GameObject>();
        public int TotalSpawnedPlots => spawnedPlots.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (plotsContainer == null)
            {
                GameObject container = new GameObject("--- [PLOTS_CONTAINER] ---");
                container.transform.SetParent(transform);
                plotsContainer = container.transform;
            }
        }

        private void Start()
        {
            // Subscribe ke event perubahan persentase lahan dari EconomyManager
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLandPercentageChanged += HandleLandPercentageChanged;
                // Sinkronisasi awal sesuai persentase lahan saat ini
                SyncGridWithLandPercentage(EconomyManager.Instance.CurrentLandPercentage);
            }
            else
            {
                // Fallback default jika diuji tanpa EconomyManager
                SyncGridWithLandPercentage(5f);
            }
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLandPercentageChanged -= HandleLandPercentageChanged;
            }
        }

        /// <summary>
        /// Dipanggil setiap kali lahan dibeli atau berubah persentasenya.
        /// </summary>
        private void HandleLandPercentageChanged(float currentPercentage, float delta)
        {
            SyncGridWithLandPercentage(currentPercentage);
        }

        /// <summary>
        /// Menyesuaikan jumlah plot yang di-instantiate agar sesuai persentase lahan saat ini.
        /// </summary>
        public void SyncGridWithLandPercentage(float landPercentage)
        {
            int requiredPlots = Mathf.Clamp(Mathf.RoundToInt(landPercentage / landPercentagePerPlot), 0, 100);

            // Tambahkan plot baru jika persentase bertambah
            while (spawnedPlots.Count < requiredPlots)
            {
                SpawnNextPlot(spawnedPlots.Count);
            }

            // Kurangi plot jika persentase berkurang (misal saat load savegame lebih awal)
            while (spawnedPlots.Count > requiredPlots)
            {
                int lastIdx = spawnedPlots.Count - 1;
                if (spawnedPlots[lastIdx] != null)
                {
                    Destroy(spawnedPlots[lastIdx]);
                }
                spawnedPlots.RemoveAt(lastIdx);
            }
        }

        /// <summary>
        /// Memunculkan 1 plot baru pada indeks grid berikutnya.
        /// </summary>
        private void SpawnNextPlot(int plotIndex)
        {
            // Hitung baris dan kolom
            int col = plotIndex % columns;
            int row = plotIndex / columns;

            // Hitung posisi koordinat dunia (Kiri ke Kanan, Atas ke Bawah)
            Vector3 spawnPosition = new Vector3(
                gridOrigin.x + (col * cellSpacing),
                gridOrigin.y - (row * cellSpacing),
                0f
            );

            // Buat root GameObject untuk plot ini
            GameObject plotRoot = new GameObject($"Plot_Lahan_{plotIndex + 1}_(Col{col}_Row{row})");
            plotRoot.transform.position = spawnPosition;
            plotRoot.transform.SetParent(plotsContainer);

            // 1. INSTANTIATE TANAH KOSONG
            GameObject tanahObj;
            if (tanahKosongPrefab != null)
            {
                tanahObj = Instantiate(tanahKosongPrefab, spawnPosition, Quaternion.identity, plotRoot.transform);
            }
            else
            {
                // Fallback procedural jika user belum assign prefab tanah
                tanahObj = CreateFallbackSprite("Tanah_Visual", spawnPosition, new Color(0.45f, 0.28f, 0.12f), new Vector2(1f, 1f), 0);
                tanahObj.transform.SetParent(plotRoot.transform);
            }

            // 2. INSTANTIATE POHON SAWIT DI ATAS TANAH
            GameObject pohonObj;
            Vector3 treePosition = spawnPosition + new Vector3(0f, 0.15f, -0.1f); // Sedikit offset ke atas untuk efek isometrik 2D

            if (pohonSawitPrefab != null)
            {
                pohonObj = Instantiate(pohonSawitPrefab, treePosition, Quaternion.identity, plotRoot.transform);
            }
            else
            {
                // Fallback procedural jika user belum assign prefab pohon sawit
                pohonObj = CreateFallbackTreeSprite("PohonSawit_Visual", treePosition);
                pohonObj.transform.SetParent(plotRoot.transform);
            }

            spawnedPlots.Add(plotRoot);
            Debug.Log($"[GridManager] Berhasil memunculkan Plot ke-{plotIndex + 1} di kordinat {spawnPosition}!");
        }

        #region Procedural Fallback Visuals (Langsung Tampil Tanpa Error)
        private GameObject CreateFallbackSprite(string name, Vector3 pos, Color color, Vector2 size, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

            Texture2D tex = new Texture2D(32, 32);
            Color[] cols = new Color[32 * 32];
            for (int i = 0; i < cols.Length; i++) cols[i] = color;
            tex.SetPixels(cols);
            tex.Apply();

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            sr.sortingOrder = sortingOrder;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            return go;
        }

        private GameObject CreateFallbackTreeSprite(string name, Vector3 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

            // Buat sprite pohon sawit sederhana (hijau gelap bertekstur)
            Texture2D tex = new Texture2D(32, 40);
            Color[] cols = new Color[32 * 40];
            Color green = new Color(0.12f, 0.55f, 0.18f);
            Color trunk = new Color(0.35f, 0.20f, 0.08f);

            for (int y = 0; y < 40; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (y < 12 && x >= 13 && x <= 18)
                        cols[y * 32 + x] = trunk; // Batang
                    else if (y >= 10 && Vector2.Distance(new Vector2(x, y), new Vector2(16, 26)) < 13)
                        cols[y * 32 + x] = green; // Daun pelepah
                    else
                        cols[y * 32 + x] = Color.clear;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 40), new Vector2(0.5f, 0.2f), 32);
            sr.sortingOrder = 5;
            go.transform.localScale = Vector3.one * 1.1f;
            return go;
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < 20; i++)
            {
                int col = i % columns;
                int row = i / columns;
                Vector3 p = new Vector3(gridOrigin.x + (col * cellSpacing), gridOrigin.y - (row * cellSpacing), 0f);
                Gizmos.DrawWireCube(p, Vector3.one * cellSpacing * 0.9f);
            }
        }
    }
}
