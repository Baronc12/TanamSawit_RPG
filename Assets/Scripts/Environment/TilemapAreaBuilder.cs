using UnityEngine;
using UnityEngine.Tilemaps;
using TanamSawit.Managers;

namespace TanamSawit.Environment
{
    /// <summary>
    /// Membangun ground ber-tiling per-area saat runtime menggunakan tile
    /// yang di-generate secara prosedural (3 nuansa warna per area).
    /// Menggantikan flat colored ground sprite dari WorldBuildingBuilder.
    ///
    /// Wall colliders dari WorldBuildingBuilder tetap utuh — TilemapAreaBuilder
    /// hanya mengganti visual ground.
    /// </summary>
    public class TilemapAreaBuilder : MonoBehaviour
    {
        // ── Palet warna per-area (3 nuansa tiap area) ──
        private static readonly Color[][] AreaPalettes = new Color[][]
        {
            // Kebun — hijau segar
            new Color[]
            {
                new Color(0.24f, 0.58f, 0.18f),
                new Color(0.28f, 0.64f, 0.22f),
                new Color(0.20f, 0.52f, 0.14f),
            },
            // Perumahan — pasir/hangat
            new Color[]
            {
                new Color(0.82f, 0.73f, 0.52f),
                new Color(0.88f, 0.79f, 0.58f),
                new Color(0.76f, 0.67f, 0.46f),
            },
            // Kota — abu-abu beton
            new Color[]
            {
                new Color(0.55f, 0.55f, 0.59f),
                new Color(0.61f, 0.61f, 0.65f),
                new Color(0.49f, 0.49f, 0.53f),
            },
            // Pabrik — coklat tanah
            new Color[]
            {
                new Color(0.42f, 0.32f, 0.22f),
                new Color(0.48f, 0.38f, 0.28f),
                new Color(0.36f, 0.26f, 0.16f),
            },
        };

        private static readonly Color RoadColor = new Color(0.38f, 0.36f, 0.34f);
        private static readonly Color BorderColor = new Color(0.15f, 0.12f, 0.08f);

        private Grid grid;
        private Tilemap groundTilemap;
        private Tilemap roadTilemap;
        private Tile[] groundTiles;
        private Tile roadTile;
        private Tile borderTile;

        /// <summary>
        /// Inisialisasi grid dan tile. Dipanggil sekali oleh WorldBuildingBuilder
        /// sebelum BuildAreaTiles.
        /// </summary>
        public void InitGrid(Transform parent)
        {
            var gridGo = new GameObject("=== [TILEMAP_GRID] ===");
            gridGo.transform.SetParent(parent);
            grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 1f);

            // Tilemap ground (sortingOrder 0 — di bawah segalanya)
            groundTilemap = CreateTilemap(gridGo.transform, "Ground_Tilemap", 0);
            // Tilemap jalan (sortingOrder 1)
            roadTilemap = CreateTilemap(gridGo.transform, "Road_Tilemap", 1);

            // Generate tiles prosedural
            GenerateTiles();
        }

        private Tilemap CreateTilemap(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sortingOrder;
            tr.mode = TilemapRenderer.Mode.Chunk;
            return tm;
        }

        private void GenerateTiles()
        {
            int paletteCount = AreaPalettes.Length;
            groundTiles = new Tile[paletteCount * 3];

            for (int p = 0; p < paletteCount; p++)
            {
                for (int s = 0; s < 3; s++)
                {
                    groundTiles[p * 3 + s] = CreateTile(AreaPalettes[p][s]);
                }
            }

            roadTile = CreateTile(RoadColor);
            borderTile = CreateTile(BorderColor);
        }

        private Tile CreateTile(Color color)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = CreateSolidSprite(color);
            tile.color = Color.white;
            return tile;
        }

        private Sprite CreateSolidSprite(Color color)
        {
            // 4x4 texture dengan sedikit noise agar tidak terlihat flat
            int size = 4;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // Tambah noise halus ±5%
                    float n = (Random.value - 0.5f) * 0.05f;
                    tex.SetPixel(x, y, new Color(
                        Mathf.Clamp01(color.r + n),
                        Mathf.Clamp01(color.g + n),
                        Mathf.Clamp01(color.b + n),
                        1f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// Membangun ground ber-tiling untuk satu area.
        /// areaIndex: 0=Kebun, 1=Perumahan, 2=Kota, 3=Pabrik
        /// center: pusat area di koordinat dunia
        /// width/height: ukuran area (harus kelipatan integer)
        /// </summary>
        public void BuildAreaTiles(int areaIndex, Vector2 center, float width, float height)
        {
            if (groundTilemap == null) return;

            int halfW = Mathf.RoundToInt(width * 0.5f);
            int halfH = Mathf.RoundToInt(height * 0.5f);
            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);

            // Offset tilemap ke pusat area
            groundTilemap.transform.position = Vector3.zero; // grid di origin, tile di world coords

            int baseTileIndex = areaIndex * 3;

            for (int x = cx - halfW; x < cx + halfW; x++)
            {
                for (int y = cy - halfH; y < cy + halfH; y++)
                {
                    // Pola pseudo-noise: gunakan hash sederhana untuk variasi
                    int hash = (x * 73856093) ^ (y * 19349663);
                    int shade = ((hash >> 3) & 0xFF) % 3;
                    Tile tile = groundTiles[baseTileIndex + shade];

                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            // Border decorative tiles (garis tepi lebih gelap)
            for (int x = cx - halfW; x < cx + halfW; x++)
            {
                groundTilemap.SetTile(new Vector3Int(x, cy - halfH, 0), borderTile);
                groundTilemap.SetTile(new Vector3Int(x, cy + halfH - 1, 0), borderTile);
            }
            for (int y = cy - halfH; y < cy + halfH; y++)
            {
                groundTilemap.SetTile(new Vector3Int(cx - halfW, y, 0), borderTile);
                groundTilemap.SetTile(new Vector3Int(cx + halfW - 1, y, 0), borderTile);
            }
        }

        /// <summary>
        /// Membangun jalan penghubung antar area sebagai tile.
        /// </summary>
        public void BuildRoadTiles(Vector2 center, Vector2 size)
        {
            if (roadTilemap == null) return;

            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);
            int halfW = Mathf.Max(1, Mathf.RoundToInt(size.x * 0.5f));
            int halfH = Mathf.Max(1, Mathf.RoundToInt(size.y * 0.5f));

            for (int x = cx - halfW; x < cx + halfW; x++)
            {
                for (int y = cy - halfH; y < cy + halfH; y++)
                {
                    roadTilemap.SetTile(new Vector3Int(x, y, 0), roadTile);
                }
            }
        }

        /// <summary>
        /// Membersihkan semua tile (dipanggil saat world di-rebuild).
        /// </summary>
        public void ClearTiles()
        {
            if (groundTilemap != null) groundTilemap.ClearAllTiles();
            if (roadTilemap != null) roadTilemap.ClearAllTiles();
        }
    }
}
