using UnityEngine;

namespace PlayableAd
{
    public sealed class SpeedVisualFeedback : MonoBehaviour
    {
        private SpeedVisualProfile profile;
        private VisualPerformanceSettings performance;
        private TrailRenderer mainTrail;
        private LineRenderer energyTrail;
        private LineRenderer[] edgeLines;
        private float[] lineSeeds;
        private ParticleSystem auraParticles;
        private ParticleSystem airflowParticles;
        private ParticleSystem groundFlowParticles;
        private LineRenderer pressureCone;
        private LineRenderer[] afterimages;
        private float[] afterimageTimers;
        private float afterimageSpawnTimer;
        private ParticleSystem levelBurstParticles;
        private LineRenderer[] levelUpRings;
        private float[] ringTimers;
        private float[] ringDurations;
        private float[] ringScales;
        private Color[] ringColors;
        private int nextRing;
        private LineRenderer[] sonicBoomRings;
        private float[] sonicRingTimers;
        private float[] sonicRingDurations;
        private float[] sonicRingScales;
        private float[] sonicRingAlphas;
        private int nextSonicRing;
        private Transform sonicBoomPoolRoot;
        private Color currentPrimary;
        private Color currentSecondary;
        private float currentTrailLength;
        private float currentTrailWidth;
        private float currentBrightness;
        private float currentLineIntensity;
        private float currentEnergyIntensity;
        private float currentEmission;
        private float currentAirflowEmission;
        private float currentAirflowSpeed;
        private float currentAirflowAlpha;
        private float currentGroundFlow;
        private float currentAfterimageRate;
        private float currentAfterimageLifetime;
        private float currentPressureCone;
        private float currentWindVolume;
        private float currentWindPitch = 1f;
        private float currentImpactMultiplier = 1f;
        private float currentFovBonus;
        private float currentAmbientShake;
        private float currentChargeLean;
        private float temporaryBoost;
        private int currentLevel = 1;
        private float animationTime;

        public float CurrentFovBonus => currentFovBonus;
        public float CurrentAmbientShake => currentAmbientShake;
        public float CurrentChargeLean => currentChargeLean;
        public Color CurrentColor => currentPrimary;
        public int ActiveAuraParticleCount => auraParticles != null ? auraParticles.particleCount : 0;
        public int ActiveLevelUpParticleCount => levelBurstParticles != null ? levelBurstParticles.particleCount : 0;
        public float CurrentWindVolume => currentWindVolume;
        public float CurrentWindPitch => currentWindPitch;
        public float CurrentImpactMultiplier => currentImpactMultiplier;

        private void Update()
        {
            float worldDeltaTime = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.GetWorldDeltaTime()
                : Time.deltaTime;
            UpdateSonicBoomRings(worldDeltaTime);
        }

        public void Initialize(SpeedVisualProfile visualProfile, VisualPerformanceSettings performanceSettings)
        {
            profile = visualProfile;
            performance = performanceSettings ?? new VisualPerformanceSettings();
            SpeedTierVisualData initial = profile.Get(1);
            currentPrimary = initial.primaryColor;
            currentSecondary = initial.secondaryColor;
            currentTrailLength = initial.trailLength;
            currentTrailWidth = initial.trailWidth;
            currentBrightness = initial.trailBrightness;
            currentLineIntensity = initial.speedLineIntensity;
            currentEnergyIntensity = initial.characterEnergyIntensity;
            currentEmission = initial.particleEmissionRate;
            currentAirflowEmission = initial.airflowEmission;
            currentAirflowSpeed = initial.airflowSpeed;
            currentAirflowAlpha = initial.airflowAlpha;
            currentGroundFlow = initial.groundFlowIntensity;
            currentAfterimageRate = initial.afterimageRate;
            currentAfterimageLifetime = initial.afterimageLifetime;
            currentPressureCone = initial.pressureConeIntensity;
            currentWindVolume = initial.windVolume;
            currentWindPitch = initial.windPitch;
            currentImpactMultiplier = initial.impactFeedbackMultiplier;

            mainTrail = gameObject.AddComponent<TrailRenderer>();
            mainTrail.sharedMaterial = profile.trailMaterial != null
                ? profile.trailMaterial
                : RuntimeStyle.CreateMaterial(Color.white, 0f, 0.1f);
            mainTrail.endWidth = 0f;
            mainTrail.minVertexDistance = 0.05f;
            mainTrail.numCapVertices = 2;

            energyTrail = gameObject.AddComponent<LineRenderer>();
            energyTrail.sharedMaterial = profile.lineMaterial != null ? profile.lineMaterial : mainTrail.sharedMaterial;
            energyTrail.useWorldSpace = false;
            energyTrail.positionCount = 2;
            energyTrail.enabled = true;

            BuildAuraParticles();
            BuildAirflowParticles();
            BuildGroundFlowParticles();
            BuildPressureCone();
            BuildAfterimagePool();
            BuildEdgeLinePool();
            BuildLevelUpPool();
        }

