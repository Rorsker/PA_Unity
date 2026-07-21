using System;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class SpeedLevelFeedbackData
    {
        [InspectorName("Is Major Level（是否为关键等级）")] public bool isMajorLevel;
        [Range(4, 48), InspectorName("Burst Particle Count（爆发粒子数量）")] public int burstParticleCount = 10;
        [Range(0.5f, 3f), InspectorName("Shockwave Scale（冲击波缩放）")] public float shockwaveScale = 1.15f;
        [Range(0.2f, 0.35f), InspectorName("Shockwave Duration（冲击波时长）")] public float shockwaveDuration = 0.25f;
        [Range(0.8f, 1.8f), InspectorName("Level Badge Scale（等级徽章缩放）")] public float levelBadgeScale = 1f;
        [Range(0.6f, 1f), InspectorName("Level Badge Duration（等级徽章时长）")] public float levelBadgeDuration = 0.75f;
        [Range(0f, 7f), InspectorName("Camera Zoom Strength（镜头缩放强度）")] public float cameraZoomStrength = 2.2f;
        [Range(0f, 0.5f), InspectorName("Camera Impulse Strength（镜头冲击强度）")] public float cameraImpulseStrength = 0.08f;
        [Range(1f, 1.8f), InspectorName("Trail Boost Multiplier（拖尾增益倍率）")] public float trailBoostMultiplier = 1.18f;
        [Range(0.2f, 0.7f), InspectorName("Feedback Duration（反馈时长）")] public float feedbackDuration = 0.34f;
    }

    [CreateAssetMenu(fileName = "SpeedLevelFeedbackConfig", menuName = "Playable Ad/Speed Level Feedback Config")]
    public sealed class SpeedLevelFeedbackConfig : ScriptableObject
    {
        [Header("Global（全局设置）")]
        [Range(0.5f, 1.5f), InspectorName("Level Up Feedback Strength（升级反馈强度）")] public float levelUpFeedbackStrength = 1f;
        [Range(1f, 1.8f), InspectorName("Multi Level Max Multiplier（多级最大倍率）")] public float multiLevelMaxMultiplier = 1.45f;
        [InspectorName("Camera Feedback Enabled（启用镜头反馈）")] public bool cameraFeedbackEnabled = true;
        [InspectorName("Level Badge Enabled（启用等级徽章）")] public bool levelBadgeEnabled = true;
        [InspectorName("Accessibility Reduced Flash（无障碍降低闪光）")] public bool accessibilityReducedFlash;
        [Range(0, 2), InspectorName("VFX Quality Level（特效质量等级）")] public int vfxQualityLevel = 2;

        [SerializeField, InspectorName("Levels（等级反馈配置）")] private SpeedLevelFeedbackData[] levels = CreateDefaults();

        public SpeedLevelFeedbackData Get(int level)
        {
            EnsureValid();
            return levels[Mathf.Clamp(level, 1, levels.Length) - 1];
        }

        private void OnEnable() => EnsureValid();
        private void OnValidate() => EnsureValid();

        private void EnsureValid()
        {
            if (levels == null || levels.Length != PlayerSpeedSettings.RequiredLevelCount)
                levels = CreateDefaults();
            SpeedLevelFeedbackData[] defaults = null;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null) continue;
                if (defaults == null) defaults = CreateDefaults();
                levels[i] = defaults[i];
            }
        }

        private static SpeedLevelFeedbackData[] CreateDefaults()
        {
            SpeedLevelFeedbackData[] data = new SpeedLevelFeedbackData[PlayerSpeedSettings.RequiredLevelCount];
            for (int i = 0; i < data.Length; i++)
            {
                int level = i + 1;
                float t = i / 9f;
                bool major = level == 4 || level == 7 || level == 9 || level == 10;
                float majorScale = major ? 1.22f : 1f;
                data[i] = new SpeedLevelFeedbackData
                {
                    isMajorLevel = major,
                    burstParticleCount = Mathf.RoundToInt(Mathf.Lerp(7f, 30f, t) * majorScale),
                    shockwaveScale = Mathf.Lerp(0.9f, 2.15f, t) * majorScale,
                    shockwaveDuration = Mathf.Lerp(0.22f, 0.32f, t),
                    levelBadgeScale = Mathf.Lerp(0.92f, 1.35f, t) * (major ? 1.08f : 1f),
                    levelBadgeDuration = major ? 0.88f : 0.7f,
                    cameraZoomStrength = Mathf.Lerp(1.5f, 5.8f, t) * majorScale,
                    cameraImpulseStrength = Mathf.Lerp(0.04f, 0.18f, t) * majorScale,
                    trailBoostMultiplier = Mathf.Lerp(1.1f, 1.42f, t),
                    feedbackDuration = Mathf.Lerp(0.28f, 0.5f, t)
                };
            }
            return data;
        }
    }
}
