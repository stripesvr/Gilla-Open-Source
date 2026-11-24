// Redux: build settings the restored lightmaps (GillaLightmaps) depend on.
//  - Lightmap shader variants are kept. Unity strips them when no scene has baked lighting data,
//    and ours is applied from script, so it would look unlit in a build.
//  - Lightmap encoding is Normal (RGBM), which is how the original's lightmap textures are stored.
//  - Static batching is off on Android: batched meshes ignore a lightmap slot set from script.
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class GillaLightmapSetup
{
    static GillaLightmapSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool("GillaLightmapSetup", false))
            {
                SessionState.SetBool("GillaLightmapSetup", true);
                Apply();
            }
        };
    }

    [MenuItem("Gilla/Apply Lightmap Settings")]
    public static void Apply()
    {
        try
        {
            UnityEngine.Object gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
            SerializedObject so = new SerializedObject(gs);
            Set(so, "m_LightmapStripping", 1);
            Set(so, "m_LightmapKeepPlain", 1);
            Set(so, "m_LightmapKeepDirCombined", 1);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GillaLightmapSetup] couldn't keep the lightmap shader variants: " + e.Message);
        }

        BindingFlags any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type ps = typeof(PlayerSettings);
        Type quality = ps.Assembly.GetType("UnityEditor.LightmapEncodingQuality");
        foreach (BuildTarget target in new[] { BuildTarget.Android, BuildTarget.StandaloneWindows64 })
        {
            try
            {
                MethodInfo setEnc = ps.GetMethod("SetLightmapEncodingQualityForPlatform", any);
                if (setEnc != null && quality != null)
                {
                    setEnc.Invoke(null, new object[] { target, Enum.ToObject(quality, 1) });
                }
                else
                {
                    MethodInfo old = ps.GetMethod("SetLightmapEncodingQualityForPlatformGroup", any);
                    if (old != null && quality != null)
                    {
                        old.Invoke(null, new object[] { BuildPipeline.GetBuildTargetGroup(target), Enum.ToObject(quality, 1) });
                    }
                    else
                    {
                        Debug.LogWarning("[GillaLightmapSetup] no way found to set the lightmap encoding for " + target);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GillaLightmapSetup] lightmap encoding for " + target + ": " + e.Message);
            }
        }
        try
        {
            MethodInfo setBatch = ps.GetMethod("SetBatchingForPlatform", any);
            if (setBatch != null)
            {
                setBatch.Invoke(null, new object[] { BuildTarget.Android, 0, 0 });
            }
            else
            {
                Debug.LogWarning("[GillaLightmapSetup] no way found to turn static batching off");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GillaLightmapSetup] static batching: " + e.Message);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[GillaLightmapSetup] lightmap build settings applied");
    }

    private static void Set(SerializedObject so, string name, int value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p == null) return;
        if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value != 0;
        else p.intValue = value;
    }
}
