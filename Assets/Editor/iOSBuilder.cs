using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;

public class iOSBuilder
{
    public static string Execute()
    {
        string result = "";

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            return "ERROR: Active build target is not iOS. Run iOSBuildSetup first.";

        string buildPath = Path.Combine(
            Path.GetDirectoryName(Application.dataPath),
            "Builds", "iOS");

        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        result += $"Build output path: {buildPath}\n";

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            scenes = new string[]
            {
                "Assets/Scenes/LoadingScene.unity",
                "Assets/Scenes/MainmenuScene.unity",
                "Assets/Scenes/GameScene.unity"
            };
            result += "No scenes in build settings, using default scene list.\n";
        }

        result += $"Scenes to build ({scenes.Length}):\n";
        foreach (var s in scenes)
            result += $"  - {s}\n";

        result += "Starting iOS build...\n";

        var buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        result += $"\nBuild result: {summary.result}\n";
        result += $"Total time: {summary.totalTime}\n";
        result += $"Output path: {summary.outputPath}\n";

        if (summary.result == BuildResult.Succeeded)
        {
            result += "\niOS Xcode project generated successfully!\n";
            result += $"Open in Xcode: {buildPath}/Unity-iPhone.xcodeproj\n";
        }
        else
        {
            result += $"\nBuild failed with {summary.totalErrors} error(s).\n";
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error)
                        result += $"  ERROR: {msg.content}\n";
                }
            }
        }

        return result;
    }
}
