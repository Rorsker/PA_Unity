using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayableAd
{
    public sealed class PlayableAdGame : MonoBehaviour
    {
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
            CenterAndRight
        }

        [Serializable]
        public sealed class SoldierFormationSettings
        {
            [InspectorName("Section Name（区段名称）")] public string sectionName = "Momentum";
            [Min(0f), InspectorName("Start Offset From Tutorial（距教学起始偏移）")] public float startOffsetFromTutorial = 7.38f;
            [Range(6, 50), InspectorName("Density Rows（密度行数）")] public int soldierCount = 10;
            [InspectorName("Placement Mode（摆放模式）")] public SoldierPlacementMode placementMode = SoldierPlacementMode.RandomDense;
            [Range(0.65f, 1.2f), InspectorName("Forward Spacing（前后间距）")] public float minimumForwardSpacing = 0.8f;
            [Range(0.4f, 1f), InspectorName("Horizontal Coverage（横向覆盖范围）")] public float horizontalCoverage = 1f;
            [Range(0f, 0.9f), InspectorName("Forward Randomness（前后随机度）")] public float forwardRandomness = 0.8f;
        }

        [Serializable]
        public sealed class StoneWallSectionSettings
        {
            [InspectorName("Section Name（区段名称）")] public string sectionName = "StoneWall";
            [Min(0f), InspectorName("Start Offset From Tutorial（距教学起始偏移）")] public float startOffsetFromTutorial = 100f;
            [InspectorName("Blocking Mode（阻挡模式）")] public StoneWallBlockingMode blockingMode = StoneWallBlockingMode.AllThreeLanes;
        }

        [Serializable]
        public sealed class PrefabModules
        {
            [Header("Soldier section modules（士兵区段模块）")]
            [InspectorName("Soldier Sections（士兵区段）")] public SoldierFormationSettings[] soldierSections = Array.Empty<SoldierFormationSettings>();

            [Header("Stone wall modules（石墙模块）")]
            [InspectorName("Stone Wall Prefab（石墙预制体）")] public GameObject stoneWallPrefab;
            [InspectorName("Additional Stone Walls（额外石墙区段）")] public StoneWallSectionSettings[] additionalStoneWalls = Array.Empty<StoneWallSectionSettings>();
        }

        [Serializable]
        public sealed class Tuning
        {
            [Header("Experience（体验时长）")]
            [Tooltip("Approximate duration target for the complete playable loop, including the Boss climax.")]
            [Min(30f), InspectorName("Target Duration（目标总时长）")] public float targetDuration = 36.92f;
            [Min(1f), InspectorName("Tutorial Duration（教学时长）")] public float tutorialDuration = 4.31f;
            [Header("Opening Tutorial（开场教学）")]
            [Range(1f, 2f), InspectorName("Opening Elixir Time（开场药剂时间）")] public float openingElixirTime = 1.23f;
            [Range(3, 5), InspectorName("Tutorial Soldier Count（教学士兵数量）")] public int tutorialSoldierCount = 5;
            [Range(1.2f, 3.2f), InspectorName("Tutorial Soldier Spacing（教学士兵间距）")] public float tutorialSoldierSpacing = 1.85f;
            [Range(1.5f, 5f), InspectorName("Tutorial First Soldier Gap（首个教学士兵间隔）")] public float tutorialFirstSoldierGap = 2.46f;
            [Range(3f, 12f), InspectorName("Tutorial Wall Gap（教学墙体间隔）")] public float tutorialWallGap = 6.15f;
            [InspectorName("Tutorial Wall Blocking Mode（教学墙阻挡模式）")] public StoneWallBlockingMode tutorialWallBlockingMode = StoneWallBlockingMode.AllThreeLanes;

            [Header("Forward Speed Loss（前进速度损失）")]
            [InspectorName("Speed Loss Enabled（启用速度损失）")] public bool forwardSpeedLossEnabled = true;
            [Range(0f, 0.5f), InspectorName("Speed Loss Per Second（每秒速度损失）")] public float forwardSpeedLossPerSecond = 0.08f;
            [Range(1f, 10f), InspectorName("Minimum Speed After Loss（损失后最低速度）")] public float minimumSpeedAfterLoss = 1f;
            [Range(0f, 300f), InspectorName("Boss Speed Protection Distance（Boss 前停止掉速距离）")] public float bossSpeedProtectionDistance = 150f;

            [Header("Course（路线）")]
            [Tooltip("Total world-space distance from the start to the Boss encounter.")]
            [Min(20f), InspectorName("Boss Distance（Boss 距离）")] public float bossDistance = 800f;
            [Header("Data-driven Main Run（数据驱动主流程）")]
            [Tooltip("Safe-lane recovery spacing. Keeps the normal route out of the level-one dead zone without granting high-tier progression.")]
            [Range(80f, 300f), InspectorName("Maintenance Reward Spacing（维护奖励间距）")] public float maintenanceRewardSpacing = 135.38f;
            [Range(3, 6), InspectorName("Maintenance Reward Level（维护奖励等级）")] public int maintenanceRewardLevel = 4;
            [Range(40f, 300f), InspectorName("Special Reward Spacing（特殊奖励间距）")] public float specialRewardSpacing = 166.15f;
            [Min(10f), InspectorName("Boss Approach Padding（Boss 接近缓冲）")] public float bossApproachPadding = 21.54f;
            [InspectorName("Procedural Seed（程序化随机种子）")] public int proceduralSeed = 41723;
            [Header("Special reward progression（特殊奖励进程）")]
            [InspectorName("Special Reward Levels（特殊奖励等级）")] public int[] specialRewardLevels = { 7, 8, 9, 10 };
            [Header("Mobile encounter budget（移动端遭遇预算）")]
            [Range(24, 64), InspectorName("Max Active Enemies（最大活动敌人数）")] public int maxActiveEnemies = 48;
            [Range(40f, 140f), InspectorName("Spawn Ahead Distance（前方生成距离）")] public float spawnAheadDistance = 90f;
            [Range(8f, 30f), InspectorName("Recycle Distance（回收距离）")] public float recycleDistance = 18f;
            [Range(1.2f, 1.6f), InspectorName("Target Preview Time（目标预览时间）")] public float targetPreviewTime = 1.35f;
            [Range(8f, 24f), InspectorName("Minimum Target Preview Distance（最小目标预览距离）")] public float minimumTargetPreviewDistance = 12f;
            [Range(30f, 100f), InspectorName("Maximum Target Preview Distance（最大目标预览距离）")] public float maximumTargetPreviewDistance = 54f;
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
            [Range(24, 64), InspectorName("Max Distant Visible Enemies（最大远处可见敌人数）")] public int maxDistantVisibleEnemies = 48;
            [Header("Fallback Route（备用路线）")]
            [Range(4f, 10f), InspectorName("Fallback Boost Magnet Distance（备用增益吸附距离）")] public float fallbackBoostMagnetDistance = 7f;
            [Range(6f, 20f), InspectorName("Fallback Boost Magnet Speed（备用增益吸附速度）")] public float fallbackBoostMagnetSpeed = 12f;
            [Min(1f), InspectorName("Lane Half Width（路线半宽）")] public float laneHalfWidth = 3.2f;
            [Range(0.5f, 1.5f), InspectorName("Drag Follow Ratio（拖动跟随比例）")] public float dragFollowRatio = 1f;
            [Min(1f), InspectorName("Keyboard Lateral Speed（键盘横移速度）")] public float lateralSpeed = 9f;
            [Range(0f, 0.05f), InspectorName("Drag Dead Zone（拖拽死区）")] public float dragDeadZone = 0.005f;
            [InspectorName("Invert Drag Input（反转拖拽输入）")] public bool invertDragInput;
            [InspectorName("Block Input Over UI（阻止界面区域输入）")] public bool blockInputOverUi = true;

            [Header("Impact（冲击）")]
            [Range(0.03f, 0.06f), InspectorName("Hit Stop Seconds（顿帧秒数）")] public float hitStopSeconds = 0.045f;
            [Range(0f, 1f), InspectorName("Camera Shake（镜头抖动）")] public float cameraShake = 0.24f;
            [Range(0f, 8f), InspectorName("FOV Punch（视场角冲击）")] public float fovPunch = 3.5f;
            [Range(0f, 1f), InspectorName("Flash Alpha（闪光透明度）")] public float flashAlpha = 0.52f;
            [Range(0.5f, 6f), InspectorName("Target Launch Seconds（目标飞出时长）")] public float targetLaunchSeconds = 2.25f;
            [Range(2f, 30f), InspectorName("Target Launch Force（目标飞出力度）")] public float targetLaunchForce = 14f;
            [Range(0f, 6f), InspectorName("Target Launch Side Speed（目标飞出侧向速度）")] public float targetLaunchSideSpeed = 2.6f;
            [Range(0f, 8f), InspectorName("Target Launch Up Speed（目标飞出向上速度）")] public float targetLaunchUpSpeed = 3.8f;
            [Range(0.1f, 1f), InspectorName("Target Launch Forward Scale（目标飞出前向倍率）")] public float targetLaunchForwardScale = 0.42f;
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
            [Range(5f, 16f), InspectorName("Environment Reference Spacing（环境参考间距）")] public float environmentReferenceSpacing = 7.38f;

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
            [Range(0.45f, 0.8f), InspectorName("Reference Aspect Ratio（参考宽高比）")] public float referenceAspectRatio = 9f / 16f;
            [Range(45f, 75f), InspectorName("Field Of View（视场角）")] public float fieldOfView = 50f;
            [Range(3f, 12f), InspectorName("Camera Height（镜头高度）")] public float cameraHeight = 4.4f;
            [Range(5f, 20f), InspectorName("Camera Back Distance（镜头后置距离）")] public float cameraBackDistance = 7f;
            [Range(0f, 18f), InspectorName("Camera Look Ahead（镜头前视距离）")] public float cameraLookAhead = 2.8f;
            [Range(0f, 8f), InspectorName("Max Speed FOV Bonus（最高速视场角加成）")] public float maxSpeedFovBonus = 4f;
            [Range(0.15f, 0.35f), InspectorName("Player Viewport Position（玩家视口位置）")] public float playerViewportPosition = 0.24f;
            [Range(0.6f, 0.85f), InspectorName("Vanishing Point Viewport Position（消失点视口位置）")] public float vanishingPointViewportPosition = 0.72f;
            [Min(1f), InspectorName("Near Road Width（近处道路宽度）")] public float nearRoadWidth = 8.5f;
            [Min(0.5f), InspectorName("Far Road Width（远处道路宽度）")] public float farRoadWidth = 1.5f;
            [Min(20f), InspectorName("Visible Depth Range（可见深度范围）")] public float visibleDepthRange = 60f;
            [Range(0.4f, 0.7f), InspectorName("Min Supported Aspect（最小支持宽高比）")] public float minSupportedAspect = 0.45f;
            [Range(0.6f, 0.9f), InspectorName("Max Supported Aspect（最大支持宽高比）")] public float maxSupportedAspect = 0.8f;
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
            public bool openingBoost;
            public bool fallbackBoost;
            public bool anticipated;
            public bool dangerPreviewPlayed;
            public BreakableWallVisual wall;
            public ObstacleOutline outline;
            public ObstacleController obstacle;
            public EnemyVisibilityController visibility;
            public ElixirPickup elixir;
            public float wallCenterX;
            public float wallHalfWidth;
            public bool hasPreviousDistance;
            public float previousDistance;
        }

        [Header("All gameplay values are configurable（所有玩法数值均可配置）")]
        [SerializeField, InspectorName("Tuning（玩法调校）")] private Tuning tuning = new Tuning();

        [Header("Reusable gameplay prefabs（可复用玩法预制体）")]
        [SerializeField, InspectorName("Prefab（可复用预制体）")] private PrefabModules prefab = new PrefabModules();

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

        [Header("Elixir presentation（药剂表现）")]
        [SerializeField, InspectorName("Elixir Presentation（药剂表现设置）")] private ElixirPresentationSettings elixirPresentation = new ElixirPresentationSettings();

        [Header("Normal impact presentation（普通冲击表现）")]
        [SerializeField, InspectorName("Impact Presentation（冲击表现设置）")] private ImpactPresentationSettings impactPresentation = new ImpactPresentationSettings();

        [Header("Pooled enemy break presentation（敌人破碎对象池表现）")]
        [SerializeField, InspectorName("Enemy Break Presentation（敌人破碎表现设置）")] private EnemyBreakPresentationSettings enemyBreakPresentation = new EnemyBreakPresentationSettings();

        [Header("Tutorial wall presentation（教学墙体表现）")]
        [SerializeField, InspectorName("Wall Break Presentation（墙体破碎表现设置）")] private WallBreakSettings wallBreakPresentation = new WallBreakSettings();

        [Header("Obstacle risk outline（障碍风险轮廓）")]
        [SerializeField, InspectorName("Outline Presentation（轮廓表现设置）")] private ObstacleOutlineSettings outlinePresentation = new ObstacleOutlineSettings();

        [Header("Authoritative level-up feedback（权威升级反馈）")]
        [SerializeField, InspectorName("Speed Level Feedback Config（速度等级反馈配置）")] private SpeedLevelFeedbackConfig speedLevelFeedbackConfig;

        [Header("Boss five-phase presentation（Boss 五阶段表现）")]
        [SerializeField, InspectorName("Boss Clash Presentation（Boss 对抗表现设置）")] private BossClashSettings bossClashPresentation = new BossClashSettings();

        [Header("Audio clips and mobile haptics（音频片段与移动触觉）")]
        [SerializeField, InspectorName("Audio Presentation（音频表现设置）")] private AudioFeedbackSettings audioPresentation = new AudioFeedbackSettings();

        [Header("Replace visuals here - gameplay colliders stay on parent objects（在此替换视觉，玩法碰撞体保留在父对象）")]
        [SerializeField, InspectorName("Player Visual Prefab（玩家视觉预制体）")] private GameObject playerVisualPrefab;
        [SerializeField, InspectorName("Player Animator（玩家动画控制器）")] private RuntimeAnimatorController playerAnimator;
        [SerializeField, InspectorName("Tier 1 Soldier Prefab（等级 1 士兵预制体）")] private GameObject tier1SoldierPrefab;
        [SerializeField, InspectorName("Tier 4 Soldier Prefab（等级 4 士兵预制体）")] private GameObject tier4SoldierPrefab;
        [SerializeField, InspectorName("Tier 1 Soldier Break Prefab（等级 1 士兵破碎预制体）")] private GameObject tier1SoldierBreakPrefab;
        [SerializeField, InspectorName("Tier 4 Soldier Break Prefab（等级 4 士兵破碎预制体）")] private GameObject tier4SoldierBreakPrefab;
        [SerializeField, Min(0.1f), InspectorName("Tier 1 Target Height（等级 1 目标高度）")] private float tier1TargetHeight = 1.6f;
        [SerializeField, Min(0.1f), InspectorName("Tier 4 Target Height（等级 4 目标高度）")] private float tier4TargetHeight = 2.2f;
        [SerializeField, InspectorName("Boss Visual Prefab（Boss 视觉预制体）")] private GameObject bossVisualPrefab;
        [SerializeField, InspectorName("Boss Animator（Boss 动画控制器）")] private RuntimeAnimatorController bossAnimator;
        [SerializeField, InspectorName("Princess Visual Prefab（公主视觉预制体）")] private GameObject princessVisualPrefab;
        [SerializeField, InspectorName("Princess Animator（公主动画控制器）")] private RuntimeAnimatorController princessAnimator;

        [Header("Development-only speed diagnostics（仅开发用速度诊断）")]
        [SerializeField, InspectorName("Show Speed Debug Overlay（显示速度调试覆盖层）")] private bool showSpeedDebugOverlay;
        [SerializeField, Min(20f), InspectorName("Debug Test Segment Length（调试测试区段长度）")] private float debugTestSegmentLength = 100f;

        private readonly List<Encounter> encounters = new List<Encounter>();
        private readonly HashSet<int> targetVisualFallbackWarnings = new HashSet<int>();
        private readonly List<Renderer> roadRenderers = new List<Renderer>();
        private Transform worldRoot;
        private Transform runner;
        private Transform runnerVisual;
        private Transform boss;
        private Transform bossVisual;
        private Transform princess;
        private Transform cage;
        private Camera gameCamera;
        private AudioFeedbackController audioFeedback;
        private SpeedVisualFeedback speedFeedback;
        private ImpactEffectPool effectPool;
        private EnemyBreakEffectPool enemyBreakPool;
        private SoldierBreakEffectPool soldierBreakPool;
        private BossClashVisual bossClashVisual;
        private SpeedBarView speedBarView;
        private SpeedLevelFeedbackController speedLevelFeedback;
        private VisualTimeScaleController visualTimeScale;
        private PlayerSpeedController speedController;
        private PlayerForwardMotionController forwardMotion;
        private Animator runnerAnimator;
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
        private float fovPunchOffset;
        private float baseFov = 58f;
        private bool dragging;
        private float dragStartNormalizedX;
        private float dragOriginTargetX;
        private int activeTouchId = -1;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private bool bossSequence;
        private bool ending;
        private bool fallbackActive;
        private BossClashPhase currentBossPhase;
        private string callout = string.Empty;
        private float calloutUntil;
        private Texture2D whiteTexture;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private LineRenderer upgradeRing;
        private float lastImpactTime;
        private float lastNormalShakeTime;
        private int comboPitchIndex;
        private int launchVariant;
        private bool lowSpeedWarningShown;
        private bool gameplayStarted;
        private int lastUiTier = -1;
        private string speedReadout = "1/10";
        private int generatedLevelOneSoldierCount;
        private readonly int[] generatedRewardLaneCounts = new int[3];

        public PlayerSpeedController SpeedController => speedController;
        public RunFlowState CurrentFlowState => flowController != null ? flowController.CurrentState : RunFlowState.Intro;
        public int GeneratedLevelOneSoldierCount => generatedLevelOneSoldierCount;
        public int GeneratedSoldierSectionCount => prefab != null && prefab.soldierSections != null ? prefab.soldierSections.Length : 0;
        public int GeneratedLeftRewardSections => generatedRewardLaneCounts[0];
        public int GeneratedCenterRewardSections => generatedRewardLaneCounts[1];
        public int GeneratedRightRewardSections => generatedRewardLaneCounts[2];
        public float ConfiguredBossDistance => tuning.bossDistance;
        public float TargetForwardSpeed => forwardMotion != null ? forwardMotion.TargetForwardSpeed : 0f;
        public float CurrentForwardSpeed => forwardMotion != null ? forwardMotion.CurrentForwardSpeed : 0f;
        public float CurrentAnimationSpeed => runnerAnimator != null ? runnerAnimator.speed : 0f;
        private int CurrentTier => speedController != null ? speedController.GetCurrentLevel() : 1;
        private bool FormalStarted => flowController != null && flowController.CurrentState == RunFlowState.MainRun;
        private float CourseDistanceScale => tuning != null ? Mathf.Max(0.1f, tuning.bossDistance / 1300f) : 1f;
        private float OpeningElixirZ => tuning.openingElixirTime * playerSpeed.forwardSpeeds[0];

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = false;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            if (!ValidatePrefabModules())
            {
                enabled = false;
                return;
            }
            if (playerSpeed == null) playerSpeed = new PlayerSpeedSettings();
            if (speedVisualProfile == null)
            {
                speedVisualProfile = ScriptableObject.CreateInstance<SpeedVisualProfile>();
                speedVisualProfile.name = "RuntimeSpeedVisualProfileFallback";
            }
            flowController = GetComponent<RunFlowController>();
            if (flowController == null) flowController = gameObject.AddComponent<RunFlowController>();
            flowController.ResetToIntro();
            BuildWorld();
            BuildLevel();
            targetX = 0f;
            callout = string.Empty;
            calloutUntil = 0f;
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
            flowController?.ResetToIntro();
            forwardMotion?.Tick(0f, false);
            dragging = false;
            targetX = 0f;
            callout = string.Empty;
            calloutUntil = 0f;
        }

        public void BeginGameplay()
        {
            if (gameplayStarted)
            {
                return;
            }

            gameplayStarted = true;
            elapsed = 0f;
            speedController.SetLevel(playerSpeed.startingLevel, SpeedChangeReason.InitialSetup, this);
            forwardMotion?.SnapToTarget();
            targetX = 0f;
            flowController.StartTutorial();
            dragging = false;
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
                CheckBossEntry();
            }

            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 3.8f);
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
            if (bossSequence || !FormalStarted)
            {
                if (!FormalStarted)
                {
                    dragging = false;
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
                        dragging = true;
                        activeTouchId = candidate.fingerId;
                        dragStartNormalizedX = candidate.position.x / Mathf.Max(1f, Screen.width);
                        dragOriginTargetX = targetX;
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
                        dragging = true;
                        dragStartNormalizedX = Input.mousePosition.x / Mathf.Max(1f, Screen.width);
                        dragOriginTargetX = targetX;
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    dragging = false;
                }

                if (dragging)
                    ApplyNormalizedDrag(Input.mousePosition.x / Mathf.Max(1f, Screen.width));
            }

            targetX += Input.GetAxisRaw("Horizontal") * tuning.lateralSpeed * Time.deltaTime;
            targetX = Mathf.Clamp(targetX, -tuning.laneHalfWidth, tuning.laneHalfWidth);
        }

        private void ApplyNormalizedDrag(float normalizedX)
        {
            float delta = normalizedX - dragStartNormalizedX;
            if (Mathf.Abs(delta) <= tuning.dragDeadZone) return;
            delta -= Mathf.Sign(delta) * tuning.dragDeadZone;
            if (tuning.invertDragInput) delta = -delta;
            float pixelDelta = delta * Mathf.Max(1f, Screen.width);
            targetX = dragOriginTargetX + pixelDelta / GetRunnerPixelsPerWorldUnit() * tuning.dragFollowRatio;
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
        }

        private void MoveRunner()
        {
            float forwardSpeed = forwardMotion != null
                ? forwardMotion.Tick(Time.deltaTime, true)
                : speedController.GetForwardSpeed();
            float x = dragging
                ? targetX
                : Mathf.MoveTowards(runner.position.x, targetX, tuning.lateralSpeed * Time.deltaTime);
            runner.position = new Vector3(x, runner.position.y, runner.position.z + forwardSpeed * Time.deltaTime);

            if (FormalStarted && !bossSequence && tuning.forwardSpeedLossEnabled && !IsInsideBossSpeedProtectionZone())
            {
                speedController.ApplyContinuousSpeedLoss(Time.deltaTime, tuning.forwardSpeedLossPerSecond,
                    tuning.minimumSpeedAfterLoss, this);
            }

            UpdateRunnerFeedback(forwardSpeed, true);
        }

        private bool IsInsideBossSpeedProtectionZone()
        {
            if (runner == null) return false;
            float distanceToBoss = tuning.bossDistance - runner.position.z;
            return distanceToBoss <= Mathf.Max(0f, tuning.bossSpeedProtectionDistance);
        }

        private float GetForwardSpeed()
        {
            return forwardMotion != null ? forwardMotion.CurrentForwardSpeed : speedController != null ? speedController.GetForwardSpeed() : playerSpeed.forwardSpeeds[0];
        }

        private void UpdateRunnerFeedback(float forwardSpeed, bool movementActive)
        {
            float actualNormalized = forwardMotion != null ? forwardMotion.NormalizedActualSpeed : 0f;
            if (runnerVisual != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * forwardSpeed * 1.8f) * 0.035f;
                float lean = (targetX - runner.position.x) * -7f;
                runnerVisual.localScale = new Vector3(pulse, 1.55f / pulse, pulse);
                float chargeLean = speedFeedback != null ? speedFeedback.CurrentChargeLean : 0f;
                runnerVisual.localRotation = Quaternion.Euler(lean + chargeLean * actualNormalized, 0f, 0f);
            }

            if (runnerAnimator != null)
                runnerAnimator.speed = movementActive ? Mathf.Lerp(0.9f, 1.65f, actualNormalized) : 0f;

            if (speedFeedback != null)
            {
                speedFeedback.UpdateFeedback(speedController.CurrentSpeed, CurrentTier, forwardSpeed, Time.unscaledDeltaTime);
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
            int activeEnemies = 0;
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
                        activeEnemies++;
                    }

                    encounter.visibility?.SetState(desiredState);
                    if (desiredState == EnemyVisibilityState.DistantVisible)
                    {
                        float approach = 1f - Mathf.InverseLerp(activeDistance, visibleDistance, dz);
                        encounter.visibility?.SetDistantPreviewStrength(Mathf.Lerp(0.22f, 0.74f, approach));
                    }
                    if (desiredState == EnemyVisibilityState.DistantVisible || desiredState == EnemyVisibilityState.Active)
                    {
                        CollisionOutcome previewOutcome = encounter.outline != null
                            ? encounter.outline.CurrentOutcome
                            : ObstacleController.EvaluateCollisionOutcome(CurrentTier, encounter.tier);
                        HandleTargetPreview(encounter, previewOutcome);
                    }
                    if (desiredState != EnemyVisibilityState.Active) continue;
                }

                bool crossedThisFrame = encounter.hasPreviousDistance && encounter.previousDistance > 0f && dz <= 0f;
                encounter.previousDistance = dz;
                encounter.hasPreviousDistance = true;
                if (dz < -tuning.recycleDistance && !crossedThisFrame)
                {
                    encounter.consumed = true;
                    if (encounter.visibility != null) encounter.visibility.Recycle();
                    else encounter.root.SetActive(false);
                    continue;
                }

                if (encounter.type == EncounterType.Wall)
                {
                    bool runnerInsideWall = Mathf.Abs(runner.position.x - encounter.wallCenterX)
                        <= encounter.wallHalfWidth + 0.35f;
                    if (runnerInsideWall && !encounter.anticipated
                        && dz <= GetForwardSpeed() * wallBreakPresentation.anticipationDuration)
                    {
                        encounter.anticipated = true;
                        fovPunchOffset = -wallBreakPresentation.fovAnticipation;
                        speedFeedback?.Pulse(0.45f);
                    }
                    if (runnerInsideWall && (Mathf.Abs(dz) < 1.3f || crossedThisFrame))
                    {
                        encounter.consumed = true;
                        ResolveObstacle(encounter);
                        StartCoroutine(BreakWallSequence(encounter));
                    }
                    continue;
                }

                if (encounter.type == EncounterType.Elixir && encounter.fallbackBoost && dz > 0f && dz <= tuning.fallbackBoostMagnetDistance)
                {
                    Vector3 position = encounter.root.transform.position;
                    position.x = Mathf.MoveTowards(position.x, runner.position.x, tuning.fallbackBoostMagnetSpeed * Time.deltaTime);
                    encounter.root.transform.position = position;
                }

                float collectionHalfWidth = encounter.fallbackBoost ? tuning.laneHalfWidth * 2f : 1.15f;
                if ((Mathf.Abs(dz) < 1.25f || crossedThisFrame) && Mathf.Abs(encounter.root.transform.position.x - runner.position.x) < collectionHalfWidth)
                {
                    encounter.consumed = true;
                    if (encounter.type == EncounterType.Elixir)
                    {
                        CollectElixir(encounter);
                    }
                    else
                    {
                        HitTarget(encounter);
                    }
                }
            }
        }

        private void HandleTargetPreview(Encounter encounter, CollisionOutcome outcome)
        {
            if (outcome == CollisionOutcome.SpeedLoss && !encounter.dangerPreviewPlayed)
            {
                encounter.dangerPreviewPlayed = true;
                audioFeedback?.PlayDangerPreview();
            }
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
            if (flowController.CurrentState == RunFlowState.Tutorial)
            {
                flowController.EnterMainRun();
                callout = "<  DRAG TO STEER  >";
                calloutUntil = elapsed + 2.5f;
            }
            flashAlpha = Mathf.Max(flashAlpha, 0.42f);
            directionalShake = Vector3.up * 0.35f;
            PunchCamera(0.34f, wallBreakPresentation.cameraShake, wallBreakPresentation.fovImpact);
            SpeedTierVisualData wallTier = speedVisualProfile.Get(playerSpeed.tutorialElixirTargetLevel);
            effectPool?.PlayImpact(encounter.root.transform.position + Vector3.up, wallTier.secondaryColor, 1.75f);
            effectPool?.PlayImpact(encounter.root.transform.position + new Vector3(0f, 0.2f, -0.4f), new Color(0.48f, 0.43f, 0.38f), 1.2f);
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
            StartCoroutine(ElixirUpgradeSequence(encounter, encounter.tier));
        }

        private IEnumerator ElixirUpgradeSequence(Encounter encounter, int nextTier)
        {
            GameObject pickup = encounter.root;
            ElixirVisual visual = pickup != null ? pickup.GetComponent<ElixirVisual>() : null;
            visual?.BeginConsume();
            flashAlpha = Mathf.Max(flashAlpha, elixirPresentation.pickupFlash);
            fovPunchOffset = -elixirPresentation.cameraPushIn;
            PunchCamera(0.13f, tuning.cameraShake * 0.45f, 0f);

            visualTimeScale.RequestSlowMotion(elixirPresentation.slowMotionScale, elixirPresentation.slowMotionDuration);
            SpeedTierVisualData elixirTier = speedVisualProfile.Get(playerSpeed.tutorialElixirTargetLevel);
            effectPool?.PlayImpact(pickup.transform.position + Vector3.up * 0.35f, elixirTier.primaryColor, 0.85f);
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
                    StartCoroutine(PlayUpgradeRing());
                }

                yield return null;
            }

            if (pickup != null)
            {
                pickup.SetActive(false);
            }
        }

        private IEnumerator PlayUpgradeRing()
        {
            if (upgradeRing == null)
            {
                yield break;
            }

            upgradeRing.enabled = true;
            float timer = 0f;
            const float duration = 0.34f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float radius = Mathf.Lerp(0.12f, elixirPresentation.energyRingMaxRadius, 1f - Mathf.Pow(1f - t, 2f));
                upgradeRing.transform.localScale = new Vector3(radius, radius, radius);
                Color color = Color.Lerp(speedVisualProfile.Get(1).primaryColor, speedVisualProfile.Get(playerSpeed.tutorialElixirTargetLevel).primaryColor, t);
                color.a = 1f - t;
                upgradeRing.startColor = color;
                upgradeRing.endColor = color;
                yield return null;
            }
            upgradeRing.enabled = false;
        }

        private void HitTarget(Encounter encounter)
        {
            float speedBeforeImpact = speedController.CurrentSpeed;
            ObstacleResolutionType resolution = ResolveObstacle(encounter);
            if (resolution == ObstacleResolutionType.Boosted)
            {
                callout = "SMASH!";
                calloutUntil = elapsed + 0.75f;
                Impact(encounter, CollisionOutcome.SpeedGain, true, 1.15f);
                bool wasAtMaximum = speedBeforeImpact >= playerSpeed.maximumSpeed - 0.001f;
                int shards = wasAtMaximum ? impactPresentation.minEnergyShards : impactPresentation.maxEnergyShardsPerHit;
                effectPool?.PlayEnergyReturn(encounter.root.transform.position + Vector3.up * 0.65f, shards,
                    speedVisualProfile.Get(CurrentTier).primaryColor, wasAtMaximum ? 0.55f : 1f);
            }
            else if (resolution == ObstacleResolutionType.Equal)
            {
                callout = "HOLD SPEED";
                calloutUntil = elapsed + 0.7f;
                Impact(encounter, CollisionOutcome.Neutral, true, 0.85f);
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
                Impact(encounter, CollisionOutcome.SpeedLoss, false, 1.25f);
            }
        }

        private ObstacleResolutionType ResolveObstacle(Encounter encounter)
        {
            if (encounter.obstacle == null) return ObstacleResolutionType.Equal;
            float boost = encounter.obstacle.Type == ObstacleType.Soldier && encounter.tier == 1
                ? playerSpeed.levelOneSoldierBoost
                : playerSpeed.normalImpactBoost;
            return encounter.obstacle.Resolve(speedController, boost, playerSpeed.maximumSpeed);
        }

        private void Impact(Encounter encounter, CollisionOutcome outcome, bool launchTarget, float strength)
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
            audioFeedback?.PlayCollisionOutcome(outcome, comboPitchIndex, impactPresentation.comboPitchStep,
                normalizedActualSpeed, encounter.root.transform.position);
            flashAlpha = Mathf.Max(flashAlpha, impactPresentation.normalFlash * strength);
            float side = Mathf.Sign(encounter.root.transform.position.x - runner.position.x);
            if (Mathf.Abs(side) < 0.1f) side = launchVariant % 2 == 0 ? -1f : 1f;
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
                launchTarget ? impactTier.secondaryColor : outlinePresentation.dangerColor,
                strength * actualImpactScale);

            Vector3 sourceDimensions = encounter.root.transform.localScale;
            Color breakColor = outcome == CollisionOutcome.SpeedLoss
                ? outlinePresentation.dangerColor
                : impactTier.primaryColor;
            bool usedSoldierBreak = false;
            if ((encounter.tier == 1 || encounter.tier == 4) && soldierBreakPool != null)
            {
                Vector3 breakPosition = encounter.root.transform.position
                    - Vector3.up * encounter.root.transform.lossyScale.y * 0.5f;
                usedSoldierBreak = soldierBreakPool.PlayBreak(encounter.tier, breakPosition,
                    encounter.root.transform.rotation, normalizedActualSpeed, side);
            }
            if (!usedSoldierBreak)
            {
                enemyBreakPool?.PlayBreak(encounter.root.transform.position, sourceDimensions, breakColor,
                    normalizedActualSpeed, side);
            }
            encounter.root.SetActive(false);

            if (impactPresentation.enableNormalHitStop)
                visualTimeScale.RequestSlowMotion(impactPresentation.hitStopTimeScale, impactPresentation.hitStopDuration);
        }

        private IEnumerator LaunchTarget(GameObject target, float strength)
        {
            if (target == null)
            {
                yield break;
            }

            int variant = launchVariant++ % 3;
            float side = Mathf.Sign(target.transform.position.x - runner.position.x);
            if (Mathf.Abs(side) < 0.1f) side = variant == 1 ? -1f : 1f;
            Vector3[] rotations =
            {
                new Vector3(430f, 260f, 150f),
                new Vector3(280f, -390f, 240f),
                new Vector3(-360f, 310f, -210f)
            };
            KnockedEnemyPhysics knockedPhysics = target.GetComponent<KnockedEnemyPhysics>();
            if (knockedPhysics != null)
            {
                float sideVariation = 1f + variant * 0.12f;
                Vector3 velocity = new Vector3(
                    side * tuning.targetLaunchSideSpeed * sideVariation,
                    tuning.targetLaunchUpSpeed * (0.94f + variant * 0.06f),
                    tuning.targetLaunchForce * tuning.targetLaunchForwardScale * strength);
                knockedPhysics.Launch(velocity, rotations[variant] * Mathf.Deg2Rad);
                yield break;
            }

            float timer = 0f;
            while (timer < tuning.targetLaunchSeconds && target != null)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (target != null) target.SetActive(false);
        }

        private IEnumerator BounceTarget(GameObject target)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 start = target.transform.position;
            float timer = 0f;
            while (timer < 0.35f && target != null)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / 0.35f;
                target.transform.position = start + Vector3.forward * Mathf.Sin(t * Mathf.PI) * 0.8f;
                yield return null;
            }
        }

        private IEnumerator SquashRunner()
        {
            if (runnerVisual == null)
            {
                yield break;
            }

            float timer = 0f;
            while (timer < 0.28f)
            {
                timer += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(timer / 0.28f * Mathf.PI);
                runnerVisual.localScale = new Vector3(1f + wave * 0.35f, 1.55f - wave * 0.5f, 1f + wave * 0.35f);
                yield return null;
            }
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
            if (boss != null && runner.position.z >= tuning.bossDistance - 3.1f)
            {
                StartCoroutine(BossClash());
            }
        }

        private IEnumerator BossClash()
        {
            bossSequence = true;
            flowController.EnterBoss();
            targetX = 0f;
            bool wins = CurrentTier >= playerSpeed.bossVictoryLevel;
            callout = string.Empty;
            calloutUntil = elapsed;
            bossClashVisual.Begin(wins);

            Vector3 runnerStart = runner.position;
            Vector3 bossStart = boss.position;
            Vector3 runnerContact = new Vector3(0f, runnerStart.y, tuning.bossDistance - 1.1f);
            Vector3 bossContact = new Vector3(0f, bossStart.y, tuning.bossDistance + 1.1f);

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
            audioFeedback?.PlayBossContact();
            flashAlpha = Mathf.Max(flashAlpha, 0.48f);
            PunchCamera(bossClashPresentation.contactDuration, bossClashPresentation.contactShake, bossClashPresentation.contactFovPunch);
            effectPool?.PlayImpact(Vector3.Lerp(runner.position, boss.position, 0.5f) + Vector3.up, wins ? bossClashPresentation.playerEnergy : bossClashPresentation.bossEnergy, 1.65f);
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
            shakeStrength = bossClashPresentation.struggleShake;
            shakeUntil = Time.unscaledTime + bossClashPresentation.struggleDuration;
            directionalShake = new Vector3(0.35f, 0.15f, 0f);
            Vector3 runnerPressure = runnerContact + Vector3.forward * (wins ? 0.85f : -bossClashPresentation.pressureTravel);
            Vector3 bossPressure = bossContact + Vector3.forward * (wins ? bossClashPresentation.pressureTravel : -0.85f);
            timer = 0f;
            while (timer < bossClashPresentation.struggleDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / bossClashPresentation.struggleDuration);
                float pressure = Mathf.SmoothStep(0f, 1f, t);
                runner.position = Vector3.Lerp(runnerContact, runnerPressure, pressure);
                boss.position = Vector3.Lerp(bossContact, bossPressure, pressure);
                Vector3 center = Vector3.Lerp(runner.position, boss.position, wins ? 0.6f : 0.4f) + Vector3.up;
                bossClashVisual.UpdatePresentation(t, center);
                yield return null;
            }

            currentBossPhase = BossClashPhase.Stagger;
            bossClashVisual.SetPhase(currentBossPhase);
            Vector3 staggerStartRunner = runner.position;
            Vector3 staggerStartBoss = boss.position;
            Vector3 staggerRunner = staggerStartRunner + Vector3.back * (wins ? 0f : 1.2f);
            Vector3 staggerBoss = staggerStartBoss + Vector3.forward * (wins ? 1.4f : 0f);
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
            audioFeedback?.PlayBossFinish();
            flashAlpha = Mathf.Max(flashAlpha, 0.56f);
            PunchCamera(bossClashPresentation.finishDuration, bossClashPresentation.finishShake, bossClashPresentation.finishFovPunch);
            effectPool?.PlayImpact(Vector3.Lerp(runner.position, boss.position, 0.55f) + Vector3.up, bossClashPresentation.playerEnergy, 2f);
            Vector3 bossStart = boss.position;
            Vector3 velocity = new Vector3(0f, 10f, 24f);
            float timer = 0f;
            while (timer < bossClashPresentation.finishDuration)
            {
                float dt = Time.unscaledDeltaTime;
                timer += dt;
                velocity += Vector3.down * 7f * dt;
                boss.position += velocity * dt;
                boss.Rotate(520f * dt, 340f * dt, 240f * dt, Space.Self);
                bossClashVisual.UpdatePresentation(timer / bossClashPresentation.finishDuration, bossStart + Vector3.up);
                yield return null;
            }

            bossClashVisual.SetVisible(false);
            BreakCage();
            fallbackActive = false;
            currentBossPhase = BossClashPhase.None;
            yield return new WaitForSecondsRealtime(0.55f);
            ending = true;
            bossSequence = false;
            flowController.EnterResult();
        }

        private IEnumerator BossFinishFailure()
        {
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
            speedController.DropOneLevel(SpeedChangeReason.BossEvent, this);
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
            boss.position = new Vector3(0f, 1.5f, tuning.bossDistance + 2.5f);
            boss.rotation = Quaternion.identity;
            fallbackActive = true;
            callout = "BOOST ROUTE UNLOCKED!";
            calloutUntil = elapsed + 2f;
            SpawnFallbackBoosts();
            bossSequence = false;
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

        private void SpawnFallbackBoosts()
        {
            float scale = CourseDistanceScale;
            float start = tuning.bossDistance - 46f * scale;
            CreateElixir(-2.2f, start, false, true, 8);
            CreateElixir(2.2f, start + 11f * scale, false, true, 9);
            CreateElixir(0f, start + 22f * scale, false, true, 10);
            CreateElixir(0f, tuning.bossDistance - 10f * scale, false, true, playerSpeed.bossVictoryLevel);
        }

        private void BuildWorld()
        {
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();

            worldRoot = new GameObject("GeneratedWorld").transform;
            worldRoot.SetParent(transform, false);
            BuildCameraAndLight();
            BuildRoad();
            BuildRunner();
            BuildEffectPool();
            BuildEnemyBreakPool();
            BuildSpeedBar();
            BuildSpeedLevelFeedback();
            BuildBossArea();
        }

        private void BuildEffectPool()
        {
            GameObject poolRoot = new GameObject("ImpactEffectPool");
            poolRoot.transform.SetParent(worldRoot, false);
            effectPool = poolRoot.AddComponent<ImpactEffectPool>();
            effectPool.Initialize(runner, impactPresentation, speedVisualProfile, visualPerformance);
            effectPool.EnergyShardAbsorbed = OnEnergyShardAbsorbed;
        }

        private void BuildEnemyBreakPool()
        {
            GameObject poolRoot = new GameObject("EnemyBreakEffectPool");
            poolRoot.transform.SetParent(worldRoot, false);
            enemyBreakPool = poolRoot.AddComponent<EnemyBreakEffectPool>();
            enemyBreakPool.Initialize(enemyBreakPresentation, visualPerformance);

            GameObject soldierPoolRoot = new GameObject("SoldierBreakEffectPool");
            soldierPoolRoot.transform.SetParent(worldRoot, false);
            soldierBreakPool = soldierPoolRoot.AddComponent<SoldierBreakEffectPool>();
            soldierBreakPool.Initialize(tier1SoldierBreakPrefab, tier4SoldierBreakPrefab);
        }

        private void BuildSpeedBar()
        {
            GameObject canvasRoot = new GameObject("SpeedBarCanvas");
            canvasRoot.transform.SetParent(transform, false);
            speedBarView = canvasRoot.AddComponent<SpeedBarView>();
            speedBarView.Initialize(speedController, speedVisualProfile);
        }

        private void BuildSpeedLevelFeedback()
        {
            if (speedLevelFeedbackConfig == null)
            {
                speedLevelFeedbackConfig = ScriptableObject.CreateInstance<SpeedLevelFeedbackConfig>();
                speedLevelFeedbackConfig.name = "RuntimeSpeedLevelFeedbackConfigFallback";
            }
            speedLevelFeedback = gameObject.AddComponent<SpeedLevelFeedbackController>();
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
            CreateBox("Road", new Vector3(0f, -0.3f, tuning.bossDistance * 0.5f), new Vector3(8.5f, 0.5f, tuning.bossDistance + 28f), environment.roadColor, worldRoot);
            CreateBox("LeftWall", new Vector3(-4.7f, 0.25f, tuning.bossDistance * 0.5f), new Vector3(1.1f, 1.1f, tuning.bossDistance + 28f), environment.wallColor, worldRoot);
            CreateBox("RightWall", new Vector3(4.7f, 0.25f, tuning.bossDistance * 0.5f), new Vector3(1.1f, 1.1f, tuning.bossDistance + 28f), environment.wallColor, worldRoot);
            CreateRoadBoundary("LeftRoadBoundary", -4.12f);
            CreateRoadBoundary("RightRoadBoundary", 4.12f);

            CreateDecorationBox("LeftRouteGuide", new Vector3(-1.35f, -0.015f, tuning.bossDistance * 0.5f), new Vector3(0.055f, 0.025f, tuning.bossDistance + 20f), environment.routeMarkColor, worldRoot);
            CreateDecorationBox("RightRouteGuide", new Vector3(1.35f, -0.015f, tuning.bossDistance * 0.5f), new Vector3(0.055f, 0.025f, tuning.bossDistance + 20f), environment.routeMarkColor, worldRoot);

            for (float z = 0f; z < tuning.bossDistance; z += environment.environmentReferenceSpacing)
            {
                CreateDecorationBox("RoadBand", new Vector3(0f, -0.02f, z), new Vector3(8.4f, 0.04f, 0.16f), environment.routeMarkColor, worldRoot);
                CreateDecorationBox("TorchLeft", new Vector3(-4.05f, 1.1f, z + 4f), new Vector3(0.25f, 2.2f, 0.25f), environment.timberColor, worldRoot);
                CreateDecorationBox("TorchRight", new Vector3(4.05f, 1.1f, z + 4f), new Vector3(0.25f, 2.2f, 0.25f), environment.timberColor, worldRoot);
            }

            BuildDistantCastle();
        }

        private void BuildDistantCastle()
        {
            Transform castle = new GameObject("DistantCastle").transform;
            castle.SetParent(worldRoot, false);
            castle.position = new Vector3(0f, 0f, tuning.bossDistance + 19f);

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

            speedFeedback = root.AddComponent<SpeedVisualFeedback>();
            speedFeedback.Initialize(speedVisualProfile, visualPerformance);
            BuildUpgradeRing(root.transform);
            audioFeedback = root.AddComponent<AudioFeedbackController>();
            audioFeedback.Initialize(audioPresentation);
            visualTimeScale = gameObject.GetComponent<VisualTimeScaleController>();
            if (visualTimeScale == null) visualTimeScale = gameObject.AddComponent<VisualTimeScaleController>();
        }

        private void OnPlayerSpeedChanged(SpeedChangedEvent change)
        {
            int level = change.NewLevel;
            lastUiTier = level;
            speedReadout = level + "/" + speedController.MaxLevel;
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
        }

        private void BuildBossArea()
        {
            CreateBox("BossPlatform", new Vector3(0f, 0f, tuning.bossDistance + 5f), new Vector3(9f, 0.7f, 17f), new Color(0.42f, 0.34f, 0.26f), worldRoot);

            GameObject bossRoot = new GameObject("Boss");
            bossRoot.transform.SetParent(worldRoot, false);
            bossRoot.transform.position = new Vector3(0f, 1.5f, tuning.bossDistance + 2.5f);
            boss = bossRoot.transform;
            ReplaceableVisual bossReplaceable = bossRoot.AddComponent<ReplaceableVisual>();
            bossReplaceable.Build(bossVisualPrefab, bossAnimator, PrimitiveType.Cylinder, new Color(0.58f, 0.08f, 0.06f), new Vector3(1.75f, 3f, 1.75f));
            bossVisual = bossReplaceable.VisualRoot;

            GameObject princessRoot = new GameObject("Princess");
            princessRoot.transform.SetParent(worldRoot, false);
            princessRoot.transform.position = new Vector3(0f, 1f, tuning.bossDistance + 9f);
            princess = princessRoot.transform;
            ReplaceableVisual princessReplaceable = princessRoot.AddComponent<ReplaceableVisual>();
            princessReplaceable.Build(princessVisualPrefab, princessAnimator, PrimitiveType.Cylinder, new Color(1f, 0.45f, 0.7f), new Vector3(0.75f, 1.8f, 0.75f));
            princessRoot.SetActive(false);

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
        }

        private void BuildLevel()
        {
            float openingElixirZ = OpeningElixirZ;
            CreateElixir(0f, openingElixirZ, true, false, playerSpeed.tutorialElixirTargetLevel);

            float firstSoldierZ = openingElixirZ + tuning.tutorialFirstSoldierGap;
            int soldierCount = Mathf.Clamp(tuning.tutorialSoldierCount, 3, 5);
            for (int i = 0; i < soldierCount; i++)
            {
                CreateTarget(0f, firstSoldierZ + i * tuning.tutorialSoldierSpacing, 1);
            }

            float wallZ = firstSoldierZ + (soldierCount - 1) * tuning.tutorialSoldierSpacing + tuning.tutorialWallGap;
            CreateBreakableWall(wallZ, "TutorialStoneWall", tuning.tutorialWallBlockingMode);
            BuildConfiguredMainRun(wallZ);
        }

        private void BuildConfiguredMainRun(float tutorialEndZ)
        {
            generatedLevelOneSoldierCount = 0;
            for (int i = 0; i < generatedRewardLaneCounts.Length; i++) generatedRewardLaneCounts[i] = 0;
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
                        section.blockingMode);
                }
            }

            float scale = CourseDistanceScale;
            int[] rewards = tuning.specialRewardLevels;
            float maintenanceZ = tutorialEndZ + tuning.maintenanceRewardSpacing;
            while (maintenanceZ < tuning.bossDistance - tuning.bossApproachPadding)
            {
                while (IsNearSoldierSection(maintenanceZ, tutorialEndZ, 5f * scale)
                    || IsNearStoneWallSection(maintenanceZ, tutorialEndZ, 5f * scale))
                    maintenanceZ += 12f * scale;
                CreateElixir(0f, maintenanceZ, false, false, tuning.maintenanceRewardLevel);
                maintenanceZ += tuning.maintenanceRewardSpacing;
            }

            if (rewards != null && rewards.Length > 0)
            {
                float rewardZ = tutorialEndZ + tuning.specialRewardSpacing;
                int rewardIndex = 0;
                while (rewardZ < tuning.bossDistance - 18f * scale)
                {
                    while (IsNearSoldierSection(rewardZ, tutorialEndZ, 5f * scale)
                        || IsNearStoneWallSection(rewardZ, tutorialEndZ, 5f * scale))
                        rewardZ += 12f * scale;
                    float x = rewardIndex % 2 == 0 ? 2.25f : -2.25f;
                    int targetLevel = Mathf.Clamp(rewards[Mathf.Min(rewardIndex, rewards.Length - 1)], 1, speedController.MaxLevel);
                    CreateElixir(x, rewardZ, false, false, targetLevel);
                    rewardIndex++;
                    rewardZ += tuning.specialRewardSpacing;
                }
            }

            CreateElixir(0f, tuning.bossDistance - 14f * scale, false, false, playerSpeed.bossVictoryLevel);
        }

        private void CreateSoldierSection(float tutorialEndZ, SoldierFormationSettings section, int sectionIndex)
        {
            if (section == null) return;
            GameObject sectionObject = new GameObject("SoldierSection_" + (sectionIndex + 1) + "_" + section.sectionName);
            sectionObject.transform.SetParent(worldRoot, false);
            float startZ = tutorialEndZ + section.startOffsetFromTutorial;
            int densityRows = Mathf.Clamp(section.soldierCount, 6, 50);
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
                    "Soldier_L1_" + section.placementMode + "_" + (soldierIndex + 1));
                generatedLevelOneSoldierCount++;
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

        public static int GetControlledRewardLane(int sectionIndex, int seed)
        {
            // Six entries contain two left, two center and two right placements, while
            // avoiding repeated center groups. The seed only rotates this stable sequence.
            int[] sequence = { -1, 1, 0, 1, -1, 0 };
            int seedOffset = (int)((uint)seed % (uint)sequence.Length);
            return sequence[(Mathf.Max(0, sectionIndex) + seedOffset) % sequence.Length];
        }

        private bool IsNearSoldierSection(float z, float tutorialEndZ, float padding)
        {
            if (prefab == null || prefab.soldierSections == null) return false;
            for (int i = 0; i < prefab.soldierSections.Length; i++)
            {
                SoldierFormationSettings section = prefab.soldierSections[i];
                if (section == null) continue;
                float start = tutorialEndZ + section.startOffsetFromTutorial - padding;
                float end = start + Mathf.Max(0, section.soldierCount - 1) * GetSectionForwardSpacing(section) + padding * 2f;
                if (z >= start && z <= end) return true;
            }
            return false;
        }

        private bool IsNearStoneWallSection(float z, float tutorialEndZ, float padding)
        {
            if (prefab == null || prefab.additionalStoneWalls == null) return false;
            for (int i = 0; i < prefab.additionalStoneWalls.Length; i++)
            {
                StoneWallSectionSettings section = prefab.additionalStoneWalls[i];
                if (section == null) continue;
                float wallZ = tutorialEndZ + section.startOffsetFromTutorial;
                if (Mathf.Abs(z - wallZ) <= padding) return true;
            }
            return false;
        }

        private void CreateTarget(float x, float z, int tier, Transform parent = null, string objectName = "Target")
        {
            Vector3 dimensions = targetShapes.Get(tier);
            Color color = speedVisualProfile.Get(tier).primaryColor;
            GameObject root = CreateBox(objectName, new Vector3(x, dimensions.y * 0.5f, z), dimensions, color, parent != null ? parent : worldRoot);
            Renderer placeholderRenderer = root.GetComponent<Renderer>();
            ObstacleController obstacle = root.AddComponent<ObstacleController>();
            Collider[] colliders = root.GetComponents<Collider>();
            obstacle.Initialize(tier, ObstacleType.Soldier, root, root, colliders, ObstacleFeedbackType.NormalImpact);

            Renderer[] visibilityRenderers = placeholderRenderer != null
                ? new[] { placeholderRenderer }
                : Array.Empty<Renderer>();
            Renderer[] outlineSources = null;
            GameObject visualPrefab = tier == 1 ? tier1SoldierPrefab : tier == 4 ? tier4SoldierPrefab : null;
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
                    outlineSources = modelRenderers;
                    EnemySoldierVisual soldierVisual = visual.GetComponentInChildren<EnemySoldierVisual>(true);
                    soldierVisual?.Initialize(obstacle);
                }
                else
                {
                    Destroy(visualRoot.gameObject);
                    WarnTargetVisualFallback(tier, visualPrefab.name + " has no Renderer");
                }
            }
            else if (tier == 1 || tier == 4)
            {
                WarnTargetVisualFallback(tier, tier == 1 ? nameof(tier1SoldierPrefab) : nameof(tier4SoldierPrefab));
            }

            ObstacleOutline outline = root.AddComponent<ObstacleOutline>();
            outline.Initialize(outlinePresentation, speedController, obstacle, outlineSources);
            EnemyVisibilityController visibility = root.AddComponent<EnemyVisibilityController>();
            visibility.Initialize(visibilityRenderers, colliders, outline);
            encounters.Add(new Encounter
            {
                root = root,
                type = EncounterType.Target,
                tier = tier,
                outline = outline,
                obstacle = obstacle,
                visibility = visibility
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
            GameObject boundary = new GameObject(name);
            boundary.transform.SetParent(worldRoot, false);
            boundary.transform.position = new Vector3(x, environment.roadBoundaryHeight * 0.5f, tuning.bossDistance * 0.5f);
            BoxCollider collider = boundary.AddComponent<BoxCollider>();
            collider.size = new Vector3(
                environment.roadBoundaryThickness,
                environment.roadBoundaryHeight,
                tuning.bossDistance + 28f);
        }

        private void CreateElixir(float x, float z, bool openingBoost = false, bool fallbackBoost = false, int targetLevel = 0)
        {
            int resolvedTargetLevel = targetLevel > 0 ? Mathf.Clamp(targetLevel, 1, speedController.MaxLevel) : playerSpeed.tutorialElixirTargetLevel;
            GameObject root = new GameObject("RoyalElixir");
            root.transform.SetParent(worldRoot, false);
            root.transform.position = new Vector3(x, 0.9f, z);

            GameObject bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottle.transform.SetParent(root.transform, false);
            bottle.transform.localScale = new Vector3(0.42f, 0.72f, 0.42f);
            SpeedTierVisualData elixirTier = speedVisualProfile.Get(resolvedTargetLevel);
            bottle.GetComponent<Renderer>().sharedMaterial = RuntimeStyle.CreateMaterial(elixirTier.primaryColor, 0.15f, 0.9f);
            Destroy(bottle.GetComponent<Collider>());

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.transform.SetParent(root.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            cap.transform.localScale = new Vector3(0.32f, 0.2f, 0.32f);
            cap.GetComponent<Renderer>().sharedMaterial = RuntimeStyle.CreateMaterial(elixirTier.secondaryColor, 0.4f, 0.8f);
            Destroy(cap.GetComponent<Collider>());

            Renderer[] renderers = { bottle.GetComponent<Renderer>(), cap.GetComponent<Renderer>() };
            root.AddComponent<ElixirVisual>().Initialize(elixirPresentation, renderers, speedVisualProfile, resolvedTargetLevel);
            SphereCollider pickupCollider = root.AddComponent<SphereCollider>();
            pickupCollider.isTrigger = true;
            pickupCollider.radius = 1.1f;
            ElixirPickup pickup = root.AddComponent<ElixirPickup>();
            pickup.Initialize(speedController, resolvedTargetLevel, new Collider[] { pickupCollider });
            encounters.Add(new Encounter { root = root, type = EncounterType.Elixir, tier = resolvedTargetLevel, openingBoost = openingBoost, fallbackBoost = fallbackBoost, elixir = pickup });
        }

        private void CreateBreakableWall(float z, string objectName = "TutorialStoneWall",
            StoneWallBlockingMode blockingMode = StoneWallBlockingMode.AllThreeLanes)
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
            obstacle.Initialize(playerSpeed.tutorialElixirTargetLevel, ObstacleType.StoneWall, root, root, root.GetComponentsInChildren<Collider>(), ObstacleFeedbackType.HeavyBreak);
            encounters.Add(new Encounter
            {
                root = root,
                type = EncounterType.Wall,
                tier = playerSpeed.tutorialElixirTargetLevel,
                wall = wall,
                obstacle = obstacle,
                wallCenterX = centerX,
                wallHalfWidth = halfWidth
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

            centerX = blockingMode == StoneWallBlockingMode.LeftAndCenter
                ? -roadHalfWidth / 3f
                : roadHalfWidth / 3f;
            halfWidth = roadHalfWidth * 2f / 3f;
            widthScale = 2f / 3f;
        }

        private void BuildUpgradeRing(Transform parent)
        {
            GameObject ringObject = new GameObject("UpgradeEnergyRing");
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
            if (name == "Road" || name == "RoadBand")
            {
                roadRenderers.Add(renderer);
            }
            return box;
        }


        private void SpawnImpactBurst(Vector3 position, Color color)
        {
            effectPool?.PlayImpact(position, color, 1f);
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (showSpeedDebugOverlay && speedController != null)
            {
                string diagnostics = "Speed " + speedController.CurrentSpeed.ToString("F2") + "  Level " + speedController.GetCurrentLevel() + "/" + speedController.MaxLevel
                    + "\nTarget " + TargetForwardSpeed.ToString("F2") + " m/s  Actual " + CurrentForwardSpeed.ToString("F2") + " m/s"
                    + "\nFOV " + (gameCamera != null ? gameCamera.fieldOfView.ToString("F1") : "-") + "  Anim " + CurrentAnimationSpeed.ToString("F2")
                    + "  Wind " + (audioFeedback != null ? audioFeedback.CurrentWindVolume.ToString("F2") : "-")
                    + "\nLast " + speedController.LastSpeedChangeReason + " @ " + speedController.LastSpeedChangeTime.ToString("F2")
                    + "  SpeedLoss " + tuning.forwardSpeedLossEnabled + " @ " + tuning.forwardSpeedLossPerSecond.ToString("F2") + "/s"
                    + "\n[1-0] Level  [T] 100m test  Last " + (debugLastSegmentTime > 0f ? debugLastSegmentTime.ToString("F2") + "s" : "-");
                GUI.Box(new Rect(width - 315f, 18f, 300f, 130f), diagnostics);
            }
#endif

            if (!ending && elapsed < calloutUntil)
            {
                GUI.Label(new Rect(20f, height * 0.18f, width - 40f, 65f), callout, titleStyle);
            }

            if (!FormalStarted && !ending)
            {
                GUI.Label(new Rect(20f, height - 108f, width - 40f, 45f), "TRAINING - NO SPEED LOSS", smallStyle);
            }
            else if (!ending && !bossSequence)
            {
                GUI.Label(new Rect(20f, height - 108f, width - 40f, 45f), "KEEP MOMENTUM", smallStyle);
            }

            if (fallbackActive && !ending)
            {
                GUI.Label(new Rect(20f, height * 0.26f, width - 40f, 48f), "COLLECT EVERY BLUE ELIXIR", bodyStyle);
            }

            if (ending)
            {
                DrawRect(new Rect(0f, 0f, width, height), new Color(0.02f, 0.03f, 0.04f, 0.72f));
                GUI.Label(new Rect(25f, height * 0.28f, width - 50f, 72f), "PRINCESS RESCUED!", titleStyle);
                GUI.Label(new Rect(25f, height * 0.38f, width - 50f, 52f), "THE KINGDOM NEEDS YOU", bodyStyle);
                if (GUI.Button(new Rect(72f, height * 0.56f, width - 144f, 72f), "PLAY NOW", buttonStyle))
                {
                    Debug.Log("PlayableAd CTA clicked. Store URL is not configured yet.");
                }
            }

            if (flashAlpha > 0.001f)
            {
                DrawRect(new Rect(0f, 0f, width, height), new Color(1f, 0.86f, 0.4f, flashAlpha));
            }
        }

        private void EnsureGuiStyles()
        {
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
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 27,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = MakeTexture(new Color(0.95f, 0.25f, 0.08f)) },
                active = { textColor = Color.white, background = MakeTexture(new Color(1f, 0.42f, 0.08f)) }
            };
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
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
