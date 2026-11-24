// Editor-only: builds the Quest APK (not part of the game).
// Menu: Gilla > Build Quest APK. Or from the command line: -executeMethod GillaBuild.Apk
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GillaBuild
{
    public const string Output = "D:/GillaUnity/Build/GillaRedux.apk";

    [MenuItem("Gilla/Build Quest APK")]
    public static void Apk()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("[GillaBuild] switching the project to Android (the first time re-imports assets and takes a while)");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.gillaredux.stripes");
        PlayerSettings.productName = "Gilla Redux";
        EditorUserBuildSettings.buildAppBundle = false;
        GillaXRSetup.Android();
        GillaLightmapSetup.Apply();

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Output,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };
        Debug.Log("[GillaBuild] building " + string.Join(", ", options.scenes) + " -> " + Output);
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary s = report.summary;
        Debug.Log("[GillaBuild] result=" + s.result + " errors=" + s.totalErrors + " warnings=" + s.totalWarnings
            + " size=" + (s.totalSize / (1024 * 1024)) + "MB time=" + s.totalTime);
        if (s.result != BuildResult.Succeeded)
        {
            foreach (var step in report.steps)
                foreach (var m in step.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception)
                        Debug.LogError("[GillaBuild] " + step.name + ": " + m.content);
        }
        else
        {
            EditorUtility.RevealInFinder(Output);
        }
    }
}
