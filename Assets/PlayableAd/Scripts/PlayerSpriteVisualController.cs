using UnityEngine;

namespace PlayableAd
{
    public enum FallShadowMode
    {
        Keep,
        Expand,
        Fade
    }

    [DisallowMultipleComponent]
    public sealed class PlayerSpriteVisualController : MonoBehaviour
    {
        private static readonly int HorizontalInput = Animator.StringToHash("HorizontalInput");
        private static readonly int IsShieldCharging = Animator.StringToHash("IsShieldCharging");
        private static readonly int IsFallen = Animator.StringToHash("IsFallen");
        private static readonly int IdleState = Animator.StringToHash("111");
        private static readonly int ShieldChargeState = Animator.StringToHash("333");
        private static readonly int FallenState = Animator.StringToHash("444");
        private static Material persistentParticleMaterial;

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer characterRenderer;
        [SerializeField] private bool enableAnimationDebug;
        [Header("Ground Shadow")]
        [SerializeField] private Transform groundShadow;
        [SerializeField] private SpriteRenderer groundShadowRenderer;
        [SerializeField] private Vector3 shadowLocalPosition = new Vector3(0f, -1.02f, 0.02f);
        [SerializeField] private Vector3 shadowLocalScale = new Vector3(0.62f, 0.34f, 1f);
        [SerializeField] private Color shadowColor = new Color(0.07f, 0.1f, 0.15f, 1f);
        [SerializeField, Range(0f, 1f)] private float baseAlpha = 0.28f;
        [SerializeField, Range(0f, 0.25f)] private float speedScaleMultiplier = 0.08f;
        [SerializeField, Range(1f, 1.4f)] private float chargeScaleMultiplier = 1.12f;
        [SerializeField] private FallShadowMode fallShadowMode = FallShadowMode.Expand;
        [Header("Persistent Character Highlight")]
        [SerializeField] private Color characterTint = new Color(1.14f, 1.16f, 1.28f, 1f);
        [SerializeField] private Color haloColor = new Color(0.16f, 0.72f, 1f, 0.26f);
        [SerializeField, Range(2f, 20f)] private float orbitParticleRate = 12f;

        private float shieldChargeUntil;
        private bool shieldHeld;
        private float speedNormalized;
        private bool movementActive;
        private bool recoveringFromFall;
        private SpriteRenderer haloRenderer;
        private Transform haloTransform;
        private ParticleSystem persistentOrbitParticles;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private int lastDebugStateHash;
#endif

        public void Configure(Animator targetAnimator, SpriteRenderer spriteRenderer,
            Transform shadow, SpriteRenderer shadowRenderer)
        {
            animator = targetAnimator;
            characterRenderer = spriteRenderer;
            groundShadow = shadow;
            groundShadowRenderer = shadowRenderer;
            EnsurePersistentHighlight();
            ApplyShadowImmediate();
        }

        public void ConfigureGroundShadowLighting(Vector3 lightEuler, float shadowStrength)
        {
            Vector3 lightDirection = Quaternion.Euler(lightEuler) * Vector3.forward;
            Vector3 groundDirection = Vector3.ProjectOnPlane(lightDirection, Vector3.up).normalized;
            if (groundDirection.sqrMagnitude > 0.001f)
            {
                shadowLocalPosition.x = groundDirection.x * 0.18f;
                shadowLocalPosition.z = 0.02f + groundDirection.z * 0.06f;
            }
            baseAlpha = Mathf.Lerp(0.18f, 0.38f, Mathf.Clamp01(shadowStrength));
            ApplyShadowImmediate();
        }

        public void SetHorizontalInput(float value)
        {
            if (animator != null)
                animator.SetFloat(HorizontalInput, Mathf.Clamp(value, -1f, 1f));
        }

        public void PlayShieldCharge(float duration)
        {
            shieldChargeUntil = Mathf.Max(shieldChargeUntil, Time.unscaledTime + Mathf.Max(0.05f, duration));
            if (animator != null)
                animator.SetBool(IsShieldCharging, true);
        }

        public void SetShieldHeld(bool held)
        {
            shieldHeld = held;
            if (!held) shieldChargeUntil = 0f;
            if (animator != null)
                animator.SetBool(IsShieldCharging, held);
        }

        public void SetFallen(bool fallen)
        {
            recoveringFromFall = false;
            if (animator != null)
                animator.SetBool(IsFallen, fallen);
        }

        public void BeginFallenRecovery()
        {
            recoveringFromFall = true;
            if (animator == null) return;
            animator.SetBool(IsFallen, true);
            animator.speed = 0f;
            animator.Play(FallenState, 0, 1f);
            animator.Update(0f);
        }

        public void SetFallenRecoveryProgress(float normalizedProgress)
        {
            if (animator == null) return;
            animator.Play(FallenState, 0, 1f - Mathf.Clamp01(normalizedProgress));
            animator.Update(0f);
        }

