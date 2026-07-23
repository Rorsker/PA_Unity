using System;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class ImpactPresentationSettings
    {
        [Header("Pools（对象池）")]
        [Range(3, 10), InspectorName("Impact Pool Size（冲击对象池大小）")] public int impactPoolSize = 6;
        [Range(12, 40), InspectorName("Max Energy Shards（最大能量碎片数）")] public int maxEnergyShards = 24;
        [Range(4, 18), InspectorName("Normal Impact Particles（普通冲击粒子数）")] public int normalImpactParticles = 9;

        [Header("Normal Hit Hierarchy（普通命中层级）")]
        [Range(0.03f, 0.06f), InspectorName("Hit Stop Duration（顿帧时长）")] public float hitStopDuration = 0.045f;
        [Range(0.2f, 0.8f), InspectorName("Hit Stop Time Scale（顿帧时间缩放）")] public float hitStopTimeScale = 0.35f;
        [Range(0.05f, 0.35f), InspectorName("Normal Flash（普通闪光）")] public float normalFlash = 0.16f;
        [InspectorName("Enable Normal Hit Stop（启用普通命中顿帧）")] public bool enableNormalHitStop = false;
        [Range(0f, 0.18f), InspectorName("Normal Camera Shake（普通镜头抖动）")] public float normalCameraShake = 0.065f;
        [Range(0f, 2f), InspectorName("Normal FOV Punch（普通视场角冲击）")] public float normalFovPunch = 0.8f;
        [Range(0.08f, 0.4f), InspectorName("Normal Shake Cooldown（普通抖动冷却）")] public float normalShakeCooldown = 0.2f;

        [Header("Penalty Screen Feedback（受罚屏幕反馈）")]
        [InspectorName("Penalty Edge Color（受罚红边颜色）")]
        public Color penaltyEdgeColor = new Color(0.92f, 0.02f, 0.015f, 1f);
        [Range(0.1f, 1f), InspectorName("Penalty Edge Opacity（受罚红边透明度）")]
        public float penaltyEdgeOpacity = 0.62f;
        [Range(0.08f, 0.32f), InspectorName("Penalty Edge Width（受罚红边宽度）")]
        public float penaltyEdgeWidth = 0.18f;
        [Range(0.15f, 1.5f), InspectorName("Penalty Edge Fade Duration（受罚红边淡出时长）")]
        public float penaltyEdgeFadeDuration = 0.65f;

        [Header("Combo Rhythm（连击节奏）")]
        [Range(0.2f, 1f), InspectorName("Combo Window（连击窗口）")] public float comboWindow = 0.65f;
        [Range(0f, 0.12f), InspectorName("Combo Pitch Step（连击音调步进）")] public float comboPitchStep = 0.045f;
        [Range(2, 5), InspectorName("Combo Pitch Steps（连击音调级数）")] public int comboPitchSteps = 5;

        [Header("Energy Return（能量回收）")]
        [Range(3, 6), InspectorName("Min Energy Shards（最少能量碎片）")] public int minEnergyShards = 3;
        [Range(3, 6), InspectorName("Max Energy Shards Per Hit（每次命中最大能量碎片）")] public int maxEnergyShardsPerHit = 6;
        [Range(0.25f, 0.5f), InspectorName("Energy Return Duration（能量回收时长）")] public float energyReturnDuration = 0.38f;
        [Range(0.2f, 1.2f), InspectorName("Scatter Radius（散射半径）")] public float scatterRadius = 0.62f;
    }

    public sealed class ImpactEffectPool : MonoBehaviour
    {
        private sealed class EnergyShard
        {
            public GameObject root;
            public LineRenderer line;
            public Vector3 origin;
            public Vector3 scattered;
            public float timer;
            public float duration;
            public bool active;
        }

        private ImpactPresentationSettings settings;
        private Transform runner;
        private ParticleSystem[] impactPool;
        private ParticleSystem[] highlightPool;
        private EnergyShard[] energyShards;
        private int impactCursor;
        private int shardCursor;
        private Material impactParticleMaterial;
        private Material highlightParticleMaterial;
        private Material lineMaterial;
        private SpeedVisualProfile visualProfile;
        private VisualPerformanceSettings performance;
        private float lastParticleSimulationScale = -1f;

        public Action EnergyShardAbsorbed;
        public int LastRequestedEnergyShardCount { get; private set; }
        public int LastSpawnedEnergyShardCount { get; private set; }
        public int ActiveEnergyShardCount
        {
            get
            {
                int count = 0;
                if (energyShards == null) return count;
                for (int i = 0; i < energyShards.Length; i++) if (energyShards[i].active) count++;
                return count;
            }
        }

        public void Initialize(Transform runnerTransform, ImpactPresentationSettings presentationSettings,
            SpeedVisualProfile profile, VisualPerformanceSettings performanceSettings)
        {
            runner = runnerTransform;
            settings = presentationSettings;
            visualProfile = profile;
            performance = performanceSettings ?? new VisualPerformanceSettings();
            BuildImpactPool();
            BuildEnergyShardPool();
        }

        public void PlayImpact(Vector3 position, Color color, float strength, int speedLevel = 0)
        {
            if (impactPool == null || impactPool.Length == 0 || settings == null)
            {
                return;
            }

            Color impactColor = speedLevel > 0 ? GetImpactStageColor(speedLevel) : color;
            float visualStrength = strength * GetImpactStageIntensity(speedLevel);
            int poolIndex = impactCursor++ % impactPool.Length;
            ParticleSystem particles = impactPool[poolIndex];
            particles.transform.position = position;
            BuildImpactPalette(impactColor, out Color deepColor, out Color midColor,
                out Color brightColor, out Color glintColor);

            ParticleSystem.MainModule main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(deepColor, midColor);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4.5f * visualStrength, 9f * visualStrength);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.64f * visualStrength);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play();

            float quality = performance.lowQualityMode ? performance.lowQualityParticleMultiplier : 1f;
            int baseAmount = Mathf.Clamp(
                Mathf.RoundToInt(settings.normalImpactParticles * visualStrength * quality * 2.05f), 7, 48);
            particles.Emit(baseAmount);

            if (highlightPool == null || poolIndex >= highlightPool.Length ||
                highlightPool[poolIndex] == null) return;

            ParticleSystem highlights = highlightPool[poolIndex];
            highlights.transform.position = position;
            ParticleSystem.MainModule highlightMain = highlights.main;
            highlightMain.startColor = new ParticleSystem.MinMaxGradient(brightColor, glintColor);
            highlightMain.startSpeed = new ParticleSystem.MinMaxCurve(
                3.2f * visualStrength, 7f * visualStrength);
            highlightMain.startSize = new ParticleSystem.MinMaxCurve(
                0.14f, 0.46f * visualStrength);
            highlights.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            highlights.Play();
            highlights.Emit(Mathf.Clamp(Mathf.RoundToInt(baseAmount * 0.85f), 6, 30));

            ParticleSystem.EmitParams coreFlash = new ParticleSystem.EmitParams
            {
                position = Vector3.zero,
                velocity = Vector3.zero,
                startColor = glintColor,
                startSize = 1.05f * visualStrength,
                startLifetime = 0.16f
            };
            highlights.Emit(coreFlash, 1);

            ParticleSystem.EmitParams coreGlow = new ParticleSystem.EmitParams
            {
                position = Vector3.zero,
                velocity = Vector3.zero,
                startColor = brightColor,
                startSize = 0.68f * visualStrength,
                startLifetime = 0.26f
            };
            highlights.Emit(coreGlow, 1);
        }

        public void PlayEnergyReturn(Vector3 position, int count, Color color, float strength = 1f)
        {
            LastRequestedEnergyShardCount = count;
            LastSpawnedEnergyShardCount = 0;
            if (energyShards == null || energyShards.Length == 0 || settings == null)
            {
                return;
            }

            int qualityLimit = performance.lowQualityMode ? performance.lowQualityEnergyShardLimit : settings.maxEnergyShardsPerHit;
            int amount = Mathf.Clamp(count, settings.minEnergyShards, Mathf.Min(settings.maxEnergyShardsPerHit, qualityLimit));
            LastSpawnedEnergyShardCount = amount;
            for (int i = 0; i < amount; i++)
            {
                EnergyShard shard = GetNextShard();
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 scatter = new Vector3(
                    side * UnityEngine.Random.Range(0.2f, settings.scatterRadius),
                    UnityEngine.Random.Range(0.12f, 0.65f),
                    UnityEngine.Random.Range(-0.25f, 0.75f));
                shard.origin = position;
                shard.scattered = position + scatter;
                shard.timer = 0f;
                shard.duration = settings.energyReturnDuration * UnityEngine.Random.Range(0.9f, 1.08f);
                shard.active = true;
                shard.root.transform.position = position;
                shard.line.SetPosition(0, position);
                shard.line.SetPosition(1, position);
                shard.line.startColor = new Color(color.r, color.g, color.b, 0.9f * strength);
                shard.line.endColor = new Color(color.r, color.g, color.b, 0f);
                shard.root.SetActive(true);
            }
        }

        private void Update()
        {
            float bulletTimeScale = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.WorldTimeScale
                : 1f;
            if (!Mathf.Approximately(lastParticleSimulationScale, bulletTimeScale))
            {
                lastParticleSimulationScale = bulletTimeScale;
                if (impactPool != null)
                {
                    for (int i = 0; i < impactPool.Length; i++)
                    {
                        if (impactPool[i] == null) continue;
                        ParticleSystem.MainModule main = impactPool[i].main;
                        main.simulationSpeed = bulletTimeScale;
                    }
                }

                if (highlightPool != null)
                {
                    for (int i = 0; i < highlightPool.Length; i++)
                    {
                        if (highlightPool[i] == null) continue;
                        ParticleSystem.MainModule main = highlightPool[i].main;
                        main.simulationSpeed = bulletTimeScale;
                    }
                }
            }

            if (runner == null || energyShards == null)
            {
                return;
            }

            float worldDeltaTime = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.GetWorldDeltaTime()
                : Time.deltaTime;
            for (int i = 0; i < energyShards.Length; i++)
            {
                EnergyShard shard = energyShards[i];
                if (shard == null || !shard.active)
                {
                    continue;
                }

                if (shard.root == null || shard.line == null)
                {
                    shard.active = false;
                    if (shard.root != null) shard.root.SetActive(false);
                    continue;
                }

                shard.timer += worldDeltaTime;
                float t = Mathf.Clamp01(shard.timer / shard.duration);
                Vector3 target = runner.position + Vector3.up * 0.75f;
                Vector3 position;
                if (t < 0.28f)
                {
                    float outward = t / 0.28f;
                    position = Vector3.Lerp(shard.origin, shard.scattered, 1f - Mathf.Pow(1f - outward, 2f));
                }
                else
                {
                    float returnT = (t - 0.28f) / 0.72f;
                    Vector3 control = Vector3.Lerp(shard.scattered, target, 0.45f) + Vector3.up * 0.9f;
                    float inverse = 1f - returnT;
                    position = inverse * inverse * shard.scattered + 2f * inverse * returnT * control + returnT * returnT * target;
                }

                Vector3 previous = shard.root.transform.position;
                shard.root.transform.position = position;
                shard.line.SetPosition(0, position);
                shard.line.SetPosition(1, Vector3.Lerp(previous, position, 0.25f));

                if (t >= 1f)
                {
                    shard.active = false;
                    shard.root.SetActive(false);
                    EnergyShardAbsorbed?.Invoke();
                }
            }
        }

        private void BuildImpactPool()
        {
            int count = Mathf.Clamp(settings.impactPoolSize, 3, 10);
            impactPool = new ParticleSystem[count];
            highlightPool = new ParticleSystem[count];
            Material particleMaterial = BuildImpactParticleMaterial();
            Material highlightMaterial = BuildHighlightParticleMaterial();
            bool hasImpactAtlas = impactParticleMaterial != null;
            for (int i = 0; i < count; i++)
            {
                impactPool[i] = BuildPooledImpactParticles(
                    "PooledImpact_" + i, particleMaterial, false, hasImpactAtlas);
                highlightPool[i] = BuildPooledImpactParticles(
                    "PooledImpactHighlight_" + i,
                    highlightMaterial != null ? highlightMaterial : particleMaterial,
                    true, hasImpactAtlas);
            }
        }

        private ParticleSystem BuildPooledImpactParticles(string objectName, Material material,
            bool highlight, bool hasImpactAtlas)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(transform, false);
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.duration = highlight ? 0.2f : 0.26f;
            main.startLifetime = highlight
                ? new ParticleSystem.MinMaxCurve(0.12f, 0.34f)
                : new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.gravityModifier = highlight ? 0.12f : 0.75f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.maxParticles = highlight ? 36 : 52;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = highlight ? 0.14f : 0.2f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                highlight
                    ? new[] { new GradientAlphaKey(0.35f, 0f), new GradientAlphaKey(1f, 0.06f),
                        new GradientAlphaKey(0.42f, 0.56f), new GradientAlphaKey(0f, 1f) }
                    : new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(1f, 0.08f),
                        new GradientAlphaKey(0.72f, 0.62f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = fade;

            ParticleSystem.TextureSheetAnimationModule textureSheet = particles.textureSheetAnimation;
            textureSheet.enabled = hasImpactAtlas;
            textureSheet.mode = ParticleSystemAnimationMode.Grid;
            textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheet.numTilesX = 6;
            textureSheet.numTilesY = 6;
            textureSheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 33.99f / 36f);
            textureSheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = highlight ? 1 : 0;
            return particles;
        }


        private Material BuildImpactParticleMaterial()
        {
            Material source = visualProfile != null && visualProfile.particleMaterial != null
                ? visualProfile.particleMaterial
                : RuntimeStyle.CreateMaterial(Color.white, 0f, 0f);
            Texture2D atlas = Resources.Load<Texture2D>("ImpactTextures/ImpactBurstAtlas");
            if (atlas == null) return source;

            impactParticleMaterial = new Material(source)
            {
                name = "RuntimeImpactTextureMaterial",
                mainTexture = atlas,
                renderQueue = 3000
            };
            impactParticleMaterial.SetOverrideTag("RenderType", "Transparent");
            if (impactParticleMaterial.HasProperty("_Mode")) impactParticleMaterial.SetFloat("_Mode", 2f);
            if (impactParticleMaterial.HasProperty("_SrcBlend"))
                impactParticleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (impactParticleMaterial.HasProperty("_DstBlend"))
                impactParticleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (impactParticleMaterial.HasProperty("_ZWrite")) impactParticleMaterial.SetInt("_ZWrite", 0);
            impactParticleMaterial.DisableKeyword("_ALPHATEST_ON");
            impactParticleMaterial.EnableKeyword("_ALPHABLEND_ON");
            impactParticleMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return impactParticleMaterial;
        }

        private Material BuildHighlightParticleMaterial()
        {
            if (impactParticleMaterial == null) return null;
            highlightParticleMaterial = new Material(impactParticleMaterial)
            {
                name = "RuntimeImpactHighlightMaterial",
                renderQueue = 3001
            };
            if (highlightParticleMaterial.HasProperty("_Mode"))
                highlightParticleMaterial.SetFloat("_Mode", 4f);
            if (highlightParticleMaterial.HasProperty("_SrcBlend"))
                highlightParticleMaterial.SetInt("_SrcBlend",
                    (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (highlightParticleMaterial.HasProperty("_DstBlend"))
                highlightParticleMaterial.SetInt("_DstBlend",
                    (int)UnityEngine.Rendering.BlendMode.One);
            if (highlightParticleMaterial.HasProperty("_ZWrite"))
                highlightParticleMaterial.SetInt("_ZWrite", 0);
            return highlightParticleMaterial;
        }

        private static void BuildImpactPalette(Color source, out Color deep,
            out Color mid, out Color bright, out Color glint)
        {
            Color.RGBToHSV(source, out float hue, out float saturation, out float value);
            float alpha = Mathf.Clamp01(source.a);
            deep = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 1.08f + 0.04f),
                Mathf.Clamp(value * 0.62f, 0.12f, 1f));
            mid = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 0.88f),
                Mathf.Clamp(value * 1.02f, 0.35f, 1f));
            bright = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 0.48f),
                Mathf.Clamp(value * 1.24f, 0.78f, 1f));
            glint = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 0.18f), 1f);
            deep.a = alpha * 0.88f;
            mid.a = alpha;
            bright.a = Mathf.Clamp01(alpha * 1.06f);
            glint.a = Mathf.Clamp01(alpha * 1.18f);
        }



        private void BuildEnergyShardPool()
        {
            int configuredCount = performance.lowQualityMode
                ? Mathf.Min(settings.maxEnergyShards, performance.lowQualityEnergyShardLimit)
                : settings.maxEnergyShards;
            int count = Mathf.Clamp(configuredCount, 4, 40);
            energyShards = new EnergyShard[count];
            lineMaterial = visualProfile != null && visualProfile.lineMaterial != null
                ? visualProfile.lineMaterial
                : RuntimeStyle.CreateMaterial(Color.white, 0f, 0f);
            for (int i = 0; i < count; i++)
            {
                GameObject root = new GameObject("PooledEnergyShard_" + i);
                root.transform.SetParent(transform, false);
                LineRenderer line = root.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.09f;
                line.endWidth = 0.015f;
                line.sharedMaterial = lineMaterial;
                root.SetActive(false);
                energyShards[i] = new EnergyShard { root = root, line = line };
            }
        }

        private EnergyShard GetNextShard()
        {
            for (int i = 0; i < energyShards.Length; i++)
            {
                int index = (shardCursor + i) % energyShards.Length;
                if (!energyShards[index].active)
                {
                    shardCursor = index + 1;
                    return energyShards[index];
                }
            }

            EnergyShard reused = energyShards[shardCursor++ % energyShards.Length];
            reused.active = false;
            reused.root.SetActive(false);
            return reused;
        }

        private void OnDestroy()
        {
            if (impactParticleMaterial != null)
            {
                if (Application.isPlaying) Destroy(impactParticleMaterial);
                else DestroyImmediate(impactParticleMaterial);
            }
            if (highlightParticleMaterial != null)
            {
                if (Application.isPlaying) Destroy(highlightParticleMaterial);
                else DestroyImmediate(highlightParticleMaterial);
            }
        }


        private static Color GetImpactStageColor(int speedLevel)
        {
            if (speedLevel <= 2) return new Color(0.92f, 0.97f, 1f);
            if (speedLevel <= 6) return new Color(0.08f, 0.5f, 1f);
            if (speedLevel <= 8) return new Color(1f, 0.66f, 0.04f);
            return new Color(1f, 0.07f, 0.025f);
        }

        private static float GetImpactStageIntensity(int speedLevel)
        {
            if (speedLevel <= 0) return 1f;
            if (speedLevel <= 2) return 1.38f;
            if (speedLevel <= 6) return 1.22f;
            if (speedLevel <= 8) return 1f;
            return 1.12f;
        }
}
}
