// Redux addition (not in the original game): puts the map's baked lighting back.
// The original build lit the map with 16 baked lightmaps. The textures survived the rip, but
// the file that says which object uses which patch of which lightmap (Unity's LightingData
// asset) can't be exported. The slot of each of the 8,051 lit objects was read out of the
// original game's data instead and is stored on this component, which hands it all back to
// Unity when the map loads. The lamps that were baked into those lightmaps are switched off,
// as they are in the original, so nothing is lit twice.
using UnityEngine;

[DefaultExecutionOrder(-30000)]
public class GillaLightmaps : MonoBehaviour
{
    public Texture2D[] light;
    public Texture2D[] dir;
    public LightProbes probes;
    public Renderer[] renderers;
    public int[] index;
    public Vector4[] scaleOffset;
    public Light[] bakedLamps;

    private void Awake()
    {

        Apply();
    }

    private void OnEnable()
    {

        Apply();
    }

    public void Apply()
    {
        if (light == null || light.Length == 0 || renderers == null)
        {
            return;
        }
        LightmapData[] maps = new LightmapData[light.Length];
        bool directional = dir != null && dir.Length == light.Length;
        for (int i = 0; i < maps.Length; i++)
        {
            LightmapData d = new LightmapData();
            d.lightmapColor = light[i];
            if (directional)
            {
                d.lightmapDir = dir[i];
                if (dir[i] == null) directional = false;
            }
            maps[i] = d;
        }
        LightmapSettings.lightmapsMode = directional ? LightmapsMode.CombinedDirectional : LightmapsMode.NonDirectional;
        LightmapSettings.lightmaps = maps;
        // Redux: the 49 MB light-probe set is only handed over on the headset. In the editor it
        // seemed to stall Play for minutes; the lightmaps alone are enough to see the lighting.
        if (probes != null && !Application.isEditor)
        {
            LightmapSettings.lightProbes = probes;
        }
        int n = Mathf.Min(renderers.Length, Mathf.Min(index.Length, scaleOffset.Length));
        int done = 0;
        for (int i = 0; i < n; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            r.lightmapIndex = index[i];
            // Combined (static-batched) meshes already carry the original's lightmap placement in
            // their UVs; setting it again only makes Unity log a warning per object, which is slow.
            if (!r.isPartOfStaticBatch)
            {
                r.lightmapScaleOffset = scaleOffset[i];
            }
            done++;
        }
        int off = 0;
        if (bakedLamps != null)
        {
            for (int i = 0; i < bakedLamps.Length; i++)
            {
                Light l = bakedLamps[i];
                if (l != null && l.enabled)
                {
                    l.enabled = false;
                    off++;
                }
            }
        }
        if (off > 0 || !applied)
        {
            Debug.Log("[GillaLightmaps] " + maps.Length + " lightmaps on " + done + " objects, " + off + " baked lamps switched off");
        }
        applied = true;
    }

    private bool applied;
}
