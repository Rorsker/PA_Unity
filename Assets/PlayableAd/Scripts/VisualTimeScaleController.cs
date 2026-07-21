using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayableAd
{
    public sealed class VisualTimeScaleController : MonoBehaviour
    {
        private struct SlowMotionRequest
        {
            public float scale;
            public float expiresAt;
        }

        private readonly List<SlowMotionRequest> requests = new List<SlowMotionRequest>(4);
        private float restoreTimeScale = 1f;
        private float restoreFixedDeltaTime = 0.02f;
        private bool ownsTimeScale;

        public void RequestSlowMotion(float scale, float duration)
        {
            if (duration <= 0f) return;
            if (BulletTimeManager.Instance != null && BulletTimeManager.Instance.IsBulletTime()) return;
            if (!ownsTimeScale)
            {
                restoreTimeScale = Time.timeScale;
                restoreFixedDeltaTime = Time.fixedDeltaTime;
                ownsTimeScale = true;
            }

            requests.Add(new SlowMotionRequest
            {
                scale = Mathf.Clamp(scale, 0.05f, 1f),
                expiresAt = Time.unscaledTime + Mathf.Min(duration, 0.2f)
            });
            ApplyStrongestRequest();
        }

        private void Update()
        {
            for (int i = requests.Count - 1; i >= 0; i--)
                if (Time.unscaledTime >= requests[i].expiresAt) requests.RemoveAt(i);

            if (requests.Count == 0)
                Restore();
            else
                ApplyStrongestRequest();
        }

        public void Restore()
        {
            requests.Clear();
            if (!ownsTimeScale) return;
            Time.timeScale = restoreTimeScale;
            Time.fixedDeltaTime = restoreFixedDeltaTime;
            ownsTimeScale = false;
        }

        private void ApplyStrongestRequest()
        {
            float scale = 1f;
            for (int i = 0; i < requests.Count; i++) scale = Mathf.Min(scale, requests[i].scale);
            Time.timeScale = restoreTimeScale * scale;
            Time.fixedDeltaTime = restoreFixedDeltaTime * scale;
        }

        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();
    }
}
