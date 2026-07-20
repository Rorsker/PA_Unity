using System;
using UnityEngine;

namespace PlayableAd
{
    public enum SpeedChangeReason
    {
        Initialization,
        TutorialElixir,
        NormalImpact,
        MainRunDecay,
        ObstaclePenalty,
        Debug,
        InitialSetup,
        LowLevelCollisionReward,
        HighLevelCollisionPenalty,
        PotionPickup,
        SpecialReward,
        BossEvent,
        DebugCommand
    }

    [Serializable]
    public sealed class PlayerSpeedSettings
    {
        public const int RequiredLevelCount = 10;

        [Header("Continuous Speed（连续速度）")]
        [Min(0f), InspectorName("Minimum Speed（最低速度）")] public float minimumSpeed = 1f;
        [Min(0f), InspectorName("Maximum Speed（最高速度）")] public float maximumSpeed = 10f;
        [Tooltip("Continuous speed at which each configured level begins.")]
        [InspectorName("Level Start Speeds（各等级起始速度）")] public float[] levelStartSpeeds = { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 9.5f };
        [Tooltip("World-space forward movement speed for every configured level.")]
        [InspectorName("Forward Speeds（各等级前进速度）")] public float[] forwardSpeeds = { 6f, 7f, 8.5f, 10f, 12f, 14f, 16.5f, 19f, 22f, 26f };

        [Header("Forward Speed Response（前进速度响应）")]
        [Min(1f), InspectorName("Base Acceleration（基础加速度）")] public float baseAcceleration = 22f;
        [Min(1f), InspectorName("Special Upgrade Acceleration（特殊升级加速度）")] public float specialUpgradeAcceleration = 16f;
        [Min(1f), InspectorName("Natural Deceleration（自然减速度）")] public float naturalDeceleration = 14f;
        [Min(1f), InspectorName("Penalty Deceleration（惩罚减速度）")] public float penaltyDeceleration = 32f;

        [Header("P0 Rules（P0 规则）")]
        [Range(1, RequiredLevelCount), InspectorName("Starting Level（起始等级）")] public int startingLevel = 1;
        [Range(1, RequiredLevelCount), InspectorName("Tutorial Elixir Target Level（教学药剂目标等级）")] public int tutorialElixirTargetLevel = 4;
        [Tooltip("Legacy compatibility only. Current course keeps this disabled so speed changes only through gameplay events.")]
        [InspectorName("Automatic Speed Decay Enabled（启用自动速度衰减）")] public bool automaticSpeedDecayEnabled = false;
        [Min(0f), InspectorName("Main Run Decay Per Second（主流程每秒衰减）")] public float mainRunDecayPerSecond = 0.1f;
        [InspectorName("Log Speed Changes（记录速度变化）")] public bool logSpeedChanges = false;
        [Min(0f), InspectorName("Level One Soldier Boost（一级士兵增益）")] public float levelOneSoldierBoost = 0.12f;
        [Min(0f), InspectorName("Normal Impact Boost（普通冲击增益）")] public float normalImpactBoost = 0.18f;
        [Min(0f), InspectorName("Normal Impact Soft Cap（普通冲击软上限）")] public float normalImpactSoftCap = 6.5f;
        [Min(0f), InspectorName("Tutorial Impact Soft Cap（教学冲击软上限）")] public float tutorialImpactSoftCap = 4f;
        [Range(1, RequiredLevelCount), InspectorName("Boss Victory Level（Boss 胜利等级）")] public int bossVictoryLevel = 10;

        public int LevelCount => levelStartSpeeds != null ? levelStartSpeeds.Length : RequiredLevelCount;
    }

    public readonly struct SpeedChangedEvent
    {
        public readonly float OldValue;
        public readonly float NewValue;
        public readonly int OldLevel;
        public readonly int NewLevel;
        public readonly SpeedChangeReason Reason;
        public readonly UnityEngine.Object Source;

        public SpeedChangedEvent(float oldValue, float newValue, int oldLevel, int newLevel, SpeedChangeReason reason, UnityEngine.Object source)
        {
            OldValue = oldValue;
            NewValue = newValue;
            OldLevel = oldLevel;
            NewLevel = newLevel;
            Reason = reason;
            Source = source;
        }
    }

