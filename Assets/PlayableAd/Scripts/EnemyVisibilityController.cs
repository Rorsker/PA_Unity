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
        private Animator[] animators = Array.Empty<Animator>();
        private EnemySoldierVisual[] animationDrivers = Array.Empty<EnemySoldierVisual>();
        private PooledSoldierVisual pooledVisual;
        private EnemyVisibilityState state;

        public EnemyVisibilityState State => state;

        public void Initialize(Renderer[] renderers, Collider[] colliders)
        {
            visualRenderers = renderers ?? Array.Empty<Renderer>();
            gameplayColliders = colliders ?? Array.Empty<Collider>();
            pooledVisual = GetComponent<PooledSoldierVisual>();
            RefreshAnimationComponents();
            state = EnemyVisibilityState.Pooled;
            SetAnimationActive(false);
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
                    SetAnimationActive(false);
                    SetRenderers(false);
                    SetGameplayColliders(false);
                    pooledVisual?.ReleaseVisual();
                    gameObject.SetActive(false);
                    break;
                case EnemyVisibilityState.Preloaded:
                    SetAnimationActive(false);
                    SetRenderers(false);
                    SetGameplayColliders(false);
                    if (gameObject.activeSelf) gameObject.SetActive(false);
                    break;
                case EnemyVisibilityState.DistantVisible:
                    if (!gameObject.activeSelf) gameObject.SetActive(true);
                    EnsureVisual();
                    SetAnimationActive(false);
                    SetRenderers(true);
                    SetGameplayColliders(false);
                    break;
                case EnemyVisibilityState.Active:
                    if (!gameObject.activeSelf) gameObject.SetActive(true);
                    EnsureVisual();
                    SetRenderers(true);
                    SetGameplayColliders(true);
                    SetAnimationActive(true);
                    break;
            }
        }

        public void MarkKnockedBack()
        {
            state = EnemyVisibilityState.KnockedBack;
            SetAnimationActive(false);
            SetGameplayColliders(false);
            SetRenderers(true);
        }

        public void SetActiveAnimationBudget(bool active)
        {
            if (state == EnemyVisibilityState.Active)
                SetAnimationActive(active);
        }

        public void Recycle()
        {
            state = EnemyVisibilityState.Recycled;
            SetAnimationActive(false);
            SetGameplayColliders(false);
            SetRenderers(false);
            pooledVisual?.ReleaseVisual();
            gameObject.SetActive(false);
        }

        private void EnsureVisual()
        {
            if (pooledVisual == null || !pooledVisual.EnsureVisual()) return;
            visualRenderers = pooledVisual.Renderers;
            animators = pooledVisual.Animators;
            animationDrivers = pooledVisual.AnimationDrivers;
        }

        private void RefreshAnimationComponents()
        {
            animators = GetComponentsInChildren<Animator>(true);
            animationDrivers = GetComponentsInChildren<EnemySoldierVisual>(true);
        }

        private void SetAnimationActive(bool active)
        {
            for (int i = 0; i < animationDrivers.Length; i++)
                if (animationDrivers[i] != null) animationDrivers[i].enabled = active;
            for (int i = 0; i < animators.Length; i++)
                if (animators[i] != null) animators[i].enabled = active;
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
