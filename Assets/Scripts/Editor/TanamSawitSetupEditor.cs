#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TanamSawit.Managers;
using TanamSawit.Buildings;
using TanamSawit.SaveSystem;
using TanamSawit.Environment;
using TanamSawit.UI;

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
            managersGo.AddComponent<EcologyManager>();
            managersGo.AddComponent<EnvironmentalKarmaManager>();
            managersGo.AddComponent<RivalManager>();
            managersGo.AddComponent<SaveManager>();
            managersGo.AddComponent<GridManager>();
            managersGo.AddComponent<EcologySpawner>();
            managersGo.AddComponent<TycoonHUD>();

            // Tandai scene berubah agar bisa di-Ctrl+S
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = managersGo;

            EditorUtility.DisplayDialog("Sukses!", 
                "GameObject [MANAGERS] dan seluruh sistem GDD (Ekonomi, Waktu, Pekerja, Karma, Rival, HUD) telah berhasil dipasang!\n\nSekarang Anda cukup tekan tombol PLAY di Unity.", 
                "Mantap!");
        }
    }
}
#endif
