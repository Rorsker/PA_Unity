using NUnit.Framework;
using PlayableAd;
using UnityEditor;
using UnityEngine;

namespace PlayableAdEditor.Tests
{
    public sealed class PlayerSpeedControllerTests
    {
        private GameObject root;
        private PlayerSpeedController speed;
        private PlayerSpeedSettings settings;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("SpeedTestRoot");
            speed = root.AddComponent<PlayerSpeedController>();
            settings = new PlayerSpeedSettings();
            speed.Initialize(settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        [Test]
        public void ContinuousSpeedMapsToConfiguredLevelsAndForwardSpeed()
        {
            speed.SetSpeed(3.5f);

            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(3));
            Assert.That(speed.GetNormalizedProgressInLevel(), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(speed.GetForwardSpeed(), Is.EqualTo(9.25f).Within(0.0001f));
        }

        [Test]
        public void AuthoritativeConfigurationSupportsAllTenLevels()
        {
            speed.SetSpeed(10f);

            Assert.That(speed.LevelCount, Is.EqualTo(10));
            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(10));
            Assert.That(speed.GetForwardSpeed(), Is.EqualTo(26f).Within(0.0001f));
            Assert.That(settings.bossVictoryLevel, Is.EqualTo(10));
        }

        [Test]
        public void DropOneLevelLandsAtPreviousConfiguredLevelStart()
        {
            speed.SetSpeed(3.6f);

            speed.DropOneLevel();

            Assert.That(speed.CurrentSpeed, Is.EqualTo(2f));
            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(2));
        }

