using System;
using UnityEngine;

namespace PlayableAd
{
    public enum ObstacleType
    {
        Soldier,
        StoneWall
    }

    public enum ObstacleFeedbackType
    {
        NormalImpact,
        HeavyBreak
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
        [SerializeField, InspectorName("Intact Object（完整对象）")] private GameObject intactObject;
        [SerializeField, InspectorName("Destroyed Object（破坏后对象）")] private GameObject destroyedObject;
        [SerializeField, InspectorName("Obstacle Colliders（障碍物碰撞体）")] private Collider[] obstacleColliders = Array.Empty<Collider>();
        [SerializeField, InspectorName("Feedback Type（反馈类型）")] private ObstacleFeedbackType feedbackType;
        [SerializeField, InspectorName("Has Resolved（已结算）")] private bool hasResolved;
        [SerializeField, InspectorName("Initial World Position（初始世界坐标）")] private Vector3 initialWorldPosition;
        [SerializeField, InspectorName("Last Resolution（上次结算结果）")] private ObstacleResolutionType lastResolution;
        [SerializeField, InspectorName("Player Level At Resolution（结算时玩家等级）")] private int playerLevelAtResolution;

        public event Action<ObstacleResolvedEvent> Resolved;

        public int RequiredSpeedLevel => requiredSpeedLevel;
        public ObstacleType Type => obstacleType;
        public ObstacleFeedbackType FeedbackType => feedbackType;
        public bool HasResolved => hasResolved;
        public Vector3 InitialWorldPosition => initialWorldPosition;
        public ObstacleResolutionType LastResolution => lastResolution;
        public int PlayerLevelAtResolution => playerLevelAtResolution;

        public static CollisionOutcome EvaluateCollisionOutcome(int playerLevel, int requiredLevel)
        {
            if (playerLevel > requiredLevel) return CollisionOutcome.SpeedGain;
            if (playerLevel < requiredLevel) return CollisionOutcome.SpeedLoss;
            return CollisionOutcome.Neutral;
        }

        public void Initialize(int requiredLevel, ObstacleType type, GameObject intact, GameObject destroyed,
            Collider[] colliders, ObstacleFeedbackType feedback)
        {
            requiredSpeedLevel = Mathf.Clamp(requiredLevel, 1, PlayerSpeedSettings.RequiredLevelCount);
            obstacleType = type;
            intactObject = intact;
            destroyedObject = destroyed;
            obstacleColliders = colliders ?? Array.Empty<Collider>();
            feedbackType = feedback;
            hasResolved = false;
            initialWorldPosition = transform.position;
            lastResolution = ObstacleResolutionType.Equal;
            playerLevelAtResolution = 0;
            if (destroyedObject != null && destroyedObject != intactObject)
                destroyedObject.SetActive(false);
        }

        public ObstacleResolutionType Resolve(PlayerSpeedController speedController, float boostAmount, float boostSoftCap)
        {
            if (hasResolved || speedController == null)
                return ObstacleResolutionType.Equal;

            // Set first so multiple custom or physics callbacks cannot settle this obstacle twice.
            hasResolved = true;
            DisableColliders();

            int playerLevel = speedController.GetCurrentLevel();
            playerLevelAtResolution = playerLevel;
            CollisionOutcome outcome = EvaluateCollisionOutcome(playerLevel, requiredSpeedLevel);
            ObstacleResolutionType resolution;
            if (outcome == CollisionOutcome.SpeedGain)
            {
                resolution = ObstacleResolutionType.Boosted;
                speedController.AddSpeed(boostAmount, boostSoftCap, SpeedChangeReason.LowLevelCollisionReward, this);
            }
            else if (outcome == CollisionOutcome.Neutral)
            {
                resolution = ObstacleResolutionType.Equal;
            }
            else
            {
                resolution = ObstacleResolutionType.Dropped;
                speedController.DropOneLevel(SpeedChangeReason.HighLevelCollisionPenalty, this);
            }

            lastResolution = resolution;

            if (destroyedObject != null && destroyedObject != intactObject)
            {
                destroyedObject.SetActive(true);
                if (intactObject != null) intactObject.SetActive(false);
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
