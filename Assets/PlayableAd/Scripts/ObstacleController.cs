using System;
using UnityEngine;

namespace PlayableAd
{
    public enum ObstacleType
    {
        Soldier,
        StoneWall
    }

    public enum ObstacleResolutionType
    {
        Boosted,
        Equal,
        Dropped
    }

    public enum CollisionOutcome
    {
        SpeedGain,
        Neutral,
        SpeedLoss
    }

    public readonly struct ObstacleResolvedEvent
    {
        public readonly ObstacleController Obstacle;
        public readonly ObstacleResolutionType Resolution;
        public readonly CollisionOutcome Outcome;
        public readonly int PlayerLevelAtImpact;
        public readonly int RequiredSpeedLevel;

        public ObstacleResolvedEvent(ObstacleController obstacle, ObstacleResolutionType resolution,
            CollisionOutcome outcome, int playerLevelAtImpact, int requiredSpeedLevel)
        {
            Obstacle = obstacle;
            Resolution = resolution;
            Outcome = outcome;
            PlayerLevelAtImpact = playerLevelAtImpact;
            RequiredSpeedLevel = requiredSpeedLevel;
        }
    }

    public sealed class ObstacleController : MonoBehaviour
    {
        [SerializeField, Range(1, PlayerSpeedSettings.RequiredLevelCount), InspectorName("Required Speed Level（要求速度等级）")] private int requiredSpeedLevel = 1;
        [SerializeField, InspectorName("Obstacle Type（障碍物类型）")] private ObstacleType obstacleType;
        [SerializeField, InspectorName("Obstacle Colliders（障碍物碰撞体）")] private Collider[] obstacleColliders = Array.Empty<Collider>();
        [SerializeField, InspectorName("Has Resolved（已结算）")] private bool hasResolved;

        public event Action<ObstacleResolvedEvent> Resolved;

        public int RequiredSpeedLevel => requiredSpeedLevel;
        public ObstacleType Type => obstacleType;
        public bool HasResolved => hasResolved;

        public static CollisionOutcome EvaluateCollisionOutcome(int playerLevel, int requiredLevel)
        {
            return playerLevel >= requiredLevel ? CollisionOutcome.SpeedGain : CollisionOutcome.SpeedLoss;
        }

        public void Initialize(int requiredLevel, ObstacleType type, Collider[] colliders)
        {
            requiredSpeedLevel = Mathf.Clamp(requiredLevel, 1, PlayerSpeedSettings.RequiredLevelCount);
            obstacleType = type;
            obstacleColliders = colliders ?? Array.Empty<Collider>();
            hasResolved = false;
        }

        public ObstacleResolutionType Resolve(PlayerSpeedController speedController, float boostAmount)
        {
            return ResolveInternal(speedController, boostAmount, null);
        }

        public ObstacleResolutionType Resolve(PlayerSpeedController speedController, float boostAmount,
            float speedLossAmount)
        {
            return ResolveInternal(speedController, boostAmount, Mathf.Max(0f, speedLossAmount));
        }

        private ObstacleResolutionType ResolveInternal(PlayerSpeedController speedController, float boostAmount,
            float? fixedSpeedLossAmount)
        {
            if (hasResolved || speedController == null)
                return ObstacleResolutionType.Equal;

            // Set first so multiple custom or physics callbacks cannot settle this obstacle twice.
            hasResolved = true;
            DisableColliders();

            int playerLevel = speedController.GetCurrentLevel();
            CollisionOutcome outcome = EvaluateCollisionOutcome(playerLevel, requiredSpeedLevel);
            ObstacleResolutionType resolution;
            if (outcome == CollisionOutcome.SpeedGain)
            {
                resolution = ObstacleResolutionType.Boosted;
                speedController.AddSpeed(boostAmount, SpeedChangeReason.LowLevelCollisionReward, this);
            }
            else if (outcome == CollisionOutcome.Neutral)
            {
                resolution = ObstacleResolutionType.Equal;
            }
            else
            {
                resolution = ObstacleResolutionType.Dropped;
                if (fixedSpeedLossAmount.HasValue)
                {
                    speedController.SetSpeed(speedController.CurrentSpeed - fixedSpeedLossAmount.Value,
                        SpeedChangeReason.HighLevelCollisionPenalty, this);
                }
                else
                {
                    speedController.DropOneLevel(SpeedChangeReason.HighLevelCollisionPenalty, this);
                }
            }

            Resolved?.Invoke(new ObstacleResolvedEvent(this, resolution, outcome, playerLevel, requiredSpeedLevel));
            return resolution;
        }

        public void DisableColliders()
        {
            for (int i = 0; i < obstacleColliders.Length; i++)
                if (obstacleColliders[i] != null) obstacleColliders[i].enabled = false;
        }
    }
}
