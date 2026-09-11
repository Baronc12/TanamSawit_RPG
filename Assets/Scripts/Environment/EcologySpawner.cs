using System.Collections.Generic;
using UnityEngine;
using TanamSawit.Managers;

namespace TanamSawit.Environment
{
    /// <summary>
    /// EcologySpawner merespons event dari EnvironmentalKarmaManager dan memunculkan
    /// representasi visual satwa liar (Monyet, Gajah, Macan) di Scene 2D.
    /// Dilengkapi otomatis dengan fallback procedural jika Prefab belum di-assign di Inspector!
    /// </summary>
    public class EcologySpawner : MonoBehaviour
    {
        public static EcologySpawner Instance { get; private set; }

        [Header("Prefab Satwa (Sesuai Request)")]
        [Tooltip("Prefab karakter Monyet yang akan dimunculkan saat deforestasi 75%.")]
        [SerializeField] private GameObject monyetPrefab;

        [Tooltip("Prefab Gajah saat deforestasi 85% (Opsional).")]
        [SerializeField] private GameObject gajahPrefab;

        [Tooltip("Prefab Macan saat deforestasi 90% (Opsional).")]
        [SerializeField] private GameObject macanPrefab;

        [Header("Pengaturan Area Spawn")]
        [Tooltip("Titik pusat area spawn (default: posisi objek ini).")]
        [SerializeField] private Transform spawnCenter;

        [Tooltip("Radius sebaran kemunculan satwa di luar kebun.")]
        [SerializeField] private float spawnRadius = 6.0f;

        [Header("Jumlah Hewan per Event")]
        [SerializeField] private int monkeyCount = 4;
        [SerializeField] private int elephantCount = 2;
        [SerializeField] private int tigerCount = 2;

        private List<GameObject> activeAnimals = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (spawnCenter == null) spawnCenter = transform;
        }

        private void Start()
        {
            if (EnvironmentalKarmaManager.Instance != null)
            {
                EnvironmentalKarmaManager.Instance.OnKarmaTriggered += HandleKarmaEvent;
            }
        }

        private void OnDestroy()
        {
            if (EnvironmentalKarmaManager.Instance != null)
            {
                EnvironmentalKarmaManager.Instance.OnKarmaTriggered -= HandleKarmaEvent;
            }
        }

        private void HandleKarmaEvent(KarmaLevel level, string log)
        {
            switch (level)
            {
                case KarmaLevel.InvasiMonyet:
                    SpawnMonkeys();
                    break;
                case KarmaLevel.SeranganGajah:
                    SpawnElephants();
                    break;
                case KarmaLevel.TerorMacan:
                    SpawnTigers();
                    break;
            }
        }

        /// <summary>
        /// Memunculkan kawanan monyet di sekitar perimeter lahan sawit.
        /// </summary>
        [ContextMenu("Test Spawn Monyet")]
        public void SpawnMonkeys()
        {
            Debug.Log("<color=#FFAA00>[EcologySpawner] Memunculkan kawanan Monyet di sekitar kebun!</color>");

            for (int i = 0; i < monkeyCount; i++)
            {
                Vector3 spawnPos = GetRandomPerimeterPosition();
                GameObject monkeyObj;

                if (monyetPrefab != null)
                {
                    monkeyObj = Instantiate(monyetPrefab, spawnPos, Quaternion.identity, transform);
                }
                else
                {
                    // Fallback visual jika user belum memasang prefab di Inspector
                    monkeyObj = CreateFallbackAnimal("Monyet_Visual", spawnPos, new Color(0.6f, 0.4f, 0.2f), 0.6f);
                }

                // Pastikan memiliki komponen AI pergerakan mondar-mandir
                var wanderer = monkeyObj.GetComponent<AnimalWanderer>();
                if (wanderer == null)
                {
                    wanderer = monkeyObj.AddComponent<AnimalWanderer>();
                }
                wanderer.SetWanderCenter(spawnPos, 3.5f);

                activeAnimals.Add(monkeyObj);
            }
        }

        [ContextMenu("Test Spawn Gajah")]
        public void SpawnElephants()
        {
            Debug.Log("<color=#FFAA00>[EcologySpawner] Memunculkan Gajah perusak pabrik!</color>");

            for (int i = 0; i < elephantCount; i++)
            {
                Vector3 spawnPos = GetRandomPerimeterPosition();
                GameObject elephantObj;

                if (gajahPrefab != null)
                {
                    elephantObj = Instantiate(gajahPrefab, spawnPos, Quaternion.identity, transform);
                }
                else
                {
                    elephantObj = CreateFallbackAnimal("Gajah_Visual", spawnPos, Color.gray, 1.4f);
                }

                var wanderer = elephantObj.GetComponent<AnimalWanderer>();
                if (wanderer == null) wanderer = elephantObj.AddComponent<AnimalWanderer>();
                wanderer.SetWanderCenter(spawnPos, 4.5f);

                activeAnimals.Add(elephantObj);
            }
        }

        [ContextMenu("Test Spawn Macan")]
        public void SpawnTigers()
        {
            Debug.Log("<color=#FF5500>[EcologySpawner] Memunculkan Macan tutul pemangsa!</color>");

            for (int i = 0; i < tigerCount; i++)
            {
                Vector3 spawnPos = GetRandomPerimeterPosition();
                GameObject tigerObj;

                if (macanPrefab != null)
                {
                    tigerObj = Instantiate(macanPrefab, spawnPos, Quaternion.identity, transform);
                }
                else
                {
                    tigerObj = CreateFallbackAnimal("Macan_Visual", spawnPos, new Color(1f, 0.5f, 0f), 0.9f);
                }

                var wanderer = tigerObj.GetComponent<AnimalWanderer>();
                if (wanderer == null) wanderer = tigerObj.AddComponent<AnimalWanderer>();
                wanderer.SetWanderCenter(spawnPos, 5f);

                activeAnimals.Add(tigerObj);
            }
        }

        private Vector3 GetRandomPerimeterPosition()
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(spawnRadius * 0.7f, spawnRadius);
            Vector3 center = spawnCenter != null ? spawnCenter.position : Vector3.zero;
            return center + new Vector3(randomDir.x * dist, randomDir.y * dist, 0f);
        }

        /// <summary>
        /// Membuat Sprite 2D procedural sederhana sebagai visual sementara jika prefab belum disiapkan.
        /// </summary>
        private GameObject CreateFallbackAnimal(string name, Vector3 pos, Color color, float scale)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            go.transform.SetParent(transform);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = color;
            sr.sortingOrder = 10;

            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private Sprite CreateCircleSprite()
        {
            Texture2D tex = new Texture2D(32, 32);
            Color[] colors = new Color[32 * 32];
            Vector2 center = new Vector2(16, 16);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    colors[y * 32 + x] = d <= 14 ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;
            Gizmos.DrawWireSphere(center, spawnRadius);
        }
    }
}