        [Test]
        public void TutorialUpgradeReachesLevelFourMovementSpeedWithinQuarterSecond()
        {
            PlayerForwardMotionController motion = root.AddComponent<PlayerForwardMotionController>();
            motion.Initialize(speed, settings);
            speed.SetLevel(4, SpeedChangeReason.TutorialElixir);

            motion.Tick(0.25f, true);

            Assert.That(motion.TargetForwardSpeed, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(motion.CurrentForwardSpeed, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void AutomaticMainRunDecayIsDisabledByDefault()
        {
            speed.SetSpeed(3f);
            int events = 0;
            speed.SpeedChanged += _ => events++;

            for (int i = 0; i < 1800; i++)
                speed.ApplyMainRunDecay(1f / 60f);

            Assert.That(speed.AutomaticSpeedDecayEnabled, Is.False);
            Assert.That(speed.CurrentSpeed, Is.EqualTo(3f).Within(0.000001f));
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void LegacyDecayRequiresExplicitOptIn()
        {
            settings.automaticSpeedDecayEnabled = true;
            speed.SetSpeed(3f);

            speed.ApplyMainRunDecay(0.5f);

            Assert.That(speed.CurrentSpeed, Is.EqualTo(2.95f).Within(0.0001f));
        }

        [TestCase(1f)]
        [TestCase(4f)]
        [TestCase(6.4f)]
        [TestCase(7f)]
        [TestCase(10f)]
        public void EmptyRoadThirtySecondsKeepsAllAuthoritativeSpeedValuesStable(float value)
        {
            speed.SetSpeed(value, SpeedChangeReason.DebugCommand);
            PlayerForwardMotionController motion = root.AddComponent<PlayerForwardMotionController>();
            motion.Initialize(speed, settings);
            float initialSpeed = speed.CurrentSpeed;
            int initialLevel = speed.GetCurrentLevel();
            float initialTarget = motion.TargetForwardSpeed;
            SpeedChangeReason initialReason = speed.LastSpeedChangeReason;
            int events = 0;
            speed.SpeedChanged += _ => events++;

            for (int i = 0; i < 1800; i++)
            {
                speed.ApplyMainRunDecay(1f / 60f);
                motion.Tick(1f / 60f, true);
            }

            Assert.That(speed.CurrentSpeed, Is.EqualTo(initialSpeed).Within(0.000001f));
            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(initialLevel));
            Assert.That(motion.TargetForwardSpeed, Is.EqualTo(initialTarget).Within(0.000001f));
            Assert.That(motion.CurrentForwardSpeed, Is.EqualTo(initialTarget).Within(0.000001f));
            Assert.That(speed.LastSpeedChangeReason, Is.EqualTo(initialReason));
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void HighestLevelHasAStableConfiguredBandBelowMaximum()
        {
            speed.SetSpeed(10f, SpeedChangeReason.DebugCommand);

            speed.ApplyMainRunDecay(1f / 60f);

            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(10));
            Assert.That(speed.CurrentSpeed, Is.EqualTo(10f));
        }

        [Test]
        public void AddSpeedIgnoresLegacySoftCapAndPublishesFeedbackEvent()
        {
            speed.SetSpeed(3.5f);
            int eventCount = 0;
            SpeedChangedEvent last = default;
            speed.SpeedChanged += change =>
            {
                eventCount++;
                last = change;
            };

            speed.AddSpeed(0.2f, 3.5f);

            Assert.That(speed.CurrentSpeed, Is.EqualTo(3.7f));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(last.NewValue, Is.GreaterThan(last.OldValue));
            Assert.That(last.Reason, Is.EqualTo(SpeedChangeReason.NormalImpact));
        }

        [Test]
        public void NormalBoostAtMaximumNeverExceedsGlobalMaximum()
        {
            speed.SetSpeed(10f, SpeedChangeReason.SpecialReward);
            int eventCount = 0;
            SpeedChangedEvent last = default;
            speed.SpeedChanged += change => { eventCount++; last = change; };

            speed.AddSpeed(settings.levelOneSoldierBoost, settings.normalImpactSoftCap,
                SpeedChangeReason.LowLevelCollisionReward, root);

            Assert.That(speed.CurrentSpeed, Is.EqualTo(10f));
            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(10));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(last.OldValue, Is.EqualTo(last.NewValue));
        }

        [Test]
        public void TutorialElixirRaisesDirectlyToLevelFourOnlyOnce()
        {
            SphereCollider collider = root.AddComponent<SphereCollider>();
            ElixirPickup pickup = root.AddComponent<ElixirPickup>();
            pickup.Initialize(speed, 4, new Collider[] { collider });

            bool first = pickup.TryCollect();
            int firstLevel = speed.GetCurrentLevel();
            speed.SetLevel(1, SpeedChangeReason.Debug);
            bool second = pickup.TryCollect();

            Assert.That(first, Is.True);
            Assert.That(firstLevel, Is.EqualTo(4));
            Assert.That(second, Is.False);
            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(1));
            Assert.That(collider.enabled, Is.False);
        }

        [Test]
        public void LevelOneSoldierBoostCanRisePastLegacySoftCap()
        {
            speed.SetSpeed(6.45f);

            speed.AddSpeed(settings.levelOneSoldierBoost, settings.normalImpactSoftCap);
            float firstBoost = speed.CurrentSpeed;
            speed.AddSpeed(settings.levelOneSoldierBoost, settings.normalImpactSoftCap);

            Assert.That(settings.levelOneSoldierBoost, Is.EqualTo(0.12f).Within(0.0001f));
            Assert.That(firstBoost, Is.EqualTo(6.57f).Within(0.0001f));
            Assert.That(speed.CurrentSpeed, Is.EqualTo(6.69f).Within(0.0001f));
        }

        [Test]
        public void LevelFourPotionSpeedCanGainFromLevelOneSoldier()
        {
            speed.SetLevel(4, SpeedChangeReason.PotionPickup);
            float before = speed.CurrentSpeed;

            speed.AddSpeed(settings.levelOneSoldierBoost, settings.normalImpactSoftCap,
                SpeedChangeReason.LowLevelCollisionReward, root);

            Assert.That(before, Is.LessThan(settings.maximumSpeed));
            Assert.That(speed.CurrentSpeed, Is.EqualTo(before + 0.12f).Within(0.0001f));
            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(4));
        }

