using System;
using UnityEngine;

namespace PlayableAd
{
    public sealed class ElixirPickup : MonoBehaviour
    {
        [SerializeField, Range(1, PlayerSpeedSettings.RequiredLevelCount), InspectorName("Target Speed Level（目标速度等级）")] private int targetSpeedLevel = 4;
        [SerializeField, InspectorName("Pickup Colliders（拾取碰撞体）")] private Collider[] pickupColliders = Array.Empty<Collider>();
        [SerializeField, InspectorName("Has Collected（已拾取）")] private bool hasCollected;

        private PlayerSpeedController speedController;

        public bool HasCollected => hasCollected;

        public void Initialize(PlayerSpeedController controller, int targetLevel, Collider[] colliders)
        {
            speedController = controller;
            targetSpeedLevel = Mathf.Clamp(targetLevel, 1, controller != null ? controller.MaxLevel : PlayerSpeedSettings.RequiredLevelCount);
            pickupColliders = colliders ?? Array.Empty<Collider>();
            hasCollected = false;
        }

        public bool TryCollect()
        {
            if (hasCollected || speedController == null) return false;

            hasCollected = true;
            for (int i = 0; i < pickupColliders.Length; i++)
                if (pickupColliders[i] != null) pickupColliders[i].enabled = false;
            float targetSpeed = speedController.GetLevelStartSpeed(targetSpeedLevel);
            speedController.SetSpeed(Mathf.Max(speedController.CurrentSpeed, targetSpeed), SpeedChangeReason.PotionPickup, this);
            return true;
        }
    }
}
