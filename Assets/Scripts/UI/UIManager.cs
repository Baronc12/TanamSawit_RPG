using UnityEngine;
using TMPro;
using TanamSawit.Managers;

namespace TanamSawit.UI
{
    /// <summary>
    /// UIManager menghubungkan seluruh data core (Waktu, Uang, Lahan, Hutang, Notifikasi Event)
    /// ke komponen TextMeshProUGUI di Canvas UI game Anda.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Referensi Komponen TextMeshPro di Canvas")]
        [Tooltip("Menampilkan Hari/Bulan/Tahun dan Kecepatan.")]
        [SerializeField] private TextMeshProUGUI dateText;

        [Tooltip("Menampilkan Saldo Kas Pemain.")]
        [SerializeField] private TextMeshProUGUI moneyText;

        [Tooltip("Menampilkan Total Net Worth Pemain.")]
        [SerializeField] private TextMeshProUGUI netWorthText;

        [Tooltip("Menampilkan Persentase Penguasaan Lahan.")]
        [SerializeField] private TextMeshProUGUI landPercentageText;

        [Tooltip("Menampilkan Total Hutang Aktif (Bank/Pinjol/Madura).")]
        [SerializeField] private TextMeshProUGUI debtText;

        [Tooltip("Menampilkan Banner Notifikasi Berita Harian / Event Ekologi / Cuaca.")]
        [SerializeField] private TextMeshProUGUI notificationText;

        [Header("Panel Game Over / Ending (Opsional)")]
        [SerializeField] private GameObject endingPanel;
        [SerializeField] private TextMeshProUGUI endingResultText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe ke event EconomyManager
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
                EconomyManager.Instance.OnLandPercentageChanged += HandleLandChanged;
                EconomyManager.Instance.OnNetWorthChanged += HandleNetWorthChanged;
            }

            // Subscribe ke event TimeManager
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed += HandleDayPassed;
            }

            // Subscribe ke event EnvironmentalKarmaManager (ecology/ending notifications)
            if (EnvironmentalKarmaManager.Instance != null)
            {
                EnvironmentalKarmaManager.Instance.OnEcologyEvent += HandleEcologyNotification;
                EnvironmentalKarmaManager.Instance.OnEndingTriggered += HandleEndingNotification;
            }

            // Subscribe ke event LoanManager
            if (LoanManager.Instance != null)
            {
                LoanManager.Instance.OnLoanEventTriggered += HandleLoanNotification;
            }

            // Refresh tampilan pertama kali
            RefreshAllUIDisplays();
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
                EconomyManager.Instance.OnLandPercentageChanged -= HandleLandChanged;
                EconomyManager.Instance.OnNetWorthChanged -= HandleNetWorthChanged;
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayPassed -= HandleDayPassed;
            }

            if (EnvironmentalKarmaManager.Instance != null)
            {
                EnvironmentalKarmaManager.Instance.OnEcologyEvent -= HandleEcologyNotification;
                EnvironmentalKarmaManager.Instance.OnEndingTriggered -= HandleEndingNotification;
            }

            if (LoanManager.Instance != null)
            {
                LoanManager.Instance.OnLoanEventTriggered -= HandleLoanNotification;
            }
        }

        public void RefreshAllUIDisplays()
        {
            if (TimeManager.Instance != null)
            {
                UpdateDateDisplay(TimeManager.Instance.GetFormattedDate());
            }

            if (EconomyManager.Instance != null)
            {
                UpdateMoneyDisplay(EconomyManager.Instance.CurrentMoney);
                UpdateNetWorthDisplay(EconomyManager.Instance.GetNetWorth());
                UpdateLandDisplay(EconomyManager.Instance.CurrentLandPercentage);
            }

            UpdateDebtDisplay();
        }

        #region Event Handlers
        private void HandleMoneyChanged(double currentMoney, double delta)
        {
            UpdateMoneyDisplay(currentMoney);
            UpdateDebtDisplay();
        }

        private void HandleNetWorthChanged(double netWorth)
        {
            UpdateNetWorthDisplay(netWorth);
        }

        private void HandleLandChanged(float currentPercentage, float delta)
        {
            UpdateLandDisplay(currentPercentage);
        }

        private void HandleDayPassed(int day, int month, int year)
        {
            if (TimeManager.Instance != null)
            {
                UpdateDateDisplay(TimeManager.Instance.GetFormattedDate());
            }
            UpdateDebtDisplay();
        }

        private void HandleEcologyNotification(KarmaLevel level, string message)
        {
            SetNotificationMessage(message);
        }

        private void HandleEndingNotification(string endingMessage)
        {
            SetNotificationMessage(endingMessage);

            if (endingPanel != null)
            {
                endingPanel.SetActive(true);
            }
            if (endingResultText != null)
            {
                endingResultText.text = endingMessage;
            }
        }

        private void HandleLoanNotification(string message)
        {
            SetNotificationMessage(message);
            UpdateDebtDisplay();
        }
        #endregion

        #region Teks Update Helpers
        private void UpdateDateDisplay(string dateString)
        {
            if (dateText != null)
                dateText.text = $"📅 {dateString}";
        }

        private void UpdateMoneyDisplay(double amount)
        {
            if (moneyText != null)
                moneyText.text = $"Kas: {EconomyManager.FormatCurrency(amount)}";
        }

        private void UpdateNetWorthDisplay(double netWorth)
        {
            if (netWorthText != null)
                netWorthText.text = $"Net Worth: {EconomyManager.FormatCurrency(netWorth)}";
        }

        private void UpdateLandDisplay(float percentage)
        {
            if (landPercentageText != null)
                landPercentageText.text = $"Lahan: {percentage:F1}%";
        }

        private void UpdateDebtDisplay()
        {
            if (debtText != null)
            {
                double total = LoanManager.Instance != null ? LoanManager.Instance.TotalDebt : 0;
                string debtColor = total > 0 ? "#FF4444" : "#FFFFFF";
                debtText.text = $"<color={debtColor}>Hutang: {EconomyManager.FormatCurrency(total)}</color>";
            }
        }

        public void SetNotificationMessage(string message)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
            }
        }
        #endregion
    }
}
