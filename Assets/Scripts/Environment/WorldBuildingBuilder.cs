using System.Collections.Generic;
using UnityEngine;
using TanamSawit.Managers;
using TanamSawit.NPC;

namespace TanamSawit.Environment
{
    /// <summary>
    /// Builder procedural yang membangun keempat area dunia game Tanam Sawit
    /// (Kebun Sawit, Perumahan, Kota, Pabrik CPO) beserta jalan penghubung, fasilitas,
    /// dan portal transisi antar area — langsung dalam satu Unity scene, tanpa prefab wajib.
    ///
    /// Cara pakai:
    ///   1. Pasang script ini ke sebuah GameObject di scene gameplay.
    ///   2. Klik kanan di Inspector > "Build 4 Area Dunia Sekarang", ATAU
    ///   3. Gunakan menu Editor: Tanam Sawit > 🌴 1-Klik: Bangun 4 Area Dunia.
    /// </summary>
    [DefaultExecutionOrder(-35)]
    public class WorldBuildingBuilder : MonoBehaviour
    {
        public static WorldBuildingBuilder Instance { get; private set; }

        [Header("Pengaturan Jarak Antar Area")]
        [Tooltip("Jarak horizontal antar pusat masing-masing area (dalam unit Unity).")]
#pragma warning disable CS0414
        [SerializeField] private float areaSpacing = 60f;
#pragma warning restore CS0414

        [Header("Ukuran Tanah Area")]
        [SerializeField] private float areaWidth = 50f;
        [SerializeField] private float areaHeight = 40f;

        [Header("Warna Area")]
        [SerializeField] private Color colorKebun = new Color(0.26f, 0.62f, 0.20f);
        [SerializeField] private Color colorPerumahan = new Color(0.85f, 0.76f, 0.55f);
        [SerializeField] private Color colorKota = new Color(0.58f, 0.58f, 0.62f);
        [SerializeField] private Color colorPabrik = new Color(0.45f, 0.35f, 0.25f);

        [Header("Spawn Points (titik masuk tiap area)")]
        public Transform kebunSpawnPoint;
        public Transform perumahanSpawnPoint;
        public Transform kotaSpawnPoint;
        public Transform pabrikSpawnPoint;

        // Pusat tiap area (X offset, Y = 0)
        private static readonly Vector2[] AreaCenters = new Vector2[]
        {
            new Vector2(0f,   0f),   // Kebun
            new Vector2(60f,  0f),   // Perumahan
            new Vector2(120f, 0f),   // Kota
            new Vector2(0f,  -60f),  // Pabrik (di bawah kebun)
        };

        private static readonly string[] AreaNames = { "Area Kebun Sawit", "Area Perumahan", "Area Kota", "Area Pabrik CPO" };

        private GameObject worldRoot;
        private readonly List<GameObject> builtObjects = new List<GameObject>();
        private int worldLayer = -1;
        private TilemapAreaBuilder tilemapBuilder;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            worldLayer = LayerMask.NameToLayer("World");
        }

        private void Start()
        {
            if (worldRoot == null)
            {
                BuildWorld();
            }
        }

        [ContextMenu("Build 4 Area Dunia Sekarang")]
        public void BuildWorld()
        {
            ClearWorld();

            worldRoot = new GameObject("=== [WORLD_4_AREA] ===");

            // Tilemap builder untuk ground ber-tiling
            tilemapBuilder = worldRoot.AddComponent<TilemapAreaBuilder>();
            tilemapBuilder.InitGrid(worldRoot.transform);

            Color[] colors = { colorKebun, colorPerumahan, colorKota, colorPabrik };

            for (int i = 0; i < 4; i++)
            {
                BuildArea(i, AreaNames[i], AreaCenters[i], colors[i], worldRoot.transform);
            }

            // Jalan penghubung
            BuildRoad("Jalan_Kebun_Perumahan", new Vector2(30f, 0f), new Vector2(58f, 3f), worldRoot.transform);
            BuildRoad("Jalan_Perumahan_Kota", new Vector2(90f, 0f), new Vector2(58f, 3f), worldRoot.transform);
            BuildRoad("Jalan_Kebun_Pabrik", new Vector2(0f, -30f), new Vector2(3f, 58f), worldRoot.transform);

            // Portal transisi antar area
            BuildPortals();

            // Fasilitas di tiap area
            BuildKebunFacilities();
            BuildPerumahanFacilities();
            BuildKotaFacilities();
            BuildPabrikFacilities();

            // NPC dengan dialog
            BuildNPCs();

            // Simpan spawn points
            RegisterSpawnPoints();

            Debug.Log("<color=#00FF88><b>[WorldBuildingBuilder]</b></color> 4 Area berhasil dibangun!");
        }

