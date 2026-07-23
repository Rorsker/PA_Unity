using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PlayableAd
{
    [Serializable]
    public sealed class NumberCombatSettings
    {
        [InspectorName("Enabled（启用数值系统）")]
        public bool enabled = true;

        [Header("Level colors（等级颜色）")]
        [FormerlySerializedAs("beatableColor")]
        [InspectorName("Player Color（玩家等级颜色）")]
        public Color playerColor = new Color(0.08f, 0.5f, 1f, 1f);

        [InspectorName("Beatable Target Color（可无损撞击目标颜色）")]
        public Color beatableTargetColor = new Color(0.86f, 0.86f, 0.86f, 1f);

        [InspectorName("Stronger Color（高等级目标颜色）")]
        public Color strongerColor = new Color(1f, 0.08f, 0.08f, 1f);

        [InspectorName("Outline Color（描边颜色）")]
        public Color outlineColor = new Color(0.01f, 0.015f, 0.025f, 0.96f);

        [Header("Level label presentation（等级标签表现）")]
        [Range(24, 120), InspectorName("Player Font Size（玩家字号）")]
        public int playerFontSize = 64;

        [Range(24, 120), InspectorName("Target Font Size（目标字号）")]
        public int targetFontSize = 56;

        [Range(1f, 2f), InspectorName("LV10 Font Scale（LV10字号倍率）")]
        public float levelTenFontScale = 1.35f;

        [Range(0.5f, 8f), InspectorName("Outline Thickness（描边粗细）")]
        public float outlineThickness = 3f;

        [Min(80f), InspectorName("Label Width（标签宽度）")]
        public float labelWidth = 320f;

        [Min(40f), InspectorName("Label Height（标签高度）")]
        public float labelHeight = 96f;

        [Min(0f), InspectorName("Player Head Clearance（玩家头顶间隙）")]
        public float playerHeadClearance = 0.42f;

        [Min(0f), InspectorName("Soldier Head Clearance（小兵头顶间隙）")]
        public float soldierHeadClearance = 0.32f;

        [Min(0f), InspectorName("Stone Wall Head Clearance（石墙头顶间隙）")]
        public float stoneWallHeadClearance = 0.45f;

        [Min(0f), InspectorName("Boss Head Clearance（Boss 头顶间隙）")]
        public float bossHeadClearance = 0.55f;

        [Range(0, 100), InspectorName("Canvas Sorting Order（画布排序层级）")]
        public int canvasSortingOrder = 54;

        [Range(10f, 120f), InspectorName("Target Label Preview Distance（目标标签显示距离）")]
        public float targetLabelPreviewDistance = 45f;
    }

    public sealed class NumberCombatTarget
    {
        internal Transform Owner;
        internal Text Label;
        internal EnemyVisibilityController Visibility;
        internal float HeadOffset;
        internal bool Resolved;

        public int FixedLevel { get; internal set; }
        public bool HasResolved => Resolved;
    }

    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class NumberCombatSystem : MonoBehaviour
    {
        private readonly List<NumberCombatTarget> targets = new List<NumberCombatTarget>();
        private NumberCombatSettings settings;
        private Camera gameCamera;
        private RectTransform canvasRect;
        private Transform playerOwner;
        private PlayerSpeedController speedController;
        private Text playerLabel;
        private float playerHeadOffset;
        private int playerLevel;
        private Font labelFont;
        private bool initialized;

        public event Action<int> PlayerLevelChanged;

        public int PlayerLevel => playerLevel;

        public void Initialize(NumberCombatSettings numberSettings, Camera targetCamera,
            Transform playerTransform, Renderer[] playerRenderers, PlayerSpeedController playerSpeedController)
        {
            settings = numberSettings ?? new NumberCombatSettings();
            gameCamera = targetCamera;
            playerOwner = playerTransform;
            speedController = playerSpeedController;
            targets.Clear();

            BuildCanvas();
            playerLabel = CreateLabel("PlayerNumber", GetLevelFontSize(settings.playerFontSize, 1), settings.playerColor);
            playerHeadOffset = CalculateHeadOffset(playerOwner, playerRenderers,
                settings.playerHeadClearance, 2f);
            playerLevel = GetCurrentPlayerLevel();
            RefreshPlayerLabel();
            initialized = true;
        }

        public NumberCombatTarget RegisterTarget(Transform owner, Renderer[] renderers, int fixedLevel,
            float headClearance, EnemyVisibilityController visibility = null)
        {
            if (!initialized || owner == null) return null;

            int clampedLevel = Mathf.Clamp(fixedLevel, 1, 10);
            NumberCombatTarget target = new NumberCombatTarget
            {
                Owner = owner,
                Label = CreateLabel(owner.name + "_Number",
                    GetLevelFontSize(settings.targetFontSize, clampedLevel), settings.strongerColor),
                Visibility = visibility,
                FixedLevel = clampedLevel,
                HeadOffset = CalculateHeadOffset(owner, renderers, headClearance, 1.8f)
            };
            target.Label.text = FormatLevel(target.FixedLevel);
            target.Label.gameObject.SetActive(false);
            targets.Add(target);
            return target;
        }

        public bool ResolveTarget(NumberCombatTarget target)
        {
            if (!initialized || target == null || target.Resolved) return false;

            target.Resolved = true;
            SetLabelActive(target.Label, false);
            return true;
        }

        public void ResetTarget(NumberCombatTarget target)
        {
            if (target == null) return;
            target.Resolved = false;
            if (target.Label != null)
            {
                target.Label.text = FormatLevel(target.FixedLevel);
                target.Label.fontSize = GetLevelFontSize(settings.targetFontSize, target.FixedLevel);
            }
        }

        public void RefreshPlayerLevel()
        {
            if (!initialized) return;
            SyncPlayerLevel(true);
        }

        public void SetVisible(bool visible)
        {
            if (canvasRect != null)
                canvasRect.gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            if (!initialized || gameCamera == null || canvasRect == null) return;

            SyncPlayerLevel();
            UpdatePlayerLabel();
            for (int i = 0; i < targets.Count; i++)
                UpdateTargetLabel(targets[i]);
        }

        private void BuildCanvas()
        {
            GameObject existing = transform.Find("NumberCombatCanvas")?.gameObject;
            if (existing != null) Destroy(existing);

            GameObject canvasObject = new GameObject("NumberCombatCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = settings.canvasSortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = canvasObject.GetComponent<RectTransform>();
            labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private Text CreateLabel(string objectName, int fontSize, Color color)
        {
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(canvasRect, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(settings.labelWidth, settings.labelHeight);

            Text label = labelObject.GetComponent<Text>();
            label.font = labelFont;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.color = color;

            Outline outline = labelObject.AddComponent<Outline>();
            outline.effectColor = settings.outlineColor;
            outline.effectDistance = new Vector2(settings.outlineThickness, -settings.outlineThickness);
            outline.useGraphicAlpha = true;

            Shadow shadow = labelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(settings.outlineThickness * 0.8f,
                -settings.outlineThickness * 1.15f);
            shadow.useGraphicAlpha = true;
            return label;
        }

        private void UpdatePlayerLabel()
        {
            bool visible = playerOwner != null && playerOwner.gameObject.activeInHierarchy;
            if (visible)
                visible = PositionLabel(playerLabel, playerOwner.position + Vector3.up * playerHeadOffset);
            SetLabelActive(playerLabel, visible);
            if (visible)
            {
                playerLabel.color = settings.playerColor;
                playerLabel.fontSize = GetLevelFontSize(settings.playerFontSize, playerLevel);
            }
        }

        private void UpdateTargetLabel(NumberCombatTarget target)
        {
            if (target == null || target.Label == null || target.Owner == null || target.Resolved)
            {
                if (target != null) SetLabelActive(target.Label, false);
                return;
            }

            target.Label.fontSize = GetLevelFontSize(settings.targetFontSize, target.FixedLevel);

            float distanceAhead = playerOwner != null
                ? target.Owner.position.z - playerOwner.position.z
                : float.MaxValue;
            bool visible = distanceAhead <= settings.targetLabelPreviewDistance
                && target.Owner.gameObject.activeInHierarchy
                && IsVisibilityStateRenderable(target.Visibility);
            if (visible)
                visible = PositionLabel(target.Label, target.Owner.position + Vector3.up * target.HeadOffset);
            SetLabelActive(target.Label, visible);
            if (visible) ApplyTargetColor(target);
        }

        private bool PositionLabel(Text label, Vector3 worldPosition)
        {
            if (label == null) return false;
            Vector3 screenPoint = gameCamera.WorldToScreenPoint(worldPosition);
            float padding = settings.labelWidth * 0.5f;
            if (screenPoint.z <= gameCamera.nearClipPlane
                || screenPoint.z > gameCamera.farClipPlane
                || screenPoint.x < -padding || screenPoint.x > Screen.width + padding
                || screenPoint.y < -settings.labelHeight || screenPoint.y > Screen.height + settings.labelHeight)
                return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null,
                    out Vector2 localPoint))
                return false;
            label.rectTransform.anchoredPosition = localPoint;
            return true;
        }

        private void RefreshPlayerLabel()
        {
            if (playerLabel == null) return;
            playerLabel.text = FormatLevel(playerLevel);
            playerLabel.color = settings.playerColor;
            playerLabel.fontSize = GetLevelFontSize(settings.playerFontSize, playerLevel);
        }

        private void RefreshTargetColors()
        {
            for (int i = 0; i < targets.Count; i++)
                ApplyTargetColor(targets[i]);
        }

        private void ApplyTargetColor(NumberCombatTarget target)
        {
            if (target == null || target.Label == null) return;
            target.Label.fontSize = GetLevelFontSize(settings.targetFontSize, target.FixedLevel);
            target.Label.color = target.FixedLevel <= playerLevel
                ? settings.beatableTargetColor
                : settings.strongerColor;
        }

        private int GetLevelFontSize(int baseFontSize, int level)
        {
            float normalizedLevel = Mathf.InverseLerp(1f, 10f, Mathf.Clamp(level, 1, 10));
            float maxScale = settings != null ? Mathf.Max(1f, settings.levelTenFontScale) : 1.35f;
            float scale = Mathf.Lerp(1f, maxScale, normalizedLevel);
            return Mathf.Max(1, Mathf.RoundToInt(baseFontSize * scale));
        }

        private void SyncPlayerLevel(bool force = false)
        {
            if (!initialized && !force) return;
            int nextLevel = GetCurrentPlayerLevel();
            if (!force && nextLevel == playerLevel) return;
            playerLevel = nextLevel;
            RefreshPlayerLabel();
            RefreshTargetColors();
            PlayerLevelChanged?.Invoke(playerLevel);
        }

        private int GetCurrentPlayerLevel()
        {
            if (speedController != null)
                return Mathf.Clamp(speedController.GetCurrentLevel(), 1, 10);
            return 1;
        }

        private static bool IsVisibilityStateRenderable(EnemyVisibilityController visibility)
        {
            if (visibility == null) return true;
            return visibility.State == EnemyVisibilityState.DistantVisible
                   || visibility.State == EnemyVisibilityState.Active
                   || visibility.State == EnemyVisibilityState.KnockedBack;
        }

        private static float CalculateHeadOffset(Transform owner, Renderer[] renderers, float clearance,
            float fallbackHeight)
        {
            if (owner == null) return Mathf.Max(0.1f, fallbackHeight + clearance);

            float maximumY = float.NegativeInfinity;
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null || renderer is ParticleSystemRenderer
                        || renderer is TrailRenderer || renderer is LineRenderer)
                        continue;
                    maximumY = Mathf.Max(maximumY, renderer.bounds.max.y);
                }
            }

            float height = float.IsNegativeInfinity(maximumY)
                ? fallbackHeight
                : maximumY - owner.position.y;
            return Mathf.Max(0.1f, height + Mathf.Max(0f, clearance));
        }

        private static void SetLabelActive(Text label, bool active)
        {
            if (label != null && label.gameObject.activeSelf != active)
                label.gameObject.SetActive(active);
        }

        private static string FormatLevel(int value)
        {
            return "LV" + value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
