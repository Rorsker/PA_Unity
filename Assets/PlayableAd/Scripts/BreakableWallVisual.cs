using System;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class WallBreakSettings
    {
        [Header("Timing（时序）")]
        [Range(0.15f, 0.25f), InspectorName("Anticipation Duration（预备时长）")] public float anticipationDuration = 0.2f;
        [Range(0.6f, 1f), InspectorName("Total Duration（总时长）")] public float totalDuration = 0.82f;
        [Range(0.08f, 0.15f), InspectorName("Slow Motion Duration（慢动作时长）")] public float slowMotionDuration = 0.11f;
        [Range(0.2f, 0.65f), InspectorName("Slow Motion Scale（慢动作缩放）")] public float slowMotionScale = 0.32f;
        [Range(0.2f, 0.45f), InspectorName("Camera Recovery（镜头恢复）")] public float cameraRecovery = 0.3f;

        [Header("Impact（冲击）")]
        [Range(0.25f, 0.8f), InspectorName("Camera Shake（镜头抖动）")] public float cameraShake = 0.48f;
        [Range(0f, 8f), InspectorName("FOV Anticipation（预备视场角变化）")] public float fovAnticipation = 2.5f;
        [Range(0f, 10f), InspectorName("FOV Impact（冲击视场角变化）")] public float fovImpact = 6.5f;
        [Range(5f, 24f), InspectorName("Chunk Forward Speed（碎块前向速度）")] public float chunkForwardSpeed = 12f;
        [Range(2f, 14f), InspectorName("Chunk Side Speed（碎块侧向速度）")] public float chunkSideSpeed = 6f;
        [Range(0.6f, 1.2f), InspectorName("Chunk Lifetime（碎块持续时间）")] public float chunkLifetime = 0.9f;
        [Range(8, 24), InspectorName("Dust Amount（尘雾数量）")] public int dustAmount = 16;
    }

    public sealed class BreakableWallVisual : MonoBehaviour
    {
        private sealed class ChunkState
        {
            public Transform transform;
            public Collider collider;
            public Vector3 velocity;
            public Vector3 angularVelocity;
        }

        private WallBreakSettings settings;
        private ChunkState[] chunks;
        private LineRenderer shockwave;
        private ParticleSystem dustParticles;
        private bool breaking;
        private float timer;
        private VisualPerformanceSettings performance;
        private Material lineMaterial;

        public bool IsBreaking => breaking;
        public bool AllCollidersDisabled
        {
            get
            {
                if (chunks == null) return false;
                for (int i = 0; i < chunks.Length; i++)
                {
                    if (chunks[i].collider != null && chunks[i].collider.enabled) return false;
                }
                return true;
            }
        }

        public void Initialize(WallBreakSettings wallSettings, Color stoneColor, SpeedVisualProfile profile,
            VisualPerformanceSettings performanceSettings)
        {
            settings = wallSettings;
            performance = performanceSettings ?? new VisualPerformanceSettings();
            lineMaterial = profile != null ? profile.lineMaterial : null;
            BuildChunks(stoneColor);
            BuildShockwave();
            BuildDustParticles(profile);
        }

        public void Break()
        {
            if (breaking) return;
            breaking = true;
            timer = 0f;
            shockwave.enabled = true;
            if (dustParticles != null)
            {
                int amount = performance.lowQualityMode
                    ? Mathf.Max(6, Mathf.RoundToInt(settings.dustAmount * performance.lowQualityParticleMultiplier))
                    : settings.dustAmount;
                dustParticles.Emit(amount);
            }

            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkState chunk = chunks[i];
                if (chunk.collider != null) chunk.collider.enabled = false;
                float side = Mathf.Sign(chunk.transform.localPosition.x);
                if (Mathf.Abs(side) < 0.1f) side = i % 2 == 0 ? -1f : 1f;
                bool central = Mathf.Abs(chunk.transform.localPosition.x) < 1.35f;
                float forward = settings.chunkForwardSpeed * 0.48f * (central ? 1.25f : 0.82f);
                float lateral = settings.chunkSideSpeed * (central ? 0.75f : 1.2f) * side;
                chunk.velocity = new Vector3(lateral, 3.5f + (i % 3) * 1.1f, forward);
                chunk.angularVelocity = new Vector3(180f + i * 17f, side * (260f + i * 13f), 140f + (i % 4) * 45f);
            }
        }

        private void Update()
        {
            if (!breaking) return;
            float dt = Time.unscaledDeltaTime;
            timer += dt;
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkState chunk = chunks[i];
                if (chunk == null || chunk.transform == null) continue;
                if (!chunk.transform.gameObject.activeSelf) continue;
                chunk.velocity += Vector3.down * 12f * dt;
                chunk.transform.position += chunk.velocity * dt;
                chunk.transform.Rotate(chunk.angularVelocity * dt, Space.Self);
                if (timer >= settings.chunkLifetime)
                {
                    chunk.transform.gameObject.SetActive(false);
                }
            }

            float waveT = Mathf.Clamp01(timer / 0.42f);
            float radius = Mathf.Lerp(0.2f, 5.2f, 1f - Mathf.Pow(1f - waveT, 2f));
            shockwave.transform.localScale = new Vector3(radius, radius, radius);
            Color color = new Color(1f, 0.75f, 0.28f, (1f - waveT) * 0.72f);
            shockwave.startColor = color;
            shockwave.endColor = color;
            if (waveT >= 1f) shockwave.enabled = false;
            if (timer >= settings.totalDuration)
            {
                breaking = false;
                shockwave.enabled = false;
            }
        }

        private void BuildChunks(Color stoneColor)
        {
            int chunkCount = performance.lowQualityMode ? performance.lowQualityWallChunkCount : 10;
            int columns = Mathf.CeilToInt(chunkCount * 0.5f);
            const int rows = 2;
            chunks = new ChunkState[chunkCount];
            Material sharedStone = RuntimeStyle.CreateMaterial(stoneColor, 0f, 0.28f);
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (index >= chunkCount) break;
                    GameObject chunkObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chunkObject.name = "WallChunk_" + row + "_" + column;
                    chunkObject.transform.SetParent(transform, false);
                    float centeredColumn = column - (columns - 1) * 0.5f;
                    chunkObject.transform.localPosition = new Vector3(centeredColumn * 1.3f + (row == 1 ? 0.18f : 0f), 0.58f + row * 1.05f, 0f);
                    chunkObject.transform.localScale = new Vector3(1.24f, 0.96f, 0.64f);
                    chunkObject.GetComponent<Renderer>().sharedMaterial = sharedStone;
                    chunks[index++] = new ChunkState
                    {
                        transform = chunkObject.transform,
                        collider = chunkObject.GetComponent<Collider>()
                    };
                }
            }
        }

        private void BuildShockwave()
        {
            GameObject ring = new GameObject("WallShockwave");
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.08f, -0.4f);
            shockwave = ring.AddComponent<LineRenderer>();
            shockwave.useWorldSpace = false;
            shockwave.loop = true;
            shockwave.positionCount = 40;
            shockwave.startWidth = 0.14f;
            shockwave.endWidth = 0.14f;
            shockwave.sharedMaterial = lineMaterial != null
                ? lineMaterial
                : RuntimeStyle.CreateMaterial(Color.white, 0f, 0.1f);
            for (int i = 0; i < 40; i++)
            {
                float angle = i / 40f * Mathf.PI * 2f;
                shockwave.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }
            shockwave.enabled = false;
        }

        private void BuildDustParticles(SpeedVisualProfile profile)
        {
            GameObject root = new GameObject("WallDustPool");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, 1.1f, -0.15f);
            dustParticles = root.AddComponent<ParticleSystem>();
            dustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = dustParticles.main;
            main.loop = false;
            main.duration = 0.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.62f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.58f, 0.52f, 0.45f, 0.82f),
                new Color(0.88f, 0.76f, 0.56f, 0.5f));
            main.gravityModifier = 0.25f;
            main.maxParticles = Mathf.Max(8, settings.dustAmount);
            ParticleSystem.EmissionModule emission = dustParticles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = dustParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3.1f, 1.2f, 0.35f);
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = profile != null && profile.particleMaterial != null
                ? profile.particleMaterial
                : RuntimeStyle.CreateMaterial(Color.white, 0f, 0f);
        }
    }
}