        [Test]
        public void ObstacleResolvesOnceAndDisablesColliderImmediately()
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            ObstacleController obstacle = root.AddComponent<ObstacleController>();
            obstacle.Initialize(1, ObstacleType.Soldier, root, root, new Collider[] { collider }, ObstacleFeedbackType.NormalImpact);
            speed.SetLevel(3, SpeedChangeReason.Debug);
            int eventCount = 0;
            obstacle.Resolved += _ => eventCount++;

            ObstacleResolutionType first = obstacle.Resolve(speed, 0.2f, 3.5f);
            float afterFirst = speed.CurrentSpeed;
            obstacle.Resolve(speed, 0.2f, 3.5f);

            Assert.That(first, Is.EqualTo(ObstacleResolutionType.Boosted));
            Assert.That(afterFirst, Is.EqualTo(3.2f).Within(0.0001f));
            Assert.That(speed.CurrentSpeed, Is.EqualTo(afterFirst));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(collider.enabled, Is.False);
            Assert.That(obstacle.HasResolved, Is.True);
        }

        [Test]
        public void KnockedEnemyUsesSeparatePhysicsColliderAfterGameplayResolution()
        {
            BoxCollider gameplayCollider = root.AddComponent<BoxCollider>();
            ObstacleController obstacle = root.AddComponent<ObstacleController>();
            obstacle.Initialize(1, ObstacleType.Soldier, root, root, new Collider[] { gameplayCollider }, ObstacleFeedbackType.NormalImpact);
            KnockedEnemyPhysics knockedPhysics = root.AddComponent<KnockedEnemyPhysics>();
            knockedPhysics.Initialize(2.25f);
            Collider[] colliders = root.GetComponents<Collider>();
            Collider knockbackCollider = colliders[1];
            speed.SetLevel(3, SpeedChangeReason.Debug);

            obstacle.Resolve(speed, 0.2f, 3.5f);
            knockedPhysics.Launch(new Vector3(2.6f, 3.8f, 5.88f), Vector3.one);

            Assert.That(gameplayCollider.enabled, Is.False);
            Assert.That(knockbackCollider.enabled, Is.True);
            Assert.That(root.GetComponent<Rigidbody>().isKinematic, Is.False);
            Assert.That(knockedPhysics.IsLaunched, Is.True);
        }

        [Test]
        public void HigherRequirementStillBreaksAndDropsExactlyOneLevel()
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            ObstacleController obstacle = root.AddComponent<ObstacleController>();
            obstacle.Initialize(3, ObstacleType.StoneWall, root, root, new Collider[] { collider }, ObstacleFeedbackType.HeavyBreak);
            speed.SetSpeed(2.4f);

            ObstacleResolutionType resolution = obstacle.Resolve(speed, 0.2f, 3.5f);

            Assert.That(resolution, Is.EqualTo(ObstacleResolutionType.Dropped));
            Assert.That(speed.CurrentSpeed, Is.EqualTo(1f));
            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(1));
            Assert.That(collider.enabled, Is.False);
        }

        [Test]
        public void EqualLevelWallCanIncreaseSpeed()
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            ObstacleController obstacle = root.AddComponent<ObstacleController>();
            obstacle.Initialize(3, ObstacleType.StoneWall, root, root, new Collider[] { collider }, ObstacleFeedbackType.HeavyBreak);
            speed.SetLevel(3, SpeedChangeReason.Debug);

            ObstacleResolutionType resolution = obstacle.Resolve(speed, 0.2f, 3.5f);

            Assert.That(resolution, Is.EqualTo(ObstacleResolutionType.Boosted));
            Assert.That(speed.CurrentSpeed, Is.EqualTo(3.2f).Within(0.0001f));
            Assert.That(collider.enabled, Is.False);
        }

        [Test]
        public void SpeedVisualProfileUsesOneConfiguredSourceForAllTenLevels()
        {
            SpeedVisualProfile profile = AssetDatabase.LoadAssetAtPath<SpeedVisualProfile>(
                "Assets/PlayableAd/Visuals/SpeedVisualProfile.asset");

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.trailMaterial, Is.Not.Null);
            Assert.That(profile.lineMaterial, Is.Not.Null);
            Assert.That(profile.particleMaterial, Is.Not.Null);
            Assert.That(profile.LevelCount, Is.EqualTo(10));
            Assert.That(profile.Get(6).trailLength, Is.GreaterThan(profile.Get(1).trailLength * 4f));
            Assert.That(profile.Get(10).particleEmissionRate, Is.GreaterThan(profile.Get(9).particleEmissionRate));
            Assert.That(profile.Get(10).trailLength, Is.GreaterThan(profile.Get(9).trailLength));
            Assert.That(profile.Get(1).uiColor, Is.EqualTo(profile.Get(1).primaryColor));
        }

        [Test]
        public void VisualTimeScaleRestoreReturnsTimeAndFixedStepToBaseline()
        {
            float originalScale = Time.timeScale;
            float originalFixedDelta = Time.fixedDeltaTime;
            VisualTimeScaleController controller = root.AddComponent<VisualTimeScaleController>();

            controller.RequestSlowMotion(0.4f, 0.15f);

            Assert.That(Time.timeScale, Is.EqualTo(originalScale * 0.4f).Within(0.0001f));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(originalFixedDelta * 0.4f).Within(0.0001f));

            controller.Restore();

            Assert.That(Time.timeScale, Is.EqualTo(originalScale).Within(0.0001f));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(originalFixedDelta).Within(0.0001f));
        }

        [Test]
        public void SpeedBarBuildsCanvasAndTenConfiguredSlots()
        {
            SpeedVisualProfile profile = AssetDatabase.LoadAssetAtPath<SpeedVisualProfile>(
                "Assets/PlayableAd/Visuals/SpeedVisualProfile.asset");
            GameObject uiRoot = new GameObject("SpeedBarTest");
            try
            {
                SpeedBarView view = uiRoot.AddComponent<SpeedBarView>();
                view.Initialize(speed, profile);

                Assert.That(uiRoot.GetComponent<Canvas>(), Is.Not.Null);
                UnityEngine.UI.CanvasScaler scaler = uiRoot.GetComponent<UnityEngine.UI.CanvasScaler>();
                Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
                Assert.That(uiRoot.transform.Find("SafeArea/TopHUD/SpeedBar"), Is.Not.Null);
                Assert.That(uiRoot.transform.Find("SafeArea/TopHUD/SpeedBar/Background/Fill"), Is.Not.Null);
                Assert.That(uiRoot.transform.Find("SafeArea/TopHUD/SpeedBar/LevelTicks").childCount, Is.EqualTo(10));
                Assert.That(uiRoot.GetComponentsInChildren<UnityEngine.UI.Text>(true).Length, Is.EqualTo(12));
                Assert.That(uiRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true).Length, Is.GreaterThanOrEqualTo(25));
            }
            finally
            {
                Object.DestroyImmediate(uiRoot);
            }
        }

        [Test]
        public void AudioFeedbackBuildsProceduralPlaceholdersWithinFixedVoiceBudget()
        {
            AudioFeedbackSettings audioSettings = new AudioFeedbackSettings
            {
                audioEnabled = false,
                hapticsEnabled = false,
                useProceduralPlaceholders = true,
                actionVoiceCount = 3
            };
            AudioFeedbackController controller = root.AddComponent<AudioFeedbackController>();

            controller.Initialize(audioSettings);
            controller.Initialize(audioSettings);

            Assert.That(controller.ActionVoiceCount, Is.EqualTo(3));
            Assert.That(controller.ProceduralClipCount, Is.GreaterThanOrEqualTo(20));
            Assert.That(audioSettings.footstepsLoop, Is.Not.Null);
            Assert.That(audioSettings.tierDrop, Is.Not.Null);
            Assert.That(audioSettings.soldierImpactVariants.Length, Is.EqualTo(4));
            Assert.That(audioSettings.impactTransient, Is.Not.Null);
            Assert.That(audioSettings.armorContact, Is.Not.Null);
            Assert.That(audioSettings.bodyWeight, Is.Not.Null);
            Assert.That(audioSettings.armorBreak, Is.Not.Null);
            Assert.That(audioSettings.highSpeedWhoosh, Is.Not.Null);
            Assert.That(audioSettings.wallLowImpact, Is.Not.Null);
            Assert.That(audioSettings.bossFinishImpact, Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<AudioSource>(true).Length, Is.EqualTo(8));
        }

        [Test]
        public void BackgroundMusicUsesDedicatedLoopSourceAtConfiguredVolume()
        {
            AudioClip clip = AudioClip.Create("BackgroundMusicTest", 256, 2, 22050, false);
            try
            {
                AudioFeedbackSettings audioSettings = new AudioFeedbackSettings
                {
                    audioEnabled = true,
                    hapticsEnabled = false,
                    useProceduralPlaceholders = false,
                    masterVolume = 0.5f,
                    backgroundMusicLoop = clip,
                    backgroundMusicVolume = 0.4f,
                    actionVoiceCount = 2
                };
                AudioFeedbackController controller = root.AddComponent<AudioFeedbackController>();

                controller.Initialize(audioSettings);

                Transform musicObject = root.transform.Find("Audio_Music");
                Assert.That(musicObject, Is.Not.Null);
                AudioSource source = musicObject.GetComponent<AudioSource>();
                Assert.That(source, Is.Not.Null);
                Assert.That(source.clip, Is.EqualTo(clip));
                Assert.That(source.loop, Is.True);
                Assert.That(source.playOnAwake, Is.False);
                Assert.That(source.volume, Is.EqualTo(0.2f).Within(0.0001f));
                Assert.That(source.priority, Is.EqualTo(96));
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void CollisionAudioFallsBackWhenVariantSlotIsMissingAndUsesWorldPosition()
        {
            AudioClip fallback = AudioClip.Create("CollisionFallbackTest", 256, 1, 22050, false);
            try
            {
                AudioFeedbackSettings audioSettings = new AudioFeedbackSettings
                {
                    audioEnabled = true,
                    hapticsEnabled = false,
                    useProceduralPlaceholders = false,
                    actionVoiceCount = 3,
                    soldierImpactVariants = new AudioClip[] { null },
                    neutralImpact = fallback,
                    collisionSpatialBlend = 0.8f,
                    collisionMinDistance = 1.5f,
                    collisionMaxDistance = 18f
                };
                AudioFeedbackController controller = root.AddComponent<AudioFeedbackController>();
                controller.Initialize(audioSettings);
                Vector3 impactPosition = new Vector3(2f, 1f, 14f);

                controller.PlayCollisionOutcome(CollisionOutcome.Neutral, 0, 0f, 0f, impactPosition);

                Assert.That(controller.LastCollisionLayerCount, Is.EqualTo(1));
                Assert.That(controller.LastCollisionWasSpatial, Is.True);
                Assert.That(controller.LastCollisionPosition, Is.EqualTo(impactPosition));
                AudioSource[] sources = root.GetComponentsInChildren<AudioSource>(true);
                bool foundSpatialFallback = false;
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i].clip != fallback) continue;
                    foundSpatialFallback = true;
                    Assert.That(sources[i].spatialBlend, Is.EqualTo(0.8f).Within(0.0001f));
                    Assert.That(sources[i].transform.position, Is.EqualTo(impactPosition));
                }
                Assert.That(foundSpatialFallback, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(fallback);
            }
        }

        [Test]
        public void CollisionAndUpgradeAudioGrowStrongerAndFasterWithSpeed()
        {
            AudioClip clip = AudioClip.Create("SpeedResponsiveAudioTest", 256, 1, 22050, false);
            GameObject lowObject = new GameObject("LowSpeedAudio");
            GameObject highObject = new GameObject("HighSpeedAudio");
            lowObject.transform.SetParent(root.transform);
            highObject.transform.SetParent(root.transform);
            try
            {
                AudioFeedbackSettings lowSettings = CreateSpeedResponsiveAudioSettings(clip);
                AudioFeedbackSettings highSettings = CreateSpeedResponsiveAudioSettings(clip);
                AudioFeedbackController low = lowObject.AddComponent<AudioFeedbackController>();
                AudioFeedbackController high = highObject.AddComponent<AudioFeedbackController>();
                low.Initialize(lowSettings);
                high.Initialize(highSettings);

                low.PlayCollisionOutcome(CollisionOutcome.Neutral, 0, 0f, 0f, Vector3.zero);
                high.PlayCollisionOutcome(CollisionOutcome.Neutral, 0, 0f, 1f, Vector3.zero);
                low.PlaySpeedLevelUp(1, false, false);
                high.PlaySpeedLevelUp(PlayerSpeedSettings.RequiredLevelCount, false, false);

                Assert.That(high.LastCollisionVolume, Is.GreaterThan(low.LastCollisionVolume + 0.2f));
                Assert.That(high.LastCollisionPitch, Is.GreaterThan(low.LastCollisionPitch + 0.12f));
                Assert.That(high.LastUpgradeVolume, Is.GreaterThan(low.LastUpgradeVolume + 0.15f));
                Assert.That(high.LastUpgradePitch, Is.GreaterThan(low.LastUpgradePitch + 0.2f));
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        private static AudioFeedbackSettings CreateSpeedResponsiveAudioSettings(AudioClip clip)
        {
            return new AudioFeedbackSettings
            {
                audioEnabled = true,
                hapticsEnabled = false,
                useProceduralPlaceholders = false,
                masterVolume = 1f,
                normalImpactVolume = 0.62f,
                upgradeVolume = 0.68f,
                actionVoiceCount = 5,
                impactTransient = clip,
                neutralImpact = clip,
                tierUpgrade = clip,
                soldierImpactVariants = new[] { clip }
            };
        }

        [TestCase(4, 1, CollisionOutcome.SpeedGain)]
        [TestCase(4, 4, CollisionOutcome.SpeedGain)]
        [TestCase(4, 5, CollisionOutcome.SpeedLoss)]
        public void CollisionOutcomeUsesOneThreeStateBoundary(int playerLevel, int requiredLevel, CollisionOutcome expected)
        {
            Assert.That(ObstacleController.EvaluateCollisionOutcome(playerLevel, requiredLevel), Is.EqualTo(expected));
        }

        [Test]
        public void ControlledRewardLanesAreSeededBalancedAndNeverTripleCenter()
        {
            int[] first = new int[6];
            int[] repeated = new int[6];
            int left = 0;
            int center = 0;
            int right = 0;
            for (int i = 0; i < first.Length; i++)
            {
                first[i] = PlayableAdGame.GetControlledRewardLane(i, 41723);
                repeated[i] = PlayableAdGame.GetControlledRewardLane(i, 41723);
                if (first[i] < 0) left++;
                else if (first[i] > 0) right++;
                else center++;
            }

            CollectionAssert.AreEqual(first, repeated);
            Assert.That(left, Is.EqualTo(2));
            Assert.That(center, Is.EqualTo(2));
            Assert.That(right, Is.EqualTo(2));
            for (int i = 2; i < first.Length; i++)
                Assert.That(first[i - 2] == 0 && first[i - 1] == 0 && first[i] == 0, Is.False);
        }

        [Test]
        public void EnemyBreakPoolUsesFixedCapacityAndNonCollidingFragments()
        {
            GameObject poolObject = new GameObject("EnemyBreakPoolTest");
            poolObject.transform.SetParent(root.transform);
            EnemyBreakEffectPool pool = poolObject.AddComponent<EnemyBreakEffectPool>();
            EnemyBreakPresentationSettings presentation = new EnemyBreakPresentationSettings
            {
                maxActiveFragments = 24,
                fragmentsPerEnemy = 4
            };
            pool.Initialize(presentation, new VisualPerformanceSettings());

            pool.PlayBreak(Vector3.zero, Vector3.one, Color.green, 1f, 1f);

            Assert.That(pool.Capacity, Is.EqualTo(24));
            Assert.That(pool.ActiveFragmentCount, Is.EqualTo(4));
            Collider[] colliders = poolObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) Assert.That(colliders[i].enabled, Is.False);
        }

        [Test]
        public void ObstacleResolutionPublishesTheSameOutcomeUsedByPreview()
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            ObstacleController obstacle = root.AddComponent<ObstacleController>();
            obstacle.Initialize(5, ObstacleType.Soldier, root, root, new Collider[] { collider }, ObstacleFeedbackType.NormalImpact);
            speed.SetLevel(4, SpeedChangeReason.DebugCommand);
            CollisionOutcome published = CollisionOutcome.Neutral;
            obstacle.Resolved += resolved => published = resolved.Outcome;

            ObstacleResolutionType resolution = obstacle.Resolve(speed, settings.normalImpactBoost, settings.normalImpactSoftCap);

            Assert.That(published, Is.EqualTo(CollisionOutcome.SpeedLoss));
            Assert.That(resolution, Is.EqualTo(ObstacleResolutionType.Dropped));
            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(3));
        }

        [Test]
        public void DirectMultiLevelUpgradePublishesOneAuthoritativeLevelEvent()
        {
            int eventCount = 0;
            SpeedLevelChangeData last = default;
            speed.SpeedLevelChanged += change => { eventCount++; last = change; };

            speed.SetLevel(4, SpeedChangeReason.PotionPickup, root);

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(last.OldLevel, Is.EqualTo(1));
            Assert.That(last.NewLevel, Is.EqualTo(4));
            Assert.That(last.LevelsChanged, Is.EqualTo(3));
            Assert.That(last.IsLevelUp, Is.True);
            Assert.That(last.IsMajorLevel, Is.True);
        }

        [Test]
        public void SameLevelBoostDoesNotPublishFormalLevelEvent()
        {
            speed.SetSpeed(4.2f, SpeedChangeReason.DebugCommand);
            int eventCount = 0;
            speed.SpeedLevelChanged += _ => eventCount++;

            speed.SetSpeed(4.32f, SpeedChangeReason.LowLevelCollisionReward);

            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void RoutePreviewSimulatesOrderedGainWithoutLegacySoftCap()
        {
            var steps = new System.Collections.Generic.List<RoutePreviewStep>();
            for (int i = 0; i < 30; i++)
                steps.Add(RoutePreviewStep.Obstacle(1, settings.levelOneSoldierBoost, settings.normalImpactSoftCap));
            float realSpeed = speed.CurrentSpeed;

            RouteEvaluation result = RoutePreviewEvaluator.Evaluate(4f, steps, settings, new RoutePreviewSettings());

            Assert.That(result.State, Is.EqualTo(RouteState.StrongGain));
            Assert.That(result.ExpectedEndSpeed, Is.EqualTo(7.6f).Within(0.0001f));
            Assert.That(result.GainTargetCount, Is.EqualTo(30));
            Assert.That(speed.CurrentSpeed, Is.EqualTo(realSpeed));
        }

        [Test]
        public void RoutePreviewDistinguishesGainRiskAndSpecialBoost()
        {
            RoutePreviewSettings preview = new RoutePreviewSettings();
            var gain = new[] { RoutePreviewStep.Obstacle(4, settings.normalImpactBoost, settings.normalImpactSoftCap) };
            var risk = new[] { RoutePreviewStep.Obstacle(5, settings.normalImpactBoost, settings.normalImpactSoftCap) };
            var special = new[] { RoutePreviewStep.SetLevelReward(7) };

            Assert.That(RoutePreviewEvaluator.Evaluate(4f, gain, settings, preview).State, Is.EqualTo(RouteState.Gain));
            Assert.That(RoutePreviewEvaluator.Evaluate(4f, risk, settings, preview).State, Is.EqualTo(RouteState.HeavyRisk));
            Assert.That(RoutePreviewEvaluator.Evaluate(4f, special, settings, preview).State, Is.EqualTo(RouteState.SpecialBoost));
        }

        [Test]
        public void PositiveRouteWithIntermediateDropKeepsRiskBadgeData()
        {
            var steps = new System.Collections.Generic.List<RoutePreviewStep>
            {
                RoutePreviewStep.Obstacle(5, settings.normalImpactBoost, settings.normalImpactSoftCap)
            };
            for (int i = 0; i < 24; i++)
                steps.Add(RoutePreviewStep.Obstacle(1, settings.levelOneSoldierBoost, settings.normalImpactSoftCap));

            RouteEvaluation result = RoutePreviewEvaluator.Evaluate(4f, steps, settings, new RoutePreviewSettings());

            Assert.That(result.ExpectedSpeedDelta, Is.GreaterThan(0.15f));
            Assert.That(result.HasForcedSpeedLoss, Is.True);
            Assert.That(result.State, Is.EqualTo(RouteState.StrongGain));
        }

        [Test]
        public void LevelFeedbackConfigMarksOnlyConfiguredMilestonesAsMajor()
        {
            SpeedLevelFeedbackConfig config = ScriptableObject.CreateInstance<SpeedLevelFeedbackConfig>();
            try
            {
                Assert.That(config.Get(4).isMajorLevel, Is.True);
                Assert.That(config.Get(7).isMajorLevel, Is.True);
                Assert.That(config.Get(9).isMajorLevel, Is.True);
                Assert.That(config.Get(10).isMajorLevel, Is.True);
                Assert.That(config.Get(6).isMajorLevel, Is.False);
                Assert.That(config.Get(10).shockwaveScale, Is.GreaterThan(config.Get(4).shockwaveScale));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void EnemyVisibilityStagesSeparateRenderingFromGameplayCollision()
        {
            GameObject enemy = new GameObject("VisibilityStageTest");
            try
            {
                MeshRenderer renderer = enemy.AddComponent<MeshRenderer>();
                BoxCollider collider = enemy.AddComponent<BoxCollider>();
                EnemyVisibilityController visibility = enemy.AddComponent<EnemyVisibilityController>();
                visibility.Initialize(new Renderer[] { renderer }, new Collider[] { collider }, null);

                visibility.SetState(EnemyVisibilityState.Preloaded);
                Assert.That(enemy.activeSelf, Is.True);
                Assert.That(renderer.enabled, Is.False);
                Assert.That(collider.enabled, Is.False);

                visibility.SetState(EnemyVisibilityState.DistantVisible);
                Assert.That(renderer.enabled, Is.True);
                Assert.That(collider.enabled, Is.False);

                visibility.SetState(EnemyVisibilityState.Active);
                Assert.That(renderer.enabled, Is.True);
                Assert.That(collider.enabled, Is.True);

                visibility.MarkKnockedBack();
                Assert.That(visibility.State, Is.EqualTo(EnemyVisibilityState.KnockedBack));
                Assert.That(collider.enabled, Is.False);
                visibility.Recycle();
                Assert.That(visibility.State, Is.EqualTo(EnemyVisibilityState.Recycled));
                Assert.That(enemy.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void RoutePreviewDefaultsRemainShortThinAndLowAlpha()
        {
            RoutePreviewSettings route = new RoutePreviewSettings();

            Assert.That(route.ribbonLength, Is.InRange(3f, 8f));
            Assert.That(route.ribbonWidth, Is.LessThanOrEqualTo(0.15f));
            Assert.That(route.normalRibbonAlpha, Is.InRange(0.15f, 0.3f));
            Assert.That(route.recommendedRibbonAlpha, Is.InRange(0.3f, 0.45f));
            Assert.That(route.recommendedRibbonAlpha, Is.GreaterThan(route.normalRibbonAlpha));
        }
    }
}