        private void ClearWorld()
        {
            if (tilemapBuilder != null)
                tilemapBuilder.ClearTiles();

            foreach (var obj in builtObjects)
            {
                if (obj != null)
                    DestroyImmediate(obj);
            }
            builtObjects.Clear();

            var existing = GameObject.Find("=== [WORLD_4_AREA] ===");
            if (existing != null) DestroyImmediate(existing);
        }

        // ─── Area Ground ───────────────────────────────────────────────────

        private void BuildArea(int areaIndex, string areaName, Vector2 center, Color groundColor, Transform parent)
        {
            GameObject areaRoot = new GameObject(areaName);
            areaRoot.transform.SetParent(parent);
            areaRoot.transform.position = new Vector3(center.x, center.y, 0f);
            builtObjects.Add(areaRoot);

            // Ground ber-tiling (gantikan flat colored sprite)
            if (tilemapBuilder != null)
                tilemapBuilder.BuildAreaTiles(areaIndex, center, areaWidth, areaHeight);
            else
                CreateColoredSprite(areaRoot.transform, $"Ground_{areaName}", Vector3.zero,
                    new Vector2(areaWidth, areaHeight), groundColor, 0);

            // Label nama area
            CreateTextLabel(areaRoot.transform, areaName, new Vector3(0f, areaHeight * 0.5f - 1.5f, -1f));

            // Border dinding area (top, bottom, left, right)
            Color wallColor = Color.Lerp(groundColor, Color.black, 0.4f);
            CreateColoredSprite(areaRoot.transform, "Wall_Top",    new Vector3(0f,  areaHeight * 0.5f + 0.25f, 0f), new Vector2(areaWidth, 0.5f), wallColor, 2);
            CreateColoredSprite(areaRoot.transform, "Wall_Bot",    new Vector3(0f, -areaHeight * 0.5f - 0.25f, 0f), new Vector2(areaWidth, 0.5f), wallColor, 2);
            CreateColoredSprite(areaRoot.transform, "Wall_Left",   new Vector3(-areaWidth * 0.5f - 0.25f, 0f, 0f), new Vector2(0.5f, areaHeight), wallColor, 2);
            CreateColoredSprite(areaRoot.transform, "Wall_Right",  new Vector3( areaWidth * 0.5f + 0.25f, 0f, 0f), new Vector2(0.5f, areaHeight), wallColor, 2);

            // Tambah solid collider 2D untuk dinding
            AddBoundaryColliders(areaRoot.transform);
        }

        private void AddBoundaryColliders(Transform areaRoot)
        {
            float hw = areaWidth * 0.5f;
            float hh = areaHeight * 0.5f;

            // Top & Bottom
            CreateWallCollider(areaRoot, "Coll_Top",  new Vector2(0f,  hh + 0.25f), new Vector2(areaWidth + 0.5f, 0.5f));
            CreateWallCollider(areaRoot, "Coll_Bot",  new Vector2(0f, -hh - 0.25f), new Vector2(areaWidth + 0.5f, 0.5f));
            // Left & Right (leave gaps in center for roads)
            CreateWallCollider(areaRoot, "Coll_Left", new Vector2(-hw - 0.25f, 2f),  new Vector2(0.5f, hh * 2f - 4f));
            CreateWallCollider(areaRoot, "Coll_LeftBot", new Vector2(-hw - 0.25f, -hh + 1f), new Vector2(0.5f, hh - 1f));
            CreateWallCollider(areaRoot, "Coll_Right", new Vector2( hw + 0.25f, 2f), new Vector2(0.5f, hh * 2f - 4f));
            CreateWallCollider(areaRoot, "Coll_RightBot", new Vector2( hw + 0.25f, -hh + 1f), new Vector2(0.5f, hh - 1f));
        }

        private void CreateWallCollider(Transform parent, string name, Vector2 localPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            if (worldLayer >= 0)
                go.layer = worldLayer;
        }