        public void CompleteFallenRecovery()
        {
            recoveringFromFall = false;
            if (animator == null) return;
            animator.speed = 1f;
            animator.SetBool(IsFallen, false);
            animator.Update(0f);
        }

        public void CompleteFallenRecoveryToShieldCharge()
        {
            recoveringFromFall = false;
            shieldHeld = true;
            shieldChargeUntil = 0f;
            if (animator == null) return;
            animator.speed = 1f;
            animator.SetBool(IsFallen, false);
            animator.SetBool(IsShieldCharging, true);
            animator.Play(ShieldChargeState, 0, 0f);
            animator.Update(0f);
        }

        public void PlayVictoryIdle()
        {
            recoveringFromFall = false;
            shieldHeld = false;
            shieldChargeUntil = 0f;
            speedNormalized = 0f;
            movementActive = false;
            if (animator == null) return;
            animator.speed = 1f;
            animator.SetFloat(HorizontalInput, 0f);
            animator.SetBool(IsShieldCharging, false);
            animator.SetBool(IsFallen, false);
            animator.Play(IdleState, 0, 0f);
            animator.Update(0f);
        }

        public void SetMovement(float normalizedSpeed, bool active)
        {
            speedNormalized = Mathf.Clamp01(normalizedSpeed);
            movementActive = active;
        }

        public void ResetVisualState()
        {
            shieldChargeUntil = 0f;
            shieldHeld = false;
            recoveringFromFall = false;
            if (animator == null) return;
            animator.SetFloat(HorizontalInput, 0f);
            animator.SetBool(IsShieldCharging, false);
            animator.SetBool(IsFallen, false);
            animator.Rebind();
            animator.Update(0f);
            speedNormalized = 0f;
            movementActive = false;
            ApplyShadowImmediate();
        }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            if (characterRenderer == null && animator != null)
                characterRenderer = animator.GetComponent<SpriteRenderer>();
            EnsurePersistentHighlight();
            ResetVisualState();
        }

        private void Update()
        {
            if (animator != null)
            {
                bool shouldCharge = shieldHeld || Time.unscaledTime < shieldChargeUntil;
                if (animator.GetBool(IsShieldCharging) != shouldCharge)
                    animator.SetBool(IsShieldCharging, shouldCharge);
            }

            if (animator != null)
            {
                if (recoveringFromFall)
                {
                    animator.speed = 0f;
                }
                else
                {
                    bool actionState = animator.GetBool(IsShieldCharging) || animator.GetBool(IsFallen);
                    float baseSpeed = actionState
                        ? 1f
                        : movementActive ? Mathf.Lerp(0.8f, 1.4f, speedNormalized) : 0f;
                    float worldScale = BulletTimeManager.Instance != null
                        ? BulletTimeManager.Instance.WorldTimeScale
                        : 1f;
                    animator.speed = baseSpeed * worldScale;
                }
            }

            UpdatePersistentHighlight();
            UpdateShadow(false);
            LogStateChange();
        }

        private void LateUpdate()
        {
            if (groundShadow == null) return;
            Vector3 scale = transform.lossyScale;
            groundShadow.position = transform.position + new Vector3(
                shadowLocalPosition.x * scale.x,
                shadowLocalPosition.y * scale.y,
                shadowLocalPosition.z);
            groundShadow.rotation = Quaternion.identity;
        }

        private void EnsurePersistentHighlight()
        {
            if (characterRenderer == null) return;
            characterRenderer.color = characterTint;

            if (haloRenderer == null)
            {
                GameObject halo = new GameObject("PersistentCharacterHalo");
                haloTransform = halo.transform;
                haloTransform.SetParent(characterRenderer.transform, false);
                haloTransform.localPosition = new Vector3(0f, 0f, 0.015f);
                haloTransform.localScale = Vector3.one * 1.09f;
                haloRenderer = halo.AddComponent<SpriteRenderer>();
                haloRenderer.sharedMaterial = characterRenderer.sharedMaterial;
                haloRenderer.sortingLayerID = characterRenderer.sortingLayerID;
                haloRenderer.sortingOrder = characterRenderer.sortingOrder - 1;
                haloRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                haloRenderer.receiveShadows = false;
            }

            if (persistentOrbitParticles == null)
                BuildPersistentOrbitParticles();
            UpdatePersistentHighlight();
        }

