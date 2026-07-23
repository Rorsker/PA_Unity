using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace PlayableAd
{
    public sealed class PlayableAdGame : MonoBehaviour
    {
        private const float WallCollisionDistance = 1.3f;
        private const int GameplayLevelConfigurationVersion = 10;
        private const float FirstSpeedLossSectionEndZ = 150f;
        private const float SecondSpeedLossSectionEndZ = 350f;
        private const int SoldierDisplayLevel = 1;
        private const int OpeningElixirGroupId = 1;
        private const float NaturalSpeedLossProtectionDuration = 3f;
        private const int MainRunLowSpeedRecoveryLevel = 4;
        private const float ElixirVisualTargetHeight = 1.45f;
        private const float ElixirVisualBottomOffset = -0.72f;
        private static readonly Color SpeedLossImpactColor = new Color(0.82f, 0.035f, 0.025f, 0.76f);
        private const float RoadBorderSourceSegmentLength = 6.012f;
        private const float RoadBorderSegmentScale = 4f;
        private const float RoadBorderSegmentLength = RoadBorderSourceSegmentLength * RoadBorderSegmentScale;
        private const float RoadBorderCenterX = 4.4f;
        private const float RoadBorderCenterY = 0.25f;
        private const int RoadBorderSegmentsPerChunk = 5;
        private const int RoadBorderPedestalInterval = 2;
        private const float BossDeathFlightDistance = 9f;
        private const float BossDeathFlightHeight = 2.4f;
        private const float BossDeathAnimationDuration = 1.05f;
        private const float BossStandingY = 6.5f;
        private static readonly int BossDieHash = Animator.StringToHash("Die");
        private static readonly int BossClashHash = Animator.StringToHash("boss3");
        private static readonly int PrincessIdleHash = Animator.StringToHash("gongzhu");
        private static readonly int PrincessWalkHash = Animator.StringToHash("gongzhu2");
        private static readonly int BossAttackingHash = Animator.StringToHash("IsAttacking");

        public enum SoldierPlacementMode
        {
            [InspectorName("Free Random Dense（自由随机密集）")]
            RandomDense,
            [InspectorName("Left Lane Line（左路线直线队列）")]
            LeftLaneLine,
            [InspectorName("Center Lane Line（中路线直线队列）")]
            CenterLaneLine,
            [InspectorName("Right Lane Line（右路线直线队列）")]
            RightLaneLine
        }

        public enum StoneWallBlockingMode
        {
            [InspectorName("All Three Lanes（阻挡三条路线）")]
            AllThreeLanes,
            [InspectorName("Left And Center（阻挡左中路）")]
            LeftAndCenter,
            [InspectorName("Center And Right（阻挡中右路）")]
            CenterAndRight,
            [InspectorName("Left Lane Only（只阻挡左路）")]
            LeftLaneOnly,
            [InspectorName("Right Lane Only（只阻挡右路）")]
            RightLaneOnly,
            [InspectorName("Center Lane Only（只阻挡中路）")]
            CenterLaneOnly
        }

        [Serializable]
        public sealed class SoldierFormationSettings
        {
            [InspectorName("Section Name（区段名称）")] public string sectionName = "Momentum";
            [Min(0f), InspectorName("Start Offset From Tutorial（距教学起始偏移）")] public float startOffsetFromTutorial = 4.6125f;
            [Range(1, 50), InspectorName("Density Rows（密度行数）")] public int soldierCount = 10;
            [InspectorName("Placement Mode（摆放模式）")] public SoldierPlacementMode placementMode = SoldierPlacementMode.RandomDense;
            [Range(0.4f, 3f), InspectorName("Forward Spacing（前后间距）")] public float minimumForwardSpacing = 1.5f;
            [Range(0.4f, 1f), InspectorName("Horizontal Coverage（横向覆盖范围）")] public float horizontalCoverage = 1f;
            [Range(0f, 0.9f), InspectorName("Forward Randomness（前后随机度）")] public float forwardRandomness = 0.8f;
        }

        [Serializable]
        public sealed class StoneWallSectionSettings
        {
            [InspectorName("Section Name（区段名称）")] public string sectionName = "StoneWall";
            [Min(0f), InspectorName("Start Offset From Tutorial（距教学起始偏移）")] public float startOffsetFromTutorial = 62.5f;
            [InspectorName("Blocking Mode（阻挡模式）")] public StoneWallBlockingMode blockingMode = StoneWallBlockingMode.AllThreeLanes;
            [InspectorName("Bullet Time（子弹时间）")] public BulletTimeSettings bulletTime = new BulletTimeSettings { enabled = false };
        }

        [Serializable]
        public sealed class BossRewardRunSettings
        {
            [InspectorName("Enabled（启用奖励关）")] public bool enabled = true;
            [Min(20f), InspectorName("Reward Run Length（奖励关长度）")] public float length = 200f;
            [Range(1, 20), InspectorName("Soldier Section Count（士兵区段数量）")] public int soldierSectionCount = 10;
            [Range(1, 50), InspectorName("Soldiers Per Section（每区段士兵数量）")] public int soldiersPerSection = 40;
            [Range(1, 20), InspectorName("Stone Wall Count（石墙数量）")] public int stoneWallCount = 10;
            [Min(0f), InspectorName("Start Offset After Boss（Boss后起始偏移）")] public float startOffsetAfterBoss = 14f;
            [Range(0.1f, 1.2f), InspectorName("Dense Soldier Spacing（密集士兵间距）")] public float soldierSpacing = 0.22f;
        }

        [Serializable]
        public sealed class PrefabModules
        {
            [Header("Soldier section modules（士兵区段模块）")]
            [InspectorName("Soldier Sections（士兵区段）")] public SoldierFormationSettings[] soldierSections = Array.Empty<SoldierFormationSettings>();

            [Header("Elixir visual modules（药剂视觉模块）")]
            [InspectorName("Tutorial Left Elixir - Bottle 8（教学左路药剂 - Bottle 8）")] public GameObject openingElixirLeftPrefab;
            [InspectorName("Tutorial Center Elixir - Bottle 9（教学中路药剂 - Bottle 9）")] public GameObject openingElixirCenterPrefab;
            [InspectorName("Tutorial Right Elixir - Bottle 10（教学右路药剂 - Bottle 10）")] public GameObject openingElixirRightPrefab;
            [InspectorName("Boss Max Speed Elixir - Bottle 4（Boss前满速药剂 - Bottle 4）")] public GameObject bossMaxSpeedElixirPrefab;

            [Header("Stone wall modules（石墙模块）")]
            [InspectorName("Stone Wall Prefab（石墙预制体）")] public GameObject stoneWallPrefab;
            [InspectorName("Additional Stone Walls（额外石墙区段）")] public StoneWallSectionSettings[] additionalStoneWalls = Array.Empty<StoneWallSectionSettings>();
        }

        [Serializable]
        public sealed class Tuning
        {
            [SerializeField, HideInInspector]
            public int gameplayLevelConfigurationVersion;

            [Header("Opening Tutorial（开场教学）")]
            [Range(1f, 2f), InspectorName("Opening Elixir Time（开场药剂时间）")] public float openingElixirTime = 1.23f;
            [Range(3, 5), InspectorName("Tutorial Soldier Count（教学士兵数量）")] public int tutorialSoldierCount = 5;
            [Range(1.2f, 3.2f), InspectorName("Tutorial Soldier Spacing（教学士兵间距）")] public float tutorialSoldierSpacing = 1.85f;
            [Range(1.5f, 5f), InspectorName("Tutorial First Soldier Gap（首个教学士兵间隔）")] public float tutorialFirstSoldierGap = 2.46f;
            [Range(3f, 12f), InspectorName("Tutorial Wall Gap（教学墙体间隔）")] public float tutorialWallGap = 6.15f;
            [InspectorName("Tutorial Wall Blocking Mode（教学墙阻挡模式）")] public StoneWallBlockingMode tutorialWallBlockingMode = StoneWallBlockingMode.AllThreeLanes;
            [InspectorName("Tutorial Wall Bullet Time（教学墙子弹时间）")] public BulletTimeSettings tutorialWallBulletTime = new BulletTimeSettings
            {
                enabled = true,
                triggerDistance = 5f,
                duration = 0.5f,
                worldTimeScale = 0.25f,
                enterDuration = 0.1f,
                exitDuration = 0.1f
            };
            [Range(1, PlayerSpeedSettings.RequiredLevelCount), InspectorName("Tutorial Exit Speed Level（教学结束速度等级）")]
            public int tutorialExitSpeedLevel = 2;
            [Header("Stone Wall Collision（石墙碰撞）")]
            [Min(1), InspectorName("Stone Wall Safe Speed Level（石墙安全速度等级）")]
            public int stoneWallSafeSpeedLevel = 6;
            [Tooltip("Subtracts from the continuous speed value（从连续速度值中扣除）")]
            [Min(0f), InspectorName("Stone Wall Speed Penalty Levels（石墙减速等级）")]
            public float stoneWallSpeedPenaltyLevels = 0.5f;

            [Header("Forward Speed Loss（前进速度损失）")]
            [InspectorName("Speed Loss Enabled（启用速度损失）")] public bool forwardSpeedLossEnabled = true;
            [FormerlySerializedAs("forwardSpeedLossPerSecond")]
            [Min(0f), InspectorName("Section 1 Loss Per Second, Z 0-150（第一区段每秒速度损失，Z 0-150）")]
            public float firstSectionSpeedLossPerSecond;
            [Min(0f), InspectorName("Section 2 Loss Per Second, Z 150-350（第二区段每秒速度损失，Z 150-350）")]
            public float secondSectionSpeedLossPerSecond;
            [Min(0f), InspectorName("Section 3 Loss Per Second, Z 350-500（第三区段每秒速度损失，Z 350-500）")]
            public float thirdSectionSpeedLossPerSecond;
            [Range(1f, 10f), InspectorName("Minimum Speed After Loss（损失后最低速度）")] public float minimumSpeedAfterLoss = 1f;

            [Header("Course（路线）")]
            [Tooltip("Total world-space distance from the start to the Boss encounter.")]
            [Min(20f), InspectorName("Boss Distance（Boss 距离）")] public float bossDistance = 500f;
            [Min(0f), InspectorName("Boss Max Speed Elixir Distance（Boss前满速药剂距离）")]
            public float bossMaxSpeedElixirDistance = 30f;
            [Header("Data-driven Main Run（数据驱动主流程）")]
            [InspectorName("Use Document Main Run Layout（使用文档主路线排布）")]
            public bool useDocumentMainRunLayout = true;
            [Min(0f), InspectorName("Document Main Run Start Offset（文档主路线起始偏移）")]
            public float documentMainRunStartOffset = 10f;
            [Min(0f), InspectorName("Document Zone Gap（文档区块间隔）")]
            public float documentZoneGap = 7f;
            [Min(0.1f), InspectorName("Document Single Content Length（单个内容占用长度）")]
            public float documentSingleContentLength = 1f;
            [Range(1, PlayerSpeedSettings.RequiredLevelCount), InspectorName("Document Elite Level（文档精英等级）")]
            public int documentEliteLevel = 8;
            [Min(0f), InspectorName("Document Elite Speed Penalty（文档精英速度惩罚）")]
            public float documentEliteSpeedPenalty = 0.5f;
            [Header("Document Temporary Potions（文档临时药剂）")]
            [Min(0f), InspectorName("Small Potion Boost（小瓶速度增量）")]
            public float documentSmallPotionBoost = 1f;
            [Min(0f), InspectorName("Small Potion Hold（小瓶保持时长）")]
            public float documentSmallPotionHold = 3f;
            [Range(0f, 0.5f), InspectorName("Small Potion Invulnerability Gold Filter（小瓶无敌金色滤镜强度）")]
            public float smallPotionInvulnerabilityGoldFilter = 0.2f;
            [Range(0.02f, 0.5f), InspectorName("Small Potion Filter Fade Duration（小瓶滤镜淡入淡出时长）")]
            public float smallPotionFilterFadeDuration = 0.1f;
            [Min(0f), InspectorName("Large Potion Boost（大瓶速度增量）")]
            public float documentLargePotionBoost = 2f;
            [Min(0f), InspectorName("Large Potion Hold（大瓶保持时长）")]
            public float documentLargePotionHold = 1f;
            [Min(10f), InspectorName("Boss Approach Padding（Boss 接近缓冲）")] public float bossApproachPadding = 13.46154f;
            [InspectorName("Procedural Seed（程序化随机种子）")] public int proceduralSeed = 41723;
            [Header("Mobile encounter budget（移动端遭遇预算）")]
            [Range(8f, 30f), InspectorName("Recycle Distance（回收距离）")] public float recycleDistance = 18f;
            [Header("Continuous enemy visibility staging（连续敌人可见性分级）")]
            [Range(4f, 6f), InspectorName("Enemy Preload Time（敌人预加载时间）")] public float enemyPreloadTime = 5f;
            [Range(2f, 4f), InspectorName("Enemy Visible Preview Time（敌人可见预览时间）")] public float enemyVisiblePreviewTime = 3.2f;
            [Range(0.8f, 2f), InspectorName("Enemy Active Time（敌人活动时间）")] public float enemyActiveTime = 1.35f;
            [Range(24f, 60f), InspectorName("Minimum Enemy Preload Distance（最小敌人预加载距离）")] public float minimumEnemyPreloadDistance = 32f;
            [Range(80f, 160f), InspectorName("Maximum Enemy Preload Distance（最大敌人预加载距离）")] public float maximumEnemyPreloadDistance = 140f;
            [Range(14f, 40f), InspectorName("Minimum Enemy Visible Distance（最小敌人可见距离）")] public float minimumEnemyVisibleDistance = 20f;
            [Range(60f, 120f), InspectorName("Maximum Enemy Visible Distance（最大敌人可见距离）")] public float maximumEnemyVisibleDistance = 100f;
            [Range(8f, 24f), InspectorName("Minimum Enemy Active Distance（最小敌人活动距离）")] public float minimumEnemyActiveDistance = 10f;
            [Range(30f, 70f), InspectorName("Maximum Enemy Active Distance（最大敌人活动距离）")] public float maximumEnemyActiveDistance = 52f;
            [Range(48, 96), InspectorName("Max Preloaded Enemies（最大预加载敌人数）")] public int maxPreloadedEnemies = 80;

            public void UpgradeGameplayLevelConfiguration(PlayerSpeedSettings speedSettings,
                PrefabModules prefabModules)
            {
                if (gameplayLevelConfigurationVersion >= GameplayLevelConfigurationVersion) return;

                if (gameplayLevelConfigurationVersion < 1)
                {
                    stoneWallSafeSpeedLevel = 6;
                    documentEliteLevel = 8;
                }
                if (gameplayLevelConfigurationVersion < 2)
                {
                    documentLargePotionBoost = 2f;
                    if (speedSettings != null) speedSettings.tutorialElixirTargetLevel = 2;
                }
                if (gameplayLevelConfigurationVersion < 3)
                    documentSmallPotionHold = 1f;
                if (gameplayLevelConfigurationVersion < 4)
                {
                    firstSectionSpeedLossPerSecond = 0f;
                    secondSectionSpeedLossPerSecond = 0f;
                    thirdSectionSpeedLossPerSecond = 0f;
                }
                if (gameplayLevelConfigurationVersion < 5)
                {
                    if (tutorialWallBulletTime == null) tutorialWallBulletTime = new BulletTimeSettings();
                    tutorialWallBulletTime.triggerDistance = 5f;
                    tutorialWallBulletTime.duration = 0.5f;
                    tutorialWallBulletTime.enterDuration = 0.1f;
                    tutorialWallBulletTime.exitDuration = 0.1f;
                    documentEliteSpeedPenalty = 0.5f;
                }
                if (gameplayLevelConfigurationVersion < 6 && prefabModules != null
                    && prefabModules.soldierSections != null)
                {
                    for (int i = 0; i < prefabModules.soldierSections.Length; i++)
                    {
                        SoldierFormationSettings section = prefabModules.soldierSections[i];
                        if (section != null) section.minimumForwardSpacing = 1.5f;
                    }
                }
                if (gameplayLevelConfigurationVersion < 7)
                    documentZoneGap = 7f;
                if (gameplayLevelConfigurationVersion < 8)
                    tutorialExitSpeedLevel = 2;
                if (gameplayLevelConfigurationVersion < 9)
                {
                    smallPotionInvulnerabilityGoldFilter = 0.2f;
                    smallPotionFilterFadeDuration = 0.1f;
                }
                if (gameplayLevelConfigurationVersion < 10)
                    documentSmallPotionHold = 3f;
                gameplayLevelConfigurationVersion = GameplayLevelConfigurationVersion;
            }
            [Range(24, 64), InspectorName("Max Distant Visible Enemies（最大远处可见敌人数）")] public int maxDistantVisibleEnemies = 48;
            [Header("Touch steering（触屏横向操控）")]
            [Min(1f), InspectorName("Lane Half Width（路线半宽）")] public float laneHalfWidth = 3.2f;
            [Range(0.5f, 1.5f), InspectorName("Drag Follow Ratio（拖动跟随比例）")] public float dragFollowRatio = 1f;
            [Tooltip("Fine movement response. Large drags automatically use a faster catch-up response.")]
            [Range(30f, 70f), InspectorName("Drag Follow Sharpness（拖拽跟随灵敏度）")] public float dragFollowSharpness = 45f;
            [Min(1f), InspectorName("Keyboard Lateral Speed（键盘横移速度）")] public float lateralSpeed = 9f;
            [Range(0f, 0.05f), InspectorName("Drag Dead Zone（拖拽死区）")] public float dragDeadZone = 0.005f;
            [InspectorName("Invert Drag Input（反转拖拽输入）")] public bool invertDragInput;
            [InspectorName("Block Input Over UI（阻止界面区域输入）")] public bool blockInputOverUi = true;

        }

        [Serializable]
        public sealed class EnvironmentTuning
        {
            [Header("Fixed 2.5D Camera（固定 2.5D 镜头）")]
            [InspectorName("Camera Follows Player（镜头跟随玩家）")] public bool cameraFollowsPlayer = true;
            [InspectorName("Camera Follows Player Laterally（镜头横向跟随玩家）")] public bool cameraFollowsPlayerLaterally = false;
            [Range(45f, 70f), InspectorName("Base Field Of View（基础视场角）")] public float baseFieldOfView = 50f;
            [Range(3f, 12f), InspectorName("Camera Height（镜头高度）")] public float cameraHeight = 4.5f;
            [Range(5f, 20f), InspectorName("Camera Back Distance（镜头后置距离）")] public float cameraBackDistance = 7.2f;
            [Range(0f, 18f), InspectorName("Camera Look Ahead（镜头前视距离）")] public float cameraLookAhead = 3f;
            [Range(4f, 14f), InspectorName("Camera Follow Sharpness（镜头跟随锐度）")] public float cameraFollowSharpness = 9f;
            [Range(0f, 6f), InspectorName("High Speed Pullback（高速后拉）")] public float highSpeedPullback = 1f;
            [Range(0f, 10f), InspectorName("High Speed Look Ahead Bonus（高速前视加成）")] public float highSpeedLookAheadBonus = 1.5f;
            [Range(2f, 16f), InspectorName("Environment Reference Spacing（环境参考间距）")] public float environmentReferenceSpacing = 4.615384f;

            [Header("Road Collision Boundaries（道路碰撞边界）")]
            [Min(0.1f), InspectorName("Road Boundary Thickness（道路边界厚度）")] public float roadBoundaryThickness = 0.35f;
            [Min(2f), InspectorName("Road Boundary Height（道路边界高度）")] public float roadBoundaryHeight = 5.5f;

            [Header("Medieval Stone Palette（中世纪石材色板）")]
            [InspectorName("Sky Fog Color（天空雾色）")] public Color skyFogColor = new Color(0.24f, 0.32f, 0.39f);
            [InspectorName("Road Color（道路颜色）")] public Color roadColor = new Color(0.30f, 0.33f, 0.34f);
            [InspectorName("Route Mark Color（路线标记颜色）")] public Color routeMarkColor = new Color(0.53f, 0.54f, 0.50f);
            [InspectorName("Wall Color（墙体颜色）")] public Color wallColor = new Color(0.31f, 0.29f, 0.28f);
            [InspectorName("Castle Color（城堡颜色）")] public Color castleColor = new Color(0.25f, 0.28f, 0.31f);
            [InspectorName("Timber Color（木材颜色）")] public Color timberColor = new Color(0.25f, 0.19f, 0.15f);

            [Header("Lighting And Depth（光照与景深）")]
            [InspectorName("Light Euler（光源欧拉角）")] public Vector3 lightEuler = new Vector3(46f, -32f, 0f);
            [Range(0.3f, 2f), InspectorName("Light Intensity（光照强度）")] public float lightIntensity = 1.25f;
            [InspectorName("Light Color（光源颜色）")] public Color lightColor = new Color(1f, 0.88f, 0.75f);
            [InspectorName("Ambient Color（环境颜色）")] public Color ambientColor = new Color(0.52f, 0.55f, 0.58f);
            [Range(0f, 1f), InspectorName("Shadow Strength（阴影强度）")] public float shadowStrength = 0.58f;
            [Range(15f, 100f), InspectorName("Shadow Distance（阴影距离）")] public float shadowDistance = 48f;
            [Range(10f, 100f), InspectorName("Fog Start（雾效起始距离）")] public float fogStart = 42f;
            [Range(50f, 220f), InspectorName("Fog End（雾效结束距离）")] public float fogEnd = 125f;
        }

        [Serializable]
        public sealed class PortraitCameraFraming
        {
            [Range(45f, 75f), InspectorName("Field Of View（视场角）")] public float fieldOfView = 50f;
            [Range(3f, 12f), InspectorName("Camera Height（镜头高度）")] public float cameraHeight = 4.4f;
            [Range(5f, 20f), InspectorName("Camera Back Distance（镜头后置距离）")] public float cameraBackDistance = 7f;
            [Range(0f, 18f), InspectorName("Camera Look Ahead（镜头前视距离）")] public float cameraLookAhead = 2.8f;
            [Range(0f, 8f), InspectorName("Max Speed FOV Bonus（最高速视场角加成）")] public float maxSpeedFovBonus = 4f;
        }

        [Serializable]
        public sealed class TargetShapeSettings
        {
            [Tooltip("Placeholder silhouette dimensions for all configured target levels.")]
            [InspectorName("Tier Shapes（等级目标外形）")] public Vector3[] tierShapes =
            {
                new Vector3(0.82f, 1.55f, 0.78f), new Vector3(1.05f, 1.35f, 0.76f),
                new Vector3(1.18f, 1.5f, 0.8f), new Vector3(1.28f, 1.7f, 0.82f),
                new Vector3(1.35f, 1.82f, 0.84f), new Vector3(1.42f, 1.92f, 0.86f),
                new Vector3(1.5f, 2.02f, 0.88f), new Vector3(1.58f, 2.12f, 0.9f),
                new Vector3(1.68f, 2.22f, 0.94f), new Vector3(1.8f, 2.35f, 1f)
            };

            public Vector3 Get(int tier)
            {
                if (tierShapes == null || tierShapes.Length != PlayerSpeedSettings.RequiredLevelCount)
                    tierShapes = new TargetShapeSettings().tierShapes;
                return tierShapes[Mathf.Clamp(tier, 1, tierShapes.Length) - 1];
            }
        }

        private enum EncounterType
        {
            Target,
            Elixir,
            Wall
        }

        private sealed class Encounter
        {
            public GameObject root;
            public EncounterType type;
            public int tier;
            public bool consumed;
            public int exclusivePickupGroupId;
            public bool anticipated;
            public BreakableWallVisual wall;
            public ObstacleController obstacle;
            public NumberCombatTarget numberTarget;
            public EnemyVisibilityController visibility;
            public SoldierKnockbackEffect soldierKnockback;
            public ElixirPickup elixir;
            public float temporaryBoostAmount;
            public float temporaryBoostHoldDuration;
            public bool temporaryBoostGrantsInvulnerability;
            public float wallCenterX;
            public float wallHalfWidth;
            public BulletTimeSettings bulletTimeSettings;
            public bool bulletTimeTriggered;
            public bool completesTutorial;
            public bool hasPreviousDistance;
            public float previousDistance;
        }

        private enum MainRunContentType
        {
            Empty,
            Soldiers,
            StoneWall,
            Elite,
            SmallPotion,
            LargePotion
        }

        private sealed class MainRunLaneContent
        {
            public readonly MainRunContentType type;
            public readonly int count;

            public MainRunLaneContent(MainRunContentType contentType, int soldierCount = 0)
            {
                type = contentType;
                count = soldierCount;
            }
        }

        private sealed class MainRunZone
        {
            public readonly MainRunLaneContent left;
            public readonly MainRunLaneContent center;
            public readonly MainRunLaneContent right;

            public MainRunZone(MainRunLaneContent leftContent,
                MainRunLaneContent centerContent, MainRunLaneContent rightContent)
            {
                left = leftContent ?? EmptyLane();
                center = centerContent ?? EmptyLane();
                right = rightContent ?? EmptyLane();
            }
        }

        private static MainRunLaneContent EmptyLane()
        {
            return new MainRunLaneContent(MainRunContentType.Empty);
        }

        private static MainRunLaneContent SoldiersLane(int count)
        {
            return new MainRunLaneContent(MainRunContentType.Soldiers, Mathf.Max(1, count));
        }

        private static MainRunLaneContent StoneWallLane()
        {
            return new MainRunLaneContent(MainRunContentType.StoneWall);
        }

        private static MainRunLaneContent EliteLane()
        {
            return new MainRunLaneContent(MainRunContentType.Elite);
        }

        private static MainRunLaneContent SmallPotionLane()
        {
            return new MainRunLaneContent(MainRunContentType.SmallPotion);
        }

        private static MainRunLaneContent LargePotionLane()
        {
            return new MainRunLaneContent(MainRunContentType.LargePotion);
        }

        private static MainRunZone CreateMainRunZone(MainRunLaneContent left,
            MainRunLaneContent center, MainRunLaneContent right)
        {
            return new MainRunZone(left, center, right);
        }

        [Header("All gameplay values are configurable（所有玩法数值均可配置）")]
        [SerializeField, InspectorName("Tuning（玩法调校）")] private Tuning tuning = new Tuning();

        [Header("Reusable gameplay prefabs（可复用玩法预制体）")]
        [SerializeField, InspectorName("Prefab（可复用预制体）")] private PrefabModules prefab = new PrefabModules();

        [Header("Boss reward run（Boss奖励关）")]
        [SerializeField, InspectorName("Boss Reward Run（Boss奖励关设置）")]
        private BossRewardRunSettings rewardRun = new BossRewardRunSettings();

        [SerializeField, InspectorName("Gameplay Combo（玩法连击）")]
        private GameplayComboSettings gameplayCombo = new GameplayComboSettings();

        [Header("Number combat system（数值对抗系统）")]
        [SerializeField, InspectorName("Number Combat（数值对抗）")]
        private NumberCombatSettings numberCombat = new NumberCombatSettings();

        [Header("Authoritative player speed（权威玩家速度）")]
        [SerializeField, InspectorName("Player Speed（玩家速度设置）")] private PlayerSpeedSettings playerSpeed = new PlayerSpeedSettings();

        [Header("Flow entry for future intro camera sequence（未来开场镜头流程入口）")]
        [SerializeField, InspectorName("Auto Begin Gameplay（自动开始玩法）")] private bool autoBeginGameplay = true;

        [Header("Visual environment only - does not affect gameplay（仅视觉环境，不影响玩法）")]
        [SerializeField, InspectorName("Environment（环境设置）")] private EnvironmentTuning environment = new EnvironmentTuning();

        [Header("Portrait camera framing - 9:16 reference（竖屏镜头构图 - 9:16 参考）")]
        [SerializeField, InspectorName("Portrait Camera（竖屏镜头设置）")] private PortraitCameraFraming portraitCamera = new PortraitCameraFraming();

        [Header("Placeholder target silhouettes - replace visuals without changing rules（目标占位外形，不改变规则）")]
        [SerializeField, InspectorName("Target Shapes（目标外形）")] private TargetShapeSettings targetShapes = new TargetShapeSettings();

        [Header("Unified speed visual profile - does not affect gameplay values（统一速度视觉配置，不影响玩法数值）")]
        [SerializeField, InspectorName("Speed Visual Profile（速度视觉配置）")] private SpeedVisualProfile speedVisualProfile;
        [SerializeField, InspectorName("Visual Performance（视觉性能设置）")] private VisualPerformanceSettings visualPerformance = new VisualPerformanceSettings();

        [Header("Speed bar collision hints（速度条碰撞提示）")]
        [SerializeField, InspectorName("Hint Frame（提示框）")] private Sprite speedBarHintFrame;
        [SerializeField, InspectorName("Level 1 Soldier Icon（1级士兵图标）")] private Sprite speedBarSoldierHintIcon;
        [SerializeField, InspectorName("Stone Wall Icon（石墙图标）")] private Sprite speedBarStoneWallHintIcon;
        [SerializeField, InspectorName("Locked Stone Wall Icon（未解锁石墙图标）")] private Sprite speedBarStoneWallLockedHintIcon;

        [Header("External speed VFX（外部速度特效）")]
        [SerializeField, InspectorName("Running Wind Trail Prefab（跑步风痕预制体）")] private GameObject runningWindTrailPrefab;
        [SerializeField, InspectorName("Acceleration Aura Prefab（加速法阵预制体）")] private GameObject accelerationAuraPrefab;
        [SerializeField, InspectorName("Invulnerability Magic Circle Texture（无敌法阵贴图）")] private Texture2D upgradeMagicCircleTexture;

        [Header("Elixir presentation（药剂表现）")]
        [SerializeField, InspectorName("Elixir Presentation（药剂表现设置）")] private ElixirPresentationSettings elixirPresentation = new ElixirPresentationSettings();

        [Header("Normal impact presentation（普通冲击表现）")]
        [SerializeField, InspectorName("Impact Presentation（冲击表现设置）")] private ImpactPresentationSettings impactPresentation = new ImpactPresentationSettings();

        [Header("Whole soldier knockback presentation（完整士兵撞飞表现）")]
        [SerializeField, InspectorName("Soldier Knockback Presentation（士兵撞飞表现设置）")] private SoldierKnockbackSettings soldierKnockbackPresentation = new SoldierKnockbackSettings();

        [Header("Tutorial wall presentation（教学墙体表现）")]
        [SerializeField, InspectorName("Wall Break Presentation（墙体破碎表现设置）")] private WallBreakSettings wallBreakPresentation = new WallBreakSettings();
        [SerializeField, InspectorName("Bullet Time Warning Arrow（子弹时间警告右箭头）")]
        private Texture2D tutorialBulletWarningArrow;

        [Header("Authoritative level-up feedback（权威升级反馈）")]
        [SerializeField, InspectorName("Speed Level Feedback Config（速度等级反馈配置）")] private SpeedLevelFeedbackConfig speedLevelFeedbackConfig;

        [Header("Boss battle presentation（Boss 战斗表现）")]
        [SerializeField, InspectorName("Boss Clash Presentation（Boss 对抗表现设置）")] private BossClashSettings bossClashPresentation = new BossClashSettings();
        [SerializeField, InspectorName("Boss Rapid Tap Hand（Boss 连点手指提示）")] private Sprite bossTapHintSprite;

        [Header("End card presentation（结算页面表现）")]
        [SerializeField, InspectorName("Game Logo（游戏Logo）")] private Texture2D endCardGameLogo;
        [SerializeField, InspectorName("Victory Logo（胜利Logo）")] private Texture2D endCardVictoryLogo;
        [SerializeField, InspectorName("Play Now Image（立即游玩图片）")] private Texture2D endCardPlayNowImage;
        [SerializeField, InspectorName("Tap Hand（点击手指）")] private Texture2D endCardTapHand;
        [SerializeField, InspectorName("Store URL（商店链接）")] private string endCardStoreUrl = string.Empty;

        [Header("Audio clips and mobile haptics（音频片段与移动触觉）")]
        [SerializeField, InspectorName("Audio Presentation（音频表现设置）")] private AudioFeedbackSettings audioPresentation = new AudioFeedbackSettings();

        [Header("Replace visuals here - gameplay colliders stay on parent objects（在此替换视觉，玩法碰撞体保留在父对象）")]
        [SerializeField, InspectorName("Player Visual Prefab（玩家视觉预制体）")] private GameObject playerVisualPrefab;
        [SerializeField, InspectorName("Player Animator（玩家动画控制器）")] private RuntimeAnimatorController playerAnimator;
        [SerializeField, InspectorName("Tier 1 Soldier Prefab（等级 1 士兵预制体）")] private GameObject tier1SoldierPrefab;
        [SerializeField, InspectorName("Tier 4 Soldier Prefab（等级 4 士兵预制体）")] private GameObject tier4SoldierPrefab;
        [SerializeField, Min(0.1f), InspectorName("Tier 1 Target Height（等级 1 目标高度）")] private float tier1TargetHeight = 1.6f;
        [SerializeField, Min(0.1f), InspectorName("Tier 4 Target Height（等级 4 目标高度）")] private float tier4TargetHeight = 2.2f;
        [SerializeField, InspectorName("Boss Visual Prefab（Boss 视觉预制体）")] private GameObject bossVisualPrefab;
        [SerializeField, InspectorName("Boss Animator（Boss 动画控制器）")] private RuntimeAnimatorController bossAnimator;
        [SerializeField, InspectorName("Princess Visual Prefab（公主视觉预制体）")] private GameObject princessVisualPrefab;
        [SerializeField, InspectorName("Princess Animator（公主动画控制器）")] private RuntimeAnimatorController princessAnimator;

        [Header("Environment visual assets（环境视觉资源）")]
        [SerializeField, InspectorName("Road Surface Material（路面材质）")] private Material roadSurfaceMaterial;
        [SerializeField, InspectorName("Road Border Straight Model（路缘直线模型）")] private GameObject roadBorderStraightPrefab;
        [SerializeField, InspectorName("Road Border Pedestal Model（路缘基座模型）")] private GameObject roadBorderPedestalPrefab;
        [SerializeField, InspectorName("Road Border Material（路缘材质）")] private Material roadBorderMaterial;
        [SerializeField, InspectorName("Road Border Flame Prefab（路缘火焰预制体）")] private GameObject roadBorderFlamePrefab;

        [Header("Development-only speed diagnostics（仅开发用速度诊断）")]
        [SerializeField, InspectorName("Show Speed Debug Overlay（显示速度调试覆盖层）")] private bool showSpeedDebugOverlay;
        [SerializeField, Min(20f), InspectorName("Debug Test Segment Length（调试测试区段长度）")] private float debugTestSegmentLength = 100f;

        private readonly List<Encounter> encounters = new List<Encounter>();
        private readonly List<GameObject> rewardStageWallRoots = new List<GameObject>();
        private readonly HashSet<int> targetVisualFallbackWarnings = new HashSet<int>();
        private Transform worldRoot;
        private Transform runner;
        private Transform runnerVisual;
        private Transform boss;
        private Transform bossVisual;
        private Animator bossRuntimeAnimator;
        private Animator princessRuntimeAnimator;
        private Transform princess;
        private Transform cage;
        private Camera gameCamera;
        private AudioFeedbackController audioFeedback;
        private SpeedVisualFeedback speedFeedback;
        private ImpactEffectPool effectPool;
        private BossClashVisual bossClashVisual;
        private BossTapPromptView bossTapPromptView;
        private SpeedBarView speedBarView;
        private ComboManager comboManager;
        private ComboUIController comboUIController;
        private NumberCombatSystem numberCombatSystem;
        private NumberCombatTarget bossNumberTarget;
        private VisualTimeScaleController visualTimeScale;
        private PlayerSpeedController speedController;
        private PlayerForwardMotionController forwardMotion;
        private Animator runnerAnimator;
        private PlayerSpriteVisualController runnerSpriteVisual;
        private RunFlowController flowController;
        private float targetX;
        private float elapsed;
        private bool debugSegmentRecording;
        private float debugSegmentStartZ;
        private float debugSegmentElapsed;
        private float debugLastSegmentTime;
        private float shakeUntil;
        private float shakeStrength;
        private Vector3 directionalShake;
        private float flashAlpha;
        private float penaltyEdgeIntensity;
        private float smallPotionGoldOverlayAlpha;
        private float fovPunchOffset;
        private float baseFov = 58f;
        private bool dragging;
        private float dragStartNormalizedX;
        private float dragOriginTargetX;
        private float dragPixelsPerWorldUnit;
        private int activeTouchId = -1;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private bool bossSequence;
        private bool bossTapInputActive;
        private bool bossSupportsAttackAnimation;
        private int bossTapCount;
        private bool bossDefeated;
        private bool rewardStageActive;
        private bool rewardStageBuilt;
        private bool rewardStageCompleted;
        private float rewardStageEndZ;
        private bool ending;
        private BossClashPhase currentBossPhase;
        private string callout = string.Empty;
        private float calloutUntil;
        private Texture2D whiteTexture;
        private Texture2D penaltyEdgeTexture;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smashTitleStyle;
        private GUIStyle smashSubtitleStyle;
        private GUIStyle powerUpStyle;
        private GUIStyle powerUpShadowStyle;
        private GUIStyle tutorialBulletWarningStyle;
        private GUIStyle tutorialBulletWarningShadowStyle;
        private float smashEffectStart;
        private float smashEffectUntil;
        private float powerUpNoticeStart;
        private float powerUpNoticeUntil;
        private bool tutorialBulletTimeWarningActive;
        private LineRenderer upgradeRing;
        private SpriteRenderer upgradeMagicCircle;
        private SpriteRenderer upgradeMagicCircleGlow;
        private Sprite upgradeMagicCircleSprite;
        private float lastImpactTime;
        private float lastNormalShakeTime;
        private int comboPitchIndex;
        private int impactSequence;
        private bool lowSpeedWarningShown;
        private bool gameplayStarted;
        private float runnerAnimationTime;
        private Texture2D buttonNormalTexture;
        private Texture2D buttonActiveTexture;
        private bool ownsSpeedVisualProfile;
        private bool ownsSpeedLevelFeedbackConfig;
        private float naturalSpeedLossProtectedUntil;
        private int naturalSpeedLossProtectedLevel;
        private int smallPotionInvulnerabilityCount;
        private float tutorialElixirVisualUntil;

        public PlayerSpeedController SpeedController => speedController;
        private bool SmallPotionInvulnerabilityActive => smallPotionInvulnerabilityCount > 0;
        public RunFlowState CurrentFlowState => flowController != null ? flowController.CurrentState : RunFlowState.Intro;
        public float TargetForwardSpeed => forwardMotion != null ? forwardMotion.TargetForwardSpeed : 0f;
        public float CurrentForwardSpeed => forwardMotion != null ? forwardMotion.CurrentForwardSpeed : 0f;
        public float CurrentAnimationSpeed => runnerAnimator != null ? runnerAnimator.speed : 0f;
        public int CurrentCombo => comboManager != null ? comboManager.GetCombo() : 0;
        private int CurrentTier => speedController != null ? speedController.GetCurrentLevel() : 1;
        private bool FormalStarted => flowController != null && flowController.CurrentState == RunFlowState.MainRun;
        private bool RewardRunEnabled => rewardRun != null && rewardRun.enabled;
        private float RewardRunStartZ => tuning != null
            ? tuning.bossDistance + (RewardRunEnabled ? Mathf.Max(0f, rewardRun.startOffsetAfterBoss) : 0f)
            : 0f;
        private float CourseEndZ => RewardRunEnabled
            ? RewardRunStartZ + Mathf.Max(20f, rewardRun.length)
            : tuning != null ? tuning.bossDistance : 0f;
        private float CourseDistanceScale => tuning != null ? Mathf.Max(0.1f, tuning.bossDistance / 1300f) : 1f;
        private float OpeningElixirZ => tuning.openingElixirTime * playerSpeed.forwardSpeeds[0];

        private void OnValidate()
        {
            if (tuning == null) tuning = new Tuning();
            if (playerSpeed == null) playerSpeed = new PlayerSpeedSettings();
            tuning.UpgradeGameplayLevelConfiguration(playerSpeed, prefab);
            if (gameplayCombo == null) gameplayCombo = new GameplayComboSettings();
            gameplayCombo.UpgradeLegacyDefaults();
            if (numberCombat == null) numberCombat = new NumberCombatSettings();
            if (rewardRun == null) rewardRun = new BossRewardRunSettings();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = false;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            if (tuning == null) tuning = new Tuning();
            if (playerSpeed == null) playerSpeed = new PlayerSpeedSettings();
            tuning.UpgradeGameplayLevelConfiguration(playerSpeed, prefab);
            if (!ValidatePrefabModules())
            {
                enabled = false;
                return;
            }
            if (rewardRun == null) rewardRun = new BossRewardRunSettings();
            ApplyDocumentCourseLength();
            if (speedVisualProfile == null)
            {
                speedVisualProfile = ScriptableObject.CreateInstance<SpeedVisualProfile>();
                speedVisualProfile.name = "RuntimeSpeedVisualProfileFallback";
                ownsSpeedVisualProfile = true;
            }
            flowController = GetComponent<RunFlowController>();
            if (flowController == null) flowController = gameObject.AddComponent<RunFlowController>();
            flowController.ResetToIntro();
            BuildWorld();
            BuildLevel();
            targetX = 0f;
            callout = string.Empty;
            calloutUntil = 0f;
            powerUpNoticeStart = 0f;
            powerUpNoticeUntil = 0f;
            if (autoBeginGameplay)
            {
                BeginGameplay();
            }
            else
            {
                BeginIntro();
            }
        }

        public void BeginIntro()
        {
            gameplayStarted = false;
            bossDefeated = false;
            rewardStageActive = false;
            rewardStageBuilt = false;
            rewardStageCompleted = false;
            rewardStageEndZ = 0f;
            rewardStageWallRoots.Clear();
            naturalSpeedLossProtectedUntil = 0f;
            naturalSpeedLossProtectedLevel = 0;
            smallPotionInvulnerabilityCount = 0;
            tutorialElixirVisualUntil = 0f;
            smallPotionGoldOverlayAlpha = 0f;
            flowController?.ResetToIntro();
            comboManager?.ResetCombo();
            numberCombatSystem?.RefreshPlayerLevel();
            forwardMotion?.Tick(0f, false);
            CancelDrag();
            targetX = 0f;
            callout = string.Empty;
            calloutUntil = 0f;
            powerUpNoticeStart = 0f;
            powerUpNoticeUntil = 0f;
            penaltyEdgeIntensity = 0f;
            tutorialBulletTimeWarningActive = false;
            runnerSpriteVisual?.ResetVisualState();
            ResetBossTapInteraction(true);
            SetGameplayHudVisible(true);
        }

        public void BeginGameplay()
        {
            if (gameplayStarted)
            {
                return;
            }

            gameplayStarted = true;
            elapsed = 0f;
            bossDefeated = false;
            rewardStageActive = false;
            rewardStageBuilt = false;
            rewardStageCompleted = false;
            rewardStageEndZ = 0f;
            rewardStageWallRoots.Clear();
            smallPotionInvulnerabilityCount = 0;
            tutorialElixirVisualUntil = 0f;
            smallPotionGoldOverlayAlpha = 0f;
            powerUpNoticeStart = 0f;
            powerUpNoticeUntil = 0f;
            comboManager?.ResetCombo();
            numberCombatSystem?.RefreshPlayerLevel();
            speedController.SetLevel(playerSpeed.startingLevel, SpeedChangeReason.InitialSetup, this);
            forwardMotion?.SnapToTarget();
            targetX = 0f;
            flowController.StartTutorial();
            CancelDrag();
            penaltyEdgeIntensity = 0f;
            tutorialBulletTimeWarningActive = false;
            runnerSpriteVisual?.ResetVisualState();
            ResetBossTapInteraction(true);
            SetGameplayHudVisible(true);
        }

        public void StartTutorial()
        {
            BeginGameplay();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleSpeedDebugInput();
#endif
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 3.8f);
            float penaltyFadeDuration = impactPresentation != null
                ? Mathf.Max(0.01f, impactPresentation.penaltyEdgeFadeDuration)
                : 0.65f;
            penaltyEdgeIntensity = Mathf.MoveTowards(penaltyEdgeIntensity, 0f,
                Time.unscaledDeltaTime / penaltyFadeDuration);
            UpdateSmallPotionInvulnerabilityPresentation();
            UpdateTutorialBulletTimeWarningState();
            bool movementActive = gameplayStarted && !ending && flowController != null && flowController.IsGameplayActive && Time.timeScale > 0f;
            if (!movementActive)
            {
                forwardMotion?.Tick(0f, false);
                UpdateRunnerFeedback(0f, false);
                return;
            }

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.B) && runner != null)
            {
                speedController.SetLevel(speedController.MaxLevel, SpeedChangeReason.DebugCommand, this);
                runner.position = new Vector3(0f, runner.position.y, tuning.bossDistance - 12f * CourseDistanceScale);
                targetX = 0f;
            }
