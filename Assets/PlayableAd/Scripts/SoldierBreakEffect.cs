using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class SoldierBreakEffect : MonoBehaviour
    {
        [SerializeField, Range(3, 8), InspectorName("Fragment Count（碎片数量）")] private int fragmentCount = 5;
        [SerializeField, InspectorName("Fragments（碎片刚体）")] private Rigidbody[] fragments = new Rigidbody[0];
        [SerializeField, InspectorName("Particles（粒子系统）")] private ParticleSystem[] particles = new ParticleSystem[0];
        [SerializeField, Min(0f), InspectorName("Forward Force（前向力度）")] private float forwardForce = 7f;
        [SerializeField, Min(0f), InspectorName("Speed Forward Bonus（速度前向加成）")] private float speedForwardBonus = 7f;
        [SerializeField, Min(1f), InspectorName("Forward Speed Multiplier（前向速度倍率）")] private float forwardSpeedMultiplier = 1.25f;
        [SerializeField, Min(0f), InspectorName("High Speed Multiplier Bonus（高速前向倍率加成）")] private float highSpeedMultiplierBonus = 0.85f;
        [SerializeField, Min(0f), InspectorName("Lateral Range Per Speed Level（每级横向区间增量）")] private float lateralRangePerSpeedLevel = 1f;
        [SerializeField, Min(0f), InspectorName("Fall Recycle Height（坠落回收高度）")] private float fallRecycleHeight = 3f;
        [SerializeField, Min(0f), InspectorName("Upward Force（向上力度）")] private float upwardForce = 3.4f;
        [SerializeField, Min(0f), InspectorName("Torque（扭矩）")] private float torque = 9f;
        [SerializeField, Min(0f), InspectorName("Gravity Multiplier（重力倍率）")] private float gravityMultiplier = 1f;
        [SerializeField, Min(0.1f), InspectorName("Fragment Lifetime（碎片持续时间）")] private float fragmentLifetime = 1f;
        [SerializeField, Min(0.05f), InspectorName("Fade Duration（淡出时长）")] private float fadeDuration = 0.25f;
        [SerializeField, InspectorName("Fragment Scale Range（碎片缩放范围）")] private Vector2 fragmentScaleRange = new Vector2(0.88f, 1.08f);

        private Vector3[] localPositions;
        private Quaternion[] localRotations;
        private Vector3[] localScales;
        private Vector3[] velocities;
        private Vector3[] angularVelocities;
        private float remaining;
        private bool playing;
        private uint sequence;

        public bool IsPlaying => playing;
        public bool IsConfigured
        {
            get
            {
                if (fragments == null) return false;
                for (int i = 0; i < fragments.Length; i++)
                    if (fragments[i] != null) return true;
                return false;
            }
        }
        public uint Sequence => sequence;

        private void Awake()
        {
            if (fragments == null) fragments = new Rigidbody[0];
            if (particles == null) particles = new ParticleSystem[0];
            int count = fragments.Length;
            localPositions = new Vector3[count];
            localRotations = new Quaternion[count];
            localScales = new Vector3[count];
            velocities = new Vector3[count];
            angularVelocities = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                if (fragments[i] == null) continue;
                Transform fragment = fragments[i].transform;
                localPositions[i] = fragment.localPosition;
                localRotations[i] = fragment.localRotation;
                localScales[i] = fragment.localScale;
                fragments[i].isKinematic = true;
                fragments[i].useGravity = false;
                fragments[i].detectCollisions = false;
            }
        }

        public void Play(Vector3 position, Quaternion rotation, float normalizedSpeed, float impactForwardSpeed,
            int speedLevel, uint playSequence)
        {
            gameObject.SetActive(true);
            transform.SetPositionAndRotation(position, rotation);
            playing = true;
            sequence = playSequence;
            remaining = fragmentLifetime;
            int count = Mathf.Min(fragmentCount, fragments.Length);
            float speedT = Mathf.Clamp01(normalizedSpeed);
            float authoredForwardSpeed = forwardForce * 0.65f + speedForwardBonus * speedT;
            float impactSpeedMultiplier = Mathf.Max(1f, forwardSpeedMultiplier) + highSpeedMultiplierBonus * speedT;
            float minimumForwardSpeed = Mathf.Max(0f, impactForwardSpeed) * impactSpeedMultiplier;
            float forwardSpeed = Mathf.Max(authoredForwardSpeed, minimumForwardSpeed);
            float lateralRange = Mathf.Clamp(speedLevel, 1, 10) * lateralRangePerSpeedLevel;

            for (int i = 0; i < fragments.Length; i++)
            {
                Rigidbody body = fragments[i];
                if (body == null) continue;
                body.gameObject.SetActive(i < count);
                if (i >= count) continue;
                Transform fragment = body.transform;
                fragment.localPosition = localPositions[i];
                fragment.localRotation = localRotations[i];
                fragment.localScale = localScales[i] * Random.Range(fragmentScaleRange.x, fragmentScaleRange.y);
                body.isKinematic = true;
                body.useGravity = false;
                body.detectCollisions = false;
                float lateralMomentum = Random.Range(-lateralRange, lateralRange);
                velocities[i] = Vector3.right * lateralMomentum
                    + Vector3.forward * forwardSpeed * Random.Range(1f, 1.08f)
                    + Vector3.up * upwardForce * Random.Range(0.72f, 1.12f);
                angularVelocities[i] = Random.onUnitSphere * torque * Random.Range(0.7f, 1.15f) * Mathf.Rad2Deg;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] == null) continue;
                ParticleSystem.MainModule main = particles[i].main;
                main.simulationSpeed = GetWorldTimeScale();
                particles[i].Clear(true);
                particles[i].Play(true);
            }
        }

        private void Update()
        {
            if (!playing) return;
            UpdateParticleTimeScale();
            float worldDeltaTime = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.GetWorldDeltaTime()
                : Time.deltaTime;
            remaining -= worldDeltaTime;
            if (remaining <= 0f)
            {
                StopAndHide();
                return;
            }

            int count = Mathf.Min(fragmentCount, fragments.Length);
            for (int i = 0; i < count; i++)
            {
                if (fragments[i] == null) continue;
                if (!fragments[i].gameObject.activeSelf) continue;
                velocities[i] += Physics.gravity * gravityMultiplier * worldDeltaTime;
                fragments[i].transform.position += velocities[i] * worldDeltaTime;
                fragments[i].transform.Rotate(angularVelocities[i] * worldDeltaTime, Space.Self);
                if (fragments[i].transform.position.y <= -fallRecycleHeight)
                {
                    fragments[i].gameObject.SetActive(false);
                    velocities[i] = Vector3.zero;
                    angularVelocities[i] = Vector3.zero;
                    continue;
                }
                if (remaining <= fadeDuration)
                {
                    float scale = Mathf.Clamp01(remaining / Mathf.Max(0.01f, fadeDuration));
                    fragments[i].transform.localScale = localScales[i] * scale;
                }
            }
        }

        private void UpdateParticleTimeScale()
        {
            float worldScale = GetWorldTimeScale();
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] == null) continue;
                ParticleSystem.MainModule main = particles[i].main;
                main.simulationSpeed = worldScale;
            }
        }

        private static float GetWorldTimeScale()
        {
            return BulletTimeManager.Instance != null ? BulletTimeManager.Instance.WorldTimeScale : 1f;
        }

        public void StopAndHide()
        {
            playing = false;
            remaining = 0f;
            for (int i = 0; i < fragments.Length; i++)
            {
                Rigidbody body = fragments[i];
                if (body == null) continue;
                body.useGravity = false;
                body.isKinematic = true;
                body.detectCollisions = false;
                velocities[i] = Vector3.zero;
                angularVelocities[i] = Vector3.zero;
                body.transform.localPosition = localPositions[i];
                body.transform.localRotation = localRotations[i];
                body.transform.localScale = localScales[i];
            }
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] == null) continue;
                ParticleSystem.MainModule main = particles[i].main;
                main.simulationSpeed = 1f;
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            gameObject.SetActive(false);
        }
    }
}
