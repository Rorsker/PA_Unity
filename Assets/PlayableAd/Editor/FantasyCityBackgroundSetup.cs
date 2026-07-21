using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayableAd.Editor
{
    [InitializeOnLoad]
    public static class FantasyCityBackgroundSetup
    {
        private const string RawPath = "Assets/PlayableAd/Visuals/Background/Source/FantasyCityBackground_Raw.jpg";
        private const string AdaptedPath = "Assets/PlayableAd/Visuals/Background/Processed/FantasyCityBackground_Adapted.jpg";
        private const string FogPath = "Assets/PlayableAd/Visuals/Background/Processed/FantasyCityFogExtension.png";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string SessionKey = "PlayableAd.FantasyCityBackgroundSetup.20260720.v1";

        static FantasyCityBackgroundSetup()
        {
            if (!SessionState.GetBool(SessionKey, false) && File.Exists(RawPath))
                EditorApplication.delayCall += AutoBuild;
        }

        [MenuItem("Playable Ad/Build Fantasy City Background")]
        public static void Build()
        {
            EnsureFolder("Assets/PlayableAd/Visuals/Background");
            EnsureFolder("Assets/PlayableAd/Visuals/Background/Source");
            EnsureFolder("Assets/PlayableAd/Visuals/Background/Processed");

            File.Copy(RawPath, AdaptedPath, true);
            CreateFogTexture();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureSpriteImporter(AdaptedPath, TextureImporterCompression.CompressedHQ, 2048);
            ConfigureSpriteImporter(FogPath, TextureImporterCompression.Uncompressed, 512);
            EnsureBackgroundSortingLayer();

            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(AdaptedPath);
            Sprite fog = AssetDatabase.LoadAssetAtPath<Sprite>(FogPath);
            if (background == null || fog == null)
                throw new InvalidOperationException("Unable to import background sprites.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlayableAdGame game = UnityEngine.Object.FindObjectOfType<PlayableAdGame>();
            if (game == null) throw new InvalidOperationException("PlayableAdGame not found in " + ScenePath);

            DistantBackgroundController controller = game.GetComponent<DistantBackgroundController>();
            if (controller == null) controller = game.gameObject.AddComponent<DistantBackgroundController>();
            controller.Configure(background, fog);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            SessionState.SetBool(SessionKey, true);
            Debug.Log("Fantasy city background setup completed: 1024x768 source, focusX=0.58, overscan=1.08, brightness=0.84, fogHeight=0.48.");
            Selection.activeObject = controller;
        }

        [MenuItem("Playable Ad/Background QA/Set Game View 9x16")]
        public static void SetGameView9x16()
        {
            Screen.SetResolution(1080, 1920, false);
        }

        [MenuItem("Playable Ad/Background QA/Set Game View 9x19.5")]
        public static void SetGameView9x19_5()
        {
            Screen.SetResolution(1080, 2340, false);
        }

        [MenuItem("Playable Ad/Background QA/Set Game View 9x20")]
        public static void SetGameView9x20()
        {
            Screen.SetResolution(1080, 2400, false);
        }

        private static void AutoBuild()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoBuild;
                return;
            }

            try
            {
                Build();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void CreateFogTexture()
        {
            const int width = 32;
            const int height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            var pixels = new Color32[width * height];
            Color top = new Color(0.34f, 0.42f, 0.53f, 0f);
            Color middle = new Color(0.25f, 0.33f, 0.43f, 0.68f);
            Color bottom = new Color(0.13f, 0.18f, 0.24f, 0.98f);
            for (int y = 0; y < height; y++)
            {
                float t = y / (height - 1f);
                Color color = t < 0.45f
                    ? Color.Lerp(bottom, middle, t / 0.45f)
                    : Color.Lerp(middle, top, (t - 0.45f) / 0.55f);
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(FogPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void ConfigureSpriteImporter(string path, TextureImporterCompression compression, int maxSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("TextureImporter missing for " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = compression;
            importer.compressionQuality = 85;
            importer.maxTextureSize = maxSize;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
        }

        private static void EnsureBackgroundSortingLayer()
        {
            UnityEngine.Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Background")
                    return;
            }

            layers.InsertArrayElementAtIndex(0);
            SerializedProperty layer = layers.GetArrayElementAtIndex(0);
            layer.FindPropertyRelative("name").stringValue = "Background";
            layer.FindPropertyRelative("uniqueID").longValue = DateTime.UtcNow.Ticks & 0x7fffffff;
            layer.FindPropertyRelative("locked").boolValue = false;
            tagManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(tagManagerAsset);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
