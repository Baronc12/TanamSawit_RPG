using System;
using UnityEngine;

namespace TanamSawit.Managers
{
    /// <summary>
    /// DEPRECATED SHIM — Use EnvironmentalKarmaManager instead.
    /// This class forwards all calls to EnvironmentalKarmaManager so existing UI code
    /// (ModernTycoonHUD, UIManager, SaveManager) keeps compiling during the transition.
    /// Will be deleted in Phase 1C after grep-verified zero references.
    /// </summary>
    [Obsolete("Use EnvironmentalKarmaManager. Will be deleted in Phase 1C.")]
    [DefaultExecutionOrder(-68)]
    public class EcologyManager : MonoBehaviour
    {
        // Instance kept for compilation compatibility but no singleton logic —
        // the real ecology system lives in EnvironmentalKarmaManager.
        public static EcologyManager Instance { get; private set; }

        public KarmaLevel ActiveKarma =>
            EnvironmentalKarmaManager.Instance != null ? EnvironmentalKarmaManager.Instance.CurrentKarma : KarmaLevel.Aman;

        public string LatestEcologyNews =>
            EnvironmentalKarmaManager.Instance != null ? EnvironmentalKarmaManager.Instance.LastIncidentLog : string.Empty;

        // Forward event subscriptions to EnvironmentalKarmaManager
        public event Action<KarmaLevel, string> OnEcologyEvent
        {
            add
            {
                if (EnvironmentalKarmaManager.Instance != null)
                    EnvironmentalKarmaManager.Instance.OnEcologyEvent += value;
            }
            remove
            {
                if (EnvironmentalKarmaManager.Instance != null)
                    EnvironmentalKarmaManager.Instance.OnEcologyEvent -= value;
            }
        }

        public event Action<string> OnEndingTriggered
        {
            add
            {
                if (EnvironmentalKarmaManager.Instance != null)
                    EnvironmentalKarmaManager.Instance.OnEndingTriggered += value;
            }
            remove
            {
                if (EnvironmentalKarmaManager.Instance != null)
                    EnvironmentalKarmaManager.Instance.OnEndingTriggered -= value;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Forwards to EnvironmentalKarmaManager.ResetEndingStates().
        /// Called by SaveManager on load.
        /// </summary>
        public void ResetEndingStates()
        {
            EnvironmentalKarmaManager.Instance?.ResetEndingStates();
        }
    }
}
