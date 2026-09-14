using System;
using UnityEngine;

namespace TanamSawit.Managers
{
    /// <summary>
    /// Pilihan kecepatan waktu dalam simulasi tycoon.
    /// </summary>
    public enum GameSpeed
    {
        Paused = 0,
        Normal = 1,
        Fast = 2,
        SuperFast = 4
    }

    /// <summary>
    /// TimeManager mengatur perputaran waktu permainan (Hari, Bulan, Tahun),
    /// kecepatan simulasi (1x, 2x, 4x, Pause), serta memicu event berkala (gaji bulanan, panen, dsb).
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class TimeManager : MonoBehaviour
    {
        #region Singleton
        public static TimeManager Instance { get; private set; }

        [Header("Pengaturan Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        #endregion

        #region Konfigurasi Waktu
        [Header("Konfigurasi Kalender")]
        [Tooltip("Berapa detik real-time untuk menyelesaikan 1 hari di dalam game pada kecepatan Normal (1x).")]
        [SerializeField] private float secondsPerDay = 3.0f;

        [Tooltip("Jumlah hari dalam satu bulan game.")]
        [SerializeField] private int daysPerMonth = 30;

        [Tooltip("Jumlah bulan dalam satu tahun game.")]
        [SerializeField] private int monthsPerYear = 12;

        [Header("Tanggal Mulai")]
        [SerializeField] private int startDay = 1;
        [SerializeField] private int startMonth = 1;
        [SerializeField] private int startYear = 2024;
        #endregion

        #region Status Waktu Saat Ini
        [Header("Waktu Saat Ini (Live)")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int currentMonth = 1;
        [SerializeField] private int currentYear = 2024;

        public int CurrentDay => currentDay;
        public int CurrentMonth => currentMonth;
        public int CurrentYear => currentYear;

        [Tooltip("Jam awal saat hari dimulai (0-24). 6 = 06:00 pagi.")]
        [SerializeField] private float startHour = 6f;

        [SerializeField] private GameSpeed currentGameSpeed = GameSpeed.Normal;
        public GameSpeed CurrentGameSpeed => currentGameSpeed;

        private float dayTimer = 0f;
        private GameSpeed speedBeforePause = GameSpeed.Normal;
        private int lastHourInt = -1;
        #endregion

        #region Jam dalam Sehari
        /// <summary>
        /// Jam saat ini dalam sehari (0.0 - 24.0). Dihitung dari dayTimer.
        /// Saat hari berganti (dayTimer reset), jam kembali ke startHour.
        /// </summary>
        public float HourOfDay => Mathf.Repeat(startHour + 24f * dayTimer / secondsPerDay, 24f);

        /// <summary>Dipanggil setiap kali jam berubah (integer hour berganti).</summary>
        public event Action<float> OnHourChanged;
        #endregion

        #region Nama Bulan
        private static readonly string[] MonthNames = new string[]
        {
            "Januari", "Februari", "Maret", "April", "Mei", "Juni",
            "Juli", "Agustus", "September", "Oktober", "November", "Desember"
        };
        #endregion

        #region Events (Observer Pattern)
        /// <summary>
        /// Dipanggil setiap kali 1 hari game berganti. Parameter: (day, month, year).
        /// </summary>
        public event Action<int, int, int> OnDayPassed;

        /// <summary>
        /// Dipanggil setiap kali 1 bulan game berganti. Sangat cocok untuk: potong gaji karyawan, tagihan operasional kebun.
        /// Parameter: (month, year).
        /// </summary>
        public event Action<int, int> OnMonthPassed;

        /// <summary>
        /// Dipanggil setiap kali 1 tahun game berganti. Cocok untuk: evaluasi laba rugi tahunan, pajak lahan.
        /// Parameter: (year).
        /// </summary>
        public event Action<int> OnYearPassed;

        /// <summary>
        /// Dipanggil saat kecepatan waktu berubah. Parameter: (GameSpeed).
        /// </summary>
        public event Action<GameSpeed> OnSpeedChanged;
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[TimeManager] Ditemukan instance duplikat pada GameObject {gameObject.name}. Menghancurkan objek ini.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            // Set tanggal awal
            currentDay = Mathf.Max(1, startDay);
            currentMonth = Mathf.Clamp(startMonth, 1, monthsPerYear);
            currentYear = Mathf.Max(1, startYear);
        }

        private void Start()
        {
            // Sinkronisasi awal ke listener
            OnDayPassed?.Invoke(currentDay, currentMonth, currentYear);
            OnSpeedChanged?.Invoke(currentGameSpeed);
        }

        private void Update()
        {
            // Jangan jalankan waktu jika simulasi di-pause atau GameManager dalam state selain Playing
            if (currentGameSpeed == GameSpeed.Paused) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            // Hitung pertambahan timer berdasarkan delta waktu dan multiplier kecepatan
            float speedMultiplier = (float)currentGameSpeed;
            dayTimer += Time.deltaTime * speedMultiplier;

            if (dayTimer >= secondsPerDay)
            {
                dayTimer -= secondsPerDay;
                AdvanceDay();
            }

            // Cek perubahan jam (integer hour)
            int currentHourInt = Mathf.FloorToInt(HourOfDay);
            if (currentHourInt != lastHourInt)
            {
                lastHourInt = currentHourInt;
                OnHourChanged?.Invoke(HourOfDay);
            }
        }

        /// <summary>
        /// Memajukan kalender sebanyak 1 hari dan memicu event terkait.
        /// </summary>
        public void AdvanceDay()
        {
            currentDay++;

            if (currentDay > daysPerMonth)
            {
                currentDay = 1;
                AdvanceMonth();
            }

            OnDayPassed?.Invoke(currentDay, currentMonth, currentYear);
        }

        /// <summary>
        /// Memajukan kalender sebanyak 1 bulan.
        /// </summary>
        private void AdvanceMonth()
        {
            currentMonth++;

            if (currentMonth > monthsPerYear)
            {
                currentMonth = 1;
                AdvanceYear();
            }

            Debug.Log($"[TimeManager] Masuk ke Bulan Baru: {GetMonthName()} {currentYear}");
            OnMonthPassed?.Invoke(currentMonth, currentYear);
        }

        /// <summary>
        /// Memajukan kalender sebanyak 1 tahun.
        /// </summary>
        private void AdvanceYear()
        {
            currentYear++;
            Debug.Log($"[TimeManager] Tahun Baru Dimulai: Tahun {currentYear}!");
            OnYearPassed?.Invoke(currentYear);
        }

        /// <summary>
        /// Mengatur tanggal kalender (biasanya dipanggil oleh SaveManager saat Load).
        /// </summary>
        public void SetDate(int day, int month, int year)
        {
            currentDay = Mathf.Clamp(day, 1, daysPerMonth);
            currentMonth = Mathf.Clamp(month, 1, monthsPerYear);
            currentYear = Mathf.Max(1, year);
            dayTimer = 0f;
            lastHourInt = -1; // paksa OnHourChanged terpicu setelah load

            OnDayPassed?.Invoke(currentDay, currentMonth, currentYear);
        }

        #region Pengendalian Kecepatan Waktu
        /// <summary>
        /// Mengatur kecepatan simulasi (Paused, Normal 1x, Fast 2x, SuperFast 4x).
        /// </summary>
        public void SetSpeed(GameSpeed newSpeed)
        {
            if (currentGameSpeed == newSpeed) return;

            currentGameSpeed = newSpeed;
            Debug.Log($"[TimeManager] Kecepatan game diubah ke: {currentGameSpeed} ({(int)currentGameSpeed}x)");

            OnSpeedChanged?.Invoke(currentGameSpeed);
        }

        /// <summary>
        /// Menghentikan waktu simulasi.
        /// </summary>
        public void Pause()
        {
            if (currentGameSpeed != GameSpeed.Paused)
            {
                speedBeforePause = currentGameSpeed;
                SetSpeed(GameSpeed.Paused);
            }
        }

        /// <summary>
        /// Melanjutkan waktu simulasi ke kecepatan sebelumnya.
        /// </summary>
        public void Resume()
        {
            if (currentGameSpeed == GameSpeed.Paused)
            {
                SetSpeed(speedBeforePause == GameSpeed.Paused ? GameSpeed.Normal : speedBeforePause);
            }
        }

        /// <summary>
        /// Toggle antara Pause dan Resume.
        /// </summary>
        public void TogglePause()
        {
            if (currentGameSpeed == GameSpeed.Paused)
                Resume();
            else
                Pause();
        }
        #endregion

        #region Helper Format Tanggal
        /// <summary>
        /// Mendapatkan nama bulan saat ini dalam Bahasa Indonesia.
        /// </summary>
        public string GetMonthName()
        {
            int index = Mathf.Clamp(currentMonth - 1, 0, MonthNames.Length - 1);
            return MonthNames[index];
        }

        /// <summary>
        /// Mendapatkan string tanggal lengkap (contoh: "12 Maret 2024").
        /// </summary>
        public string GetFormattedDate()
        {
            return $"{currentDay} {GetMonthName()} {currentYear}";
        }

        /// <summary>
        /// Mendapatkan persentase progres hari yang sedang berjalan (0.0f - 1.0f).
        /// Berguna untuk animasi matahari / jam putar UI.
        /// </summary>
        public float GetDayProgress()
        {
            return Mathf.Clamp01(dayTimer / secondsPerDay);
        }

        /// <summary>
        /// Mendapatkan string jam format "HH:MM" (contoh: "06:30", "14:45").
        /// </summary>
        public string GetFormattedTime()
        {
            float h = HourOfDay;
            int hours = Mathf.FloorToInt(h);
            int minutes = Mathf.FloorToInt((h - hours) * 60f);
            return $"{hours:D2}:{minutes:D2}";
        }

        /// <summary>
        /// Mengatur jam dalam sehari (dipanggil oleh SaveManager saat Load).
        /// Menyesuaikan dayTimer agar sesuai dengan jam yang ditentukan.
        /// </summary>
        public void SetHour(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            dayTimer = (hour - startHour) / 24f * secondsPerDay;
            if (dayTimer < 0f) dayTimer += secondsPerDay;
            lastHourInt = Mathf.FloorToInt(HourOfDay);
        }
        #endregion
    }
}
