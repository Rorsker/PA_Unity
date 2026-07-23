using System.Collections;
using NUnit.Framework;
using PlayableAd;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlayableAdEditor.Tests
{
    public sealed class PlayableAdRuntimeSmokeTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [UnityTest]
        public IEnumerator SampleSceneUsesBoundedSoldierVisualsAndCombinedRoadBands()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();

            for (int i = 0; i < 90; i++) yield return null;

            PlayableAdGame game = Object.FindObjectOfType<PlayableAdGame>();
            Assert.That(game, Is.Not.Null, "PlayableAdGame did not start.");

            EnemyVisibilityController[] targets =
                Object.FindObjectsOfType<EnemyVisibilityController>(true);
            PooledSoldierVisualInstance[] heavyVisuals =
                Object.FindObjectsOfType<PooledSoldierVisualInstance>(true);
            Assert.That(targets.Length, Is.GreaterThan(50),
                "The configured main run did not create its collision roots.");
            Assert.That(heavyVisuals.Length, Is.LessThan(targets.Length),
                "Every soldier still owns a heavyweight model; lazy pooling is not active.");

            GameObject combinedBands = GameObject.Find("CombinedRoadBands");
            Assert.That(combinedBands, Is.Not.Null, "Combined road-band mesh is missing.");
            GameObject[] sceneObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            int legacyRoadBands = 0;
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                Transform[] children = sceneObjects[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < children.Length; j++)
                    if (children[j].name == "RoadBand") legacyRoadBands++;
            }
            Assert.That(legacyRoadBands, Is.Zero,
                "Legacy per-band road renderers were created alongside the combined mesh.");

            yield return new ExitPlayMode();
        }
    }
}