        private void UpdatePersistentHighlight()
        {
            if (characterRenderer == null) return;
            characterRenderer.color = characterTint;
            if (haloRenderer == null) return;

            haloRenderer.sprite = characterRenderer.sprite;
            haloRenderer.flipX = characterRenderer.flipX;
            haloRenderer.flipY = characterRenderer.flipY;
            haloRenderer.enabled = characterRenderer.enabled && characterRenderer.sprite != null;
            haloRenderer.sortingLayerID = characterRenderer.sortingLayerID;
            haloRenderer.sortingOrder = characterRenderer.sortingOrder - 1;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.4f);
            Color color = haloColor;
            color.a *= Mathf.Lerp(0.78f, 1f, pulse);
            haloRenderer.color = color;
            haloTransform.localScale = Vector3.one * Mathf.Lerp(1.08f, 1.115f, pulse);
        }

        private void BuildPersistentOrbitParticles()
        {
            GameObject root = new GameObject("PersistentOrbitGlints");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, 0.04f, -0.02f);
            persistentOrbitParticles = root.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = persistentOrbitParticles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.simulationSpeed = 0.82f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.11f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.18f, 0.78f, 1f, 0.88f),
                new Color(1f, 0.82f, 0.28f, 0.82f));
            main.maxParticles = 32;

            ParticleSystem.EmissionModule emission = persistentOrbitParticles.emission;
            emission.rateOverTime = orbitParticleRate;

            ParticleSystem.ShapeModule shape = persistentOrbitParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.74f;
            shape.radiusThickness = 0.38f;
            shape.scale = new Vector3(0.78f, 1.18f, 1f);

            ParticleSystem.VelocityOverLifetimeModule velocity = persistentOrbitParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0.65f, 1.15f);
            velocity.radial = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = persistentOrbitParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.2f, 0.75f, 1f), 0f),
                    new GradientColorKey(new Color(0.5f, 0.94f, 1f), 0.48f),
                    new GradientColorKey(new Color(1f, 0.78f, 0.25f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.2f),
                    new GradientAlphaKey(0.68f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = colorGradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = persistentOrbitParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.35f), new Keyframe(0.28f, 1f), new Keyframe(1f, 0.15f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystemRenderer particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = GetPersistentParticleMaterial();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingLayerID = characterRenderer.sortingLayerID;
            particleRenderer.sortingOrder = characterRenderer.sortingOrder + 2;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            persistentOrbitParticles.Play();
        }

        private static Material GetPersistentParticleMaterial()
        {
            if (persistentParticleMaterial != null) return persistentParticleMaterial;
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            persistentParticleMaterial = new Material(shader)
            {
                name = "Runtime Player Orbit Glints",
                mainTexture = CreateSoftParticleTexture(),
                renderQueue = 3000,
                hideFlags = HideFlags.DontSave
            };
            return persistentParticleMaterial;
        }

        private static Texture2D CreateSoftParticleTexture()
        {
            const int resolution = 32;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Player Soft Glint",
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
                    float radius = Mathf.Sqrt(nx * nx + ny * ny);
                    float core = Mathf.Pow(Mathf.Clamp01(1f - radius), 1.8f);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, core);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void ApplyShadowImmediate()
        {
            UpdateShadow(true);
        }

        private void UpdateShadow(bool immediate)
        {
            if (groundShadow == null || groundShadowRenderer == null) return;
            bool charging = animator != null && animator.GetBool(IsShieldCharging);
            bool fallen = animator != null && animator.GetBool(IsFallen);

            Vector3 targetScale = shadowLocalScale;
            targetScale.x *= 1f + speedNormalized * speedScaleMultiplier;
            float targetAlpha = baseAlpha;
            if (charging)
            {
                targetScale.x *= chargeScaleMultiplier;
                targetScale.y *= 0.92f;
                targetAlpha = Mathf.Min(1f, baseAlpha + 0.04f);
            }
            if (fallen)
            {
                if (fallShadowMode == FallShadowMode.Expand)
                {
                    targetScale.x *= 1.62f;
                    targetScale.y *= 0.78f;
                    targetAlpha *= 0.82f;
                }
                else if (fallShadowMode == FallShadowMode.Fade)
                {
                    targetAlpha *= 0.58f;
                }
            }

            float response = immediate ? 1f : 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
            groundShadow.localScale = Vector3.Lerp(groundShadow.localScale, targetScale, response);
            Color color = shadowColor;
            color.a = targetAlpha;
            groundShadowRenderer.color = Color.Lerp(groundShadowRenderer.color, color, response);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogStateChange()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!enableAnimationDebug || animator == null || !animator.isActiveAndEnabled) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == lastDebugStateHash) return;
            lastDebugStateHash = state.fullPathHash;
            string spriteName = characterRenderer != null && characterRenderer.sprite != null
                ? characterRenderer.sprite.name
                : "<none>";
            Debug.Log("PlayerSpriteAnimator state=" + state.fullPathHash
                + " normalizedTime=" + state.normalizedTime.ToString("0.000")
                + " horizontal=" + animator.GetFloat(HorizontalInput).ToString("0.00")
                + " shield=" + animator.GetBool(IsShieldCharging)
                + " fallen=" + animator.GetBool(IsFallen)
                + " sprite=" + spriteName, this);
#endif
        }
    }
}
