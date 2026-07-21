using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class DistantBackgroundController : MonoBehaviour
    {
        private const string BackgroundRootName = "BackgroundRoot";
        private const string BackgroundName = "FantasyCityBackground";
        private const string FogName = "FogExtension";

        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite fogSprite;
        [SerializeField, Range(0f, 1f)] private float horizontalFocus = 0.58f;
        [SerializeField, Range(1f, 1.2f)] private float verticalOverscan = 1.08f;
        [SerializeField, Range(-0.2f, 0.2f)] private float verticalOffset = 0.04f;
        [SerializeField, Range(0.5f, 1f)] private float brightness = 0.84f;
        [SerializeField, Range(0.2f, 0.7f)] private float fogHeightFraction = 0.48f;

        private Camera targetCamera;
        private Transform backgroundRoot;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer fogRenderer;
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

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastFieldOfView = targetCamera.fieldOfView;
            lastOrthographicSize = targetCamera.orthographicSize;
        }
    }
}
