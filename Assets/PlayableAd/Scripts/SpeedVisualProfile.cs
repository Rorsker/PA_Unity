using System;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class SpeedTierVisualData
    {
        [InspectorName("Primary Color（主色）")] public Color primaryColor = Color.white;
        [InspectorName("Secondary Color（辅色）")] public Color secondaryColor = Color.white;
        [Min(0.05f), InspectorName("Trail Length（拖尾长度）")] public float trailLength = 0.5f;
        [Min(0.01f), InspectorName("Trail Width（拖尾宽度）")] public float trailWidth = 0.08f;
        [Min(0f), InspectorName("Trail Brightness（拖尾亮度）")] public float trailBrightness = 0.7f;
        [Min(0f), InspectorName("Particle Emission Rate（粒子发射速率）")] public float particleEmissionRate;
        [Range(0f, 1f), InspectorName("Speed Line Intensity（速度线强度）")] public float speedLineIntensity;
        [Range(0f, 2f), InspectorName("Character Energy Intensity（角色能量强度）")] public float characterEnergyIntensity;
        [InspectorName("UI Color（界面颜色）")] public Color uiColor = Color.white;

        [Header("Layered high-speed feedback（分层高速反馈）")]
        [Min(0f), InspectorName("Airflow Emission（气流发射量）")] public float airflowEmission;
        [Min(0f), InspectorName("Airflow Speed（气流速度）")] public float airflowSpeed;
        [Min(0f), InspectorName("Airflow Length（气流长度）")] public float airflowLength;
        [Range(0f, 1f), InspectorName("Airflow Alpha（气流透明度）")] public float airflowAlpha;
        [Range(0f, 1f), InspectorName("Ground Flow Intensity（地面流动强度）")] public float groundFlowIntensity;
        [Min(0f), InspectorName("Afterimage Rate（残影频率）")] public float afterimageRate;
        [Range(0.05f, 0.5f), InspectorName("Afterimage Lifetime（残影持续时间）")] public float afterimageLifetime = 0.18f;
        [Range(0f, 1f), InspectorName("Pressure Cone Intensity（压力锥强度）")] public float pressureConeIntensity;
        [Range(0f, 1f), InspectorName("Wind Volume（风声音量）")] public float windVolume;
        [Range(0.8f, 1.3f), InspectorName("Wind Pitch（风声音调）")] public float windPitch = 1f;
        [Range(0.8f, 1.5f), InspectorName("Impact Feedback Multiplier（冲击反馈倍率）")] public float impactFeedbackMultiplier = 1f;
        [Range(0f, 1f), InspectorName("Environment Response Strength（环境响应强度）")] public float environmentResponseStrength;

        [Header("Camera And Pose（镜头与姿态）")]
        [Range(0f, 24f), InspectorName("FOV Bonus（视场角加成）")] public float fovBonus;
        [Range(0f, 0.08f), InspectorName("Ambient Shake（环境抖动）")] public float ambientShake;
        [Range(0f, 14f), InspectorName("Charge Lean（蓄力倾斜）")] public float chargeLean;
    }

    [CreateAssetMenu(fileName = "SpeedVisualProfile", menuName = "Playable Ad/Speed Visual Profile")]
    public sealed class SpeedVisualProfile : ScriptableObject
    {
        [Header("Shared Materials（共享材质）")]
        [InspectorName("Trail Material（拖尾材质）")] public Material trailMaterial;
        [InspectorName("Line Material（线条材质）")] public Material lineMaterial;
        [InspectorName("Particle Material（粒子材质）")] public Material particleMaterial;

        [Header("Transitions And Pools（过渡与对象池）")]
        [Range(0.2f, 0.4f), InspectorName("Tier Transition Duration（等级过渡时长）")] public float tierTransitionDuration = 0.3f;
        [Range(4, 24), InspectorName("Pooled Speed Line Count（速度线池数量）")] public int pooledSpeedLineCount = 14;
        [Range(8, 64), InspectorName("Max Aura Particles（最大光环粒子数）")] public int maxAuraParticles = 36;
        [Range(0.05f, 0.5f), InspectorName("Normal Boost Pulse（普通增益脉冲）")] public float normalBoostPulse = 0.2f;
        [Range(0.4f, 1.5f), InspectorName("Level Up Pulse（升级脉冲）")] public float levelUpPulse = 1f;
        [Range(0f, 1f), InspectorName("High Speed Effect Intensity（高速特效强度）")] public float highSpeedEffectIntensity = 0.85f;
        [Range(2, 8), InspectorName("Afterimage Pool Size（残影池大小）")] public int afterimagePoolSize = 6;
        [Range(2, 4), InspectorName("Shock Ring Pool Size（冲击环池大小）")] public int shockRingPoolSize = 3;

        [Header("Data-driven levels 1 through 10（数据驱动等级 1 至 10）")]
        [SerializeField, InspectorName("Tiers（等级配置）")] private SpeedTierVisualData[] tiers = CreateDefaultTiers();

        public int LevelCount => tiers != null ? tiers.Length : 0;

        public SpeedTierVisualData Get(int level)
        {
            EnsureValidTiers();
            return tiers[Mathf.Clamp(level, 1, tiers.Length) - 1];
        }

        public void ResetToDefaults()
        {
            tiers = CreateDefaultTiers();
        }

        private void OnEnable()
        {
            EnsureValidTiers();
        }

        private void OnValidate()
        {
            EnsureValidTiers();
        }

        private void EnsureValidTiers()
        {
            if (tiers == null || tiers.Length != PlayerSpeedSettings.RequiredLevelCount)
                tiers = CreateDefaultTiers();
            for (int i = 0; i < tiers.Length; i++)
                if (tiers[i] == null) tiers[i] = CreateDefaultTiers()[i];
        }

        private static SpeedTierVisualData[] CreateDefaultTiers()
        {
            return new[]
            {
                CreateTier(new Color(0.42f, 0.92f, 0.48f), new Color(0.7f, 1f, 0.75f), 0.45f, 0.06f, 0.5f, 1f, 0.02f, 0.12f),
                CreateTier(new Color(0.12f, 1f, 0.24f), new Color(0.48f, 1f, 0.58f), 0.7f, 0.08f, 0.68f, 3f, 0.08f, 0.22f, 1.5f),
                CreateTier(new Color(0.32f, 0.76f, 1f), new Color(0.62f, 0.9f, 1f), 1f, 0.11f, 0.82f, 6f, 0.15f, 0.35f, 3.5f),
                CreateTier(new Color(0.06f, 0.48f, 1f), new Color(0.28f, 0.8f, 1f), 1.4f, 0.15f, 1f, 10f, 0.25f, 0.55f, 6f, 0.002f, 3f),
                CreateTier(new Color(0.72f, 0.4f, 1f), new Color(0.9f, 0.68f, 1f), 1.9f, 0.2f, 1.15f, 14f, 0.35f, 0.75f, 8f, 0.004f, 4.5f),
                CreateTier(new Color(0.58f, 0.08f, 1f), new Color(0.88f, 0.38f, 1f), 2.4f, 0.25f, 1.35f, 19f, 0.48f, 1f, 10f, 0.007f, 6f),
                CreateTier(new Color(1f, 0.78f, 0.28f), new Color(1f, 0.94f, 0.58f), 3f, 0.3f, 1.5f, 24f, 0.6f, 1.2f, 12.5f, 0.01f, 8f),
                CreateTier(new Color(1f, 0.55f, 0.02f), new Color(1f, 0.86f, 0.24f), 3.6f, 0.35f, 1.65f, 29f, 0.72f, 1.4f, 15f, 0.014f, 10f),
                CreateTier(new Color(1f, 0.1f, 0.05f), new Color(1f, 0.38f, 0.08f), 4.2f, 0.4f, 1.8f, 34f, 0.82f, 1.6f, 18f, 0.018f, 12f),
                CreateTier(new Color(1f, 0.16f, 0.02f), new Color(1f, 0.82f, 0.08f), 5f, 0.48f, 2.1f, 42f, 0.95f, 2f, 21f, 0.024f, 14f)
            };
        }

        private static SpeedTierVisualData CreateTier(Color primary, Color secondary, float length, float width,
            float brightness, float emission, float lines, float energy, float fov = 0f, float shake = 0f, float lean = 0f)
        {
            SpeedTierVisualData tier = new SpeedTierVisualData
            {
                primaryColor = primary,
                secondaryColor = secondary,
                trailLength = length,
                trailWidth = width,
                trailBrightness = brightness,
                particleEmissionRate = emission,
                speedLineIntensity = lines,
                characterEnergyIntensity = energy,
                uiColor = primary,
                fovBonus = fov,
                ambientShake = shake,
                chargeLean = lean
            };
            float highSpeed = Mathf.Clamp01((lines - 0.18f) / 0.77f);
            tier.airflowEmission = highSpeed * Mathf.Lerp(5f, 34f, highSpeed);
            tier.airflowSpeed = Mathf.Lerp(2f, 11f, highSpeed);
            tier.airflowLength = Mathf.Lerp(0.25f, 2.8f, highSpeed);
            tier.airflowAlpha = highSpeed * 0.72f;
            tier.groundFlowIntensity = highSpeed;
            tier.afterimageRate = Mathf.Max(0f, (highSpeed - 0.48f) * 13f);
            tier.afterimageLifetime = Mathf.Lerp(0.12f, 0.24f, highSpeed);
            tier.pressureConeIntensity = Mathf.Clamp01((highSpeed - 0.62f) / 0.38f);
            tier.windVolume = Mathf.SmoothStep(0f, 0.72f, highSpeed);
            tier.windPitch = Mathf.Lerp(0.94f, 1.16f, highSpeed);
            tier.impactFeedbackMultiplier = Mathf.Lerp(0.9f, 1.3f, highSpeed);
            tier.environmentResponseStrength = highSpeed;
            return tier;
        }
    }

    [Serializable]
    public sealed class VisualPerformanceSettings
    {
        [InspectorName("Low Quality Mode（低画质模式）")] public bool lowQualityMode;
        [InspectorName("Enable Secondary Speed Lines（启用次级速度线）")] public bool enableSecondarySpeedLines = true;
        [Range(0.1f, 1f), InspectorName("Low Quality Particle Multiplier（低画质粒子倍率）")] public float lowQualityParticleMultiplier = 0.45f;
        [Range(4, 12), InspectorName("Low Quality Speed Line Count（低画质速度线数量）")] public int lowQualitySpeedLineCount = 6;
        [Range(4, 10), InspectorName("Low Quality Energy Shard Limit（低画质能量碎片上限）")] public int lowQualityEnergyShardLimit = 12;
        [Range(4, 10), InspectorName("Low Quality Wall Chunk Count（低画质墙块数量）")] public int lowQualityWallChunkCount = 6;
    }
}
