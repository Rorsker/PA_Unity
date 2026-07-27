using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WebGLBuildExport
{
    public static void Build()
    {
        var scenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled && !string.IsNullOrEmpty(scene.path)) scenes.Add(scene.path);
        if (scenes.Count == 0) throw new System.Exception("No enabled scenes.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.StrictMode
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("WebGL build failed: " + report.summary.result);
    }
}
