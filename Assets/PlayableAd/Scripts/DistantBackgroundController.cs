using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class DistantBackgroundController : MonoBehaviour
    {
        private const string BackgroundRootName = "BackgroundRoot";
        private const string BackgroundName = "FantasyCityBackground";
        private const string FogName = "FogExtension";
        private const string ForegroundFogRootName = "FlowingForegroundFog";

        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite fogSprite;
        [SerializeField, Range(0f, 1f)] private float horizontalFocus = 0.58f;
        [SerializeField, Range(1f, 1.2f)] private float verticalOverscan = 1.08f;
        [SerializeField, Range(-0.2f, 0.2f)] private float verticalOffset = 0.04f;
        [SerializeField, Range(0.5f, 1f)] private float brightness = 0.84f;
        [SerializeField, Range(0.2f, 0.7f)] private float fogHeightFraction = 0.48f;
        [Header("Flowing Foreground Fog")]
        [SerializeField] private bool enableForegroundFog = true;
        [SerializeField, Range(0.1f, 0.75f)] private float foregroundFogOpacity = 0.48f;
        [SerializeField, Range(0.01f, 0.2f)] private float foregroundFogDrift = 0.07f;

        private Camera targetCamera;
        private Transform backgroundRoot;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer fogRenderer;
        private Transform foregroundFogRoot;
        private readonly SpriteRenderer[] foregroundFogRenderers = new SpriteRenderer[2];
        private Texture2D foregroundFogTexture;
        private Sprite foregroundFogSprite;
        private float foregroundViewWidth;
        private float foregroundViewHeight;
        private float foregroundBaseScale;
        private int lastWidth;
        private int lastHeight;
        private float lastFieldOfView;
        private float lastOrthographicSize;

        private void OnEnable()
        {
            EnsureHierarchy();
            FitToCamera(true);
        }

        private void LateUpdate()
        {
            if (targetCamera == null || backgroundRoot == null)
            {
                EnsureHierarchy();
                FitToCamera(true);
                return;
            }

            bool cameraChanged = lastWidth != Screen.width || lastHeight != Screen.height
                || !Mathf.Approximately(lastFieldOfView, targetCamera.fieldOfView)
                || !Mathf.Approximately(lastOrthographicSize, targetCamera.orthographicSize);
            if (cameraChanged)
                FitToCamera(false);
            UpdateForegroundFog();
        }

        public void Configure(Sprite background, Sprite fog)
        {
            backgroundSprite = background;
            fogSprite = fog;
        }

        private void EnsureHierarchy()
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;

            backgroundRoot = targetCamera.transform.Find(BackgroundRootName);
            if (backgroundRoot == null)
            {
                backgroundRoot = new GameObject(BackgroundRootName).transform;
                backgroundRoot.SetParent(targetCamera.transform, false);
            }

            backgroundRenderer = GetOrCreateRenderer(backgroundRoot, BackgroundName, -1000);
            fogRenderer = GetOrCreateRenderer(backgroundRoot, FogName, -999);
            backgroundRenderer.sprite = backgroundSprite;
            backgroundRenderer.color = new Color(brightness, brightness, Mathf.Min(1f, brightness + 0.04f), 1f);
            fogRenderer.sprite = fogSprite;
            fogRenderer.color = Color.white;
            EnsureForegroundFog();
        }

        private void EnsureForegroundFog()
        {
            if (!Application.isPlaying || !enableForegroundFog || targetCamera == null) return;
            foregroundFogRoot = targetCamera.transform.Find(ForegroundFogRootName);
            if (foregroundFogRoot == null)
            {
                foregroundFogRoot = new GameObject(ForegroundFogRootName).transform;
                foregroundFogRoot.SetParent(targetCamera.transform, false);
            }

            if (foregroundFogSprite == null)
                foregroundFogSprite = CreateForegroundFogSprite();

            foregroundFogRenderers[0] = GetOrCreateForegroundRenderer(
                foregroundFogRoot, "FogDriftNear", -997);
            foregroundFogRenderers[1] = GetOrCreateForegroundRenderer(
                foregroundFogRoot, "FogDriftFar", -998);
            for (int i = 0; i < foregroundFogRenderers.Length; i++)
                foregroundFogRenderers[i].sprite = foregroundFogSprite;

            foregroundFogRenderers[0].color = new Color(0.7f, 0.77f, 0.87f, foregroundFogOpacity);
            foregroundFogRenderers[1].color = new Color(0.6f, 0.69f, 0.82f, foregroundFogOpacity * 0.52f);
        }

        private static SpriteRenderer GetOrCreateForegroundRenderer(Transform parent, string name, int order)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return renderer;
        }

        private static SpriteRenderer GetOrCreateRenderer(Transform parent, string name, int order)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return renderer;
        }

        private void FitToCamera(bool force)
        {
            if (targetCamera == null || backgroundRoot == null || backgroundRenderer == null || backgroundSprite == null)
                return;

            float distance = Mathf.Max(targetCamera.nearClipPlane + 10f, targetCamera.farClipPlane - 8f);
            float viewHeight = targetCamera.orthographic
                ? targetCamera.orthographicSize * 2f
                : 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float viewWidth = viewHeight * targetCamera.aspect;
            Vector2 sourceSize = backgroundSprite.bounds.size;
            float uniformScale = Mathf.Max(viewWidth / sourceSize.x, viewHeight / sourceSize.y) * verticalOverscan;
            float renderedWidth = sourceSize.x * uniformScale;
            float focusOffset = (horizontalFocus - 0.5f) * renderedWidth;

            backgroundRoot.localPosition = new Vector3(0f, 0f, distance);
            backgroundRoot.localRotation = Quaternion.identity;
            backgroundRoot.localScale = Vector3.one;

            Transform image = backgroundRenderer.transform;
            image.localPosition = new Vector3(-focusOffset, viewHeight * verticalOffset, 0f);
            image.localRotation = Quaternion.identity;
            image.localScale = Vector3.one * uniformScale;

            if (fogRenderer != null && fogSprite != null)
            {
                Vector2 fogSize = fogSprite.bounds.size;
                float fogHeight = viewHeight * fogHeightFraction;
                Transform fog = fogRenderer.transform;
                fog.localPosition = new Vector3(0f, -viewHeight * (0.5f - fogHeightFraction * 0.5f), -1f);
                fog.localRotation = Quaternion.identity;
                fog.localScale = new Vector3(viewWidth * 1.08f / fogSize.x, fogHeight / fogSize.y, 1f);
            }

            FitForegroundFog();

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastFieldOfView = targetCamera.fieldOfView;
            lastOrthographicSize = targetCamera.orthographicSize;
        }

        private void FitForegroundFog()
        {
            if (!enableForegroundFog || targetCamera == null || foregroundFogRoot == null
                || foregroundFogSprite == null) return;

            // Keep the moving clouds at background depth so world geometry always occludes them.
            float distance = Mathf.Max(targetCamera.nearClipPlane + 10f, targetCamera.farClipPlane - 10f);
            foregroundViewHeight = targetCamera.orthographic
                ? targetCamera.orthographicSize * 2f
                : 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            foregroundViewWidth = foregroundViewHeight * targetCamera.aspect;
            Vector2 spriteSize = foregroundFogSprite.bounds.size;
            foregroundBaseScale = Mathf.Max(
                foregroundViewWidth * 1.28f / spriteSize.x,
                foregroundViewHeight * 1.2f / spriteSize.y);

            foregroundFogRoot.localPosition = new Vector3(0f, 0f, distance);
            foregroundFogRoot.localRotation = Quaternion.identity;
            foregroundFogRoot.localScale = Vector3.one;
            UpdateForegroundFog();
        }

        private void UpdateForegroundFog()
        {
            if (!enableForegroundFog || foregroundFogRoot == null || foregroundBaseScale <= 0f) return;
            float time = Time.unscaledTime * Mathf.Max(0.01f, foregroundFogDrift) * 10f;

            Transform near = foregroundFogRenderers[0] != null ? foregroundFogRenderers[0].transform : null;
            if (near != null)
            {
                near.localPosition = new Vector3(
                    Mathf.Sin(time * 0.31f) * foregroundViewWidth * 0.042f,
                    Mathf.Cos(time * 0.23f) * foregroundViewHeight * 0.018f,
                    0f);
                near.localScale = Vector3.one * foregroundBaseScale
                    * (1f + Mathf.Sin(time * 0.17f) * 0.018f);
            }

            Transform far = foregroundFogRenderers[1] != null ? foregroundFogRenderers[1].transform : null;
            if (far != null)
            {
                far.localPosition = new Vector3(
                    Mathf.Sin(time * 0.19f + 2.1f) * foregroundViewWidth * 0.058f,
                    Mathf.Sin(time * 0.27f + 0.8f) * foregroundViewHeight * 0.024f,
                    0.01f);
                far.localScale = Vector3.one * foregroundBaseScale * 1.045f
                    * (1f + Mathf.Cos(time * 0.13f) * 0.022f);
            }
        }

        private Sprite CreateForegroundFogSprite()
        {
            const int width = 192;
            const int height = 384;
            foregroundFogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Flowing Foreground Fog",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float vertical = y / (height - 1f);
                float clearHalfWidth = Mathf.Lerp(0.68f, 0.26f, vertical);
                for (int x = 0; x < width; x++)
                {
                    float horizontal = x / (width - 1f) * 2f - 1f;
                    float edgeDistance = Mathf.Abs(horizontal);
                    float largeNoise = Mathf.PerlinNoise(x * 0.026f + 3.7f, y * 0.018f + 8.4f);
                    float mediumNoise = Mathf.PerlinNoise(x * 0.061f + 15.2f, y * 0.048f + 2.6f);
                    float fineNoise = Mathf.PerlinNoise(x * 0.13f + 5.1f, y * 0.105f + 19.3f);
                    float noise = largeNoise * 0.52f + mediumNoise * 0.32f + fineNoise * 0.16f;

                    float unevenBoundary = clearHalfWidth + (largeNoise - 0.5f) * 0.1f;
                    float sideFog = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(unevenBoundary + 0.01f, unevenBoundary + 0.19f, edgeDistance));
                    float lowerWeight = Mathf.Lerp(1.18f, 0.7f, vertical);
                    float cloudBody = sideFog * Mathf.Lerp(0.56f, 1.08f, noise) * lowerWeight;
                    float boundaryWisp = Mathf.Clamp01(1f
                        - Mathf.Abs(edgeDistance - (unevenBoundary + 0.08f)) / 0.13f)
                        * Mathf.Lerp(0.08f, 0.28f, mediumNoise);
                    float alpha = Mathf.Clamp01(cloudBody + boundaryWisp);

                    float brightnessNoise = (noise - 0.5f) * 0.12f;
                    Color color = new Color(
                        0.63f + brightnessNoise,
                        0.69f + brightnessNoise,
                        0.79f + brightnessNoise * 0.75f,
                        alpha);
                    pixels[y * width + x] = color;
                }
            }
            foregroundFogTexture.SetPixels32(pixels);
            foregroundFogTexture.Apply(false, true);
            foregroundFogSprite = Sprite.Create(foregroundFogTexture,
                new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            foregroundFogSprite.name = "Runtime Flowing Foreground Fog";
            foregroundFogSprite.hideFlags = HideFlags.DontSave;
            return foregroundFogSprite;
        }

        private void OnDestroy()
        {
            if (foregroundFogSprite != null)
            {
                if (Application.isPlaying) Destroy(foregroundFogSprite);
                else DestroyImmediate(foregroundFogSprite);
            }
            if (foregroundFogTexture != null)
            {
                if (Application.isPlaying) Destroy(foregroundFogTexture);
                else DestroyImmediate(foregroundFogTexture);
            }
        }
    }
}
