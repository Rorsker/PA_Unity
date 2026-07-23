using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayableAd.Editor
{
    public static class PlayableAdSceneCleanup
    {
        [MenuItem("Tools/PlayableAd/Cleanup Runtime Scene Residue")]
        public static void CleanupRuntimeSceneResidue()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;

            int removed = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || !IsKnownRuntimeResidue(root.name)) continue;
                Undo.DestroyObjectImmediate(root);
                removed++;
            }

            if (removed <= 0)
            {
                Debug.Log("[PlayableAd] No saved runtime VFX residue was found in the active scene.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PlayableAd] Removed " + removed
                + " saved runtime VFX/preview roots from " + scene.path + ".");
        }

        private static bool IsKnownRuntimeResidue(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return false;
            return objectName.StartsWith("Afterimage_")
                || objectName == "SpeedLevelUpVFX_SonicBoomPool"
                || objectName == "player"
                || objectName == "20260721015024_2d184f8f_0"
                || objectName == "20260722122343_452ad5bc";
        }
    }
}
