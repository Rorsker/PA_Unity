using System;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class EnemyBreakPresentationSettings
    {
        [Header("Pool（对象池）")]
        [Range(24, 96), InspectorName("Max Active Fragments（最大活动碎片数）")] public int maxActiveFragments = 48;
        [Range(3, 6), InspectorName("Fragments Per Enemy（每个敌人碎片数）")] public int fragmentsPerEnemy = 4;
        [Range(0.5f, 2.5f), InspectorName("Fragment Lifetime（碎片持续时间）")] public float fragmentLifetime = 1.35f;

        [Header("Velocity（速度）")]
        [Range(2f, 12f), InspectorName("Min Fragment Force（最小碎片力度）")] public float minFragmentForce = 4.5f;
        [Range(4f, 18f), InspectorName("Max Fragment Force（最大碎片力度）")] public float maxFragmentForce = 10.5f;
        [Range(1f, 8f), InspectorName("Upward Force（向上力度）")] public float upwardForce = 4.2f;
        [Range(0.5f, 7f), InspectorName("Lateral Spread（横向散布）")] public float lateralSpread = 3.2f;
        [Range(1f, 12f), InspectorName("Min Angular Velocity（最小角速度）")] public float minAngularVelocity = 4f;
        [Range(4f, 24f), InspectorName("Max Angular Velocity（最大角速度）")] public float maxAngularVelocity = 14f;
        [Range(0.1f, 1f), InspectorName("Low Quality Fragment Multiplier（低画质碎片倍率）")] public float lowQualityFragmentMultiplier = 0.55f;
    }

    [DisallowMultipleComponent]
    public sealed class EnemyBreakEffectPool : MonoBehaviour
    {
        private sealed class Fragment
        {
            public GameObject root;
            public Transform transform;
            public Renderer renderer;
            public Rigidbody body;
            public float remaining;
            public uint launchSequence;
            public bool active;
        }

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private Fragment[] fragments = Array.Empty<Fragment>();
        private EnemyBreakPresentationSettings settings;
        private VisualPerformanceSettings performance;
        private Material sharedMaterial;
        private MaterialPropertyBlock propertyBlock;
        private int cursor;
        private uint launchSequence;

        public int Capacity => fragments.Length;
        public int ActiveFragmentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < fragments.Length; i++)
                    if (fragments[i].active) count++;
                return count;
            }
        }

        public void Initialize(EnemyBreakPresentationSettings presentationSettings,
            VisualPerformanceSettings performanceSettings)
        {
            settings = presentationSettings ?? new EnemyBreakPresentationSettings();
            performance = performanceSettings ?? new VisualPerformanceSettings();
            sharedMaterial = RuntimeStyle.CreateMaterial(Color.white, 0.45f, 0.28f);
            propertyBlock = new MaterialPropertyBlock();
            BuildPool();
        }

        public void PlayBreak(Vector3 position, Vector3 sourceDimensions, Color color,
            float normalizedActualSpeed, float preferredSide)
        {
            if (fragments.Length == 0 || settings == null) return;

            int requested = performance.lowQualityMode
                ? Mathf.Max(3, Mathf.RoundToInt(settings.fragmentsPerEnemy * settings.lowQualityFragmentMultiplier))
                : settings.fragmentsPerEnemy;
            int count = Mathf.Clamp(requested, 3, 6);
            float speedT = Mathf.Clamp01(normalizedActualSpeed);
            float forwardForce = Mathf.Lerp(settings.minFragmentForce, settings.maxFragmentForce, speedT);
            float angularForce = Mathf.Lerp(settings.minAngularVelocity, settings.maxAngularVelocity, speedT);
            float sideSign = Mathf.Abs(preferredSide) > 0.05f ? Mathf.Sign(preferredSide) : ((launchSequence & 1u) == 0u ? -1f : 1f);

            for (int i = 0; i < count; i++)
            {
                Fragment fragment = GetNextFragment();
                float horizontalSlot = count <= 1 ? 0f : i / (float)(count - 1) - 0.5f;
                float side = horizontalSlot * 2f + sideSign * 0.18f;
                Vector3 localOffset = new Vector3(
                    horizontalSlot * sourceDimensions.x * 0.42f,
                    ((i & 1) == 0 ? 0.2f : 0.68f) * sourceDimensions.y,
                    ((i % 3) - 1) * sourceDimensions.z * 0.18f);
                Vector3 scale = new Vector3(
                    sourceDimensions.x * (i == 0 ? 0.48f : 0.3f),
                    sourceDimensions.y * (i < 2 ? 0.42f : 0.28f),
                    sourceDimensions.z * (i == 1 ? 0.48f : 0.3f));

                fragment.root.SetActive(true);
                fragment.transform.position = position + localOffset;
                fragment.transform.rotation = Quaternion.Euler(0f, i * 37f, i * 23f);
                fragment.transform.localScale = scale;
                propertyBlock.SetColor(ColorId, Color.Lerp(color, i == 1 ? Color.white : new Color(0.18f, 0.2f, 0.24f), i == 1 ? 0.35f : 0.2f));
                fragment.renderer.SetPropertyBlock(propertyBlock);

                fragment.body.isKinematic = false;
                fragment.body.useGravity = true;
                fragment.body.velocity = new Vector3(
                    side * settings.lateralSpread * UnityEngine.Random.Range(0.72f, 1.08f),
                    settings.upwardForce * UnityEngine.Random.Range(0.8f, 1.18f),
                    forwardForce * UnityEngine.Random.Range(0.86f, 1.12f));
                fragment.body.angularVelocity = UnityEngine.Random.onUnitSphere * angularForce;
                fragment.body.WakeUp();
                fragment.remaining = settings.fragmentLifetime * UnityEngine.Random.Range(0.88f, 1.08f);
                fragment.launchSequence = ++launchSequence;
                fragment.active = true;
            }
        }

        private void Update()
        {
            // Match Rigidbody simulation and freeze the presentation while gameplay is paused.
            float dt = Time.deltaTime;
            for (int i = 0; i < fragments.Length; i++)
            {
                Fragment fragment = fragments[i];
                if (!fragment.active) continue;
                fragment.remaining -= dt;
                if (fragment.remaining <= 0f || fragment.transform.position.y < -2f)
                    Recycle(fragment);
            }
        }

        private void BuildPool()
        {
            int count = Mathf.Clamp(settings.maxActiveFragments, 24, 96);
            fragments = new Fragment[count];
            for (int i = 0; i < count; i++)
            {
                GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
                root.name = "PooledEnemyFragment_" + (i + 1);
                root.transform.SetParent(transform, false);
                Collider collider = root.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                Renderer renderer = root.GetComponent<Renderer>();
                renderer.sharedMaterial = sharedMaterial;
                Rigidbody body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.drag = 0.22f;
                body.angularDrag = 0.32f;
                body.maxAngularVelocity = settings.maxAngularVelocity;
                root.SetActive(false);
                fragments[i] = new Fragment { root = root, transform = root.transform, renderer = renderer, body = body };
            }
        }

        private Fragment GetNextFragment()
        {
            for (int i = 0; i < fragments.Length; i++)
            {
                int index = (cursor + i) % fragments.Length;
                if (!fragments[index].active)
                {
                    cursor = (index + 1) % fragments.Length;
                    return fragments[index];
                }
            }

            Fragment oldest = fragments[0];
            for (int i = 1; i < fragments.Length; i++)
                if (fragments[i].launchSequence < oldest.launchSequence) oldest = fragments[i];
            Recycle(oldest);
            return oldest;
        }

        private static void Recycle(Fragment fragment)
        {
            fragment.active = false;
            fragment.remaining = 0f;
            fragment.body.velocity = Vector3.zero;
            fragment.body.angularVelocity = Vector3.zero;
            fragment.body.useGravity = false;
            fragment.body.isKinematic = true;
            fragment.transform.localScale = Vector3.one;
            fragment.root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (sharedMaterial == null) return;
            if (Application.isPlaying) Destroy(sharedMaterial);
            else DestroyImmediate(sharedMaterial);
        }
    }
}
