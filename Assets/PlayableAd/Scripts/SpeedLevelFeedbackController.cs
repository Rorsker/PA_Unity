using System;
using UnityEngine;

namespace PlayableAd
{
    public sealed class SpeedLevelFeedbackController : MonoBehaviour
    {
        private PlayerSpeedController speedController;
        private SpeedLevelFeedbackConfig config;
        private SpeedVisualProfile visualProfile;
        private SpeedVisualFeedback visualFeedback;
        private SpeedBarView speedBar;
        private AudioFeedbackController audioFeedback;
        private Action<float, float, float> cameraFeedback;
        private ulong lastSettlementId;

        public void Initialize(PlayerSpeedController controller, SpeedLevelFeedbackConfig feedbackConfig,
            SpeedVisualProfile profile, SpeedVisualFeedback visuals, SpeedBarView bar,
            AudioFeedbackController audio, Action<float, float, float> cameraCallback)
        {
            speedController = controller;
            config = feedbackConfig;
            visualProfile = profile;
            visualFeedback = visuals;
            speedBar = bar;
            audioFeedback = audio;
            cameraFeedback = cameraCallback;
            speedController.SpeedLevelChanged += OnSpeedLevelChanged;
        }

        private void OnSpeedLevelChanged(SpeedLevelChangeData change)
        {
            if (!change.IsLevelUp || change.SettlementId == lastSettlementId) return;
            lastSettlementId = change.SettlementId;
            SpeedLevelFeedbackData level = config.Get(change.NewLevel);
            float multiLevel = Mathf.Min(config.multiLevelMaxMultiplier, 1f + (change.LevelsChanged - 1) * 0.16f);
            float strength = config.levelUpFeedbackStrength * multiLevel;
            Color color = visualProfile.Get(change.NewLevel).primaryColor;
            visualFeedback?.PlayLevelUpBurst(change.NewLevel, level, color, strength,
                config.vfxQualityLevel, config.accessibilityReducedFlash);
            speedBar?.PlayLevelUp(change.NewLevel, color, level, config.levelBadgeEnabled);
            audioFeedback?.PlaySpeedLevelUp(change.NewLevel, level.isMajorLevel, change.NewLevel == speedController.MaxLevel);
            if (config.cameraFeedbackEnabled)
                cameraFeedback?.Invoke(level.cameraZoomStrength * strength,
                    level.cameraImpulseStrength * strength, level.feedbackDuration);
        }

        private void OnDestroy()
        {
            if (speedController != null) speedController.SpeedLevelChanged -= OnSpeedLevelChanged;
        }
    }
}