#endif

            elapsed += Time.deltaTime;
            UpdateDebugSegmentTimer();
            ReadInput();

            if (!bossSequence)
            {
                MoveRunner();
                ProcessEncounters();
                RecoverAtMinimumSpeedAfterTutorial();
                if (rewardStageActive)
                    CheckRewardStageCompletion();
                else
                    CheckBossEntry();
            }

        }

        private void LateUpdate()
        {
            if (gameCamera == null || runner == null)
            {
                return;
            }

            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                baseFov = GetConfiguredBaseFov();
            }

            bool portrait = Screen.height >= Screen.width;
            float cameraHeight = portrait ? portraitCamera.cameraHeight : environment.cameraHeight;
            float cameraBackDistance = portrait ? portraitCamera.cameraBackDistance : environment.cameraBackDistance;
            float cameraLookAhead = portrait ? portraitCamera.cameraLookAhead : environment.cameraLookAhead;
            float cameraTargetX = environment.cameraFollowsPlayerLaterally ? runner.position.x : 0f;
            float actualNormalized = forwardMotion != null ? forwardMotion.NormalizedActualSpeed : 0f;
            float dynamicBackDistance = cameraBackDistance + environment.highSpeedPullback * actualNormalized;
            Vector3 desired = environment.cameraFollowsPlayer
                ? new Vector3(cameraTargetX, runner.position.y + cameraHeight, runner.position.z - dynamicBackDistance)
                : new Vector3(0f, cameraHeight, -cameraBackDistance);
            if (bossSequence && boss != null)
            {
                Vector3 clashCenter = Vector3.Lerp(runner.position, boss.position, 0.5f);
                desired = clashCenter + new Vector3(0f, cameraHeight * 0.9f, -cameraBackDistance * 0.72f);
            }

            if (Time.unscaledTime < shakeUntil)
            {
                desired += UnityEngine.Random.insideUnitSphere * shakeStrength;
                desired += directionalShake * (Mathf.Sin(Time.unscaledTime * 74f) * shakeStrength);
            }

            if (speedFeedback != null && speedFeedback.CurrentAmbientShake > 0.0001f)
            {
                float lowFrequency = Mathf.Sin(Time.unscaledTime * 5.2f) * speedFeedback.CurrentAmbientShake;
                desired += new Vector3(lowFrequency, lowFrequency * 0.35f, 0f);
            }

            gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, desired, 1f - Mathf.Exp(-environment.cameraFollowSharpness * Time.unscaledDeltaTime));
            Vector3 lookTarget = bossSequence && boss != null
                ? Vector3.Lerp(runner.position, boss.position, 0.55f) + Vector3.up * 1.1f
                : environment.cameraFollowsPlayer
                    ? new Vector3(cameraTargetX, runner.position.y + 1f, runner.position.z + cameraLookAhead + environment.highSpeedLookAheadBonus * actualNormalized)
                    : new Vector3(0f, 1f, cameraLookAhead);
            gameCamera.transform.rotation = Quaternion.Slerp(
                gameCamera.transform.rotation,
                Quaternion.LookRotation(lookTarget - gameCamera.transform.position),
                1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            fovPunchOffset = Mathf.MoveTowards(fovPunchOffset, 0f, Time.unscaledDeltaTime * 18f);
            float speedFov = speedFeedback != null ? speedFeedback.CurrentFovBonus : 0f;
            float targetFov = baseFov + speedFov + fovPunchOffset;
            if (portrait)
                targetFov = Mathf.Min(targetFov, baseFov + portraitCamera.maxSpeedFovBonus);
            float fovResponse = 1f / Mathf.Max(0.05f, speedVisualProfile.tierTransitionDuration);
            gameCamera.fieldOfView = Mathf.Lerp(gameCamera.fieldOfView, targetFov, 1f - Mathf.Exp(-fovResponse * Time.unscaledDeltaTime));
        }

        private void ReadInput()
        {
            if (bossSequence || flowController == null || !flowController.IsGameplayActive)
            {
                CancelDrag();
                if (flowController == null || flowController.CurrentState == RunFlowState.Intro)
                {
                    targetX = 0f;
                }
                return;
            }

            if (Input.touchCount > 0)
            {
                if (activeTouchId < 0)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        Touch candidate = Input.GetTouch(i);
                        if (candidate.phase != TouchPhase.Began) continue;
                        bool overUi = tuning.blockInputOverUi && EventSystem.current != null
                            && EventSystem.current.IsPointerOverGameObject(candidate.fingerId);
                        if (overUi) continue;
                        BeginDrag(candidate.position.x / Mathf.Max(1f, Screen.width), candidate.fingerId);
                        break;
                    }
                }

                bool activeTouchFound = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.fingerId != activeTouchId) continue;
                    activeTouchFound = true;
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        CancelDrag();
                    else if (dragging)
                        ApplyNormalizedDrag(touch.position.x / Mathf.Max(1f, Screen.width));
                    break;
                }
                if (activeTouchId >= 0 && !activeTouchFound) CancelDrag();
            }
            else
            {
                if (activeTouchId >= 0) CancelDrag();
                if (Input.GetMouseButtonDown(0))
                {
                    bool overUi = tuning.blockInputOverUi && EventSystem.current != null
                        && EventSystem.current.IsPointerOverGameObject();
                    if (!overUi)
                    {
                        BeginDrag(Input.mousePosition.x / Mathf.Max(1f, Screen.width), -1);
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    CancelDrag();
                }

                if (dragging)
                    ApplyNormalizedDrag(Input.mousePosition.x / Mathf.Max(1f, Screen.width));
            }

            targetX += Input.GetAxisRaw("Horizontal") * tuning.lateralSpeed * Time.unscaledDeltaTime;
            targetX = Mathf.Clamp(targetX, -tuning.laneHalfWidth, tuning.laneHalfWidth);
        }

        private void BeginDrag(float normalizedX, int touchId)
        {
            dragging = true;
            activeTouchId = touchId;
            dragStartNormalizedX = normalizedX;
            dragOriginTargetX = runner != null ? runner.position.x : targetX;
            targetX = dragOriginTargetX;
            dragPixelsPerWorldUnit = GetRunnerPixelsPerWorldUnit();
        }

        private void ApplyNormalizedDrag(float normalizedX)
        {
            float delta = normalizedX - dragStartNormalizedX;
            delta = Mathf.Sign(delta) * Mathf.Max(0f, Mathf.Abs(delta) - tuning.dragDeadZone);
            if (tuning.invertDragInput) delta = -delta;
            float pixelDelta = delta * Mathf.Max(1f, Screen.width);
            float pixelsPerWorldUnit = dragPixelsPerWorldUnit > 0f
                ? dragPixelsPerWorldUnit
                : GetRunnerPixelsPerWorldUnit();
            float unclampedTarget = dragOriginTargetX
                + pixelDelta / pixelsPerWorldUnit * tuning.dragFollowRatio;
            targetX = Mathf.Clamp(unclampedTarget, -tuning.laneHalfWidth, tuning.laneHalfWidth);

            // Rebase at the road edge so reversing direction responds immediately after an over-drag.
            if (!Mathf.Approximately(targetX, unclampedTarget))
            {
                dragStartNormalizedX = normalizedX;
                dragOriginTargetX = targetX;
            }
        }

        private float GetRunnerPixelsPerWorldUnit()
        {
            if (gameCamera == null || runner == null)
                return Mathf.Max(1f, Screen.width / (tuning.laneHalfWidth * 2f));

            Vector3 center = gameCamera.WorldToScreenPoint(runner.position);
            Vector3 oneUnitRight = gameCamera.WorldToScreenPoint(runner.position + Vector3.right);
            return Mathf.Max(1f, Mathf.Abs(oneUnitRight.x - center.x));
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            CancelDrag();
            AudioListener.pause = !hasFocus;
            if (hasFocus) visualTimeScale?.Restore();
        }

        private void OnApplicationPause(bool paused)
        {
            CancelDrag();
            AudioListener.pause = paused;
            if (!paused) visualTimeScale?.Restore();
        }

        private void CancelDrag()
        {
            dragging = false;
            activeTouchId = -1;
            dragPixelsPerWorldUnit = 0f;
        }

        private void MoveRunner()
        {
            if (rewardStageActive && speedController != null
                && speedController.GetCurrentLevel() < speedController.MaxLevel)
            {
                // The reward run is a fixed max-level sequence, independent of collision or decay tuning.
                speedController.SetLevel(speedController.MaxLevel, SpeedChangeReason.BossEvent, this);
                forwardMotion?.SnapToTarget();
            }

            float worldDeltaTime = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.GetWorldDeltaTime()
                : Time.deltaTime;
            float forwardSpeed = forwardMotion != null
                ? forwardMotion.Tick(worldDeltaTime, true)
                : speedController.GetForwardSpeed();
            float previousX = runner.position.x;
            float horizontalDeltaTime = Time.unscaledDeltaTime;
            float x;
            if (dragging)
            {
                float error = Mathf.Abs(targetX - previousX);
                float catchUp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f,
                    Mathf.Max(0.2f, tuning.laneHalfWidth * 0.4f), error));
                float baseSharpness = Mathf.Max(1f, tuning.dragFollowSharpness);
                float sharpness = Mathf.Lerp(baseSharpness,
                    Mathf.Min(120f, baseSharpness * 2.2f), catchUp);
                float response = 1f - Mathf.Exp(-sharpness * horizontalDeltaTime);
                x = Mathf.Lerp(previousX, targetX, response);
                if (Mathf.Abs(x - targetX) < 0.0001f) x = targetX;
            }
            else
            {
                x = Mathf.MoveTowards(previousX, targetX,
                    tuning.lateralSpeed * horizontalDeltaTime);
            }
            runner.position = new Vector3(x, runner.position.y,
                runner.position.z + forwardSpeed * worldDeltaTime);
            float lateralInput = horizontalDeltaTime > 0f
                ? (x - previousX) / (tuning.lateralSpeed * horizontalDeltaTime)
                : 0f;
            runnerSpriteVisual?.SetHorizontalInput(lateralInput);

            if (FormalStarted && !bossSequence && !rewardStageActive
                && !SmallPotionInvulnerabilityActive && tuning.forwardSpeedLossEnabled)
            {
                float minimumRetainedSpeed = tuning.minimumSpeedAfterLoss;
                // After a level-up, natural decay may reach the new tier's floor for 3s,
                // while collision penalties continue to use their normal direct path.
                if (Time.unscaledTime < naturalSpeedLossProtectedUntil
                    && naturalSpeedLossProtectedLevel > 1
                    && speedController.GetCurrentLevel() >= naturalSpeedLossProtectedLevel)
                {
                    minimumRetainedSpeed = Mathf.Max(minimumRetainedSpeed,
                        speedController.GetLevelStartSpeed(naturalSpeedLossProtectedLevel));
                }

                speedController.ApplyContinuousSpeedLoss(worldDeltaTime,
                    GetSpeedLossPerSecond(runner.position.z),
                    minimumRetainedSpeed, this);
            }

            UpdateRunnerFeedback(forwardSpeed, true);
        }

        private int GetSpeedLossSectionIndex(float worldZ)
        {
            if (worldZ < FirstSpeedLossSectionEndZ) return 1;
            return worldZ < SecondSpeedLossSectionEndZ ? 2 : 3;
        }

        private float GetSpeedLossPerSecond(float worldZ)
        {
            int section = GetSpeedLossSectionIndex(worldZ);
            if (section == 1) return Mathf.Max(0f, tuning.firstSectionSpeedLossPerSecond);
            if (section == 2) return Mathf.Max(0f, tuning.secondSectionSpeedLossPerSecond);
            return Mathf.Max(0f, tuning.thirdSectionSpeedLossPerSecond);
        }

        private void RecoverAtMinimumSpeedAfterTutorial()
        {
            if (!FormalStarted || rewardStageActive || speedController == null) return;

            float minimumSpeed = speedController.Settings != null
                ? speedController.Settings.minimumSpeed
                : speedController.GetLevelStartSpeed(1);
            if (speedController.CurrentSpeed > minimumSpeed + 0.001f) return;

            int recoveryLevel = Mathf.Min(MainRunLowSpeedRecoveryLevel, speedController.MaxLevel);
            if (recoveryLevel <= 1) return;

            speedController.SetLevel(recoveryLevel, SpeedChangeReason.SpecialReward, this);
            forwardMotion?.SnapToTarget();
        }

        private float GetForwardSpeed()
        {
            return forwardMotion != null ? forwardMotion.CurrentForwardSpeed : speedController != null ? speedController.GetForwardSpeed() : playerSpeed.forwardSpeeds[0];
        }

        private void UpdateRunnerFeedback(float forwardSpeed, bool movementActive)
        {
            float actualNormalized = forwardMotion != null ? forwardMotion.NormalizedActualSpeed : 0f;
            float worldDeltaTime = BulletTimeManager.Instance != null
                ? BulletTimeManager.Instance.GetWorldDeltaTime()
                : Time.deltaTime;
            if (movementActive) runnerAnimationTime += worldDeltaTime;
            if (runnerVisual != null)
            {
                float pulse = 1f + Mathf.Sin(runnerAnimationTime * forwardSpeed * 1.8f) * 0.035f;
                float lean = (targetX - runner.position.x) * -7f;
                runnerVisual.localScale = new Vector3(pulse, 1.55f / pulse, pulse);
                float chargeLean = speedFeedback != null ? speedFeedback.CurrentChargeLean : 0f;
                runnerVisual.localRotation = Quaternion.Euler(lean + chargeLean * actualNormalized, 0f, 0f);
            }

            if (runnerSpriteVisual != null)
                runnerSpriteVisual.SetMovement(actualNormalized, movementActive);
            else if (runnerAnimator != null)
            {
                float worldScale = BulletTimeManager.Instance != null
                    ? BulletTimeManager.Instance.WorldTimeScale
                    : 1f;
                runnerAnimator.speed = movementActive
                    ? Mathf.Lerp(0.8f, 1.4f, actualNormalized) * worldScale
                    : 0f;
            }
            if (!movementActive)
                runnerSpriteVisual?.SetHorizontalInput(0f);

            if (speedFeedback != null)
            {
                speedFeedback.UpdateFeedback(speedController.CurrentSpeed, CurrentTier, forwardSpeed,
                    Time.unscaledDeltaTime);
            }
            audioFeedback?.UpdateSpeed(CurrentTier,
                Mathf.InverseLerp(playerSpeed.minimumSpeed, playerSpeed.maximumSpeed, speedController.CurrentSpeed),
                actualNormalized,
                movementActive,
                speedFeedback != null ? speedFeedback.CurrentWindVolume : -1f,
                speedFeedback != null ? speedFeedback.CurrentWindPitch : 1f);
        }

        private void ProcessEncounters()
        {
            int preloadedEnemies = 0;
            int distantVisibleEnemies = 0;
            float currentForwardSpeed = GetForwardSpeed();
            float preloadDistance = Mathf.Clamp(currentForwardSpeed * tuning.enemyPreloadTime,
                tuning.minimumEnemyPreloadDistance, tuning.maximumEnemyPreloadDistance);
            float visibleDistance = Mathf.Clamp(currentForwardSpeed * tuning.enemyVisiblePreviewTime,
                tuning.minimumEnemyVisibleDistance, tuning.maximumEnemyVisibleDistance);
            float activeDistance = Mathf.Clamp(currentForwardSpeed * tuning.enemyActiveTime,
                tuning.minimumEnemyActiveDistance, tuning.maximumEnemyActiveDistance);
            for (int i = 0; i < encounters.Count; i++)
            {
                Encounter encounter = encounters[i];
                if (encounter.consumed || encounter.root == null)
                {
                    continue;
                }

                float dz = encounter.root.transform.position.z - runner.position.z;
                if (encounter.type == EncounterType.Elixir && dz > preloadDistance)
                {
                    if (encounter.root.activeSelf) encounter.root.SetActive(false);
                    continue;
                }
                if (encounter.type == EncounterType.Elixir && !encounter.root.activeSelf)
                    encounter.root.SetActive(true);

                if (encounter.type == EncounterType.Target)
                {
                    if (dz < -tuning.recycleDistance)
                    {
                        encounter.consumed = true;
                        encounter.visibility?.Recycle();
                        continue;
                    }

                    EnemyVisibilityState desiredState;
                    if (dz > preloadDistance)
                    {
                        desiredState = EnemyVisibilityState.Pooled;
                    }
                    else if (dz > visibleDistance)
                    {
                        desiredState = preloadedEnemies < tuning.maxPreloadedEnemies
                            ? EnemyVisibilityState.Preloaded
                            : EnemyVisibilityState.Pooled;
                        if (desiredState == EnemyVisibilityState.Preloaded) preloadedEnemies++;
                    }
                    else if (dz > activeDistance)
                    {
                        desiredState = distantVisibleEnemies < tuning.maxDistantVisibleEnemies
                            ? EnemyVisibilityState.DistantVisible
                            : EnemyVisibilityState.Preloaded;
                        if (desiredState == EnemyVisibilityState.DistantVisible) distantVisibleEnemies++;
                        else preloadedEnemies++;
                    }
                    else
                    {
                        desiredState = EnemyVisibilityState.Active;
                    }

                    encounter.visibility?.SetState(desiredState);
                    if (desiredState != EnemyVisibilityState.Active) continue;
                }

                bool crossedThisFrame = encounter.hasPreviousDistance && encounter.previousDistance > 0f && dz <= 0f;
                encounter.previousDistance = dz;
                encounter.hasPreviousDistance = true;
                if (dz < -tuning.recycleDistance && !crossedThisFrame)
                {
                    encounter.consumed = true;
                    CompleteTutorialIfNeeded(encounter);
                    if (encounter.visibility != null) encounter.visibility.Recycle();
                    else encounter.root.SetActive(false);
                    continue;
                }

                if (encounter.type == EncounterType.Wall)
                {
                    TryTriggerBulletTime(encounter, dz - WallCollisionDistance);
                    bool runnerInsideWall = Mathf.Abs(runner.position.x - encounter.wallCenterX)
                        <= encounter.wallHalfWidth + 0.35f;
                    if (crossedThisFrame) CompleteTutorialIfNeeded(encounter);
                    if (runnerInsideWall && !encounter.anticipated
                        && dz <= GetForwardSpeed() * wallBreakPresentation.anticipationDuration)
                    {
                        encounter.anticipated = true;
                        runnerSpriteVisual?.PlayShieldCharge(Mathf.Max(0.72f,
                            wallBreakPresentation.anticipationDuration + 0.15f));
                        fovPunchOffset = -wallBreakPresentation.fovAnticipation;
                        speedFeedback?.Pulse(0.45f);
                    }
                    if (runnerInsideWall && (Mathf.Abs(dz) < WallCollisionDistance || crossedThisFrame))
                    {
                        encounter.consumed = true;
                        ObstacleResolutionType resolution = ResolveObstacle(encounter);
                        UpdateGameplayCombo(encounter, resolution);
                        StartCoroutine(BreakWallSequence(encounter));
                    }
                    continue;
                }

                const float collectionHalfWidth = 1.15f;
                bool isExclusiveElixir = encounter.type == EncounterType.Elixir
                    && encounter.exclusivePickupGroupId > 0;
                bool insideCollectionWidth = isExclusiveElixir
                    || Mathf.Abs(encounter.root.transform.position.x - runner.position.x) < collectionHalfWidth;
                if ((Mathf.Abs(dz) < 1.25f || crossedThisFrame) && insideCollectionWidth)
                {
                    if (isExclusiveElixir)
                    {
                        CollectExclusiveElixirGroup(encounter.exclusivePickupGroupId);
                    }
                    else
                    {
                        encounter.consumed = true;
                        if (encounter.type == EncounterType.Elixir)
                            CollectElixir(encounter);
                        else
                            HitTarget(encounter);
                    }
                }
            }
        }

        private void CompleteTutorialIfNeeded(Encounter encounter)
        {
            if (encounter == null || !encounter.completesTutorial || flowController == null
                || flowController.CurrentState != RunFlowState.Tutorial)
                return;
            flowController.EnterMainRun();
            speedController?.SetLevel(tuning.tutorialExitSpeedLevel,
                SpeedChangeReason.TutorialElixirExpired, this);
        }

        private void TryTriggerBulletTime(Encounter encounter, float distanceToWall)
        {
            if (encounter.bulletTimeTriggered || encounter.bulletTimeSettings == null
                || !encounter.bulletTimeSettings.enabled || distanceToWall <= 0f)
                return;

            if (distanceToWall > encounter.bulletTimeSettings.triggerDistance) return;
            encounter.bulletTimeTriggered = true;
            BulletTimeManager manager = BulletTimeManager.Instance;
            manager?.StartBulletTime(encounter.bulletTimeSettings);

            if (!encounter.completesTutorial || manager == null || !manager.IsBulletTime()) return;

            tutorialBulletTimeWarningActive = true;
            callout = string.Empty;
            calloutUntil = elapsed;
            smashEffectStart = 0f;
            smashEffectUntil = 0f;
        }

        private void UpdateTutorialBulletTimeWarningState()
        {
            if (!tutorialBulletTimeWarningActive) return;

            BulletTimeManager manager = BulletTimeManager.Instance;
            if (manager == null || !manager.IsBulletTime())
                tutorialBulletTimeWarningActive = false;
        }

        private bool ValidatePrefabModules()
        {
            List<string> missing = new List<string>();
            if (prefab == null)
            {
                missing.Add("Prefab container");
            }
            else
            {
                if (prefab.soldierSections == null || prefab.soldierSections.Length == 0)
                    missing.Add("Soldier Sections");
                if (prefab.stoneWallPrefab == null)
                    missing.Add("Stone Wall Prefab");
                if (prefab.additionalStoneWalls == null || prefab.additionalStoneWalls.Length == 0)
                    missing.Add("Additional Stone Walls");
            }

            if (missing.Count == 0) return true;
            Debug.LogError("[PlayableAd] Prefab configuration is incomplete: "
                + string.Join(", ", missing) + ". Configure the scene's Prefab group before Play Mode.", this);
            return false;
        }

        private IEnumerator BreakWallSequence(Encounter encounter)
        {
            BreakableWallVisual wall = encounter.wall;
            wall?.Break();
            CompleteTutorialIfNeeded(encounter);
            flashAlpha = Mathf.Max(flashAlpha, 0.42f);
            directionalShake = Vector3.up * 0.35f;
            PunchCamera(0.34f, wallBreakPresentation.cameraShake, wallBreakPresentation.fovImpact);
            SpeedTierVisualData wallTier = speedVisualProfile.Get(playerSpeed.tutorialElixirTargetLevel);
            effectPool?.PlayImpact(encounter.root.transform.position + Vector3.up,
                wallTier.secondaryColor, 1.75f, playerSpeed.tutorialElixirTargetLevel);
            effectPool?.PlayImpact(encounter.root.transform.position + new Vector3(0f, 0.2f, -0.4f),
                new Color(0.48f, 0.43f, 0.38f), 1.2f, playerSpeed.tutorialElixirTargetLevel);
            audioFeedback?.PlayWallBreak();
            speedFeedback?.Pulse(1f);

            visualTimeScale.RequestSlowMotion(wallBreakPresentation.slowMotionScale, wallBreakPresentation.slowMotionDuration);
            yield return new WaitForSecondsRealtime(wallBreakPresentation.slowMotionDuration);
        }

        private void CollectElixir(Encounter encounter)
        {
            if (encounter.elixir == null || encounter.elixir.HasCollected) return;
            audioFeedback?.PlayElixirContact();
            if (!encounter.elixir.TryCollect()) return;
            if (encounter.exclusivePickupGroupId == OpeningElixirGroupId)
                BeginTutorialElixirVisuals();
            if (encounter.temporaryBoostAmount > 0f
                || encounter.temporaryBoostGrantsInvulnerability)
            {
                StartCoroutine(TemporaryBoostSequence(encounter));
            }
            else
            {
                StartCoroutine(ElixirUpgradeSequence(encounter, encounter.tier));
            }
        }

        private void BeginTutorialElixirVisuals()
        {
            tutorialElixirVisualUntil = Time.unscaledTime
                + Mathf.Max(0.1f, elixirPresentation.totalDuration);
            powerUpNoticeStart = Time.unscaledTime;
            powerUpNoticeUntil = powerUpNoticeStart + 0.9f;
        }

        private void CollectExclusiveElixirGroup(int groupId)
        {
            Encounter selected = null;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < encounters.Count; i++)
            {
                Encounter candidate = encounters[i];
                if (candidate.consumed || candidate.root == null
                    || candidate.type != EncounterType.Elixir
                    || candidate.elixir == null || candidate.elixir.HasCollected
                    || candidate.exclusivePickupGroupId != groupId)
                    continue;

                float distance = Mathf.Abs(candidate.root.transform.position.x - runner.position.x);
                if (distance >= closestDistance) continue;
                closestDistance = distance;
                selected = candidate;
            }

            if (selected == null) return;
            for (int i = 0; i < encounters.Count; i++)
            {
                Encounter member = encounters[i];
                if (member.type != EncounterType.Elixir
                    || member.exclusivePickupGroupId != groupId)
                    continue;

                member.consumed = true;
                if (member != selected && member.root != null)
                    member.root.SetActive(false);
            }

            selected.root.SetActive(true);
            CollectElixir(selected);
        }

        private IEnumerator ElixirUpgradeSequence(Encounter encounter, int nextTier)
        {
            GameObject pickup = encounter.root;
            ElixirVisual visual = pickup != null ? pickup.GetComponent<ElixirVisual>() : null;
            visual?.BeginConsume();
            flashAlpha = Mathf.Max(flashAlpha, elixirPresentation.pickupFlash);
            fovPunchOffset = -elixirPresentation.cameraPushIn;
            PunchCamera(0.13f, elixirPresentation.cameraShake, 0f);

            visualTimeScale.RequestSlowMotion(elixirPresentation.slowMotionScale, elixirPresentation.slowMotionDuration);
            SpeedTierVisualData elixirTier = speedVisualProfile.Get(nextTier);
            effectPool?.PlayImpact(pickup.transform.position + Vector3.up * 0.35f,
                elixirTier.primaryColor, 0.85f, nextTier);
            Vector3 startScale = pickup != null ? pickup.transform.localScale : Vector3.one;
            float timer = 0f;
            bool upgraded = false;

            while (timer < elixirPresentation.totalDuration)
            {
                float dt = Time.unscaledDeltaTime;
                timer += dt;
                if (pickup != null && timer <= elixirPresentation.collapseDuration)
                {
                    float collapse = 1f - Mathf.Clamp01(timer / elixirPresentation.collapseDuration);
                    pickup.transform.localScale = startScale * collapse;
                }

                if (!upgraded && timer >= elixirPresentation.upgradeMoment)
                {
                    upgraded = true;
                    speedFeedback?.PulseLevelUp();
                    fovPunchOffset = Mathf.Max(fovPunchOffset, elixirPresentation.cameraRebound);
                    callout = nextTier == speedController.MaxLevel ? "MAX SPEED!" : "";
                    calloutUntil = elapsed + 0.6f;
                }

                yield return null;
            }

            if (pickup != null)
            {
                pickup.SetActive(false);
            }
        }

        private void UpdateSmallPotionInvulnerabilityPresentation()
        {
            bool visualInvulnerabilityActive = SmallPotionInvulnerabilityActive
                || Time.unscaledTime < tutorialElixirVisualUntil;
            speedFeedback?.SetInvulnerabilityAuraActive(visualInvulnerabilityActive);
            float fadeDuration = tuning != null
                ? Mathf.Max(0.02f, tuning.smallPotionFilterFadeDuration)
                : 0.1f;
            float targetOverlayAlpha = visualInvulnerabilityActive && tuning != null
                ? Mathf.Clamp(tuning.smallPotionInvulnerabilityGoldFilter, 0f, 0.5f)
                : 0f;
            smallPotionGoldOverlayAlpha = Mathf.MoveTowards(smallPotionGoldOverlayAlpha,
                targetOverlayAlpha, Time.unscaledDeltaTime * 0.5f / fadeDuration);

            if (!visualInvulnerabilityActive)
            {
                if (upgradeRing != null) upgradeRing.enabled = false;
                if (upgradeMagicCircle != null) upgradeMagicCircle.enabled = false;
                if (upgradeMagicCircleGlow != null) upgradeMagicCircleGlow.enabled = false;
                return;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7.5f) * 0.035f;
            float radius = (elixirPresentation != null
                ? Mathf.Max(0.4f, elixirPresentation.energyRingMaxRadius)
                : 1.25f) * pulse;
            Color ringColor = new Color(1f, 0.72f, 0.12f, 0.78f);
            if (upgradeRing != null)
            {
                upgradeRing.enabled = true;
                upgradeRing.transform.localScale = Vector3.one * radius;
                upgradeRing.transform.Rotate(0f, 34f * Time.unscaledDeltaTime, 0f, Space.Self);
                upgradeRing.startColor = ringColor;
                upgradeRing.endColor = ringColor;
            }

            float circleScale = 0.7f * pulse;
            if (upgradeMagicCircle != null)
            {
                upgradeMagicCircle.enabled = true;
                upgradeMagicCircle.transform.localScale = Vector3.one * circleScale;
                upgradeMagicCircle.transform.Rotate(0f, 0f, 38f * Time.unscaledDeltaTime, Space.Self);
                upgradeMagicCircle.color = new Color(1f, 0.76f, 0.16f, 0.82f);
            }
            if (upgradeMagicCircleGlow != null)
            {
                upgradeMagicCircleGlow.enabled = true;
                upgradeMagicCircleGlow.transform.localScale = Vector3.one * circleScale * 1.14f;
                upgradeMagicCircleGlow.transform.Rotate(0f, 0f, -17f * Time.unscaledDeltaTime, Space.Self);
                upgradeMagicCircleGlow.color = new Color(1f, 0.58f, 0.06f, 0.3f);
            }
        }

        private void HitTarget(Encounter encounter)
        {
            runnerSpriteVisual?.PlayShieldCharge(0.72f);
            float speedBeforeImpact = speedController.CurrentSpeed;
            ObstacleResolutionType resolution = ResolveObstacle(encounter);
            UpdateGameplayCombo(encounter, resolution);
            if (resolution == ObstacleResolutionType.Boosted)
            {
                callout = "SMASH\nSPEED UP";
                calloutUntil = elapsed + 0.75f;
                smashEffectStart = Time.unscaledTime;
                smashEffectUntil = smashEffectStart + 0.28f;
                Impact(encounter, CollisionOutcome.SpeedGain, 1.15f);
                bool wasAtMaximum = speedBeforeImpact >= playerSpeed.maximumSpeed - 0.001f;
                int shards = wasAtMaximum ? impactPresentation.minEnergyShards : impactPresentation.maxEnergyShardsPerHit;
                effectPool?.PlayEnergyReturn(encounter.root.transform.position + Vector3.up * 0.65f, shards,
                    speedVisualProfile.Get(CurrentTier).primaryColor, wasAtMaximum ? 0.55f : 1f);
            }
            else if (resolution == ObstacleResolutionType.Equal)
            {
                callout = "HOLD SPEED";
                calloutUntil = elapsed + 0.7f;
                Impact(encounter, CollisionOutcome.Neutral, 0.85f);
            }
            else
            {
                if (!lowSpeedWarningShown)
                {
                    lowSpeedWarningShown = true;
                    callout = "TOO SLOW - IMPACT COSTS SPEED!";
                    calloutUntil = elapsed + 1.6f;
                }
                else
                {
                    callout = string.Empty;
                    calloutUntil = elapsed;
                }
                Impact(encounter, CollisionOutcome.SpeedLoss, 1.25f);
            }
        }

        private void UpdateGameplayCombo(Encounter encounter, ObstacleResolutionType resolution)
        {
            if (encounter == null || encounter.obstacle == null) return;

            if (encounter.obstacle.Type == ObstacleType.Soldier)
            {
                if (resolution == ObstacleResolutionType.Boosted)
                    comboManager?.AddCombo();
                else if (resolution == ObstacleResolutionType.Dropped)
                    comboManager?.ResetCombo();
            }
            else if (encounter.obstacle.Type == ObstacleType.StoneWall
                     && resolution == ObstacleResolutionType.Dropped)
            {
                comboManager?.ResetCombo();
            }
        }

        private ObstacleResolutionType ResolveObstacle(Encounter encounter)
        {
            if (encounter.obstacle == null || encounter.obstacle.HasResolved)
                return ObstacleResolutionType.Equal;
            numberCombatSystem?.ResolveTarget(encounter.numberTarget);
            float speedBeforeResolution = speedController.CurrentSpeed;
            int levelBeforeImpact = speedController.GetCurrentLevel();
            float boost = encounter.obstacle.Type == ObstacleType.Soldier && encounter.tier == 1
                ? playerSpeed.levelOneSoldierBoost
                : playerSpeed.normalImpactBoost;
            bool isStoneWall = encounter.obstacle.Type == ObstacleType.StoneWall;
            bool isElite = encounter.obstacle.Type == ObstacleType.Soldier && encounter.tier >= 4;
            ObstacleResolutionType resolution;
            if (SmallPotionInvulnerabilityActive)
                resolution = encounter.obstacle.ResolveWithoutPenalty(speedController, boost);
            else
                resolution = isStoneWall
                    ? encounter.obstacle.Resolve(speedController, boost, tuning.stoneWallSpeedPenaltyLevels)
                    : isElite
                        ? encounter.obstacle.Resolve(speedController, boost, tuning.documentEliteSpeedPenalty)
                        : encounter.obstacle.Resolve(speedController, boost);
            if (resolution == ObstacleResolutionType.Dropped
                && speedController.CurrentSpeed < speedBeforeResolution - 0.001f)
            {
                penaltyEdgeIntensity = 1f;
            }
            int levelAfterImpact = speedController.GetCurrentLevel();
            if (levelBeforeImpact >= 9 && levelAfterImpact <= levelBeforeImpact)
            {
                float strength = encounter.obstacle.Type == ObstacleType.StoneWall ? 1.15f : 1f;
                speedFeedback?.PlayHighSpeedImpactSonicBoom(levelBeforeImpact, strength,
                    speedLevelFeedbackConfig != null && speedLevelFeedbackConfig.accessibilityReducedFlash);
            }
            return resolution;
        }

        private void Impact(Encounter encounter, CollisionOutcome outcome, float strength)
        {
            encounter.visibility?.MarkKnockedBack();
            if (Time.unscaledTime - lastImpactTime <= impactPresentation.comboWindow)
            {
                comboPitchIndex = Mathf.Min(comboPitchIndex + 1, impactPresentation.comboPitchSteps - 1);
            }
            else
            {
                comboPitchIndex = 0;
            }
            lastImpactTime = Time.unscaledTime;
            float normalizedActualSpeed = forwardMotion != null ? forwardMotion.NormalizedActualSpeed : 0f;
            float impactForwardSpeed = forwardMotion != null ? forwardMotion.CurrentForwardSpeed : GetForwardSpeed();
            int sequence = impactSequence++;
            audioFeedback?.PlayCollisionOutcome(outcome, comboPitchIndex, impactPresentation.comboPitchStep,
                normalizedActualSpeed, encounter.root.transform.position);
            flashAlpha = Mathf.Max(flashAlpha, impactPresentation.normalFlash * strength);
            float side = Mathf.Sign(encounter.root.transform.position.x - runner.position.x);
            if (Mathf.Abs(side) < 0.1f) side = (sequence & 1) == 0 ? -1f : 1f;
            directionalShake = new Vector3(side, 0.25f, 0f).normalized;
            if (Time.unscaledTime - lastNormalShakeTime >= impactPresentation.normalShakeCooldown && comboPitchIndex == 0)
            {
                lastNormalShakeTime = Time.unscaledTime;
                PunchCamera(0.1f, impactPresentation.normalCameraShake, impactPresentation.normalFovPunch);
            }
            SpeedTierVisualData impactTier = speedVisualProfile.Get(CurrentTier);
            float actualImpactScale = Mathf.Lerp(0.85f, 1.3f, normalizedActualSpeed)
                * (speedFeedback != null ? speedFeedback.CurrentImpactMultiplier : 1f);
            effectPool?.PlayImpact(encounter.root.transform.position + Vector3.up,
                outcome != CollisionOutcome.SpeedLoss ? impactTier.secondaryColor : SpeedLossImpactColor,
                strength * actualImpactScale, CurrentTier);

            bool launched = encounter.soldierKnockback != null
                && encounter.soldierKnockback.Launch(soldierKnockbackPresentation,
                    normalizedActualSpeed, impactForwardSpeed, CurrentTier, encounter.visibility);
            if (!launched)
            {
                if (encounter.visibility != null) encounter.visibility.Recycle();
                else encounter.root.SetActive(false);
            }

            if (impactPresentation.enableNormalHitStop)
                visualTimeScale.RequestSlowMotion(impactPresentation.hitStopTimeScale, impactPresentation.hitStopDuration);
        }

        private void PunchCamera(float duration, float strength, float fov)
        {
            shakeUntil = Mathf.Max(shakeUntil, Time.unscaledTime + duration);
            shakeStrength = Mathf.Max(shakeStrength, strength);
            if (gameCamera != null)
            {
                fovPunchOffset = Mathf.Max(fovPunchOffset, fov);
            }
        }

        private void CheckBossEntry()
        {
            if (!bossDefeated && !rewardStageActive && boss != null
                && runner.position.z >= tuning.bossDistance - 3.1f)
            {
                StartCoroutine(BossClash());
            }
        }

        private void CheckRewardStageCompletion()
        {
            if (!rewardStageActive || rewardStageCompleted || runner == null
                || runner.position.z < rewardStageEndZ)
                return;

            rewardStageCompleted = true;
            rewardStageActive = false;
            ending = true;
            SetGameplayHudVisible(false);
            flowController.EnterResult();
        }

        private static bool HasAnimatorParameter(Animator animator, int parameterHash)
        {
            if (animator == null) return false;
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].nameHash == parameterHash) return true;
            return false;
        }

        private void SetBossAttacking(bool attacking)
        {
            if (bossRuntimeAnimator != null && bossSupportsAttackAnimation)
                bossRuntimeAnimator.SetBool(BossAttackingHash, attacking);
        }

        private void ResetBossTapInteraction(bool immediatePromptHide)
        {
            bossTapInputActive = false;
            bossTapCount = 0;
            bossTapPromptView?.Hide(immediatePromptHide);
            SetBossAttacking(false);
            runnerSpriteVisual?.SetShieldHeld(false);
            audioFeedback?.StopBossStruggle();
        }

        private bool ReadBossTapDown()
        {
            if (!bossTapInputActive) return false;

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                    if (Input.GetTouch(i).phase == TouchPhase.Began) return true;
                return false;
            }

            return Input.GetMouseButtonDown(0);
        }

        private IEnumerator BossClash()
        {
            bossSequence = true;
            speedFeedback?.SetRunningTrailsVisible(false);
            flowController.EnterBoss();
            CancelDrag();
            ResetBossTapInteraction(true);
            targetX = 0f;
            bool speedQualified = CurrentTier >= playerSpeed.bossVictoryLevel;
            callout = string.Empty;
            calloutUntil = elapsed;
            bossClashVisual.Begin(speedQualified);
            runnerSpriteVisual?.SetShieldHeld(true);

            Vector3 runnerStart = runner.position;
            Vector3 bossStart = boss.position;
            Vector3 runnerContact = new Vector3(0f, runnerStart.y,
                tuning.bossDistance - 1.1f);
            Vector3 bossContact = new Vector3(0f, bossStart.y,
                tuning.bossDistance + 1.1f);

            currentBossPhase = BossClashPhase.Approach;
            bossClashVisual.SetPhase(currentBossPhase);
            float timer = 0f;
            while (timer < bossClashPresentation.approachDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / bossClashPresentation.approachDuration);
                runner.position = Vector3.Lerp(runnerStart, runnerContact, t * t);
                boss.position = Vector3.Lerp(bossStart, bossContact, t * t);
                bossClashVisual.UpdatePresentation(t, Vector3.Lerp(runner.position, boss.position, 0.5f) + Vector3.up);
                yield return null;
            }

            currentBossPhase = BossClashPhase.Contact;
            bossClashVisual.SetPhase(currentBossPhase);
            if (bossRuntimeAnimator != null)
                bossRuntimeAnimator.Play(BossClashHash, 0, 0f);
            numberCombatSystem?.ResolveTarget(bossNumberTarget);
            audioFeedback?.PlayBossContact();
            speedFeedback?.PlayHighSpeedImpactSonicBoom(CurrentTier, 1.2f,
                speedLevelFeedbackConfig != null && speedLevelFeedbackConfig.accessibilityReducedFlash);
            flashAlpha = Mathf.Max(flashAlpha, 0.48f);
            PunchCamera(bossClashPresentation.contactDuration, bossClashPresentation.contactShake, bossClashPresentation.contactFovPunch);
            effectPool?.PlayImpact(Vector3.Lerp(runner.position, boss.position, 0.5f) + Vector3.up,
                speedQualified ? bossClashPresentation.playerEnergy : bossClashPresentation.bossEnergy,
                1.65f, CurrentTier);
            timer = 0f;
            while (timer < bossClashPresentation.contactDuration)
            {
                timer += Time.unscaledDeltaTime;
                bossClashVisual.UpdatePresentation(timer / bossClashPresentation.contactDuration, Vector3.Lerp(runner.position, boss.position, 0.5f) + Vector3.up);
                yield return null;
            }

            currentBossPhase = BossClashPhase.Struggle;
            bossClashVisual.SetPhase(currentBossPhase);
            audioFeedback?.BeginBossStruggle();
            SetBossAttacking(true);
            bossTapCount = 0;
            bossTapInputActive = speedQualified;
            if (speedQualified) bossTapPromptView?.Show();
            else bossTapPromptView?.Hide(true);

            int requiredTaps = Mathf.Max(1, bossClashPresentation.requiredTapCount);
            float failureDuration = Mathf.Max(0.1f, bossClashPresentation.struggleDuration);
            float tapPush = 0f;
            timer = 0f;
            while ((speedQualified && bossTapCount < requiredTaps)
                   || (!speedQualified && timer < failureDuration))
            {
                float deltaTime = Time.unscaledDeltaTime;
                timer += deltaTime;
                if (speedQualified && ReadBossTapDown())
                {
                    bossTapCount = Mathf.Min(requiredTaps, bossTapCount + 1);
                    comboManager?.AddCombo();
                    tapPush = 1f;
                    bossTapPromptView?.RegisterTap();
                }

                float progress = speedQualified
                    ? bossTapCount / (float)requiredTaps
                    : Mathf.Clamp01(timer / failureDuration);
                float tugFrequency = Mathf.Max(0.1f, bossClashPresentation.zAxisTugFrequency);
                float tug = Mathf.Sin(timer * tugFrequency * Mathf.PI * 2f)
                    * Mathf.Max(0f, bossClashPresentation.zAxisTugAmplitude);
                float pushDistance = Mathf.Max(0f, bossClashPresentation.tapPushDistance);
                float push = (progress * 0.55f + tapPush * 0.45f) * pushDistance;

                runner.position = runnerContact + Vector3.forward * (tug + push * 0.22f);
                boss.position = bossContact + Vector3.back * tug + Vector3.forward * push;
                float returnDuration = Mathf.Max(0.02f,
                    bossClashPresentation.tapPushReturnDuration);
                tapPush = Mathf.MoveTowards(tapPush, 0f, deltaTime / returnDuration);

                shakeStrength = bossClashPresentation.struggleShake;
                shakeUntil = Time.unscaledTime + 0.08f;
                directionalShake = new Vector3(0f, 0.15f, Mathf.Sign(tug)).normalized;
                Vector3 center = Vector3.Lerp(runner.position, boss.position,
                    speedQualified ? 0.6f : 0.4f) + Vector3.up;
                bossClashVisual.UpdatePresentation(progress, center);
                yield return null;
            }

            bool wins = speedQualified && bossTapCount >= requiredTaps;
            bossTapInputActive = false;
            bossTapPromptView?.Hide();
            SetBossAttacking(false);

            currentBossPhase = BossClashPhase.Stagger;
            bossClashVisual.SetPhase(currentBossPhase);
            Vector3 staggerStartRunner = runner.position;
            Vector3 staggerStartBoss = boss.position;
            Vector3 staggerRunner = new Vector3(0f, staggerStartRunner.y,
                staggerStartRunner.z - (wins ? 0f : 1.2f));
            Vector3 staggerBoss = new Vector3(0f, staggerStartBoss.y,
                staggerStartBoss.z + (wins ? 1.4f : 0f));
            timer = 0f;
            while (timer < bossClashPresentation.staggerDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / bossClashPresentation.staggerDuration);
                runner.position = Vector3.Lerp(staggerStartRunner, staggerRunner, t);
                boss.position = Vector3.Lerp(staggerStartBoss, staggerBoss, t);
                if (wins) boss.rotation = Quaternion.Euler(Mathf.Lerp(0f, -18f, t), 0f, 0f);
                bossClashVisual.UpdatePresentation(t, Vector3.Lerp(runner.position, boss.position, wins ? 0.68f : 0.32f) + Vector3.up);
                yield return null;
            }

            runnerSpriteVisual?.SetShieldHeld(false);
            currentBossPhase = BossClashPhase.Finish;
            bossClashVisual.SetPhase(currentBossPhase);
            if (wins)
            {
                yield return StartCoroutine(BossFinishWin());
            }
            else
            {
                yield return StartCoroutine(BossFinishFailure());
            }
        }

        private IEnumerator BossFinishWin()
        {
            Coroutine rewardBuildCoroutine = RewardRunEnabled
                ? StartCoroutine(BuildRewardStage())
                : null;
            if (bossRuntimeAnimator != null)
            {
                bossRuntimeAnimator.ResetTrigger(BossDieHash);
                bossRuntimeAnimator.SetTrigger(BossDieHash);
            }

            audioFeedback?.PlayBossFinish();
            speedFeedback?.PlayHighSpeedImpactSonicBoom(CurrentTier, 1.35f,
                speedLevelFeedbackConfig != null && speedLevelFeedbackConfig.accessibilityReducedFlash);
            flashAlpha = Mathf.Max(flashAlpha, 0.56f);
            PunchCamera(bossClashPresentation.finishDuration, bossClashPresentation.finishShake, bossClashPresentation.finishFovPunch);
            effectPool?.PlayImpact(Vector3.Lerp(runner.position, boss.position, 0.55f) + Vector3.up,
                bossClashPresentation.playerEnergy, 2f, CurrentTier);
            Vector3 bossStart = boss.position;
            Vector3 bossEnd = bossStart + Vector3.forward * BossDeathFlightDistance;
            Quaternion bossStartRotation = boss.rotation;
            float deathDuration = Mathf.Max(BossDeathAnimationDuration, bossClashPresentation.finishDuration);
            float timer = 0f;
            while (timer < deathDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / deathDuration);
                float flightT = Mathf.SmoothStep(0f, 1f, t);
                float arc = Mathf.Sin(t * Mathf.PI) * BossDeathFlightHeight;
                boss.position = Vector3.Lerp(bossStart, bossEnd, flightT) + Vector3.up * arc;
                boss.rotation = Quaternion.Slerp(bossStartRotation, Quaternion.identity, flightT);
                bossClashVisual.UpdatePresentation(t, boss.position + Vector3.up);
                yield return null;
            }

            boss.position = bossEnd;
            boss.rotation = Quaternion.identity;

            bossClashVisual.SetVisible(false);
            BreakCage();
            currentBossPhase = BossClashPhase.None;
            yield return StartCoroutine(PrincessWalkToPlayer());
            yield return new WaitForSecondsRealtime(0.55f);

            if (rewardBuildCoroutine != null)
                yield return rewardBuildCoroutine;
            if (RewardRunEnabled)
            {
                EnterRewardStage();
                yield break;
            }

            bossDefeated = true;
            ending = true;
            bossSequence = false;
            SetGameplayHudVisible(false);
            flowController.EnterResult();
        }

        private void EnterRewardStage()
        {
            if (rewardStageActive || rewardStageCompleted) return;

            bossDefeated = true;
            rewardStageActive = true;
            rewardStageCompleted = false;
            rewardStageEndZ = CourseEndZ;
            bossSequence = false;
            ending = false;
            targetX = 0f;
            CancelDrag();
            if (boss != null)
            {
                DisableColliders(boss);
                boss.gameObject.SetActive(false);
            }
            if (princess != null)
                DisableColliders(princess);
            for (int i = 0; i < rewardStageWallRoots.Count; i++)
            {
                GameObject wallRoot = rewardStageWallRoots[i];
                if (wallRoot != null) wallRoot.SetActive(true);
            }
            speedController?.SetLevel(speedController.MaxLevel, SpeedChangeReason.BossEvent, this);
            forwardMotion?.SnapToTarget();
            flowController.EnterMainRun();
        }

        private static void DisableColliders(Transform root)
        {
            if (root == null) return;
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = false;
        }

        private IEnumerator BossFinishFailure()
        {
            runnerSpriteVisual?.SetFallen(true);
            audioFeedback?.PlayBossFailure();
            flashAlpha = Mathf.Max(flashAlpha, 0.22f);
            PunchCamera(0.18f, bossClashPresentation.contactShake * 0.7f, 1.5f);
            float timer = 0f;
            while (timer < bossClashPresentation.finishDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / bossClashPresentation.finishDuration);
                bossClashVisual.UpdatePresentation(t, Vector3.Lerp(runner.position, boss.position, 0.32f) + Vector3.up);
                yield return null;
            }
            bossClashVisual.SetVisible(false);
            currentBossPhase = BossClashPhase.None;
            yield return StartCoroutine(FallbackSequence());
        }

        private IEnumerator FallbackSequence()
        {
            Vector3 from = runner.position;
            Vector3 to = new Vector3(0f, runner.position.y, tuning.bossDistance - 58f * CourseDistanceScale);
            float timer = 0f;
            while (timer < 0.75f)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / 0.75f;
                runner.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                runnerVisual.Rotate(0f, 0f, 600f * Time.unscaledDeltaTime, Space.Self);
                yield return null;
            }

            runnerVisual.localRotation = Quaternion.identity;
            runnerSpriteVisual?.SetFallen(false);
            speedController.SetLevel(speedController.MaxLevel, SpeedChangeReason.BossEvent, this);
            forwardMotion?.SnapToTarget();
            boss.position = new Vector3(0f, BossStandingY, tuning.bossDistance + 2.5f);
            boss.rotation = Quaternion.identity;
            numberCombatSystem?.ResetTarget(bossNumberTarget);
            callout = "TRY AGAIN!";
            calloutUntil = elapsed + 2f;
            bossSequence = false;
            speedFeedback?.SetRunningTrailsVisible(true);
            flowController.EnterMainRun();
        }

        private void BreakCage()
        {
            if (cage != null)
            {
                for (int i = 0; i < cage.childCount; i++)
                {
                    Transform bar = cage.GetChild(i);
                    Collider[] colliders = bar.GetComponentsInChildren<Collider>();
                    for (int c = 0; c < colliders.Length; c++) colliders[c].enabled = false;
                    StartCoroutine(LaunchDebris(bar.gameObject, i));
                }
            }

            if (princess != null)
            {
                princess.gameObject.SetActive(true);
                princess.localScale = Vector3.one;
            }

            for (int i = 0; i < 5; i++)
            {
                SpawnImpactBurst(new Vector3(UnityEngine.Random.Range(-2f, 2f), 1.5f, tuning.bossDistance + 8f),
                    Color.HSVToRGB(UnityEngine.Random.value, 0.75f, 1f));
            }
        }

        private IEnumerator PrincessWalkToPlayer()
        {
            if (princess == null || runner == null) yield break;

            if (princessRuntimeAnimator != null)
                princessRuntimeAnimator.Play(PrincessIdleHash, 0, 0f);
            yield return new WaitForSecondsRealtime(0.25f);

            if (princessRuntimeAnimator != null)
                princessRuntimeAnimator.Play(PrincessWalkHash, 0, 0f);

            Vector3 start = princess.position;
            Vector3 target = new Vector3(
                runner.position.x,
                start.y,
                runner.position.z + 1.4f);
            float distance = Vector3.Distance(start, target);
            float duration = Mathf.Clamp(distance / 3.6f, 1f, 2.4f);
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);
                princess.position = Vector3.Lerp(
                    start, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            princess.position = target;
            if (princessRuntimeAnimator != null)
                princessRuntimeAnimator.Play(PrincessIdleHash, 0, 0f);
        }

        private IEnumerator LaunchDebris(GameObject debris, int index)
        {
            Vector3 velocity = new Vector3((index % 2 == 0 ? -1f : 1f) * UnityEngine.Random.Range(3f, 7f), UnityEngine.Random.Range(4f, 9f), UnityEngine.Random.Range(2f, 7f));
            float timer = 0f;
            while (timer < 1.5f && debris != null)
            {
                float dt = Time.unscaledDeltaTime;
                timer += dt;
                velocity += Vector3.down * 12f * dt;
                debris.transform.position += velocity * dt;
                debris.transform.Rotate(280f * dt, 360f * dt, 190f * dt);
                yield return null;
            }
        }

        private void BuildWorld()
        {
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            penaltyEdgeTexture = CreatePenaltyEdgeTexture(64,
                impactPresentation != null ? impactPresentation.penaltyEdgeWidth : 0.18f);

            worldRoot = new GameObject("GeneratedWorld").transform;
            worldRoot.SetParent(transform, false);
            if (GetComponent<BulletTimeManager>() == null)
                gameObject.AddComponent<BulletTimeManager>();
            BuildCameraAndLight();
            BuildRoad();
            BuildRunner();
            BuildNumberCombatSystem();
            BuildEffectPool();
            BuildSpeedBar();
            BuildSpeedLevelFeedback();
            BuildBossArea();
        }

        private void BuildNumberCombatSystem()
        {
            if (numberCombat == null) numberCombat = new NumberCombatSettings();
            if (!numberCombat.enabled)
            {
                numberCombatSystem = null;
                return;
            }

            numberCombatSystem = GetComponent<NumberCombatSystem>();
            if (numberCombatSystem == null) numberCombatSystem = gameObject.AddComponent<NumberCombatSystem>();
            numberCombatSystem.Initialize(numberCombat, gameCamera, runner,
                runner != null ? runner.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>(),
                speedController);
        }

        private void BuildEffectPool()
        {
            GameObject poolRoot = new GameObject("ImpactEffectPool");
            poolRoot.transform.SetParent(worldRoot, false);
            effectPool = poolRoot.AddComponent<ImpactEffectPool>();
            effectPool.Initialize(runner, impactPresentation, speedVisualProfile, visualPerformance);
            effectPool.EnergyShardAbsorbed = OnEnergyShardAbsorbed;
        }

        private void BuildSpeedBar()
        {
            GameObject canvasRoot = new GameObject("SpeedBarCanvas");
            canvasRoot.transform.SetParent(transform, false);
            speedBarView = canvasRoot.AddComponent<SpeedBarView>();
            speedBarView.Initialize(speedController, speedVisualProfile,
                speedBarHintFrame, speedBarSoldierHintIcon, speedBarStoneWallHintIcon,
                speedBarStoneWallLockedHintIcon,
                Mathf.Clamp(tuning.stoneWallSafeSpeedLevel, 1, speedController.MaxLevel),
                runner, gameCamera);
            BuildComboSystem(canvasRoot);
        }

        private void BuildComboSystem(GameObject canvasRoot)
        {
            if (gameplayCombo == null) gameplayCombo = new GameplayComboSettings();
            gameplayCombo.UpgradeLegacyDefaults();
            comboManager = GetComponent<ComboManager>();
            if (comboManager == null) comboManager = gameObject.AddComponent<ComboManager>();
            comboManager.Initialize(gameplayCombo);

            comboUIController = canvasRoot.AddComponent<ComboUIController>();
            comboUIController.Initialize(comboManager, gameplayCombo.presentation);
        }

        private void BuildSpeedLevelFeedback()
        {
            if (speedLevelFeedbackConfig == null)
            {
                speedLevelFeedbackConfig = ScriptableObject.CreateInstance<SpeedLevelFeedbackConfig>();
                speedLevelFeedbackConfig.name = "RuntimeSpeedLevelFeedbackConfigFallback";
                ownsSpeedLevelFeedbackConfig = true;
            }
            SpeedLevelFeedbackController speedLevelFeedback = gameObject.AddComponent<SpeedLevelFeedbackController>();
            speedLevelFeedback.Initialize(speedController, speedLevelFeedbackConfig, speedVisualProfile,
                speedFeedback, speedBarView, audioFeedback, OnLevelUpCameraFeedback);
        }

        private void OnLevelUpCameraFeedback(float fovStrength, float impulseStrength, float duration)
        {
            directionalShake = Vector3.up;
            PunchCamera(Mathf.Clamp(duration, 0.12f, 0.25f), impulseStrength, fovStrength);
        }

        private void OnEnergyShardAbsorbed()
        {
            flashAlpha = Mathf.Max(flashAlpha, 0.045f);
            speedFeedback?.PulseNormalBoost();
            speedBarView?.PulseNormalBoost();
            audioFeedback?.PlayEnergyReturn();
        }

        private void BuildCameraAndLight()
        {
            baseFov = GetConfiguredBaseFov();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            Camera existing = Camera.main;
            if (existing != null)
            {
                gameCamera = existing;
            }
            else
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                gameCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            // The scene is laid out along world Z. A 2D-template camera left in
            // orthographic mode removes all near/far perspective from the road.
            gameCamera.orthographic = false;
            gameCamera.usePhysicalProperties = false;
            bool portrait = Screen.height >= Screen.width;
            float cameraHeight = portrait ? portraitCamera.cameraHeight : environment.cameraHeight;
            float cameraBackDistance = portrait ? portraitCamera.cameraBackDistance : environment.cameraBackDistance;
            gameCamera.transform.position = new Vector3(0f, cameraHeight, -cameraBackDistance);
            gameCamera.fieldOfView = baseFov;
            gameCamera.nearClipPlane = 0.1f;
            gameCamera.farClipPlane = 420f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = environment.skyFogColor;

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(worldRoot, false);
            lightObject.transform.rotation = Quaternion.Euler(environment.lightEuler);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = environment.lightIntensity;
            light.color = environment.lightColor;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = environment.shadowStrength;

            RenderSettings.ambientLight = environment.ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogColor = environment.skyFogColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = environment.fogStart;
            RenderSettings.fogEndDistance = environment.fogEnd;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = environment.shadowDistance;
        }

        private float GetConfiguredBaseFov()
        {
            return Screen.height >= Screen.width ? portraitCamera.fieldOfView : environment.baseFieldOfView;
        }

        private void BuildRoad()
        {
            float courseEndZ = CourseEndZ;
            float roadLength = courseEndZ + 28f;
            GameObject road = CreateBox("Road", new Vector3(0f, -0.3f, courseEndZ * 0.5f), new Vector3(8.5f, 0.5f, roadLength), environment.roadColor, worldRoot);
            if (roadSurfaceMaterial != null)
                road.GetComponent<Renderer>().sharedMaterial = roadSurfaceMaterial;
            GameObject leftWall = CreateBox("LeftWall", new Vector3(-4.7f, 0.25f, courseEndZ * 0.5f), new Vector3(1.1f, 1.1f, roadLength), environment.wallColor, worldRoot);
            GameObject rightWall = CreateBox("RightWall", new Vector3(4.7f, 0.25f, courseEndZ * 0.5f), new Vector3(1.1f, 1.1f, roadLength), environment.wallColor, worldRoot);
            bool hasAuthoredRoadBorders = BuildRoadBorderVisuals(leftWall, rightWall);
            CreateRoadBoundary("LeftRoadBoundary", -4.12f);
            CreateRoadBoundary("RightRoadBoundary", 4.12f);

            CreateDecorationBox("LeftRouteGuide", new Vector3(-1.35f, -0.015f, courseEndZ * 0.5f), new Vector3(0.055f, 0.025f, courseEndZ + 20f), environment.routeMarkColor, worldRoot);
            CreateDecorationBox("RightRouteGuide", new Vector3(1.35f, -0.015f, courseEndZ * 0.5f), new Vector3(0.055f, 0.025f, courseEndZ + 20f), environment.routeMarkColor, worldRoot);

            for (float z = 0f; z < courseEndZ; z += environment.environmentReferenceSpacing)
            {
                CreateDecorationBox("RoadBand", new Vector3(0f, -0.02f, z), new Vector3(8.4f, 0.04f, 0.16f), environment.routeMarkColor, worldRoot);
                if (!hasAuthoredRoadBorders)
                {
                    CreateDecorationBox("TorchLeft", new Vector3(-4.05f, 1.1f, z + 2.5f), new Vector3(0.25f, 2.2f, 0.25f), environment.timberColor, worldRoot);
                    CreateDecorationBox("TorchRight", new Vector3(4.05f, 1.1f, z + 2.5f), new Vector3(0.25f, 2.2f, 0.25f), environment.timberColor, worldRoot);
                }
            }

            BuildDistantCastle();
        }

        private bool BuildRoadBorderVisuals(GameObject leftWall, GameObject rightWall)
        {
            if (roadBorderStraightPrefab == null || roadBorderPedestalPrefab == null || roadBorderMaterial == null)
                return false;

            MeshFilter straightFilter = roadBorderStraightPrefab.GetComponentInChildren<MeshFilter>(true);
            MeshFilter pedestalFilter = roadBorderPedestalPrefab.GetComponentInChildren<MeshFilter>(true);
            if (straightFilter == null || straightFilter.sharedMesh == null
                || pedestalFilter == null || pedestalFilter.sharedMesh == null)
                return false;

            leftWall.GetComponent<Renderer>().enabled = false;
            rightWall.GetComponent<Renderer>().enabled = false;

            float courseEndZ = CourseEndZ;
            float roadLength = courseEndZ + 28f;
            float roadStart = courseEndZ * 0.5f - roadLength * 0.5f;
            int segmentCount = Mathf.CeilToInt(roadLength / RoadBorderSegmentLength);
            BuildRoadBorderSide(-1f, roadStart, segmentCount, straightFilter, pedestalFilter);
            BuildRoadBorderSide(1f, roadStart, segmentCount, straightFilter, pedestalFilter);
            return true;
        }

        private void BuildRoadBorderSide(float side, float roadStart, int segmentCount,
            MeshFilter straightFilter, MeshFilter pedestalFilter)
        {
            Matrix4x4 straightAssetMatrix = Matrix4x4.TRS(straightFilter.transform.localPosition,
                straightFilter.transform.localRotation, straightFilter.transform.localScale);
            Matrix4x4 pedestalAssetMatrix = Matrix4x4.TRS(pedestalFilter.transform.localPosition,
                pedestalFilter.transform.localRotation, pedestalFilter.transform.localScale);
            Quaternion sideRotation = Quaternion.Euler(0f, side > 0f ? 180f : 0f, 0f);

            for (int chunkStart = 0; chunkStart < segmentCount; chunkStart += RoadBorderSegmentsPerChunk)
            {
                int chunkEnd = Mathf.Min(segmentCount, chunkStart + RoadBorderSegmentsPerChunk);
                int straightSubMeshCount = straightFilter.sharedMesh.subMeshCount;
                int pedestalSubMeshCount = pedestalFilter.sharedMesh.subMeshCount;
                var combines = new List<CombineInstance>(
                    RoadBorderSegmentsPerChunk * straightSubMeshCount + pedestalSubMeshCount * 3);
                for (int segmentIndex = chunkStart; segmentIndex < chunkEnd; segmentIndex++)
                {
                    float segmentZ = roadStart + (segmentIndex + 0.5f) * RoadBorderSegmentLength;
                    Matrix4x4 placement = Matrix4x4.TRS(
                        new Vector3(side * RoadBorderCenterX, RoadBorderCenterY, segmentZ),
                        sideRotation, new Vector3(1f, 1f, RoadBorderSegmentScale));
                    AddMeshSubmeshes(combines, straightFilter.sharedMesh, placement * straightAssetMatrix);

                    if (segmentIndex % RoadBorderPedestalInterval == 0)
                    {
                        float pedestalZ = roadStart + segmentIndex * RoadBorderSegmentLength;
                        Matrix4x4 pedestalPlacement = Matrix4x4.TRS(
                            new Vector3(side * RoadBorderCenterX, RoadBorderCenterY, pedestalZ),
                            sideRotation, Vector3.one);
                        AddMeshSubmeshes(combines, pedestalFilter.sharedMesh,
                            pedestalPlacement * pedestalAssetMatrix);
                        CreateRoadBorderFlame(side, pedestalZ, segmentIndex);
                    }
                }

                GameObject chunk = new GameObject((side < 0f ? "Left" : "Right")
                    + "RoadBorderChunk_" + (chunkStart / RoadBorderSegmentsPerChunk));
                chunk.transform.SetParent(worldRoot, false);
                Mesh mesh = new Mesh
                {
                    name = chunk.name + "Mesh",
                    indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
                };
                mesh.CombineMeshes(combines.ToArray(), true, true, false);
                mesh.RecalculateBounds();
                chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = roadBorderMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static void AddMeshSubmeshes(List<CombineInstance> combines, Mesh mesh,
            Matrix4x4 transform)
        {
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                combines.Add(new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = subMeshIndex,
                    transform = transform
                });
            }
        }

        private void CreateRoadBorderFlame(float side, float z, int segmentIndex)
        {
            if (roadBorderFlamePrefab == null) return;
            GameObject flame = Instantiate(roadBorderFlamePrefab, worldRoot);
            flame.name = (side < 0f ? "Left" : "Right") + "RoadBorderFlame_" + segmentIndex;
            flame.transform.localPosition = new Vector3(side * RoadBorderCenterX, 0.72f, z);
            flame.transform.localRotation = Quaternion.identity;
        }

        private void BuildDistantCastle()
        {
            Transform castle = new GameObject("DistantCastle").transform;
            castle.SetParent(worldRoot, false);
            float castleZ = RewardRunEnabled ? CourseEndZ + 19f : tuning.bossDistance + 19f;
            castle.position = new Vector3(0f, 0f, castleZ);

            CreateDecorationBox("CastleKeep", new Vector3(0f, 3.1f, 0f), new Vector3(5.2f, 6.2f, 3.2f), environment.castleColor, castle);
            CreateDecorationBox("CastleTowerLeft", new Vector3(-4.2f, 3.8f, 0.6f), new Vector3(2.3f, 7.6f, 2.8f), environment.castleColor, castle);
            CreateDecorationBox("CastleTowerRight", new Vector3(4.2f, 3.8f, 0.6f), new Vector3(2.3f, 7.6f, 2.8f), environment.castleColor, castle);
            CreateDecorationBox("CastleGate", new Vector3(0f, 1.5f, -1.7f), new Vector3(2.2f, 3f, 0.45f), new Color(0.13f, 0.15f, 0.17f), castle);
        }

        private GameObject CreateDecorationBox(string name, Vector3 position, Vector3 scale, Color color, Transform parent)
        {
            GameObject decoration = CreateBox(name, position, scale, color, parent);
            Collider collider = decoration.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            return decoration;
        }

        private void BuildRunner()
        {
            GameObject root = new GameObject("Player");
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(0f, 1f, 0f);
            runner = root.transform;

            speedController = root.AddComponent<PlayerSpeedController>();
            speedController.SpeedChanged += OnPlayerSpeedChanged;
            speedController.Initialize(playerSpeed);
            forwardMotion = root.AddComponent<PlayerForwardMotionController>();
            forwardMotion.Initialize(speedController, playerSpeed);

            ReplaceableVisual replaceable = root.AddComponent<ReplaceableVisual>();
            replaceable.Build(playerVisualPrefab, playerAnimator, PrimitiveType.Cylinder, new Color(0.12f, 0.45f, 0.85f), new Vector3(0.8f, 1f, 0.8f));
            runnerVisual = replaceable.VisualRoot;
            runnerAnimator = replaceable.Animator;
            runnerSpriteVisual = replaceable.GetComponentInChildren<PlayerSpriteVisualController>(true);
            runnerSpriteVisual?.ResetVisualState();

            speedFeedback = root.AddComponent<SpeedVisualFeedback>();
            speedFeedback.Initialize(speedVisualProfile, visualPerformance, runningWindTrailPrefab,
                accelerationAuraPrefab);
            BuildInvulnerabilityCircle(root.transform);
            audioFeedback = root.AddComponent<AudioFeedbackController>();
            audioFeedback.Initialize(audioPresentation);
            visualTimeScale = gameObject.GetComponent<VisualTimeScaleController>();
            if (visualTimeScale == null) visualTimeScale = gameObject.AddComponent<VisualTimeScaleController>();
        }

        private void OnPlayerSpeedChanged(SpeedChangedEvent change)
        {
            if (change.NewLevel > change.OldLevel
                && change.Reason != SpeedChangeReason.InitialSetup
                && change.Reason != SpeedChangeReason.Initialization)
            {
                naturalSpeedLossProtectedLevel = change.NewLevel;
                naturalSpeedLossProtectedUntil = Mathf.Max(naturalSpeedLossProtectedUntil,
                    Time.unscaledTime + NaturalSpeedLossProtectionDuration);
            }

            audioFeedback?.HandleSpeedChanged(change);
            if (change.NewLevel > change.OldLevel)
                speedFeedback?.PulseLevelUp();
            else if (change.Reason == SpeedChangeReason.NormalImpact || change.Reason == SpeedChangeReason.LowLevelCollisionReward)
                speedFeedback?.PulseNormalBoost();
        }

        private void OnDestroy()
        {
            if (speedController != null) speedController.SpeedChanged -= OnPlayerSpeedChanged;
            visualTimeScale?.Restore();
            if (ownsSpeedVisualProfile) DestroyOwnedObject(speedVisualProfile);
            if (ownsSpeedLevelFeedbackConfig) DestroyOwnedObject(speedLevelFeedbackConfig);
            DestroyOwnedObject(whiteTexture);
            DestroyOwnedObject(penaltyEdgeTexture);
            DestroyOwnedObject(buttonNormalTexture);
            DestroyOwnedObject(buttonActiveTexture);
            DestroyOwnedObject(upgradeMagicCircleSprite);
        }

        private static void DestroyOwnedObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private void BuildBossArea()
        {
            GameObject bossRoot = new GameObject("Boss");
            bossRoot.transform.SetParent(worldRoot, false);
            bossRoot.transform.position = new Vector3(0f, BossStandingY, tuning.bossDistance + 2.5f);
            boss = bossRoot.transform;
            ReplaceableVisual bossReplaceable = bossRoot.AddComponent<ReplaceableVisual>();
            bossReplaceable.Build(bossVisualPrefab, bossAnimator, PrimitiveType.Cylinder, new Color(0.58f, 0.08f, 0.06f), new Vector3(1.75f, 3f, 1.75f));
            bossVisual = bossReplaceable.VisualRoot;
            bossRuntimeAnimator = bossReplaceable.Animator;
            bossSupportsAttackAnimation = HasAnimatorParameter(bossRuntimeAnimator, BossAttackingHash);
            int bossLabelLevel = Mathf.Clamp(playerSpeed.bossVictoryLevel, 1, 10);
            bossNumberTarget = numberCombatSystem?.RegisterTarget(boss,
                bossRoot.GetComponentsInChildren<Renderer>(true), bossLabelLevel,
                numberCombat.bossHeadClearance);

            GameObject princessRoot = new GameObject("Princess");
            princessRoot.transform.SetParent(worldRoot, false);
            princessRoot.transform.position = new Vector3(0f, 1f, tuning.bossDistance + 9f);
            princess = princessRoot.transform;
            ReplaceableVisual princessReplaceable = princessRoot.AddComponent<ReplaceableVisual>();
            princessReplaceable.Build(princessVisualPrefab, princessAnimator, PrimitiveType.Cylinder, new Color(1f, 0.45f, 0.7f), new Vector3(0.75f, 1.8f, 0.75f));
            princessRuntimeAnimator = princessReplaceable.Animator;
            if (princessRuntimeAnimator != null)
                princessRuntimeAnimator.Play(PrincessIdleHash, 0, 0f);
            princessRoot.SetActive(true);

            cage = new GameObject("Cage").transform;
            cage.SetParent(worldRoot, false);
            cage.position = new Vector3(0f, 0f, tuning.bossDistance + 9f);
            Color iron = new Color(0.12f, 0.13f, 0.15f);
            for (int i = -2; i <= 2; i++)
            {
                CreateBox("CageBar", new Vector3(i * 0.55f, 2.1f, 0f), new Vector3(0.16f, 4.2f, 0.16f), iron, cage);
            }
            CreateBox("CageTop", new Vector3(0f, 4.15f, 0f), new Vector3(2.6f, 0.2f, 1.8f), iron, cage);

            GameObject clashVisualRoot = new GameObject("BossClashVisual");
            clashVisualRoot.transform.SetParent(worldRoot, false);
            bossClashVisual = clashVisualRoot.AddComponent<BossClashVisual>();
            bossClashVisual.Initialize(runner, boss, bossClashPresentation);

            GameObject tapPromptRoot = new GameObject("BossTapPromptCanvas", typeof(RectTransform));
            tapPromptRoot.transform.SetParent(transform, false);
            bossTapPromptView = tapPromptRoot.AddComponent<BossTapPromptView>();
            bossTapPromptView.Initialize(bossTapHintSprite, bossClashPresentation);
        }

        private void BuildLevel()
        {
            float openingElixirZ = OpeningElixirZ;
            float safeHalfWidth = Mathf.Max(0.5f, tuning.laneHalfWidth - 0.35f);
            float openingLaneOffset = GetSoldierLaneCenter(SoldierPlacementMode.RightLaneLine, safeHalfWidth);
            CreateElixir(-openingLaneOffset, openingElixirZ,
                playerSpeed.tutorialElixirTargetLevel, OpeningElixirGroupId,
                prefab != null ? prefab.openingElixirLeftPrefab : null);
            CreateElixir(0f, openingElixirZ,
                playerSpeed.tutorialElixirTargetLevel, OpeningElixirGroupId,
                prefab != null ? prefab.openingElixirLeftPrefab : null);
            CreateElixir(openingLaneOffset, openingElixirZ,
                playerSpeed.tutorialElixirTargetLevel, OpeningElixirGroupId,
                prefab != null ? prefab.openingElixirLeftPrefab : null);

            float firstSoldierZ = openingElixirZ + tuning.tutorialFirstSoldierGap;
            int soldierCount = Mathf.Clamp(tuning.tutorialSoldierCount, 3, 5);
            for (int i = 0; i < soldierCount; i++)
            {
                CreateTarget(0f, firstSoldierZ + i * tuning.tutorialSoldierSpacing, 1,
                    showLevelLabel: i == 0);
            }

            float wallZ = firstSoldierZ + (soldierCount - 1) * tuning.tutorialSoldierSpacing + tuning.tutorialWallGap;
            CreateBreakableWall(wallZ, "TutorialStoneWall", tuning.tutorialWallBlockingMode,
                tuning.tutorialWallBulletTime, true);
            BuildConfiguredMainRun(wallZ);

            if (tuning == null || !tuning.useDocumentMainRunLayout)
            {
                float bossElixirZ = Mathf.Max(0f,
                    tuning.bossDistance - Mathf.Max(0f, tuning.bossMaxSpeedElixirDistance));
                CreateElixir(0f, bossElixirZ, speedController.MaxLevel, 0,
                    prefab != null ? prefab.bossMaxSpeedElixirPrefab : null,
                    "BossMaxSpeedElixir");
            }
        }

        private static MainRunZone[] CreateDocumentMainRunLayout()
        {
            return new[]
            {
                CreateMainRunZone(SoldiersLane(5), null, null),
                CreateMainRunZone(null, SoldiersLane(5), null),
                CreateMainRunZone(null, null, SoldiersLane(5)),
                CreateMainRunZone(SoldiersLane(5), null, null),
                CreateMainRunZone(null, SoldiersLane(3), null),
                CreateMainRunZone(null, null, SoldiersLane(1)),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(null, SoldiersLane(3), null),
                CreateMainRunZone(null, null, StoneWallLane()),
                CreateMainRunZone(SoldiersLane(2), null, null),
                CreateMainRunZone(null, SoldiersLane(2), null),
                CreateMainRunZone(null, StoneWallLane(), null),
                CreateMainRunZone(SoldiersLane(3), null, null),
                CreateMainRunZone(StoneWallLane(), null, null),
                CreateMainRunZone(null, null, SoldiersLane(5)),
                CreateMainRunZone(null, null, StoneWallLane()),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(null, StoneWallLane(), StoneWallLane()),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(StoneWallLane(), StoneWallLane(), null),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(null, StoneWallLane(), StoneWallLane()),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(SmallPotionLane(), null, null),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(StoneWallLane(), StoneWallLane(), StoneWallLane()),
                CreateMainRunZone(StoneWallLane(), StoneWallLane(), StoneWallLane()),
                CreateMainRunZone(StoneWallLane(), StoneWallLane(), StoneWallLane()),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(null, SoldiersLane(4), null),
                CreateMainRunZone(null, null, StoneWallLane()),
                CreateMainRunZone(SoldiersLane(4), null, StoneWallLane()),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(null, StoneWallLane(), null),
                CreateMainRunZone(SoldiersLane(3), SoldiersLane(4), EliteLane()),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(null, EliteLane(), null),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(StoneWallLane(), null, EliteLane()),
                CreateMainRunZone(SoldiersLane(2), null, SmallPotionLane()),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(SoldiersLane(3), EliteLane(), StoneWallLane()),
                CreateMainRunZone(StoneWallLane(), SoldiersLane(3), SoldiersLane(3)),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(SoldiersLane(4), null, null),
                CreateMainRunZone(null, null, SoldiersLane(4)),
                CreateMainRunZone(SoldiersLane(4), StoneWallLane(), EliteLane()),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(SoldiersLane(2), EliteLane(), null),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(SoldiersLane(2), null, EliteLane()),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(SoldiersLane(2), EliteLane(), null),
                CreateMainRunZone(null, null, null),
                CreateMainRunZone(SoldiersLane(2), null, EliteLane()),
                CreateMainRunZone(null, LargePotionLane(), null),
                CreateMainRunZone(null, null, null)
            };
        }

        private void ApplyDocumentCourseLength()
        {
            if (tuning == null || !tuning.useDocumentMainRunLayout) return;

            float tutorialEndZ = CalculateTutorialWallZ();
            float layoutEndZ = CalculateDocumentMainRunEndZ(tutorialEndZ);
            tuning.bossDistance = Mathf.Max(20f,
                layoutEndZ + Mathf.Max(0f, tuning.bossApproachPadding));
        }

        private float CalculateTutorialWallZ()
        {
            float openingElixirZ = tuning.openingElixirTime * playerSpeed.forwardSpeeds[0];
            int soldierCount = Mathf.Clamp(tuning.tutorialSoldierCount, 3, 5);
            float firstSoldierZ = openingElixirZ + tuning.tutorialFirstSoldierGap;
            return firstSoldierZ + (soldierCount - 1) * tuning.tutorialSoldierSpacing
                + tuning.tutorialWallGap;
        }

        private float CalculateDocumentMainRunEndZ(float tutorialEndZ)
        {
            MainRunZone[] layout = CreateDocumentMainRunLayout();
            float currentZ = tutorialEndZ + Mathf.Max(0f, tuning.documentMainRunStartOffset);
            for (int i = 0; i < layout.Length; i++)
            {
                currentZ += GetDocumentZoneLength(layout[i], i)
                    + Mathf.Max(0f, tuning.documentZoneGap);
            }
            return currentZ;
        }

        private void BuildDocumentMainRun(float tutorialEndZ)
        {
            MainRunZone[] layout = CreateDocumentMainRunLayout();
            float currentZ = tutorialEndZ + Mathf.Max(0f, tuning.documentMainRunStartOffset);
            for (int i = 0; i < layout.Length; i++)
            {
                BuildDocumentZone(layout[i], i, currentZ);
                currentZ += GetDocumentZoneLength(layout[i], i)
                    + Mathf.Max(0f, tuning.documentZoneGap);
            }
        }

        private IEnumerator TemporaryBoostSequence(Encounter encounter)
        {
            GameObject pickup = encounter.root;
            ElixirVisual visual = pickup != null ? pickup.GetComponent<ElixirVisual>() : null;
            visual?.BeginConsume();
            // Hide the collected road potion immediately; its invulnerability timer continues independently.
            if (pickup != null) pickup.SetActive(false);
            flashAlpha = Mathf.Max(flashAlpha, elixirPresentation.pickupFlash * 0.8f);
            PunchCamera(0.13f, elixirPresentation.cameraShake * 0.8f, 0f);
            visualTimeScale.RequestSlowMotion(elixirPresentation.slowMotionScale,
                elixirPresentation.slowMotionDuration * 0.7f);

            bool grantsInvulnerability = encounter.temporaryBoostGrantsInvulnerability;
            if (grantsInvulnerability)
            {
                smallPotionInvulnerabilityCount++;
                powerUpNoticeStart = Time.unscaledTime;
                powerUpNoticeUntil = powerUpNoticeStart + 0.9f;
            }

            float boostedSpeed = Mathf.Min(speedController.Settings.maximumSpeed,
                speedController.CurrentSpeed + encounter.temporaryBoostAmount);
            speedController.SetSpeed(boostedSpeed, SpeedChangeReason.PotionPickup, this);

            float timer = 0f;
            float holdDuration = Mathf.Max(0f, encounter.temporaryBoostHoldDuration);
            while (timer < holdDuration)
            {
                timer += Time.unscaledDeltaTime;
                if (speedController.CurrentSpeed < boostedSpeed)
                    speedController.SetSpeed(boostedSpeed, SpeedChangeReason.PotionPickup, this);
                yield return null;
            }

            if (grantsInvulnerability)
                smallPotionInvulnerabilityCount = Mathf.Max(0, smallPotionInvulnerabilityCount - 1);
            if (pickup != null) pickup.SetActive(false);
        }

        private void BuildDocumentZone(MainRunZone zone, int zoneIndex, float startZ)
        {
            float safeHalfWidth = Mathf.Max(0.5f, tuning.laneHalfWidth - 0.35f);
            BuildDocumentLane(zone.left, zoneIndex, 0, startZ, safeHalfWidth);
            BuildDocumentLane(zone.center, zoneIndex, 1, startZ, safeHalfWidth);
            BuildDocumentLane(zone.right, zoneIndex, 2, startZ, safeHalfWidth);
        }

        private void BuildDocumentLane(MainRunLaneContent content, int zoneIndex,
            int laneIndex, float startZ, float safeHalfWidth)
        {
            if (content == null || content.type == MainRunContentType.Empty) return;

            float laneX = GetDocumentLaneX(laneIndex, safeHalfWidth);
            string laneName = laneIndex == 0 ? "Left" : laneIndex == 1 ? "Center" : "Right";
            string zoneName = "DocumentZone_" + (zoneIndex + 1) + "_" + laneName;
            switch (content.type)
            {
                case MainRunContentType.Soldiers:
                    CreateDocumentSoldierChain(laneX, startZ, content.count, zoneIndex, zoneName);
                    break;
                case MainRunContentType.StoneWall:
                    CreateBreakableWall(startZ, zoneName + "_StoneWall",
                        GetSingleLaneWallMode(laneIndex));
                    break;
                case MainRunContentType.Elite:
                    CreateTarget(laneX, startZ,
                        Mathf.Clamp(tuning.documentEliteLevel, 1, speedController.MaxLevel),
                        null, zoneName + "_Elite", true);
                    break;
                case MainRunContentType.SmallPotion:
                    CreateTemporaryBoostPickup(laneX, startZ, 0f,
                        tuning.documentSmallPotionHold,
                        prefab != null ? prefab.openingElixirCenterPrefab : null,
                        zoneName + "_SmallPotion", true);
                    break;
                case MainRunContentType.LargePotion:
                    CreateTemporaryBoostPickup(laneX, startZ, tuning.documentLargePotionBoost,
                        tuning.documentLargePotionHold,
                        prefab != null ? prefab.bossMaxSpeedElixirPrefab : null,
                        zoneName + "_LargePotion", false);
                    break;
            }
        }

        private float GetDocumentZoneLength(MainRunZone zone, int zoneIndex)
        {
            return Mathf.Max(
                GetDocumentLaneLength(zone.left, zoneIndex),
                Mathf.Max(GetDocumentLaneLength(zone.center, zoneIndex),
                    GetDocumentLaneLength(zone.right, zoneIndex)));
        }

        private float GetDocumentLaneLength(MainRunLaneContent content, int zoneIndex)
        {
            if (content == null || content.type == MainRunContentType.Empty) return 0f;
            if (content.type == MainRunContentType.Soldiers)
            {
                float spacing = GetDocumentSoldierSpacing(zoneIndex);
                return content.count > 1
                    ? (content.count - 1) * spacing
                    : spacing;
            }
            return Mathf.Max(0.1f, tuning.documentSingleContentLength);
        }

        private float GetDocumentSoldierSpacing(int zoneIndex)
        {
            if (prefab != null && prefab.soldierSections != null
                && prefab.soldierSections.Length > 0)
            {
                int templateIndex = zoneIndex < 6
                    ? Mathf.Min(zoneIndex, prefab.soldierSections.Length - 1)
                    : Mathf.Min(6, prefab.soldierSections.Length - 1);
                SoldierFormationSettings template = prefab.soldierSections[templateIndex];
                if (template != null) return GetSectionForwardSpacing(template);
            }
            return 0.5f;
        }

        private float GetDocumentLaneX(int laneIndex, float safeHalfWidth)
        {
            return GetSoldierLaneCenter(laneIndex == 0
                ? SoldierPlacementMode.LeftLaneLine
                : laneIndex == 1 ? SoldierPlacementMode.CenterLaneLine
                : SoldierPlacementMode.RightLaneLine, safeHalfWidth);
        }

        private static StoneWallBlockingMode GetSingleLaneWallMode(int laneIndex)
        {
            return laneIndex == 0 ? StoneWallBlockingMode.LeftLaneOnly
                : laneIndex == 2 ? StoneWallBlockingMode.RightLaneOnly
                : StoneWallBlockingMode.CenterLaneOnly;
        }

        private void CreateDocumentSoldierChain(float laneX, float startZ, int count,
            int zoneIndex, string zoneName)
        {
            int clampedCount = Mathf.Clamp(count, 1, 50);
            float spacing = GetDocumentSoldierSpacing(zoneIndex);
            for (int i = 0; i < clampedCount; i++)
            {
                CreateTarget(laneX, startZ + i * spacing, 1, null,
                    zoneName + "_Soldier_" + (i + 1), i == 0);
            }
        }

        // The main RandomDense setting counts rows and expands across lanes; this reward path
        // intentionally uses the configured exact total count per section.
        private IEnumerator BuildRewardStage()
        {
            if (rewardStageBuilt) yield break;
            rewardStageBuilt = true;

            int sectionCount = Mathf.Clamp(
                rewardRun != null ? rewardRun.soldierSectionCount : 10, 1, 20);
            int wallCount = Mathf.Clamp(
                rewardRun != null ? rewardRun.stoneWallCount : 10, 1, 20);
            int soldiersPerSection = Mathf.Clamp(
                rewardRun != null ? rewardRun.soldiersPerSection : 40, 1, 50);
            float rewardLength = Mathf.Max(20f, rewardRun != null ? rewardRun.length : 200f);
            float soldierInterval = rewardLength / sectionCount;
            float wallInterval = rewardLength / wallCount;

            int totalSteps = Mathf.Max(sectionCount, wallCount);
            for (int i = 0; i < totalSteps; i++)
            {
                if (i < sectionCount)
                {
                    float sectionStart = RewardRunStartZ + soldierInterval * (i + 0.04f);
                    yield return StartCoroutine(CreateRewardSoldierSection(i, sectionStart,
                        soldiersPerSection, soldierInterval * 0.46f));
                    yield return null;
                }

                if (i < wallCount)
                {
                    float wallZ = RewardRunStartZ + wallInterval * (i + 0.72f);
                    CreateRewardStoneWall(wallZ, i + 1);
                    yield return null;
                }
            }
        }

        private void CreateRewardStoneWall(float z, int index)
        {
            int encounterCountBeforeCreate = encounters.Count;
            CreateBreakableWall(z, "RewardStoneWall_" + index,
                StoneWallBlockingMode.AllThreeLanes);
            if (encounters.Count <= encounterCountBeforeCreate) return;

            GameObject root = encounters[encounterCountBeforeCreate].root;
            if (root == null) return;
            root.SetActive(false);
            rewardStageWallRoots.Add(root);
        }

        private IEnumerator CreateRewardSoldierSection(int sectionIndex, float startZ,
            int totalSoldiers, float maximumSectionLength)
        {
            int count = Mathf.Clamp(totalSoldiers, 1, 50);
            GameObject sectionObject = new GameObject("RewardSoldierSection_" + (sectionIndex + 1));
            sectionObject.transform.SetParent(worldRoot, false);

            float safeHalfWidth = Mathf.Max(0.5f, tuning.laneHalfWidth - 0.35f);
            float requestedSpacing = rewardRun != null ? Mathf.Max(0.02f, rewardRun.soldierSpacing) : 0.22f;
            float spacing = count > 1
                ? Mathf.Min(requestedSpacing, Mathf.Max(0.02f, maximumSectionLength / (count - 1)))
                : requestedSpacing;
            float sectionLength = count > 1 ? spacing * (count - 1) : 0f;
            System.Random random = new System.Random(
                GetSoldierSectionSeed(tuning.proceduralSeed, 1000 + sectionIndex));

            for (int soldierIndex = 0; soldierIndex < count; soldierIndex++)
            {
                float normalizedX = (float)random.NextDouble();
                float x = Mathf.Lerp(-safeHalfWidth, safeHalfWidth, normalizedX);
                float jitter = soldierIndex == 0 || soldierIndex == count - 1
                    ? 0f
                    : ((float)random.NextDouble() * 2f - 1f) * spacing * 0.24f;
                float z = startZ + Mathf.Clamp(soldierIndex * spacing + jitter, 0f, sectionLength);
                CreateTarget(x, z, 1, sectionObject.transform,
                    "RewardSoldier_" + (sectionIndex + 1) + "_" + (soldierIndex + 1),
                    soldierIndex == 0);
                if ((soldierIndex + 1) % 8 == 0)
                    yield return null;
            }
        }

        private void BuildConfiguredMainRun(float tutorialEndZ)
        {
            if (tuning != null && tuning.useDocumentMainRunLayout)
            {
                BuildDocumentMainRun(tutorialEndZ);
                return;
            }

            if (prefab != null && prefab.soldierSections != null)
            {
                for (int i = 0; i < prefab.soldierSections.Length; i++)
                    CreateSoldierSection(tutorialEndZ, prefab.soldierSections[i], i);
            }

            if (prefab != null && prefab.additionalStoneWalls != null)
            {
                for (int i = 0; i < prefab.additionalStoneWalls.Length; i++)
                {
                    StoneWallSectionSettings section = prefab.additionalStoneWalls[i];
                    if (section == null) continue;
                    CreateBreakableWall(tutorialEndZ + section.startOffsetFromTutorial,
                        "StoneWallSection_" + (i + 1) + "_" + section.sectionName,
                        section.blockingMode, section.bulletTime);
                }
            }

        }

        private void CreateSoldierSection(float tutorialEndZ, SoldierFormationSettings section, int sectionIndex)
        {
            if (section == null) return;
            GameObject sectionObject = new GameObject("SoldierSection_" + (sectionIndex + 1) + "_" + section.sectionName);
            sectionObject.transform.SetParent(worldRoot, false);
            float startZ = tutorialEndZ + section.startOffsetFromTutorial;
            int densityRows = Mathf.Clamp(section.soldierCount, 1, 50);
            bool useStraightLane = section.placementMode != SoldierPlacementMode.RandomDense;
            int totalSoldiers = useStraightLane ? densityRows : densityRows * 3;
            float spacing = GetSectionForwardSpacing(section);
            float sectionLength = Mathf.Max(spacing, (densityRows - 1) * spacing);
            float longitudinalStep = sectionLength / Mathf.Max(1, totalSoldiers - 1);
            float safeHalfWidth = Mathf.Max(0.5f, tuning.laneHalfWidth - 0.35f);
            float coverageHalfWidth = safeHalfWidth * Mathf.Clamp(section.horizontalCoverage, 0.4f, 1f);
            float forwardJitter = Mathf.Clamp(section.forwardRandomness, 0f, 0.9f);
            System.Random random = new System.Random(GetSoldierSectionSeed(tuning.proceduralSeed, sectionIndex));
            float laneCenterX = GetSoldierLaneCenter(section.placementMode, safeHalfWidth);

            for (int soldierIndex = 0; soldierIndex < totalSoldiers; soldierIndex++)
            {
                float x;
                float z;
                if (useStraightLane)
                {
                    x = laneCenterX;
                    z = soldierIndex * spacing;
                }
                else
                {
                    x = Mathf.Lerp(-coverageHalfWidth, coverageHalfWidth, (float)random.NextDouble());
                    float baseZ = soldierIndex * longitudinalStep;
                    float jitter = ((float)random.NextDouble() * 2f - 1f) * longitudinalStep * forwardJitter;
                    z = Mathf.Clamp(baseZ + jitter, 0f, sectionLength);
                }

                CreateTarget(x, startZ + z, 1, sectionObject.transform,
                    "Soldier_L1_" + section.placementMode + "_" + (soldierIndex + 1),
                    soldierIndex == 0);
            }
        }

        private float GetSoldierLaneCenter(SoldierPlacementMode placementMode, float safeHalfWidth)
        {
            float laneCenterOffset = Mathf.Clamp(tuning.laneHalfWidth * (2f / 3f), 0f, safeHalfWidth);
            switch (placementMode)
            {
                case SoldierPlacementMode.LeftLaneLine:
                    return -laneCenterOffset;
                case SoldierPlacementMode.RightLaneLine:
                    return laneCenterOffset;
                default:
                    return 0f;
            }
        }

        private static int GetSoldierSectionSeed(int seed, int sectionIndex)
        {
            unchecked
            {
                return seed * 486187739 + (sectionIndex + 1) * 16777619;
            }
        }

        private static float GetSectionForwardSpacing(SoldierFormationSettings section)
        {
            return Mathf.Max(0.1f, section.minimumForwardSpacing);
        }

        private void CreateTarget(float x, float z, int tier, Transform parent = null,
            string objectName = "Target", bool showLevelLabel = true)
        {
            Vector3 dimensions = targetShapes.Get(tier);
            Color color = speedVisualProfile.Get(tier).primaryColor;
            GameObject root = CreateBox(objectName, new Vector3(x, dimensions.y * 0.5f, z), dimensions, color, parent != null ? parent : worldRoot);
            Renderer placeholderRenderer = root.GetComponent<Renderer>();
            ObstacleController obstacle = root.AddComponent<ObstacleController>();
            Collider[] colliders = root.GetComponents<Collider>();
            obstacle.Initialize(tier, ObstacleType.Soldier, colliders);

            Renderer[] visibilityRenderers = placeholderRenderer != null
                ? new[] { placeholderRenderer }
                : Array.Empty<Renderer>();
            GameObject visualPrefab = tier == 1 ? tier1SoldierPrefab : tier >= 4 ? tier4SoldierPrefab : null;
            if (visualPrefab != null)
            {
                Transform visualRoot = new GameObject("VisualRoot").transform;
                visualRoot.SetParent(root.transform, false);
                visualRoot.localPosition = new Vector3(0f, -0.5f, 0f);
                visualRoot.localScale = new Vector3(
                    1f / Mathf.Max(0.001f, dimensions.x),
                    1f / Mathf.Max(0.001f, dimensions.y),
                    1f / Mathf.Max(0.001f, dimensions.z));

                GameObject visual = Instantiate(visualPrefab, visualRoot, false);
                visual.name = visualPrefab.name;
                SanitizeTargetVisual(visual);
                Renderer[] modelRenderers = visual.GetComponentsInChildren<Renderer>(true);
                if (modelRenderers.Length > 0)
                {
                    float targetHeight = tier == 1 ? tier1TargetHeight : tier4TargetHeight;
                    FitTargetVisual(visual, visualRoot, modelRenderers, targetHeight);
                    ConfigureTargetCollider(root.GetComponent<BoxCollider>(), dimensions, modelRenderers, targetHeight);

                    if (placeholderRenderer != null) Destroy(placeholderRenderer);
                    MeshFilter placeholderMesh = root.GetComponent<MeshFilter>();
                    if (placeholderMesh != null) Destroy(placeholderMesh);
                    visibilityRenderers = modelRenderers;
                }
                else
                {
                    Destroy(visualRoot.gameObject);
                    WarnTargetVisualFallback(tier, visualPrefab.name + " has no Renderer");
                }
            }
            else if (tier == 1 || tier >= 4)
            {
                WarnTargetVisualFallback(tier, tier == 1 ? nameof(tier1SoldierPrefab) : nameof(tier4SoldierPrefab));
            }

            EnemyVisibilityController visibility = root.AddComponent<EnemyVisibilityController>();
            SoldierKnockbackEffect soldierKnockback = root.AddComponent<SoldierKnockbackEffect>();
            soldierKnockback.Initialize(visibilityRenderers);
            int displayLevel = tier >= 4 ? tier : SoldierDisplayLevel;
            NumberCombatTarget numberTarget = showLevelLabel
                ? numberCombatSystem?.RegisterTarget(root.transform, visibilityRenderers,
                    displayLevel, numberCombat.soldierHeadClearance, visibility)
                : null;
            visibility.Initialize(visibilityRenderers, colliders);
            encounters.Add(new Encounter
            {
                root = root,
                type = EncounterType.Target,
                tier = tier,
                obstacle = obstacle,
                numberTarget = numberTarget,
                visibility = visibility,
                soldierKnockback = soldierKnockback
            });
            root.SetActive(false);
        }

        private static void SanitizeTargetVisual(GameObject visual)
        {
            Collider[] visualColliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < visualColliders.Length; i++) visualColliders[i].enabled = false;

            Rigidbody[] bodies = visual.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].isKinematic = true;
                bodies[i].detectCollisions = false;
            }

            Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++) animators[i].applyRootMotion = false;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }
        }

        private static void FitTargetVisual(GameObject visual, Transform groundReference, Renderer[] renderers,
            float targetHeight)
        {
            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.Rebind();
                animator.Update(0f);
            }

            Bounds bounds = CalculateRendererBounds(renderers);
            float scaleFactor = targetHeight / Mathf.Max(0.001f, bounds.size.y);
            visual.transform.localScale *= scaleFactor;
            if (animator != null) animator.Update(0f);

            bounds = CalculateRendererBounds(renderers);
            visual.transform.position += Vector3.up * (groundReference.position.y - bounds.min.y);
        }

        private static void ConfigureTargetCollider(BoxCollider collider, Vector3 rootDimensions,
            Renderer[] renderers, float targetHeight)
        {
            if (collider == null) return;
            Bounds bounds = CalculateRendererBounds(renderers);
            Vector3 worldSize = new Vector3(
                bounds.size.x * 0.8f,
                targetHeight * 0.9f,
                bounds.size.z * 0.8f);
            collider.size = new Vector3(
                worldSize.x / Mathf.Max(0.001f, rootDimensions.x),
                worldSize.y / Mathf.Max(0.001f, rootDimensions.y),
                worldSize.z / Mathf.Max(0.001f, rootDimensions.z));
            collider.center = new Vector3(0f,
                (worldSize.y * 0.5f - rootDimensions.y * 0.5f) / Mathf.Max(0.001f, rootDimensions.y),
                0f);
        }

        private static Bounds CalculateRendererBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void WarnTargetVisualFallback(int tier, string missingReference)
        {
            if (!targetVisualFallbackWarnings.Add(tier)) return;
            Debug.LogWarning("Target visual fallback: tier " + tier + " is using the procedural cube because "
                + missingReference + " is missing or invalid.", this);
        }

        private void CreateRoadBoundary(string name, float x)
        {
            float courseEndZ = CourseEndZ;
            GameObject boundary = new GameObject(name);
            boundary.transform.SetParent(worldRoot, false);
            boundary.transform.position = new Vector3(x, environment.roadBoundaryHeight * 0.5f, courseEndZ * 0.5f);
            BoxCollider collider = boundary.AddComponent<BoxCollider>();
            collider.size = new Vector3(
                environment.roadBoundaryThickness,
                environment.roadBoundaryHeight,
                courseEndZ + 28f);
        }

        private void CreateElixir(float x, float z, int targetLevel = 0,
            int exclusivePickupGroupId = 0, GameObject visualPrefab = null,
            string objectNameOverride = null)
        {
            int resolvedTargetLevel = targetLevel > 0 ? Mathf.Clamp(targetLevel, 1, speedController.MaxLevel) : playerSpeed.tutorialElixirTargetLevel;
            string objectName = !string.IsNullOrEmpty(objectNameOverride)
                ? objectNameOverride
                : "RoyalElixir";
            if (exclusivePickupGroupId > 0 && string.IsNullOrEmpty(objectNameOverride))
                objectName = x < -0.01f ? "OpeningElixir_Left"
                    : x > 0.01f ? "OpeningElixir_Right" : "OpeningElixir_Center";
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(x, 0.9f, z);

            Renderer[] renderers = CreateElixirVisual(root, visualPrefab, resolvedTargetLevel);
            root.AddComponent<ElixirVisual>().Initialize(elixirPresentation, renderers, speedVisualProfile, resolvedTargetLevel);
            SphereCollider pickupCollider = root.AddComponent<SphereCollider>();
            pickupCollider.isTrigger = true;
            pickupCollider.radius = 1.1f;
            ElixirPickup pickup = root.AddComponent<ElixirPickup>();
            pickup.Initialize(speedController, resolvedTargetLevel, new Collider[] { pickupCollider });
            encounters.Add(new Encounter
            {
                root = root,
                type = EncounterType.Elixir,
                tier = resolvedTargetLevel,
                exclusivePickupGroupId = exclusivePickupGroupId,
                elixir = pickup
            });
            root.SetActive(false);
        }

        private void CreateTemporaryBoostPickup(float x, float z, float boostAmount,
            float holdDuration, GameObject visualPrefab, string objectName,
            bool grantsInvulnerability)
        {
            int encounterCountBeforeCreate = encounters.Count;
            CreateElixir(x, z, 1, 0, visualPrefab, objectName);
            if (encounters.Count <= encounterCountBeforeCreate) return;

            Encounter encounter = encounters[encounterCountBeforeCreate];
            encounter.temporaryBoostAmount = Mathf.Max(0f, boostAmount);
            encounter.temporaryBoostHoldDuration = Mathf.Max(0f, holdDuration);
            encounter.temporaryBoostGrantsInvulnerability = grantsInvulnerability;
        }

        private Renderer[] CreateElixirVisual(GameObject root, GameObject visualPrefab, int targetLevel)
        {
            if (visualPrefab != null)
            {
                GameObject visual = Instantiate(visualPrefab, root.transform, false);
                visual.name = visualPrefab.name;
                Collider[] visualColliders = visual.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < visualColliders.Length; i++) visualColliders[i].enabled = false;

                Rigidbody[] visualBodies = visual.GetComponentsInChildren<Rigidbody>(true);
                for (int i = 0; i < visualBodies.Length; i++)
                {
                    visualBodies[i].isKinematic = true;
                    visualBodies[i].detectCollisions = false;
                }

                Renderer[] importedRenderers = visual.GetComponentsInChildren<Renderer>(true);
                if (importedRenderers.Length > 0)
                {
                    FitElixirVisual(root.transform, visual.transform, importedRenderers);
                    return importedRenderers;
                }

                Destroy(visual);
            }

            return CreateProceduralElixirVisual(root, targetLevel);
        }

        private static void FitElixirVisual(Transform root, Transform visual, Renderer[] renderers)
        {
            Bounds bounds = CalculateRendererBounds(renderers);
            float scaleFactor = ElixirVisualTargetHeight / Mathf.Max(0.001f, bounds.size.y);
            visual.localScale *= scaleFactor;

            bounds = CalculateRendererBounds(renderers);
            Vector3 rootPosition = root.position;
            visual.position += new Vector3(
                rootPosition.x - bounds.center.x,
                rootPosition.y + ElixirVisualBottomOffset - bounds.min.y,
                rootPosition.z - bounds.center.z);
        }

        private Renderer[] CreateProceduralElixirVisual(GameObject root, int targetLevel)
        {
            GameObject bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottle.transform.SetParent(root.transform, false);
            bottle.transform.localScale = new Vector3(0.42f, 0.72f, 0.42f);
            SpeedTierVisualData elixirTier = speedVisualProfile.Get(targetLevel);
            bottle.GetComponent<Renderer>().sharedMaterial = RuntimeStyle.CreateMaterial(elixirTier.primaryColor, 0.15f, 0.9f);
            Destroy(bottle.GetComponent<Collider>());

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.transform.SetParent(root.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            cap.transform.localScale = new Vector3(0.32f, 0.2f, 0.32f);
            cap.GetComponent<Renderer>().sharedMaterial = RuntimeStyle.CreateMaterial(elixirTier.secondaryColor, 0.4f, 0.8f);
            Destroy(cap.GetComponent<Collider>());

            return new[] { bottle.GetComponent<Renderer>(), cap.GetComponent<Renderer>() };
        }

        private void CreateBreakableWall(float z, string objectName = "TutorialStoneWall",
            StoneWallBlockingMode blockingMode = StoneWallBlockingMode.AllThreeLanes,
            BulletTimeSettings bulletTimeSettings = null, bool completesTutorial = false)
        {
            GameObject root;
            GameObject configuredWallPrefab = prefab != null ? prefab.stoneWallPrefab : null;
            if (configuredWallPrefab != null)
            {
                root = Instantiate(configuredWallPrefab, worldRoot, false);
            }
            else
            {
                root = new GameObject(objectName);
                root.transform.SetParent(worldRoot, false);
            }

            root.name = objectName;
            GetStoneWallPlacement(blockingMode, out float centerX, out float halfWidth, out float widthScale);
            root.transform.position = new Vector3(centerX, 0f, z);
            Vector3 rootScale = root.transform.localScale;
            rootScale.x *= widthScale;
            root.transform.localScale = rootScale;
            BreakableWallVisual wall = root.GetComponent<BreakableWallVisual>();
            if (wall == null) wall = root.AddComponent<BreakableWallVisual>();
            wall.Initialize(wallBreakPresentation, new Color(0.4f, 0.43f, 0.44f), speedVisualProfile, visualPerformance);
            ObstacleController obstacle = root.GetComponent<ObstacleController>();
            if (obstacle == null) obstacle = root.AddComponent<ObstacleController>();
            int requiredSpeedLevel = Mathf.Clamp(tuning.stoneWallSafeSpeedLevel, 1, speedController.MaxLevel);
            obstacle.Initialize(requiredSpeedLevel, ObstacleType.StoneWall,
                root.GetComponentsInChildren<Collider>(true));
            NumberCombatTarget numberTarget = numberCombatSystem?.RegisterTarget(root.transform,
                root.GetComponentsInChildren<Renderer>(true), requiredSpeedLevel,
                numberCombat.stoneWallHeadClearance);
            encounters.Add(new Encounter
            {
                root = root,
                type = EncounterType.Wall,
                tier = requiredSpeedLevel,
                wall = wall,
                obstacle = obstacle,
                numberTarget = numberTarget,
                wallCenterX = centerX,
                wallHalfWidth = halfWidth,
                bulletTimeSettings = bulletTimeSettings != null ? bulletTimeSettings.Clone() : null,
                completesTutorial = completesTutorial
            });
        }

        private void GetStoneWallPlacement(StoneWallBlockingMode blockingMode, out float centerX,
            out float halfWidth, out float widthScale)
        {
            float roadHalfWidth = Mathf.Max(0.5f, tuning.laneHalfWidth);
            if (blockingMode == StoneWallBlockingMode.AllThreeLanes)
            {
                centerX = 0f;
                halfWidth = roadHalfWidth;
                widthScale = 1f;
                return;
            }

            if (blockingMode == StoneWallBlockingMode.LeftLaneOnly
                || blockingMode == StoneWallBlockingMode.RightLaneOnly
                || blockingMode == StoneWallBlockingMode.CenterLaneOnly)
            {
                centerX = blockingMode == StoneWallBlockingMode.LeftLaneOnly
                    ? -roadHalfWidth * 2f / 3f
                    : blockingMode == StoneWallBlockingMode.RightLaneOnly
                    ? roadHalfWidth * 2f / 3f
                    : 0f;
                halfWidth = roadHalfWidth / 3f;
                widthScale = 1f / 3f;
                return;
            }

            centerX = blockingMode == StoneWallBlockingMode.LeftAndCenter
                ? -roadHalfWidth / 3f
                : roadHalfWidth / 3f;
            halfWidth = roadHalfWidth * 2f / 3f;
            widthScale = 2f / 3f;
        }

        private void BuildInvulnerabilityCircle(Transform parent)
        {
            GameObject ringObject = new GameObject("InvulnerabilityEnergyRing");
            ringObject.transform.SetParent(parent, false);
            ringObject.transform.localPosition = new Vector3(0f, -0.85f, 0f);
            upgradeRing = ringObject.AddComponent<LineRenderer>();
            upgradeRing.useWorldSpace = false;
            upgradeRing.loop = true;
            upgradeRing.positionCount = 36;
            upgradeRing.startWidth = 0.09f;
            upgradeRing.endWidth = 0.09f;
            upgradeRing.sharedMaterial = speedVisualProfile.lineMaterial != null
                ? speedVisualProfile.lineMaterial
                : RuntimeStyle.CreateMaterial(Color.white, 0f, 0.1f);
            for (int i = 0; i < 36; i++)
            {
                float angle = i / 36f * Mathf.PI * 2f;
                upgradeRing.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }
            upgradeRing.enabled = false;

            if (upgradeMagicCircleTexture == null) return;
            upgradeMagicCircleSprite = Sprite.Create(upgradeMagicCircleTexture,
                new Rect(0f, 0f, upgradeMagicCircleTexture.width, upgradeMagicCircleTexture.height),
                new Vector2(0.5f, 0.5f), Mathf.Max(upgradeMagicCircleTexture.width, upgradeMagicCircleTexture.height) * 0.5f,
                0, SpriteMeshType.FullRect);
            upgradeMagicCircleSprite.name = "RuntimeInvulnerabilityMagicCircle";
            upgradeMagicCircleGlow = BuildUpgradeMagicCircleLayer(
                "InvulnerabilityMagicCircleGlow", parent, upgradeMagicCircleSprite, -0.84f, -1);
            upgradeMagicCircle = BuildUpgradeMagicCircleLayer(
                "InvulnerabilityMagicCircleRunes", parent, upgradeMagicCircleSprite, -0.82f, 0);
        }

        private static SpriteRenderer BuildUpgradeMagicCircleLayer(
            string objectName, Transform parent, Sprite sprite, float localY, int sortingOrder)
        {
            GameObject layer = new GameObject(objectName);
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = new Vector3(0f, localY, 0f);
            layer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            layer.transform.localScale = Vector3.one * 0.05f;
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }

        private GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color, Transform parent)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            Renderer renderer = box.GetComponent<Renderer>();
            renderer.sharedMaterial = RuntimeStyle.CreateMaterial(color, 0f, 0.38f);
            return box;
        }


        private void SpawnImpactBurst(Vector3 position, Color color)
        {
            effectPool?.PlayImpact(position, color, 1f, CurrentTier);
        }

        private void OnGUI()
        {
            if (!gameplayStarted)
            {
                return;
            }

            EnsureGuiStyles();
            float scale = Mathf.Max(0.45f, Mathf.Min(Screen.width / 540f, Screen.height / 960f));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (smallPotionGoldOverlayAlpha > 0.001f)
            {
                DrawRect(new Rect(0f, 0f, width, height),
                    new Color(1f, 0.68f, 0.08f, smallPotionGoldOverlayAlpha));
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (showSpeedDebugOverlay && speedController != null)
            {
                float runnerZ = runner != null ? runner.position.z : 0f;
                int speedLossSection = GetSpeedLossSectionIndex(runnerZ);
                float currentSpeedLoss = GetSpeedLossPerSecond(runnerZ);
                string diagnostics = "Speed " + speedController.CurrentSpeed.ToString("F2") + "  Level " + speedController.GetCurrentLevel() + "/" + speedController.MaxLevel
                    + "\nTarget " + TargetForwardSpeed.ToString("F2") + " m/s  Actual " + CurrentForwardSpeed.ToString("F2") + " m/s"
                    + "\nFOV " + (gameCamera != null ? gameCamera.fieldOfView.ToString("F1") : "-") + "  Anim " + CurrentAnimationSpeed.ToString("F2")
                    + "  Wind " + (audioFeedback != null ? audioFeedback.CurrentWindVolume.ToString("F2") : "-")
                    + "\nLast " + speedController.LastSpeedChangeReason + " @ " + speedController.LastSpeedChangeTime.ToString("F2")
                    + "  SpeedLoss " + tuning.forwardSpeedLossEnabled + " S" + speedLossSection
                    + " @ " + currentSpeedLoss.ToString("F2") + "/s"
                    + "\n[1-0] Level  [T] 100m test  Last " + (debugLastSegmentTime > 0f ? debugLastSegmentTime.ToString("F2") + "s" : "-");
                GUI.Box(new Rect(width - 315f, 18f, 300f, 130f), diagnostics);
            }
#endif

            BulletTimeManager bulletTimeManager = BulletTimeManager.Instance;
            bool showTutorialBulletWarning = tutorialBulletTimeWarningActive
                && bulletTimeManager != null
                && bulletTimeManager.IsBulletTime();
            if (showTutorialBulletWarning)
            {
                DrawTutorialBulletTimeWarning(width, height);
            }
            else if (!ending && elapsed < calloutUntil)
            {
                if (callout == "SMASH\nSPEED UP")
                {
                    DrawSmashCallout(width, height);
                }
                else
                {
                    GUI.Label(new Rect(20f, height * 0.18f, width - 40f, 65f), callout, titleStyle);
                }
            }

            if (!FormalStarted && !ending)
            {
                GUI.Label(new Rect(20f, height - 108f, width - 40f, 45f), "TRAINING - NO SPEED LOSS", smallStyle);
            }
            else if (!ending && !bossSequence)
            {
                GUI.Label(new Rect(20f, height - 108f, width - 40f, 45f), "KEEP MOMENTUM", smallStyle);
            }

            if (!ending)
            {
                DrawPowerUpNotice(width, height);
            }

            if (ending)
            {
                DrawEndCard(width, height);
            }

            if (flashAlpha > 0.001f)
            {
                DrawRect(new Rect(0f, 0f, width, height), new Color(1f, 0.86f, 0.4f, flashAlpha));
            }

            DrawPenaltyEdgeFeedback(width, height);
        }

        private void DrawEndCard(float width, float height)
        {
            DrawRect(new Rect(0f, 0f, width, height), new Color(0.02f, 0.03f, 0.04f, 0.82f));

            if (endCardGameLogo != null)
            {
                Rect logoBounds = new Rect(width * 0.07f, height * 0.035f,
                    width * 0.86f, height * 0.27f);
                Rect logoRect = FitTextureInBounds(endCardGameLogo, logoBounds);
                GUI.DrawTexture(logoRect, endCardGameLogo, ScaleMode.StretchToFill, true);

                if (endCardVictoryLogo != null)
                {
                    float victorySize = Mathf.Min(width * 0.8232f, height * 0.4508f);
                    float victoryY = logoRect.yMax + height * 0.025f;
                    Rect victoryRect = new Rect((width - victorySize) * 0.5f,
                        victoryY, victorySize, victorySize);
                    GUI.DrawTexture(victoryRect, endCardVictoryLogo,
                        ScaleMode.StretchToFill, true);
                }
            }
            else if (endCardVictoryLogo != null)
            {
                float victorySize = Mathf.Min(width * 0.8232f, height * 0.4508f);
                Rect victoryRect = new Rect((width - victorySize) * 0.5f,
                    height * 0.17f, victorySize, victorySize);
                GUI.DrawTexture(victoryRect, endCardVictoryLogo,
                    ScaleMode.StretchToFill, true);
            }

            if (endCardPlayNowImage == null) return;

            Rect ctaBounds = new Rect(width * 0.045f, height * 0.55f,
                width * 0.91f, height * 0.4f);
            Rect ctaBaseRect = FitTextureInBounds(endCardPlayNowImage, ctaBounds);
            float ctaPulse = 1f + Mathf.Sin(Time.unscaledTime * 3.8f) * 0.055f;
            Rect ctaRect = ScaleRectFromCenter(ctaBaseRect, ctaPulse);
            GUI.DrawTexture(ctaRect, endCardPlayNowImage,
                ScaleMode.StretchToFill, true);

            if (endCardTapHand != null)
            {
                float tapPhase = (Mathf.Sin(Time.unscaledTime * 6.4f) + 1f) * 0.5f;
                float handHeight = Mathf.Min(height * 0.15f, 142f);
                float handAspect = endCardTapHand.width / (float)Mathf.Max(1, endCardTapHand.height);
                float handWidth = handHeight * handAspect;
                float handScale = 1f - tapPhase * 0.055f;
                handWidth *= handScale;
                handHeight *= handScale;
                Rect handRect = new Rect(
                    ctaRect.x + ctaRect.width * 0.69f - handWidth * 0.5f,
                    ctaRect.y + ctaRect.height * 0.42f + tapPhase * 9f,
                    handWidth,
                    handHeight);
                GUI.DrawTexture(handRect, endCardTapHand,
                    ScaleMode.StretchToFill, true);
            }

            Rect clickRect = ctaRect;
            if (GUI.Button(clickRect, GUIContent.none, GUIStyle.none))
                HandleEndCardClick();
        }

        private void HandleEndCardClick()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.name))
                SceneManager.LoadScene(activeScene.name);
        }

        private void SetGameplayHudVisible(bool visible)
        {
            if (speedBarView != null)
                speedBarView.gameObject.SetActive(visible);
            numberCombatSystem?.SetVisible(visible);
            if (!visible)
                bossTapPromptView?.Hide(true);
        }

        private static Rect FitTextureInBounds(Texture texture, Rect bounds)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
                return bounds;

            float textureAspect = texture.width / (float)texture.height;
            float boundsAspect = bounds.width / Mathf.Max(0.001f, bounds.height);
            if (textureAspect >= boundsAspect)
            {
                float fittedHeight = bounds.width / textureAspect;
                return new Rect(bounds.x, bounds.center.y - fittedHeight * 0.5f,
                    bounds.width, fittedHeight);
            }

            float fittedWidth = bounds.height * textureAspect;
            return new Rect(bounds.center.x - fittedWidth * 0.5f, bounds.y,
                fittedWidth, bounds.height);
        }

        private static Rect ScaleRectFromCenter(Rect rect, float scale)
        {
            float scaledWidth = rect.width * scale;
            float scaledHeight = rect.height * scale;
            return new Rect(rect.center.x - scaledWidth * 0.5f,
                rect.center.y - scaledHeight * 0.5f, scaledWidth, scaledHeight);
        }

        private void DrawPenaltyEdgeFeedback(float width, float height)
        {
            if (penaltyEdgeIntensity <= 0.001f || penaltyEdgeTexture == null) return;

            Color previous = GUI.color;
            Color edgeColor = impactPresentation != null
                ? impactPresentation.penaltyEdgeColor
                : new Color(0.92f, 0.02f, 0.015f, 1f);
            float opacity = impactPresentation != null ? impactPresentation.penaltyEdgeOpacity : 0.62f;
            edgeColor.a *= Mathf.Clamp01(opacity * penaltyEdgeIntensity);
            GUI.color = edgeColor;
            GUI.DrawTexture(new Rect(0f, 0f, width, height), penaltyEdgeTexture,
                ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        private void DrawSmashCallout(float width, float height)
        {
            float now = Time.unscaledTime;
            float effectProgress = smashEffectUntil > smashEffectStart
                ? Mathf.Clamp01((now - smashEffectStart) / (smashEffectUntil - smashEffectStart))
                : 1f;
            float remainingEffect = 1f - effectProgress;
            float jitterX = Mathf.Sin(now * 92f) * 7f * remainingEffect;
            float jitterY = Mathf.Cos(now * 117f) * 4f * remainingEffect;

            if (remainingEffect > 0f)
            {
                for (int i = 3; i >= 1; i--)
                {
                    float ghostOffset = i * 5f * remainingEffect;
                    float ghostAlpha = 0.08f * remainingEffect * (4 - i);
                    DrawSmashText(width, height,
                        new Vector2(jitterX - ghostOffset, jitterY + i * 2f), ghostAlpha,
                        new Color(1f, 0.72f, 0.3f, 1f));
                }
            }

            DrawSmashText(width, height, new Vector2(jitterX, jitterY), 1f, Color.white);
        }

        private void DrawSmashText(float width, float height, Vector2 offset, float alpha, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, alpha);
            float labelWidth = width * 0.84f;
            float labelX = (width - labelWidth) * 0.5f + offset.x;
            GUI.Label(new Rect(labelX, height * 0.23f + offset.y, labelWidth, 68f), "SMASH", smashTitleStyle);
            GUI.Label(new Rect(labelX, height * 0.305f + offset.y, labelWidth, 48f), "↑  SPEED UP  ↑", smashSubtitleStyle);
            GUI.color = previousColor;
        }

        private void DrawPowerUpNotice(float width, float height)
        {
            if (powerUpNoticeUntil <= powerUpNoticeStart || Time.unscaledTime >= powerUpNoticeUntil)
                return;

            float progress = Mathf.Clamp01((Time.unscaledTime - powerUpNoticeStart)
                / (powerUpNoticeUntil - powerUpNoticeStart));
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.16f));
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 1f, progress));
            float pulse = 1f + Mathf.Sin(progress * Mathf.PI * 3f) * 0.06f;
            float alpha = fadeIn * fadeOut;
            if (alpha <= 0.001f) return;

            powerUpStyle.fontSize = Mathf.RoundToInt(46f * pulse);
            powerUpShadowStyle.fontSize = powerUpStyle.fontSize;
            Color previousColor = GUI.color;
            GUI.color = new Color(0.12f, 0.06f, 0.01f, alpha * 0.8f);
            Rect labelRect = new Rect(20f, height * 0.42f + 3f, width - 40f, 64f);
            GUI.Label(labelRect, "POWER UP!", powerUpShadowStyle);
            GUI.color = new Color(1f, 0.86f, 0.28f, alpha);
            GUI.Label(new Rect(labelRect.x, labelRect.y - 3f, labelRect.width, labelRect.height),
                "POWER UP!", powerUpStyle);
            GUI.color = previousColor;
        }

        private void DrawTutorialBulletTimeWarning(float width, float height)
        {
            const string warning = "NOT YET!";
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 6.4f) * 0.1f;
            tutorialBulletWarningStyle.fontSize = Mathf.RoundToInt(58f * pulse);
            tutorialBulletWarningShadowStyle.fontSize = tutorialBulletWarningStyle.fontSize;

            float labelWidth = width * 0.94f;
            float centerX = width * 0.5f + 18f;
            float centerY = height * 0.4f;
            Rect labelRect = new Rect(centerX - labelWidth * 0.5f, centerY - 43f, labelWidth, 86f);

            const float outlineOffset = 2.5f;
            GUI.Label(new Rect(labelRect.x - outlineOffset, labelRect.y, labelRect.width, labelRect.height),
                warning, tutorialBulletWarningShadowStyle);
            GUI.Label(new Rect(labelRect.x + outlineOffset, labelRect.y, labelRect.width, labelRect.height),
                warning, tutorialBulletWarningShadowStyle);
            GUI.Label(new Rect(labelRect.x, labelRect.y - outlineOffset, labelRect.width, labelRect.height),
                warning, tutorialBulletWarningShadowStyle);
            GUI.Label(new Rect(labelRect.x, labelRect.y + outlineOffset, labelRect.width, labelRect.height),
                warning, tutorialBulletWarningShadowStyle);
            GUI.Label(labelRect, warning, tutorialBulletWarningStyle);

            if (tutorialBulletWarningArrow != null)
            {
                float arrowHeight = 64f * pulse;
                float arrowAspect = tutorialBulletWarningArrow.width
                    / (float)Mathf.Max(1, tutorialBulletWarningArrow.height);
                float arrowWidth = arrowHeight * arrowAspect;
                Rect arrowRect = new Rect(centerX - arrowWidth * 0.5f,
                    labelRect.yMax + 4f, arrowWidth, arrowHeight);
                GUI.DrawTextureWithTexCoords(arrowRect, tutorialBulletWarningArrow,
                    new Rect(1f, 0f, -1f, 1f), true);
            }
        }

        private void EnsureGuiStyles()
        {
            if (tutorialBulletWarningStyle == null || tutorialBulletWarningShadowStyle == null)
            {
                tutorialBulletWarningStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 58,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.06f, 0.025f) }
                };
                tutorialBulletWarningShadowStyle = new GUIStyle(tutorialBulletWarningStyle)
                {
                    normal = { textColor = new Color(0.22f, 0f, 0f, 0.96f) }
                };
            }

            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            smallStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.92f, 0.94f) }
            };
            smashTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 56,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            smashSubtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                fontStyle = FontStyle.Normal,
                normal = { textColor = Color.white }
            };
            powerUpStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 46,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.86f, 0.28f, 1f) }
            };
            powerUpShadowStyle = new GUIStyle(powerUpStyle)
            {
                normal = { textColor = new Color(0.12f, 0.06f, 0.01f, 0.8f) }
            };
            buttonNormalTexture = MakeTexture(new Color(0.95f, 0.25f, 0.08f));
            buttonActiveTexture = MakeTexture(new Color(1f, 0.42f, 0.08f));
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 27,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = buttonNormalTexture },
                active = { textColor = Color.white, background = buttonActiveTexture }
            };
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreatePenaltyEdgeTexture(int size, float normalizedWidth)
        {
            int textureSize = Mathf.Max(8, size);
            float edgeWidth = Mathf.Clamp(normalizedWidth, 0.02f, 0.48f);
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Penalty Edge Overlay",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color32[] pixels = new Color32[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                float normalizedY = (y + 0.5f) / textureSize;
                for (int x = 0; x < textureSize; x++)
                {
                    float normalizedX = (x + 0.5f) / textureSize;
                    float horizontalEdgeDistance = Mathf.Min(normalizedX, 1f - normalizedX);
                    float verticalEdgeDistance = Mathf.Min(normalizedY, 1f - normalizedY);
                    float distanceToEdge = Mathf.Min(horizontalEdgeDistance, verticalEdgeDistance);
                    float alpha = 1f - Mathf.SmoothStep(0f, edgeWidth, distanceToEdge);
                    pixels[y * textureSize + x] = new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = previous;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void HandleSpeedDebugInput()
        {
            if (speedController == null) return;
            for (int level = 1; level <= Mathf.Min(9, speedController.MaxLevel); level++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + level)))
                    speedController.SetLevel(level, SpeedChangeReason.DebugCommand, this);
            }
            if (speedController.MaxLevel >= 10 && Input.GetKeyDown(KeyCode.Alpha0))
                speedController.SetLevel(10, SpeedChangeReason.DebugCommand, this);
            if (Input.GetKeyDown(KeyCode.T) && runner != null)
            {
                debugSegmentRecording = true;
                debugSegmentStartZ = runner.position.z;
                debugSegmentElapsed = 0f;
            }
        }

        private void UpdateDebugSegmentTimer()
        {
            if (!debugSegmentRecording || runner == null) return;
            debugSegmentElapsed += Time.deltaTime;
            if (runner.position.z - debugSegmentStartZ < debugTestSegmentLength) return;
            debugLastSegmentTime = debugSegmentElapsed;
            debugSegmentRecording = false;
            Debug.Log("Speed test: level=" + speedController.GetCurrentLevel() + ", distance=" + debugTestSegmentLength.ToString("F0") + "m, time=" + debugLastSegmentTime.ToString("F3") + "s");
        }
#else
        private void UpdateDebugSegmentTimer() { }
#endif

    }
}
