using System;
using UnityEngine;

namespace PlayableAd
{
    public enum EnemyVisibilityState
    {
        Pooled,
        Preloaded,
        DistantVisible,
        Active,
        KnockedBack,
        Recycled
    }

    [DisallowMultipleComponent]
    public sealed class EnemyVisibilityController : MonoBehaviour
    {
        private Renderer[] visualRenderers = Array.Empty<Renderer>();
        private Collider[] gameplayColliders = Array.Empty<Collider>();
        private ObstacleOutline outline;
        private EnemyVisibilityState state;
        private float previewStrength = -1f;

        public EnemyVisibilityState State => state;

        public void Initialize(Renderer[] renderers, Collider[] colliders, ObstacleOutline obstacleOutline)
        {
            visualRenderers = renderers ?? Array.Empty<Renderer>();
            gameplayColliders = colliders ?? Array.Empty<Collider>();
            outline = obstacleOutline;
            state = EnemyVisibilityState.Pooled;
            SetRenderers(false);
            SetGameplayColliders(false);
            outline?.SetPreviewPresentation(false, 0f);
            gameObject.SetActive(false);
        }

        public void SetState(EnemyVisibilityState nextState)
        {
            if (state == EnemyVisibilityState.KnockedBack || state == EnemyVisibilityState.Recycled)
                return;
            if (state == nextState) return;

            state = nextState;
            switch (state)
            {
                case EnemyVisibilityState.Pooled:
                    SetRenderers(false);
                    SetGameplayColliders(false);
                    outline?.SetPreviewPresentation(false, 0f);
                    gameObject.SetActive(false);
                    break;
                case EnemyVisibilityState.Preloaded:
                    if (!gameObject.activeSelf) gameObject.SetActive(true);
                    SetRenderers(false);
                    SetGameplayColliders(false);
                    outline?.SetPreviewPresentation(false, 0f);
                    break;
                case EnemyVisibilityState.DistantVisible:
                    if (!gameObject.activeSelf) gameObject.SetActive(true);
                    SetRenderers(true);
                    SetGameplayColliders(false);
                    outline?.SetPreviewPresentation(true, Mathf.Max(0.2f, previewStrength));
                    break;
                case EnemyVisibilityState.Active:
                    if (!gameObject.activeSelf) gameObject.SetActive(true);
                    SetRenderers(true);
                    SetGameplayColliders(true);
                    outline?.SetPreviewPresentation(true, 1f);
                    break;
            }
        }

        public void SetDistantPreviewStrength(float strength)
        {
            if (state != EnemyVisibilityState.DistantVisible) return;
            strength = Mathf.Clamp01(strength);
            if (Mathf.Abs(strength - previewStrength) < 0.04f) return;
            previewStrength = strength;
            outline?.SetPreviewPresentation(true, strength);
        }

        public void MarkKnockedBack()
        {
            state = EnemyVisibilityState.KnockedBack;
            previewStrength = 0f;
            outline?.SetPreviewPresentation(false, 0f);
            SetGameplayColliders(false);
            SetRenderers(true);
        }

        public void Recycle()
        {
            state = EnemyVisibilityState.Recycled;
            previewStrength = 0f;
            outline?.SetPreviewPresentation(false, 0f);
            SetGameplayColliders(false);
            gameObject.SetActive(false);
        }

        private void SetRenderers(bool visible)
        {
            for (int i = 0; i < visualRenderers.Length; i++)
                if (visualRenderers[i] != null) visualRenderers[i].enabled = visible;
        }

        private void SetGameplayColliders(bool active)
        {
            for (int i = 0; i < gameplayColliders.Length; i++)
                if (gameplayColliders[i] != null) gameplayColliders[i].enabled = active;
        }
    }
}
