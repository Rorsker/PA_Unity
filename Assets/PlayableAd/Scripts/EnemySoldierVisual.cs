using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class EnemySoldierVisual : MonoBehaviour
    {
        [SerializeField, InspectorName("Animator（动画控制器）")] private Animator animator;

        private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.Idle");
        private const float DistantIdlePoseTime = 0.15f;

        public Animator Animator => animator;

        private void OnEnable()
        {
            ConfigureAnimator();
        }

        private void ConfigureAnimator()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null) return;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        public void FreezeIdlePose()
        {
            ConfigureAnimator();
            if (animator == null) return;

            // Sample a deliberate idle frame before disabling the Animator. This
            // keeps distant pooled soldiers out of their imported bind pose while
            // avoiding any ongoing Animator cost.
            AnimatorCullingMode previousCullingMode = animator.cullingMode;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;
            animator.Rebind();
            if (animator.HasState(0, IdleStateHash))
                animator.Play(IdleStateHash, 0, DistantIdlePoseTime);
            animator.Update(0f);
            animator.cullingMode = previousCullingMode;
            animator.enabled = false;
            enabled = false;
        }

        public void ResumeAnimation()
        {
            ConfigureAnimator();
            if (animator == null) return;

            animator.enabled = true;
            animator.speed = GetWorldAnimationScale();
            enabled = true;
        }

        public void PauseCurrentPose()
        {
            if (animator != null) animator.enabled = false;
            enabled = false;
        }

        private void Update()
        {
            if (animator == null || !animator.enabled) return;
            animator.speed = GetWorldAnimationScale();
        }

        private static float GetWorldAnimationScale()
        {
            return BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.WorldTimeScale
                : 1f;
        }
    }
}
