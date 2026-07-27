using System;
using UnityEngine;

namespace PlayableAd
{
    public sealed class BossAnimationEvents : MonoBehaviour
    {
        private AudioFeedbackController audioFeedback;
        private Action attackImpactFeedback;

        public void Initialize(AudioFeedbackController controller, Action onAttackImpact = null)
        {
            audioFeedback = controller;
            attackImpactFeedback = onAttackImpact;
        }

        public void OnBossGroundSlam()
        {
            audioFeedback?.PlayBossGroundSlam();
            attackImpactFeedback?.Invoke();
        }
    }
}
