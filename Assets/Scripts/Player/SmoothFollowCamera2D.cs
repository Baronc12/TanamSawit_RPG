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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
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
        }

        /// <summary>
        /// Memindahkan posisi kamera secara instan (tanpa glide) saat pemain berpindah area.
        /// </summary>
        public void SnapTo(Vector3 position)
        {
            currentVelocity = Vector3.zero;
            transform.position = new Vector3(position.x + offset.x, position.y + offset.y, offset.z);
        }
    }
}
