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
        private EnemyVisibilityState state;

        public EnemyVisibilityState State => state;

        public void Initialize(Renderer[] renderers, Collider[] colliders)
        {
            visualRenderers = renderers ?? Array.Empty<Renderer>();
            gameplayColliders = colliders ?? Array.Empty<Collider>();
            state = EnemyVisibilityState.Pooled;
            SetRenderers(false);
            SetGameplayColliders(false);
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
                    gameObject.SetActive(false);
                    break;
                case EnemyVisibilityState.Preloaded:
                    if (!gameObject.activeSelf) gameObject.SetActive(true);
                    SetRenderers(false);
                    SetGameplayColliders(false);
                    break;
                case EnemyVisibilityState.DistantVisible:
                    if (!gameObject.activeSelf) gameObject.SetActive(true);
                    SetRenderers(true);
                    SetGameplayColliders(false);
                    break;
                case EnemyVisibilityState.Active:
                    if (!gameObject.activeSelf) gameObject.SetActive(true);
                    SetRenderers(true);
                    SetGameplayColliders(true);
                    break;
            }
        }

        public void MarkKnockedBack()
        {
            state = EnemyVisibilityState.KnockedBack;
            SetGameplayColliders(false);
            SetRenderers(true);
        }

        public void Recycle()
        {
            state = EnemyVisibilityState.Recycled;
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
