using System;
using System.Collections.Generic;
using TanamSawit.Managers;

namespace TanamSawit.SaveSystem
{
    /// <summary>
    /// Struktur data serializable untuk menyimpan seluruh progres permainan Tanam Sawit.
    /// Menggunakan JSON format yang optimal untuk data terstruktur kompleks.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        #region Metadata Save
        public string saveTimestamp;
        public string saveGameVersion = "1.0.0";
        public int saveVersion = 3;          // 1 = legacy, 2 = multi-slot, 3 = pending kos income
        public string saveSlotName;          // e.g. "Slot 1"
        public float playTimeSeconds;
        #endregion

        #region Waktu & Kalender
        public int currentDay = 1;
        public int currentMonth = 1;
        public int currentYear = 2024;
        public int gameSpeedIndex;           // (int)GameSpeed: 0=Paused,1=Normal,2=Fast,4=SuperFast
        #endregion

        #region Posisi Pemain & Area
        public string currentAreaId;         // "kebun" | "perumahan" | "kota" | "pabrik"
        public float playerPosX;
        public float playerPosY;
        #endregion

        #region Keuangan & Lahan
        public double currentMoney;
        public double savedNetWorth;
        public float currentLandPercentage;
        public double otherAssetsValuation;
        #endregion

        #region Status Hutang & Kredit
        public double bankDebt;
        public double pinjolDebt;
        public double rentenirDebt;
        public int ownedBoardingHouses;
        public double pendingKosIncome;
        #endregion

        #region Pekerja & Hasil Kebun
        public List<Worker> workers = new List<Worker>();
        public float tbsStockTon;
        public float cpoStockTon;
        public bool hasFactory;
        #endregion

        #region Bangunan & Pabrik (BuildingManager-owned state)
        public bool factoryDamaged;
        public int landCount;
        public int maxWorkerCapacity;
        #endregion

        #region Karma Ekologi
        public int karmaLevel;
        public string lastIncidentLog;
        public bool triggered75;
        public bool triggered85;
        public bool triggered90;
        public bool triggered100;
        #endregion

        #region Rival Sepupu & Ending
        public double cousinNetWorth;
        public int finalEnding;
        #endregion
    }
}
