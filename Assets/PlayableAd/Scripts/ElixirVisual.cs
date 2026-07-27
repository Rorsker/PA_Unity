using System;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class ElixirPresentationSettings
    {
        [Header("Idle Readability（待机可读性）")]
        [Range(0.05f, 0.5f), InspectorName("Hover Height（悬浮高度）")] public float hoverHeight = 0.18f;
        [Range(20f, 180f), InspectorName("Rotation Speed（旋转速度）")] public float rotationSpeed = 72f;
        [Range(0.5f, 5f), InspectorName("Breath Speed（呼吸速度）")] public float breathSpeed = 2.2f;
        [Range(0f, 2f), InspectorName("Emission Intensity（发光强度）")] public float emissionIntensity = 0.8f;
        [Range(0.8f, 2.2f), InspectorName("Ring Radius（光环半径）")] public float ringRadius = 1.1f;

        [Header("Large potion highlight（大药瓶高亮）")]
        [Range(1f, 4f), InspectorName("Large Potion Emission Multiplier（大药瓶发光倍率）")]
        public float largePotionEmissionMultiplier = 2.2f;
        [Range(1.2f, 3f), InspectorName("Large Potion Ring Radius Multiplier（大药瓶光环半径倍率）")]
        public float largePotionRingRadiusMultiplier = 2.1f;
        [Range(0.04f, 0.18f), InspectorName("Large Potion Ring Width（大药瓶光环宽度）")]
        public float largePotionRingWidth = 0.1f;
        [Range(0.5f, 6f), InspectorName("Large Potion Pulse Speed（大药瓶脉冲速度）")]
        public float largePotionPulseSpeed = 3f;
        [ColorUsage(false, true), InspectorName("Large Potion Highlight Color（大药瓶高亮颜色）")]
        public Color largePotionHighlightColor = new Color(1f, 0.76f, 0.18f, 1f);

        [Header("Pickup Sequence（拾取流程）")]
        [Range(0.6f, 0.9f), InspectorName("Total Duration（总时长）")] public float totalDuration = 0.8f;
        [Range(0.06f, 0.16f), InspectorName("Collapse Duration（收缩时长）")] public float collapseDuration = 0.11f;
        [Range(0.1f, 0.3f), InspectorName("Upgrade Moment（升级时刻）")] public float upgradeMoment = 0.18f;
        [Range(0.05f, 0.15f), InspectorName("Slow Motion Duration（慢动作时长）")] public float slowMotionDuration = 0.09f;
        [Range(0.35f, 0.9f), InspectorName("Slow Motion Scale（慢动作缩放）")] public float slowMotionScale = 0.62f;
        [Range(0f, 0.5f), InspectorName("Camera Shake（镜头抖动）")] public float cameraShake = 0.11f;
        [Range(0f, 5f), InspectorName("Camera Push In（镜头推进）")] public float cameraPushIn = 1.8f;
        [Range(0f, 6f), InspectorName("Camera Rebound（镜头回弹）")] public float cameraRebound = 3.2f;
        [Range(0f, 1f), InspectorName("Pickup Flash（拾取闪光）")] public float pickupFlash = 0.32f;
        [Range(0.2f, 1.5f), InspectorName("Energy Ring Max Radius（能量环最大半径）")] public float energyRingMaxRadius = 1.25f;
    }

    public sealed class ElixirVisual : MonoBehaviour
    {
        private ElixirPresentationSettings settings;
        private Renderer[] renderers;
        private LineRenderer groundRing;
        private LineRenderer largePotionHighlightRing;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 basePosition;
        private bool consumed;
        private float seed;
        private Color primaryColor;
        private Color secondaryColor;
        private Material sharedLineMaterial;
        private float animationTime;
        private bool largePotionHighlight;

        public void Initialize(ElixirPresentationSettings presentationSettings,
            Renderer[] visualRenderers, SpeedVisualProfile profile, int targetLevel,
            bool useLargePotionHighlight = false)
        {
            settings = presentationSettings;
            renderers = visualRenderers;
            propertyBlock = new MaterialPropertyBlock();
            basePosition = transform.localPosition;
            seed = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            animationTime = 0f;
            SpeedTierVisualData tier = profile.Get(targetLevel);
            primaryColor = tier.primaryColor;
            secondaryColor = tier.secondaryColor;
            sharedLineMaterial = profile.lineMaterial;
            largePotionHighlight = useLargePotionHighlight;
            BuildGroundRing();
            if (largePotionHighlight) BuildLargePotionHighlightRing();

            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].sharedMaterial;
                if (material != null && material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                }
            }
        }

        public void BeginConsume()
        {
            consumed = true;
            if (groundRing != null)
            {
                groundRing.enabled = false;
            }
            if (largePotionHighlightRing != null)
            {
                largePotionHighlightRing.enabled = false;
            }
        }

        private void Update()
        {
            if (consumed || settings == null || renderers == null || propertyBlock == null)
            {
                return;
            }

            float worldDeltaTime = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.GetWorldDeltaTime()
                : Time.deltaTime;
            animationTime += worldDeltaTime;
            float wave = (Mathf.Sin(animationTime * settings.breathSpeed + seed) + 1f) * 0.5f;
            transform.localPosition = basePosition + Vector3.up * (wave * settings.hoverHeight);
            transform.Rotate(0f, settings.rotationSpeed * worldDeltaTime, 0f, Space.Self);

            float highlightWave = (Mathf.Sin(animationTime * settings.largePotionPulseSpeed + seed) + 1f) * 0.5f;
            Color emissionColor = largePotionHighlight
                ? Color.Lerp(secondaryColor, settings.largePotionHighlightColor, 0.65f)
                : secondaryColor;
            float emissionMultiplier = largePotionHighlight
                ? settings.largePotionEmissionMultiplier
                : 1f;
            Color emission = emissionColor
                * (Mathf.Lerp(0.25f, settings.emissionIntensity, wave) * emissionMultiplier);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_EmissionColor", emission);
                renderer.SetPropertyBlock(propertyBlock);
            }

            if (groundRing != null)
            {
                float radius = settings.ringRadius * Mathf.Lerp(0.9f, 1.08f, wave);
                groundRing.transform.localScale = new Vector3(radius, radius, radius);
                Color color = Color.Lerp(primaryColor, secondaryColor, wave);
                color.a = Mathf.Lerp(0.28f, 0.68f, wave);
                groundRing.startColor = color;
                groundRing.endColor = color;
            }

            if (largePotionHighlightRing != null)
            {
                float radius = settings.ringRadius * settings.largePotionRingRadiusMultiplier
                    * Mathf.Lerp(0.9f, 1.1f, highlightWave);
                largePotionHighlightRing.transform.localScale = Vector3.one * radius;
                Color color = settings.largePotionHighlightColor;
                color.a = Mathf.Lerp(0.32f, 0.82f, highlightWave);
                largePotionHighlightRing.startColor = color;
                largePotionHighlightRing.endColor = color;
            }
        }

        private void BuildGroundRing()
        {
            groundRing = BuildRing("PickupRing", 32, 0.045f);
        }

        private void BuildLargePotionHighlightRing()
        {
            largePotionHighlightRing = BuildRing("LargePotionHighlightRing", 40,
                settings.largePotionRingWidth);
        }

        private LineRenderer BuildRing(string objectName, int segmentCount, float width)
        {
            GameObject ringObject = new GameObject(objectName);
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = new Vector3(0f, -0.82f, 0f);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = segmentCount;
            ring.startWidth = width;
            ring.endWidth = width;
            ring.sharedMaterial = sharedLineMaterial != null
                ? sharedLineMaterial
                : RuntimeStyle.CreateMaterial(Color.white, 0f, 0.1f);
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i / (float)segmentCount * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }
            return ring;
        }
    }
}
