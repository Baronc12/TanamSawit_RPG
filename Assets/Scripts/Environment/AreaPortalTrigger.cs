using UnityEngine;

namespace TanamSawit.Environment
{
    /// <summary>
    /// Ditempelkan ke gerbang atau jalan perbatasan antar area.
    /// Saat pemain menyentuh trigger collider ini, memicu transisi fade/loading
    /// dan memindahkan posisi pemain ke area target.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AreaPortalTrigger : MonoBehaviour
    {
        [Header("Konfigurasi Area Tujuan")]
        [Tooltip("Nama area tujuan yang akan ditampilkan di layar transisi.")]
        [SerializeField] private string targetAreaName = "Area Perumahan";

        [Tooltip("Titik kedatangan pemain di area tujuan.")]
        [SerializeField] private Transform targetSpawnPoint;

        [Tooltip("Koordinat cadangan jika targetSpawnPoint tidak di-assign.")]
        [SerializeField] private Vector3 fallbackSpawnCoordinates = Vector3.zero;

        [Header("Pengaturan Trigger")]
        [Tooltip("Pemain harus menekan E untuk menyeberang jika true, atau otomatis saat disentuh jika false.")]
        [SerializeField] private bool requireInteractionKey = false;

        private bool playerInRange = false;

        public string TargetAreaName => targetAreaName;
        public Vector3 TargetPosition => targetSpawnPoint != null ? targetSpawnPoint.position : fallbackSpawnCoordinates;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;

            playerInRange = true;

            if (!requireInteractionKey)
            {
                TriggerTransition();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            playerInRange = false;
        }

        private void Update()
        {
            if (playerInRange && requireInteractionKey)
            {
                if (!TanamSawit.UI.UIRoot.IsModalOpen && IsInteractPressed())
                {
                    TriggerTransition();
                }
            }
        }

        public void TriggerTransition()
        {
            if (AreaTransitionManager.Instance == null)
            {
                Debug.LogWarning("[AreaPortalTrigger] AreaTransitionManager tidak ditemukan di scene!");
                return;
            }

            Vector3 spawnPos = targetSpawnPoint != null ? targetSpawnPoint.position : fallbackSpawnCoordinates;
            AreaTransitionManager.Instance.TransitionToArea(targetAreaName, spawnPos);
        }

        private static bool IsPlayer(Collider2D col)
        {
            if (col.CompareTag("Player")) return true;
            string lower = col.name.ToLower();
            return lower.Contains("player") || lower.Contains("char");
        }

        private static bool IsInteractPressed()
        {
            return TanamSawit.Core.InputEdgeDetection.EOrSpace();
        }

        public void Configure(string areaName, Vector3 targetPos, bool requireKey = false)
        {
            targetAreaName = areaName;
            fallbackSpawnCoordinates = targetPos;
            requireInteractionKey = requireKey;
        }
    }
}