        // Compatibility overload used by the pre-animation local gameplay controller.
        public void UpdateFeedback(int level, float forwardSpeed, float worldDeltaTime)
        {
            UpdateFeedback(0f, level, forwardSpeed, worldDeltaTime);
        }

        public void UpdateFeedback(float continuousSpeed, int level, float forwardSpeed, float unscaledDeltaTime)
        {
            if (profile == null) return;
            currentLevel = Mathf.Clamp(level, 1, profile.LevelCount);
            SpeedTierVisualData target = profile.Get(currentLevel);
            // Visual effects belong to the simulated world, so they use the same local
            // delta time as movement while touch/UI input remains unscaled elsewhere.
            float worldDeltaTime = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.GetWorldDeltaTime()
                : unscaledDeltaTime;
            worldDeltaTime = Mathf.Max(0f, worldDeltaTime);
            animationTime += worldDeltaTime;
            float response = 1f - Mathf.Exp(-worldDeltaTime / Mathf.Max(0.01f, profile.tierTransitionDuration));

            currentPrimary = Color.Lerp(currentPrimary, target.primaryColor, response);
            currentSecondary = Color.Lerp(currentSecondary, target.secondaryColor, response);
            currentTrailLength = Mathf.Lerp(currentTrailLength, target.trailLength + temporaryBoost * 0.3f, response);
            currentTrailWidth = Mathf.Lerp(currentTrailWidth, target.trailWidth + temporaryBoost * 0.045f, response);
            currentBrightness = Mathf.Lerp(currentBrightness, target.trailBrightness + temporaryBoost * 0.25f, response);
            currentLineIntensity = Mathf.Lerp(currentLineIntensity, target.speedLineIntensity, response);
            currentEnergyIntensity = Mathf.Lerp(currentEnergyIntensity, target.characterEnergyIntensity + temporaryBoost * 0.3f, response);
            currentEmission = Mathf.Lerp(currentEmission, target.particleEmissionRate, response);
            currentAirflowEmission = Mathf.Lerp(currentAirflowEmission, target.airflowEmission, response);
            currentAirflowSpeed = Mathf.Lerp(currentAirflowSpeed, target.airflowSpeed, response);
            currentAirflowAlpha = Mathf.Lerp(currentAirflowAlpha, target.airflowAlpha, response);
            currentGroundFlow = Mathf.Lerp(currentGroundFlow, target.groundFlowIntensity, response);
            currentAfterimageRate = Mathf.Lerp(currentAfterimageRate, target.afterimageRate, response);
            currentAfterimageLifetime = Mathf.Lerp(currentAfterimageLifetime, target.afterimageLifetime, response);
            currentPressureCone = Mathf.Lerp(currentPressureCone, target.pressureConeIntensity, response);
            currentWindVolume = Mathf.Lerp(currentWindVolume, target.windVolume, response);
            currentWindPitch = Mathf.Lerp(currentWindPitch, target.windPitch, response);
            currentImpactMultiplier = Mathf.Lerp(currentImpactMultiplier, target.impactFeedbackMultiplier, response);
            currentFovBonus = Mathf.Lerp(currentFovBonus, target.fovBonus, response);
            currentAmbientShake = Mathf.Lerp(currentAmbientShake, target.ambientShake, response);
            currentChargeLean = Mathf.Lerp(currentChargeLean, target.chargeLean, response);
            temporaryBoost = Mathf.MoveTowards(temporaryBoost, 0f, worldDeltaTime * 3.4f);

            float worldScale = BulletTimeManager.Instance != null
                ? Mathf.Max(0.1f, BulletTimeManager.Instance.WorldTimeScale)
                : 1f;
            mainTrail.time = currentTrailLength / Mathf.Max(1f, forwardSpeed * worldScale);
            mainTrail.startWidth = currentTrailWidth;
            Color trailColor = MultiplyRgb(currentPrimary, currentBrightness);
            mainTrail.startColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0.92f);
            mainTrail.endColor = new Color(currentSecondary.r, currentSecondary.g, currentSecondary.b, 0f);

