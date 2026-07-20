using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace PlayableAd
{
    public enum HapticStrength { Light, Medium, Heavy }

    [Serializable]
    public sealed class AudioFeedbackSettings
    {
        [Header("Global（全局设置）")]
        [InspectorName("Audio Enabled（启用音频）")] public bool audioEnabled = true;
        [InspectorName("Haptics Enabled（启用触觉反馈）")] public bool hapticsEnabled = true;
        [InspectorName("Use Procedural Placeholders（使用程序化占位音频）")] public bool useProceduralPlaceholders = true;
        [Range(0f, 1f), InspectorName("Master Volume（主音量）")] public float masterVolume = 0.85f;
        [InspectorName("SFX Mixer Group（音效混音组）")] public AudioMixerGroup sfxMixerGroup;
        [Range(0f, 1f), InspectorName("Collision Spatial Blend（碰撞空间混合）")] public float collisionSpatialBlend = 0.78f;
        [Min(0.1f), InspectorName("Collision Min Distance（碰撞最小距离）")] public float collisionMinDistance = 2f;
        [Min(1f), InspectorName("Collision Max Distance（碰撞最大距离）")] public float collisionMaxDistance = 24f;

        [Header("Movement Loops（移动循环音效）")]
        [InspectorName("Footsteps Loop（脚步循环）")] public AudioClip footstepsLoop;
        [InspectorName("Running Wind Loop（奔跑风声循环）")] public AudioClip runningWindLoop;
        [InspectorName("Speed Energy Loop（速度能量循环）")] public AudioClip speedEnergyLoop;

        [Header("Elixir And Tier Feedback（药剂与等级反馈）")]
        [InspectorName("Elixir Pickup（药剂拾取）")] public AudioClip elixirPickup;
        [InspectorName("Elixir Absorb（药剂吸收）")] public AudioClip elixirAbsorb;
        [InspectorName("Tier Upgrade（等级升级）")] public AudioClip tierUpgrade;
        [InspectorName("Tier Upgrade Major（关键等级升级）")] public AudioClip tierUpgradeMajor;
        [InspectorName("Tier Upgrade Max（最高等级升级）")] public AudioClip tierUpgradeMax;
        [InspectorName("Tier Drop（等级下降）")] public AudioClip tierDrop;
        [InspectorName("Impact Penalty（冲击惩罚）")] public AudioClip impactPenalty;

        [Header("Collision Outcome Events（碰撞结果事件）")]
        [InspectorName("Speed Gain Impact（速度增加冲击）")] public AudioClip speedGainImpact;
        [InspectorName("Neutral Impact（中性冲击）")] public AudioClip neutralImpact;
        [InspectorName("Speed Loss Impact（速度降低冲击）")] public AudioClip speedLossImpact;
        [InspectorName("Danger Preview（危险预览）")] public AudioClip dangerPreview;

        [Header("Soldier Hit Layers（士兵命中音层）")]
        [InspectorName("Soldier Impact Variants（士兵冲击变体）")] public AudioClip[] soldierImpactVariants = Array.Empty<AudioClip>();
        [InspectorName("Impact Transient（冲击瞬态）")] public AudioClip impactTransient;
        [InspectorName("Armor Contact（装甲接触）")] public AudioClip armorContact;
        [InspectorName("Body Weight（身体重量感）")] public AudioClip bodyWeight;
        [InspectorName("Armor Break（装甲破碎）")] public AudioClip armorBreak;
        [InspectorName("High Speed Whoosh（高速呼啸）")] public AudioClip highSpeedWhoosh;
        [InspectorName("Soldier Fly Away（士兵飞离）")] public AudioClip soldierFlyAway;
        [InspectorName("Energy Return（能量回收）")] public AudioClip energyReturn;

        [Header("Wall Break Layers（墙体破碎音层）")]
        [InspectorName("Wall Low Impact（墙体低强度冲击）")] public AudioClip wallLowImpact;
        [InspectorName("Wall Stone Debris（墙石碎屑）")] public AudioClip wallStoneDebris;
        [InspectorName("Wall Dust（墙尘）")] public AudioClip wallDust;
        [InspectorName("Wall Impact Tail（墙体冲击尾音）")] public AudioClip wallImpactTail;

        [Header("Boss Layers（Boss 音层）")]
        [InspectorName("Boss Contact（Boss 接触）")] public AudioClip bossContact;
        [InspectorName("Boss Struggle Loop（Boss 对抗循环）")] public AudioClip bossStruggleLoop;
        [InspectorName("Boss Finish Impact（Boss 终结冲击）")] public AudioClip bossFinishImpact;
        [InspectorName("Cage Break（牢笼破碎）")] public AudioClip cageBreak;

        [Header("Mix Hierarchy（混音层级）")]
        [Range(0f, 1f), InspectorName("Movement Volume（移动音量）")] public float movementVolume = 0.22f;
        [Range(0f, 1f), InspectorName("Normal Impact Volume（普通冲击音量）")] public float normalImpactVolume = 0.5f;
        [Range(0f, 1f), InspectorName("Upgrade Volume（升级音量）")] public float upgradeVolume = 0.68f;
        [Range(0f, 1f), InspectorName("Wall Volume（墙体音量）")] public float wallVolume = 0.82f;
        [Range(0f, 1f), InspectorName("Boss Volume（Boss 音量）")] public float bossVolume = 1f;
        [Range(0f, 0.6f), InspectorName("Priority Duck Amount（高优先级压低量）")] public float priorityDuckAmount = 0.32f;

        [Header("Voice Limits（并发音源限制）")]
        [Range(2, 8), InspectorName("Action Voice Count（动作音源数量）")] public int actionVoiceCount = 5;
        [Range(0.03f, 0.2f), InspectorName("Normal Impact Min Interval（普通冲击最小间隔）")] public float normalImpactMinInterval = 0.055f;
        [Range(0.04f, 0.3f), InspectorName("Energy Return Min Interval（能量回收最小间隔）")] public float energyReturnMinInterval = 0.08f;
        [Range(0.25f, 1.5f), InspectorName("Danger Preview Global Cooldown（危险预览全局冷却）")] public float dangerPreviewGlobalCooldown = 0.65f;
        [Range(2f, 24f), InspectorName("Movement Smoothing（移动平滑度）")] public float movementSmoothing = 12f;
    }

    public sealed class AudioFeedbackController : MonoBehaviour
    {
        private sealed class Voice
        {
            public AudioSource source;
            public double busyUntil;
            public int priority;
        }

        private AudioFeedbackSettings settings;
        private AudioSource footsteps;
        private AudioSource wind;
        private AudioSource speedEnergy;
        private AudioSource bossLoop;
        private AudioSource prioritySource;
        private Voice[] actionVoices = Array.Empty<Voice>();
        private readonly List<AudioClip> ownedProceduralClips = new List<AudioClip>();
        private float baseMovementVolume;
        private float lastSoldierImpactTime = float.NegativeInfinity;
        private float lastEnergyReturnTime = float.NegativeInfinity;
        private float lastHapticTime = float.NegativeInfinity;
        private float lastDangerPreviewTime = float.NegativeInfinity;

        public Action<HapticStrength> ExternalHapticHandler;
        public int ActionVoiceCount => actionVoices.Length;
        public int ProceduralClipCount => ownedProceduralClips.Count;
        public float CurrentWindVolume => wind != null ? wind.volume : 0f;
        public int LastCollisionLayerCount { get; private set; }
        public bool LastCollisionWasSpatial { get; private set; }
        public Vector3 LastCollisionPosition { get; private set; }

        public void Initialize(AudioFeedbackSettings feedbackSettings)
        {
            if (settings != null) return;
            settings = feedbackSettings ?? new AudioFeedbackSettings();
            if (settings.useProceduralPlaceholders)
                ProceduralAudioLibrary.FillMissing(settings, ownedProceduralClips);

            footsteps = CreateSource("Audio_Footsteps", settings.footstepsLoop, true);
            wind = CreateSource("Audio_Wind", settings.runningWindLoop, true);
            speedEnergy = CreateSource("Audio_SpeedEnergy", settings.speedEnergyLoop, true);
            bossLoop = CreateSource("Audio_BossStruggle", settings.bossStruggleLoop, true);
            prioritySource = CreateSource("Audio_Priority", null, false);

            int voiceCount = Mathf.Clamp(settings.actionVoiceCount, 2, 8);
            actionVoices = new Voice[voiceCount];
            for (int i = 0; i < actionVoices.Length; i++)
            {
                actionVoices[i] = new Voice
                {
                    source = CreateSource("Audio_Action_" + (i + 1), null, false)
                };
            }

            baseMovementVolume = settings.movementVolume * settings.masterVolume;
            StartLoopIfAssigned(footsteps);
            StartLoopIfAssigned(wind);
            StartLoopIfAssigned(speedEnergy);
        }

        public void UpdateSpeed(int tier, float continuousNormalizedSpeed, float actualNormalizedSpeed, bool movementActive,
            float configuredWindVolume = -1f, float configuredWindPitch = 1f)
        {
            if (settings == null) return;
            float duck = prioritySource != null && prioritySource.isPlaying ? settings.priorityDuckAmount : 0f;
            float movement = settings.audioEnabled && movementActive ? baseMovementVolume * (1f - duck) : 0f;
            float response = 1f - Mathf.Exp(-settings.movementSmoothing * Time.unscaledDeltaTime);
            SetLoop(footsteps, movement * Mathf.Lerp(0.42f, 0.9f, actualNormalizedSpeed), Mathf.Lerp(0.9f, 1.14f, actualNormalizedSpeed), response);
            float windStrength = configuredWindVolume >= 0f
                ? Mathf.Clamp01(configuredWindVolume)
                : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 1f, actualNormalizedSpeed));
            float windPitch = configuredWindVolume >= 0f ? configuredWindPitch : Mathf.Lerp(0.92f, 1.16f, actualNormalizedSpeed);
            SetLoop(wind, movement * windStrength, windPitch, response);
            float energyInput = Mathf.Min(actualNormalizedSpeed, continuousNormalizedSpeed + 0.12f);
            float energyStrength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 1f, energyInput));
            SetLoop(speedEnergy, movement * energyStrength * 0.8f, Mathf.Lerp(0.92f, 1.18f, actualNormalizedSpeed), response);
        }

        public void HandleSpeedChanged(SpeedChangedEvent change)
        {
            if (settings == null || change.Reason == SpeedChangeReason.Initialization || change.Reason == SpeedChangeReason.InitialSetup
                || change.Reason == SpeedChangeReason.Debug || change.Reason == SpeedChangeReason.DebugCommand)
                return;

            if (change.NewLevel < change.OldLevel)
            {
                PlayTierDrop(change.Reason == SpeedChangeReason.ObstaclePenalty || change.Reason == SpeedChangeReason.HighLevelCollisionPenalty);
            }
        }

        public void PlayElixirContact()
        {
            PlayVoice(settings.elixirPickup, settings.upgradeVolume * 0.78f, 1f, 3);
            PlayVoice(settings.elixirAbsorb, settings.upgradeVolume * 0.46f, 1.06f, 2);
        }

        public void PlayTierUpgrade(int targetLevel)
        {
            float progress = Mathf.InverseLerp(1f, PlayerSpeedSettings.RequiredLevelCount, targetLevel);
            PlayVoice(settings.tierUpgrade, settings.upgradeVolume, Mathf.Lerp(0.94f, 1.12f, progress), 4);
            TriggerHaptic(HapticStrength.Medium);
        }

        public void PlaySpeedLevelUp(int targetLevel, bool major, bool maximum)
        {
            float progress = Mathf.InverseLerp(1f, PlayerSpeedSettings.RequiredLevelCount, targetLevel);
            AudioClip clip = maximum && settings.tierUpgradeMax != null
                ? settings.tierUpgradeMax
                : major && settings.tierUpgradeMajor != null ? settings.tierUpgradeMajor : settings.tierUpgrade;
            float volume = settings.upgradeVolume * (maximum ? 1.08f : major ? 0.94f : 0.8f);
            PlayVoice(clip, volume, Mathf.Lerp(0.96f, 1.08f, progress), maximum ? 5 : 4);
            TriggerHaptic(maximum || major ? HapticStrength.Medium : HapticStrength.Light);
        }

        public void PlayCollisionOutcome(CollisionOutcome outcome, int comboIndex, float pitchStep,
            float normalizedActualSpeed, Vector3 worldPosition)
        {
            if (Time.unscaledTime - lastSoldierImpactTime < settings.normalImpactMinInterval) return;
            lastSoldierImpactTime = Time.unscaledTime;
            LastCollisionLayerCount = 0;
            LastCollisionWasSpatial = true;
            LastCollisionPosition = worldPosition;

            float speed = Mathf.Clamp01(normalizedActualSpeed);
            float comboCompression = Mathf.Lerp(1f, 0.76f, Mathf.Clamp01(comboIndex / 5f));
            float pitch = 1f + Mathf.Clamp(comboIndex, 0, 3) * Mathf.Min(0.012f, pitchStep * 0.25f)
                + UnityEngine.Random.Range(-0.03f, 0.03f);
            AudioClip variant = settings.impactTransient;
            if (settings.soldierImpactVariants != null && settings.soldierImpactVariants.Length > 0)
            {
                AudioClip selected = settings.soldierImpactVariants[comboIndex % settings.soldierImpactVariants.Length];
                if (selected != null) variant = selected;
            }

            float baseVolume = settings.normalImpactVolume * comboCompression;
            if (PlayVoiceAt(variant, baseVolume * Mathf.Lerp(0.76f, 0.92f, speed), pitch, 3, worldPosition))
                LastCollisionLayerCount++;
            if (PlayVoiceAt(settings.armorContact, baseVolume * Mathf.Lerp(0.34f, 0.48f, speed), pitch * 0.985f, 2, worldPosition))
                LastCollisionLayerCount++;

            if (speed >= 0.3f || outcome == CollisionOutcome.SpeedLoss)
            {
                if (PlayVoiceAt(settings.bodyWeight, baseVolume * Mathf.Lerp(0.2f, 0.42f, speed), 0.94f, 2, worldPosition))
                    LastCollisionLayerCount++;
            }

            if (LastCollisionLayerCount < 4 && speed >= 0.68f && (comboIndex & 1) != 0)
            {
                if (PlayVoiceAt(settings.highSpeedWhoosh, baseVolume * Mathf.InverseLerp(0.68f, 1f, speed) * 0.3f, 1.02f, 1, worldPosition))
                    LastCollisionLayerCount++;
            }
            else if (LastCollisionLayerCount < 4 && (speed >= 0.48f || outcome == CollisionOutcome.SpeedGain))
            {
                if (PlayVoiceAt(settings.armorBreak, baseVolume * Mathf.Lerp(0.2f, 0.38f, speed), pitch * 1.015f, 2, worldPosition))
                    LastCollisionLayerCount++;
            }

            AudioClip outcomeAccent = outcome == CollisionOutcome.SpeedGain ? settings.speedGainImpact
                : outcome == CollisionOutcome.SpeedLoss ? settings.speedLossImpact : settings.neutralImpact;
            float accentVolume = outcome == CollisionOutcome.SpeedLoss ? 0.4f : outcome == CollisionOutcome.SpeedGain ? 0.22f : 0.16f;
            if (LastCollisionLayerCount < 4 && PlayVoiceAt(outcomeAccent, baseVolume * accentVolume,
                    outcome == CollisionOutcome.SpeedLoss ? 0.88f : 1.02f,
                    outcome == CollisionOutcome.SpeedLoss ? 3 : 1, worldPosition))
                LastCollisionLayerCount++;
            TriggerHaptic(outcome == CollisionOutcome.SpeedLoss ? HapticStrength.Medium : HapticStrength.Light);
        }

        public bool PlayDangerPreview()
        {
            if (Time.unscaledTime - lastDangerPreviewTime < settings.dangerPreviewGlobalCooldown) return false;
            lastDangerPreviewTime = Time.unscaledTime;
            PlayVoice(settings.dangerPreview, settings.normalImpactVolume * 0.28f, 0.92f, 2);
            return true;
        }

        public void PlayTierDrop(bool collisionPenalty)
        {
            PlayVoice(settings.tierDrop, settings.upgradeVolume * (collisionPenalty ? 0.78f : 0.48f), collisionPenalty ? 0.9f : 1.04f, 4);
            if (collisionPenalty)
            {
                PlayVoice(settings.impactPenalty, settings.normalImpactVolume * 0.62f, 0.86f, 3);
                TriggerHaptic(HapticStrength.Medium);
            }
        }

        public void PlaySoldierImpact(int comboIndex, float pitchStep)
        {
            if (Time.unscaledTime - lastSoldierImpactTime < settings.normalImpactMinInterval) return;
            lastSoldierImpactTime = Time.unscaledTime;

            float pitch = 1f + Mathf.Clamp(comboIndex, 0, 4) * pitchStep + UnityEngine.Random.Range(-0.018f, 0.018f);
            AudioClip impact = null;
            if (settings.soldierImpactVariants != null && settings.soldierImpactVariants.Length > 0)
                impact = settings.soldierImpactVariants[comboIndex % settings.soldierImpactVariants.Length];
            float comboVolume = settings.normalImpactVolume * Mathf.Lerp(1f, 0.82f, Mathf.Clamp01(comboIndex / 4f));
            PlayVoice(impact, comboVolume, pitch, 2);
            if ((comboIndex & 1) == 0)
                PlayVoice(settings.armorContact, comboVolume * 0.34f, pitch * 0.96f, 1);
            if (comboIndex == 0)
                PlayVoice(settings.soldierFlyAway, comboVolume * 0.24f, pitch * 1.05f, 1);
            TriggerHaptic(HapticStrength.Light);
        }

        public void PlayEnergyReturn()
        {
            if (Time.unscaledTime - lastEnergyReturnTime < settings.energyReturnMinInterval) return;
            lastEnergyReturnTime = Time.unscaledTime;
            PlayVoice(settings.energyReturn, settings.normalImpactVolume * 0.38f, UnityEngine.Random.Range(0.98f, 1.08f), 1);
        }

        public void PlayWallBreak()
        {
            PlayPriority(settings.wallLowImpact, settings.wallVolume, 0.95f);
            PlayVoice(settings.wallStoneDebris, settings.wallVolume * 0.62f, 1f, 4);
            PlayVoice(settings.wallDust, settings.wallVolume * 0.3f, 0.9f, 3);
            PlayVoice(settings.wallImpactTail, settings.wallVolume * 0.46f, 1f, 4);
            TriggerHaptic(HapticStrength.Heavy);
        }

        public void PlayBossContact()
        {
            PlayPriority(settings.bossContact, settings.bossVolume * 0.82f, 1f);
            TriggerHaptic(HapticStrength.Medium);
        }

        public void BeginBossStruggle()
        {
            if (!CanPlay(settings.bossStruggleLoop) || bossLoop.isPlaying) return;
            bossLoop.clip = settings.bossStruggleLoop;
            bossLoop.volume = settings.bossVolume * settings.masterVolume * 0.5f;
            bossLoop.Play();
        }

        public void StopBossStruggle()
        {
            if (bossLoop != null && bossLoop.isPlaying) bossLoop.Stop();
        }

        public void PlayBossFinish()
        {
            StopBossStruggle();
            PlayPriority(settings.bossFinishImpact, settings.bossVolume, 1f);
            PlayVoice(settings.cageBreak, settings.bossVolume * 0.68f, 1f, 5);
            TriggerHaptic(HapticStrength.Heavy);
        }

        public void PlayBossFailure()
        {
            StopBossStruggle();
            PlayTierDrop(true);
        }

        public void TriggerHaptic(HapticStrength strength)
        {
            if (settings == null || !settings.hapticsEnabled) return;
            float minimumGap = strength == HapticStrength.Light ? 0.08f : 0.16f;
            if (Time.unscaledTime - lastHapticTime < minimumGap) return;
            lastHapticTime = Time.unscaledTime;
            if (ExternalHapticHandler != null)
            {
                ExternalHapticHandler(strength);
                return;
            }
#if UNITY_ANDROID || UNITY_IOS
            if (strength != HapticStrength.Light) Handheld.Vibrate();
#endif
        }

        private AudioSource CreateSource(string objectName, AudioClip clip, bool loop)
        {
            GameObject sourceObject = new GameObject(objectName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.clip = clip;
            source.volume = loop ? 0f : 1f;
            source.priority = loop ? 160 : 128;
            if (!loop && settings != null) source.outputAudioMixerGroup = settings.sfxMixerGroup;
            return source;
        }

        private void StartLoopIfAssigned(AudioSource source)
        {
            if (settings.audioEnabled && source != null && source.clip != null) source.Play();
        }

        private static void SetLoop(AudioSource source, float volume, float pitch, float response)
        {
            if (source == null) return;
            source.volume = Mathf.Lerp(source.volume, volume, response);
            source.pitch = Mathf.Lerp(source.pitch, pitch, response);
        }

        private bool CanPlay(AudioClip clip)
        {
            return settings != null && settings.audioEnabled && clip != null;
        }

        private bool PlayVoice(AudioClip clip, float volume, float pitch, int priority)
        {
            return PlayVoiceInternal(clip, volume, pitch, priority, false, Vector3.zero);
        }

        private bool PlayVoiceAt(AudioClip clip, float volume, float pitch, int priority, Vector3 worldPosition)
        {
            return PlayVoiceInternal(clip, volume, pitch, priority, true, worldPosition);
        }

        private bool PlayVoiceInternal(AudioClip clip, float volume, float pitch, int priority,
            bool spatial, Vector3 worldPosition)
        {
            if (!CanPlay(clip) || actionVoices.Length == 0) return false;
            double now = AudioSettings.dspTime;
            int selected = -1;
            int lowestPriority = int.MaxValue;
            for (int i = 0; i < actionVoices.Length; i++)
            {
                Voice voice = actionVoices[i];
                if (!voice.source.isPlaying || voice.busyUntil <= now)
                {
                    selected = i;
                    break;
                }

                if (voice.priority < lowestPriority)
                {
                    lowestPriority = voice.priority;
                    selected = i;
                }
            }

            if (selected < 0 || (actionVoices[selected].source.isPlaying && priority < actionVoices[selected].priority)) return false;
            Voice target = actionVoices[selected];
            target.source.Stop();
            target.source.clip = clip;
            target.source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            target.source.volume = Mathf.Clamp01(volume * settings.masterVolume);
            target.source.spatialBlend = spatial ? settings.collisionSpatialBlend : 0f;
            target.source.minDistance = settings.collisionMinDistance;
            target.source.maxDistance = Mathf.Max(settings.collisionMinDistance, settings.collisionMaxDistance);
            if (spatial) target.source.transform.position = worldPosition;
            else target.source.transform.localPosition = Vector3.zero;
            target.priority = priority;
            target.busyUntil = now + clip.length / Mathf.Max(0.5f, target.source.pitch);
            target.source.Play();
            return true;
        }

        private void PlayPriority(AudioClip clip, float volume, float pitch)
        {
            if (!CanPlay(clip)) return;
            prioritySource.Stop();
            prioritySource.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            prioritySource.clip = clip;
            prioritySource.volume = Mathf.Clamp01(volume * settings.masterVolume);
            prioritySource.Play();
        }

        private void OnDisable()
        {
            StopAllSources();
        }

        private void OnEnable()
        {
            if (settings == null) return;
            StartLoopIfAssigned(footsteps);
            StartLoopIfAssigned(wind);
            StartLoopIfAssigned(speedEnergy);
        }

        private void OnDestroy()
        {
            StopAllSources();
            for (int i = 0; i < ownedProceduralClips.Count; i++)
            {
                if (ownedProceduralClips[i] == null) continue;
                if (Application.isPlaying) Destroy(ownedProceduralClips[i]);
                else DestroyImmediate(ownedProceduralClips[i]);
            }
            ownedProceduralClips.Clear();
        }

        private void StopAllSources()
        {
            if (footsteps != null) footsteps.Stop();
            if (wind != null) wind.Stop();
            if (speedEnergy != null) speedEnergy.Stop();
            if (bossLoop != null) bossLoop.Stop();
            if (prioritySource != null) prioritySource.Stop();
            for (int i = 0; i < actionVoices.Length; i++)
                if (actionVoices[i].source != null) actionVoices[i].source.Stop();
        }
    }
}
