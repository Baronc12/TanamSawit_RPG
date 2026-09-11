using System.Collections;
using UnityEngine;

namespace TanamSawit.Environment
{
    /// <summary>
    /// AI pergerakan sederhana untuk hewan (Monyet, Gajah, Macan) di Scene 2D.
    /// Menggunakan Vector3.MoveTowards dengan siklus Berjalan -> Berhenti (Idle) -> Cari Tujuan Baru.
    /// </summary>
    public class AnimalWanderer : MonoBehaviour
    {
        [Header("Pengaturan Pergerakan")]
        [Tooltip("Kecepatan gerak hewan (unit per detik).")]
        [SerializeField] private float moveSpeed = 2.0f;

        [Tooltip("Radius area jelajah dari titik awal spawn.")]
        [SerializeField] private float wanderRadius = 4.0f;

        [Tooltip("Berapa detik hewan diam sebelum berpindah tempat.")]
        [SerializeField] private float minIdleTime = 1.0f;
        [SerializeField] private float maxIdleTime = 3.0f;

        private Vector3 spawnOrigin;
        private Vector3 currentTarget;
        private bool isMoving = false;
        public bool IsMoving => isMoving;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            spawnOrigin = transform.position;
            StartCoroutine(WanderRoutine());
        }

        private IEnumerator WanderRoutine()
        {
            while (true)
            {
                // 1. Fase Idle: Hewan diam sejenak
                isMoving = false;
                float waitTime = Random.Range(minIdleTime, maxIdleTime);
                yield return new WaitForSeconds(waitTime);

                // 2. Pilih titik target acak dalam radius wander
                Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
                currentTarget = spawnOrigin + new Vector3(randomOffset.x, randomOffset.y, 0f);
                isMoving = true;

                // Atur arah hadap sprite (Flip X) berdasarkan arah gerak
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = currentTarget.x < transform.position.x;
                }

                // 3. Fase Bergerak: Berjalan menuju target
                while (Vector3.Distance(transform.position, currentTarget) > 0.1f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position, 
                        currentTarget, 
                        moveSpeed * Time.deltaTime
                    );

                    yield return null;
                }

                transform.position = currentTarget;
            }
        }

        public void SetWanderCenter(Vector3 center, float radius)
        {
            spawnOrigin = center;
            wanderRadius = radius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Application.isPlaying ? spawnOrigin : transform.position, wanderRadius);
        }
    }
}