    public readonly struct SpeedLevelChangeData
    {
        public readonly ulong SettlementId;
        public readonly float OldSpeed;
        public readonly float NewSpeed;
        public readonly int OldLevel;
        public readonly int NewLevel;
        public readonly int LevelsChanged;
        public readonly SpeedChangeReason Reason;
        public readonly UnityEngine.Object Source;
        public readonly bool IsMajorLevel;
        public readonly Vector3 WorldPosition;

        public SpeedLevelChangeData(ulong settlementId, float oldSpeed, float newSpeed, int oldLevel, int newLevel,
            SpeedChangeReason reason, UnityEngine.Object source, Vector3 worldPosition)
        {
            SettlementId = settlementId;
            OldSpeed = oldSpeed;
            NewSpeed = newSpeed;
            OldLevel = oldLevel;
            NewLevel = newLevel;
            LevelsChanged = Mathf.Abs(newLevel - oldLevel);
            Reason = reason;
            Source = source;
            IsMajorLevel = newLevel == 4 || newLevel == 7 || newLevel == 9 || newLevel == 10;
            WorldPosition = worldPosition;
        }

        public bool IsLevelUp => NewLevel > OldLevel;
    }

    public sealed class PlayerSpeedController : MonoBehaviour
    {
        [SerializeField, InspectorName("Settings（速度设置）")] private PlayerSpeedSettings settings = new PlayerSpeedSettings();
        [SerializeField, Range(1f, 10f), InspectorName("Current Speed（当前速度）")] private float currentSpeed = 1f;

        public event Action<SpeedChangedEvent> SpeedChanged;
        public event Action<SpeedLevelChangeData> SpeedLevelChanged;

        public float CurrentSpeed => currentSpeed;
        public PlayerSpeedSettings Settings => settings;
        public int LevelCount => settings != null ? settings.LevelCount : PlayerSpeedSettings.RequiredLevelCount;
        public int MaxLevel => LevelCount;
        public SpeedChangeReason LastSpeedChangeReason { get; private set; } = SpeedChangeReason.InitialSetup;
        public float LastSpeedChangeTime { get; private set; }
        public UnityEngine.Object LastSpeedChangeSource { get; private set; }
        public bool AutomaticSpeedDecayEnabled => settings != null && settings.automaticSpeedDecayEnabled;
        public ulong SettlementSequence { get; private set; }

        public void Initialize(PlayerSpeedSettings speedSettings)
        {
            settings = speedSettings ?? new PlayerSpeedSettings();
            ValidateSettings();
            float initial = GetLevelStartSpeed(settings.startingLevel);
            float oldValue = currentSpeed;
            int oldLevel = GetLevelForSpeed(oldValue);
            currentSpeed = initial;
            Publish(oldValue, oldLevel, SpeedChangeReason.InitialSetup, this, !Mathf.Approximately(oldValue, currentSpeed));
        }

        public void SetSpeed(float value)
        {
            SetSpeed(value, SpeedChangeReason.Debug);
        }

        public void SetSpeed(float value, SpeedChangeReason reason)
        {
            SetSpeed(value, reason, null);
        }

        public void SetSpeed(float value, SpeedChangeReason reason, UnityEngine.Object source)
        {
            SetSpeedInternal(value, reason, source, false);
        }

        private void SetSpeedInternal(float value, SpeedChangeReason reason, UnityEngine.Object source, bool forceNotification)
        {
            float oldValue = currentSpeed;
            int oldLevel = GetLevelForSpeed(oldValue);
            currentSpeed = Mathf.Clamp(value, settings.minimumSpeed, settings.maximumSpeed);
            bool valueChanged = !Mathf.Approximately(oldValue, currentSpeed);
            if (forceNotification || valueChanged)
                Publish(oldValue, oldLevel, reason, source, valueChanged);
        }

