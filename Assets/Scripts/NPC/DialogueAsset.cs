using System;
using System.Collections.Generic;
using UnityEngine;

namespace TanamSawit.NPC
{
    /// <summary>
    /// ScriptableObject yang berisi urutan node dialog sebuah NPC.
    /// Setiap node punya teks, pilihan (choices), dan opsional auto-advance.
    /// Buat asset via: Create > Tanam Sawit > Dialogue Asset.
    ///
    /// actionId adalah string contract (mis. "open_bank_modal", "recruit_worker",
    /// "end_dialogue") yang dipicu sebagai event, menjaga data bebas dari engine refs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Tanam Sawit/Dialogue Asset", order = 11)]
    public class DialogueAsset : ScriptableObject
    {
        [Tooltip("NPC yang berbicara (untuk nama dan warna aksen).")]
        public NPCIdentity speaker;

        [Tooltip("Daftar node dialog dalam urutan.")]
        public List<DialogueNode> nodes = new List<DialogueNode>();

        [Serializable]
        public class DialogueNode
        {
            [Tooltip("Teks dialog NPC.")]
            [TextArea(2, 5)] public string text = "";

            [Tooltip("Pilihan yang muncul. Kosong = lanjut ke node berikutnya.")]
            public List<DialogueChoice> choices = new List<DialogueChoice>();

            [Tooltip("Index node berikutnya otomatis (-1 = tidak auto-advance).")]
            public int autoNext = -1;

            [Tooltip("Jika true, teks ini diisi dinamis pada runtime (mis. level karma).")]
            public bool isDynamicText = false;
        }

        [Serializable]
        public class DialogueChoice
        {
            [Tooltip("Teks tombol pilihan.")]
            public string text = "";

            [Tooltip("Index node tujuan (-1 = lanjut sekuensial).")]
            public int nextNodeIndex = -1;

            [Tooltip("ID aksi yang dipicu (mis. open_bank_modal, end_dialogue).")]
            public string actionId = "";
        }

        /// <summary>Factory untuk membuat instance runtime (tanpa asset file).</summary>
        public static DialogueAsset CreateRuntime(NPCIdentity speaker)
        {
            var asset = CreateInstance<DialogueAsset>();
            asset.speaker = speaker;
            return asset;
        }
    }
}
