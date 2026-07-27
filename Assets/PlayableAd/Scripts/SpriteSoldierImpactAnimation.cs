using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class SpriteSoldierImpactAnimation : MonoBehaviour
    {
        private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
        [SerializeField, Min(0.05f), InspectorName("Impact Duration（受击动画时长）")]
        private float impactDuration = 0.75f;

        private Animator animator;

        public Animator Animator
        {
            get
            {
                CacheAnimator();
                return animator;
            }
        }

        public float ImpactDuration => Mathf.Max(0.05f, impactDuration);

        private void Awake()
        {
            CacheAnimator();
        }

        public bool PlayImpactAnimation()
        {
            CacheAnimator();
            if (animator == null) return false;

            animator.enabled = true;
            animator.speed = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.WorldTimeScale
                : 1f;
            animator.ResetTrigger(HitTriggerHash);
            animator.SetTrigger(HitTriggerHash);
            return true;
        }

        private void CacheAnimator()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }
    }
}
