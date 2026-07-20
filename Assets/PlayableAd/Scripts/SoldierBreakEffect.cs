using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class SoldierBreakEffect : MonoBehaviour
    {
        [SerializeField, Range(3, 8), InspectorName("Fragment Count（碎片数量）")] private int fragmentCount = 5;
        [SerializeField, InspectorName("Fragments（碎片刚体）")] private Rigidbody[] fragments;
        [SerializeField, InspectorName("Particles（粒子系统）")] private ParticleSystem[] particles;
        [SerializeField, Min(0f), InspectorName("Forward Force（前向力度）")] private float forwardForce = 7f;
        [SerializeField, Min(0f), InspectorName("Side Force（侧向力度）")] private float sideForce = 2.2f;
        [SerializeField, Min(0f), InspectorName("Upward Force（向上力度）")] private float upwardForce = 3.4f;
        [SerializeField, Min(0f), InspectorName("Torque（扭矩）")] private float torque = 9f;
        [SerializeField, Min(0f), InspectorName("Gravity Multiplier（重力倍率）")] private float gravityMultiplier = 1f;
        [SerializeField, Min(0.1f), InspectorName("Fragment Lifetime（碎片持续时间）")] private float fragmentLifetime = 1f;
        [SerializeField, Min(0.05f), InspectorName("Fade Duration（淡出时长）")] private float fadeDuration = 0.25f;
        [SerializeField, InspectorName("Fragment Scale Range（碎片缩放范围）")] private Vector2 fragmentScaleRange = new Vector2(0.88f, 1.08f);

        private Vector3[] localPositions;
        private Quaternion[] localRotations;
        private Vector3[] localScales;
        private float remaining;
        private bool playing;
        private uint sequence;

        public bool IsPlaying => playing;
        public uint Sequence => sequence;

        private void Awake()
        {
            int count = fragments != null ? fragments.Length : 0;
            localPositions = new Vector3[count];
            localRotations = new Quaternion[count];
            localScales = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                Transform fragment = fragments[i].transform;
                localPositions[i] = fragment.localPosition;
                localRotations[i] = fragment.localRotation;
                localScales[i] = fragment.localScale;
                fragments[i].isKinematic = true;
                fragments[i].useGravity = false;
                fragments[i].detectCollisions = false;
            }
        }

        public void Play(Vector3 position, Quaternion rotation, float normalizedSpeed, float preferredSide, uint playSequence)
        {
            gameObject.SetActive(true);
            transform.SetPositionAndRotation(position, rotation);
            playing = true;
            sequence = playSequence;
            remaining = fragmentLifetime;
            int count = Mathf.Min(fragmentCount, fragments.Length);
            float speedScale = Mathf.Lerp(0.88f, 1.12f, Mathf.Clamp01(normalizedSpeed));
            float sideBias = Mathf.Abs(preferredSide) > 0.05f ? Mathf.Sign(preferredSide) : 1f;

            for (int i = 0; i < fragments.Length; i++)
            {
                Rigidbody body = fragments[i];
                body.gameObject.SetActive(i < count);
                if (i >= count) continue;
                Transform fragment = body.transform;
                fragment.localPosition = localPositions[i];
                fragment.localRotation = localRotations[i];
                fragment.localScale = localScales[i] * Random.Range(fragmentScaleRange.x, fragmentScaleRange.y);
                body.isKinematic = false;
                body.useGravity = true;
                body.detectCollisions = false;
                float lane = count <= 1 ? 0f : i / (float)(count - 1) * 2f - 1f;
                body.velocity = transform.forward * forwardForce * speedScale * Random.Range(0.88f, 1.08f)
                    + transform.right * sideForce * (lane + sideBias * 0.16f)
                    + Vector3.up * upwardForce * Random.Range(0.72f, 1.12f);
                body.angularVelocity = Random.onUnitSphere * torque * Random.Range(0.7f, 1.15f);
            }

            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Clear(true);
                particles[i].Play(true);
            }
        }

        private void FixedUpdate()
        {
            if (!playing || Mathf.Approximately(gravityMultiplier, 1f)) return;
            Vector3 extraGravity = Physics.gravity * (gravityMultiplier - 1f);
            int count = Mathf.Min(fragmentCount, fragments.Length);
            for (int i = 0; i < count; i++)
                if (!fragments[i].isKinematic) fragments[i].AddForce(extraGravity, ForceMode.Acceleration);
        }

        private void Update()
        {
            if (!playing) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                StopAndHide();
                return;
            }

            if (remaining > fadeDuration) return;
            float scale = Mathf.Clamp01(remaining / Mathf.Max(0.01f, fadeDuration));
            int count = Mathf.Min(fragmentCount, fragments.Length);
            for (int i = 0; i < count; i++)
                fragments[i].transform.localScale = localScales[i] * scale;
        }

        public void StopAndHide()
        {
            playing = false;
            remaining = 0f;
            for (int i = 0; i < fragments.Length; i++)
            {
                Rigidbody body = fragments[i];
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
                body.detectCollisions = false;
                body.transform.localPosition = localPositions[i];
                body.transform.localRotation = localRotations[i];
                body.transform.localScale = localScales[i];
            }
            for (int i = 0; i < particles.Length; i++) particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            gameObject.SetActive(false);
        }
    }
}