        // ─── Jalan Penghubung ─────────────────────────────────────────────

        private void BuildRoad(string name, Vector2 center, Vector2 size, Transform parent)
        {
            Color roadColor = new Color(0.42f, 0.42f, 0.42f);
            CreateColoredSprite(parent, name, new Vector3(center.x, center.y, 0.1f), size, roadColor, 1);
        }

        // ─── Portals Transisi ─────────────────────────────────────────────

        private void BuildPortals()
        {
            // Kebun → Perumahan
            BuildPortal("Portal_Kebun_ke_Perumahan",
                new Vector3(AreaCenters[0].x + areaWidth * 0.5f, AreaCenters[0].y, 0f),
                "Area Perumahan",
                new Vector3(AreaCenters[1].x - areaWidth * 0.5f + 3f, AreaCenters[1].y, 0f));

            // Perumahan → Kebun
            BuildPortal("Portal_Perumahan_ke_Kebun",
                new Vector3(AreaCenters[1].x - areaWidth * 0.5f, AreaCenters[1].y, 0f),
                "Area Kebun Sawit",
                new Vector3(AreaCenters[0].x + areaWidth * 0.5f - 3f, AreaCenters[0].y, 0f));

            // Perumahan → Kota
            BuildPortal("Portal_Perumahan_ke_Kota",
                new Vector3(AreaCenters[1].x + areaWidth * 0.5f, AreaCenters[1].y, 0f),
                "Area Kota",
                new Vector3(AreaCenters[2].x - areaWidth * 0.5f + 3f, AreaCenters[2].y, 0f));

            // Kota → Perumahan
            BuildPortal("Portal_Kota_ke_Perumahan",
                new Vector3(AreaCenters[2].x - areaWidth * 0.5f, AreaCenters[2].y, 0f),
                "Area Perumahan",
                new Vector3(AreaCenters[1].x + areaWidth * 0.5f - 3f, AreaCenters[1].y, 0f));

            // Kebun → Pabrik
            BuildPortal("Portal_Kebun_ke_Pabrik",
                new Vector3(AreaCenters[0].x, AreaCenters[0].y - areaHeight * 0.5f, 0f),
                "Area Pabrik CPO",
                new Vector3(AreaCenters[3].x, AreaCenters[3].y + areaHeight * 0.5f - 3f, 0f));

            // Pabrik → Kebun
            BuildPortal("Portal_Pabrik_ke_Kebun",
                new Vector3(AreaCenters[3].x, AreaCenters[3].y + areaHeight * 0.5f, 0f),
                "Area Kebun Sawit",
                new Vector3(AreaCenters[0].x, AreaCenters[0].y - areaHeight * 0.5f + 3f, 0f));
        }

        private void BuildPortal(string portalName, Vector3 position, string targetArea, Vector3 targetPos)
        {
            var go = new GameObject(portalName);
            go.transform.SetParent(worldRoot.transform);
            go.transform.position = position;
            builtObjects.Add(go);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(4f, 2f);

            // Set layer Portal jika tersedia
            int portalLayer = LayerMask.NameToLayer("Portal");
            if (portalLayer >= 0)
                go.layer = portalLayer;

            var portal = go.AddComponent<AreaPortalTrigger>();
            portal.Configure(targetArea, targetPos, requireKey: false);

            // Visual tanda panah portal
            CreateColoredSprite(go.transform, "PortalVisual", Vector3.zero,
                new Vector2(4f, 0.3f), new Color(1f, 0.9f, 0.2f, 0.6f), 3);
        }

        // ─── Fasilitas Kebun ──────────────────────────────────────────────

