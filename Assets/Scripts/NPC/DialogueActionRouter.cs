using System;
using TanamSawit.Environment;
using TanamSawit.Managers;
using TanamSawit.Buildings;

namespace TanamSawit.NPC
{
    /// <summary>
    /// Memetakan actionId string dari DialogueChoice ke panggilan game action.
    /// Memastikan data dialog tetap bebas dari engine references.
    /// </summary>
    public static class DialogueActionRouter
    {
        /// <summary>
        /// Memproses actionId. Dipanggil oleh DialogueUI saat choice dipilih.
        /// </summary>
        public static void Route(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return;

            switch (actionId)
            {
                // Buka modal fasilitas dari dialog NPC
                case "open_bank_modal":
                    InteractableFacility.InvokeInteraction(FacilityType.Bank, "Bank Konvensional");
                    break;
                case "open_pinjol_modal":
                    InteractableFacility.InvokeInteraction(FacilityType.Pinjol, "Kantor Pinjol");
                    break;
                case "open_rentenir_modal":
                    InteractableFacility.InvokeInteraction(FacilityType.Rentenir, "Warung Rentenir Madura");
                    break;
                case "open_pabrik_modal":
                    InteractableFacility.InvokeInteraction(FacilityType.PabrikCPO, "Pabrik Pengolahan CPO");
                    break;
                case "open_yayasan_modal":
                    InteractableFacility.InvokeInteraction(FacilityType.YayasanCSR, "Yayasan Pendidikan CSR");
                    break;
                case "open_mess_modal":
                    InteractableFacility.InvokeInteraction(FacilityType.MessPekerja, "Mess Pekerja");
                    break;
                case "open_papan_lahan_modal":
                    InteractableFacility.InvokeInteraction(FacilityType.PapanLahan, "Papan Lahan & Plot Grid");
                    break;

                // Aksi langsung tanpa modal
                case "recruit_worker":
                    InteractableFacility.InvokeInteraction(FacilityType.MessPekerja, "Mess Pekerja");
                    break;
                case "harvest_tbs":
                    WorkerManager.Instance?.HarvestTBS();
                    break;
                case "sell_tbs":
                    WorkerManager.Instance?.SellTBS();
                    break;

                // end_dialogue ditangani oleh DialogueRunner
                case "end_dialogue":
                    break;

                default:
                    UnityEngine.Debug.LogWarning($"[DialogueActionRouter] actionId tidak dikenal: '{actionId}'");
                    break;
            }
        }
    }
}