            float energyLength = Mathf.Max(0.25f, currentTrailLength * 0.7f);
            energyTrail.SetPosition(0, new Vector3(0f, 0.3f, -0.1f));
            energyTrail.SetPosition(1, new Vector3(0f, 0.3f, -energyLength));
            energyTrail.startWidth = currentTrailWidth * Mathf.Lerp(0.35f, 1.15f, Mathf.Clamp01(currentEnergyIntensity));
            energyTrail.endWidth = 0f;
            energyTrail.startColor = new Color(currentSecondary.r, currentSecondary.g, currentSecondary.b, Mathf.Clamp01(currentEnergyIntensity * 0.42f));
            energyTrail.endColor = new Color(currentPrimary.r, currentPrimary.g, currentPrimary.b, 0f);

            ParticleSystem.EmissionModule emission = auraParticles.emission;
            float qualityMultiplier = performance.lowQualityMode ? performance.lowQualityParticleMultiplier : 1f;
            emission.rateOverTime = currentEmission * qualityMultiplier;
            ParticleSystem.MainModule main = auraParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(currentPrimary, currentSecondary);
            main.simulationSpeed = worldScale;

            UpdateLayeredAirflow(qualityMultiplier);
            UpdateAfterimages(worldDeltaTime);

            UpdateEdgeLines(forwardSpeed);
            UpdateLevelUpRings(worldDeltaTime);
            if (levelBurstParticles != null)
            {
                ParticleSystem.MainModule burstMain = levelBurstParticles.main;
                burstMain.simulationSpeed = worldScale;
            }
        }

        public void PlayLevelUpBurst(int level, SpeedLevelFeedbackData feedback, Color color, float strength,
            int qualityLevel, bool reducedFlash)
        {
            if (feedback == null) return;
            temporaryBoost = Mathf.Max(temporaryBoost,
                Mathf.Clamp01((feedback.trailBoostMultiplier - 1f) * strength * (reducedFlash ? 0.65f : 1f)));
            if (levelBurstParticles != null && qualityLevel > 0)
            {
                ParticleSystem.MainModule main = levelBurstParticles.main;
                Color secondary = new Color(color.r, color.g, color.b, reducedFlash ? 0.52f : 0.82f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, secondary);
                int qualityCount = qualityLevel == 1 ? Mathf.CeilToInt(feedback.burstParticleCount * 0.5f) : feedback.burstParticleCount;
                levelBurstParticles.Emit(Mathf.Clamp(Mathf.RoundToInt(qualityCount * strength), 4, 48));
            }
            if (levelUpRings == null || levelUpRings.Length == 0) return;
            int index = nextRing++ % levelUpRings.Length;
            ringTimers[index] = 0.0001f;
            ringDurations[index] = feedback.shockwaveDuration;
            ringScales[index] = feedback.shockwaveScale * strength;
            ringColors[index] = new Color(color.r, color.g, color.b, reducedFlash ? 0.38f : 0.66f);
            levelUpRings[index].enabled = true;
            PlaySonicBoomRing(
                Mathf.Clamp(feedback.shockwaveDuration * 0.72f, 0.15f, 0.24f),
                Mathf.Clamp(feedback.shockwaveScale * strength * 2.15f, 2.4f, 5.4f),
                reducedFlash ? 0.32f : 0.5f);
        }

