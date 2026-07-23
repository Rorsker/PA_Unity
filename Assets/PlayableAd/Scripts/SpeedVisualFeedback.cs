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
        private TrailRenderer[] airflowTrails;
        private Material airflowMaterial;
        private Gradient mainAirflowGradient;
        private Gradient coreAirflowGradient;
        private Gradient sideAirflowGradient;
        private int airflowGradientStage = -1;
        private Texture2D airflowTexture;
        private ParticleSystem groundFlowParticles;
        private LineRenderer pressureCone;
        private Transform frontImpactAirflowRoot;
        private SpriteRenderer frontImpactShock;
        private SpriteRenderer frontImpactLeft;
        private SpriteRenderer frontImpactRight;
        private SpriteRenderer frontImpactWrapLeft;
        private SpriteRenderer frontImpactWrapRight;
        private SpriteRenderer frontImpactCore;
        private Sprite[] frontImpactPlumeSprites;
        private Sprite[] frontImpactShockSprites;
        private Texture2D frontImpactAirflowAtlas;
        private LineRenderer[] afterimages;
        private float[] afterimageTimers;
        private float afterimageSpawnTimer;
        private ParticleSystem levelBurstParticles;
        private GameObject externalWindTrailRoot;
        private GameObject accelerationAuraRoot;
        private ParticleSystem[] externalWindTrailParticles;
        private ParticleSystem[] accelerationAuraParticles;
        private bool[] accelerationAuraOriginalLoops;
        private float[] accelerationAuraReverseStartTimes;
        private bool accelerationAuraReversing;
        private float accelerationAuraReverseElapsed;
        private float accelerationAuraReverseDuration;
        private LineRenderer[] levelUpRings;
        private float[] ringTimers;
        private float[] ringDurations;
        private float[] ringScales;
        private Color[] ringColors;
        private int nextRing;
        private SpriteRenderer[] sonicBoomRings;
        private Sprite[] sonicBoomSprites;
        private float[] sonicRingTimers;
        private float[] sonicRingDurations;
        private float[] sonicRingScales;
        private float[] sonicRingAlphas;
        private Color[] sonicRingColors;
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
        private bool invulnerabilityAuraActive;
        private int currentLevel = 1;
        private float animationTime;
        private bool runningTrailsVisible = true;

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
            Initialize(visualProfile, performanceSettings, null, null);
        }

        public void Initialize(SpeedVisualProfile visualProfile, VisualPerformanceSettings performanceSettings,
            GameObject windTrailPrefab, GameObject accelerationAuraPrefab)
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
            BuildAirflowTrail();
            BuildGroundFlowParticles();
            BuildPressureCone();
            BuildFrontImpactAirflow();
            BuildAfterimagePool();
            BuildEdgeLinePool();
            BuildLevelUpPool();
            BuildExternalSpeedVfx(windTrailPrefab, accelerationAuraPrefab);
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
            float levelStrength = Mathf.InverseLerp(1f, 10f, currentLevel);
            float stageIntensity = GetImpactStageIntensity(currentLevel);

            float mainTrailLength = Mathf.Max(currentTrailLength * 0.72f,
                Mathf.Lerp(1.6f, 4.7f, levelStrength) * stageIntensity);
            mainTrail.time = mainTrailLength / Mathf.Max(1f, forwardSpeed * worldScale);
            mainTrail.emitting = runningTrailsVisible && forwardSpeed > 0.1f;
            mainTrail.startWidth = Mathf.Lerp(0.14f, 0.34f, levelStrength) * stageIntensity;
            mainTrail.endWidth = Mathf.Lerp(0.025f, 0.065f, levelStrength);
            EnsureAirflowGradients(currentLevel);
            mainTrail.colorGradient = mainAirflowGradient;

            float energyLength = Mathf.Max(0.25f, currentTrailLength * 0.7f);
            energyTrail.SetPosition(0, new Vector3(0f, 0.3f, -0.1f));
            energyTrail.SetPosition(1, new Vector3(0f, 0.3f, -energyLength));
            energyTrail.startWidth = Mathf.Lerp(0.08f, 0.22f, levelStrength) * stageIntensity;
            energyTrail.endWidth = 0f;
            energyTrail.colorGradient = sideAirflowGradient;
            energyTrail.enabled = runningTrailsVisible;

            ParticleSystem.EmissionModule emission = auraParticles.emission;
            float qualityMultiplier = performance.lowQualityMode ? performance.lowQualityParticleMultiplier : 1f;
            emission.rateOverTime = runningTrailsVisible ? currentEmission * qualityMultiplier : 0f;
            ParticleSystem.MainModule main = auraParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(currentPrimary, currentSecondary);
            main.simulationSpeed = worldScale;

            UpdateLayeredAirflow(qualityMultiplier, forwardSpeed, worldScale);
            UpdateFrontImpactAirflow(forwardSpeed);
            UpdateAfterimages(worldDeltaTime);
            UpdateExternalSpeedVfx(forwardSpeed, continuousSpeed, worldDeltaTime, worldScale);

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
            ringDurations[index] = Mathf.Max(0.9f, feedback.shockwaveDuration * 2.4f);
            ringScales[index] = feedback.shockwaveScale * strength;
            ringColors[index] = new Color(color.r, color.g, color.b, reducedFlash ? 0.38f : 0.66f);
            levelUpRings[index].enabled = true;
            PlaySonicBoomRing(
                Mathf.Clamp(feedback.shockwaveDuration * 0.72f, 0.15f, 0.24f),
                Mathf.Clamp(feedback.shockwaveScale * strength * 2.15f, 2.4f, 5.4f),
                reducedFlash ? 0.32f : 0.5f, color);
        }

        public void PlayHighSpeedImpactSonicBoom(int speedLevel, float strength = 1f,
            bool reducedFlash = false)
        {
            if (speedLevel < 9) return;
            float levelT = Mathf.InverseLerp(9f, 10f, Mathf.Clamp(speedLevel, 9, 10));
            float scale = Mathf.Lerp(4.6f, 5.4f, levelT) * Mathf.Clamp(strength, 0.75f, 1.35f);
            PlaySonicBoomRing(Mathf.Lerp(0.16f, 0.19f, levelT),
                Mathf.Clamp(scale, 2.4f, 6.2f), reducedFlash ? 0.32f : 0.5f,
                GetImpactStageColor(speedLevel));
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

        public void SetInvulnerabilityAuraActive(bool active)
        {
            invulnerabilityAuraActive = active;
        }

public void SetRunningTrailsVisible(bool visible)
        {
            runningTrailsVisible = visible;
            if (mainTrail != null)
            {
                mainTrail.emitting = visible;
                if (!visible) mainTrail.Clear();
            }

            if (airflowTrails != null)
            {
                for (int i = 0; i < airflowTrails.Length; i++)
                {
                    if (airflowTrails[i] == null) continue;
                    airflowTrails[i].emitting = visible;
                    if (!visible) airflowTrails[i].Clear();
                }
            }

            if (energyTrail != null) energyTrail.enabled = visible;
            if (frontImpactAirflowRoot != null)
                frontImpactAirflowRoot.gameObject.SetActive(visible && currentLevel >= 1);

            if (auraParticles != null)
            {
                ParticleSystem.EmissionModule emission = auraParticles.emission;
                emission.rateOverTime = visible
                    ? currentEmission * (performance.lowQualityMode ? performance.lowQualityParticleMultiplier : 1f)
                    : 0f;
                if (visible) auraParticles.Play();
                else auraParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (groundFlowParticles != null)
            {
                ParticleSystem.EmissionModule emission = groundFlowParticles.emission;
                emission.rateOverTime = visible ? currentGroundFlow * 22f : 0f;
                if (visible) groundFlowParticles.Play();
                else groundFlowParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (!visible)
            {
                if (pressureCone != null) pressureCone.enabled = false;
                if (edgeLines != null)
                    for (int i = 0; i < edgeLines.Length; i++) edgeLines[i].enabled = false;
                if (afterimages != null)
                    for (int i = 0; i < afterimages.Length; i++) afterimages[i].enabled = false;
                SetExternalVfxActive(externalWindTrailRoot, externalWindTrailParticles, false);
                SetExternalVfxActive(accelerationAuraRoot, accelerationAuraParticles, false);
            }
        }


        private void BuildExternalSpeedVfx(GameObject windTrailPrefab, GameObject auraPrefab)
        {
            externalWindTrailRoot = InstantiateExternalVfx(windTrailPrefab, "CFXR_RunWindTrail",
                new Vector3(0f, -0.35f, -0.58f), Quaternion.identity,
                new Vector3(0.46f, 0.46f, 0.52f),
                out externalWindTrailParticles);
            ConfigureExternalWindTrail();
            accelerationAuraRoot = InstantiateExternalVfx(auraPrefab, "CFXR_AccelerationRunicAura",
                new Vector3(0f, -0.96f, 0f), Quaternion.identity, Vector3.one,
                out accelerationAuraParticles);
            accelerationAuraParticles = KeepSafeAuraLayers(accelerationAuraParticles);

            SetExternalVfxActive(externalWindTrailRoot, externalWindTrailParticles, false);
            SetExternalVfxActive(accelerationAuraRoot, accelerationAuraParticles, false);
            accelerationAuraOriginalLoops = new bool[accelerationAuraParticles.Length];
            accelerationAuraReverseStartTimes = new float[accelerationAuraParticles.Length];
            for (int i = 0; i < accelerationAuraParticles.Length; i++)
            {
                accelerationAuraOriginalLoops[i] = accelerationAuraParticles[i].main.loop;
                ParticleSystemRenderer renderer = accelerationAuraParticles[i].GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = 20;
                    renderer.sortingFudge = -1f;
                }
            }
        }

        // Only the two runic-ring layers are reliable in the player render path. The prefab's
        // root mesh and stretched Rays layer can render as an opaque red rectangle on the road.
        private static ParticleSystem[] KeepSafeAuraLayers(ParticleSystem[] particles)
        {
            if (particles == null || particles.Length == 0) return new ParticleSystem[0];

            int safeCount = 0;
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (IsSafeAuraLayer(particle))
                {
                    safeCount++;
                    continue;
                }

                if (particle == null) continue;
                ParticleSystemRenderer renderer = particle.GetComponent<ParticleSystemRenderer>();
                if (renderer != null) renderer.enabled = false;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ParticleSystem[] safeParticles = new ParticleSystem[safeCount];
            int safeIndex = 0;
            for (int i = 0; i < particles.Length; i++)
                if (IsSafeAuraLayer(particles[i])) safeParticles[safeIndex++] = particles[i];
            return safeParticles;
        }

        private static bool IsSafeAuraLayer(ParticleSystem particle)
        {
            if (particle == null) return false;
            string layerName = particle.gameObject.name;
            return layerName == "Runes" || layerName == "Runes small";
        }

        private void UpdateExternalSpeedVfx(float forwardSpeed, float continuousSpeed,
            float worldDeltaTime, float worldScale)
        {
            if (!runningTrailsVisible)
            {
                SetExternalVfxActive(externalWindTrailRoot, externalWindTrailParticles, false);
                SetExternalVfxActive(accelerationAuraRoot, accelerationAuraParticles, false);
                return;
            }

            float normalizedSpeed = Mathf.InverseLerp(1f, 10f, continuousSpeed);
            bool running = forwardSpeed > 0.1f;
            SetExternalVfxActive(externalWindTrailRoot, externalWindTrailParticles, running);

            bool showInvulnerabilityAura = running && invulnerabilityAuraActive;
            if (showInvulnerabilityAura)
            {
                RestartAccelerationAuraIfReversing();
                SetExternalVfxActive(accelerationAuraRoot, accelerationAuraParticles, true);
            }
            else
            {
                UpdateAccelerationAuraReverse(worldDeltaTime);
            }

            float windSimulationSpeed = Mathf.Lerp(0.8f, 1.65f, normalizedSpeed);
            SetParticleSimulationSpeed(externalWindTrailParticles, windSimulationSpeed * worldScale);
            SetParticleSimulationSpeed(accelerationAuraParticles,
                Mathf.Lerp(0.9f, 1.35f, normalizedSpeed) * worldScale);
        }

        private void ConfigureExternalWindTrail()
        {
            for (int i = 0; i < externalWindTrailParticles.Length; i++)
            {
                ParticleSystem particle = externalWindTrailParticles[i];
                ParticleSystem.MainModule main = particle.main;
                main.startLifetimeMultiplier *= 0.48f;
                ParticleSystem.TrailModule trails = particle.trails;
                if (trails.enabled) trails.lifetimeMultiplier *= 0.52f;
            }
        }

        private void RestartAccelerationAuraIfReversing()
        {
            if (!accelerationAuraReversing) return;
            accelerationAuraReversing = false;
            for (int i = 0; i < accelerationAuraParticles.Length; i++)
            {
                ParticleSystem.MainModule main = accelerationAuraParticles[i].main;
                main.loop = accelerationAuraOriginalLoops != null && i < accelerationAuraOriginalLoops.Length
                    ? accelerationAuraOriginalLoops[i]
                    : true;
                accelerationAuraParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void UpdateAccelerationAuraReverse(float worldDeltaTime)
        {
            if (accelerationAuraRoot == null || !accelerationAuraRoot.activeSelf) return;
            if (!accelerationAuraReversing)
            {
                accelerationAuraReversing = true;
                accelerationAuraReverseElapsed = 0f;
                accelerationAuraReverseDuration = 0.6f;
                for (int i = 0; i < accelerationAuraParticles.Length; i++)
                {
                    ParticleSystem particle = accelerationAuraParticles[i];
                    ParticleSystem.MainModule main = particle.main;
                    main.loop = false;
                    float latestVisibleTime = Mathf.Max(0.01f, main.duration - 0.001f);
                    accelerationAuraReverseStartTimes[i] = Mathf.Min(0.55f, latestVisibleTime);
                }
                for (int i = 0; i < accelerationAuraParticles.Length; i++)
                {
                    ParticleSystem particle = accelerationAuraParticles[i];
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particle.Simulate(accelerationAuraReverseStartTimes[i], false, true, false);
                }
            }

            accelerationAuraReverseElapsed += worldDeltaTime;
            float reverseProgress = Mathf.Clamp01(accelerationAuraReverseElapsed / Mathf.Max(0.1f, accelerationAuraReverseDuration));
            float reverseTime = 1f - reverseProgress;
            for (int i = 0; i < accelerationAuraParticles.Length; i++)
            {
                ParticleSystem particle = accelerationAuraParticles[i];
                particle.Simulate(Mathf.Max(0f, accelerationAuraReverseStartTimes[i] * reverseTime),
                    false, true, false);
            }

            if (reverseProgress < 1f) return;
            for (int i = 0; i < accelerationAuraParticles.Length; i++)
            {
                ParticleSystem.MainModule main = accelerationAuraParticles[i].main;
                main.loop = accelerationAuraOriginalLoops != null && i < accelerationAuraOriginalLoops.Length
                    ? accelerationAuraOriginalLoops[i]
                    : true;
                accelerationAuraParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            accelerationAuraRoot.SetActive(false);
            accelerationAuraReversing = false;
        }

        private GameObject InstantiateExternalVfx(GameObject prefab, string instanceName, Vector3 localPosition,
            Quaternion localRotation, Vector3 localScale, out ParticleSystem[] particles)
        {
            particles = new ParticleSystem[0];
            if (prefab == null) return null;
            GameObject instance = Object.Instantiate(prefab);
            instance.name = instanceName;
            instance.transform.SetParent(transform, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
            particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            return instance;
        }

        private void SetExternalVfxActive(GameObject root, ParticleSystem[] particles, bool active)
        {
            if (root == null) return;
            if (active)
            {
                if (!root.activeSelf) root.SetActive(true);
                for (int i = 0; i < particles.Length; i++)
                    if (!particles[i].isPlaying) particles[i].Play(true);
            }
            else if (root.activeSelf)
            {
                for (int i = 0; i < particles.Length; i++)
                    particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                root.SetActive(false);
            }
        }

        private static void SetParticleSimulationSpeed(ParticleSystem[] particles, float speed)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.simulationSpeed = speed;
            }
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

        private void BuildAirflowTrail()
        {
            airflowMaterial = BuildAirflowMaterial();
            airflowTrails = new[]
            {
                BuildAirflowTrailRenderer("CharacterAirflowCore",
                    new Vector3(0f, 0.62f, 0.12f), -4),
                BuildAirflowTrailRenderer("CharacterAirflowLeft",
                    new Vector3(-0.31f, 0.57f, 0.18f), -3),
                BuildAirflowTrailRenderer("CharacterAirflowRight",
                    new Vector3(0.31f, 0.57f, 0.18f), -3)
            };
        }

        private TrailRenderer BuildAirflowTrailRenderer(
            string objectName, Vector3 localPosition, int sortingOrder)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = localPosition;
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.sharedMaterial = airflowMaterial;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.minVertexDistance = 0.035f;
            trail.numCapVertices = 4;
            trail.numCornerVertices = 3;
            trail.autodestruct = false;
            trail.generateLightingData = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sortingOrder = sortingOrder;
            trail.time = 0.01f;
            trail.startWidth = 0f;
            trail.endWidth = 0f;
            trail.emitting = false;
            return trail;
        }


        private Material BuildAirflowMaterial()
        {
            Material source = profile.trailMaterial != null ? profile.trailMaterial : mainTrail.sharedMaterial;
            Material material = new Material(source);
            airflowTexture = BuildAirflowTexture();
            material.mainTexture = airflowTexture;
            return material;
        }

        private static Texture2D BuildAirflowTexture()
        {
            const int width = 64;
            const int height = 128;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float vertical = y / (height - 1f);
                float endFade = Mathf.SmoothStep(0f, 1f,
                    Mathf.Min(vertical, 1f - vertical) * 5f);
                float streak = 0.72f + 0.28f * Mathf.Pow(
                    0.5f + 0.5f * Mathf.Sin(vertical * Mathf.PI * 13f), 2f);
                for (int x = 0; x < width; x++)
                {
                    float horizontal = Mathf.Abs(x / (width - 1f) * 2f - 1f);
                    float edgeFade = 1f - Mathf.SmoothStep(0.36f, 1f, horizontal);
                    pixels[y * width + x] = new Color(1f, 1f, 1f,
                        edgeFade * endFade * streak);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
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
                root.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
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

        private void UpdateLayeredAirflow(float qualityMultiplier, float forwardSpeed, float worldScale)
        {
            if (airflowTrails == null || groundFlowParticles == null || pressureCone == null) return;

            float levelStrength = Mathf.InverseLerp(1f, 10f, currentLevel);
            float airflowStrength = Mathf.Clamp01(Mathf.Max(
                currentAirflowAlpha / 0.74f, 0.22f + levelStrength * 0.78f));
            float stageIntensity = GetImpactStageIntensity(currentLevel);
            Color stageColor = GetImpactStageColor(currentLevel);
            BuildAirflowPalette(stageColor, out Color deepColor, out Color midColor,
                out Color brightColor, out Color glintColor);
            bool showAirflow = runningTrailsVisible && forwardSpeed > 0.1f;

            for (int i = 0; i < airflowTrails.Length; i++)
            {
                TrailRenderer trail = airflowTrails[i];
                if (trail == null) continue;
                bool sideTrail = i > 0;
                trail.emitting = showAirflow && (!performance.lowQualityMode || i < 2);
                if (trail.emitting)
                {
                    float trailLength = Mathf.Lerp(sideTrail ? 1.4f : 1.8f,
                        sideTrail ? 4.2f : 5.2f, airflowStrength) * stageIntensity;
                    trail.time = trailLength / Mathf.Max(1f, forwardSpeed * worldScale);
                    trail.startWidth = Mathf.Lerp(sideTrail ? 0.2f : 0.68f,
                        sideTrail ? 0.34f : 1.02f, airflowStrength) * stageIntensity;
                    trail.endWidth = Mathf.Lerp(sideTrail ? 0.05f : 0.1f,
                        sideTrail ? 0.1f : 0.18f, airflowStrength);
                    trail.colorGradient = sideTrail
                        ? sideAirflowGradient
                        : coreAirflowGradient;
                }
                else
                {
                    trail.time = 0.01f;
                    trail.startWidth = 0f;
                    trail.endWidth = 0f;
                    trail.Clear();
                }
            }

            ParticleSystem.EmissionModule groundEmission = groundFlowParticles.emission;
            groundEmission.rateOverTime = runningTrailsVisible
                ? currentGroundFlow * 22f * qualityMultiplier
                : 0f;
            ParticleSystem.MainModule groundMain = groundFlowParticles.main;
            groundMain.simulationSpeed = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.WorldTimeScale
                : 1f;
            Color groundColor = midColor;
            groundColor.a = currentGroundFlow * 0.56f;
            groundMain.startColor = groundColor;
            groundMain.startSpeed = Mathf.Max(0.5f, currentAirflowSpeed * 0.72f);

            bool showCone = runningTrailsVisible && !performance.lowQualityMode
                && currentPressureCone > 0.02f;
            pressureCone.enabled = showCone;
            if (showCone)
            {
                float length = Mathf.Lerp(0.8f, 3.6f, currentPressureCone);
                pressureCone.SetPosition(0, new Vector3(-0.35f, 0.05f, 0f));
                pressureCone.SetPosition(1, new Vector3(0f, 0.16f, -length));
                pressureCone.SetPosition(2, new Vector3(0.35f, 0.05f, 0f));
                pressureCone.startWidth = 0.025f + currentPressureCone * 0.04f;
                pressureCone.endWidth = pressureCone.startWidth;
                Color coneColor = brightColor;
                coneColor.a = currentPressureCone * 0.5f;
                pressureCone.startColor = coneColor;
                pressureCone.endColor = new Color(deepColor.r, deepColor.g, deepColor.b, 0f);
            }
        }

        private void UpdateAfterimages(float worldDeltaTime)
        {
            if (afterimages == null) return;
            bool allowed = runningTrailsVisible && !performance.lowQualityMode && currentAfterimageRate > 0.01f;
            afterimageSpawnTimer -= worldDeltaTime;
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
                afterimageTimers[i] = Mathf.Max(0f, afterimageTimers[i] - worldDeltaTime);
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

            BuildSonicBoomPool(Mathf.Clamp(count * 2, 4, 8));
        }

        private void BuildSonicBoomPool(int count)
        {
            Texture2D atlas = Resources.Load<Texture2D>("ImpactTextures/ImpactBurstAtlas");
            if (atlas == null) return;
            const int atlasColumns = 6;
            const int atlasRows = 6;
            float cellWidth = atlas.width / (float)atlasColumns;
            float cellHeight = atlas.height / (float)atlasRows;
            int[] frameColumns = { 2, 3, 5 };
            sonicBoomSprites = new Sprite[frameColumns.Length];
            for (int i = 0; i < frameColumns.Length; i++)
            {
                Rect rect = new Rect(frameColumns[i] * cellWidth,
                    atlas.height - cellHeight * 3f, cellWidth, cellHeight);
                sonicBoomSprites[i] = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f),
                    cellWidth * 0.5f, 0, SpriteMeshType.FullRect);
                sonicBoomSprites[i].name = "RuntimeCollisionShockwave_" + i;
            }

            sonicBoomPoolRoot = new GameObject("SpeedLevelUpVFX_SonicBoomPool").transform;
            sonicBoomPoolRoot.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            sonicBoomPoolRoot.SetParent(transform.parent, false);
            sonicBoomRings = new SpriteRenderer[count];
            sonicRingTimers = new float[count];
            sonicRingDurations = new float[count];
            sonicRingScales = new float[count];
            sonicRingAlphas = new float[count];
            sonicRingColors = new Color[count];
            for (int i = 0; i < count; i++)
            {
                GameObject ringObject = new GameObject("CollisionShockwaveTexture_" + i);
                ringObject.transform.SetParent(sonicBoomPoolRoot, false);
                SpriteRenderer ring = ringObject.AddComponent<SpriteRenderer>();
                ring.sprite = sonicBoomSprites[i % sonicBoomSprites.Length];
                ring.sortingOrder = 8;
                ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ring.receiveShadows = false;
                ring.enabled = false;
                sonicBoomRings[i] = ring;
            }
        }

        private void PlaySonicBoomRing(float duration, float scale, float alpha, Color stageColor)
        {
            if (sonicBoomRings == null || sonicBoomRings.Length == 0) return;
            int index = nextSonicRing++ % sonicBoomRings.Length;
            SpriteRenderer ring = sonicBoomRings[index];
            Transform ringTransform = ring.transform;
            Vector3 forward = transform.forward.sqrMagnitude > 0.001f ? transform.forward : Vector3.forward;
            ringTransform.SetPositionAndRotation(
                transform.position + Vector3.up * 0.7f + forward * 0.85f,
                Quaternion.LookRotation(forward, Vector3.up));
            ringTransform.localScale = Vector3.one * 0.22f;
            ring.sprite = sonicBoomSprites[index % sonicBoomSprites.Length];
            sonicRingTimers[index] = 0.0001f;
            sonicRingDurations[index] = Mathf.Max(0.01f, duration);
            sonicRingScales[index] = Mathf.Max(0.28f, scale);
            sonicRingAlphas[index] = Mathf.Clamp01(alpha);
            sonicRingColors[index] = Color.Lerp(stageColor, Color.white, 0.22f);
            Color initialColor = sonicRingColors[index];
            initialColor.a = sonicRingAlphas[index];
            ring.color = initialColor;
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
                float scale;
                float alpha;
                if (t < 0.22f)
                {
                    float phase = t / 0.22f;
                    scale = Mathf.Lerp(0.12f, ringScales[i] * 1.16f, 1f - Mathf.Pow(1f - phase, 3f));
                    alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phase * 2f));
                }
                else if (t < 0.42f)
                {
                    float phase = (t - 0.22f) / 0.2f;
                    scale = Mathf.Lerp(ringScales[i] * 1.16f, ringScales[i], Mathf.SmoothStep(0f, 1f, phase));
                    alpha = 1f;
                }
                else if (t < 0.68f)
                {
                    scale = ringScales[i] * (1f + Mathf.Sin((t - 0.42f) * Mathf.PI * 4f) * 0.018f);
                    alpha = 0.92f;
                }
                else
                {
                    float phase = (t - 0.68f) / 0.32f;
                    float eased = Mathf.SmoothStep(0f, 1f, phase);
                    scale = Mathf.Lerp(ringScales[i], 0.06f, eased);
                    alpha = 1f - eased;
                }
                levelUpRings[i].transform.localScale = Vector3.one * scale;
                Color color = ringColors[i];
                color.a *= alpha;
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
                SpriteRenderer ring = sonicBoomRings[i];
                float scale = Mathf.Lerp(0.22f, sonicRingScales[i], expansion);
                float distortion = Mathf.Sin(sonicRingTimers[i] * 34f + i) * 0.035f;
                ring.transform.localScale = new Vector3(
                    scale * (1f + distortion), scale * (1f - distortion * 0.7f), 1f);
                ring.transform.Rotate(0f, 0f, (i % 2 == 0 ? 1f : -1f) *
                    95f * worldDeltaTime, Space.Self);
                float alpha = sonicRingAlphas[i] * (1f - Mathf.SmoothStep(0.08f, 1f, t));
                Color color = sonicRingColors[i];
                color.a = alpha;
                ring.color = color;
                if (t >= 1f)
                {
                    sonicRingTimers[i] = 0f;
                    ring.enabled = false;
                }
            }
        }

        private void UpdateEdgeLines(float forwardSpeed)
        {
            bool visible = runningTrailsVisible && performance.enableSecondarySpeedLines && !performance.lowQualityMode && currentLevel >= 4;
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
            DestroyFrontAirflowSprites();
            if (afterimages != null)
            {
                for (int i = 0; i < afterimages.Length; i++)
                {
                    if (afterimages[i] == null) continue;
                    if (Application.isPlaying) Destroy(afterimages[i].gameObject);
                    else DestroyImmediate(afterimages[i].gameObject);
                }
            }
            if (sonicBoomSprites != null)
            {
                for (int i = 0; i < sonicBoomSprites.Length; i++)
                {
                    if (sonicBoomSprites[i] == null) continue;
                    if (Application.isPlaying) Destroy(sonicBoomSprites[i]);
                    else DestroyImmediate(sonicBoomSprites[i]);
                }
            }
            if (sonicBoomPoolRoot != null)
            {
                if (Application.isPlaying) Destroy(sonicBoomPoolRoot.gameObject);
                else DestroyImmediate(sonicBoomPoolRoot.gameObject);
            }
            if (airflowMaterial != null)
            {
                if (Application.isPlaying) Destroy(airflowMaterial);
                else DestroyImmediate(airflowMaterial);
            }
            if (airflowTexture != null)
            {
                if (Application.isPlaying) Destroy(airflowTexture);
                else DestroyImmediate(airflowTexture);
            }
        }


        private void BuildFrontImpactAirflow()
        {
            frontImpactAirflowAtlas =
                Resources.Load<Texture2D>("SpeedTextures/FrontImpactAirflowAtlas");
            if (frontImpactAirflowAtlas == null) return;

            int[] plumeFrames = { 0, 2, 3, 4 };
            int[] shockFrames = { 1, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 };
            frontImpactPlumeSprites = BuildFrontAirflowSprites(plumeFrames);
            frontImpactShockSprites = BuildFrontAirflowSprites(shockFrames);

            frontImpactAirflowRoot = new GameObject("FrontImpactAirflow").transform;
            frontImpactAirflowRoot.SetParent(transform, false);
            frontImpactAirflowRoot.localPosition = new Vector3(0f, 0.26f, 0.9f);

            frontImpactShock = BuildFrontAirflowRenderer(
                "FrontImpactShockWave", frontImpactAirflowRoot, -4);
            frontImpactShock.transform.localPosition = new Vector3(0f, -0.14f, 0.16f);

            frontImpactLeft = BuildFrontAirflowRenderer(
                "FrontImpactTurbulenceLeft", frontImpactAirflowRoot, -3);
            frontImpactLeft.transform.localPosition = new Vector3(-0.22f, -0.05f, 0f);
            frontImpactLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 9f);

            frontImpactRight = BuildFrontAirflowRenderer(
                "FrontImpactTurbulenceRight", frontImpactAirflowRoot, -3);
            frontImpactRight.transform.localPosition = new Vector3(0.22f, -0.05f, -0.02f);
            frontImpactRight.transform.localRotation = Quaternion.Euler(0f, 0f, -9f);
            frontImpactRight.flipX = true;

            frontImpactWrapLeft = BuildFrontAirflowRenderer(
                "FrontImpactWrapLeft", frontImpactAirflowRoot, -5);
            frontImpactWrapLeft.transform.localPosition = new Vector3(-0.58f, -0.12f, -0.56f);
            frontImpactWrapLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);

            frontImpactWrapRight = BuildFrontAirflowRenderer(
                "FrontImpactWrapRight", frontImpactAirflowRoot, -5);
            frontImpactWrapRight.transform.localPosition = new Vector3(0.58f, -0.12f, -0.56f);
            frontImpactWrapRight.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            frontImpactWrapRight.flipX = true;

            frontImpactCore = BuildFrontAirflowRenderer(
                "FrontImpactCoreHighlight", frontImpactAirflowRoot, -2);
            frontImpactCore.transform.localPosition = new Vector3(0f, -0.1f, 0.12f);

            frontImpactShock.sprite = frontImpactShockSprites[0];
            frontImpactLeft.sprite = frontImpactPlumeSprites[0];
            frontImpactRight.sprite = frontImpactPlumeSprites[2];
            frontImpactWrapLeft.sprite = frontImpactPlumeSprites[1];
            frontImpactWrapRight.sprite = frontImpactPlumeSprites[3];
            frontImpactCore.sprite = frontImpactShockSprites[1];
            frontImpactAirflowRoot.gameObject.SetActive(false);
        }

        private Sprite[] BuildFrontAirflowSprites(int[] frameIndices)
        {
            const int columns = 4;
            const int rows = 5;
            float cellWidth = frontImpactAirflowAtlas.width / (float)columns;
            float cellHeight = frontImpactAirflowAtlas.height / (float)rows;
            Sprite[] sprites = new Sprite[frameIndices.Length];
            for (int i = 0; i < frameIndices.Length; i++)
            {
                int frame = frameIndices[i];
                int column = frame % columns;
                int rowFromTop = frame / columns;
                Rect rect = new Rect(column * cellWidth,
                    frontImpactAirflowAtlas.height - (rowFromTop + 1) * cellHeight,
                    cellWidth, cellHeight);
                sprites[i] = Sprite.Create(frontImpactAirflowAtlas, rect,
                    new Vector2(0.5f, 0.5f), 128f, 0, SpriteMeshType.FullRect);
                sprites[i].name = "RuntimeFrontAirflow_" + frame;
            }
            return sprites;
        }

        private static SpriteRenderer BuildFrontAirflowRenderer(
            string objectName, Transform parent, int sortingOrder)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(parent, false);
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void UpdateFrontImpactAirflow(float forwardSpeed)
        {
            if (frontImpactAirflowRoot == null || frontImpactShockSprites == null ||
                frontImpactPlumeSprites == null) return;

            bool visible = runningTrailsVisible && forwardSpeed > 0.1f;
            if (frontImpactAirflowRoot.gameObject.activeSelf != visible)
                frontImpactAirflowRoot.gameObject.SetActive(visible);
            if (!visible) return;

            float levelStrength = Mathf.InverseLerp(1f, 10f, currentLevel);
            float pulse = 1f + Mathf.Sin(animationTime * Mathf.Lerp(7f, 12f, levelStrength))
                * Mathf.Lerp(0.045f, 0.1f, levelStrength);
            float boost = 1f + temporaryBoost * 0.1f;
            float stageIntensity = GetImpactStageIntensity(currentLevel);
            Color stageColor = GetImpactStageColor(currentLevel);
            BuildAirflowPalette(stageColor, out Color deepColor, out Color midColor,
                out Color brightColor, out Color glintColor);

            int shockFrame = (Mathf.FloorToInt(animationTime *
                Mathf.Lerp(4f, 7.5f, levelStrength)) + currentLevel * 2)
                % frontImpactShockSprites.Length;
            int plumeFrame = (Mathf.FloorToInt(animationTime *
                Mathf.Lerp(5f, 9f, levelStrength)) + currentLevel)
                % frontImpactPlumeSprites.Length;

            frontImpactShock.sprite = frontImpactShockSprites[shockFrame];
            frontImpactCore.sprite =
                frontImpactShockSprites[(shockFrame + 5) % frontImpactShockSprites.Length];
            frontImpactLeft.sprite = frontImpactPlumeSprites[plumeFrame];
            frontImpactRight.sprite =
                frontImpactPlumeSprites[(plumeFrame + 2) % frontImpactPlumeSprites.Length];
            frontImpactWrapLeft.sprite =
                frontImpactPlumeSprites[(plumeFrame + 1) % frontImpactPlumeSprites.Length];
            frontImpactWrapRight.sprite =
                frontImpactPlumeSprites[(plumeFrame + 3) % frontImpactPlumeSprites.Length];

            float shockAlpha = Mathf.Clamp01(
                Mathf.Lerp(0.48f, 0.9f, levelStrength) * stageIntensity * boost);
            float plumeAlpha = Mathf.Clamp01(
                Mathf.Lerp(0.42f, 0.78f, levelStrength) * stageIntensity * boost);
            float wrapAlpha = Mathf.Clamp01(
                Mathf.Lerp(0.34f, 0.68f, levelStrength) * stageIntensity * boost);
            frontImpactShock.color = new Color(
                midColor.r, midColor.g, midColor.b, shockAlpha);
            frontImpactLeft.color = new Color(
                brightColor.r, brightColor.g, brightColor.b, plumeAlpha);
            frontImpactRight.color = frontImpactLeft.color;
            frontImpactWrapLeft.color = new Color(
                stageColor.r, stageColor.g, stageColor.b, wrapAlpha);
            frontImpactWrapRight.color = frontImpactWrapLeft.color;
            frontImpactCore.color = new Color(
                glintColor.r, glintColor.g, glintColor.b, shockAlpha * 0.72f);

            frontImpactAirflowRoot.localPosition = new Vector3(
                0f, Mathf.Lerp(0.2f, 0.34f, levelStrength),
                Mathf.Lerp(0.92f, 1.45f, levelStrength));

            frontImpactShock.transform.localScale = new Vector3(
                Mathf.Lerp(0.62f, 0.78f, levelStrength) * pulse * boost,
                Mathf.Lerp(0.54f, 0.72f, levelStrength) * boost, 1f);
            Vector3 plumeScale = new Vector3(
                Mathf.Lerp(0.38f, 0.5f, levelStrength) * boost,
                Mathf.Lerp(0.82f, 1.08f, levelStrength) * pulse * boost, 1f);
            frontImpactLeft.transform.localScale = plumeScale;
            frontImpactRight.transform.localScale = plumeScale;

            float wrapOffset = Mathf.Lerp(0.34f, 0.44f, levelStrength);
            float wrapDepth = Mathf.Lerp(-0.48f, -0.72f, levelStrength);
            frontImpactWrapLeft.transform.localPosition =
                new Vector3(-wrapOffset, -0.12f, wrapDepth);
            frontImpactWrapRight.transform.localPosition =
                new Vector3(wrapOffset, -0.12f, wrapDepth);
            Vector3 wrapScale = new Vector3(
                Mathf.Lerp(0.28f, 0.38f, levelStrength) * boost,
                Mathf.Lerp(0.78f, 1.08f, levelStrength) * pulse * boost, 1f);
            frontImpactWrapLeft.transform.localScale = wrapScale;
            frontImpactWrapRight.transform.localScale = wrapScale;

            frontImpactCore.transform.localScale = new Vector3(
                Mathf.Lerp(0.34f, 0.46f, levelStrength) * pulse * boost,
                Mathf.Lerp(0.32f, 0.46f, levelStrength) * boost, 1f);
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
            if (speedLevel <= 2) return 1.38f;
            if (speedLevel <= 6) return 1.22f;
            if (speedLevel <= 8) return 1f;
            return 1.12f;
        }

        private static void BuildAirflowPalette(Color source, out Color deep,
            out Color mid, out Color bright, out Color glint)
        {
            Color.RGBToHSV(source, out float hue, out float saturation, out float value);
            deep = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 1.08f + 0.04f),
                Mathf.Clamp(value * 0.62f, 0.12f, 1f));
            mid = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 0.88f),
                Mathf.Clamp(value * 1.02f, 0.35f, 1f));
            bright = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 0.48f),
                Mathf.Clamp(value * 1.24f, 0.78f, 1f));
            glint = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 0.18f), 1f);
            deep.a = 0.88f;
            mid.a = 1f;
            bright.a = 1f;
            glint.a = 1f;
        }

        private void EnsureAirflowGradients(int speedLevel)
        {
            int stage = speedLevel <= 2 ? 0 : speedLevel <= 6 ? 1 : speedLevel <= 8 ? 2 : 3;
            if (airflowGradientStage == stage && mainAirflowGradient != null) return;

            airflowGradientStage = stage;
            Color stageColor = GetImpactStageColor(speedLevel);
            float intensity = GetImpactStageIntensity(speedLevel);
            BuildAirflowPalette(stageColor, out Color deep, out Color mid,
                out Color bright, out Color glint);

            mainAirflowGradient = BuildTailGradient(
                glint, bright, mid, deep, Mathf.Clamp01(0.5f * intensity));
            coreAirflowGradient = BuildTailGradient(
                glint, bright, mid, deep, Mathf.Clamp01(0.82f * intensity));
            sideAirflowGradient = BuildTailGradient(
                bright, glint, mid, deep, Mathf.Clamp01(0.7f * intensity));
        }

        private static Gradient BuildTailGradient(Color start, Color highlight,
            Color middle, Color end, float alpha)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(highlight, 0.18f),
                    new GradientColorKey(middle, 0.52f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(alpha * 0.72f, 0f),
                    new GradientAlphaKey(alpha, 0.16f),
                    new GradientAlphaKey(alpha * 0.68f, 0.58f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }



        private void DestroyFrontAirflowSprites()
        {
            DestroySpriteArray(frontImpactPlumeSprites);
            DestroySpriteArray(frontImpactShockSprites);
        }

        private static void DestroySpriteArray(Sprite[] sprites)
        {
            if (sprites == null) return;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                if (Application.isPlaying) Destroy(sprites[i]);
                else DestroyImmediate(sprites[i]);
            }
        }
}
}
