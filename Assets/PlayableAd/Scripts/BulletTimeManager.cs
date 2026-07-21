using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PlayableAd
{
    [Serializable]
    public sealed class BulletTimeSettings
    {
        [InspectorName("Enabled（启用子弹时间）")] public bool enabled = true;
        [Min(0f), InspectorName("Trigger Distance（触发距离）")] public float triggerDistance = 3f;
        [Min(0f), InspectorName("Duration（触发到恢复的总时间）")] public float duration = 0.55f;
        [Range(0.01f, 1f), InspectorName("World Time Scale（世界时间倍率）")] public float worldTimeScale = 0.25f;
        [Min(0f), InspectorName("Enter Duration（进入时长）")] public float enterDuration = 0.2f;
        [Min(0.1f), InspectorName("Exit Duration（退出时长）")] public float exitDuration = 0.15f;

        public BulletTimeSettings Clone()
        {
            return new BulletTimeSettings
            {
                enabled = enabled,
                triggerDistance = triggerDistance,
                duration = duration,
                worldTimeScale = worldTimeScale,
                enterDuration = enterDuration,
                exitDuration = exitDuration
            };
        }
    }

    [DisallowMultipleComponent]
    public sealed class BulletTimeManager : MonoBehaviour
    {
        private enum State
        {
            Inactive,
            Entering,
            Holding,
            Exiting
        }

        private static BulletTimeManager instance;

        [Header("Default Bullet Time Values（默认子弹时间数值）")]
        [SerializeField, InspectorName("Default Settings（默认参数）")] private BulletTimeSettings defaultSettings = new BulletTimeSettings();

        [Header("Overlay Feedback（覆盖层反馈）")]
        [SerializeField, Range(0f, 1f), InspectorName("Warm Tint Strength（暖色滤镜强度）")] private float warmTintStrength = 0.22f;
        [FormerlySerializedAs("desaturationStrength")]
        [SerializeField, Range(0f, 1f), InspectorName("Additional Tint Strength（附加滤镜强度）")] private float additionalTintStrength = 0.18f;
        [SerializeField, Range(0f, 1f), InspectorName("Vignette Strength（暗角强度）")] private float vignetteStrength = 0.28f;

        private State state;
        private BulletTimeSettings activeSettings;
        private float currentScale = 1f;
        private float transitionStartScale = 1f;
        private float elapsed;
        private float exitElapsed;
        private float exitStartScale = 1f;
        private Image tintImage;
        private RawImage vignetteImage;
        private Texture2D vignetteTexture;

        public static BulletTimeManager Instance => instance;
        public float WorldTimeScale => currentScale;
        public bool IsBulletTimeActive => state != State.Inactive;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            currentScale = 1f;
            EnsureOverlay();
            UpdateOverlay();
        }

        private void Update()
        {
            float unscaledDeltaTime = Time.unscaledDeltaTime;
            switch (state)
            {
                case State.Entering:
                    elapsed += unscaledDeltaTime;
                    float enterT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, activeSettings.enterDuration));
                    currentScale = Mathf.Lerp(transitionStartScale, activeSettings.worldTimeScale, Mathf.SmoothStep(0f, 1f, enterT));
                    if (elapsed >= activeSettings.enterDuration)
                    {
                        currentScale = activeSettings.worldTimeScale;
                        state = State.Holding;
                    }
                    if (elapsed >= activeSettings.duration) BeginExit();
                    break;

                case State.Holding:
                    elapsed += unscaledDeltaTime;
                    currentScale = activeSettings.worldTimeScale;
                    if (elapsed >= activeSettings.duration) BeginExit();
                    break;

                case State.Exiting:
                    exitElapsed += unscaledDeltaTime;
                    float exitT = Mathf.Clamp01(exitElapsed / Mathf.Max(0.0001f, activeSettings.exitDuration));
                    currentScale = Mathf.Lerp(exitStartScale, 1f, Mathf.SmoothStep(0f, 1f, exitT));
                    if (exitElapsed >= activeSettings.exitDuration)
                    {
                        currentScale = 1f;
                        state = State.Inactive;
                    }
                    break;
            }

            UpdateOverlay();
        }

        public bool IsBulletTime()
        {
            return IsBulletTimeActive;
        }

        public float GetWorldDeltaTime()
        {
            // Existing impact hit-stop still owns Time.timeScale outside Bullet Time.
            // While this module is active, real time keeps the two slow-motion systems from multiplying.
            return state == State.Inactive
                ? Time.deltaTime
                : Time.unscaledDeltaTime * currentScale;
        }

        public void StartBulletTime(float duration)
        {
            BulletTimeSettings settings = defaultSettings != null
                ? defaultSettings.Clone()
                : new BulletTimeSettings();
            settings.duration = Mathf.Max(0f, duration);
            StartBulletTime(settings);
        }

        public void StartBulletTime(BulletTimeSettings settings)
        {
            if (settings == null || !settings.enabled) return;

            activeSettings = settings.Clone();
            activeSettings.worldTimeScale = Mathf.Clamp(activeSettings.worldTimeScale, 0.01f, 1f);
            activeSettings.enterDuration = Mathf.Max(0f, activeSettings.enterDuration);
            activeSettings.exitDuration = Mathf.Clamp(activeSettings.exitDuration, 0.1f, 0.2f);
            activeSettings.duration = Mathf.Max(0f, activeSettings.duration);
            transitionStartScale = currentScale;
            elapsed = 0f;
            exitElapsed = 0f;
            state = State.Entering;
            if (activeSettings.enterDuration <= 0f)
            {
                currentScale = activeSettings.worldTimeScale;
                state = State.Holding;
            }
        }

        public void StopBulletTime()
        {
            if (state == State.Inactive) return;
            BeginExit();
        }

        private void BeginExit()
        {
            if (state == State.Exiting || state == State.Inactive) return;
            state = State.Exiting;
            exitStartScale = currentScale;
            exitElapsed = 0f;
        }

        private void EnsureOverlay()
        {
            GameObject canvasObject = new GameObject("BulletTimeOverlay");
            canvasObject.transform.SetParent(transform, false);
            Canvas overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 40;

            GameObject tintObject = new GameObject("WarmDesaturationTint");
            tintObject.transform.SetParent(canvasObject.transform, false);
            tintImage = tintObject.AddComponent<Image>();
            tintImage.color = new Color(0.68f, 0.56f, 0.34f, 0f);
            tintImage.raycastTarget = false;
            SetFullScreen(tintObject.GetComponent<RectTransform>());

            GameObject vignetteObject = new GameObject("Vignette");
            vignetteObject.transform.SetParent(canvasObject.transform, false);
            vignetteImage = vignetteObject.AddComponent<RawImage>();
            vignetteImage.color = new Color(0.04f, 0.025f, 0.01f, 0f);
            vignetteImage.raycastTarget = false;
            vignetteImage.texture = BuildVignetteTexture();
            SetFullScreen(vignetteObject.GetComponent<RectTransform>());
        }

        private static void SetFullScreen(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Texture2D BuildVignetteTexture()
        {
            const int size = 96;
            vignetteTexture = new Texture2D(size, size, TextureFormat.Alpha8, false);
            vignetteTexture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x / (size - 1f) - 0.5f) * 2f;
                    float dy = (y / (size - 1f) - 0.5f) * 2f;
                    float edge = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.3f) / 0.7f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(edge * 255f));
                }
            }
            vignetteTexture.SetPixels32(pixels);
            vignetteTexture.Apply(false, true);
            return vignetteTexture;
        }

        private void UpdateOverlay()
        {
            if (tintImage == null || vignetteImage == null) return;
            float amount = Mathf.Clamp01(1f - currentScale);
            tintImage.color = new Color(0.68f, 0.56f, 0.34f,
                amount * Mathf.Clamp01(warmTintStrength + additionalTintStrength * 0.35f));
            vignetteImage.color = new Color(0.04f, 0.025f, 0.01f, amount * vignetteStrength);
        }

        private void OnDisable()
        {
            currentScale = 1f;
            state = State.Inactive;
            UpdateOverlay();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (vignetteTexture != null) Destroy(vignetteTexture);
        }
    }
}
