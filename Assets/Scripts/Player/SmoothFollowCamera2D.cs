using UnityEngine;

namespace TanamSawit.Player
{
    /// <summary>
    /// Mengontrol kamera 2D agar mengikuti pergerakan MC secara halus (Smooth Follow)
    /// di tengah layar dan mendukung snap instan saat transisi loading antar area.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SmoothFollowCamera2D : MonoBehaviour
    {
        public static SmoothFollowCamera2D Instance { get; private set; }

        [Header("Target Karakter")]
        [SerializeField] private Transform target;

        [Header("Pengaturan Gerakan Halus")]
        [Tooltip("Waktu redaman pergerakan kamera (semakin kecil semakin cepat mengikuti).")]
        [SerializeField, Range(0.01f, 0.5f)] private float smoothTime = 0.12f;

        [Tooltip("Offset posisi kamera terhadap target.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("Ikuti Kamera")]
        [Tooltip("Jika dimatikan, kamera tidak akan mengikuti target (mis. saat main menu). Aktifkan saat gameplay berjalan.")]
        [SerializeField] private bool followEnabled = false;
        public bool FollowEnabled
        {
            get => followEnabled;
            set => followEnabled = value;
        }

        private Vector3 currentVelocity = Vector3.zero;
        private Bounds? worldBounds;
        private Camera cam;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                FindPlayerTarget();
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                SnapTo(target.position);
            }
        }

        private void FindPlayerTarget()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
#pragma warning disable CS0618
                var controller = FindFirstObjectByType<OpeningGameplayPlayerController>();
#pragma warning restore CS0618
                if (controller != null) target = controller.transform;
            }
        }

        private void LateUpdate()
        {
            if (!followEnabled) return;

            if (target == null)
            {
                FindPlayerTarget();
                if (target == null) return;
            }

            Vector3 destination = target.position + offset;
            destination.z = offset.z; // Kunci sumbu Z kamera tetap pada jarak 2D

            transform.position = Vector3.SmoothDamp(transform.position, destination, ref currentVelocity, smoothTime);

            // Clamp posisi kamera dalam batas area aktif
            if (worldBounds.HasValue)
            {
                ClampToBounds();
            }
        }

        private void ClampToBounds()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;

            Bounds b = worldBounds.Value;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            Vector3 pos = transform.position;

            // Jika area lebih kecil dari viewport pada sumbu X, pusatkan; selalu clamp
            if (b.size.x >= halfW * 2f)
                pos.x = Mathf.Clamp(pos.x, b.min.x + halfW, b.max.x - halfW);
            else
                pos.x = b.center.x;

            if (b.size.y >= halfH * 2f)
                pos.y = Mathf.Clamp(pos.y, b.min.y + halfH, b.max.y - halfH);
            else
                pos.y = b.center.y;

            pos.z = transform.position.z;
            transform.position = pos;
        }

        /// <summary>
        /// Menetapkan batas area aktif untuk kamera. Dipanggil saat pemasuki area baru.
        /// </summary>
        public void SetBounds(Bounds bounds)
        {
            worldBounds = bounds;
        }

        /// <summary>
        /// Menghapus batas area (mis. saat main menu). Kamera bebas mengikuti.
        /// </summary>
        public void ClearBounds()
        {
            worldBounds = null;
        }

        /// <summary>
        /// Memindahkan posisi kamera secara instan (tanpa glide) saat pemain berpindah area.
        /// </summary>
        public void SnapTo(Vector3 position)
        {
            currentVelocity = Vector3.zero;
            Vector3 snapped = new Vector3(position.x + offset.x, position.y + offset.y, offset.z);

            // Jika bounds aktif, clamp posisi snap juga
            if (worldBounds.HasValue)
            {
                if (cam == null) cam = GetComponent<Camera>();
                if (cam != null)
                {
                    Bounds b = worldBounds.Value;
                    float halfH = cam.orthographicSize;
                    float halfW = halfH * cam.aspect;
                    if (b.size.x >= halfW * 2f)
                        snapped.x = Mathf.Clamp(snapped.x, b.min.x + halfW, b.max.x - halfW);
                    if (b.size.y >= halfH * 2f)
                        snapped.y = Mathf.Clamp(snapped.y, b.min.y + halfH, b.max.y - halfH);
                }
            }

            transform.position = snapped;
        }
    }
}