        public void PlayHighSpeedImpactSonicBoom(int speedLevel, float strength = 1f,
            bool reducedFlash = false)
        {
            if (speedLevel < 9) return;
            float levelT = Mathf.InverseLerp(9f, 10f, Mathf.Clamp(speedLevel, 9, 10));
            float scale = Mathf.Lerp(4.6f, 5.4f, levelT) * Mathf.Clamp(strength, 0.75f, 1.35f);
            PlaySonicBoomRing(Mathf.Lerp(0.16f, 0.19f, levelT),
                Mathf.Clamp(scale, 2.4f, 6.2f), reducedFlash ? 0.32f : 0.5f);
        }

        public void Pulse(float strength)
        {
            temporaryBoost = Mathf.Max(temporaryBoost, Mathf.Clamp01(strength));
        }

        public void PulseNormalBoost()
        {
            Pulse(profile != null ? profile.normalBoostPulse : 0.2f);
        }

        public void PulseLevelUp()
        {
            Pulse(profile != null ? profile.levelUpPulse : 1f);
        }

        private void BuildAuraParticles()
        {
            GameObject root = new GameObject("SpeedAuraPool");
            root.transform.SetParent(transform, false);
            auraParticles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = auraParticles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
            main.maxParticles = profile.maxAuraParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.ShapeModule shape = auraParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.58f;
            ParticleSystem.EmissionModule emission = auraParticles.emission;
            emission.rateOverTime = 0f;
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = profile.particleMaterial != null ? profile.particleMaterial : mainTrail.sharedMaterial;
            auraParticles.Play();
        }

