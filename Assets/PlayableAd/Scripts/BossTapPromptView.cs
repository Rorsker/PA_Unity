using UnityEngine;
using UnityEngine.UI;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class BossTapPromptView : MonoBehaviour
    {
        private BossClashSettings settings;
        private RectTransform safeAreaRoot;
        private RectTransform handRect;
        private Image handImage;
        private CanvasGroup canvasGroup;
        private Rect lastSafeArea;
        private bool visible;
        private float tapPunch;

        public void Initialize(Sprite handSprite, BossClashSettings clashSettings)
        {
            settings = clashSettings ?? new BossClashSettings();

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            safeAreaRoot = CreateRect("SafeArea", transform);
            handRect = CreateRect("RapidTapHand", safeAreaRoot);
            handRect.anchorMin = handRect.anchorMax = settings.tapHintScreenAnchor;
            handRect.pivot = new Vector2(0.5f, 0.5f);
            handRect.anchoredPosition = settings.tapHintScreenOffset;
            handRect.sizeDelta = settings.tapHintSize;

            handImage = handRect.gameObject.AddComponent<Image>();
            handImage.sprite = handSprite;
            handImage.preserveAspect = true;
            handImage.raycastTarget = false;
            handImage.enabled = handSprite != null;

            Shadow shadow = handRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
            shadow.effectDistance = new Vector2(4f, -5f);
            shadow.useGraphicAlpha = true;

            ApplySafeArea();
        }

        public void Show()
        {
            visible = handImage != null && handImage.sprite != null;
            tapPunch = 0f;
            if (handImage != null) handImage.enabled = visible;
        }

        public void RegisterTap()
        {
            if (!visible) return;
            tapPunch = 1f;
        }

        public void Hide(bool immediate = false)
        {
            visible = false;
            tapPunch = 0f;
            if (immediate && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                if (handImage != null) handImage.enabled = false;
            }
        }

        private void Update()
        {
            if (canvasGroup == null || handRect == null) return;
            if (Screen.safeArea != lastSafeArea) ApplySafeArea();

            float deltaTime = Time.unscaledDeltaTime;
            float fadeResponse = 1f - Mathf.Exp(-14f * deltaTime);
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, visible ? 1f : 0f, fadeResponse);
            if (!visible && canvasGroup.alpha <= 0.002f)
            {
                canvasGroup.alpha = 0f;
                if (handImage != null) handImage.enabled = false;
                return;
            }

            float pulsePhase = Time.unscaledTime * Mathf.Max(0.1f, settings.tapHintPulseSpeed)
                * Mathf.PI * 2f;
            float idleWave = Mathf.Sin(pulsePhase);
            float scale = 1f + idleWave * Mathf.Max(0f, settings.tapHintPulseScale)
                - tapPunch * Mathf.Clamp01(settings.tapHintTapCompression);
            float bob = idleWave * Mathf.Max(0f, settings.tapHintBobDistance)
                - tapPunch * Mathf.Max(0f, settings.tapHintTapDrop);
            handRect.anchoredPosition = settings.tapHintScreenOffset + Vector2.up * bob;
            handRect.localScale = Vector3.one * Mathf.Max(0.1f, scale);

            float tapResponse = Mathf.Max(0.02f, settings.tapHintTapResponse);
            tapPunch = Mathf.MoveTowards(tapPunch, 0f, deltaTime / tapResponse);
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safeArea = Screen.safeArea;
            safeAreaRoot.anchorMin = new Vector2(
                safeArea.xMin / Screen.width,
                safeArea.yMin / Screen.height);
            safeAreaRoot.anchorMax = new Vector2(
                safeArea.xMax / Screen.width,
                safeArea.yMax / Screen.height);
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
