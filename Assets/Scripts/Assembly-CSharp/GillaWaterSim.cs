// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;

public class GillaWaterSim : MonoBehaviour
{
    public Shader simShader;
    [Tooltip("How quick the waves spread (2 m/s or so at 0.22).")]
    public float spread = 0.22f;
    [Tooltip("How much of its speed and its height a wave keeps each step.")]
    public float damp = 0.993f;
    [Tooltip("How much of its speed and its height a wave keeps each step.")]
    public float keep = 0.9985f;
    public const int Size = 256;
    public const float Cell = 0.125f;
    private const float StepEvery = 0.013888889f;
    private const int MaxDrops = 8;
    private static GillaWaterSim instance;
    private static readonly List<Vector4> pending = new List<Vector4>();
    private static readonly Vector4[] drops = new Vector4[8];
    private static readonly int WaveTex = Shader.PropertyToID("_GillaWaveTex");
    private static readonly int WaveRect = Shader.PropertyToID("_GillaWaveRect");
    private static readonly int SolidTex = Shader.PropertyToID("_SolidTex");
    private static readonly int SolidRect = Shader.PropertyToID("_SolidRect");
    private static readonly int SimRect = Shader.PropertyToID("_SimRect");
    private static readonly int Shift = Shader.PropertyToID("_Shift");
    private static readonly int C = Shader.PropertyToID("_C");
    private static readonly int Damp = Shader.PropertyToID("_Damp");
    private static readonly int Keep = Shader.PropertyToID("_Keep");
    private static readonly int DropCount = Shader.PropertyToID("_DropCount");
    private static readonly int Drops = Shader.PropertyToID("_Drops");
    private RenderTexture front;
    private RenderTexture back;
    private Material mat;
    private int cornerX;
    private int cornerZ;
    private bool placed;
    private bool broken;
    private float budget;

    public static void Drop(Vector3 at, float strength)
    {
        if (instance == null)
        {
            return;
        }
        if (pending.Count > 15)
        {
            pending.RemoveAt(0);
        }
        float s = Mathf.Clamp01(strength);
        pending.Add(new Vector4(at.x, at.z, Mathf.Lerp(0.25f, 0.8f, s), s * 0.57f + 0.18f));
    }

    private void OnEnable()
    {

        instance = this;
    }

    private void OnDisable()
    {
        if (instance == this)
        {
            instance = null;
        }
        pending.Clear();
        Shader.SetGlobalVector(WaveRect, Vector4.zero);
    }

    private void OnDestroy()
    {
        if (front != null)
        {
            front.Release();
        }
        if (back != null)
        {
            back.Release();
        }
        if (mat != null)
        {
            Destroy(mat);
        }
    }

    private bool Ready()
    {
        if (front != null)
        {
            return true;
        }
        if (!broken && !(simShader == null))
        {
            RenderTextureFormat format = RenderTextureFormat.RGHalf;
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf))
            {
                format = RenderTextureFormat.ARGBHalf;
            }
            if (SystemInfo.SupportsRenderTextureFormat(format))
            {
                front = Make("GillaWavesA", format);
                back = Make("GillaWavesB", format);
                mat = new Material(simShader);
                return true;
            }
            broken = true;
            Debug.LogWarning("[GillaWaterSim] no float render textures here: the water stays still");
        }
        return false;
    }

    private static Texture PoolFoamMap(GillaSplashPool pool)
    {
        System.Reflection.FieldInfo f = typeof(GillaSplashPool).GetField("foamMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic); /* ? */
        return f != null ? f.GetValue(pool) as Texture : null;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        GillaSplashPool pool = null;
        if (cam != null)
        {
            pool = GillaSplashPool.Near(cam.transform.position, 16f);
        }
        if (!(pool == null))
        {
            if (!(PoolFoamMap(pool) == null) && Ready()) /* ? */
            {
                Vector3 pos = cam.transform.position;
                int cx = Mathf.RoundToInt(pos.x * 8f);
                int cz = Mathf.RoundToInt(pos.z * 8f);
                if (!placed)
                {
                    cornerX = cx - 128;
                    cornerZ = cz - 128;
                    placed = true;
                }
                budget = Mathf.Min(budget + Time.deltaTime, 0.027777778f);
                RenderTexture prev = RenderTexture.active;
                while (budget >= 0.013888889f)
                {
                    budget -= 0.013888889f;
                    Step(pool, cx - 128, cz - 128);
                }
                RenderTexture.active = prev;
                Shader.SetGlobalTexture(WaveTex, front);
                Shader.SetGlobalVector(WaveRect, new Vector4(cornerX, cornerZ, 0f, 0f) /* ? */);
                return;
            }
        }
        pending.Clear();
        Shader.SetGlobalVector(WaveRect, Vector4.zero);
        placed = false;
    }

    private void Step(GillaSplashPool pool, int x, int z)
    {
        mat.SetVector(Shift, new Vector4((x - cornerX) / 256f, (z - cornerZ) / 256f, 0f, 0f));
        cornerX = x;
        cornerZ = z;
        mat.SetVector(SimRect, new Vector4(x / 8f, z / 8f, 32f, 0f));
        mat.SetTexture(SolidTex, PoolFoamMap(pool)); /* ? */
        mat.SetVector(SolidRect, pool.FoamRect);
        mat.SetFloat(C, spread);
        mat.SetFloat(Damp, damp);
        mat.SetFloat(Keep, keep);
        int count = pending.Count;
        if (count > 7)
        {
            count = 8;
        }
        for (int i = 0; i < count; i++)
        {
            drops[i] = pending[i];
        }
        pending.RemoveRange(0, count);
        mat.SetFloat(DropCount, count);
        mat.SetVectorArray(Drops, drops);
        Graphics.Blit(front, back, mat);
        RenderTexture t = front;
        front = back;
        back = t;
    }

    internal static RenderTexture Make(string name, RenderTextureFormat format)
    {
        RenderTexture rt = new RenderTexture(256, 256, 0, format, RenderTextureReadWrite.Linear);
        rt.name = name;
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.useMipMap = false;
        rt.antiAliasing = 1;
        rt.Create();
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, new Color(0f, 0f, 0f, 0f));
        RenderTexture.active = prev;
        return rt;
    }
}