        private void BuildKebunFacilities()
        {
            var center = AreaCenters[0];
            var parent = worldRoot.transform.Find(AreaNames[0]);
            if (parent == null) return;

            // Papan Lahan
            BuildFacilityBuilding(parent, "🌿 Papan Lahan & Plot Grid", new Vector3(-15f, 12f, 0f),
                FacilityType.PapanLahan, "Kelola Lahan Sawit", new Color(0.18f, 0.5f, 0.18f), new Vector2(7f, 4f));

            // Mandor kebun
            BuildFacilityBuilding(parent, "👷 Pos Mandor Kebun", new Vector3(5f, 8f, 0f),
                FacilityType.MandorKebun, "Bicara dengan Mandor", new Color(0.7f, 0.55f, 0.2f), new Vector2(5f, 4f));

            // Gudang TBS
            BuildFacilityBuilding(parent, "📦 Gudang TBS", new Vector3(15f, -5f, 0f),
                FacilityType.GudangTBS, "Kelola Stok TBS & Jual", new Color(0.5f, 0.35f, 0.15f), new Vector2(8f, 5f));

            // Rumah kayu dari asset gambar agar terlihat langsung di peta Kebun.
            CreateBuildingAsset(parent, "Rumah_Kebun_Sawit",
                new Vector3(-10f, -8f, -0.2f),
                "building/0421c6e8-130b-4892-9758-0e56c07307e5",
                new Vector2(8f, 7f), 2);
        }

        // ─── Fasilitas Perumahan ──────────────────────────────────────────

        private void BuildPerumahanFacilities()
        {
            var center = AreaCenters[1];
            var parent = worldRoot.transform.Find(AreaNames[1]);
            if (parent == null) return;

            // Mess Pekerja
            BuildFacilityBuilding(parent, "🏠 Mess Pekerja", new Vector3(-10f, 8f, 0f),
                FacilityType.MessPekerja, "Kelola & Rekrut Pekerja", new Color(0.75f, 0.6f, 0.35f), new Vector2(8f, 6f));

            // Kos-kosan
            BuildFacilityBuilding(parent, "🏘️ Kos-kosan", new Vector3(10f, 5f, 0f),
                FacilityType.KosKosan, "Bangun / Kelola Kos-kosan", new Color(0.9f, 0.7f, 0.4f), new Vector2(7f, 5f));
        }

        // ─── Fasilitas Kota ───────────────────────────────────────────────

        private void BuildKotaFacilities()
        {
            var center = AreaCenters[2];
            var parent = worldRoot.transform.Find(AreaNames[2]);
            if (parent == null) return;

            // Bank
            BuildFacilityBuilding(parent, "🏦 Bank Konvensional", new Vector3(-14f, 12f, 0f),
                FacilityType.Bank, "Pinjaman Bank / Cicilan", new Color(0.2f, 0.35f, 0.7f), new Vector2(9f, 6f));

            // Pinjol
            BuildFacilityBuilding(parent, "📱 Kantor Pinjol", new Vector3(0f, 10f, 0f),
                FacilityType.Pinjol, "Cairkan Pinjol Instan!", new Color(0.8f, 0.2f, 0.2f), new Vector2(7f, 5f));

            // Rentenir
            BuildFacilityBuilding(parent, "💰 Warung Rentenir Madura", new Vector3(14f, 8f, 0f),
                FacilityType.Rentenir, "Pinjaman Tunai Madura", new Color(0.6f, 0.4f, 0.1f), new Vector2(8f, 5f));

            // Yayasan CSR
            BuildFacilityBuilding(parent, "🎓 Yayasan Pendidikan CSR", new Vector3(-8f, -8f, 0f),
                FacilityType.YayasanCSR, "Danai Yayasan Desa", new Color(0.3f, 0.6f, 0.8f), new Vector2(9f, 5f));

            // Billboard Sepupu
            BuildFacilityBuilding(parent, "📊 Billboard Saingan Sepupu", new Vector3(12f, -10f, 0f),
                FacilityType.BillboardSepupu, "Cek Status Persaingan", new Color(0.25f, 0.25f, 0.3f), new Vector2(6f, 4f));
        }

        // ─── Fasilitas Pabrik ─────────────────────────────────────────────

        private void BuildPabrikFacilities()
        {
            var center = AreaCenters[3];
            var parent = worldRoot.transform.Find(AreaNames[3]);
            if (parent == null) return;

            // Pabrik CPO
            BuildFacilityBuilding(parent, "🏭 Pabrik Pengolahan CPO", new Vector3(0f, 5f, 0f),
                FacilityType.PabrikCPO, "Bangun / Kelola Pabrik CPO", new Color(0.5f, 0.45f, 0.3f), new Vector2(14f, 9f));
        }


        // ─── NPC dengan Dialog ───────────────────────────────────────────

