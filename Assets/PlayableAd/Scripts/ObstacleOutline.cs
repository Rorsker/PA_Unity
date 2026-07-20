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
        [Range(0.25f, 0.8f), InspectorName("Symbol Scale（符号缩放）")] public float symbolScale = 0.48f;
        [InspectorName("Outline Material（轮廓材质）")] public Material outlineMaterial;
    }

    public sealed class ObstacleOutline : MonoBehaviour
    {
        private static Material fallbackSharedMaterial;
        private static Material statusRingSharedMaterial;
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
        private MaterialPropertyBlock symbolPropertyBlock;
        private LineRenderer outcomeSymbol;
        private LineRenderer statusRing;
        private Coroutine transitionRoutine;
        private int lastPlayerLevel = -1;
        private bool subscribed;
        private float previewStrength = 1f;

        public bool IsSafe { get; private set; }
        public CollisionOutcome CurrentOutcome { get; private set; } = CollisionOutcome.Neutral;

        public void Initialize(ObstacleOutlineSettings outlineSettings, PlayerSpeedController controller,
            ObstacleController targetObstacle, Renderer[] visualSources = null)
        {
            settings = outlineSettings;
            speedController = controller;
            obstacle = targetObstacle;
            propertyBlock = new MaterialPropertyBlock();
            symbolPropertyBlock = new MaterialPropertyBlock();

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
                // Soldiers use a low-interference ground ring instead of mesh outlines.
                outlineRenderers = Array.Empty<Renderer>();
                BuildStatusRing();
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
            BuildOutcomeSymbol(material);
            if (statusRing != null && outcomeSymbol != null) outcomeSymbol.enabled = false;
            SetPreviewActive(gameObject.activeInHierarchy);
        }

        private void BuildStatusRing()
        {
            if (statusRingSharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                statusRingSharedMaterial = new Material(shader) { name = "SharedEnemyStatusRing" };
            }

            GameObject ring = new GameObject("RiskStatusRing");
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.485f, 0f);
            statusRing = ring.AddComponent<LineRenderer>();
            statusRing.useWorldSpace = false;
            statusRing.loop = true;
            statusRing.positionCount = 32;
            statusRing.startWidth = 0.035f;
            statusRing.endWidth = 0.035f;
            statusRing.numCornerVertices = 2;
            statusRing.numCapVertices = 2;
            statusRing.sharedMaterial = statusRingSharedMaterial;
            statusRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            statusRing.receiveShadows = false;

            float radius = obstacle.RequiredSpeedLevel >= 4 ? 0.78f : 0.56f;
            float scaleX = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
            float scaleZ = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.z));
            for (int i = 0; i < statusRing.positionCount; i++)
            {
                float angle = i / (float)statusRing.positionCount * Mathf.PI * 2f;
                statusRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius / scaleX, 0f,
                    Mathf.Sin(angle) * radius / scaleZ));
            }
        }

        private Renderer[] BuildVisualOutlines(Renderer[] sources, Material material)
        {
            Renderer[] results = new Renderer[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                Renderer source = sources[i];
                GameObject outline = new GameObject("RiskOutline_" + source.name);
                outline.transform.SetParent(source.transform, false);
                Renderer result;
                SkinnedMeshRenderer skinnedSource = source as SkinnedMeshRenderer;
                if (skinnedSource != null)
                {
                    SkinnedMeshRenderer skinnedOutline = outline.AddComponent<SkinnedMeshRenderer>();
                    skinnedOutline.sharedMesh = skinnedSource.sharedMesh;
                    skinnedOutline.bones = skinnedSource.bones;
                    skinnedOutline.rootBone = skinnedSource.rootBone;
                    skinnedOutline.localBounds = skinnedSource.localBounds;
                    skinnedOutline.updateWhenOffscreen = false;
                    result = skinnedOutline;
                }
                else
                {
                    MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
                    MeshFilter targetFilter = outline.AddComponent<MeshFilter>();
                    targetFilter.sharedMesh = sourceFilter != null ? sourceFilter.sharedMesh : null;
                    result = outline.AddComponent<MeshRenderer>();
                }
                result.sharedMaterial = material;
                result.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                result.receiveShadows = false;
                results[i] = result;
            }
            return results;
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
            if (statusRing != null) statusRing.enabled = visible && settings.enabled && previewStrength >= 0.35f;
            if (outcomeSymbol != null) outcomeSymbol.enabled = visible && settings.enabled && previewStrength >= 0.72f;
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
            if ((outlineRenderers.Length == 0 && statusRing == null) || obstacle == null) return;
            lastPlayerLevel = playerLevel;
            CurrentOutcome = ObstacleController.EvaluateCollisionOutcome(playerLevel, obstacle.RequiredSpeedLevel);
            IsSafe = CurrentOutcome != CollisionOutcome.SpeedLoss;
            SetOutlineRenderersEnabled(settings.enabled && !obstacle.HasResolved);
            Color color = GetOutcomeColor(CurrentOutcome);
            color *= settings.maxSceneBrightness;
            color.a = GetOutcomeColor(CurrentOutcome).a;
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            if (smooth && isActiveAndEnabled)
                transitionRoutine = StartCoroutine(TransitionVisual(color));
            else
                ApplyVisual(color);
            ConfigureSymbol(CurrentOutcome, color);
            ConfigureStatusRing(color);
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

        private void BuildOutcomeSymbol(Material material)
        {
            GameObject symbol = new GameObject("OutcomeSymbol_NoNumbers");
            symbol.transform.SetParent(transform, false);
            symbol.transform.localPosition = new Vector3(0f, 1.12f, -0.48f);
            symbol.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            outcomeSymbol = symbol.AddComponent<LineRenderer>();
            outcomeSymbol.useWorldSpace = false;
            outcomeSymbol.loop = false;
            outcomeSymbol.sharedMaterial = material;
            outcomeSymbol.numCapVertices = 2;
            outcomeSymbol.numCornerVertices = 2;
            outcomeSymbol.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outcomeSymbol.receiveShadows = false;
        }

        private void ConfigureSymbol(CollisionOutcome outcome, Color color)
        {
            if (outcomeSymbol == null) return;
            if (statusRing != null)
            {
                outcomeSymbol.enabled = false;
                return;
            }
            float scale = settings.symbolScale * (settings.enhancedOutcomeCues ? 1.25f : 1f);
            outcomeSymbol.startWidth = settings.baseWidth * 1.6f;
            outcomeSymbol.endWidth = outcomeSymbol.startWidth;
            outcomeSymbol.startColor = color;
            outcomeSymbol.endColor = color;
            outcomeSymbol.enabled = settings.enabled && previewStrength >= 0.72f && !obstacle.HasResolved;
            outcomeSymbol.GetPropertyBlock(symbolPropertyBlock);
            symbolPropertyBlock.SetColor(ColorId, color);
            symbolPropertyBlock.SetFloat(WidthId, 0f);
            symbolPropertyBlock.SetFloat(DangerId, outcome == CollisionOutcome.SpeedLoss ? 1f : 0f);
            symbolPropertyBlock.SetFloat(PulseSpeedId, settings.dangerPulseSpeed);
            symbolPropertyBlock.SetFloat(PulseAmountId, outcome == CollisionOutcome.SpeedLoss ? settings.dangerPulseAmount : 0f);
            symbolPropertyBlock.SetFloat(FlowDirectionId, outcome == CollisionOutcome.SpeedGain ? 1f : outcome == CollisionOutcome.SpeedLoss ? -1f : 0f);
            outcomeSymbol.SetPropertyBlock(symbolPropertyBlock);
            if (outcome == CollisionOutcome.Neutral)
            {
                outcomeSymbol.positionCount = 2;
                outcomeSymbol.SetPosition(0, new Vector3(-scale, 0f, 0f));
                outcomeSymbol.SetPosition(1, new Vector3(scale, 0f, 0f));
                return;
            }
            float direction = outcome == CollisionOutcome.SpeedGain ? 1f : -1f;
            outcomeSymbol.positionCount = 5;
            outcomeSymbol.SetPosition(0, new Vector3(0f, -scale * direction, 0f));
            outcomeSymbol.SetPosition(1, new Vector3(0f, scale * direction, 0f));
            outcomeSymbol.SetPosition(2, new Vector3(-scale * 0.48f, scale * 0.45f * direction, 0f));
            outcomeSymbol.SetPosition(3, new Vector3(0f, scale * direction, 0f));
            outcomeSymbol.SetPosition(4, new Vector3(scale * 0.48f, scale * 0.45f * direction, 0f));
        }

        private void ConfigureStatusRing(Color color)
        {
            if (statusRing == null) return;
            color.a = Mathf.Lerp(0.24f, 0.58f, previewStrength);
            statusRing.startColor = color;
            statusRing.endColor = color;
            statusRing.enabled = settings.enabled && previewStrength >= 0.35f && !obstacle.HasResolved;
        }

        private void OnObstacleResolved(ObstacleResolvedEvent resolved)
        {
            SetOutlineRenderersEnabled(false);
            if (statusRing != null) statusRing.enabled = false;
            if (outcomeSymbol != null) outcomeSymbol.enabled = false;
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
