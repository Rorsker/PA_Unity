using System;
using System.Collections;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class ObstacleOutlineSettings
    {
        [InspectorName("Enabled（启用轮廓）")] public bool enabled = true;
        [InspectorName("Gain Color（增益颜色）")] public Color gainColor = new Color(0.04f, 1f, 0.48f, 0.92f);
        [InspectorName("Neutral Color（中性颜色）")] public Color neutralColor = new Color(0.68f, 0.86f, 1f, 0.84f);
        [InspectorName("Danger Color（危险颜色）")] public Color dangerColor = new Color(0.46f, 0.018f, 0.025f, 0.86f);
        [Range(0.015f, 0.12f), InspectorName("Base Width（基础宽度）")] public float baseWidth = 0.045f;
        [Range(0.1f, 0.2f), InspectorName("Transition Duration（过渡时长）")] public float transitionDuration = 0.15f;
        [Range(0.5f, 4f), InspectorName("Danger Pulse Speed（危险脉冲速度）")] public float dangerPulseSpeed = 1.8f;
        [Range(0f, 0.4f), InspectorName("Danger Pulse Amount（危险脉冲幅度）")] public float dangerPulseAmount = 0.18f;
        [Range(0.2f, 1f), InspectorName("Max Scene Brightness（场景最大亮度）")] public float maxSceneBrightness = 0.82f;
        [InspectorName("Enhanced Outcome Cues（增强结果提示）")] public bool enhancedOutcomeCues;
        [InspectorName("Outline Material（轮廓材质）")] public Material outlineMaterial;
    }

    public sealed class ObstacleOutline : MonoBehaviour
    {
        private static Material fallbackSharedMaterial;
        private static readonly int ColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int WidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int DangerId = Shader.PropertyToID("_Danger");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int FlowDirectionId = Shader.PropertyToID("_FlowDirection");

        private ObstacleOutlineSettings settings;
        private PlayerSpeedController speedController;
        private ObstacleController obstacle;
        private Renderer[] outlineRenderers = Array.Empty<Renderer>();
        private MaterialPropertyBlock propertyBlock;
        private Coroutine transitionRoutine;
        private int lastPlayerLevel = -1;
        private bool subscribed;
        private float previewStrength = 1f;

        public bool IsSafe { get; private set; }
        public CollisionOutcome CurrentOutcome { get; private set; } = CollisionOutcome.Neutral;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedMaterials()
        {
            DestroySharedMaterial(ref fallbackSharedMaterial);
        }

        private static void DestroySharedMaterial(ref Material material)
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
            material = null;
        }

        public void Initialize(ObstacleOutlineSettings outlineSettings, PlayerSpeedController controller,
            ObstacleController targetObstacle, Renderer[] visualSources = null)
        {
            settings = outlineSettings;
            speedController = controller;
            obstacle = targetObstacle;
            propertyBlock = new MaterialPropertyBlock();

            Material material = settings.outlineMaterial;
            if (material == null)
            {
                if (fallbackSharedMaterial == null)
                {
                    Shader shader = Shader.Find("PlayableAd/ObstacleOutline");
                    fallbackSharedMaterial = new Material(shader) { name = "SharedObstacleOutlineFallback" };
                }
                material = fallbackSharedMaterial;
            }

            if (visualSources != null && visualSources.Length > 0)
            {
                // Separate shells on a skinned character expose internal and back-facing mesh edges.
                // Keep collision outcome tracking without adding a ground ring to soldiers.
                outlineRenderers = Array.Empty<Renderer>();
            }
            else
            {
                MeshFilter source = GetComponent<MeshFilter>();
                if (source == null || source.sharedMesh == null)
                {
                    enabled = false;
                    return;
                }

                GameObject outline = new GameObject("RiskOutline");
                outline.transform.SetParent(transform, false);
                MeshFilter meshFilter = outline.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = source.sharedMesh;
                MeshRenderer meshRenderer = outline.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                outlineRenderers = new Renderer[] { meshRenderer };
            }
            SetPreviewActive(gameObject.activeInHierarchy);
        }

        public void SetPreviewActive(bool active)
        {
            SetPreviewPresentation(active, active ? 1f : 0f);
        }

        public void SetPreviewPresentation(bool visible, float strength)
        {
            if (obstacle == null || obstacle.HasResolved) visible = false;
            previewStrength = Mathf.Clamp01(strength);
            SetOutlineRenderersEnabled(visible && settings.enabled);
            if (visible && !subscribed)
            {
                speedController.SpeedChanged += OnSpeedChanged;
                obstacle.Resolved += OnObstacleResolved;
                subscribed = true;
                Refresh(speedController.GetCurrentLevel(), false);
            }
            else if (!visible && subscribed)
            {
                Unsubscribe();
            }
            if (visible && subscribed) Refresh(speedController.GetCurrentLevel(), false);
        }

        private void OnSpeedChanged(SpeedChangedEvent change)
        {
            if (change.NewLevel != lastPlayerLevel) Refresh(change.NewLevel, true);
        }

        private void Refresh(int playerLevel, bool smooth)
        {
            if (obstacle == null) return;
            lastPlayerLevel = playerLevel;
            CurrentOutcome = ObstacleController.EvaluateCollisionOutcome(playerLevel, obstacle.RequiredSpeedLevel);
            IsSafe = CurrentOutcome != CollisionOutcome.SpeedLoss;
            if (outlineRenderers.Length == 0) return;
            SetOutlineRenderersEnabled(settings.enabled && !obstacle.HasResolved);
            Color color = GetOutcomeColor(CurrentOutcome);
            color *= settings.maxSceneBrightness;
            color.a = GetOutcomeColor(CurrentOutcome).a;
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            if (smooth && isActiveAndEnabled)
                transitionRoutine = StartCoroutine(TransitionVisual(color));
            else
                ApplyVisual(color);
        }

        private IEnumerator TransitionVisual(Color targetColor)
        {
            if (outlineRenderers.Length > 0 && outlineRenderers[0] != null)
                outlineRenderers[0].GetPropertyBlock(propertyBlock);
            Color start = propertyBlock.GetColor(ColorId);
            float timer = 0f;
            while (timer < settings.transitionDuration)
            {
                timer += Time.unscaledDeltaTime;
                ApplyVisual(Color.Lerp(start, targetColor, Mathf.Clamp01(timer / settings.transitionDuration)));
                yield return null;
            }
            ApplyVisual(targetColor);
            transitionRoutine = null;
        }

        private void ApplyVisual(Color color)
        {
            color.a *= Mathf.Lerp(0.34f, 1f, previewStrength);
            for (int i = 0; i < outlineRenderers.Length; i++)
            {
                Renderer outlineRenderer = outlineRenderers[i];
                if (outlineRenderer == null) continue;
                outlineRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorId, color);
                propertyBlock.SetFloat(WidthId, settings.baseWidth * Mathf.Lerp(0.55f, 1f, previewStrength)
                    * (settings.enhancedOutcomeCues ? 1.35f : 1f));
                propertyBlock.SetFloat(DangerId, CurrentOutcome == CollisionOutcome.SpeedLoss ? 1f : 0f);
                propertyBlock.SetFloat(PulseSpeedId, settings.dangerPulseSpeed);
                propertyBlock.SetFloat(PulseAmountId, CurrentOutcome == CollisionOutcome.SpeedLoss ? settings.dangerPulseAmount : 0f);
                propertyBlock.SetFloat(FlowDirectionId, CurrentOutcome == CollisionOutcome.SpeedGain ? 1f : CurrentOutcome == CollisionOutcome.SpeedLoss ? -1f : 0f);
                outlineRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void SetOutlineRenderersEnabled(bool visible)
        {
            for (int i = 0; i < outlineRenderers.Length; i++)
                if (outlineRenderers[i] != null) outlineRenderers[i].enabled = visible;
        }

        private Color GetOutcomeColor(CollisionOutcome outcome)
        {
            if (outcome == CollisionOutcome.SpeedGain) return settings.gainColor;
            if (outcome == CollisionOutcome.SpeedLoss) return settings.dangerColor;
            return settings.neutralColor;
        }

        private void OnObstacleResolved(ObstacleResolvedEvent resolved)
        {
            SetOutlineRenderersEnabled(false);
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            if (speedController != null) speedController.SpeedChanged -= OnSpeedChanged;
            if (obstacle != null) obstacle.Resolved -= OnObstacleResolved;
            subscribed = false;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy() => Unsubscribe();
    }
}
