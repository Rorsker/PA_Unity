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
        private float originalTimeScale;
        private float originalFixedDeltaTime;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            originalFixedDeltaTime = Time.fixedDeltaTime;
            root = new GameObject("SpeedTestRoot");
            speed = root.AddComponent<PlayerSpeedController>();
            settings = new PlayerSpeedSettings();
            speed.Initialize(settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
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
        public void AddSpeedPublishesFeedbackEvent()
        {
            speed.SetSpeed(3.5f);
            int eventCount = 0;
            SpeedChangedEvent last = default;
            speed.SpeedChanged += change =>
            {
                eventCount++;
                last = change;
            };

            speed.AddSpeed(0.2f);

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

            speed.AddSpeed(settings.levelOneSoldierBoost, SpeedChangeReason.LowLevelCollisionReward, root);

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
        public void LevelOneSoldierBoostCanRiseWithoutIntermediateCap()
        {
            speed.SetSpeed(6.45f);

            speed.AddSpeed(settings.levelOneSoldierBoost);
            float firstBoost = speed.CurrentSpeed;
            speed.AddSpeed(settings.levelOneSoldierBoost);

            Assert.That(settings.levelOneSoldierBoost, Is.EqualTo(0.12f).Within(0.0001f));
            Assert.That(firstBoost, Is.EqualTo(6.57f).Within(0.0001f));
            Assert.That(speed.CurrentSpeed, Is.EqualTo(6.69f).Within(0.0001f));
        }

        [Test]
        public void LevelFourPotionSpeedCanGainFromLevelOneSoldier()
        {
            speed.SetLevel(4, SpeedChangeReason.PotionPickup);
            float before = speed.CurrentSpeed;

            speed.AddSpeed(settings.levelOneSoldierBoost, SpeedChangeReason.LowLevelCollisionReward, root);

            Assert.That(before, Is.LessThan(settings.maximumSpeed));
            Assert.That(speed.CurrentSpeed, Is.EqualTo(before + 0.12f).Within(0.0001f));
            Assert.That(speed.GetCurrentLevel(), Is.EqualTo(4));
        }

        [Test]
        public void ObstacleResolvesOnceAndDisablesColliderImmediately()
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            ObstacleController obstacle = root.AddComponent<ObstacleController>();
            obstacle.Initialize(1, ObstacleType.Soldier, new Collider[] { collider });
            speed.SetLevel(3, SpeedChangeReason.Debug);
            int eventCount = 0;
            obstacle.Resolved += _ => eventCount++;

            ObstacleResolutionType first = obstacle.Resolve(speed, 0.2f);
            float afterFirst = speed.CurrentSpeed;
            obstacle.Resolve(speed, 0.2f);

            Assert.That(first, Is.EqualTo(ObstacleResolutionType.Boosted));
            Assert.That(afterFirst, Is.EqualTo(3.2f).Within(0.0001f));
            Assert.That(speed.CurrentSpeed, Is.EqualTo(afterFirst));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(collider.enabled, Is.False);
            Assert.That(obstacle.HasResolved, Is.True);
        }

        [Test]
        public void HigherRequirementStillBreaksAndDropsExactlyOneLevel()
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            ObstacleController obstacle = root.AddComponent<ObstacleController>();
            obstacle.Initialize(3, ObstacleType.StoneWall, new Collider[] { collider });
            speed.SetSpeed(2.4f);

            ObstacleResolutionType resolution = obstacle.Resolve(speed, 0.2f);

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
            obstacle.Initialize(3, ObstacleType.StoneWall, new Collider[] { collider });
            speed.SetLevel(3, SpeedChangeReason.Debug);

            ObstacleResolutionType resolution = obstacle.Resolve(speed, 0.2f);

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
                Assert.That(uiRoot.GetComponentsInChildren<UnityEngine.UI.Text>(true).Length, Is.EqualTo(11));
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
        public void CollisionOutcomeUsesRequiredLevelBoundary(int playerLevel, int requiredLevel, CollisionOutcome expected)
        {
            Assert.That(ObstacleController.EvaluateCollisionOutcome(playerLevel, requiredLevel), Is.EqualTo(expected));
        }

        [Test]
        public void ObstacleResolutionPublishesItsEvaluatedOutcome()
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            ObstacleController obstacle = root.AddComponent<ObstacleController>();
            obstacle.Initialize(5, ObstacleType.Soldier, new Collider[] { collider });
            speed.SetLevel(4, SpeedChangeReason.DebugCommand);
            CollisionOutcome published = CollisionOutcome.Neutral;
            obstacle.Resolved += resolved => published = resolved.Outcome;

            ObstacleResolutionType resolution = obstacle.Resolve(speed, settings.normalImpactBoost);

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
                visibility.Initialize(new Renderer[] { renderer }, new Collider[] { collider });

                visibility.SetState(EnemyVisibilityState.Preloaded);
                Assert.That(enemy.activeSelf, Is.False);
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

    }
}
