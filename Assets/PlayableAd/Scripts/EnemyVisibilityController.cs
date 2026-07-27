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
        private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.Idle");

        private Renderer[] visualRenderers = Array.Empty<Renderer>();
        private Collider[] gameplayColliders = Array.Empty<Collider>();
        private Animator[] animators = Array.Empty<Animator>();
        private EnemySoldierVisual[] animationDrivers = Array.Empty<EnemySoldierVisual>();
        private PooledSoldierVisual pooledVisual;
        private EnemyVisibilityState state;
        private bool animationIsActive;
        private bool idlePoseIsFrozen;

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
                    idlePoseIsFrozen = false;
                    SetRenderers(false);
                    SetGameplayColliders(false);
                    pooledVisual?.ReleaseVisual();
                    ClearReleasedVisualReferences();
                    gameObject.SetActive(false);
                    break;
                case EnemyVisibilityState.Preloaded:
                    SetAnimationActive(false);
                    idlePoseIsFrozen = false;
                    SetRenderers(false);
                    SetGameplayColliders(false);
                    if (gameObject.activeSelf) gameObject.SetActive(false);
                    break;
                case EnemyVisibilityState.DistantVisible:
                    if (!gameObject.activeSelf) gameObject.SetActive(true);
                    EnsureVisual();
                    FreezeAnimationInIdlePose();
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
            {
                if (active) SetAnimationActive(true);
                else FreezeAnimationInIdlePose();
            }
        }

        public void Recycle()
        {
            state = EnemyVisibilityState.Recycled;
            SetAnimationActive(false);
            SetGameplayColliders(false);
            SetRenderers(false);
            pooledVisual?.ReleaseVisual();
            ClearReleasedVisualReferences();
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
            if (active)
            {
                if (animationIsActive) return;
                for (int i = 0; i < animationDrivers.Length; i++)
                    if (animationDrivers[i] != null) animationDrivers[i].ResumeAnimation();
                for (int i = 0; i < animators.Length; i++)
                    if (animators[i] != null) animators[i].enabled = true;
                animationIsActive = true;
                idlePoseIsFrozen = false;
                return;
            }

            for (int i = 0; i < animationDrivers.Length; i++)
                if (animationDrivers[i] != null) animationDrivers[i].PauseCurrentPose();
            for (int i = 0; i < animators.Length; i++)
                if (animators[i] != null) animators[i].enabled = false;
            animationIsActive = false;
            idlePoseIsFrozen = false;
        }

        private void FreezeAnimationInIdlePose()
        {
            if (idlePoseIsFrozen) return;

            for (int i = 0; i < animationDrivers.Length; i++)
                if (animationDrivers[i] != null) animationDrivers[i].FreezeIdlePose();

            // Keep the fallback valid for soldier prefabs that only contain an Animator.
            for (int i = 0; i < animators.Length; i++)
            {
                Animator target = animators[i];
                if (target == null || HasAnimationDriver(target)) continue;
                AnimatorCullingMode previousCullingMode = target.cullingMode;
                target.enabled = true;
                target.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                target.Rebind();
                if (target.HasState(0, IdleStateHash))
                    target.Play(IdleStateHash, 0, 0.15f);
                target.Update(0f);
                target.cullingMode = previousCullingMode;
                target.enabled = false;
            }

            animationIsActive = false;
            idlePoseIsFrozen = true;
        }

        private bool HasAnimationDriver(Animator target)
        {
            for (int i = 0; i < animationDrivers.Length; i++)
                if (animationDrivers[i] != null && animationDrivers[i].Animator == target) return true;
            return false;
        }

        private void ClearReleasedVisualReferences()
        {
            if (pooledVisual == null) return;
            visualRenderers = Array.Empty<Renderer>();
            animators = Array.Empty<Animator>();
            animationDrivers = Array.Empty<EnemySoldierVisual>();
            animationIsActive = false;
            idlePoseIsFrozen = false;
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
