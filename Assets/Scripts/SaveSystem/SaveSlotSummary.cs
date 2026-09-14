using System;

namespace TanamSawit.SaveSystem
{
    /// <summary>
    /// Lightweight summary of a save slot for UI display (load menu).
    /// Read from file header without full deserialization where possible.
    /// </summary>
    [Serializable]
    public class SaveSlotSummary
    {
        public int SlotIndex;
        public bool Exists;
        public int SaveVersion;
        public string SlotName;
        public string RealWorldTimestamp;
        public int InGameYear;
        public int InGameMonth;
        public int InGameDay;
        public double NetWorth;
        public float PlayTimeSeconds;
    }
}
