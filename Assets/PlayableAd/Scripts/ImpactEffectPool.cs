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
        [Range(0.1f, 0.4f), InspectorName("Trail Pulse Recovery（拖尾脉冲恢复）")] public float trailPulseRecovery = 0.2f;
        [InspectorName("Enable Normal Hit Stop（启用普通命中顿帧）")] public bool enableNormalHitStop = false;
        [Range(0f, 0.18f), InspectorName("Normal Camera Shake（普通镜头抖动）")] public float normalCameraShake = 0.065f;
        [Range(0f, 2f), InspectorName("Normal FOV Punch（普通视场角冲击）")] public float normalFovPunch = 0.8f;
        [Range(0.08f, 0.4f), InspectorName("Normal Shake Cooldown（普通抖动冷却）")] public float normalShakeCooldown = 0.2f;

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
        private EnergyShard[] energyShards;
        private int impactCursor;
        private int shardCursor;
        private Material lineMaterial;
        private SpeedVisualProfile visualProfile;
        private VisualPerformanceSettings performance;

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

        public void PlayImpact(Vector3 position, Color color, float strength)
        {
            if (impactPool == null || impactPool.Length == 0 || settings == null)
            {
                return;
            }

            ParticleSystem particles = impactPool[impactCursor++ % impactPool.Length];
            particles.transform.position = position;
            ParticleSystem.MainModule main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f * strength, 7f * strength);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.24f * strength);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play();
            float quality = performance.lowQualityMode ? performance.lowQualityParticleMultiplier : 1f;
            particles.Emit(Mathf.Clamp(Mathf.RoundToInt(settings.normalImpactParticles * strength * quality), 3, 18));
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
                shard.root.SetActive(true);
                shard.root.transform.position = position;
                shard.line.startColor = new Color(color.r, color.g, color.b, 0.9f * strength);
                shard.line.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }

        private void Update()
        {
            if (runner == null || energyShards == null)
            {
                return;
            }

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
                    continue;
                }

                shard.timer += Time.unscaledDeltaTime;
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
            Material particleMaterial = visualProfile != null && visualProfile.particleMaterial != null
                ? visualProfile.particleMaterial
                : RuntimeStyle.CreateMaterial(Color.white, 0f, 0f);
            for (int i = 0; i < count; i++)
            {
                GameObject root = new GameObject("PooledImpact_" + i);
                root.transform.SetParent(transform, false);
                ParticleSystem particles = root.AddComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = particles.main;
                main.loop = false;
                main.duration = 0.22f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
                main.gravityModifier = 0.75f;
                main.maxParticles = 20;
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.enabled = false;
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Hemisphere;
                shape.radius = 0.15f;
                ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = particleMaterial;
                impactPool[i] = particles;
            }
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
    }
}
