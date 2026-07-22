using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayableAd
{
    [Serializable]
    public sealed class SoldierKnockbackSettings
    {
        [Header("Trajectory（飞行轨迹）")]
        [Range(4, 24), InspectorName("Max Active Soldiers（最大同时飞行士兵数）")] public int maxActiveSoldiers = 16;
        [Min(0.1f), InspectorName("Flight Lifetime（飞行持续时间）")] public float flightLifetime = 1.6f;
        [Min(0f), InspectorName("Minimum Forward Speed（最低前向速度）")] public float minimumForwardSpeed = 8f;
        [Min(0f), InspectorName("Maximum Authored Forward Speed（预设最高前向速度）")] public float maximumAuthoredForwardSpeed = 16f;
        [Min(1f), InspectorName("Impact Speed Multiplier（撞击速度倍率）")] public float impactSpeedMultiplier = 1.25f;
        [Min(0f), InspectorName("High Speed Multiplier Bonus（高速倍率加成）")] public float highSpeedMultiplierBonus = 0.85f;
        [Min(0f), InspectorName("Lateral Range Per Speed Level（每级横向随机范围）")] public float lateralRangePerSpeedLevel = 1f;
        [Min(0f), InspectorName("Upward Speed（向上速度）")] public float upwardSpeed = 6.2f;
        [Min(0f), InspectorName("Gravity Multiplier（重力倍率）")] public float gravityMultiplier = 1.15f;
        [Min(0f), InspectorName("Below Road Recycle Depth（路面下回收深度）")] public float belowRoadRecycleDepth = 3f;

        [Header("Whole Model Rotation（整模型旋转）")]
        [Min(0f), InspectorName("Minimum Rotation Speed（最低旋转速度，度/秒）")] public float minimumRotationSpeed = 240f;
        [Min(0f), InspectorName("Maximum Rotation Speed（最高旋转速度，度/秒）")] public float maximumRotationSpeed = 720f;
    }

    [DisallowMultipleComponent]
    public sealed class SoldierKnockbackEffect : MonoBehaviour
    {
        private static readonly List<SoldierKnockbackEffect> ActiveEffects = new List<SoldierKnockbackEffect>();

        private Animator[] animators = Array.Empty<Animator>();
        private EnemyVisibilityController visibility;
        private Vector3 velocity;
        private Vector3 rotationAxis = Vector3.right;
        private float angularSpeed;
        private float gravityMultiplier;
        private float recycleHeight;
        private float remaining;
        private bool playing;

        public bool IsPlaying => playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ActiveEffects.Clear();
        }

        private void Awake()
        {
            animators = GetComponentsInChildren<Animator>(true);
        }

        public bool Launch(SoldierKnockbackSettings settings, float normalizedSpeed,
            float impactForwardSpeed, int speedLevel, EnemyVisibilityController visibilityController)
        {
            if (settings == null || playing) return false;

            int maxActive = Mathf.Clamp(settings.maxActiveSoldiers, 4, 24);
            while (ActiveEffects.Count >= maxActive)
            {
                SoldierKnockbackEffect oldest = ActiveEffects[0];
                ActiveEffects.RemoveAt(0);
                if (oldest != null) oldest.Finish();
            }

            for (int i = 0; i < animators.Length; i++)
                if (animators[i] != null) animators[i].enabled = false;

            float speedT = Mathf.Clamp01(normalizedSpeed);
            float authoredMin = Mathf.Min(settings.minimumForwardSpeed, settings.maximumAuthoredForwardSpeed);
            float authoredMax = Mathf.Max(settings.minimumForwardSpeed, settings.maximumAuthoredForwardSpeed);
            float authoredForwardSpeed = Mathf.Lerp(authoredMin, authoredMax, speedT);
            float impactMultiplier = Mathf.Max(1f, settings.impactSpeedMultiplier)
                + Mathf.Max(0f, settings.highSpeedMultiplierBonus) * speedT;
            float forwardSpeed = Mathf.Max(authoredForwardSpeed,
                Mathf.Max(0f, impactForwardSpeed) * impactMultiplier);
            float lateralRange = Mathf.Clamp(speedLevel, 1, 10)
                * Mathf.Max(0f, settings.lateralRangePerSpeedLevel);

            velocity = new Vector3(
                UnityEngine.Random.Range(-lateralRange, lateralRange),
                Mathf.Max(0f, settings.upwardSpeed) * UnityEngine.Random.Range(0.92f, 1.08f),
                forwardSpeed * UnityEngine.Random.Range(1f, 1.06f));

            rotationAxis = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-0.25f, 0.25f),
                UnityEngine.Random.Range(-1f, 1f));
            if (rotationAxis.sqrMagnitude < 0.01f) rotationAxis = Vector3.right;
            rotationAxis.Normalize();
            float angularMin = Mathf.Min(settings.minimumRotationSpeed, settings.maximumRotationSpeed);
            float angularMax = Mathf.Max(settings.minimumRotationSpeed, settings.maximumRotationSpeed);
            angularSpeed = Mathf.Lerp(angularMin, angularMax, speedT)
                * UnityEngine.Random.Range(0.85f, 1.15f);

            visibility = visibilityController;
            gravityMultiplier = Mathf.Max(0f, settings.gravityMultiplier);
            recycleHeight = -Mathf.Abs(settings.belowRoadRecycleDepth);
            remaining = Mathf.Max(0.1f, settings.flightLifetime);
            playing = true;
            ActiveEffects.Add(this);
            return true;
        }

        private void Update()
        {
            if (!playing) return;
            float worldDeltaTime = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.GetWorldDeltaTime()
                : Time.deltaTime;
            if (worldDeltaTime <= 0f) return;

            velocity += Physics.gravity * gravityMultiplier * worldDeltaTime;
            transform.position += velocity * worldDeltaTime;
            transform.rotation = Quaternion.AngleAxis(angularSpeed * worldDeltaTime, rotationAxis)
                * transform.rotation;
            remaining -= worldDeltaTime;

            if (remaining <= 0f || transform.position.y <= recycleHeight)
                Finish();
        }

        private void Finish()
        {
            if (!playing) return;
            playing = false;
            ActiveEffects.Remove(this);
            velocity = Vector3.zero;
            angularSpeed = 0f;
            EnemyVisibilityController visibilityToRecycle = visibility;
            visibility = null;
            if (visibilityToRecycle != null)
                visibilityToRecycle.Recycle();
            else
                gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            playing = false;
            ActiveEffects.Remove(this);
            velocity = Vector3.zero;
            angularSpeed = 0f;
            visibility = null;
        }
    }
}