        public void SetLevel(int level, SpeedChangeReason reason)
        {
            SetLevel(level, reason, null);
        }

        public void SetLevel(int level, SpeedChangeReason reason, UnityEngine.Object source)
        {
            bool forceNotification = reason == SpeedChangeReason.TutorialElixir || reason == SpeedChangeReason.PotionPickup
                || reason == SpeedChangeReason.Initialization || reason == SpeedChangeReason.InitialSetup;
            SetSpeedInternal(GetLevelStartSpeed(level), reason, source, forceNotification);
        }

        public void AddSpeed(float amount, float softCap)
        {
            AddSpeed(amount, softCap, SpeedChangeReason.NormalImpact);
        }

        public void AddSpeed(float amount, float softCap, SpeedChangeReason reason)
        {
            AddSpeed(amount, softCap, reason, null);
        }

        public void AddSpeed(float amount, float softCap, SpeedChangeReason reason, UnityEngine.Object source)
        {
            float cap = Mathf.Clamp(softCap, settings.minimumSpeed, settings.maximumSpeed);
            float target = currentSpeed >= cap
                ? currentSpeed
                : Mathf.Min(currentSpeed + Mathf.Max(0f, amount), cap);
            SetSpeedInternal(target, reason, source, true);
        }

        public void DropOneLevel()
        {
            DropOneLevel(SpeedChangeReason.ObstaclePenalty);
        }

        public void DropOneLevel(SpeedChangeReason reason)
        {
            DropOneLevel(reason, null);
        }

        public void DropOneLevel(SpeedChangeReason reason, UnityEngine.Object source)
        {
            int targetLevel = Mathf.Max(1, GetCurrentLevel() - 1);
            SetSpeed(GetLevelStartSpeed(targetLevel), reason, source);
        }

        public int GetCurrentLevel()
        {
            return GetLevelForSpeed(currentSpeed);
        }

        public float GetNormalizedProgressInLevel()
        {
            int level = GetCurrentLevel();
            float lower = GetLevelStartSpeed(level);
            float upper = level >= LevelCount ? settings.maximumSpeed : GetLevelStartSpeed(level + 1);
            if (upper <= lower + Mathf.Epsilon) return 1f;
            return Mathf.Clamp01(Mathf.InverseLerp(lower, upper, currentSpeed));
        }

        public float GetNormalizedOverallProgress()
        {
            ValidateSettings();
            return Mathf.InverseLerp(settings.minimumSpeed, settings.maximumSpeed, currentSpeed);
        }

        public float GetNormalizedLevelStart(int level)
        {
            ValidateSettings();
            return Mathf.InverseLerp(settings.minimumSpeed, settings.maximumSpeed, GetLevelStartSpeed(level));
        }

        public float GetLevelStartSpeed(int level)
        {
            ValidateSettings();
            return settings.levelStartSpeeds[Mathf.Clamp(level, 1, LevelCount) - 1];
        }

        public float GetForwardSpeed()
        {
            ValidateSettings();
            int level = GetCurrentLevel();
            if (level >= LevelCount) return settings.forwardSpeeds[LevelCount - 1];
            float progress = GetNormalizedProgressInLevel();
            return Mathf.Lerp(settings.forwardSpeeds[level - 1], settings.forwardSpeeds[level], progress);
        }

        public void ApplyMainRunDecay(float deltaTime)
        {
            if (!AutomaticSpeedDecayEnabled || deltaTime <= 0f || settings.mainRunDecayPerSecond <= 0f) return;
            SetSpeed(currentSpeed - settings.mainRunDecayPerSecond * deltaTime, SpeedChangeReason.MainRunDecay, this);
        }

        private int GetLevelForSpeed(float value)
        {
            ValidateSettings();
            int level = 1;
            for (int i = 1; i < LevelCount; i++)
            {
                if (value < settings.levelStartSpeeds[i]) break;
                level = i + 1;
            }
            return level;
        }

