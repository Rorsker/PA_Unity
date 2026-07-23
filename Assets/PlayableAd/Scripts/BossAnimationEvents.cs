using UnityEngine;

namespace PlayableAd
{
    public sealed class BossAnimationEvents : MonoBehaviour
    {
        private AudioFeedbackController audioFeedback;

        public void Initialize(AudioFeedbackController controller)
        {
            audioFeedback = controller;
        }

        public void OnBossGroundSlam()
        {
            audioFeedback?.PlayBossGroundSlam();
        }
    }
}
