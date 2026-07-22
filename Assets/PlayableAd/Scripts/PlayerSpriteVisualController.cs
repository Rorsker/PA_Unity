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

        private float shieldChargeUntil;
        private bool shieldHeld;
        private float speedNormalized;
        private bool movementActive;
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
            if (animator != null)
                animator.SetBool(IsFallen, fallen);
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
                bool actionState = animator.GetBool(IsShieldCharging) || animator.GetBool(IsFallen);
                float baseSpeed = actionState
                    ? 1f
                    : movementActive ? Mathf.Lerp(0.8f, 1.4f, speedNormalized) : 0f;
                float worldScale = BulletTimeManager.Instance != null
                    ? BulletTimeManager.Instance.WorldTimeScale
                    : 1f;
                animator.speed = baseSpeed * worldScale;
            }

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
