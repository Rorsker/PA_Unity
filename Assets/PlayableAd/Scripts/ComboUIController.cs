using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlayableAd
{
    [Serializable]
    public sealed class ComboPresentationSettings
    {
        [Header("Layout（布局）")]
        [InspectorName("Screen Anchor（屏幕锚点）")]
        public Vector2 screenAnchor = new Vector2(0.78f, 0.72f);
        [InspectorName("Screen Offset（屏幕偏移）")]
        public Vector2 screenOffset = new Vector2(-48f, 0f);
        [Range(-25f, 25f), InspectorName("Text Angle（文字倾斜角度）")]
        public float textAngle = 9f;
        [Range(36, 120), InspectorName("Font Size（初始字体大小）")]
        public int fontSize = 72;
        [Range(36, 150), InspectorName("Final Font Size（100连击字体大小）")]
        public int finalFontSize = 92;
        [Range(0.1f, 1.5f), InspectorName("Zero Hold Duration（归零停留时长）")]
        public float zeroHoldDuration = 0.55f;
        [Range(0.05f, 0.8f), InspectorName("Zero Fade Duration（归零淡出时长）")]
        public float zeroFadeDuration = 0.2f;

        [Header("Punch animation（弹出动画）")]
        [Range(0.03f, 0.3f), InspectorName("Rise Duration（弹出时长）")]
        public float riseDuration = 0.09f;
        [Range(0.03f, 0.35f), InspectorName("Settle Duration（回弹时长）")]
        public float settleDuration = 0.11f;
        [Range(1f, 1.5f), InspectorName("Overshoot Scale（过冲缩放）")]
        public float overshootScale = 1.2f;
        [Range(0f, 0.3f), InspectorName("Combo Punch Growth（连击弹性成长）")]
        public float comboPunchGrowth = 0.12f;
        [Range(0f, 0.08f), InspectorName("Maximum Idle Pulse（最高持续脉冲）")]
        public float maximumIdlePulse = 0.035f;
        [Range(0.03f, 0.3f), InspectorName("Old Number Exit Duration（旧数字退出时长）")]
        public float oldNumberExitDuration = 0.1f;

        [Header("Shake growth（震动成长）")]
        [Range(1, 20), InspectorName("Light Shake Max Combo（轻震动最大连击）")]
        public int lightShakeMaxCombo = 9;
        [Range(2, 49), InspectorName("Medium Shake Max Combo（中震动最大连击）")]
        public int mediumShakeMaxCombo = 19;
        [Range(3, 99), InspectorName("Strong Shake Max Combo（强震动最大连击）")]
        public int strongShakeMaxCombo = 49;
        [Range(0f, 12f), InspectorName("Light Shake Strength（轻震动强度）")]
        public float lightShakeStrength = 3f;
        [Range(0f, 20f), InspectorName("Medium Shake Strength（中震动强度）")]
        public float mediumShakeStrength = 7f;
        [Range(0f, 30f), InspectorName("Strong Shake Strength（强震动强度）")]
        public float strongShakeStrength = 12f;
        [Range(0f, 35f), InspectorName("Elite Shake Strength（精英震动强度）")]
        public float eliteShakeStrength = 15f;
        [Range(0f, 40f), InspectorName("Final Shake Strength（最终震动强度）")]
        public float finalShakeStrength = 18f;
        [Range(0.04f, 0.4f), InspectorName("Shake Duration（震动时长）")]
        public float shakeDuration = 0.16f;
        [Range(10f, 100f), InspectorName("Shake Frequency（震动频率）")]
        public float shakeFrequency = 58f;

        [Header("Color growth（颜色成长）")]
        [Range(100, 200), InspectorName("Final Effect Start（最终效果起始连击）")]
        public int finalEffectStart = 100;
        [InspectorName("Combo Color Gradient（连击颜色渐变）")]
        public Gradient comboColorGradient = CreateDefaultComboGradient();
        [InspectorName("Legendary Red Color（最终红色）")]
        public Color legendaryRedColor = new Color(1f, 0.12f, 0.04f);
        [InspectorName("Legendary Gold Color（最终金色）")]
        public Color legendaryGoldColor = new Color(1f, 0.82f, 0.16f);
        [Range(2f, 30f), InspectorName("Color Response（颜色响应速度）")]
        public float colorResponse = 18f;
        [Range(0.2f, 5f), InspectorName("Legendary Color Pulse Speed（最终颜色脉冲速度）")]
        public float legendaryColorPulseSpeed = 2.2f;
        [Range(0.1f, 1.5f), InspectorName("Final Color Blend Duration（最终颜色融合时长）")]
        public float finalColorBlendDuration = 0.65f;

        [Header("Milestone feedback（里程碑反馈）")]
        [Range(1f, 1.8f), InspectorName("Milestone Scale（里程碑放大）")]
        public float milestoneScale = 1.42f;
        [Range(1f, 3f), InspectorName("Milestone Shake Multiplier（里程碑震动倍率）")]
        public float milestoneShakeMultiplier = 1.55f;
        [Range(0.15f, 1f), InspectorName("Halo Duration（光晕时长）")]
        public float haloDuration = 0.46f;
        [Range(1f, 5f), InspectorName("Halo Maximum Scale（光晕最大缩放）")]
        public float haloMaximumScale = 3.2f;
        [Range(0f, 1f), InspectorName("Halo Alpha（光晕透明度）")]
        public float haloAlpha = 0.62f;
        [Range(4, 24), InspectorName("Particle Count（粒子数量）")]
        public int particleCount = 12;
        [Range(60f, 320f), InspectorName("Particle Travel Distance（粒子扩散距离）")]
        public float particleTravelDistance = 180f;
        [Range(0.15f, 1f), InspectorName("Particle Duration（粒子时长）")]
        public float particleDuration = 0.44f;

        public void UpgradeLegacyDefaults()
        {
            Vector2 previousAnchor = new Vector2(0.5f, 0.64f);
            if ((screenAnchor - previousAnchor).sqrMagnitude < 0.0001f
                && screenOffset.sqrMagnitude < 0.0001f)
            {
                screenAnchor = new Vector2(0.78f, 0.72f);
                screenOffset = new Vector2(-48f, 0f);
                textAngle = 9f;
            }

            if (comboColorGradient == null) comboColorGradient = CreateDefaultComboGradient();
            finalEffectStart = Mathf.Max(100, finalEffectStart);
            if (finalFontSize <= fontSize) finalFontSize = Mathf.Max(92, fontSize);

            if (lightShakeMaxCombo == 5 && mediumShakeMaxCombo == 10)
            {
                lightShakeMaxCombo = 9;
                mediumShakeMaxCombo = 19;
                strongShakeMaxCombo = 49;
                eliteShakeStrength = 15f;
                finalShakeStrength = 18f;
            }
        }

        private static Gradient CreateDefaultComboGradient()
        {
            Gradient gradient = new Gradient();
            gradient.mode = GradientMode.Blend;
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.95f, 0.98f, 1f), 0f),
                    new GradientColorKey(new Color(0.24f, 0.68f, 1f), 0.1f),
                    new GradientColorKey(new Color(0.08f, 0.9f, 0.94f), 0.2f),
                    new GradientColorKey(new Color(0.22f, 0.96f, 0.42f), 0.4f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.16f), 0.6f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.06f), 0.82f),
                    new GradientColorKey(new Color(1f, 0.12f, 0.04f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }
    }

    public sealed class ComboUIController : MonoBehaviour
    {
        private sealed class ParticleState
        {
            public RectTransform rect;
            public Image image;
            public Vector2 velocity;
            public float age;
            public float lifetime;
            public float spin;
            public bool active;
        }

        private ComboManager comboManager;
        private ComboPresentationSettings settings;
        private RectTransform layerRoot;
        private RectTransform displayRoot;
        private CanvasGroup canvasGroup;
        private Text currentText;
        private Text outgoingText;
        private Text glowText;
        private Outline currentOutline;
        private Outline glowOutline;
        private Image haloImage;
        private RectTransform haloRect;
        private ParticleState[] particles;
        private Texture2D haloTexture;
        private Sprite haloSprite;
        private Vector2 basePosition;
        private Color animatedColor;
        private Color outgoingColor;
        private int displayedCombo;
        private float popElapsed;
        private float shakeElapsed;
        private float shakeAmplitude;
        private float zeroDelay;
        private float zeroFadeElapsed;
        private float milestoneElapsed;
        private float milestoneStrength;
        private float finalColorBlend;
        private bool popActive;
        private bool zeroFadeActive;
        private bool milestoneActive;
        private bool initialized;

        public void Initialize(ComboManager manager, ComboPresentationSettings presentationSettings)
        {
            if (initialized)
            {
                Debug.LogWarning("[Combo] ComboUIController.Initialize was called more than once.", this);
                return;
            }
            if (manager == null)
            {
                Debug.LogError("[Combo] ComboUIController requires a ComboManager.", this);
                enabled = false;
                return;
            }

            comboManager = manager;
            settings = presentationSettings ?? new ComboPresentationSettings();
            BuildUI();

            comboManager.ComboChanged += HandleComboChanged;
            comboManager.OnComboMilestone += HandleComboMilestone;
            displayedCombo = comboManager.GetCombo();
            finalColorBlend = displayedCombo >= GetFinalEffectStart() ? 1f : 0f;
            animatedColor = EvaluateComboColor(displayedCombo);
            currentText.text = FormatCombo(displayedCombo);
            glowText.text = currentText.text;
            ApplyComboFontSize(displayedCombo);
            canvasGroup.alpha = displayedCombo > 0 ? 1f : 0f;
            currentText.enabled = displayedCombo > 0;
            glowText.enabled = displayedCombo > 0;
            outgoingText.enabled = false;
            layerRoot.gameObject.SetActive(comboManager.IsEnabled);
            initialized = true;
            ApplyColor();
        }

        private void BuildUI()
        {
            layerRoot = CreateRect("ComboLayer", transform);
            Stretch(layerRoot);
            Canvas layerCanvas = layerRoot.gameObject.AddComponent<Canvas>();
            layerCanvas.overrideSorting = true;
            layerCanvas.sortingOrder = 55;

            displayRoot = CreateRect("ComboDisplay", layerRoot);
            Vector2 anchor = new Vector2(Mathf.Clamp01(settings.screenAnchor.x), Mathf.Clamp01(settings.screenAnchor.y));
            displayRoot.anchorMin = anchor;
            displayRoot.anchorMax = anchor;
            displayRoot.pivot = new Vector2(0.5f, 0.5f);
            displayRoot.sizeDelta = new Vector2(480f, 190f);
            basePosition = settings.screenOffset;
            displayRoot.anchoredPosition = basePosition;
            canvasGroup = displayRoot.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            haloRect = CreateRect("MilestoneHalo", displayRoot);
            haloRect.anchorMin = haloRect.anchorMax = new Vector2(0.5f, 0.5f);
            haloRect.pivot = new Vector2(0.5f, 0.5f);
            haloRect.sizeDelta = new Vector2(190f, 190f);
            haloImage = haloRect.gameObject.AddComponent<Image>();
            haloSprite = CreateHaloSprite();
            haloImage.sprite = haloSprite;
            haloImage.raycastTarget = false;
            haloImage.preserveAspect = true;
            haloImage.enabled = false;

            BuildParticlePool();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            glowText = CreateText("Glow", displayRoot, font);
            Quaternion textRotation = Quaternion.Euler(0f, 0f, settings.textAngle);
            glowText.rectTransform.localRotation = textRotation;
            glowOutline = glowText.gameObject.AddComponent<Outline>();
            glowOutline.effectDistance = new Vector2(7f, -7f);
            glowOutline.useGraphicAlpha = true;

            outgoingText = CreateText("OutgoingCombo", displayRoot, font);
            outgoingText.rectTransform.localRotation = textRotation;
            Shadow outgoingShadow = outgoingText.gameObject.AddComponent<Shadow>();
            outgoingShadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
            outgoingShadow.effectDistance = new Vector2(3f, -4f);

            currentText = CreateText("CurrentCombo", displayRoot, font);
            currentText.rectTransform.localRotation = textRotation;
            currentOutline = currentText.gameObject.AddComponent<Outline>();
            currentOutline.effectColor = new Color(0.05f, 0.025f, 0.01f, 0.8f);
            currentOutline.effectDistance = new Vector2(3f, -3f);
            Shadow currentShadow = currentText.gameObject.AddComponent<Shadow>();
            currentShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            currentShadow.effectDistance = new Vector2(4f, -5f);
        }

        private void BuildParticlePool()
        {
            int count = Mathf.Clamp(settings.particleCount, 4, 24);
            particles = new ParticleState[count];
            for (int i = 0; i < particles.Length; i++)
            {
                RectTransform rect = CreateRect("MilestoneParticle_" + i, displayRoot);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(9f, 30f);
                Image image = rect.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                image.enabled = false;
                particles[i] = new ParticleState { rect = rect, image = image };
            }
        }

        private Text CreateText(string objectName, Transform parent, Font font)
        {
            RectTransform rect = CreateRect(objectName, parent);
            Stretch(rect);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = Mathf.Clamp(settings.fontSize, 36, 120);
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void HandleComboChanged(ComboChangedEvent change)
        {
            if (!initialized || !comboManager.IsEnabled) return;

            outgoingText.text = currentText.text;
            outgoingColor = currentText.color;
            outgoingText.color = outgoingColor;
            outgoingText.fontSize = currentText.fontSize;
            outgoingText.rectTransform.anchoredPosition = Vector2.zero;
            outgoingText.rectTransform.localScale = Vector3.one;
            outgoingText.enabled = change.PreviousCombo > 0 && canvasGroup.alpha > 0.01f;

            displayedCombo = change.CurrentCombo;
            currentText.text = FormatCombo(displayedCombo);
            glowText.text = currentText.text;
            ApplyComboFontSize(displayedCombo);
            currentText.enabled = true;
            glowText.enabled = true;
            canvasGroup.alpha = 1f;
            popElapsed = 0f;
            popActive = true;
            shakeElapsed = 0f;
            shakeAmplitude = GetShakeStrength(displayedCombo > 0 ? displayedCombo : change.PreviousCombo);

            zeroFadeActive = change.WasReset;
            zeroDelay = change.WasReset ? Mathf.Max(0f, settings.zeroHoldDuration) : 0f;
            zeroFadeElapsed = 0f;
            if (change.WasReset) CancelMilestoneFeedback();
        }

        private void HandleComboMilestone(int combo)
        {
            if (!initialized || combo <= 0) return;

            milestoneElapsed = 0f;
            milestoneStrength = GetMilestoneStrength(combo);
            milestoneActive = true;
            haloRect.localScale = Vector3.one * 0.45f;
            haloImage.enabled = true;
            shakeElapsed = 0f;
            shakeAmplitude = Mathf.Max(shakeAmplitude,
                GetShakeStrength(combo) * Mathf.Lerp(1f,
                    Mathf.Max(1f, settings.milestoneShakeMultiplier), milestoneStrength));
            SpawnParticles(combo, milestoneStrength);
        }

        private void Update()
        {
            if (!initialized || !comboManager.IsEnabled) return;

            float deltaTime = Time.unscaledDeltaTime;
            UpdateColor(deltaTime);
            UpdateMilestone(deltaTime);
            UpdatePop(deltaTime);
            UpdateShake(deltaTime);
            UpdateZeroFade(deltaTime);
            UpdateParticles(deltaTime);
        }

        private void UpdatePop(float deltaTime)
        {
            float scale = 1f;
            float progression = GetFinalProgress(displayedCombo);
            float overshootScale = Mathf.Max(1f,
                settings.overshootScale + Mathf.Max(0f, settings.comboPunchGrowth) * progression);
            if (popActive)
            {
                popElapsed += deltaTime;
                float riseDuration = Mathf.Max(0.01f, settings.riseDuration);
                float settleDuration = Mathf.Max(0.01f, settings.settleDuration);
                float exitDuration = Mathf.Max(0.01f, settings.oldNumberExitDuration);
                if (popElapsed < riseDuration)
                {
                    float t = EaseOutCubic(popElapsed / riseDuration);
                    scale = Mathf.LerpUnclamped(0.01f, overshootScale, t);
                }
                else if (popElapsed < riseDuration + settleDuration)
                {
                    float t = Smooth01((popElapsed - riseDuration) / settleDuration);
                    scale = Mathf.Lerp(overshootScale, 1f, t);
                }

                float exitT = Mathf.Clamp01(popElapsed / exitDuration);
                Color faded = outgoingColor;
                faded.a *= 1f - exitT;
                outgoingText.color = faded;
                outgoingText.rectTransform.anchoredPosition = Vector2.up * (20f * exitT);
                outgoingText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.72f, exitT);
                if (exitT >= 1f) outgoingText.enabled = false;
                if (popElapsed >= Mathf.Max(riseDuration + settleDuration, exitDuration))
                    popActive = false;
            }

            float milestoneMultiplier = GetMilestoneScaleMultiplier();
            float idlePulse = GetIdlePulseMultiplier(progression);
            currentText.rectTransform.localScale = Vector3.one * scale * milestoneMultiplier * idlePulse;
            glowText.rectTransform.localScale = Vector3.one * scale * milestoneMultiplier * idlePulse * 1.045f;
        }

        private void UpdateShake(float deltaTime)
        {
            float duration = Mathf.Max(0.01f, settings.shakeDuration);
            if (shakeElapsed >= duration || shakeAmplitude <= 0f)
            {
                displayRoot.anchoredPosition = basePosition;
                return;
            }

            shakeElapsed += deltaTime;
            float fade = 1f - Mathf.Clamp01(shakeElapsed / duration);
            float phase = Time.unscaledTime * Mathf.Max(1f, settings.shakeFrequency) + displayedCombo * 0.73f;
            Vector2 noise = new Vector2(Mathf.Sin(phase * 1.13f), Mathf.Cos(phase * 1.71f + 0.35f));
            displayRoot.anchoredPosition = basePosition + noise * (shakeAmplitude * fade);
        }

        private void UpdateZeroFade(float deltaTime)
        {
            if (!zeroFadeActive) return;
            if (zeroDelay > 0f)
            {
                zeroDelay -= deltaTime;
                return;
            }

            zeroFadeElapsed += deltaTime;
            float duration = Mathf.Max(0.01f, settings.zeroFadeDuration);
            canvasGroup.alpha = 1f - Mathf.Clamp01(zeroFadeElapsed / duration);
            if (zeroFadeElapsed < duration) return;

            zeroFadeActive = false;
            currentText.enabled = false;
            glowText.enabled = false;
            outgoingText.enabled = false;
        }

        private void UpdateColor(float deltaTime)
        {
            Color target = EvaluateComboColor(displayedCombo);
            int finalEffectStart = GetFinalEffectStart();
            float targetFinalBlend = displayedCombo >= finalEffectStart ? 1f : 0f;
            finalColorBlend = Mathf.MoveTowards(finalColorBlend, targetFinalBlend,
                deltaTime / Mathf.Max(0.01f, settings.finalColorBlendDuration));
            if (displayedCombo >= finalEffectStart)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime
                    * Mathf.Max(0.1f, settings.legendaryColorPulseSpeed) * Mathf.PI * 2f);
                Color legendaryPulse = Color.Lerp(settings.legendaryRedColor, settings.legendaryGoldColor, pulse);
                target = Color.Lerp(target, legendaryPulse, Smooth01(finalColorBlend));
            }

            float response = 1f - Mathf.Exp(-Mathf.Max(1f, settings.colorResponse) * deltaTime);
            animatedColor = Color.Lerp(animatedColor, target, response);
            ApplyColor();
        }

        private void ApplyColor()
        {
            currentText.color = animatedColor;
            Color glowColor = animatedColor;
            float comboStrength = GetFinalProgress(displayedCombo);
            glowColor.a = Mathf.Lerp(0.12f, 0.58f, comboStrength);
            glowText.color = glowColor;
            if (glowOutline != null)
            {
                Color outlineColor = glowColor;
                outlineColor.a = Mathf.Lerp(0.1f, 0.5f, comboStrength);
                glowOutline.effectColor = outlineColor;
                float glowDistance = Mathf.Lerp(5f, 12f, comboStrength);
                glowOutline.effectDistance = new Vector2(glowDistance, -glowDistance);
            }
            if (currentOutline != null)
                currentOutline.effectColor = new Color(0.05f, 0.025f, 0.01f,
                    Mathf.Lerp(0.7f, 0.94f, comboStrength));
        }

        private void UpdateMilestone(float deltaTime)
        {
            if (!milestoneActive) return;

            milestoneElapsed += deltaTime;
            float duration = Mathf.Max(0.01f, settings.haloDuration);
            float t = Mathf.Clamp01(milestoneElapsed / duration);
            float eased = EaseOutCubic(t);
            float maximumScale = Mathf.Lerp(1.25f,
                Mathf.Max(1.25f, settings.haloMaximumScale), milestoneStrength);
            haloRect.localScale = Vector3.one * Mathf.Lerp(0.45f, maximumScale, eased);
            Color color = animatedColor;
            color.a = Mathf.Clamp01(settings.haloAlpha)
                * Mathf.Lerp(0.25f, 1f, milestoneStrength) * (1f - t) * (1f - t);
            haloImage.color = color;
            if (t < 1f) return;

            milestoneActive = false;
            haloImage.enabled = false;
        }

        private void CancelMilestoneFeedback()
        {
            milestoneActive = false;
            milestoneStrength = 0f;
            haloImage.enabled = false;
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].active = false;
                particles[i].image.enabled = false;
            }
        }

        private float GetMilestoneScaleMultiplier()
        {
            if (!milestoneActive) return 1f;
            float duration = Mathf.Max(0.01f, settings.haloDuration);
            float t = Mathf.Clamp01(milestoneElapsed / duration);
            return 1f + (Mathf.Max(1f, settings.milestoneScale) - 1f)
                * milestoneStrength * Mathf.Sin(t * Mathf.PI);
        }

        private void SpawnParticles(int combo, float strength)
        {
            Color color = EvaluateComboColor(combo);
            float lifetime = Mathf.Max(0.05f, settings.particleDuration);
            float distance = Mathf.Max(1f, settings.particleTravelDistance)
                * Mathf.Lerp(0.55f, 1f, strength);
            int activeCount = Mathf.Clamp(Mathf.CeilToInt(particles.Length
                * Mathf.Lerp(0.25f, 1f, strength)), 1, particles.Length);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleState particle = particles[i];
                if (i >= activeCount)
                {
                    particle.active = false;
                    particle.image.enabled = false;
                    continue;
                }
                float normalized = (i + (combo % 7) * 0.17f) / activeCount;
                float angle = normalized * Mathf.PI * 2f;
                float variation = 0.82f + 0.18f * Mathf.Sin((i + 1) * 2.31f + combo);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                particle.velocity = direction * (distance / lifetime) * variation;
                particle.age = 0f;
                particle.lifetime = lifetime * Mathf.Lerp(0.84f, 1.08f, (i % 4) / 3f);
                particle.spin = (i % 2 == 0 ? 1f : -1f) * Mathf.Lerp(120f, 260f, (i % 5) / 4f);
                particle.active = true;
                particle.rect.anchoredPosition = Vector2.zero;
                particle.rect.localScale = Vector3.one;
                particle.rect.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
                particle.image.color = color;
                particle.image.enabled = true;
            }
        }

        private void UpdateParticles(float deltaTime)
        {
            if (particles == null) return;
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleState particle = particles[i];
                if (particle == null || !particle.active) continue;

                particle.age += deltaTime;
                float t = Mathf.Clamp01(particle.age / Mathf.Max(0.01f, particle.lifetime));
                particle.rect.anchoredPosition = particle.velocity * particle.age * (1f - 0.24f * t);
                particle.rect.Rotate(0f, 0f, particle.spin * deltaTime);
                particle.rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.22f, t);
                Color color = particle.image.color;
                color.a = (1f - t) * (1f - t);
                particle.image.color = color;
                if (t < 1f) continue;

                particle.active = false;
                particle.image.enabled = false;
            }
        }

        private Color EvaluateComboColor(int combo)
        {
            if (combo <= 0) return new Color(0.82f, 0.86f, 0.9f);
            Gradient gradient = settings.comboColorGradient;
            return gradient != null
                ? gradient.Evaluate(GetFinalProgress(combo))
                : Color.Lerp(new Color(0.24f, 0.68f, 1f), settings.legendaryRedColor,
                    GetFinalProgress(combo));
        }

        private void ApplyComboFontSize(int combo)
        {
            int startSize = Mathf.Clamp(settings.fontSize, 36, 120);
            int endSize = Mathf.Max(startSize, Mathf.Clamp(settings.finalFontSize, 36, 150));
            int size = Mathf.RoundToInt(Mathf.Lerp(startSize, endSize, GetFinalProgress(combo)));
            currentText.fontSize = size;
            glowText.fontSize = size;
        }

        private int GetFinalEffectStart()
        {
            return Mathf.Max(100, settings.finalEffectStart);
        }

        private float GetFinalProgress(int combo)
        {
            if (combo <= 0) return 0f;
            return Mathf.InverseLerp(1f, GetFinalEffectStart(), combo);
        }

        private float GetMilestoneStrength(int combo)
        {
            return Mathf.Sqrt(GetFinalProgress(combo));
        }

        private float GetIdlePulseMultiplier(float progression)
        {
            if (displayedCombo < GetFirstTransitionStart() || settings.maximumIdlePulse <= 0f) return 1f;
            float amplitude = Mathf.Max(0f, settings.maximumIdlePulse) * progression;
            float speed = Mathf.Lerp(1.25f, 2.5f, progression);
            return 1f + Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) * amplitude;
        }

        private int GetFirstTransitionStart()
        {
            return Mathf.Max(1, settings.lightShakeMaxCombo) + 1;
        }

        private float GetShakeStrength(int combo)
        {
            int lightEnd = Mathf.Max(1, settings.lightShakeMaxCombo);
            int mediumEnd = Mathf.Max(lightEnd + 1, settings.mediumShakeMaxCombo);
            int strongEnd = Mathf.Max(mediumEnd + 1, settings.strongShakeMaxCombo);
            if (combo >= GetFinalEffectStart()) return Mathf.Max(0f, settings.finalShakeStrength);
            if (combo <= lightEnd) return Mathf.Max(0f, settings.lightShakeStrength);
            if (combo <= mediumEnd) return Mathf.Max(0f, settings.mediumShakeStrength);
            if (combo <= strongEnd) return Mathf.Max(0f, settings.strongShakeStrength);
            return Mathf.Max(0f, settings.eliteShakeStrength);
        }

        private Sprite CreateHaloSprite()
        {
            const int size = 64;
            haloTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "RuntimeComboHalo",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f) * 2f;
                    float radius = point.magnitude;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(radius - 0.7f) / 0.09f);
                    float softGlow = Mathf.Clamp01(1f - Mathf.Abs(radius - 0.7f) / 0.25f) * 0.34f;
                    float alpha = Mathf.Max(ring, softGlow) * Mathf.Clamp01((1f - radius) * 5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            haloTexture.SetPixels(pixels);
            haloTexture.Apply(false, true);
            return Sprite.Create(haloTexture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static string FormatCombo(int combo)
        {
            return "连击 X" + Mathf.Max(0, combo);
        }

        private static float EaseOutCubic(float value)
        {
            float t = Mathf.Clamp01(value);
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (comboManager != null)
            {
                comboManager.ComboChanged -= HandleComboChanged;
                comboManager.OnComboMilestone -= HandleComboMilestone;
            }
            if (haloSprite != null) Destroy(haloSprite);
            if (haloTexture != null) Destroy(haloTexture);
        }
    }
}