        private void BuildNPCs()
        {
            // Mandor — Kebun (pekerja/rekrut)
            BuildNPC(AreaNames[0], "Mandor Kebun", new Vector3(8f, -6f, 0f),
                NPCIdentity.CreateRuntime("mandor", "Pak Bambang", "Mandor Kebun", new Color(0.7f, 0.55f, 0.2f)),
                CreateMandorDialogue(), wander: true);

            // Pegawai Bank — Kota
            BuildNPC(AreaNames[2], "Pegawai Bank", new Vector3(-12f, 10f, 0f),
                NPCIdentity.CreateRuntime("pegawai_bank", "Ibu Siti", "Pegawai Bank Konvensional", new Color(0.2f, 0.35f, 0.7f)),
                CreateBankDialogue(), wander: false);

            // Debt Collector Pinjol — Kota
            BuildNPC(AreaNames[2], "Debt Collector", new Vector3(2f, 8f, 0f),
                NPCIdentity.CreateRuntime("debt_collector", "Budi Toll", "Debt Collector Pinjol", new Color(0.8f, 0.2f, 0.2f)),
                CreatePinjolDialogue(), wander: true);

            // Rentenir — Perumahan
            BuildNPC(AreaNames[1], "Rentenir Madura", new Vector3(-8f, 6f, 0f),
                NPCIdentity.CreateRuntime("rentenir", "Pak Slamet", "Rentenir Madura", new Color(0.6f, 0.4f, 0.1f)),
                CreateRentenirDialogue(), wander: false);

            // Sepupu (Rival) — Kota
            BuildNPC(AreaNames[2], "Sepupu Rival", new Vector3(10f, -8f, 0f),
                NPCIdentity.CreateRuntime("sepupu", "Eddy Sepupu", "Sepupu Saingan", new Color(0.25f, 0.25f, 0.3f)),
                CreateSepupuDialogue(), wander: false);

            // Kepala Desa — Perumahan
            BuildNPC(AreaNames[1], "Kepala Desa", new Vector3(8f, -4f, 0f),
                NPCIdentity.CreateRuntime("kepala_desa", "Pak Kades", "Kepala Desa", new Color(0.3f, 0.6f, 0.3f)),
                CreateKepalaDesaDialogue(), wander: false);
        }

        private void BuildNPC(string areaName, string label, Vector3 localPos,
            NPCIdentity identity, DialogueAsset dialogue, bool wander)
        {
            var parent = worldRoot.transform.Find(areaName);
            if (parent == null) return;

            var go = new GameObject($"NPC_{identity.displayName}");
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            builtObjects.Add(go);

            // Visual: colored sprite
            Color npcColor = identity.accentColor;
            CreateColoredSprite(go.transform, "NPC_Body", new Vector3(0f, 0.5f, 0f), new Vector2(1.2f, 1.6f), npcColor, 5);
            CreateColoredSprite(go.transform, "NPC_Head", new Vector3(0f, 1.5f, 0f), new Vector2(0.8f, 0.8f),
                Color.Lerp(npcColor, Color.white, 0.3f), 5);

            // Label
            CreateTextLabel(go.transform, label, new Vector3(0f, 2.2f, -0.2f));

            // NPC component
            var npc = go.AddComponent<NPCInteractable>();
            npc.Configure(identity, dialogue, 2.5f, wander);
        }

        // ─── Runtime Dialogue Assets ─────────────────────────────────────

