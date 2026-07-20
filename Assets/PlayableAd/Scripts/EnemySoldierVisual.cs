using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class EnemySoldierVisual : MonoBehaviour
    {
        private static readonly int DeathTrigger = Animator.StringToHash("Death");

        [SerializeField, InspectorName("Animator（动画控制器）")] private Animator animator;
        private ObstacleController obstacle;
        private bool subscribed;

        public void Initialize(ObstacleController source)
        {
            obstacle = source;
            ConfigureAnimator();
            Subscribe();
        }

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
            Subscribe();
        }

        private void ConfigureAnimator()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null) return;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private void Subscribe()
        {
            if (subscribed || obstacle == null) return;
            obstacle.Resolved += OnObstacleResolved;
            subscribed = true;
        }

        private void OnObstacleResolved(ObstacleResolvedEvent resolved)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.ResetTrigger(DeathTrigger);
            animator.SetTrigger(DeathTrigger);
            animator.Update(0f);
            animator.enabled = false;
        }

        private void OnDisable()
        {
            if (!subscribed || obstacle == null) return;
            obstacle.Resolved -= OnObstacleResolved;
            subscribed = false;
        }
    }
}
