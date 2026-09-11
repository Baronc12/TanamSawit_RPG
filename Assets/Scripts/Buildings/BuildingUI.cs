using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TanamSawit.Buildings
{
    /// <summary>
    /// BuildingUI menghubungkan tombol-tombol uGUI Canvas ke fungsi-fungsi BuildingManager.
    /// Anda dapat menghubungkan method public script ini ke event OnClick() Button di Unity Inspector.
    /// </summary>
    public class BuildingUI : MonoBehaviour
    {
        [Header("Tombol-Tombol UI")]
        [SerializeField] private Button buyLandButton;
        [SerializeField] private Button buildFactoryButton;
        [SerializeField] private Button repairFactoryButton;
        [SerializeField] private Button buildFoundationButton;

        [Header("Teks Status (Opsional)")]
        [SerializeField] private TextMeshProUGUI statusText;

        private void Start()
        {
            // Auto-wire listener jika tombol sudah di-assign di Inspector
            if (buyLandButton != null) buyLandButton.onClick.AddListener(OnBuyLandClicked);
            if (buildFactoryButton != null) buildFactoryButton.onClick.AddListener(OnBuildFactoryClicked);
            if (repairFactoryButton != null) repairFactoryButton.onClick.AddListener(OnRepairFactoryClicked);
            if (buildFoundationButton != null) buildFoundationButton.onClick.AddListener(OnBuildFoundationClicked);

            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.OnBuildingEventTriggered += UpdateStatusMessage;
            }
        }

        private void OnDestroy()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.OnBuildingEventTriggered -= UpdateStatusMessage;
            }
        }

        #region Public OnClick Handlers (Bisa dipilih langsung di OnClick Unity)
        public void OnBuyLandClicked()
        {
            BuildingManager.Instance?.BuyLandPlot();
        }

        public void OnBuildFactoryClicked()
        {
            BuildingManager.Instance?.BuildFactory();
        }

        public void OnRepairFactoryClicked()
        {
            BuildingManager.Instance?.RepairFactory();
        }

        public void OnBuildFoundationClicked()
        {
            BuildingManager.Instance?.BuildFoundation();
        }
        #endregion

        private void UpdateStatusMessage(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
