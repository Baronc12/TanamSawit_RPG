using UnityEngine;
using UnityEngine.SceneManagement;

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

        private SpriteRenderer spriteRenderer;
        private Sprite[] walkFrames;
        private float animationTimer;
        private int currentFrame;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 20;
            CreateWalkFrames();
        }

        private void Start()
        {
            if (placeAtCameraCenterOnStart && Camera.main != null)
            {
                Vector3 cameraPosition = Camera.main.transform.position;
                transform.position = new Vector3(cameraPosition.x, cameraPosition.y - 1.5f, 0f);
            }
        }

        private void Update()
        {
            Vector2 movement = ReadWasdInput();
            bool isMoving = movement.sqrMagnitude > 0f;

            if (isMoving)
            {
                movement.Normalize();
                transform.position += (Vector3)(movement * moveSpeed * Time.deltaTime);

                if (movement.x != 0f)
                    spriteRenderer.flipX = movement.x < 0f;

                AnimateWalk();
            }
            else
            {
                animationTimer = 0f;
                currentFrame = 0;
                ShowFrame(currentFrame);
            }
        }

        private static Vector2 ReadWasdInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W)) vertical += 1f;

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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreatePlayerForOpeningGameplay()
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
