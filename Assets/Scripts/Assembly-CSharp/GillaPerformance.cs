// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class GillaPerformance : MonoBehaviour
{
    private const float Foveation = 0.66f;
    private const float FarClip = 150f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (Application.isEditor)
        {
            return;
        }
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
        Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        GameObject go = new GameObject("GillaPerformance");
        go.hideFlags = (HideFlags)0x3d;
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GillaPerformance>();
    }

    private static void Loaded(Scene scene, LoadSceneMode mode)
    {
        GameObject baseObj = GameObject.Find("Gilla_NewBase");
        if (baseObj == null)
        {
            return;
        }
        BakedLampsOff();
        Volume[] volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.InstanceID);
        for (int i = 0; i < volumes.Length; i++)
        {
            Volume volume = volumes[i];
            if (volume.sharedProfile != null)
            {
                Bloom bloom;
                if (volume.sharedProfile.TryGet<Bloom>(out bloom))
                {
                    bloom.active = false;
                }
            }
        }
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.farClipPlane = FarClip;
        }
        GillaAreas.Make(baseObj.transform);
        GillaWarmUp.Run();
    }

    public static int BakedLampsOff()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID);
        int count = 0;
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light.enabled && light.bakingOutput.isBaked && light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked)
            {
                light.enabled = false;
                count++;
            }
        }
        return count;
    }

    private IEnumerator Start()
    {
        Apply();
        yield return new WaitForSeconds(2f);
        Apply();
    }

    private static void Apply()
    {
        List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems<XRDisplaySubsystem>(displays);
        foreach (XRDisplaySubsystem display in displays)
        {
            display.foveatedRenderingFlags = (XRDisplaySubsystem.FoveatedRenderingFlags)0;
            display.foveatedRenderingLevel = Foveation;
        }
    }
}
