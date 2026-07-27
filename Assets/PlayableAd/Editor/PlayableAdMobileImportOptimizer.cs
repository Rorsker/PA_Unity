using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlayableAd.Editor
{
    public static class PlayableAdMobileImportOptimizer
    {
        private static readonly HashSet<string> PotionTextureNames = new HashSet<string>(
            new[]
            {
                "MagicPotion_Albedo_with_Alpha.png",
                "MagicPotion_Emissive.png",
                "MagicPotion_Albedo_with_Alpha2K.png",
                "MagicPotion_Emissive2K.png",
                "MagicPotion_Roughness2K.png"
            },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> EnvironmentTextureNames = new HashSet<string>(
            new[]
            {
                "RoadBorder_Albedo.tga",
                "RoadBorder_Normal.png",
                "StoneRoad_Albedo.png",
                "StoneRoad_Normal.png",
                "T_trim01_BC.TGA",
                "T_stonePlain01_BC.TGA"
            },
            StringComparer.OrdinalIgnoreCase);

        private static readonly string[] MobilePlatforms = { "WebGL", "Android", "iPhone" };

        [MenuItem("Tools/PlayableAd/Optimize Mobile Texture Imports")]
        public static void Optimize()
        {
            int changed = 0;
            string[] paths = AssetDatabase.GetAllAssetPaths();
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    string path = paths[i];
                    if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) continue;

                    string fileName = Path.GetFileName(path);
                    int maximumSize = PotionTextureNames.Contains(fileName)
                        ? 512
                        : EnvironmentTextureNames.Contains(fileName) ? 1024 : 0;
                    if (maximumSize == 0) continue;

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    bool importerChanged = false;
                    for (int platformIndex = 0; platformIndex < MobilePlatforms.Length; platformIndex++)
                    {
                        string platform = MobilePlatforms[platformIndex];
                        TextureImporterPlatformSettings settings =
                            importer.GetPlatformTextureSettings(platform);
                        if (settings.overridden && settings.maxTextureSize == maximumSize
                            && settings.textureCompression == TextureImporterCompression.Compressed
                            && settings.compressionQuality == 60)
                        {
                            continue;
                        }

                        settings.name = platform;
                        settings.overridden = true;
                        settings.maxTextureSize = maximumSize;
                        settings.format = TextureImporterFormat.Automatic;
                        settings.textureCompression = TextureImporterCompression.Compressed;
                        settings.compressionQuality = 60;
                        settings.crunchedCompression = false;
                        importer.SetPlatformTextureSettings(settings);
                        importerChanged = true;
                    }

                    if (!importerChanged) continue;
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[PlayableAd] Updated mobile texture import overrides for " + changed + " assets.");
        }

        public static void OptimizeFromCommandLine()
        {
            Optimize();
        }
    }
}
