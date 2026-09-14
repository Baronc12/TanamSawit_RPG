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
    public static class UnifiedSceneBuilder
    {
        [MenuItem("Tanam Sawit/🎮 1-Klik: Bangun Scene Gameplay Terpadu (SampleScene)", false, 3)]
        public static void BuildUnifiedGameplayScene()
        {
            BuildUnifiedGameplaySceneImpl();
            EditorUtility.DisplayDialog("Sukses!",
                "Scene gameplay terpadu berhasil dibangun!\n\n" +
                "4 Area Dunia, Portal Transisi, Fasilitas Interaktif, Player Controller, " +
                "Smooth Follow Camera, dan semua Manager tersedia.\n\n" +
                "Tekan PLAY untuk memulai!",
                "Mantul!");
        }

        public static void BuildUnifiedGameplaySceneImpl()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";

            // Buka scene secara eksplisit
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Ensure managers exist
            EnsureManagers();

            // Ensure world is built
            WorldBuildingBuilder builder = FindOrCreateWorldBuilder();
            if (builder != null)
            {
                builder.BuildWorld();
            }

            // Ensure AreaTransitionManager exists
            if (Object.FindAnyObjectByType<AreaTransitionManager>() == null)
            {
                GameObject go = new GameObject("AreaTransitionManager");
                Undo.RegisterCreatedObjectUndo(go, "Create AreaTransitionManager");
                go.AddComponent<AreaTransitionManager>();
            }

            // Ensure player exists
            SetupPlayer(builder);

            // Ensure smooth camera
            SetupCamera();

            // Add prototype_Movement scene to build settings if not already there
            var buildScenes = EditorBuildSettings.scenes;
            bool hasMovement = false;
            foreach (var s in buildScenes)
            {
                if (System.IO.Path.GetFileName(s.path) == "prototype_Movement.unity")
                {
                    hasMovement = true;
                    break;
                }
            }
            if (!hasMovement)
            {
                var newScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(buildScenes);
                newScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/prototype_Movement.unity", true));
                EditorBuildSettings.scenes = newScenes.ToArray();
            }

            // Simpan scene
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath, false);
            AssetDatabase.SaveAssets();

            Debug.Log("[UnifiedSceneBuilder] Scene saved successfully");
        }

        private static void EnsureManagers()
        {
            GameObject managers = GameObject.Find("[MANAGERS]");
            if (managers == null)
            {
                managers = new GameObject("[MANAGERS]");

                managers.AddComponent<GameManager>();
                managers.AddComponent<EconomyManager>();
                managers.AddComponent<TimeManager>();
                managers.AddComponent<WorkerManager>();
                managers.AddComponent<BuildingManager>();
                managers.AddComponent<LoanManager>();
                managers.AddComponent<EnvironmentalKarmaManager>();
                managers.AddComponent<RivalManager>();
                managers.AddComponent<SaveManager>();
                managers.AddComponent<SettingsManager>();
                managers.AddComponent<GridManager>();
                managers.AddComponent<EcologySpawner>();
                managers.AddComponent<ModernTycoonHUD>();
                managers.AddComponent<TycoonHUD>();

                Undo.RegisterCreatedObjectUndo(managers, "Create [MANAGERS]");
                Debug.Log("[UnifiedSceneBuilder] [MANAGERS] created with all systems");
            }
            else
            {
                // Ensure ModernTycoonHUD is present
                if (managers.GetComponent<ModernTycoonHUD>() == null)
                    managers.AddComponent<ModernTycoonHUD>();
            }
        }

        private static WorldBuildingBuilder FindOrCreateWorldBuilder()
        {
            WorldBuildingBuilder builder = Object.FindAnyObjectByType<WorldBuildingBuilder>();
            if (builder == null)
            {
                GameObject go = new GameObject("WorldBuildingBuilder");
                Undo.RegisterCreatedObjectUndo(go, "Create WorldBuildingBuilder");
                builder = go.AddComponent<WorldBuildingBuilder>();
            }
            return builder;
        }

        private static void SetupPlayer(WorldBuildingBuilder builder)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("animation_Char");

            if (player == null)
            {
                player = new GameObject("animation_Char");
                Undo.RegisterCreatedObjectUndo(player, "Create Player Character");
                player.tag = "Player";
                player.AddComponent<SpriteRenderer>();
                player.AddComponent<OpeningGameplayPlayerController>();
            }
            else
            {
                if (player.GetComponent<OpeningGameplayPlayerController>() == null)
                    player.AddComponent<OpeningGameplayPlayerController>();
            }

            Vector3 spawnPos = builder != null && builder.kebunSpawnPoint != null
                ? builder.kebunSpawnPoint.position
                : new Vector3(0f, -5f, 0f);
            player.transform.position = spawnPos;
        }

        private static void SetupCamera()
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
            }
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            cam.tag = "MainCamera";

            var smoothCam = camGO.GetComponent<SmoothFollowCamera2D>();
            if (smoothCam == null)
            {
                smoothCam = camGO.AddComponent<SmoothFollowCamera2D>();
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                smoothCam.SetTarget(player.transform);
            }
        }
    }
}
#endif
