using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class WorldTimeScaledAnimator : MonoBehaviour
    {
        [SerializeField, InspectorName("Animator（动画控制器）")]
        private Animator targetAnimator;

        private void OnEnable()
        {
            if (targetAnimator == null) targetAnimator = GetComponent<Animator>();
            if (targetAnimator != null) targetAnimator.speed = 1f;
        }

        private void Update()
        {
            if (targetAnimator == null) return;
            targetAnimator.speed = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.WorldTimeScale
                : 1f;
        }
    }
}
