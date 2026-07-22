using UnityEngine;
using UnityEngine.UI;

namespace PlayableAd
{
    public sealed class SpeedBarView : MonoBehaviour
    {
        private sealed class EncounterHintView
        {
            public RectTransform root;
            public Image icon;
            public Sprite unlockedSprite;
            public Sprite lockedSprite;
            public Color unlockedTint;
            public int unlockLevel;
            public bool initialized;
            public bool unlocked;
            public float animationTimeRemaining;
        }

        private const float HintUnlockAnimationDuration = 0.24f;
        private const float HintUnlockPeakScale = 1.32f;
        private Image[] tickMarkers;
        private Image[] tickHighlights;
        private Text[] tickLabels;
        private PlayerSpeedController speedController;
        private SpeedVisualProfile profile;
        private Sprite hintFrameSprite;
        private Sprite soldierHintIcon;
        private Sprite stoneWallHintIcon;
        private Sprite stoneWallLockedHintIcon;
        private int stoneWallHintLevel = 7;
        private EncounterHintView soldierHint;
        private EncounterHintView stoneWallHint;
        private RectTransform safeAreaRoot;
        private RectTransform panel;
        private RectTransform continuousFill;
        private RectTransform currentIndicator;
        private Image continuousFillImage;
        private Image pulseOverlay;
        private Text currentLevelLabel;
        private RawImage levelUpBadge;
        private RectTransform canvasRect;
        private Texture2D[] levelUpTextures;
        private Transform levelUpTarget;
        private Camera levelUpCamera;
        private Rect lastSafeArea;
        private int currentLevel = 1;
        private float targetFill;
        private float displayedFill;
        private float pulse;
        private float badgeTimer;
        private float badgeDuration;
        private float badgeScale = 1f;
        private float badgeRise;

        private const float LevelUpBadgeRiseDistance = 54f;
        private const float LevelUpBadgeWorldDownOffset = 0.65f;
        private const float LevelUpBadgeScreenDownOffset = 40f;

        public void Initialize(PlayerSpeedController controller, SpeedVisualProfile visualProfile)
        {
            Initialize(controller, visualProfile, null, null, null, null, 7);
        }

        public void Initialize(PlayerSpeedController controller, SpeedVisualProfile visualProfile,
            Sprite frameSprite, Sprite soldierIcon, Sprite stoneWallIcon,
            Sprite lockedStoneWallIcon, int wallHintLevel,
            Transform levelUpTargetTransform = null, Camera targetCamera = null)
        {
            speedController = controller;
            profile = visualProfile;
            levelUpTarget = levelUpTargetTransform;
            levelUpCamera = targetCamera;
            hintFrameSprite = frameSprite;
            soldierHintIcon = soldierIcon;
            stoneWallHintIcon = stoneWallIcon;
            stoneWallLockedHintIcon = lockedStoneWallIcon;
            stoneWallHintLevel = Mathf.Clamp(wallHintLevel, 1, speedController.LevelCount);
            levelUpTextures = LoadLevelUpTextures();
            tickMarkers = new Image[speedController.LevelCount];
            tickHighlights = new Image[speedController.LevelCount];
            tickLabels = new Text[speedController.LevelCount];
            BuildCanvas();
            speedController.SpeedChanged += OnSpeedChanged;
            currentLevel = speedController.GetCurrentLevel();
            targetFill = displayedFill = speedController.GetNormalizedOverallProgress();
            RefreshLevel(currentLevel);
            ApplyFill(displayedFill);
        }

        private void BuildCanvas()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasRect = GetComponent<RectTransform>();

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            safeAreaRoot = CreateRect("SafeArea", transform);
            Stretch(safeAreaRoot);

            RectTransform topHud = CreateRect("TopHUD", safeAreaRoot);
            topHud.anchorMin = new Vector2(0f, 1f);
            topHud.anchorMax = Vector2.one;
            topHud.pivot = new Vector2(0.5f, 1f);
            topHud.anchoredPosition = Vector2.zero;
            topHud.sizeDelta = new Vector2(0f, 180f);

