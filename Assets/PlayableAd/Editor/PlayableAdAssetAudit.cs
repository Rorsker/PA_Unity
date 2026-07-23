using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PlayableAdEditor
{
    public static class PlayableAdAssetAudit
    {
        private const string ReportPath = "Logs/PlayableAdAssetAudit.txt";
        private static readonly HashSet<string> SourceExtensions = new HashSet<string>(
            new[] { ".7z", ".zip", ".spp", ".ma", ".mb", ".max" },
            StringComparer.OrdinalIgnoreCase);

        [MenuItem("Tools/Playable Ad/Generate Build Dependency Audit")]
        public static void Generate()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddDependencies(included, scenes);

            string[] allAssets = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            string[] alwaysIncludedFolders = allAssets
                .Where(path => path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("/StreamingAssets/", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            AddDependencies(included, alwaysIncludedFolders);

            List<AssetSize> includedFiles = included
                .Select(CreateAssetSize)
                .Where(item => item.Bytes >= 0)
                .OrderByDescending(item => item.Bytes)
                .ToList();
            List<AssetSize> unusedSourceFiles = allAssets
                .Where(path => SourceExtensions.Contains(Path.GetExtension(path)))
                .Where(path => !included.Contains(path))
                .Select(CreateAssetSize)
                .Where(item => item.Bytes >= 0)
                .OrderByDescending(item => item.Bytes)
                .ToList();

            var builder = new StringBuilder(16 * 1024);
            builder.AppendLine("Playable Ad Build Dependency Audit");
            builder.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine("Build scenes: " + string.Join(", ", scenes));
            builder.AppendLine("Included project assets: " + includedFiles.Count);
            builder.AppendLine("Included source size (uncompressed): "
                + FormatBytes(includedFiles.Sum(item => item.Bytes)));
            builder.AppendLine();
            builder.AppendLine("Included size by extension:");
            foreach (var group in includedFiles.GroupBy(item => item.Extension)
                         .OrderByDescending(group => group.Sum(item => item.Bytes)))
            {
                builder.AppendLine(string.Format("{0,-10} {1,6} files  {2,12}",
                    string.IsNullOrEmpty(group.Key) ? "<none>" : group.Key,
                    group.Count(), FormatBytes(group.Sum(item => item.Bytes))));
            }

            builder.AppendLine();
            builder.AppendLine("Largest included assets:");
            foreach (AssetSize item in includedFiles.Take(80))
                builder.AppendLine(string.Format("{0,12}  {1}", FormatBytes(item.Bytes), item.Path));

            builder.AppendLine();
            builder.AppendLine("Large source/archive files not included in builds:");
            builder.AppendLine("These increase repository/import storage, but deleting them will not reduce the player build.");
            foreach (AssetSize item in unusedSourceFiles)
                builder.AppendLine(string.Format("{0,12}  {1}", FormatBytes(item.Bytes), item.Path));

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Logs");
            File.WriteAllText(ReportPath, builder.ToString(), new UTF8Encoding(false));
            Debug.Log("Playable Ad asset audit written to " + ReportPath);
        }

        public static void GenerateFromCommandLine()
        {
            Generate();
        }

        private static void AddDependencies(HashSet<string> destination, string[] roots)
        {
            if (roots == null || roots.Length == 0) return;
            string[] dependencies = AssetDatabase.GetDependencies(roots, true);
            for (int i = 0; i < dependencies.Length; i++)
            {
                string path = dependencies[i];
                if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    destination.Add(path);
            }
        }

        private static AssetSize CreateAssetSize(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            long length = File.Exists(fullPath) ? new FileInfo(fullPath).Length : -1L;
            return new AssetSize(assetPath, Path.GetExtension(assetPath).ToLowerInvariant(), length);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L)
                return (bytes / (1024d * 1024d * 1024d)).ToString("F2") + " GB";
            if (bytes >= 1024L * 1024L)
                return (bytes / (1024d * 1024d)).ToString("F2") + " MB";
            if (bytes >= 1024L)
                return (bytes / 1024d).ToString("F2") + " KB";
            return bytes + " B";
        }

        private readonly struct AssetSize
        {
            public readonly string Path;
            public readonly string Extension;
            public readonly long Bytes;

            public AssetSize(string path, string extension, long bytes)
            {
                Path = path;
                Extension = extension;
                Bytes = bytes;
            }
        }
    }
}
