using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayableAd
{
    // Soldier prefabs contain a full animated hierarchy. Keep only nearby model
    // instances alive and reuse them while lightweight collision roots remain in place.
    [DisallowMultipleComponent]
    public sealed class SoldierVisualPool : MonoBehaviour
    {
        private readonly Dictionary<GameObject, Stack<PooledSoldierVisualInstance>> available =
            new Dictionary<GameObject, Stack<PooledSoldierVisualInstance>>();
        private Transform poolRoot;

        public void Initialize(Transform parent)
        {
            if (poolRoot != null) return;
            GameObject root = new GameObject("InactiveSoldierVisuals");
            poolRoot = root.transform;
            poolRoot.SetParent(parent != null ? parent : transform, false);
            root.SetActive(false);
        }

        public PooledSoldierVisualInstance Acquire(GameObject prefab, Transform parent)
        {
            if (prefab == null || parent == null) return null;
            if (poolRoot == null) Initialize(transform);

            if (!available.TryGetValue(prefab, out Stack<PooledSoldierVisualInstance> instances))
            {
                instances = new Stack<PooledSoldierVisualInstance>();
                available.Add(prefab, instances);
            }

            PooledSoldierVisualInstance instance = null;
            while (instances.Count > 0 && instance == null)
                instance = instances.Pop();

            if (instance == null)
            {
                GameObject visual = Instantiate(prefab, poolRoot, false);
                visual.name = prefab.name;
                instance = visual.AddComponent<PooledSoldierVisualInstance>();
                instance.SetSourcePrefab(prefab);
            }

            instance.transform.SetParent(parent, false);
            instance.RestoreLocalPose();
            return instance;
        }

        public void Release(PooledSoldierVisualInstance instance)
        {
            if (instance == null) return;
            if (poolRoot == null)
            {
                Destroy(instance.gameObject);
                return;
            }
            GameObject prefab = instance.SourcePrefab;
            if (prefab == null)
            {
                Destroy(instance.gameObject);
                return;
            }

            if (!available.TryGetValue(prefab, out Stack<PooledSoldierVisualInstance> instances))
            {
                instances = new Stack<PooledSoldierVisualInstance>();
                available.Add(prefab, instances);
            }

            instance.transform.SetParent(poolRoot, false);
            instances.Push(instance);
        }
    }

    [DisallowMultipleComponent]
    public sealed class PooledSoldierVisualInstance : MonoBehaviour
    {
        public GameObject SourcePrefab { get; private set; }
        public Renderer[] Renderers { get; private set; } = Array.Empty<Renderer>();
        public Animator[] Animators { get; private set; } = Array.Empty<Animator>();
        public EnemySoldierVisual[] AnimationDrivers { get; private set; } = Array.Empty<EnemySoldierVisual>();
        public bool IsPrepared { get; private set; }

        private Vector3 preparedLocalPosition;
        private Quaternion preparedLocalRotation = Quaternion.identity;
        private Vector3 preparedLocalScale = Vector3.one;

        public void SetSourcePrefab(GameObject prefab)
        {
            SourcePrefab = prefab;
        }

        public void CapturePreparedState()
        {
            Renderers = GetComponentsInChildren<Renderer>(true);
            Animators = GetComponentsInChildren<Animator>(true);
            AnimationDrivers = GetComponentsInChildren<EnemySoldierVisual>(true);
            preparedLocalPosition = transform.localPosition;
            preparedLocalRotation = transform.localRotation;
            preparedLocalScale = transform.localScale;
            IsPrepared = true;
        }

        public void RestoreLocalPose()
        {
            if (!IsPrepared)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                return;
            }

            transform.localPosition = preparedLocalPosition;
            transform.localRotation = preparedLocalRotation;
            transform.localScale = preparedLocalScale;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PooledSoldierVisual : MonoBehaviour
    {
        private SoldierVisualPool pool;
        private GameObject prefab;
        private Transform visualRoot;
        private Vector3 dimensions;
        private float targetHeight;
        private BoxCollider targetCollider;
        private SoldierKnockbackEffect knockbackEffect;
        private PooledSoldierVisualInstance instance;

        public Renderer[] Renderers => instance != null ? instance.Renderers : Array.Empty<Renderer>();
        public Animator[] Animators => instance != null ? instance.Animators : Array.Empty<Animator>();
        public EnemySoldierVisual[] AnimationDrivers => instance != null
            ? instance.AnimationDrivers
            : Array.Empty<EnemySoldierVisual>();

        public void Initialize(SoldierVisualPool visualPool, GameObject visualPrefab,
            Transform targetVisualRoot, Vector3 targetDimensions, float authoredTargetHeight,
            BoxCollider gameplayCollider, SoldierKnockbackEffect soldierKnockback)
        {
            pool = visualPool;
            prefab = visualPrefab;
            visualRoot = targetVisualRoot;
            dimensions = targetDimensions;
            targetHeight = authoredTargetHeight;
            targetCollider = gameplayCollider;
            knockbackEffect = soldierKnockback;
        }

        public bool EnsureVisual()
        {
            if (instance != null) return true;
            if (pool == null || prefab == null || visualRoot == null) return false;

            instance = pool.Acquire(prefab, visualRoot);
            if (instance == null) return false;

            if (!instance.IsPrepared)
            {
                PlayableAdGame.SanitizeTargetVisual(instance.gameObject);
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    pool.Release(instance);
                    instance = null;
                    return false;
                }

                PlayableAdGame.FitTargetVisual(instance.gameObject, visualRoot, renderers, targetHeight);
                instance.CapturePreparedState();
            }

            PlayableAdGame.ConfigureTargetCollider(targetCollider, dimensions,
                instance.Renderers, targetHeight);
            knockbackEffect?.Initialize(instance.Renderers);
            return true;
        }

        public void ReleaseVisual()
        {
            if (instance == null) return;
            knockbackEffect?.Initialize(Array.Empty<Renderer>());
            pool.Release(instance);
            instance = null;
        }

    }
}