        private static DialogueAsset CreateMandorDialogue()
        {
            var dlg = DialogueAsset.CreateRuntime(
                NPCIdentity.CreateRuntime("mandor", "Pak Bambang", "Mandor Kebun", new Color(0.7f, 0.55f, 0.2f)));
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "Halo, Bos! Pekerja lagi panen TBS di kebun. Hasilnya lumayan hari ini!",
                autoNext = 1
            });
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "Mau rekrut pekerja baru? Pergi ke Mess Pekerja di Perumahan untuk menambah tenaga kerja.",
                choices = new System.Collections.Generic.List<DialogueAsset.DialogueChoice>
                {
                    new DialogueAsset.DialogueChoice { text = "Ya, buka Mess Pekerja", actionId = "recruit_worker", nextNodeIndex = -1 },
                    new DialogueAsset.DialogueChoice { text = "Nanti saja", actionId = "end_dialogue", nextNodeIndex = -1 },
                }
            });
            return dlg;
        }

        private static DialogueAsset CreateBankDialogue()
        {
            var dlg = DialogueAsset.CreateRuntime(
                NPCIdentity.CreateRuntime("pegawai_bank", "Ibu Siti", "Pegawai Bank", new Color(0.2f, 0.35f, 0.7f)));
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "Selamat datang di Bank Konvensional. Kami menawarkan pinjaman dengan bunga 5% per tahun.",
                autoNext = 1
            });
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "Mau mengajukan pinjaman? Bunga kami paling rendah di kota!",
                choices = new System.Collections.Generic.List<DialogueAsset.DialogueChoice>
                {
                    new DialogueAsset.DialogueChoice { text = "Ya, buka layanan Bank", actionId = "open_bank_modal", nextNodeIndex = -1 },
                    new DialogueAsset.DialogueChoice { text = "Tidak, terima kasih", actionId = "end_dialogue", nextNodeIndex = -1 },
                }
            });
            return dlg;
        }

        private static DialogueAsset CreatePinjolDialogue()
        {
            var dlg = DialogueAsset.CreateRuntime(
                NPCIdentity.CreateRuntime("debt_collector", "Budi Toll", "Debt Collector Pinjol", new Color(0.8f, 0.2f, 0.2f)));
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "HEI! Hutang Pinjol kamu menumpuk! Bunga 2% per hari, lho! Mau cairkan lagi atau bayar?",
                autoNext = 1
            });
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "Jangan telat bayar, atau saya kirim debt collector lagi ke rumahmu!",
                choices = new System.Collections.Generic.List<DialogueAsset.DialogueChoice>
                {
                    new DialogueAsset.DialogueChoice { text = "Buka layanan Pinjol", actionId = "open_pinjol_modal", nextNodeIndex = -1 },
                    new DialogueAsset.DialogueChoice { text = "Aku akan bayar nanti", actionId = "end_dialogue", nextNodeIndex = -1 },
                }
            });
            return dlg;
        }

        private static DialogueAsset CreateRentenirDialogue()
        {
            var dlg = DialogueAsset.CreateRuntime(
                NPCIdentity.CreateRuntime("rentenir", "Pak Slamet", "Rentenir Madura", new Color(0.6f, 0.4f, 0.1f)));
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "Butuh uang tunai cepat? Saya bisa bantu. Bunga hanya 1% per hari, murah!",
                autoNext = 1
            });
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "Tapi ingat, jangan sampai gagal bayar. Saya tidak segan mengambil jaminan Anda.",
                choices = new System.Collections.Generic.List<DialogueAsset.DialogueChoice>
                {
                    new DialogueAsset.DialogueChoice { text = "Lihat layanan Rentenir", actionId = "open_rentenir_modal", nextNodeIndex = -1 },
                    new DialogueAsset.DialogueChoice { text = "Tidak, terima kasih", actionId = "end_dialogue", nextNodeIndex = -1 },
                }
            });
            return dlg;
        }

        private static DialogueAsset CreateSepupuDialogue()
        {
            var dlg = DialogueAsset.CreateRuntime(
                NPCIdentity.CreateRuntime("sepupu", "Eddy Sepupu", "Sepupu Saingan", new Color(0.25f, 0.25f, 0.3f)));
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "Hahaha! Masih sibuk di kebun kecilmu? Net worth saya sudah miliaran, lho!",
                autoNext = 1
            });
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "Kita lihat siapa yang menang di tahun evaluasi. Jangan kecewa ya, Sepupu!",
                choices = new System.Collections.Generic.List<DialogueAsset.DialogueChoice>
                {
                    new DialogueAsset.DialogueChoice { text = "Kita lihat nanti...", actionId = "end_dialogue", nextNodeIndex = -1 },
                }
            });
            return dlg;
        }

        private static DialogueAsset CreateKepalaDesaDialogue()
        {
            var dlg = DialogueAsset.CreateRuntime(
                NPCIdentity.CreateRuntime("kepala_desa", "Pak Kades", "Kepala Desa", new Color(0.3f, 0.6f, 0.3f)));
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                text = "Halo, anak muda. Saya perhatikan ekspansi lahanmu semakin pesat.",
                autoNext = 1
            });
            dlg.nodes.Add(new DialogueAsset.DialogueNode
            {
                isDynamicText = true,
                text = "",
                choices = new System.Collections.Generic.List<DialogueAsset.DialogueChoice>
                {
                    new DialogueAsset.DialogueChoice { text = "Terima kasih, Pak Kades", actionId = "end_dialogue", nextNodeIndex = -1 },
                }
            });
            return dlg;
        }

        // ─── Helper Builders ──────────────────────────────────────────────

        private void BuildFacilityBuilding(Transform parent, string name, Vector3 localPos,
            FacilityType type, string hint, Color buildingColor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;

            // Visual bangunan (warna solid)
            CreateColoredSprite(go.transform, "Bangunan_Visual", Vector3.zero, size, buildingColor, 2);

            // Atap bangunan (lebih terang)
            Color roofColor = Color.Lerp(buildingColor, Color.white, 0.25f);
            CreateColoredSprite(go.transform, "Atap_Visual",
                new Vector3(0f, size.y * 0.5f + 0.4f, -0.1f), new Vector2(size.x + 0.5f, 0.8f), roofColor, 3);

            // Label nama bangunan
            CreateTextLabel(go.transform, name, new Vector3(0f, size.y * 0.5f + 1.2f, -0.2f));

            // Komponen interaksi
            var facility = go.AddComponent<InteractableFacility>();
            facility.Configure(type, name, hint, Mathf.Max(size.x, size.y) * 0.7f + 1f);

            // Solid collider agar pemain tidak bisa menembus bangunan
            var solidCol = go.AddComponent<BoxCollider2D>();
            solidCol.size = size;
            solidCol.offset = Vector2.zero;
            if (worldLayer >= 0)
                go.layer = worldLayer;
        }

        // ─── Primitif Visual ──────────────────────────────────────────────

        private void CreateColoredSprite(Transform parent, string name, Vector3 localPos, Vector2 size, Color color, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.sortingOrder = sortOrder;
        }

        private void CreateBuildingAsset(Transform parent, string name, Vector3 localPos,
            string resourcePath, Vector2 displaySize, int sortOrder)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[WorldBuildingBuilder] Asset bangunan tidak ditemukan: Resources/{resourcePath}");
                return;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = displaySize;
            sr.sortingOrder = sortOrder;
        }

        private void CreateTextLabel(Transform parent, string text, Vector3 localPos)
        {
            var go = new GameObject($"Label_{text}");
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;

            var tm = go.AddComponent<TextMesh>();
            // Potong teks panjang agar tidak melebihi lebar bangunan
            string displayText = text.Length > 24 ? text.Substring(0, 24) + ".." : text;
            tm.text = displayText;
            tm.fontSize = 14;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Color.white;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.08f;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10;
        }

        private void RegisterSpawnPoints()
        {
            // Buat atau temukan spawn points dari area root
            SetupSpawnPoint(ref kebunSpawnPoint, AreaNames[0], new Vector3(AreaCenters[0].x, AreaCenters[0].y - 5f, 0f), "SpawnPoint_Kebun");
            SetupSpawnPoint(ref perumahanSpawnPoint, AreaNames[1], new Vector3(AreaCenters[1].x - 20f, AreaCenters[1].y, 0f), "SpawnPoint_Perumahan");
            SetupSpawnPoint(ref kotaSpawnPoint, AreaNames[2], new Vector3(AreaCenters[2].x - 20f, AreaCenters[2].y, 0f), "SpawnPoint_Kota");
            SetupSpawnPoint(ref pabrikSpawnPoint, AreaNames[3], new Vector3(AreaCenters[3].x, AreaCenters[3].y + 10f, 0f), "SpawnPoint_Pabrik");
        }

        private void SetupSpawnPoint(ref Transform spawnField, string areaName, Vector3 worldPos, string objName)
        {
            if (spawnField != null) return;

            var go = new GameObject(objName);
            go.transform.SetParent(worldRoot.transform);
            go.transform.position = worldPos;
            spawnField = go.transform;
        }

        public static Vector3[] GetAreaSpawnPositions() => new Vector3[]
        {
            new Vector3(AreaCenters[0].x, AreaCenters[0].y - 5f, 0f),
            new Vector3(AreaCenters[1].x - 20f, AreaCenters[1].y, 0f),
            new Vector3(AreaCenters[2].x - 20f, AreaCenters[2].y, 0f),
            new Vector3(AreaCenters[3].x, AreaCenters[3].y + 10f, 0f),
        };
    }
}
