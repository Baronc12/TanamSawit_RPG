using UnityEngine;

namespace TanamSawit.NPC
{
    /// <summary>
    /// ScriptableObject yang mendefinisikan identitas sebuah NPC.
    /// Buat asset via: Create > Tanam Sawit > NPC Identity.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNPCIdentity", menuName = "Tanam Sawit/NPC Identity", order = 10)]
    public class NPCIdentity : ScriptableObject
    {
        [Header("Identitas")]
        [Tooltip("ID unik NPC (mis. mandor, pegawai_bank, rentenir).")]
        public string npcId = "npc";

        [Tooltip("Nama tampilan NPC.")]
        public string displayName = "NPC";

        [Tooltip("Peran/jabatan NPC (mis. Mandor Kebun, Pegawai Bank).")]
        [TextArea] public string role = "";

        [Header("Visual")]
        [Tooltip("Potret NPC untuk ditampilkan di panel dialog (opsional).")]
        public Sprite portrait;

        [Tooltip("Warna aksen NPC untuk nama dan border panel dialog.")]
        public Color accentColor = new Color(0.4f, 0.8f, 0.6f);

        /// <summary>Factory untuk membuat instance runtime (tanpa asset file).</summary>
        public static NPCIdentity CreateRuntime(string id, string name, string role, Color accent)
        {
            var identity = CreateInstance<NPCIdentity>();
            identity.npcId = id;
            identity.displayName = name;
            identity.role = role;
            identity.accentColor = accent;
            return identity;
        }
    }
}
