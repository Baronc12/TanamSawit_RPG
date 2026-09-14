using System.Collections;
using UnityEngine;
using TanamSawit.Player;

namespace TanamSawit.NPC
{
    /// <summary>
    /// Komponen NPC dunia. Saat pemain mendekat dan menekan [E], membuka dialog.
    /// Mendukung idle wander opsional (pola AnimalWanderer) dan sprite visual.
    /// Membuat NPCIdentity dan DialogueAsset saat runtime jika belum di-assign.
    /// </summary>
    public class NPCInteractable : MonoBehaviour
    {
        [Header("Identitas NPC")]
        [SerializeField] private NPCIdentity identity;
        [SerializeField] private DialogueAsset dialogue;

        [Header("Interaksi")]
        [SerializeField] private float interactionRadius = 2.0f;
        [SerializeField] private string interactionHint = "Bicara";

        [Header("Wander Opsional")]
        [SerializeField] private bool enableWander = false;
        [SerializeField] private float wanderSpeed = 1.0f;
        [SerializeField] private float wanderRadius = 3.0f;
        [SerializeField] private float minIdleTime = 2f;
        [SerializeField] private float maxIdleTime = 6f;

        private bool _playerInRange = false;
        private Transform _playerTransform;
        private Vector3 _spawnOrigin;
        private Vector3 _wanderTarget;
        private bool _isMoving = false;
        private SpriteRenderer _spriteRenderer;

        // Event untuk prompt UI (jika InteractPromptUI ada)
        public static event System.Action<NPCInteractable> OnPlayerEnterRange;
        public static event System.Action<NPCInteractable> OnPlayerExitRange;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            _spawnOrigin = transform.position;
            if (enableWander) StartCoroutine(WanderRoutine());
        }

        private void Update()
        {
            // Jangan proses interaksi jika modal sedang terbuka
            if (TanamSawit.UI.UIRoot.IsModalOpen)
            {
                _playerInRange = false;
                return;
            }

            // Proximity check
            Transform playerT = GetPlayerTransform();
            if (playerT == null) return;

            float dist = Vector2.Distance(transform.position, playerT.position);
            bool wasInRange = _playerInRange;
            _playerInRange = dist <= interactionRadius;

            if (_playerInRange && !wasInRange)
                OnPlayerEnterRange?.Invoke(this);
            else if (!_playerInRange && wasInRange)
                OnPlayerExitRange?.Invoke(this);

            // Interaksi
            if (_playerInRange && IsInteractPressed())
            {
                Interact();
            }

            // Wander movement
            if (_isMoving && enableWander)
            {
                transform.position = Vector3.MoveTowards(transform.position, _wanderTarget, wanderSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, _wanderTarget) < 0.1f)
                    _isMoving = false;

                if (_spriteRenderer != null)
                    _spriteRenderer.flipX = _wanderTarget.x < transform.position.x;
            }
        }

        public void Interact()
        {
            if (dialogue == null) return;

            if (DialogueUI.Instance != null)
            {
                // Dynamic resolver untuk Kepala Desa (level karma)
                System.Func<int, string> resolver = null;
                if (identity != null && identity.npcId == "kepala_desa")
                    resolver = GetKepalaDesaDynamicText;

                DialogueUI.Instance.StartDialogue(dialogue, resolver);
            }
        }

        /// <summary>Dynamic text untuk Kepala Desa berdasarkan KarmaLevel.</summary>
        private string GetKepalaDesaDynamicText(int nodeIndex)
        {
            var karma = TanamSawit.Managers.EnvironmentalKarmaManager.Instance;
            if (karma == null) return "Halo, anak muda.";

            switch (karma.CurrentKarma)
            {
                case TanamSawit.Managers.KarmaLevel.Aman:
                    return "Alhamdulillah, hutan dan kebun masih seimbang. Pertahankan!";
                case TanamSawit.Managers.KarmaLevel.InvasiMonyet:
                    return "Hati-hati! Monyet-monyet mulai turun gunung. Kurangi ekspansi lahan!";
                case TanamSawit.Managers.KarmaLevel.SeranganGajah:
                    return "Gajah-gajah sudah masuk desa! Hentikan penebangan hutan sekarang!";
                case TanamSawit.Managers.KarmaLevel.TerorMacan:
                    return "Macan tutul berkeliling! Ini peringatan terakhir alam, Nak!";
                case TanamSawit.Managers.KarmaLevel.KiamatLongsor:
                    return "Terlambat... longsor sudah dekat. Semoga Tuhan merahmati kita semua.";
                default:
                    return "Halo, anak muda.";
            }
        }

        // ── Wander ───────────────────────────────────────────────────────

        private IEnumerator WanderRoutine()
        {
            while (true)
            {
                _isMoving = false;
                float waitTime = Random.Range(minIdleTime, maxIdleTime);
                yield return new WaitForSeconds(waitTime);

                Vector2 offset = Random.insideUnitCircle * wanderRadius;
                _wanderTarget = _spawnOrigin + new Vector3(offset.x, offset.y, 0);
                _isMoving = true;

                // Jangan wander saat pemain berinteraksi
                if (_playerInRange)
                {
                    _isMoving = false;
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private Transform GetPlayerTransform()
        {
            if (_playerTransform != null) return _playerTransform;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                return _playerTransform;
            }

            return null;
        }

        private static bool IsInteractPressed()
        {
            return TanamSawit.Core.InputEdgeDetection.EOrSpace();
        }

        // ── Public config (dipanggil oleh WorldBuildingBuilder) ──────────

        public string DisplayName => identity != null ? identity.displayName : "NPC";
        public bool PlayerInRange => _playerInRange;
        public string InteractionHint => interactionHint;

        public void Configure(NPCIdentity npcIdentity, DialogueAsset npcDialogue, float radius, bool wander = false)
        {
            identity = npcIdentity;
            dialogue = npcDialogue;
            interactionRadius = radius;
            enableWander = wander;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
