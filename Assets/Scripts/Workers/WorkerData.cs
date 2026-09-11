using UnityEngine;

namespace TanamSawit.Workers
{
    /// <summary>
    /// ScriptableObject untuk mendefinisikan tipe dan base stats pekerja perkebunan sawit.
    /// Anda dapat membuat asset ini via klik kanan di Project: Create > Tanam Sawit > Worker Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWorkerData", menuName = "Tanam Sawit/Worker Data", order = 1)]
    public class WorkerData : ScriptableObject
    {
        [Header("Identitas Pekerja")]
        [Tooltip("Nama atau peran pekerja (contoh: Buruh Egrek Sawit, Mandor Kebun, Operator Pabrik).")]
        public string workerTitle = "Pemanen Sawit";

        [Tooltip("Biaya perekrutan awal pekerja.")]
        public double hireCost = 2_500_000;

        [Header("Base RPG Stats")]
        [Tooltip("Stamina awal: Menentukan berapa siklus kerja sebelum butuh istirahat.")]
        [Range(20f, 200f)]
        public float baseStamina = 100f;

        [Tooltip("Experience: Menentukan kecepatan kerja dan bonus hasil panen TBS.")]
        [Range(1, 100)]
        public int baseExperience = 10;

        [Tooltip("Intelligence: Menentukan efisiensi kerja mesin pabrik.")]
        [Range(1, 100)]
        public int baseIntelligence = 10;

        [Header("Visual (Opsional)")]
        [Tooltip("Ikon 2D pekerja untuk UI atau display.")]
        public Sprite workerIcon;

        [Tooltip("Warna penanda unik jika menggunakan Primitive (Kapsul/Kotak).")]
        public Color workerColor = Color.blue;
    }
}
