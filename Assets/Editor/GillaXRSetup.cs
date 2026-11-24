// Editor-only: turns VR on for the Android (Quest) build (not part of the game).
// The XR settings recovered from the APK came across empty, so a build ran as a flat 2D window.
// This enables the OpenXR loader, Meta Quest support and the Touch controller profiles.
// Menu: Gilla > Set Up Quest VR. The APK build runs it automatically.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

public static class GillaXRSetup
{
    private static readonly HashSet<string> Wanted = new HashSet<string>
    {
        "MetaQuestFeature",
        "OculusTouchControllerProfile",
        "MetaQuestTouchPlusControllerProfile",
        "MetaQuestTouchProControllerProfile",
    };

    [MenuItem("Gilla/Set Up Quest VR")]
    public static void Android()
    {
        const BuildTargetGroup group = BuildTargetGroup.Android;

        // The game uses gamma colour space (as the original did), and OpenXR on Quest only
        // allows that with Vulkan, so the build uses Vulkan alone instead of Vulkan + OpenGL ES.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan });

        XRGeneralSettingsPerBuildTarget perTarget;
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget);
        if (perTarget == null)
        {
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
            AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        }
        if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
        if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

        XRGeneralSettings general = perTarget.SettingsForBuildTarget(group);
        general.InitManagerOnStart = true;
        bool loader = XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);
        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(perTarget);

        OpenXRSettings openxr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
        var on = new List<string>();
        if (openxr != null)
        {
            openxr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            foreach (OpenXRFeature feature in openxr.GetFeatures())
            {
                if (feature == null) continue;
                string type = feature.GetType().Name;
                if (Wanted.Contains(type))
                {
                    feature.enabled = true;
                    on.Add(type);
                    EditorUtility.SetDirty(feature);
                }
                else if (type == "MockRuntime" || type == "ConformanceAutomationFeature" || type == "RuntimeDebuggerOpenXRFeature")
                {
                    feature.enabled = false;
                    EditorUtility.SetDirty(feature);
                }
            }
            EditorUtility.SetDirty(openxr);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[GillaXR] OpenXR loader assigned for Android: " + loader + " (loaders now: " + general.Manager.activeLoaders.Count
            + "), features on: " + (on.Count > 0 ? string.Join(", ", on) : "NONE") + (openxr == null ? " (no OpenXR settings for Android!)" : ""));
    }
}