        private void Publish(float oldValue, int oldLevel, SpeedChangeReason reason, UnityEngine.Object source, bool valueChanged)
        {
            if (valueChanged || reason == SpeedChangeReason.InitialSetup || reason == SpeedChangeReason.Initialization)
            {
                LastSpeedChangeReason = reason;
                LastSpeedChangeTime = Time.unscaledTime;
                LastSpeedChangeSource = source;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (settings.logSpeedChanges && valueChanged)
                {
                    string sourceName = source != null ? source.name : "Unknown";
                    Debug.LogFormat("[Speed] {0:F3} -> {1:F3} | {2} | {3}", oldValue, currentSpeed, reason, sourceName);
                }
#endif
            }
            int newLevel = GetCurrentLevel();
            SpeedChanged?.Invoke(new SpeedChangedEvent(oldValue, currentSpeed, oldLevel, newLevel, reason, source));
            if (newLevel != oldLevel)
            {
                SettlementSequence++;
                SpeedLevelChanged?.Invoke(new SpeedLevelChangeData(SettlementSequence, oldValue, currentSpeed,
                    oldLevel, newLevel, reason, source, transform.position));
            }
        }

        private void ValidateSettings()
        {
            if (settings == null) settings = new PlayerSpeedSettings();
            float configuredMinimum = settings.minimumSpeed;
            float configuredMaximum = settings.maximumSpeed;
            settings.minimumSpeed = Mathf.Min(configuredMinimum, configuredMaximum);
            settings.maximumSpeed = Mathf.Max(configuredMinimum, configuredMaximum);
            if (settings.maximumSpeed - settings.minimumSpeed < 0.04f)
                settings.maximumSpeed = settings.minimumSpeed + 0.04f;
            if (!IsValidAscendingArray(settings.levelStartSpeeds, PlayerSpeedSettings.RequiredLevelCount))
                settings.levelStartSpeeds = BuildEvenLevelStarts(settings.minimumSpeed, settings.maximumSpeed, PlayerSpeedSettings.RequiredLevelCount);
            if (settings.forwardSpeeds == null || settings.forwardSpeeds.Length != settings.LevelCount)
                settings.forwardSpeeds = BuildDefaultForwardSpeeds(settings.LevelCount);
            for (int i = 0; i < settings.LevelCount; i++)
                settings.levelStartSpeeds[i] = Mathf.Clamp(settings.levelStartSpeeds[i], settings.minimumSpeed, settings.maximumSpeed);
            if (!IsValidAscendingArray(settings.levelStartSpeeds, PlayerSpeedSettings.RequiredLevelCount))
                settings.levelStartSpeeds = BuildEvenLevelStarts(settings.minimumSpeed, settings.maximumSpeed, PlayerSpeedSettings.RequiredLevelCount);
            settings.startingLevel = Mathf.Clamp(settings.startingLevel, 1, settings.LevelCount);
            settings.tutorialElixirTargetLevel = Mathf.Clamp(settings.tutorialElixirTargetLevel, 1, settings.LevelCount);
            settings.bossVictoryLevel = Mathf.Clamp(settings.bossVictoryLevel, 1, settings.LevelCount);
        }

        private static float[] BuildEvenLevelStarts(float minimum, float maximum, int count)
        {
            float[] values = new float[Mathf.Max(2, count)];
            // Level starts divide the range into bands; the final start must stay
            // below maximum or the highest level would exist at only one value.
            for (int i = 0; i < values.Length; i++)
                values[i] = Mathf.Lerp(minimum, maximum, i / (float)values.Length);
            return values;
        }

        private static float[] BuildDefaultForwardSpeeds(int count)
        {
            float[] values = new float[Mathf.Max(2, count)];
            for (int i = 0; i < values.Length; i++)
            {
                float t = i / (float)(values.Length - 1);
                values[i] = Mathf.Lerp(6f, 26f, t * t * 0.58f + t * 0.42f);
            }
            return values;
        }

        private static bool IsValidAscendingArray(float[] values, int requiredCount)
        {
            if (values == null || values.Length != requiredCount) return false;
            for (int i = 1; i < values.Length; i++)
                if (values[i] <= values[i - 1]) return false;
            return true;
        }
    }
}
