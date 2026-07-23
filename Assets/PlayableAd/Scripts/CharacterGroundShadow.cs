using UnityEngine;
using UnityEngine.Rendering;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class CharacterGroundShadow : MonoBehaviour
    {
        private const float DefaultGroundY = 0.035f;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static Mesh sharedMesh;
        private static Material sharedMaterial;

        private Transform shadowTransform;
        private MeshRenderer shadowRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Vector2 shadowSize = new Vector2(1.1f, 0.5f);
        private Vector3 lightGroundDirection = Vector3.forward;
        private Color shadowColor = new Color(0.055f, 0.075f, 0.105f, 0.3f);
        private float groundY = DefaultGroundY;
        private float standingY;
        private bool initialized;

        public void Initialize(Vector2 size, float shadowStrength, Vector3 lightEuler,
            float worldGroundY = DefaultGroundY)
        {
            shadowSize = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
            shadowColor.a = Mathf.Lerp(0.18f, 0.38f, Mathf.Clamp01(shadowStrength));
            groundY = worldGroundY;
            standingY = transform.position.y;

            Vector3 lightDirection = Quaternion.Euler(lightEuler) * Vector3.forward;
            lightGroundDirection = Vector3.ProjectOnPlane(lightDirection, Vector3.up).normalized;
            if (lightGroundDirection.sqrMagnitude < 0.001f)
                lightGroundDirection = Vector3.forward;

            initialized = true;
            EnsureShadow();
            ApplyVisual(1f);
            UpdateTransform();
        }

        private void OnEnable()
        {
            if (shadowTransform != null)
                shadowTransform.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            EnsureShadow();
            UpdateTransform();

            float lift = Mathf.Max(0f, transform.position.y - standingY);
            float visibility = Mathf.Clamp01(1f - lift / 7f);
            ApplyVisual(visibility);
        }

        private void OnDisable()
        {
            if (shadowTransform != null)
                shadowTransform.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (shadowTransform == null) return;
            if (Application.isPlaying) Destroy(shadowTransform.gameObject);
            else DestroyImmediate(shadowTransform.gameObject);
        }

        private void EnsureShadow()
        {
            if (shadowTransform != null) return;
            EnsureSharedResources();

            GameObject shadow = new GameObject(name + "_GroundShadow");
            shadow.layer = gameObject.layer;
            shadow.transform.SetParent(transform.parent, true);
            MeshFilter filter = shadow.AddComponent<MeshFilter>();
            filter.sharedMesh = sharedMesh;
            shadowRenderer = shadow.AddComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = sharedMaterial;
            shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;
            shadowRenderer.lightProbeUsage = LightProbeUsage.Off;
            shadowRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            shadowRenderer.sortingOrder = -20;
            propertyBlock = new MaterialPropertyBlock();
            shadowTransform = shadow.transform;
            shadow.SetActive(isActiveAndEnabled);
        }

        private void UpdateTransform()
        {
            float offset = Mathf.Max(0.12f, shadowSize.y * 0.3f);
            Vector3 position = transform.position + lightGroundDirection * offset;
            position.y = groundY;
            shadowTransform.SetPositionAndRotation(position, Quaternion.identity);
            shadowTransform.localScale = new Vector3(shadowSize.x, 1f, shadowSize.y);
        }

        private void ApplyVisual(float visibility)
        {
            if (shadowRenderer == null) return;
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
            float liftExpansion = Mathf.Lerp(1.18f, 1f, visibility);
            shadowTransform.localScale = new Vector3(
                shadowSize.x * liftExpansion, 1f, shadowSize.y * liftExpansion);
            Color color = shadowColor;
            color.a *= visibility;
            propertyBlock.SetColor(ColorId, color);
            shadowRenderer.SetPropertyBlock(propertyBlock);
        }

        private static void EnsureSharedResources()
        {
            if (sharedMesh == null)
            {
                sharedMesh = new Mesh { name = "Runtime Character Ground Shadow" };
                sharedMesh.vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f)
                };
                sharedMesh.uv = new[]
                {
                    new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0f, 1f), new Vector2(1f, 1f)
                };
                sharedMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                sharedMesh.RecalculateNormals();
                sharedMesh.RecalculateBounds();
                sharedMesh.hideFlags = HideFlags.DontSave;
            }

            if (sharedMaterial != null) return;
            Shader shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            sharedMaterial = new Material(shader)
            {
                name = "Runtime Character Ground Shadow",
                mainTexture = CreateSoftOvalTexture(),
                renderQueue = 3000,
                hideFlags = HideFlags.DontSave
            };
        }

        private static Texture2D CreateSoftOvalTexture()
        {
            const int resolution = 64;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Soft Oval Shadow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            Color[] pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = (x + 0.5f) / resolution * 2f - 1f;
                    float ny = (y + 0.5f) / resolution * 2f - 1f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.65f);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
