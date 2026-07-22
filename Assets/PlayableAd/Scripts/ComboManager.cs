using System;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class GameplayComboSettings
    {
        [InspectorName("Enabled（启用连击）")]
        public bool enabled = true;

        [Header("Milestones（里程碑）")]
        [Min(1), InspectorName("First Milestone（第一里程碑）")]
        public int firstMilestone = 10;
        [Min(1), InspectorName("Second Milestone（第二里程碑）")]
        public int secondMilestone = 20;
        [Min(1), InspectorName("Third Milestone（第三里程碑）")]
        public int thirdMilestone = 50;
        [Min(1), InspectorName("Fourth Milestone（第四里程碑）")]
        public int fourthMilestone = 100;

        [InspectorName("Presentation（连击表现）")]
        public ComboPresentationSettings presentation = new ComboPresentationSettings();

        public bool IsMilestone(int combo)
        {
            if (combo <= 0) return false;
            return combo == Mathf.Max(1, firstMilestone)
                || combo == Mathf.Max(1, secondMilestone)
                || combo == Mathf.Max(1, thirdMilestone)
                || combo == Mathf.Max(1, fourthMilestone);
        }

        public void UpgradeLegacyDefaults()
        {
            if (firstMilestone == 5 && secondMilestone == 10
                && thirdMilestone == 20 && fourthMilestone == 50)
            {
                firstMilestone = 10;
                secondMilestone = 20;
                thirdMilestone = 50;
                fourthMilestone = 100;
            }

            if (presentation == null) presentation = new ComboPresentationSettings();
            presentation.UpgradeLegacyDefaults();
        }
    }

    public readonly struct ComboChangedEvent
    {
        public readonly int PreviousCombo;
        public readonly int CurrentCombo;
        public readonly bool WasReset;

        public ComboChangedEvent(int previousCombo, int currentCombo, bool wasReset)
        {
            PreviousCombo = previousCombo;
            CurrentCombo = currentCombo;
            WasReset = wasReset;
        }
    }

    public sealed class ComboManager : MonoBehaviour
    {
        public static ComboManager Instance { get; private set; }

        public event Action<ComboChangedEvent> ComboChanged;
        public event Action<int> OnComboMilestone;

        private GameplayComboSettings settings;
        private int currentCombo;

        public bool IsEnabled => settings != null && settings.enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[Combo] Multiple ComboManager instances were created. The first instance remains authoritative.", this);
                return;
            }

            Instance = this;
        }

        public void Initialize(GameplayComboSettings configuredSettings)
        {
            int previousCombo = currentCombo;
            settings = configuredSettings ?? new GameplayComboSettings();
            currentCombo = 0;
            if (previousCombo > 0)
                ComboChanged?.Invoke(new ComboChangedEvent(previousCombo, currentCombo, true));
        }

        public void AddCombo()
        {
            if (!IsEnabled || currentCombo == int.MaxValue) return;

            int previousCombo = currentCombo;
            currentCombo++;
            ComboChanged?.Invoke(new ComboChangedEvent(previousCombo, currentCombo, false));
            if (settings.IsMilestone(currentCombo))
                OnComboMilestone?.Invoke(currentCombo);
        }

        public void ResetCombo()
        {
            if (currentCombo == 0) return;

            int previousCombo = currentCombo;
            currentCombo = 0;
            ComboChanged?.Invoke(new ComboChangedEvent(previousCombo, currentCombo, true));
        }

        public int GetCombo()
        {
            return currentCombo;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
