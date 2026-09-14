#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TanamSawit.Managers;
using TanamSawit.Buildings;
using TanamSawit.SaveSystem;
using TanamSawit.Environment;
using TanamSawit.UI;
using TanamSawit.Player;

namespace TanamSawit.EditorTools
{
    public static class TanamSawitSetupEditor
    {
        [MenuItem("Tanam Sawit/🚀 1-Klik: Pasang [MANAGERS] ke Scene Ini", false, 1)]
        public static void SetupManagersInScene()
        {
            // Cek apakah [MANAGERS] sudah ada
            GameObject existing = GameObject.Find("[MANAGERS]");
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                EditorUtility.DisplayDialog("Sudah Terpasang", "GameObject [MANAGERS] sudah ada di scene ini!", "OK");
                return;
            }

            // Buat GameObject baru
            GameObject managersGo = new GameObject("[MANAGERS]");
            Undo.RegisterCreatedObjectUndo(managersGo, "Create [MANAGERS]");

            // Pasang semua komponen Manager
            managersGo.AddComponent<GameManager>();
            managersGo.AddComponent<EconomyManager>();
            managersGo.AddComponent<TimeManager>();
            managersGo.AddComponent<WorkerManager>();
            managersGo.AddComponent<BuildingManager>();
            managersGo.AddComponent<LoanManager>();
            managersGo.AddComponent<EnvironmentalKarmaManager>();
            managersGo.AddComponent<RivalManager>();
            managersGo.AddComponent<SaveManager>();
            managersGo.AddComponent<SettingsManager>();

            // Pasang visual 2D Grid & Ecology Spawner
            managersGo.AddComponent<GridManager>();
            managersGo.AddComponent<EcologySpawner>();

            // Pasang UI uGUI (menggantikan IMGUI TycoonHUD/ModernTycoonHUD)
            TanamSawit.UI.UIRoot.EnsureExists();

            // Tandai scene berubah agar bisa di-Ctrl+S
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = managersGo;

            EditorUtility.DisplayDialog("Sukses!",
                "GameObject [MANAGERS] dan seluruh sistem GDD (Ekonomi, Waktu, Pekerja, Karma, Rival) + UI uGUI telah berhasil dipasang!\n\nSekarang Anda cukup tekan tombol PLAY di Unity.",
                "Mantap!");
        }

        [MenuItem("Tanam Sawit/🌴 1-Klik: Bangun 4 Area Dunia (Kebun, Perumahan, Kota, Pabrik)", false, 2)]
        public static void BuildFourWorldAreas()
        {
            // Pastikan [MANAGERS] sudah ada
            GameObject managers = GameObject.Find("[MANAGERS]");
            if (managers == null)
            {
                bool autoSetup = EditorUtility.DisplayDialog(
                    "Tanpa [MANAGERS]",
                    "[MANAGERS] belum ada di scene. Pasang otomatis dulu?",
                    "Pasang [MANAGERS]", "Batal");
                if (!autoSetup) return;
                SetupManagersInScene();
                managers = GameObject.Find("[MANAGERS]");
                if (managers == null) return;
            }

            // Pastikan ada WorldBuildingBuilder di scene
            WorldBuildingBuilder builder = FindWorldBuilder();
            if (builder == null)
            {
                GameObject builderGO = new GameObject("WorldBuildingBuilder");
                Undo.RegisterCreatedObjectUndo(builderGO, "Create WorldBuildingBuilder");
                builder = builderGO.AddComponent<WorldBuildingBuilder>();
                builder.transform.SetParent(managers.transform);
            }

            // Bangun dunia
            builder.BuildWorld();

            // Pastikan ada AreaTransitionManager
            if (AreaTransitionManager.Instance == null)
            {
                GameObject transitionGO = new GameObject("AreaTransitionManager");
                Undo.RegisterCreatedObjectUndo(transitionGO, "Create AreaTransitionManager");
                transitionGO.AddComponent<AreaTransitionManager>();
            }

            // Pastikan ada player dengan controller
            SetupPlayerInScene(builder);

            // Pastikan kamera mengikuti player
            SetupSmoothCamera();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = builder.gameObject;

            EditorUtility.DisplayDialog("Sukses!",
                "4 Area Dunia (Kebun, Perumahan, Kota, Pabrik) telah dibangun!\n\n" +
                "Portals, fasilitas interaktif, spawn points, dan sistem transisi sudah siap.\n" +
                "Tekan PLAY untuk mulai eksplor.",
                "Mantul!");
        }

        private static WorldBuildingBuilder FindWorldBuilder() // v2
        {
#pragma warning disable CS0618
            var builders = Object.FindObjectsByType<WorldBuildingBuilder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#pragma warning restore CS0618
            if (builders != null && builders.Length > 0) return builders[0];
            return null;
        }

        private static void SetupPlayerInScene(WorldBuildingBuilder builder)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("animation_Char");
            }

            if (player == null)
            {
                player = new GameObject("animation_Char");
                Undo.RegisterCreatedObjectUndo(player, "Create Player Character");
                player.tag = "Player";
                player.AddComponent<SpriteRenderer>();
                player.AddComponent<OpeningGameplayPlayerController>();
            }

            // Posisikan player di spawn point area pertama (Kebun)
            Vector3 spawnPos = builder.kebunSpawnPoint != null
                ? builder.kebunSpawnPoint.position
                : new Vector3(0f, -5f, 0f);
            player.transform.position = spawnPos;
        }

        private static void SetupSmoothCamera()
        {
            GameObject camGO = GameObject.Find("Main Camera");
            if (camGO == null)
            {
                camGO = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(camGO, "Create Main Camera");
            }

            var cam = camGO.GetComponent<Camera>();
            if (cam == null)
            {
                cam = camGO.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 8f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            }

            camGO.tag = "MainCamera";

            var smoothCam = camGO.GetComponent<SmoothFollowCamera2D>();
            if (smoothCam == null)
            {
                smoothCam = camGO.AddComponent<SmoothFollowCamera2D>();
            }

            // Link camera to player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                smoothCam.SetTarget(player.transform);
            }
        }
    }
}
#endif
