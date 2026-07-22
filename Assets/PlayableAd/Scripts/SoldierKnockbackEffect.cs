using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class SoldierKnockbackSettings
    {
        [Header("Trajectory（飞行轨迹）")]
        [Range(4, 24), InspectorName("Max Active Soldiers（最大同时飞行士兵数）")] public int maxActiveSoldiers = 16;
        [Min(0.1f), InspectorName("Flight Lifetime（飞行持续时间）")] public float flightLifetime = 1.6f;
        [Min(0f), InspectorName("Minimum Forward Speed（最低前向速度）")] public float minimumForwardSpeed = 8f;
        [Min(0f), InspectorName("Maximum Authored Forward Speed（预设最高前向速度）")] public float maximumAuthoredForwardSpeed = 16f;
        [Min(1f), InspectorName("Impact Speed Multiplier（撞击速度倍率）")] public float impactSpeedMultiplier = 1.25f;
        [Min(0f), InspectorName("High Speed Multiplier Bonus（高速倍率加成）")] public float highSpeedMultiplierBonus = 0.85f;
        [Min(0f), InspectorName("Lateral Range Per Speed Level（每级横向随机范围）")] public float lateralRangePerSpeedLevel = 1f;
        [Min(0f), InspectorName("Upward Speed（向上速度）")] public float upwardSpeed = 6.2f;
        [Min(0f), InspectorName("Gravity Multiplier（重力倍率）")] public float gravityMultiplier = 1.15f;
        [Min(0f), InspectorName("Below Road Recycle Depth（路面下回收深度）")] public float belowRoadRecycleDepth = 3f;

        [Header("Whole Model Rotation（整模型旋转）")]
        [Min(0f), InspectorName("Minimum Rotation Speed（最低旋转速度，度/秒）")] public float minimumRotationSpeed = 240f;
        [Min(0f), InspectorName("Maximum Rotation Speed（最高旋转速度，度/秒）")] public float maximumRotationSpeed = 720f;

        [Header("Impact Flash（撞击闪烁）")]
        [InspectorName("Enable Impact Flash（启用撞击闪烁）")] public bool impactFlashEnabled = true;
        [Min(0.02f), InspectorName("Flash Duration（闪烁持续时间）")] public float impactFlashDuration = 0.28f;
        [Min(1), InspectorName("Red White Cycles（红白切换循环数）")] public int impactFlashCycles = 3;
        [ColorUsage(false, true), InspectorName("Flash Red（闪烁红色）")]
        public Color impactFlashRed = new Color(0.64f, 0.018f, 0.14f, 1f);
        [ColorUsage(false, true), InspectorName("Flash White（闪烁白色）")]
        public Color impactFlashWhite = new Color(0.92f, 0.92f, 0.92f, 1f);
        [Range(0f, 1f), InspectorName("White Opacity（白色不透明度）")] public float impactFlashWhiteOpacity = 0.95f;
        [Min(0f), InspectorName("Emission Intensity（发光强度）")] public float impactFlashEmissionIntensity = 0.35f;
    }

    [DisallowMultipleComponent]
    public sealed class SoldierKnockbackEffect : MonoBehaviour
    {
        private static readonly List<SoldierKnockbackEffect> ActiveEffects = new List<SoldierKnockbackEffect>();
        private static readonly int HitFlashColorProperty = Shader.PropertyToID("_HitFlashColor");
        private static readonly int HitFlashActiveProperty = Shader.PropertyToID("_HitFlashActive");
        private static readonly int HitFlashOpacityProperty = Shader.PropertyToID("_HitFlashOpacity");
        private static readonly int HitFlashEmissionProperty = Shader.PropertyToID("_HitFlashEmission");

        private Animator[] animators = Array.Empty<Animator>();
        private Renderer[] flashRenderers = Array.Empty<Renderer>();
        private MaterialPropertyBlock[] originalPropertyBlocks = Array.Empty<MaterialPropertyBlock>();
        private MaterialPropertyBlock[] flashPropertyBlocks = Array.Empty<MaterialPropertyBlock>();
        private EnemyVisibilityController visibility;
        private Vector3 velocity;
        private Vector3 rotationAxis = Vector3.right;
        private float angularSpeed;
        private float gravityMultiplier;
        private float recycleHeight;
        private float remaining;
        private bool playing;
        private bool flashPlaying;
        private float flashRemaining;
        private float flashDuration;
        private int flashCycles;
        private Color flashRed;
        private Color flashWhite;
        private float flashWhiteOpacity;
        private float flashEmissionIntensity;

        public bool IsPlaying => playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ActiveEffects.Clear();
        }

        private void Awake()
        {
            animators = GetComponentsInChildren<Animator>(true);
        }

        public void Initialize(Renderer[] renderers)
        {
            flashRenderers = renderers ?? Array.Empty<Renderer>();
        }

        public bool Launch(SoldierKnockbackSettings settings, float normalizedSpeed,
            float impactForwardSpeed, int speedLevel, EnemyVisibilityController visibilityController)
        {
            if (settings == null || playing) return false;

            int maxActive = Mathf.Clamp(settings.maxActiveSoldiers, 4, 24);
            while (ActiveEffects.Count >= maxActive)
            {
                SoldierKnockbackEffect oldest = ActiveEffects[0];
                ActiveEffects.RemoveAt(0);
                if (oldest != null) oldest.Finish();
            }

            for (int i = 0; i < animators.Length; i++)
                if (animators[i] != null) animators[i].enabled = false;

            float speedT = Mathf.Clamp01(normalizedSpeed);
            float authoredMin = Mathf.Min(settings.minimumForwardSpeed, settings.maximumAuthoredForwardSpeed);
            float authoredMax = Mathf.Max(settings.minimumForwardSpeed, settings.maximumAuthoredForwardSpeed);
            float authoredForwardSpeed = Mathf.Lerp(authoredMin, authoredMax, speedT);
            float impactMultiplier = Mathf.Max(1f, settings.impactSpeedMultiplier)
                + Mathf.Max(0f, settings.highSpeedMultiplierBonus) * speedT;
            float forwardSpeed = Mathf.Max(authoredForwardSpeed,
                Mathf.Max(0f, impactForwardSpeed) * impactMultiplier);
            float lateralRange = Mathf.Clamp(speedLevel, 1, 10)
                * Mathf.Max(0f, settings.lateralRangePerSpeedLevel);

            velocity = new Vector3(
                UnityEngine.Random.Range(-lateralRange, lateralRange),
                Mathf.Max(0f, settings.upwardSpeed) * UnityEngine.Random.Range(0.92f, 1.08f),
                forwardSpeed * UnityEngine.Random.Range(1f, 1.06f));

            rotationAxis = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-0.25f, 0.25f),
                UnityEngine.Random.Range(-1f, 1f));
            if (rotationAxis.sqrMagnitude < 0.01f) rotationAxis = Vector3.right;
            rotationAxis.Normalize();
            float angularMin = Mathf.Min(settings.minimumRotationSpeed, settings.maximumRotationSpeed);
            float angularMax = Mathf.Max(settings.minimumRotationSpeed, settings.maximumRotationSpeed);
            angularSpeed = Mathf.Lerp(angularMin, angularMax, speedT)
                * UnityEngine.Random.Range(0.85f, 1.15f);

            visibility = visibilityController;
            gravityMultiplier = Mathf.Max(0f, settings.gravityMultiplier);
            recycleHeight = -Mathf.Abs(settings.belowRoadRecycleDepth);
            remaining = Mathf.Max(0.1f, settings.flightLifetime);
            playing = true;
            ActiveEffects.Add(this);
            StartImpactFlash(settings);
            return true;
        }

        private void Update()
        {
            if (!playing) return;
            UpdateImpactFlash(Time.unscaledDeltaTime);
            if (!playing) return;
            float worldDeltaTime = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.GetWorldDeltaTime()
                : Time.deltaTime;
            if (worldDeltaTime <= 0f) return;

            velocity += Physics.gravity * gravityMultiplier * worldDeltaTime;
            transform.position += velocity * worldDeltaTime;
            transform.rotation = Quaternion.AngleAxis(angularSpeed * worldDeltaTime, rotationAxis)
                * transform.rotation;
            remaining -= worldDeltaTime;

            if (remaining <= 0f || transform.position.y <= recycleHeight)
                Finish();
        }

        private void StartImpactFlash(SoldierKnockbackSettings settings)
        {
            RestoreImpactFlash();
            if (!settings.impactFlashEnabled) return;

            if (flashRenderers.Length == 0)
                flashRenderers = GetComponentsInChildren<Renderer>(true);
            if (flashRenderers.Length == 0) return;

            EnsurePropertyBlockBuffers(flashRenderers.Length);
            CapturePropertyBlocks();

            flashDuration = Mathf.Max(0.02f, settings.impactFlashDuration);
            flashRemaining = flashDuration;
            flashCycles = Mathf.Max(1, settings.impactFlashCycles);
            flashRed = settings.impactFlashRed;
            flashWhite = settings.impactFlashWhite;
            flashWhiteOpacity = Mathf.Clamp01(settings.impactFlashWhiteOpacity);
            flashEmissionIntensity = Mathf.Max(0f, settings.impactFlashEmissionIntensity);
            flashPlaying = true;
            ApplyImpactFlash(flashWhite, flashWhiteOpacity);
        }

        private void UpdateImpactFlash(float unscaledDeltaTime)
        {
            if (!flashPlaying) return;

            flashRemaining = Mathf.Max(0f, flashRemaining - Mathf.Max(0f, unscaledDeltaTime));
            if (flashRemaining <= 0f)
            {
                Finish();
                return;
            }

            float progress = 1f - flashRemaining / flashDuration;
            int phaseCount = flashCycles * 2;
            int phase = Mathf.Min(phaseCount - 1, Mathf.FloorToInt(progress * phaseCount));
            bool whitePhase = (phase & 1) == 0;
            ApplyImpactFlash(whitePhase ? flashWhite : flashRed,
                whitePhase ? flashWhiteOpacity : 0f);
        }

        private void EnsurePropertyBlockBuffers(int count)
        {
            if (originalPropertyBlocks.Length == count && flashPropertyBlocks.Length == count)
                return;

            originalPropertyBlocks = new MaterialPropertyBlock[count];
            flashPropertyBlocks = new MaterialPropertyBlock[count];
            for (int i = 0; i < count; i++)
            {
                originalPropertyBlocks[i] = new MaterialPropertyBlock();
                flashPropertyBlocks[i] = new MaterialPropertyBlock();
            }
        }

        private void CapturePropertyBlocks()
        {
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                Renderer targetRenderer = flashRenderers[i];
                if (targetRenderer == null) continue;

                originalPropertyBlocks[i].Clear();
                flashPropertyBlocks[i].Clear();
                targetRenderer.GetPropertyBlock(originalPropertyBlocks[i]);
                targetRenderer.GetPropertyBlock(flashPropertyBlocks[i]);
            }
        }

        private void ApplyImpactFlash(Color flashColor, float opacity)
        {
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                Renderer targetRenderer = flashRenderers[i];
                if (targetRenderer == null) continue;

                MaterialPropertyBlock block = flashPropertyBlocks[i];
                block.SetColor(HitFlashColorProperty, flashColor);
                block.SetFloat(HitFlashActiveProperty, 1f);
                block.SetFloat(HitFlashOpacityProperty, Mathf.Clamp01(opacity));
                block.SetFloat(HitFlashEmissionProperty, flashEmissionIntensity);
                targetRenderer.SetPropertyBlock(block);
            }
        }

        private void RestoreImpactFlash()
        {
            if (!flashPlaying) return;

            flashPlaying = false;
            flashRemaining = 0f;
            int count = Mathf.Min(flashRenderers.Length, originalPropertyBlocks.Length);
            for (int i = 0; i < count; i++)
            {
                Renderer targetRenderer = flashRenderers[i];
                if (targetRenderer != null)
                    targetRenderer.SetPropertyBlock(originalPropertyBlocks[i]);
            }
        }

        private void Finish()
        {
            if (!playing) return;
            RestoreImpactFlash();
            playing = false;
            ActiveEffects.Remove(this);
            velocity = Vector3.zero;
            angularSpeed = 0f;
            EnemyVisibilityController visibilityToRecycle = visibility;
            visibility = null;
            if (visibilityToRecycle != null)
                visibilityToRecycle.Recycle();
            else
                gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            RestoreImpactFlash();
            playing = false;
            ActiveEffects.Remove(this);
            velocity = Vector3.zero;
            angularSpeed = 0f;
            visibility = null;
        }
    }
}
