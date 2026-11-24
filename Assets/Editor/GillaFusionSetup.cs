// Editor-only: creates Fusion's NetworkProjectConfig and rebuilds its prefab table (not part of the game).
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GillaFusionSetup
{
    public static void Run()
    {
        Type Find(string name) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } }).FirstOrDefault(t => t.FullName == name || t.Name == name);
        Type utils = Find("FusionGlobalScriptableObjectUtils");
        Type asset = Find("NetworkProjectConfigAsset");
        Type npc = Find("NetworkProjectConfigUtilities");
        Debug.Log("GILLAFUSION types " + utils + " | " + asset + " | " + npc);
        MethodInfo ensure = utils.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).First(m => m.Name == "EnsureAssetExists" && m.IsGenericMethod);
        object r = ensure.MakeGenericMethod(asset).Invoke(null, ensure.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray());
        Debug.Log("GILLAFUSION ensure -> " + r);
        AssetDatabase.Refresh();
        npc.GetMethod("RebuildPrefabTable", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
        AssetDatabase.SaveAssets();
        Debug.Log("GILLAFUSION done");
    }
}