        private void BuildAirflowParticles()
        {
            GameObject root = new GameObject("CharacterAirflowPool");
            root.transform.SetParent(transform, false);
            airflowParticles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = airflowParticles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.05f);
            main.maxParticles = 72;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.ShapeModule shape = airflowParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.62f;
            ParticleSystem.VelocityOverLifetimeModule velocity = airflowParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.z = -1f;
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f;
            renderer.velocityScale = 0.16f;
            renderer.sharedMaterial = profile.particleMaterial != null ? profile.particleMaterial : mainTrail.sharedMaterial;
            airflowParticles.Play();
        }

        private void BuildGroundFlowParticles()
        {
            GameObject root = new GameObject("GroundFlowPool");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, -0.9f, 0.2f);
            groundFlowParticles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = groundFlowParticles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.ShapeModule shape = groundFlowParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.8f, 0.08f, 1.6f);
            ParticleSystem.VelocityOverLifetimeModule velocity = groundFlowParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.z = -1f;
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.8f;
            renderer.sharedMaterial = profile.particleMaterial != null ? profile.particleMaterial : mainTrail.sharedMaterial;
            groundFlowParticles.Play();
        }

        private void BuildPressureCone()
        {
            GameObject root = new GameObject("PressureCone");
            root.transform.SetParent(transform, false);
            pressureCone = root.AddComponent<LineRenderer>();
            pressureCone.useWorldSpace = false;
            pressureCone.positionCount = 3;
            pressureCone.sharedMaterial = profile.lineMaterial != null ? profile.lineMaterial : mainTrail.sharedMaterial;
            pressureCone.numCapVertices = 2;
            pressureCone.enabled = false;
        }

        private void BuildAfterimagePool()
        {
            int count = Mathf.Clamp(profile.afterimagePoolSize, 2, 8);
            afterimages = new LineRenderer[count];
            afterimageTimers = new float[count];
            Material material = profile.lineMaterial != null ? profile.lineMaterial : mainTrail.sharedMaterial;
            for (int i = 0; i < count; i++)
            {
                GameObject root = new GameObject("Afterimage_" + i);
                root.transform.SetParent(transform.parent, true);
                LineRenderer line = root.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.sharedMaterial = material;
                line.startWidth = 0.42f;
                line.endWidth = 0.18f;
                line.enabled = false;
                afterimages[i] = line;
            }
        }

        private void UpdateLayeredAirflow(float qualityMultiplier)
        {
            if (airflowParticles == null || groundFlowParticles == null || pressureCone == null) return;
            ParticleSystem.EmissionModule airflowEmission = airflowParticles.emission;
            airflowEmission.rateOverTime = currentAirflowEmission * qualityMultiplier * profile.highSpeedEffectIntensity;
            ParticleSystem.MainModule airflowMain = airflowParticles.main;
            airflowMain.simulationSpeed = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.WorldTimeScale
                : 1f;
            Color airflowColor = currentPrimary;
            airflowColor.a = currentAirflowAlpha;
            airflowMain.startColor = new ParticleSystem.MinMaxGradient(airflowColor, currentSecondary);
            airflowMain.startSpeed = currentAirflowSpeed;

            ParticleSystem.EmissionModule groundEmission = groundFlowParticles.emission;
            groundEmission.rateOverTime = currentGroundFlow * 22f * qualityMultiplier;
            ParticleSystem.MainModule groundMain = groundFlowParticles.main;
            groundMain.simulationSpeed = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.WorldTimeScale
                : 1f;
            Color groundColor = currentSecondary;
            groundColor.a = currentGroundFlow * 0.48f;
            groundMain.startColor = groundColor;
            groundMain.startSpeed = Mathf.Max(0.5f, currentAirflowSpeed * 0.72f);

            bool showCone = !performance.lowQualityMode && currentPressureCone > 0.02f;
            pressureCone.enabled = showCone;
            if (showCone)
            {
                float length = Mathf.Lerp(0.8f, 3.6f, currentPressureCone);
                pressureCone.SetPosition(0, new Vector3(-0.35f, 0.05f, 0f));
                pressureCone.SetPosition(1, new Vector3(0f, 0.16f, -length));
                pressureCone.SetPosition(2, new Vector3(0.35f, 0.05f, 0f));
                pressureCone.startWidth = 0.025f + currentPressureCone * 0.04f;
                pressureCone.endWidth = pressureCone.startWidth;
                Color coneColor = currentPrimary;
                coneColor.a = currentPressureCone * 0.36f;
                pressureCone.startColor = coneColor;
                pressureCone.endColor = new Color(currentSecondary.r, currentSecondary.g, currentSecondary.b, 0f);
            }
        }

        private void UpdateAfterimages(float unscaledDeltaTime)
        {
            if (afterimages == null) return;
            bool allowed = !performance.lowQualityMode && currentAfterimageRate > 0.01f;
            afterimageSpawnTimer -= unscaledDeltaTime;
            if (allowed && afterimageSpawnTimer <= 0f)
            {
                afterimageSpawnTimer = 1f / Mathf.Max(0.1f, currentAfterimageRate);
                int oldest = 0;
                for (int i = 1; i < afterimageTimers.Length; i++)
                    if (afterimageTimers[i] < afterimageTimers[oldest]) oldest = i;
                LineRenderer line = afterimages[oldest];
                Vector3 position = transform.position;
                line.SetPosition(0, position + Vector3.up * 0.75f);
                line.SetPosition(1, position - Vector3.up * 0.75f);
                line.enabled = true;
                afterimageTimers[oldest] = currentAfterimageLifetime;
            }
            for (int i = 0; i < afterimages.Length; i++)
            {
                if (afterimageTimers[i] <= 0f) continue;
                afterimageTimers[i] = Mathf.Max(0f, afterimageTimers[i] - unscaledDeltaTime);
                float alpha = afterimageTimers[i] / Mathf.Max(0.05f, currentAfterimageLifetime) * 0.28f;
                Color color = currentPrimary;
                color.a = alpha;
                afterimages[i].startColor = color;
                afterimages[i].endColor = new Color(color.r, color.g, color.b, 0f);
                if (afterimageTimers[i] <= 0f) afterimages[i].enabled = false;
            }
        }

        private void BuildEdgeLinePool()
        {
            int count = performance.lowQualityMode ? performance.lowQualitySpeedLineCount : profile.pooledSpeedLineCount;
            count = Mathf.Clamp(count, 4, 24);
            edgeLines = new LineRenderer[count];
            lineSeeds = new float[count];
            Material material = profile.lineMaterial != null ? profile.lineMaterial : mainTrail.sharedMaterial;
            for (int i = 0; i < count; i++)
            {
                GameObject lineObject = new GameObject("SpeedLine_" + i);
                lineObject.transform.SetParent(transform, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 2;
                line.sharedMaterial = material;
                line.textureMode = LineTextureMode.Stretch;
                line.numCapVertices = 0;
                edgeLines[i] = line;
                lineSeeds[i] = UnityEngine.Random.Range(0f, 24f);
            }
        }

        private void BuildLevelUpPool()
        {
            GameObject burstRoot = new GameObject("SpeedLevelUpVFX_EnergyBurst");
            burstRoot.transform.SetParent(transform, false);
            levelBurstParticles = burstRoot.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = levelBurstParticles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.EmissionModule emission = levelBurstParticles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = levelBurstParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.48f;
            ParticleSystemRenderer particleRenderer = burstRoot.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = profile.particleMaterial != null ? profile.particleMaterial : mainTrail.sharedMaterial;

            int count = Mathf.Clamp(profile.shockRingPoolSize, 2, 4);
            levelUpRings = new LineRenderer[count];
            ringTimers = new float[count];
            ringDurations = new float[count];
            ringScales = new float[count];
            ringColors = new Color[count];
            Material material = profile.lineMaterial != null ? profile.lineMaterial : mainTrail.sharedMaterial;
            const int segments = 36;
            for (int i = 0; i < count; i++)
            {
                GameObject ringObject = new GameObject("SpeedLevelUpVFX_ShockwaveRing_" + i);
                ringObject.transform.SetParent(transform, false);
                ringObject.transform.localPosition = new Vector3(0f, -0.65f, 0f);
                ringObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                LineRenderer ring = ringObject.AddComponent<LineRenderer>();
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = segments;
                ring.sharedMaterial = material;
                ring.startWidth = 0.07f;
                ring.endWidth = 0.07f;
                ring.numCornerVertices = 2;
                for (int p = 0; p < segments; p++)
                {
                    float angle = p / (float)segments * Mathf.PI * 2f;
                    ring.SetPosition(p, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.48f, 0f));
                }
                ring.enabled = false;
                levelUpRings[i] = ring;
            }

            BuildSonicBoomPool(Mathf.Clamp(count * 2, 4, 8), material);
        }

        private void BuildSonicBoomPool(int count, Material material)
        {
            sonicBoomPoolRoot = new GameObject("SpeedLevelUpVFX_SonicBoomPool").transform;
            sonicBoomPoolRoot.SetParent(transform.parent, false);
            sonicBoomRings = new LineRenderer[count];
            sonicRingTimers = new float[count];
            sonicRingDurations = new float[count];
            sonicRingScales = new float[count];
            sonicRingAlphas = new float[count];
            const int segments = 40;
            for (int i = 0; i < count; i++)
            {
                GameObject ringObject = new GameObject("SpeedLevelUpVFX_SonicBoomRing_" + i);
                ringObject.transform.SetParent(sonicBoomPoolRoot, false);
                LineRenderer ring = ringObject.AddComponent<LineRenderer>();
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = segments;
                ring.sharedMaterial = material;
                ring.startWidth = 0.28f;
                ring.endWidth = 0.28f;
                ring.numCornerVertices = 3;
                ring.numCapVertices = 2;
                for (int p = 0; p < segments; p++)
                {
                    float angle = p / (float)segments * Mathf.PI * 2f;
                    ring.SetPosition(p, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
                }
                ring.enabled = false;
                sonicBoomRings[i] = ring;
            }
        }

        private void PlaySonicBoomRing(float duration, float scale, float alpha)
        {
            if (sonicBoomRings == null || sonicBoomRings.Length == 0) return;
            int index = nextSonicRing++ % sonicBoomRings.Length;
            LineRenderer ring = sonicBoomRings[index];
            Transform ringTransform = ring.transform;
            Vector3 forward = transform.forward.sqrMagnitude > 0.001f ? transform.forward : Vector3.forward;
            ringTransform.SetPositionAndRotation(
                transform.position + Vector3.up * 0.7f + forward * 0.85f,
                Quaternion.LookRotation(forward, Vector3.up));
            ringTransform.localScale = Vector3.one * 0.28f;
            sonicRingTimers[index] = 0.0001f;
            sonicRingDurations[index] = Mathf.Max(0.01f, duration);
            sonicRingScales[index] = Mathf.Max(0.28f, scale);
            sonicRingAlphas[index] = Mathf.Clamp01(alpha);
            Color initialColor = new Color(1f, 1f, 1f, sonicRingAlphas[index]);
            ring.startColor = initialColor;
            ring.endColor = initialColor;
            ring.startWidth = 0.28f;
            ring.endWidth = 0.28f;
            ring.enabled = true;
        }

        private void UpdateLevelUpRings(float worldDeltaTime)
        {
            if (levelUpRings == null) return;
            for (int i = 0; i < levelUpRings.Length; i++)
            {
                if (ringTimers[i] <= 0f) continue;
                ringTimers[i] += worldDeltaTime;
                float t = Mathf.Clamp01(ringTimers[i] / Mathf.Max(0.01f, ringDurations[i]));
                levelUpRings[i].transform.localScale = Vector3.one * Mathf.Lerp(0.12f, ringScales[i], 1f - Mathf.Pow(1f - t, 2f));
                Color color = ringColors[i];
                color.a *= 1f - t;
                levelUpRings[i].startColor = color;
                levelUpRings[i].endColor = color;
                if (t >= 1f)
                {
                    ringTimers[i] = 0f;
                    levelUpRings[i].enabled = false;
                }
            }
        }

        private void UpdateSonicBoomRings(float worldDeltaTime)
        {
            if (sonicBoomRings == null) return;
            for (int i = 0; i < sonicBoomRings.Length; i++)
            {
                if (sonicRingTimers[i] <= 0f) continue;
                sonicRingTimers[i] += worldDeltaTime;
                float t = Mathf.Clamp01(sonicRingTimers[i] / Mathf.Max(0.01f, sonicRingDurations[i]));
                float expansion = 1f - Mathf.Pow(1f - t, 3f);
                LineRenderer ring = sonicBoomRings[i];
                ring.transform.localScale = Vector3.one * Mathf.Lerp(0.28f, sonicRingScales[i], expansion);
                float alpha = sonicRingAlphas[i] * (1f - Mathf.SmoothStep(0.08f, 1f, t));
                Color color = new Color(1f, 1f, 1f, alpha);
                ring.startColor = color;
                ring.endColor = color;
                ring.startWidth = Mathf.Lerp(0.28f, 0.1f, t);
                ring.endWidth = ring.startWidth;
                if (t >= 1f)
                {
                    sonicRingTimers[i] = 0f;
                    ring.enabled = false;
                }
            }
        }

        private void UpdateEdgeLines(float forwardSpeed)
        {
            bool visible = performance.enableSecondarySpeedLines && !performance.lowQualityMode && currentLevel >= 4;
            float timeOffset = animationTime * Mathf.Max(1f, forwardSpeed) * 1.8f;
            for (int i = 0; i < edgeLines.Length; i++)
            {
                LineRenderer line = edgeLines[i];
                line.enabled = visible;
                if (!visible) continue;
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (2.7f + (i % 3) * 0.34f);
                float z = 2f + Mathf.Repeat(lineSeeds[i] - timeOffset, 24f);
                float y = i % 4 == 0 ? 0.08f : 0.22f + (i % 3) * 0.18f;
                float length = Mathf.Lerp(0.25f, 1.8f, currentLineIntensity) * (0.75f + (i % 5) * 0.1f);
                line.SetPosition(0, new Vector3(x, y, z));
                line.SetPosition(1, new Vector3(x, y, z + length));
                line.startWidth = 0.025f + currentLineIntensity * 0.035f;
                line.endWidth = 0f;
                float alpha = currentLineIntensity * (0.5f + (i % 4) * 0.08f);
                line.startColor = new Color(currentPrimary.r, currentPrimary.g, currentPrimary.b, alpha);
                line.endColor = new Color(currentSecondary.r, currentSecondary.g, currentSecondary.b, 0f);
            }
        }

        private static Color MultiplyRgb(Color color, float multiplier)
        {
            return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
        }

        private void OnDestroy()
        {
            if (sonicBoomPoolRoot == null) return;
            if (Application.isPlaying) Destroy(sonicBoomPoolRoot.gameObject);
            else DestroyImmediate(sonicBoomPoolRoot.gameObject);
        }
    }
}