            panel = CreateRect("SpeedBar", topHud);
            panel.anchorMin = new Vector2(0.04f, 1f);
            panel.anchorMax = new Vector2(0.96f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -28f);
            panel.sizeDelta = new Vector2(0f, 122f);
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.025f, 0.035f, 0.045f, 0.9f);
            panelImage.raycastTarget = false;

            BuildTrack();
            BuildTicks();
            BuildCurrentLabel();
            BuildLevelUpBadge();
            ApplySafeArea();
        }

        private void BuildTrack()
        {
            RectTransform track = CreateRect("Background", panel);
            track.anchorMin = new Vector2(0.035f, 0.28f);
            track.anchorMax = new Vector2(0.965f, 0.58f);
            track.offsetMin = Vector2.zero;
            track.offsetMax = Vector2.zero;
            Image background = track.gameObject.AddComponent<Image>();
            background.color = new Color(0.1f, 0.12f, 0.14f, 0.98f);
            background.raycastTarget = false;

            continuousFill = CreateRect("Fill", track);
            continuousFill.anchorMin = Vector2.zero;
            continuousFill.anchorMax = new Vector2(0f, 1f);
            continuousFill.offsetMin = new Vector2(0f, 4f);
            continuousFill.offsetMax = new Vector2(0f, -4f);
            continuousFillImage = continuousFill.gameObject.AddComponent<Image>();
            continuousFillImage.raycastTarget = false;

            pulseOverlay = CreateRect("SpeedChangeFeedback", track).gameObject.AddComponent<Image>();
            Stretch(pulseOverlay.rectTransform);
            pulseOverlay.color = Color.clear;
            pulseOverlay.raycastTarget = false;

            currentIndicator = CreateRect("CurrentProgress", track);
            currentIndicator.anchorMin = new Vector2(0f, 0.5f);
            currentIndicator.anchorMax = new Vector2(0f, 0.5f);
            currentIndicator.pivot = new Vector2(0.5f, 0.5f);
            currentIndicator.sizeDelta = new Vector2(14f, 58f);
            Image indicatorImage = currentIndicator.gameObject.AddComponent<Image>();
            indicatorImage.color = Color.white;
            indicatorImage.raycastTarget = false;
        }

        private void BuildTicks()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform tickRoot = CreateRect("LevelTicks", panel);
            tickRoot.anchorMin = new Vector2(0.035f, 0.28f);
            tickRoot.anchorMax = new Vector2(0.965f, 0.58f);
            tickRoot.offsetMin = Vector2.zero;
            tickRoot.offsetMax = Vector2.zero;

            for (int level = 1; level <= tickMarkers.Length; level++)
            {
                int index = level - 1;
                float position = speedController.GetNormalizedLevelStart(level);
                RectTransform tick = CreateRect("Level_" + level, tickRoot);
                tick.anchorMin = new Vector2(position, 0.5f);
                tick.anchorMax = new Vector2(position, 0.5f);
                tick.pivot = new Vector2(0.5f, 0.5f);
                tick.sizeDelta = new Vector2(10f, 48f);
                Image marker = tick.gameObject.AddComponent<Image>();
                marker.raycastTarget = false;
                tickMarkers[index] = marker;

                RectTransform highlightRect = CreateRect("CurrentHighlight", tick);
                highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
                highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
                highlightRect.pivot = new Vector2(0.5f, 0.5f);
                highlightRect.sizeDelta = new Vector2(34f, 64f);
                Image highlight = highlightRect.gameObject.AddComponent<Image>();
                highlight.raycastTarget = false;
                tickHighlights[index] = highlight;

                RectTransform labelRect = CreateRect("Label", tick);
                labelRect.anchorMin = new Vector2(0.5f, 0f);
                labelRect.anchorMax = new Vector2(0.5f, 0f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.anchoredPosition = new Vector2(0f, -10f);
                labelRect.sizeDelta = new Vector2(level == 10 ? 58f : 44f, 42f);
                Text label = labelRect.gameObject.AddComponent<Text>();
                label.font = font;
                label.text = level.ToString();
                label.fontSize = 28;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
                tickLabels[index] = label;

                if (level == 1 && soldierHintIcon != null)
                {
                    soldierHint = BuildEncounterHint(tick, "SoldierHitHint", 1,
                        soldierHintIcon, soldierHintIcon, new Vector2(88f, 124f), Color.white);
                }
                if (level == stoneWallHintLevel && stoneWallHintIcon != null)
                {
                    stoneWallHint = BuildEncounterHint(tick, "StoneWallHitHint", stoneWallHintLevel,
                        stoneWallHintIcon,
                        stoneWallLockedHintIcon != null ? stoneWallLockedHintIcon : stoneWallHintIcon,
                        new Vector2(128f, 80f), new Color(1f, 0.78f, 0.42f, 1f));
                }
            }
        }

        private EncounterHintView BuildEncounterHint(RectTransform tick, string objectName, int unlockLevel,
            Sprite unlockedSprite, Sprite lockedSprite, Vector2 iconSize, Color unlockedTint)
        {
            RectTransform root = CreateRect(objectName, tick);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -55f);
            root.sizeDelta = new Vector2(152f, 152f);

            Image frame = root.gameObject.AddComponent<Image>();
            frame.sprite = hintFrameSprite;
            frame.preserveAspect = true;
            frame.color = Color.white;
            frame.raycastTarget = false;

            Shadow shadow = root.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            shadow.useGraphicAlpha = true;

            RectTransform iconRect = CreateRect("Icon", root);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = iconSize;
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = lockedSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            return new EncounterHintView
            {
                root = root,
                icon = icon,
                unlockedSprite = unlockedSprite,
                lockedSprite = lockedSprite,
                unlockedTint = unlockedTint,
                unlockLevel = unlockLevel
            };
        }

        private void BuildCurrentLabel()
        {
            RectTransform labelRect = CreateRect("CurrentLevelLabel", panel);
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -2f);
            labelRect.sizeDelta = new Vector2(260f, 42f);
            currentLevelLabel = labelRect.gameObject.AddComponent<Text>();
            currentLevelLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            currentLevelLabel.fontSize = 28;
            currentLevelLabel.fontStyle = FontStyle.Bold;
            currentLevelLabel.alignment = TextAnchor.MiddleCenter;
            currentLevelLabel.raycastTarget = false;
        }

        private void BuildLevelUpBadge()
        {
            RectTransform badgeRect = CreateRect("LevelUpBadge", transform);
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(300f, 75f);
            levelUpBadge = badgeRect.gameObject.AddComponent<RawImage>();
            levelUpBadge.raycastTarget = false;
            levelUpBadge.enabled = false;
        }

        private static Texture2D[] LoadLevelUpTextures()
        {
            Texture2D[] textures = new Texture2D[PlayerSpeedSettings.RequiredLevelCount];
            for (int level = 1; level <= textures.Length; level++)
            {
                string resourcePath = "PlayableAd/LevelUp/level" + level;
                textures[level - 1] = Resources.Load<Texture2D>(resourcePath);
                if (textures[level - 1] == null)
                {
                    Sprite sprite = Resources.Load<Sprite>(resourcePath);
                    textures[level - 1] = sprite != null ? sprite.texture : null;
                }
            }
            return textures;
        }

        private void OnSpeedChanged(SpeedChangedEvent change)
        {
            targetFill = speedController.GetNormalizedOverallProgress();
            if (change.NewLevel != currentLevel)
            {
                currentLevel = change.NewLevel;
                RefreshLevel(currentLevel);
                pulse = Mathf.Max(pulse, change.NewLevel > change.OldLevel ? profile.levelUpPulse : 0.28f);
            }
            else if (change.Reason == SpeedChangeReason.NormalImpact || change.Reason == SpeedChangeReason.LowLevelCollisionReward)
            {
                pulse = Mathf.Max(pulse, profile.normalBoostPulse);
            }
        }

        public void PulseNormalBoost()
        {
            pulse = Mathf.Max(pulse, profile != null ? profile.normalBoostPulse : 0.2f);
        }

        public void PlayLevelUp(int level, Color color, SpeedLevelFeedbackData feedback, bool showBadge)
        {
            currentLevel = Mathf.Clamp(level, 1, tickMarkers.Length);
            RefreshLevel(currentLevel);
            targetFill = speedController.GetNormalizedOverallProgress();
            pulse = Mathf.Max(pulse, profile.levelUpPulse);
            if (!showBadge || levelUpBadge == null || feedback == null) return;
            int textureIndex = Mathf.Clamp(level, 1, PlayerSpeedSettings.RequiredLevelCount) - 1;
            if (levelUpTextures == null || textureIndex >= levelUpTextures.Length
                || levelUpTextures[textureIndex] == null)
            {
                levelUpBadge.enabled = false;
                return;
            }

            levelUpBadge.texture = levelUpTextures[textureIndex];
            badgeDuration = feedback.levelBadgeDuration;
            badgeTimer = badgeDuration;
            badgeScale = feedback.levelBadgeScale;
            badgeRise = 0f;
            levelUpBadge.color = new Color(1f, 1f, 1f, 0f);
            levelUpBadge.rectTransform.localScale = Vector3.one * badgeScale * 1.12f;
            levelUpBadge.enabled = true;
            UpdateLevelUpBadgePosition(0f);
        }

        private void RefreshLevel(int level)
        {
            currentLevel = Mathf.Clamp(level, 1, tickMarkers.Length);
            currentLevelLabel.text = "SPEED  " + currentLevel + "/" + tickMarkers.Length;
            Color activeColor = profile.Get(currentLevel).uiColor;
            continuousFillImage.color = activeColor;
            currentIndicator.GetComponent<Image>().color = activeColor;

            for (int i = 0; i < tickMarkers.Length; i++)
            {
                int tickLevel = i + 1;
                Color tierColor = profile.Get(tickLevel).uiColor;
                bool reached = tickLevel <= currentLevel;
                Color markerColor = reached ? tierColor : new Color(0.34f, 0.37f, 0.4f, 0.55f);
                tickMarkers[i].color = markerColor;
                tickLabels[i].color = reached ? Color.white : new Color(0.72f, 0.74f, 0.76f, 0.72f);
                Color highlightColor = tierColor;
                highlightColor.a = tickLevel == currentLevel ? 0.34f : 0f;
                tickHighlights[i].color = highlightColor;
                tickHighlights[i].enabled = tickLevel == currentLevel;
            }

            RefreshEncounterHint(soldierHint, currentLevel);
            RefreshEncounterHint(stoneWallHint, currentLevel);
        }

        private static void RefreshEncounterHint(EncounterHintView hint, int level)
        {
            if (hint == null) return;
            bool shouldBeUnlocked = level >= hint.unlockLevel;
            if (!hint.initialized)
            {
                hint.initialized = true;
                hint.unlocked = shouldBeUnlocked;
                ApplyEncounterHintState(hint);
                return;
            }
            if (hint.unlocked == shouldBeUnlocked) return;

            hint.unlocked = shouldBeUnlocked;
            ApplyEncounterHintState(hint);
            if (shouldBeUnlocked)
                hint.animationTimeRemaining = HintUnlockAnimationDuration;
            else
            {
                hint.animationTimeRemaining = 0f;
                hint.root.localScale = Vector3.one;
            }
        }

        private static void ApplyEncounterHintState(EncounterHintView hint)
        {
            hint.icon.sprite = hint.unlocked ? hint.unlockedSprite : hint.lockedSprite;
            hint.icon.color = hint.unlocked
                ? hint.unlockedTint
                : new Color(0.72f, 0.72f, 0.72f, 0.82f);
            if (hint.animationTimeRemaining <= 0f)
                hint.root.localScale = Vector3.one;
        }

        private void Update()
        {
            if (safeAreaRoot == null) return;
            if (Screen.safeArea != lastSafeArea) ApplySafeArea();

            displayedFill = Mathf.Lerp(displayedFill, targetFill, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            ApplyFill(displayedFill);
            pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 3.5f);

            float wave = Mathf.Sin(Time.unscaledTime * 4.5f);
            tickHighlights[currentLevel - 1].rectTransform.localScale = Vector3.one * (1f + wave * 0.035f + pulse * 0.08f);
            Color overlayColor = profile.Get(currentLevel).uiColor;
            overlayColor.a = pulse * 0.24f;
            pulseOverlay.color = overlayColor;
            UpdateEncounterHintAnimation(soldierHint);
            UpdateEncounterHintAnimation(stoneWallHint);

            if (badgeTimer > 0f)
            {
                badgeTimer = Mathf.Max(0f, badgeTimer - Time.unscaledDeltaTime);
                float normalized = 1f - badgeTimer / Mathf.Max(0.01f, badgeDuration);
                float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalized / 0.12f));
                float fadeOut = 1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((normalized - 0.32f) / 0.68f));
                float alpha = fadeIn * fadeOut;
                levelUpBadge.color = new Color(1f, 1f, 1f, alpha);
                badgeRise = Mathf.SmoothStep(0f, LevelUpBadgeRiseDistance, normalized);
                levelUpBadge.rectTransform.localScale = Vector3.one * badgeScale * Mathf.Lerp(1.12f, 1f, normalized);
                UpdateLevelUpBadgePosition(badgeRise);
                if (badgeTimer <= 0f) levelUpBadge.enabled = false;
            }
        }

        private void UpdateLevelUpBadgePosition(float rise)
        {
            if (levelUpBadge == null || levelUpBadge.rectTransform == null) return;

            if (levelUpTarget != null && levelUpCamera != null && canvasRect != null)
            {
                Vector3 screenPoint = levelUpCamera.WorldToScreenPoint(
                    levelUpTarget.position + Vector3.down * LevelUpBadgeWorldDownOffset);
                if (screenPoint.z > 0f
                    && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, screenPoint, null, out Vector2 localPoint))
                {
                    localPoint += Vector2.down * LevelUpBadgeScreenDownOffset;
                    localPoint += Vector2.up * rise;
                    levelUpBadge.rectTransform.anchoredPosition = localPoint;
                    return;
                }
            }

            levelUpBadge.rectTransform.anchoredPosition = Vector2.up * rise;
        }

        private static void UpdateEncounterHintAnimation(EncounterHintView hint)
        {
            if (hint == null || hint.animationTimeRemaining <= 0f) return;
            hint.animationTimeRemaining = Mathf.Max(0f,
                hint.animationTimeRemaining - Time.unscaledDeltaTime);
            float progress = 1f - hint.animationTimeRemaining / HintUnlockAnimationDuration;
            float scale;
            if (progress < 0.3f)
            {
                float t = progress / 0.3f;
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                scale = Mathf.Lerp(1f, HintUnlockPeakScale, eased);
            }
            else
            {
                float t = (progress - 0.3f) / 0.7f;
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                scale = Mathf.Lerp(HintUnlockPeakScale, 1f, eased);
            }
            hint.root.localScale = Vector3.one * scale;
            if (hint.animationTimeRemaining <= 0f)
                hint.root.localScale = Vector3.one;
        }

        private void ApplyFill(float value)
        {
            value = Mathf.Clamp01(value);
            continuousFill.anchorMax = new Vector2(value, 1f);
            currentIndicator.anchorMin = currentIndicator.anchorMax = new Vector2(value, 0.5f);
        }

        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            Vector2 min = lastSafeArea.position;
            Vector2 max = lastSafeArea.position + lastSafeArea.size;
            min.x /= Mathf.Max(1f, Screen.width);
            min.y /= Mathf.Max(1f, Screen.height);
            max.x /= Mathf.Max(1f, Screen.width);
            max.y /= Mathf.Max(1f, Screen.height);
            safeAreaRoot.anchorMin = min;
            safeAreaRoot.anchorMax = max;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private void OnDestroy()
        {
            if (speedController != null) speedController.SpeedChanged -= OnSpeedChanged;
        }
    }
}
