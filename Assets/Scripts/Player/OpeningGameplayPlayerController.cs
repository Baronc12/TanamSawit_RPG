using UnityEngine;
using UnityEngine.SceneManagement;
using TanamSawit.Environment;
using TanamSawit.Managers;

namespace TanamSawit.Player
{
    /// <summary>
    /// Membuat dan mengontrol karakter pemain pada scene "opening gameplay".
    /// Gerakan menggunakan WASD dan sprite berjalan diputar hanya saat bergerak.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class OpeningGameplayPlayerController : MonoBehaviour
    {
        private const int WalkFrameCount = 6;

        [Header("Gerakan")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;

        [Header("Animasi")]
        [SerializeField, Min(1f)] private float walkFramesPerSecond = 10f;
        [SerializeField] private bool placeAtCameraCenterOnStart = true;
        [SerializeField] private bool isMovementLocked = false;
        public bool IsMovementLocked => isMovementLocked;

        private SpriteRenderer spriteRenderer;
        private Sprite[] walkFrames;
        private float animationTimer;
        private int currentFrame;
        private Rigidbody2D rb;
        private Vector2 moveInput;

        private void Awake()
        {
            if (!gameObject.CompareTag("Player"))
            {
                try { gameObject.tag = "Player"; } catch {}
            }

            // Pastikan memiliki Collider2D dan Rigidbody2D dinamis agar fisika & trigger bekerja
            if (GetComponent<Collider2D>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.8f, 1.2f);
                col.offset = new Vector2(0f, 0.4f);
            }

            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            // Dynamic body dengan gravityScale 0 = gerakan top-down via MovePosition
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Set layer Player jika tersedia di Project Settings
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                gameObject.layer = playerLayer;

            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 20;
            CreateWalkFrames();
        }

        private void Start()
        {
            // Saat berada di state MainMenu, kamera belum mengikuti karakter.
            // Kita posisikan karakter di dekat kamera tapi kunci gerakannya sampai
            // pengguna menekan tombol Play pada main menu.
            bool inMenu = GameManager.Instance != null &&
                          GameManager.Instance.CurrentState == GameState.MainMenu;
            if (inMenu)
            {
                isMovementLocked = true;
                if (placeAtCameraCenterOnStart && Camera.main != null)
                {
                    Vector3 cameraPosition = Camera.main.transform.position;
                    transform.position = new Vector3(cameraPosition.x, cameraPosition.y - 1.5f, 0f);
                }
            }
            else if (placeAtCameraCenterOnStart && Camera.main != null)
            {
                Vector3 cameraPosition = Camera.main.transform.position;
                transform.position = new Vector3(cameraPosition.x, cameraPosition.y - 1.5f, 0f);
            }
        }

        public void SetMovementLocked(bool locked)
        {
            isMovementLocked = locked;
        }

        private void Update()
        {
            // Jangan bergerak jika input dikunci oleh modal dialog, saat transisi,
            // atau ketika masih berada di state MainMenu (belum menekan Play).
            bool transitionActive = AreaTransitionManager.Instance != null && AreaTransitionManager.Instance.IsTransitioning;
            bool inMainMenu = GameManager.Instance != null &&
                              GameManager.Instance.CurrentState == GameState.MainMenu;
            if (isMovementLocked || transitionActive || inMainMenu)
            {
                moveInput = Vector2.zero;
                animationTimer = 0f;
                currentFrame = 0;
                ShowFrame(currentFrame);
                return;
            }

            Vector2 movement = ReadWasdInput();
            bool isMoving = movement.sqrMagnitude > 0f;

            if (isMoving)
            {
                movement.Normalize();
                moveInput = movement;

                if (movement.x != 0f)
                    spriteRenderer.flipX = movement.x < 0f;

                AnimateWalk();
            }
            else
            {
                moveInput = Vector2.zero;
                animationTimer = 0f;
                currentFrame = 0;
                ShowFrame(currentFrame);
            }
        }

        private void FixedUpdate()
        {
            if (isMovementLocked || rb == null)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Teleport instan pemain ke posisi dunia. Menyetel transform.position dan rb.position
        /// sekaligus agar interpolasi Rigidbody2D tidak menyebabkan slide residual.
        /// Dipanggil oleh AreaTransitionManager (portal & save/load).
        /// </summary>
        public void Teleport(Vector3 worldPosition)
        {
            Vector3 pos = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            transform.position = pos;
            if (rb != null)
            {
                rb.position = new Vector2(pos.x, pos.y);
                rb.linearVelocity = Vector2.zero;
            }
            moveInput = Vector2.zero;
        }

        private static Vector2 ReadWasdInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vertical += 1f;
                return new Vector2(horizontal, vertical);
            }
#endif
            try
            {
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            }
            catch {}

            return new Vector2(horizontal, vertical);
        }

        private void AnimateWalk()
        {
            if (walkFrames == null || walkFrames.Length == 0) return;

            animationTimer += Time.deltaTime;
            float secondsPerFrame = 1f / walkFramesPerSecond;
            if (animationTimer < secondsPerFrame) return;

            animationTimer -= secondsPerFrame;
            currentFrame = (currentFrame + 1) % walkFrames.Length;
            ShowFrame(currentFrame);
        }

        private void CreateWalkFrames()
        {
            Texture2D sheet = Resources.Load<Texture2D>("Characters/ExplorerWalk");
            if (sheet == null)
            {
                Debug.LogError("[OpeningGameplayPlayer] ExplorerWalk.png tidak ditemukan di Resources/Characters.");
                return;
            }

            sheet.filterMode = FilterMode.Point;
            walkFrames = new Sprite[WalkFrameCount];
            float frameWidth = sheet.width / (float)WalkFrameCount;
            float frameHeight = sheet.height;
            for (int i = 0; i < WalkFrameCount; i++)
            {
                // Hitung dari ukuran texture yang benar-benar dimuat. Ini tetap aman
                // jika platform build memilih ukuran import berbeda.
                Rect frameRect = new Rect(i * frameWidth, 0f, frameWidth, frameHeight);
                walkFrames[i] = Sprite.Create(sheet, frameRect, new Vector2(0.5f, 0.08f), 100f);
                walkFrames[i].name = $"ExplorerWalk_{i + 1}";
            }

            ShowFrame(0);
        }

        private void ShowFrame(int frameIndex)
        {
            if (walkFrames == null || walkFrames.Length == 0) return;
            spriteRenderer.sprite = walkFrames[Mathf.Clamp(frameIndex, 0, walkFrames.Length - 1)];
        }
    }

    /// <summary>
    /// Menempelkan controller pada object animation_Char ketika scene opening gameplay dimuat.
    /// Tidak mengubah scene lain.
    /// </summary>
    public static class OpeningGameplayPlayerBootstrapper
    {
        // Dinonaktifkan agar tidak ada eksekusi otomatis saat Play
        public static void CreatePlayerForOpeningGameplay()
        {
            if (SceneManager.GetActiveScene().name != "opening gameplay") return;

            GameObject player = GameObject.Find("animation_Char");
            if (player == null)
                player = new GameObject("animation_Char");

            if (player.GetComponent<OpeningGameplayPlayerController>() == null)
                player.AddComponent<OpeningGameplayPlayerController>();
        }
    }
}
