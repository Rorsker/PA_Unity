using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class EnemySoldierVisual : MonoBehaviour
    {
        [SerializeField, InspectorName("Animator（动画控制器）")] private Animator animator;

        private void OnEnable()
        {
            ConfigureAnimator();
            if (animator != null)
            {
                animator.enabled = true;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private void ConfigureAnimator()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null) return;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private void Update()
        {
            if (animator == null || !animator.enabled) return;
            animator.speed = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.WorldTimeScale
                : 1f;
        }
    }
}
